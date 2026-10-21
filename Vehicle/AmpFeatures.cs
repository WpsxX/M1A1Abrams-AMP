using System;
using System.Collections.Generic;
using HarmonyLib;
using GHPC;
using GHPC.Effects;
using GHPC.Player;
using GHPC.Vehicle;
using GHPC.Weapons;
using GHPC.Weaponry;
using GHPC.Utility;
using UnityEngine;
using ModUtil;

namespace M1A1Abrams
{
    /// <summary>Runtime switches and compatibility shims for the older AMP feature set.</summary>
    internal static class AmpFeatures
    {
        internal static MelonLoader.MelonPreferences_Entry<bool> AmpProxyFuze;
        internal static MelonLoader.MelonPreferences_Entry<float> AmpProxyDistance;
        internal static MelonLoader.MelonPreferences_Entry<int> AmpFragments;
        internal static MelonLoader.MelonPreferences_Entry<int> MpatFragments;
        internal static MelonLoader.MelonPreferences_Entry<int> HeorFragments;
        internal static MelonLoader.MelonPreferences_Entry<bool> BetterTransmission;
        internal static MelonLoader.MelonPreferences_Entry<bool> GovernorDelete;
        internal static MelonLoader.MelonPreferences_Entry<bool> BetterSuspension;
        internal static MelonLoader.MelonPreferences_Entry<bool> BetterTracks;
        internal static MelonLoader.MelonPreferences_Entry<bool> StabilityControl;
        internal static MelonLoader.MelonPreferences_Entry<bool> ApuM1;
        internal static MelonLoader.MelonPreferences_Entry<bool> ApuM1e1;
        internal static MelonLoader.MelonPreferences_Entry<bool> BetterFlir;
        internal static MelonLoader.MelonPreferences_Entry<bool> BetterDaysight;
        internal static MelonLoader.MelonPreferences_Entry<bool> BetterGas;
        internal static MelonLoader.MelonPreferences_Entry<bool> BetterSmoke;
        internal static MelonLoader.MelonPreferences_Entry<bool> Rosy;
        internal static MelonLoader.MelonPreferences_Entry<bool> CleanTurret;

        internal static void Config(MelonLoader.MelonPreferences_Category cfg)
        {
            AmpFragments = Prefs.Entry(cfg, "AMP Fragments", 600);
            AmpFragments.Comment = "XM1147 explosion fragments (impact/time/proximity fuze). Positive integer; default 600. Reload the mission after changing. Higher counts cost more performance.";
            MpatFragments = Prefs.Entry(cfg, "MPAT Fragments", 600);
            MpatFragments.Comment = "M830A1 explosion fragments (impact/proximity fuze). Positive integer; default 600. Existing 25 forward proximity fragments are additional. Reload the mission after changing.";
            HeorFragments = Prefs.Entry(cfg, "HEOR Fragments", 300);
            HeorFragments.Comment = "M908 HE-OR explosion fragments. Positive integer; default 300. Reload the mission after changing.";
            AmpProxyFuze = Prefs.Entry(cfg, "AMP Proxy Fuze", false);
            AmpProxyFuze.Comment = "true: middle mouse toggles the XM1147 between point-detonate and proximity; the HUD shows [Proximity] after the ammunition name only in proximity mode. false: time-delay airburst at the fire-control range and the middle mouse is ignored. The MPAT (M830A1) proximity fuze is always available and needs no config entry.";
            AmpProxyDistance = Prefs.Entry(cfg, "AMP Proxy Distance", 3f);
            BetterTransmission = Prefs.Entry(cfg, "Transmission+", false);
            GovernorDelete = Prefs.Entry(cfg, "GovernorDelete", false);
            GovernorDelete.Comment = "AMP speed commands: forward 36 m/s, reverse 20 m/s; removes the secondary NWH speed limiter. Does not increase engine power or gearing. Reload mission.";
            BetterSuspension = Prefs.Entry(cfg, "Suspension+", false);
            BetterTracks = Prefs.Entry(cfg, "Tracks+", false);
            StabilityControl = Prefs.Entry(cfg, "StabilityControl", false);
            StabilityControl.Comment = "Enable AMP's native lateral-slip assist at intensity 0.1. Acts while driving on the ground; reload mission.";
            ApuM1 = Prefs.Entry(cfg, "M1A1 APU", false);
            ApuM1e1 = Prefs.Entry(cfg, "M1E1 APU", false);
            BetterFlir = Prefs.Entry(cfg, "FLIR+", false);
            BetterFlir.Comment = "Enhances both GPS and CROWS thermals. Reload the mission after changing.";
            BetterDaysight = Prefs.Entry(cfg, "Daysight+", false);
            BetterDaysight.Comment = "Enhances GPS day sights independently of FLIR+. CROWS keeps its 1/5/10/15x zooms.";
            BetterGas = Prefs.Entry(cfg, "GAS+", false);
            BetterSmoke = Prefs.Entry(cfg, "Smoke+", false);
            BetterSmoke.Comment = "Enhanced smoke is visible in day/night/thermal sights, including CROWS.";
            Rosy = Prefs.Entry(cfg, "ROSY+", false);
            Rosy.Comment = "Fast multispectral smoke salvo; takes precedence over Smoke+.";
            CleanTurret = Prefs.Entry(cfg, "Clean Turret", false);
            CleanTurret.Description = "Remove items attached to turret like ALICE packs or MRE boxes";
        }

        internal static int FragmentCount(MelonLoader.MelonPreferences_Entry<int> entry, int defaultCount)
        {
            if (entry.Value > 0) return entry.Value;
            // GHPC treats a zero count as an engine fallback, not 'no fragments'.
            // Reject nonpositive settings consistently for impact and proximity fuzes.
            MelonLoader.MelonLogger.Warning("[M1A1] Fragment count must be positive; using " + defaultCount + ".");
            return defaultCount;
        }

        internal static AmmoClipCodexScriptable Clip(string key, string fallback)
        {
            AmmoClipCodexScriptable clip;
            key = (key ?? "").Trim().ToUpperInvariant();
            if (Ammo_120mm.ap.TryGetValue(key, out clip)) return clip;
            if (Ammo_120mm.heat.TryGetValue(key, out clip)) return clip;
            if (AmpAmmo.All.TryGetValue(key, out clip)) return clip;
            if (Ammo_120mm.ap.TryGetValue(fallback, out clip)) return clip;
            return Ammo_120mm.heat["M830"];
        }

        internal static void Apply(Vehicle vehicle, WeaponSystem mainGun, bool m1ip, bool m256)
        {
            if (vehicle == null) return;
            AmpCrew.Apply(vehicle, mainGun, m1ip);

            if (mainGun != null && mainGun.FCS != null && (m1ip ? ApuM1.Value : ApuM1e1.Value))
            {
                mainGun.FCS.ComputerNeedsPower = false;
                foreach (AimablePlatform mount in mainGun.FCS.Mounts)
                    if (mount != null && mount.Axis == EnumHelpers.Axis3.Y)
                        mount.SpeedUnpowered = 40f;
            }
            if (m256) ConfigureLahatGuidance(mainGun);
            ApplyTraverse(vehicle, mainGun, m1ip);
            AmpSmoke.Apply(vehicle, m1ip);

            if (CleanTurret.Value)
            {
                // The clutter does not live under the rig, so the old search of IPM1_rig/HULL/TURRET
                // found nothing and the switch looked dead. The shipped bundle keeps it in two
                // different places depending on the hull:
                //   _M1 (variant)   -> Decorations Manager/turret decorations parent
                //                      (children: luggage (1), luggage (2), luggage (3))
                //   _M1IP (variant) -> Turret Scripts/M1IP luggage
                //                      (children: config (1-4), MREs only, buckets only, us_20L_water)
                // Matching the "luggage" substring covers every one of those names, and hiding the
                // parent takes its children with it.
                foreach (Transform item in vehicle.GetComponentsInChildren<Transform>(true))
                {
                    string node = item.name.ToLowerInvariant();
                    if (node.IndexOf("luggage") >= 0 || node == "turret decorations parent")
                        item.gameObject.SetActive(false);
                }
            }
        }

        private static void ApplyTraverse(Vehicle vehicle, WeaponSystem mainGun, bool m1ip)
        {
            var chassis = vehicle.GetComponent<NwhChassis>();
            var controller = chassis == null ? vehicle.GetComponent<NWH.VehiclePhysics.VehicleController>() : chassis.VehicleController;
            if (mainGun == null || mainGun.FCS == null || controller == null) return;
            if (!AmpUpgradeRules.Traverse(AmpUpgrades.Traverse.Value,
                (m1ip ? ApuM1 : ApuM1e1).Value, controller.engine.maxPower)) return;
            foreach (AimablePlatform mount in mainGun.FCS.Mounts)
                if (mount != null && mount.Axis == EnumHelpers.Axis3.Y)
                    mount.SpeedPowered = 60f;
        }

        internal static void ApplyMobility(Vehicle vehicle, bool m1ip)
        {
            NwhChassis chassis = vehicle.GetComponent<NwhChassis>();
            NWH.VehiclePhysics.VehicleController controller = chassis == null ? null : chassis.VehicleController;
            if (controller == null) controller = vehicle.GetComponent<NWH.VehiclePhysics.VehicleController>();
            if (GovernorDelete.Value && chassis != null)
            {
                Reflect.Set(chassis, "_maxForwardSpeed", 36f);
                Reflect.Set(chassis, "_maxReverseSpeed", 20f);
            }
            if (controller == null)
            {
                MelonLoader.MelonLogger.Error("[AMP] " + vehicle.UniqueName + " has no vehicle controller; mobility upgrades were not applied.");
                return;
            }
            if (GovernorDelete.Value && controller.speedLimiter != 0f)
            {
                // NWH has a second governor which cuts engine power above 80% of
                // its cap; changing chassis speed commands alone does not remove it.
                controller.speedLimiter = 0f;
                if (!controller.drivingAssists.tcs.enabled) controller.engine.TcsPowerReduction = 0f;
            }
            if (StabilityControl.Value)
            {
                controller.drivingAssists.stability.active = true;
                controller.drivingAssists.stability.enabled = true;
                controller.drivingAssists.stability.intensity = 0.1f;
            }
            if (BetterTransmission.Value)
            {
                // Public setters rebuild and sort the internal gear list in the current API.
                var transmission = controller.transmission;
                transmission.ForwardGears = new List<float> { 8.28f, 5.81f, 2.98f, 1.76f, 1.36f, 1.16f };
                transmission.ReverseGears = new List<float> { -1.76f, -2.98f, -8.28f };
                transmission.gearMultiplier = 9.28f;
                transmission.shiftDuration = 0.1f;
                Reflect.Set(transmission, "initialShiftDuration", 0.1f);
                transmission.shiftDurationRandomness = 0.1f;
                transmission.shiftPointRandomness = 0.05f;
                // Chassis damage/repair callbacks otherwise restore the factory shift time.
                Reflect.Set(chassis, "_originalShiftDuration", 0.1f);
                transmission.Gear = transmission.Gear;
            }

            string engineChoice = ((m1ip ? M1A1.AgtM1.Value : M1A1.AgtM1e1.Value) ?? "").Trim().ToUpperInvariant();
            float power, brakeTorque, rpm;
            switch (engineChoice)
            {
                case "AGT2000": power = 1494.75f; brakeTorque = 76560f; rpm = controller.engine.maxRPM; break;
                case "AGT2500": power = 1868.33f; brakeTorque = 83520f; rpm = 4000f; break;
                case "AGT3000": power = 2264.77f; brakeTorque = 90480f; rpm = 4000f; break;
                case "T64": power = 3303.45f; brakeTorque = 97440f; rpm = 4300f; break;
                case "T55": power = 4474.2f; brakeTorque = 104400f; rpm = 4800f; break;
                default:
                    power = 0f; brakeTorque = 0f; rpm = 0f;
                    if (engineChoice != "AGT1500")
                        MelonLoader.MelonLogger.Warning("[AMP] Unknown " + (m1ip ? "M1A1" : "M1E1")
                            + " Engine '" + engineChoice + "'; retaining factory engine. Use AGT1500 / AGT2000 / AGT2500 / AGT3000 / T64 / T55.");
                    break;
            }
            // Default AGT1500/unknown values preserve the current vehicle's factory setup.
            if (power > 0)
            {
                controller.engine.maxPower = power;
                controller.engine.maxRPM = rpm;
                if (engineChoice != "AGT2000")
                {
                    controller.engine.maxRpmChange = 4000f;
                    controller.engine.minRPM = 600f;
                }
                controller.brakes.maxTorque = brakeTorque;
                Reflect.Set(chassis, "_originalEnginePower", power);
            }

            MelonLoader.MelonLogger.Msg("[AMP] " + vehicle.name + " [" + vehicle.UniqueName + "] uses "
                + (m1ip ? "M1A1" : "M1E1") + " Engine=" + engineChoice + "; mobility: GovernorDelete="
                + GovernorDelete.Value + ", forward=" + (chassis == null ? 0f : chassis.MaxForwardSpeed)
                + "m/s, reverse=" + (chassis == null ? 0f : -chassis.MinReverseSpeed)
                + "m/s, NWH limiter=" + controller.speedLimiter + ", stability="
                + controller.drivingAssists.stability.enabled + ", intensity="
                + controller.drivingAssists.stability.intensity + ", power=" + controller.engine.maxPower
                + "kW, healthy power=" + Reflect.Get<float>(chassis, "_originalEnginePower")
                + "kW, maxRPM=" + controller.engine.maxRPM + ", brake torque=" + controller.brakes.maxTorque
                + ", gears=" + controller.transmission.ForwardGearCount + "/" + controller.transmission.ReverseGearCount
                + ", shift=" + controller.transmission.shiftDuration + "s.");
            if (!BetterSuspension.Value && !BetterTracks.Value) return;
            foreach (var wheel in controller.wheels)
            {
                var wc = wheel.wheelController;
                if (wc == null) continue;
                if (BetterSuspension.Value)
                {
                    wc.damper.maxForce = 39000f;
                    wc.damper.unitBumpForce = 10000f;
                    wc.damper.unitReboundForce = 39000f;
                    wc.spring.length = 0.3643f;
                    wc.spring.maxForce = 39000f;
                    wc.spring.maxLength = 0.54f;
                }
                if (BetterTracks.Value)
                {
                    wc.fFriction.forceCoefficient = 1.25f;
                    wc.fFriction.slipCoefficient = 1f;
                    wc.sFriction.forceCoefficient = 0.85f;
                    wc.sFriction.slipCoefficient = 1f;
                    wc.TireWidth = 0.58f;
                }
            }
        }

        private static void ConfigureLahatGuidance(WeaponSystem mainGun)
        {
            if (mainGun == null || mainGun.FCS == null) return;
            Transform aim = mainGun.FCS.MainOptic == null ? mainGun.FCS.transform : mainGun.FCS.MainOptic.transform;
            if (aim == null) return;
            MissileGuidanceUnit unit = mainGun.GuidanceUnit;
            if (unit == null)
            {
                GameObject obj = new GameObject("Abrams Guidance Computer");
                obj.transform.SetParent(mainGun.FCS.transform, false);
                unit = obj.AddComponent<MissileGuidanceUnit>();
                mainGun.GuidanceUnit = unit;
            }
            unit.AimElement = aim;
            LahatGuidance guidance = unit.GetComponent<LahatGuidance>();
            if (guidance == null) guidance = unit.gameObject.AddComponent<LahatGuidance>();
            guidance.Bind(mainGun, unit);

            // Top attack sits next to the laser guidance it replaces: the middle mouse
            // selects it, and while it is on this component flies the missile itself.
            LahatTopAttack top_attack = unit.GetComponent<LahatTopAttack>();
            if (top_attack == null) top_attack = unit.gameObject.AddComponent<LahatTopAttack>();
            top_attack.Bind(mainGun, unit);
        }
    }

    internal sealed class AmpProxyManager : MonoBehaviour
    {
        private WeaponSystem weapon;
        private int selfId;
        private readonly AmpFuzeState state = new AmpFuzeState();
        internal AmpFuzeMode CurrentMode
        {
            get
            {
                var player = PlayerInput.Instance;
                bool selected = player != null && player.CurrentPlayerUnit != null
                    && player.CurrentPlayerUnit.gameObject.GetInstanceID() == selfId
                    && player.CurrentPlayerWeapon != null && player.CurrentPlayerWeapon.Weapon == weapon
                    && weapon != null && weapon.CurrentAmmoType == AmpAmmo.ammo_xm1147;
                return state.Read(AmpFeatures.AmpProxyFuze.Value,
                    selected && Input.GetKeyDown(KeyCode.Mouse2), Time.frameCount);
            }
        }

        private void Awake()
        {
            WeaponsManager manager = GetComponent<WeaponsManager>();
            weapon = manager == null || manager.Weapons == null || manager.Weapons.Length == 0 ? null : manager.Weapons[0].Weapon;
            selfId = gameObject.GetInstanceID();
            if (weapon != null) weapon.Fired += Fired;
        }

        private void OnDestroy() { if (weapon != null) weapon.Fired -= Fired; }

        private void Update()
        {
            // Also read from HUD/Fired: Unity does not guarantee their Update order.
            _ = CurrentMode;
        }

        private void Fired(AmmoType ammo, LiveRound round)
        {
            if (round == null || ammo != AmpAmmo.ammo_xm1147) return;
            AmpProxyRound fuse = round.GetComponent<AmpProxyRound>();
            if (fuse == null) fuse = round.gameObject.AddComponent<AmpProxyRound>();
            fuse.Arm(round, weapon, CurrentMode);
        }
    }

    /// <summary>
    /// Per-round AMP fuze. It is driven from inside <c>LiveRound.DoUpdate</c> so that it runs on
    /// the flight loop's own clock and sees the same start and end positions of every step; a
    /// MonoBehaviour.Update would race that loop and could only ever burst on a frame boundary.
    /// </summary>
    internal sealed class AmpProxyRound : MonoBehaviour
    {
        private LiveRound round;
        private WeaponSystem weapon;
        private bool armed;
        private bool started;
        private bool detonated;
        private Vector3 launchPosition;
        private Vector3 lastPosition;
        private bool proximity;
        private bool timed;
        private float countdown;
        private int shotId;

        private void OnDisable()
        {
            // Pooled: the round has left play and must stay inert until the next Arm.
            armed = false;
            started = false;
            round = null;
        }

        internal void Arm(LiveRound firedRound, WeaponSystem firingWeapon, AmpFuzeMode mode)
        {
            weapon = firingWeapon;
            round = firedRound;
            armed = firedRound != null;
            started = false;
            detonated = false;
            launchPosition = transform.position;
            lastPosition = launchPosition;
            shotId = firedRound == null ? 0 : firedRound.ID;
            proximity = mode == AmpFuzeMode.Proximity;
            timed = mode == AmpFuzeMode.Timed;
            countdown = 0f;
            ApplyProgram();
        }

        private static float GetTimedCountdown(LiveRound firedRound, WeaponSystem weapon)
        {
            if (firedRound == null || firedRound.Info == null) return 0.1f;
            float range = weapon != null && weapon.FCS != null ? weapon.FCS.CurrentRange : 0f;
            if (float.IsNaN(range) || float.IsInfinity(range) || range <= 1f) range = 1000f;

            float velocity = firedRound.Info.MuzzleVelocity;
            if (float.IsNaN(velocity) || float.IsInfinity(velocity) || velocity <= 1f) velocity = 1400f;

            float flight = 0f;
            try
            {
                // Time of flight to the lased surface, with no overshoot correction. The previous
                // setting added a flat 17 m (22.5 m beyond 2000 m) plus two range-relative terms
                // on top of the fire-control range, which is exactly the "bursts while still some
                // distance from the target" the timed setting was reported for. The range the
                // gunner lased is already the point the round is meant to function at.
                flight = BallisticComputerRepository.Instance.GetFlightTime(firedRound.Info, range);
            }
            catch (Exception error)
            {
                MelonLoader.MelonLogger.Warning("[AMP] XM1147 timed-fuze ballistic lookup failed: " + error.Message);
            }

            // A missing/invalid firing table must not silently disable the fuze.  This is a
            // conservative straight-line estimate and is only used when the native table fails.
            if (float.IsNaN(flight) || float.IsInfinity(flight) || flight <= 0.01f)
                flight = range / velocity;
            if (float.IsNaN(flight) || float.IsInfinity(flight) || flight <= 0.01f)
                flight = 0.1f;
            return Mathf.Clamp(flight, 0.05f, 30f);
        }

        /// <summary>Runs the fuze for one completed flight step of length <paramref name="dt"/>.</summary>
        internal void Tick(float dt)
        {
            if (!armed || detonated) return;
            if (round == null || round.IsDestroyed || round.Info != AmpAmmo.ammo_xm1147)
            {
                armed = false;
                return;
            }
            if (round.ID != shotId)
            {
                // The pooled round was reissued; this fuze state belongs to a previous shot.
                armed = false;
                return;
            }

            // LiveRound.Start is where the game seeds its own timed fuze. Waiting for the same
            // moment keeps the AMP timer from burning frames while the round is still inert.
            if (!started)
            {
                if (!Reflect.Get<bool>(round, "_hasStartBeenCalled")) return;
                started = true;
                // launchPosition and lastPosition keep what Arm recorded, which is the muzzle:
                // re-seeding them here would move the fuze's own-hull clearance datum and its
                // first sweep segment a whole flight step downrange.
                countdown = timed ? GetTimedCountdown(round, weapon) : 0f;
            }

            // The native impact and timed fuzes can fire the same round. Never detonate a second
            // time: the game's own Detonate has already run and is mid-teardown.
            if (Reflect.Get<bool>(round, "_detonated"))
            {
                armed = false;
                detonated = true;
                return;
            }

            Vector3 position = round.transform.position;
            Vector3 previous = lastPosition;
            lastPosition = position;

            if (timed)
            {
                float before = countdown;
                countdown -= dt;
                if (countdown > 0f) return;
                // Function at the instant the timer ran out, not at the end of the frame. The
                // round covers roughly 23 m per step at 1410 m/s, so bursting a whole step late
                // puts the explosion that far past the target.
                float fraction = dt > 0f && before > 0f ? Mathf.Clamp01(before / dt) : 0f;
                Detonate(Vector3.Lerp(previous, position, fraction));
                return;
            }

            if (!proximity) return;

            float radius = Mathf.Clamp(AmpFeatures.AmpProxyDistance.Value, 0.1f, 30f);
            Vector3 burstPoint;
            if (ProximityFuze.TryGetBurstPoint(round, launchPosition, radius, previous, position, out burstPoint))
                Detonate(burstPoint);
        }

        internal void ApplyProgram()
        {
            if (round == null || round.ID != shotId || detonated) return;
            // The AMP fuze owns the timer now, so the round's own timed fuze has to stay off: if
            // both ran, the game's fuze would airburst at the fire-control range while the AMP
            // fuze was still flying its own solution. The earlier attempt to program the native
            // timer could not be made per-shot at all, because RangedFuseTime lives on the shared
            // AmmoType - one instance for every XM1147 in the game - so a per-shot write leaked
            // into every later round, every AI vehicle and every other player.
            Reflect.Set(round, "_rangedFuseActive", false);
            Reflect.Set(round, "_rangedFuseCountdown", 0f);
        }

        private void Detonate(Vector3 burstPoint)
        {
            if (round == null || detonated) return;
            detonated = true;
            armed = false;
            // Commit the warhead where the fuze actually functioned. The flight loop has already
            // carried the round to the end of this step, so bursting "here" would put the
            // explosion up to a whole step past the sensed point.
            round.transform.position = burstPoint;
            // The native impact path leaves these on the round from whatever it last touched.
            // LiveRound.Detonate reads _terrainHit to pick the impact effect, so it has to be
            // cleared before the airburst is committed.
            Reflect.Set(round, "_terrainHit", false);
            Reflect.Set(round, "_didTerrainHit", false);
            Reflect.Set(round, "_materialHit", ParticleEffectsManager.SurfaceMaterial.None);
            Reflect.Set(round, "_impactNormal", Vector3.zero);
            // Detonate() runs doImpactEffect(), which suppresses the VFX unless the fuze reports
            // completed before the impact-effect bookkeeping runs. _didFuzedEffect is what
            // doImpactVFX checks to avoid a duplicate burst; clear it so this airburst draws its
            // own, and set _fuzeCompleted so SetEffectsData marks the round as fuzed rather than
            // unfuzed. Without these the explosion particle effect never spawned and only
            // createExplosion's spall appeared - fragments with no visible blast.
            Reflect.Set(round, "_didFuzedEffect", false);
            Reflect.Set(round, "_didTerrainImpactVfx", false);
            Reflect.Set(round, "_didNonTerrainImpactVfx", false);
            Reflect.Set(round, "_fuzeCompleted", true);
            Reflect.Set(round, "_failedToDetonate", false);
            // A fuze that has just functioned is armed by definition, and the game only ever
            // reports a Fuzed status for an armed round. LiveRound.SetEffectsData starts from
            // Unfuzed and returns early on `!_armed` (LiveRound.cs:1375-1391), so an airburst
            // used to be submitted as Unfuzed no matter what _fuzeCompleted said - the term is
            // evaluated after the _armed check and never reached. The game sets _armed only
            // inside the armour-impact handler (LiveRound.cs:237-247), which a mid-air burst
            // never runs. With Unfuzed, ParticleEffectsManager.CreateImpactEffectOfType could
            // not match a main-gun HE airburst, so the timed fuze played no explosion VFX and
            // no impact audio at all - the round simply appeared to keep flying until it hit
            // something. The proximity path looked healthy only because it usually coincides
            // with the round reaching the target, where the game's own impact effect runs.
            Reflect.Set(round, "_armed", true);
            Reflect.Set(round, "_heHitNormalRha", round.Info.RhaToFuse);
            // createExplosion is guarded by _exploded; clear it so a pooled round that was reused
            // without a full reset still produces its spall.
            Reflect.Set(round, "_exploded", false);
            Reflect.Call(round, "createExplosion", false, 0f, Vector3.zero, 0.03f,
                round.Info.DetonateSpallCount);
            round.Detonate();
        }
    }

    internal static class AmpAirburst
    {
        [HarmonyPatch(typeof(LiveRound), "Start")]
        private static class StartPatch
        {
            private static void Postfix(LiveRound __instance)
            {
                if (__instance == null || __instance.Info != AmpAmmo.ammo_xm1147) return;
                AmpProxyRound fuse = __instance.GetComponent<AmpProxyRound>();
                if (fuse != null) fuse.ApplyProgram();
            }
        }

        /// <summary>
        /// Drives the AMP fuze from inside the round's own flight step.
        /// </summary>
        /// <remarks>
        /// The flight loop advances a round once per frame by an entire frame of travel - roughly
        /// 23 m at 1410 m/s and 60 fps, several times the burst radius. Running the fuze from a
        /// MonoBehaviour.Update instead would sample whatever point the frame happened to end on:
        /// it tips over the fuze radius in a single step, so a target can be crossed between two
        /// samples and never sensed at all, and any burst that did happen landed at a frame
        /// boundary rather than on the sensed geometry. Patching DoUpdate gives the fuze the same
        /// dt and the same start and end positions the loop integrated.
        /// </remarks>
        [HarmonyPatch(typeof(LiveRound), "DoUpdate")]
        private static class DoUpdatePatch
        {
            private static void Postfix(LiveRound __instance, float dt)
            {
                if (__instance == null || __instance.Info != AmpAmmo.ammo_xm1147) return;
                AmpProxyRound fuse = __instance.GetComponent<AmpProxyRound>();
                if (fuse != null) fuse.Tick(dt);
            }
        }
    }
}
