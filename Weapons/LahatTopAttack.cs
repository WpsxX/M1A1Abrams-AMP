using System;
using System.Collections.Generic;
using System.Text;
using GHPC;
using GHPC.Infantry;
using GHPC.PhysicsHelpers;
using GHPC.Player;
using GHPC.UI.Hud;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weapons;
using GHPC.World;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace M1A1Abrams
{
    /// <summary>
    /// Laser-guided top attack for the LAHAT.
    ///
    /// There is no rangefinder lock any more. The missile follows the gunner's sight line for
    /// the whole flight, and the lock is re-decided every frame:
    ///
    ///   sight ray hits an enemy vehicle -> LOCKED   : fly the top attack profile onto its roof
    ///   sight ray hits anything else     -> UNLOCKED : fly the same top attack profile, aimed at
    ///                                                  a point at the range frozen when the round
    ///                                                  was fired (or when it lost the lock) along
    ///                                                  the current sight direction
    ///
    /// A beam that lands on the ground short of the target - a slope, a berm, our own hull - used
    /// to leave the shot unlocked for its whole flight, so a narrow cone around the sight
    /// direction is the fallback: the nearest enemy vehicle inside ConeLockDegrees still locks.
    ///
    /// Losing the lock is not permanent: put the crosshair back on a vehicle and the round locks
    /// again, even if it was fired with nothing designated. That makes the weapon deliberately
    /// unforgiving - the gunner has to keep the sight on the target for the whole flight.
    ///
    /// Flight profile (top attack mode, locked or not):
    ///   Launch run - fixed straight run off the muzzle, unsteered.
    ///   Climb      - ClimbAngle up to the cruise height for the current range.
    ///   Level      - level flight until only the fixed dive run is left.
    ///   Dive       - Terminal(), which never commands a climb again, so an overshoot ends in
    ///                the ground instead of a loop.
    /// Direct fire is not flown here at all: those rounds stay with the vanilla laser autopilot,
    /// exactly as they flew before the top attack mode existed.
    ///
    /// Steering is written through LiveRound.GoalVector; the engine rotates the velocity towards
    /// it at AmmoType.TurnSpeed, so the visible path stays a smooth arc.
    /// </summary>
    public sealed class LahatTopAttack : MonoBehaviour
    {
        // Climb and dive are fixed-distance legs: the airframe always climbs along ClimbAngle for
        // the same horizontal run and always descends along DiveAngle for the same horizontal run,
        // so the shape of the profile does not change with the shot. Only the level leg between
        // them grows with the range, which is what the gunner sees as "the missile cruises longer
        // on a long shot". A very short shot shrinks the whole climb instead of dropping the dive.
        public static float LaunchRun = 60f;         // metres of straight flight off the muzzle
        public static float CruiseHeight = 120f;     // metres, the fixed cruise altitude
        public static float CruiseMin = 30f;         // metres, floor when the shot is too short
        public static float ClimbAngle = 20f;        // degrees, climb-out
        public static float DiveAngle = 34f;         // degrees, threshold that starts the dive
        public static float AimPointMarch = 2f;      // metres, aim point pulled past the target
        public static float MaxLockRange = 6000f;    // metres, fallback when the FCS has no laser range
        public static float LockHullRadius = 6.5f;   // metres: the hit must be on the hull, not a stray collider
        public static float RoofInset = 0.35f;       // metres below the hull top, i.e. the turret roof
        public static float RoofMaxOffset = 1.2f;    // sanity cap on roof height above the hull centre
        public static float CommandSmoothing = 3f;   // 1/s, how fast the steering command may turn
        public static float SmoothingCutoff = 150f;  // metres; inside this the command is used raw
        public static float ConeLockDegrees = 4f;    // degrees, fallback cone when the laser ray misses

        private static Unit[] unit_cache;
        private static float next_unit_scan;

        /// <summary>One sight-ray sample: where the laser points, how far, and what it hit.</summary>
        private struct LaserSpot
        {
            internal bool valid;
            internal Vector3 origin;
            internal Vector3 direction;
            internal Vector3 point;
            internal float distance;
            internal Unit unit;      // null unless the ray landed on a lockable enemy vehicle
        }

        private sealed class Track
        {
            internal float launched;
            internal int stage;              // 0 launch run, 1 climb, 2 level, 3 terminal
            internal Vector3 start_position; // where the round left the tube, for the launch run
            internal float launch_distance;  // metres of straight flight before the climb
            internal float climb_distance;   // metres of horizontal run spent climbing
            internal float level_distance;   // metres of cruise, the only leg that follows the range
            internal float dive_distance;    // metres of horizontal run left for the descent
            internal float cruise_height;    // metres, recomputed from the live range
            internal Vector3 roof_offset;    // unit centre -> turret roof, re-sampled while locked
            internal float next_roof;        // when to re-sample the roof offset from the live bounds
            internal float last_range;       // horizontal distance to the aim point last frame
            internal Vector3 command;        // filtered steering command (smoothed stage transitions)
            internal Unit locked_unit;       // null while unlocked
            internal float frozen_range;     // metres, used while unlocked
            internal bool mode_top_attack;   // the mode the round was fired in, fixed for its flight
        }

        private readonly Dictionary<LiveRound, Track> tracks = new Dictionary<LiveRound, Track>();
        private WeaponSystem weapon;
        private MissileGuidanceUnit unit;
        private FireControlSystem fcs;
        private bool reported_missing_fcs;
        private bool top_attack;

        internal bool Mode { get { return top_attack; } }

        // The HUD patch runs from WeaponHud.Update, which has no handle on the vehicle, so the
        // controller publishes itself here. Only one Abrams guidance computer exists per vehicle.
        internal static LahatTopAttack Instance;

        internal void Bind(WeaponSystem gun, MissileGuidanceUnit guidance)
        {
            if (weapon != null) weapon.Fired -= OnFired;
            weapon = gun;
            unit = guidance;
            fcs = ResolveFcs(gun);
            reported_missing_fcs = false;
            Instance = this;
            if (weapon != null) weapon.Fired += OnFired;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (weapon != null) weapon.Fired -= OnFired;
            tracks.Clear();
        }

        private void Update()
        {
            if (weapon == null) return;

            // Same key as the MPAT fuse, but a different round: only one of the two can
            // answer a press because each requires its own ammunition to be loaded.
            if (!Input.GetKeyDown(KeyCode.Mouse2)) return;

            bool main_gun = IsMainGunActive();
            bool lahat = LahatLoaded();
            if (!main_gun || !lahat) return;

            top_attack = !top_attack;
        }

        // ------------------------------------------------------------------
        // Firing
        // ------------------------------------------------------------------

        private void OnFired(AmmoType ammo, LiveRound round)
        {
            if (round == null || ammo != AmpAmmo.ammo_lahat) return;

            // A pooled round must never inherit the previous shot's steering.
            round.GoalVector = round.transform.forward;
            // Fix the airframe limit for this shot before the first guidance tick, so a direct
            // round never spends its opening frames at the top attack turn rate.
            SetAirframe(top_attack);

            // Nothing is locked at launch. The only thing taken from this instant is the range:
            // an unlocked round attacks the point the sight was on when the trigger was pulled,
            // and keeps sliding along the current sight direction from there.
            LaserSpot spot = GetLaserSpot();
            Track track = new Track
            {
                launched = Time.time,
                stage = 0,
                start_position = round.transform.position,
                launch_distance = LaunchRun,
                cruise_height = CruiseHeight,
                frozen_range = spot.valid ? spot.distance : MaxLockRange,
                // Captured here, at the trigger, because this Track is the one the guidance
                // loop will reuse for the whole flight.
                mode_top_attack = top_attack
            };
            tracks[round] = track;
        }

        // ------------------------------------------------------------------
        // Sight ray
        // ------------------------------------------------------------------

        /// <summary>
        /// Where the gunner's sight is pointing, using exactly the ray the vanilla rangefinder
        /// uses (same origin, same layer mask, same smoke pre-check), so a lock here means the
        /// laser really is on that vehicle. Anything else - terrain, a building, smoke - comes
        /// back with unit == null, which is the unlocked state.
        /// </summary>
        private LaserSpot GetLaserSpot()
        {
            LaserSpot spot = new LaserSpot();

            if (fcs == null && weapon != null) fcs = ResolveFcs(weapon);
            if (fcs == null || fcs.LaserOrigin == null)
            {
                if (!reported_missing_fcs)
                {
                    reported_missing_fcs = true;
                    MelonLogger.Error("[M1A1] LAHAT: no laser sight found on "
                        + (weapon == null ? "the gun" : weapon.name)
                        + " (weapon.FCS=" + (weapon == null || weapon.FCS == null ? "null" : "set")
                        + ", vehicle FCS=" + (weapon == null || weapon.GetComponentInParent<Vehicle>() == null
                            ? "no vehicle" : "searched")
                        + "); the round will fly straight until this is fixed.");
                }
                return spot;
            }

            Vector3 origin = fcs.LaserOrigin.position;
            Vector3 direction = fcs.LaserOrigin.forward;
            if (direction.sqrMagnitude < 0.0001f) return spot;

            float max_range = fcs.MaxLaserRange > 0f ? fcs.MaxLaserRange : MaxLockRange;
            Ray ray = new Ray(origin, direction);

            // Smoke is a blocker, exactly like it is for the rangefinder: if the beam stops in a
            // smoke cloud the round must not lock the vehicle behind it.
            float smoke_distance = -1f;
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, max_range, 1 << CodeUtils.LAYER_INDEX_VISIBILITYONLY)
                && hit.collider != null && hit.collider.tag == "Smoke")
            {
                smoke_distance = hit.distance;
            }

            float collider_distance = -1f;
            Unit collider_unit = null;
            RaycastHit collider_hit;
            if (RaycastColliderUtils.Raycast(ray, out collider_hit, max_range,
                    ConstantsAndInfoManager.Instance.LaserRangefinderLayerMask))
            {
                collider_distance = collider_hit.distance;
                collider_unit = ResolveLockable(collider_hit);
            }

            float distance;
            Unit target;
            if (collider_distance >= 0f && (smoke_distance < 0f || collider_distance <= smoke_distance))
            {
                distance = collider_distance;
                target = collider_unit;
            }
            else if (smoke_distance >= 0f)
            {
                distance = smoke_distance;
                target = null;
            }
            else
            {
                // Nothing under the beam: the round still has somewhere to go, at the far end.
                distance = max_range;
                target = null;
            }

            // The beam is a single line and in practice spends most of its time on the ground
            // short of the target, or clipped by our own hull, which left every shot unlocked
            // and therefore flying straight. A narrow cone around the sight direction recovers
            // those locks without turning the sight into an aimbot.
            if (target == null && smoke_distance < 0f) target = FindUnitInCone(origin, direction);

            spot.valid = true;
            spot.origin = origin;
            spot.direction = direction;
            spot.distance = distance;
            spot.point = origin + direction * distance;
            spot.unit = target;
            return spot;
        }

        /// <summary>
        /// The unit a laser hit can lock, or null. Strict on purpose: the hit has to be on the
        /// hull of a live enemy vehicle, close enough to its centre that a graze counts as a
        /// miss, and infantry is never a target.
        /// </summary>
        private Unit ResolveLockable(RaycastHit hit)
        {
            if (hit.collider == null) return null;

            Unit candidate = hit.collider.GetComponentInParent<Unit>();
            if (candidate == null || candidate.Neutralized || candidate.Center == null) return null;
            if (candidate is InfantryUnit) return null;

            Unit own = PlayerInput.Instance == null ? null : PlayerInput.Instance.CurrentPlayerUnit;
            if (own != null && candidate == own) return null;

            Faction own_faction = own == null ? Faction.Blue : own.Allegiance;
            if (candidate.Allegiance == own_faction) return null;

            Vector3 delta = hit.point - candidate.Center.position;
            delta.y = 0f;
            if (delta.magnitude > LockHullRadius) return null;

            return candidate;
        }

        /// <summary>
        /// Fallback lock: the nearest enemy vehicle inside a narrow cone around the sight ray.
        /// The ray itself has to hit a collider on the hull, which fails whenever the beam lands on
        /// a slope, a berm or the edge of the target, and a round that never locks can only fly
        /// straight. The cone keeps the same intent (only what is under the reticle counts) but is
        /// forgiving about where exactly the line lands.
        /// </summary>
        private static Unit FindUnitInCone(Vector3 origin, Vector3 direction)
        {
            if (unit_cache == null || Time.time >= next_unit_scan)
            {
                next_unit_scan = Time.time + 0.5f;
                unit_cache = UnityEngine.Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
            }

            if (unit_cache == null || unit_cache.Length == 0) return null;

            Vector3 dir = direction.normalized;
            Unit own = PlayerInput.Instance == null ? null : PlayerInput.Instance.CurrentPlayerUnit;
            Faction own_faction = own == null ? Faction.Blue : own.Allegiance;

            Unit best = null;
            float best_angle = ConeLockDegrees;

            for (int i = 0; i < unit_cache.Length; i++)
            {
                Unit candidate = unit_cache[i];
                if (candidate == null || candidate.Neutralized || candidate.Center == null) continue;
                if (candidate is InfantryUnit) continue;
                if (candidate == own) continue;
                if (candidate.Allegiance == own_faction) continue;

                Vector3 to = candidate.Center.position - origin;
                float distance = to.magnitude;
                if (distance < 1f || distance > MaxLockRange) continue;

                float angle = Vector3.Angle(dir, to);
                if (angle > best_angle) continue;

                best_angle = angle;
                best = candidate;
            }

            return best;
        }

        // ------------------------------------------------------------------
        // Steering
        // ------------------------------------------------------------------

        /// <summary>
        /// Flies every LAHAT in the air and reports whether the vanilla autopilot should be
        /// skipped. Called from a Harmony prefix on MissileGuidanceUnit.PerformGuidance.
        /// </summary>
        internal bool Steer()
        {
            if (unit == null || weapon == null) return false;

            List<LiveRound> missiles = unit.CurrentMissiles;
            if (missiles == null || missiles.Count == 0)
            {
                tracks.Clear();
                SetAirframe(false);
                return false;
            }

            PruneTracks(missiles);

            LaserSpot spot = GetLaserSpot();

            bool steered = false;
            bool foreign = false;

            for (int i = 0; i < missiles.Count; i++)
            {
                LiveRound round = missiles[i];
                if (round == null) continue;
                if (round.Info != AmpAmmo.ammo_lahat) { foreign = true; continue; }

                Track track;
                if (!tracks.TryGetValue(round, out track))
                {
                    track = new Track
                    {
                        launched = Time.time,
                        stage = 0,
                        start_position = round.transform.position,
                        launch_distance = LaunchRun,
                        cruise_height = CruiseHeight,
                        frozen_range = spot.valid ? spot.distance : MaxLockRange,
                        mode_top_attack = top_attack
                    };
                    tracks[round] = track;
                }

                // Direct fire belongs to the vanilla autopilot. Leaving it alone is what keeps
                // direct mode the hand-aimed shot it was before the top attack mode existed: no
                // live lock, no roof point and no steering from here.
                if (!track.mode_top_attack) continue;

                UpdateLock(track, spot);

                Vector3 raw = Command(round, track, spot);

                float range = track.locked_unit != null && track.locked_unit.Center != null
                    ? Vector3.Distance(round.transform.position, track.locked_unit.Center.position)
                    : Vector3.Distance(round.transform.position, spot.point);

                round.GoalVector = SmoothCommand(track, raw, range);
                steered = true;
            }

            // Both modes share one AmmoType, so the airframe limit follows the rounds in the
            // air: the top attack geometry needs 6 rad/s to fly its dive, while direct fire keeps
            // the original 1.5 rad/s that made it read as hand-aimed instead of homing.
            SetAirframe(steered);

            // Only take over when every round in the air is ours and one of them is a top attack.
            return steered && !foreign;
        }

        private static void SetAirframe(bool top_attack)
        {
            if (AmpAmmo.ammo_lahat == null) return;

            float turn_rate = top_attack ? 6f : 1.5f;
            if (Mathf.Abs(AmpAmmo.ammo_lahat.TurnSpeed - turn_rate) > 0.01f)
            {
                AmpAmmo.ammo_lahat.TurnSpeed = turn_rate;
            }
        }

        /// <summary>
        /// Per-frame lock decision. A vehicle under the sight locks the round; anything else
        /// unlocks it and freezes the range at the distance the sight had at that moment, so the
        /// round keeps attacking that spot even if the gunner sweeps the sight around later.
        /// </summary>
        private void UpdateLock(Track track, LaserSpot spot)
        {
            if (spot.unit != null)
            {
                track.locked_unit = spot.unit;
                return;
            }

            if (track.locked_unit != null)
            {
                track.locked_unit = null;
                track.frozen_range = spot.valid ? spot.distance : track.frozen_range;
            }
        }

        /// <summary>
        /// First order filter on the steering command. Exponentially closing the gap to the raw
        /// command rounds every stage change into an arc; below SmoothingCutoff the filter is
        /// bypassed so the terminal phase steers exactly at the aim point.
        /// </summary>
        private Vector3 SmoothCommand(Track track, Vector3 raw, float range)
        {
            if (track.command == Vector3.zero || range <= SmoothingCutoff)
            {
                track.command = raw;
                return raw;
            }

            float blend = 1f - Mathf.Exp(-CommandSmoothing * Mathf.Max(Time.deltaTime, 0.0001f));
            track.command = Vector3.Slerp(track.command, raw, blend);
            return track.command;
        }

        private Vector3 Command(LiveRound round, Track track, LaserSpot spot)
        {
            Vector3 position = round.transform.position;
            Vector3 forward = round.transform.forward;

            // Launch run: no steering at all for a fixed distance, so the round never starts to
            // climb while the gun is still pointed at a nearby slope. Distance, not time, because
            // the leg has to be the same length whatever the muzzle velocity is.
            if (track.stage == 0)
            {
                if (Vector3.Distance(position, track.start_position) < track.launch_distance) return forward;
                track.stage = 1;
            }

            bool locked = track.locked_unit != null && !track.locked_unit.Neutralized && track.locked_unit.Center != null;

            Vector3 aim;
            if (locked)
            {
                aim = LiveAimPoint(track, track.locked_unit);
            }
            else
            {
                // Unlocked: a point at the frozen range along the live sight direction. The
                // gunner sweeping the sight drags that point around, which is exactly the
                // "attack whatever the laser is painting" behaviour.
                if (!spot.valid) return forward;
                aim = spot.origin + spot.direction * track.frozen_range;
            }

            Vector3 to_aim = aim - position;
            Vector3 flat = new Vector3(to_aim.x, 0f, to_aim.z);
            float flat_range = flat.magnitude;
            if (flat_range < 0.0001f) return to_aim;

            Vector3 flat_dir = flat.normalized;

            // Every top attack round flies the full profile, locked or not: an unlocked shot
            // still has a point to attack (the frozen spot), and dropping it straight into the
            // terminal phase made those shots look like ordinary direct fire.
            if (track.mode_top_attack)
            {
                float climb_rad = ClimbAngle * Mathf.Deg2Rad;
                float dive_rad = DiveAngle * Mathf.Deg2Rad;
                float climb_run_per_metre = 1f / Mathf.Max(Mathf.Tan(climb_rad), 0.05f);
                float dive_run_per_metre = 1f / Mathf.Max(Mathf.Tan(dive_rad), 0.05f);

                // Rotating from level flight into the dive is not instant: at 300 m/s and 6 rad/s
                // the turn into 34 degrees eats about 28 m of forward travel. The dive therefore
                // has to be commanded that much earlier, otherwise a short shot crosses the target
                // while still turning and sails over it.
                float speed = AmpAmmo.ammo_lahat == null ? 300f : Mathf.Max(AmpAmmo.ammo_lahat.MuzzleVelocity, 1f);
                float turn_rate = AmpAmmo.ammo_lahat == null ? 1f : Mathf.Max(AmpAmmo.ammo_lahat.TurnSpeed, 0.01f);
                float dive_turn_lead = speed / turn_rate * Mathf.Sin(dive_rad);

                // The climb and the dive keep a fixed horizontal run; only the level leg follows
                // the range. A short shot shrinks the climb instead of dropping the dive, so a
                // 400 m engagement is still a top attack.
                float available = Mathf.Max(flat_range - track.launch_distance, 10f);
                float budget = Mathf.Max(available - dive_turn_lead, 10f);
                float height = Mathf.Min(CruiseHeight, budget / (climb_run_per_metre + dive_run_per_metre));
                height = Mathf.Max(height, CruiseMin);

                track.cruise_height = height;
                track.climb_distance = height * climb_run_per_metre;
                track.dive_distance = height * dive_run_per_metre + dive_turn_lead;
                track.level_distance = Mathf.Max(available - track.climb_distance - track.dive_distance, 0f);

                // Climb leg: hold ClimbAngle until the cruise height is reached.
                if (track.stage == 1)
                {
                    if (position.y - aim.y < track.cruise_height)
                        return Quaternion.AngleAxis(-ClimbAngle, round.transform.right) * flat_dir;
                    track.stage = 2;
                }

                // Level leg: fly flat until only the dive run is left. Once past the target the
                // horizontal distance grows again, so that counts as "dive now", never "climb".
                if (track.stage == 2)
                {
                    bool passed_target = track.last_range > 0.5f && flat_range > track.last_range + 0.25f;
                    track.last_range = flat_range;
                    if (passed_target || flat_range <= track.dive_distance) track.stage = 3;
                    else return flat_dir;
                }

                track.stage = 3;
            }
            else
            {
                track.stage = 3;
            }

            return Terminal(round, track, aim, position, locked);
        }

        /// <summary>
        /// Terminal pursuit, and the only place that decides where a round ends up when it is not
        /// tracking a vehicle: the command never points up, so a round that overshot or lost its
        /// lock keeps sinking and buries itself in the ground instead of looping back.
        /// </summary>
        private Vector3 Terminal(LiveRound round, Track track, Vector3 aim, Vector3 position, bool locked)
        {
            Vector3 command = aim - position;

            // Inside the last 25 m the roof point is abandoned in favour of the hull centre: a
            // warhead a metre off the roof line still lands inside the vehicle, whereas aiming at
            // the roof right up to impact turns any small error into a clean miss.
            if (locked && track.locked_unit != null && track.locked_unit.Center != null)
            {
                Vector3 centre = track.locked_unit.Center.position;
                if ((centre - position).sqrMagnitude < 625f) command = centre - position;
            }

            if (command.y > 0f) command.y = 0f;

            return command;
        }

        /// <summary>
        /// The point the warhead should land on while locked: the top of the target's hull,
        /// expressed as an offset from its live centre. Re-sampled every 150 ms so the offset
        /// follows the target's current pose instead of the pose it had at launch.
        /// </summary>
        private Vector3 LiveAimPoint(Track track, Unit target)
        {
            if (target == null || target.Center == null) return track.roof_offset;

            if (Time.time >= track.next_roof)
            {
                track.next_roof = Time.time + 0.15f;
                track.roof_offset = RoofAimPoint(target) - target.Center.position;
            }

            return target.Center.position + track.roof_offset;
        }

        private void PruneTracks(List<LiveRound> missiles)
        {
            if (tracks.Count == 0) return;

            List<LiveRound> stale = null;
            foreach (KeyValuePair<LiveRound, Track> pair in tracks)
            {
                if (pair.Key != null && missiles.Contains(pair.Key)) continue;
                if (stale == null) stale = new List<LiveRound>();
                stale.Add(pair.Key);
            }

            if (stale == null) return;
            for (int i = 0; i < stale.Count; i++) tracks.Remove(stale[i]);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Top of the target's hull, i.e. the turret roof a top attack warhead should land on.
        /// Taken from the collider bounds rather than a named transform: every vehicle type
        /// carries its turret differently, but the top of the model is always the roof.
        /// </summary>
        private static Vector3 RoofAimPoint(Unit unit)
        {
            Vector3 centre = unit.Center.position;
            float top = float.NegativeInfinity;

            // Colliders are the honest size of a vehicle: the hull box spans exactly the hull, while
            // renderer bounds happily include decals, markers and helper meshes and end up metres too
            // tall. A roof that is too high turns the dive into a fly-over.
            Collider[] colliders = unit.GetComponentsInChildren<Collider>(true);
            if (colliders != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider collider = colliders[i];
                    if (collider == null || collider.isTrigger) continue;
                    Bounds candidate = collider.bounds;
                    if ((candidate.center - centre).sqrMagnitude > 225f) continue;   // further than 15 m
                    if (candidate.size.sqrMagnitude > 900f) continue;               // bigger than 30 m
                    if (candidate.max.y > top) top = candidate.max.y;
                }
            }

            if (float.IsNegativeInfinity(top))
            {
                Renderer[] renderers = unit.GetComponentsInChildren<Renderer>(true);
                if (renderers != null)
                {
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        Renderer renderer = renderers[i];
                        if (renderer == null || !renderer.enabled) continue;
                        if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                        Bounds candidate = renderer.bounds;
                        if ((candidate.center - centre).sqrMagnitude > 225f) continue;
                        if (candidate.size.sqrMagnitude > 900f) continue;
                        if (candidate.max.y > top) top = candidate.max.y;
                    }
                }
            }

            Vector3 roof = centre;
            roof.y = centre.y + (float.IsNegativeInfinity(top)
                ? 1f
                : Mathf.Clamp(top - centre.y, 0.2f, RoofMaxOffset));
            return roof;
        }

        /// <summary>
        /// The fire control computer that owns this gun's laser sight.
        ///
        /// The link only exists one way in vanilla: FireControlSystem holds the weapons
        /// (LinkedWeaponSystems), a WeaponSystem does not point back at its computer. Only the
        /// CROWS writes that back reference itself, which is why the main gun's WeaponSystem.FCS
        /// is null and the sight ray could never be cast. Try the three real sources in order.
        /// </summary>
        private static FireControlSystem ResolveFcs(WeaponSystem gun)
        {
            if (gun == null) return null;

            if (gun.FCS != null) return gun.FCS;

            Vehicle vehicle = gun.GetComponentInParent<Vehicle>();
            if (vehicle != null)
            {
                FireControlSystem[] systems = vehicle.GetComponentsInChildren<FireControlSystem>(true);
                if (systems != null)
                {
                    for (int i = 0; i < systems.Length; i++)
                    {
                        FireControlSystem candidate = systems[i];
                        if (candidate == null) continue;
                        WeaponSystem[] linked = candidate.LinkedWeaponSystems;
                        if (linked == null) continue;
                        for (int j = 0; j < linked.Length; j++)
                            if (linked[j] == gun) return candidate;
                    }
                }
            }

            PlayerInput input = PlayerInput.Instance;
            WeaponSystemInfo info = input == null ? null : input.CurrentPlayerWeapon;
            if (info != null && info.FCS != null) return info.FCS;

            return null;
        }

        private bool IsMainGunActive()
        {
            PlayerInput input = PlayerInput.Instance;
            WeaponSystemInfo active = input == null ? null : input.CurrentPlayerWeapon;
            return active != null && active.Weapon != null && weapon != null
                && active.Weapon.GetInstanceID() == weapon.GetInstanceID();
        }

        private bool LahatLoaded()
        {
            if (weapon == null || AmpAmmo.ammo_lahat == null) return false;
            if (weapon.CurrentAmmoType == AmpAmmo.ammo_lahat) return true;

            AmmoFeed feed = weapon.Feed;
            if (feed == null) return false;
            return IsLahatClip(feed.LoadedClipType) || IsLahatClip(feed.QueuedClipType);
        }

        private static bool IsLahatClip(AmmoType.AmmoClip clip)
        {
            var pattern = clip == null ? null : clip.MinimalPattern;
            return pattern != null && pattern.Length > 0 && pattern[0] != null
                && pattern[0].AmmoType == AmpAmmo.ammo_lahat;
        }

        [HarmonyPatch(typeof(MissileGuidanceUnit), "PerformGuidance")]
        private static class SteerPatch
        {
            private static bool Prefix(MissileGuidanceUnit __instance)
            {
                LahatTopAttack controller = __instance.GetComponent<LahatTopAttack>();
                if (controller == null) return true;
                return !controller.Steer();
            }
        }

        /// <summary>
        /// Marks the LAHAT line in the ammunition readout with the selected flight mode, so the
        /// middle-mouse toggle is visible instead of silent. Same technique as the MPAT fuse tag:
        /// let the game build the text, then insert the suffix into that line only.
        /// </summary>
        [HarmonyPatch(typeof(WeaponHud), "Update")]
        public static class LahatModeHud
        {
            private static void Prefix(WeaponHud __instance, out bool __state)
            {
                __state = __instance.IsInitialized
                    && ModUtil.Reflect.Get<float>(__instance, "_timeLeft") <= Time.deltaTime;
            }

            private static void Postfix(WeaponHud __instance, bool __state)
            {
                if (!__state) return;

                LahatTopAttack controller = LahatTopAttack.Instance;
                if (controller == null) return;

                PlayerInput player_input = ModUtil.Reflect.Get<PlayerInput>(__instance, "_playerInput");
                WeaponSystem weapon = player_input?.CurrentPlayerWeapon?.Weapon;
                if (weapon == null || weapon.Feed == null) return;
                if (weapon.CurrentAmmoType != AmpAmmo.ammo_lahat) return;

                AmmoType.AmmoClip clip = weapon.Feed.LoadedClipType;
                if (clip == null) return;

                Text hud_text = ModUtil.Reflect.Get<Text>(__instance, "_hudText");
                StringBuilder hud_builder = ModUtil.Reflect.Get<StringBuilder>(__instance, "_sb");
                if (hud_text == null || hud_builder == null) return;

                string native_text = hud_builder.ToString();
                string ammo_line = "\n" + clip.Name + " (";
                int start = native_text.IndexOf(ammo_line, StringComparison.Ordinal);
                if (start < 0) return;

                hud_text.text = native_text.Insert(start + ammo_line.Length - 2,
                    controller.Mode ? " [Top Attack]" : " [Direct]");
            }
        }
    }
}
