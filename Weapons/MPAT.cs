using System.Text;
using UnityEngine;
using UnityEngine.UI;
using GHPC.Weapons;
using HarmonyLib;
using GHPC.Player;
using GHPC.UI.Hud;
using GHPC.Utility;
using GHPC.PhysicsHelpers;
using GHPC;
using GHPC.Effects;
using System.Linq;

namespace M1A1Abrams
{
    public class MPATManager : MonoBehaviour
    {
        public bool ProximityActive { get; set; }
        public int AmmoCachedIdx { get; set; }


        private WeaponSystem weapon = null;
        private int self_id;
        private string[] ammoKeys;
        private LoadoutManager loadout;


        void Awake()
        {
            weapon = GetComponent<WeaponsManager>().Weapons[0].Weapon;
            weapon.Fired += OnFired;
            self_id = gameObject.GetInstanceID();
            ammoKeys = ModUtil.Reflect.GetStatic<string[]>(typeof(PlayerInput), "ammoKeys");
            loadout = GetComponent<LoadoutManager>();
        }

        void Update()
        {
            var playerUnit = PlayerInput.Instance?.CurrentPlayerUnit;
            if (playerUnit == null || playerUnit.gameObject.GetInstanceID() != self_id
                || weapon == null) return;

            // Middle mouse toggles the proximity fuze. It must be the main gun the player is holding
            // (CrowsAutoTrack binds the same button to CROWS lock/release) AND the M830A1 must be the
            // round currently selected: the flag means nothing for any other round, and the coaxial gun
            // shares this vehicle's fire control, so an FCS comparison alone let the coax toggle it too.
            // The breech is not required to hold an M830A1 (the flag survives a reload), but switching
            // to any other round clears it. The AMP round is excluded because AmpProxyManager reads the
            // same button for the XM1147 fuze mode.
            if (Input.GetKeyDown(KeyCode.Mouse2) && IsMainGunActive() && MpatLoaded()
                && weapon.CurrentAmmoType != AmpAmmo.ammo_xm1147)
            {
                ProximityActive = !ProximityActive;
                return;
            }

            if (ProximityActive && !MpatLoaded()) ProximityActive = false;

            // The double tap on the M830A1's ammo key is a second way to arm the fuze, so it needs the
            // same two conditions: the main gun held, and an M830A1 in the breech.
            if (!IsMainGunActive() || weapon.CurrentAmmoType == null
                || weapon.CurrentAmmoType.CachedIndex != AmmoCachedIdx
                || loadout == null || ammoKeys == null || InputUtil.MainPlayer == null) return;
            var clips = loadout.LoadedAmmoList.AmmoClips;
            for (int i = 0; i < clips.Length && i < ammoKeys.Length; i++)
            {
                var pattern = clips[i]?.ClipType?.MinimalPattern;
                if (pattern == null || pattern.Length == 0 || pattern[0] == null
                    || pattern[0].AmmoType.CachedIndex != AmmoCachedIdx) continue;
                if (!InputUtil.MainPlayer.GetButtonDoublePressDown(ammoKeys[i])) continue;
                ProximityActive = !ProximityActive;
                break;
            }
        }

        private void OnDestroy()
        {
            if (weapon != null) weapon.Fired -= OnFired;
        }

        /// <summary>
        /// True while the player is holding the main gun: the weapon the player has selected is this
        /// weapon. Mirrors CrowsAutoTrack.IsActiveWeapon, which owns the same middle mouse button for
        /// CROWS lock/release, so the two can never both answer one press. The weapon is compared
        /// directly rather than through its fire control, because the coaxial gun is wired to the same
        /// FCS as the main gun and an FCS comparison would also match while the player holds the coax.
        /// </summary>
        private bool IsMainGunActive()
        {
            PlayerInput input = PlayerInput.Instance;
            WeaponSystemInfo active = input == null ? null : input.CurrentPlayerWeapon;
            return active != null && active.Weapon != null && weapon != null
                && active.Weapon.GetInstanceID() == weapon.GetInstanceID();
        }

        /// <summary>
        /// True when the M830A1 is the round the player has selected: in the breech, or the clip the feed
        /// is holding or about to load. Any other round means the proximity fuze has nothing to act on.
        /// </summary>
        private bool MpatLoaded()
        {
            if (weapon == null) return false;
            if (IsMpatRound(weapon.CurrentAmmoType)) return true;

            AmmoFeed feed = weapon.Feed;
            if (feed == null) return false;
            return IsMpatClip(feed.LoadedClipType) || IsMpatClip(feed.QueuedClipType);
        }

        private bool IsMpatRound(AmmoType ammo)
        {
            return ammo != null && ammo.CachedIndex == AmmoCachedIdx;
        }

        private bool IsMpatClip(AmmoType.AmmoClip clip)
        {
            var pattern = clip == null ? null : clip.MinimalPattern;
            return pattern != null && pattern.Length > 0 && pattern[0] != null
                && pattern[0].AmmoType != null && pattern[0].AmmoType.CachedIndex == AmmoCachedIdx;
        }

        private void OnFired(AmmoType ammo, LiveRound live_round)
        {
            if (ammo != Ammo_120mm.ammo_m830a1 || live_round == null) return;
            MPAT fuse = live_round.GetComponent<MPAT>();
            if (!ProximityActive) { if (fuse != null) fuse.enabled = false; return; }
            if (fuse == null) fuse = live_round.gameObject.AddComponent<MPAT>();
            fuse.Arm(live_round);
        }
    }

    public class MPAT : MonoBehaviour
    {
        public bool detonated = false;

        // Detection delegates to ProximityFuze (the same sweep AMP uses); the 30 m downward probe is
        // kept from the original M830A1 fuze for rounds that pass just above a vehicle.
        private const float FuzeRadius = 3f;
        private const float VerticalReach = 30f;

        private LiveRound live_round;
        private Vector3 launchPosition;
        private Vector3 lastPosition;
        private bool started;
        // static so the early-out in Awake actually works: the effect object comes from the mission's
        // own assets and is the same for every MPAT.
        private static AmmoType dummy_he;

        void Awake()
        {
            live_round = this.GetComponent<LiveRound>();

            if (dummy_he != null) return;

            dummy_he = new AmmoType();
            // FirstOrDefault, not First: with no match the exception this used to throw aborted
            // AddComponent<MPAT> and the proximity round silently lost its effect. No match now just
            // leaves the cache empty so a later round can try again.
            dummy_he.DetonateEffect = Resources.FindObjectsOfTypeAll<GameObject>().Where(o => o.name == "HE_explosion").FirstOrDefault();
            if (dummy_he.DetonateEffect == null)
            {
                dummy_he = null;
                return;
            }
            dummy_he.ImpactEffectDescriptor = new ParticleEffectsManager.ImpactEffectDescriptor()
            {
                HasImpactEffect = true,
                ImpactCategory = ParticleEffectsManager.Category.HighExplosive,
                EffectSize = ParticleEffectsManager.EffectSize.MainGun,
                RicochetType = ParticleEffectsManager.RicochetType.None,
                Flags = ParticleEffectsManager.ImpactModifierFlags.Large,
                MinFilterStrictness = ParticleEffectsManager.FilterStrictness.Low
            };
        }

        internal void Arm(LiveRound round)
        {
            live_round = round;
            launchPosition = round.transform.position;
            lastPosition = round.transform.position;
            started = false;
            detonated = false;
            enabled = true;
        }

        void OnDisable() { enabled = false; }

        void Detonate(Vector3 burstPoint)
        {
            if (detonated) return;
            detonated = true;

            // Commit the predicted burst point: without this the explosion is placed wherever the
            // round happened to be sampled, which reads as "it went off a few metres off target".
            live_round.transform.position = burstPoint;

            // LiveRound only builds a fuzed effect while _armed is set (SetEffectsData bails out
            // immediately on !_armed) and its effect bookkeeping is one-shot, so a pooled round used
            // to produce spall but no blast, no sound and no impact effect at all.
            ModUtil.Reflect.Set(live_round, "_terrainHit", false);
            ModUtil.Reflect.Set(live_round, "_didTerrainHit", false);
            ModUtil.Reflect.Set(live_round, "_materialHit", ParticleEffectsManager.SurfaceMaterial.None);
            ModUtil.Reflect.Set(live_round, "_impactNormal", Vector3.zero);
            ModUtil.Reflect.Set(live_round, "_didFuzedEffect", false);
            ModUtil.Reflect.Set(live_round, "_didTerrainImpactVfx", false);
            ModUtil.Reflect.Set(live_round, "_didNonTerrainImpactVfx", false);
            ModUtil.Reflect.Set(live_round, "_fuzeCompleted", true);
            ModUtil.Reflect.Set(live_round, "_failedToDetonate", false);
            ModUtil.Reflect.Set(live_round, "_armed", true);
            ModUtil.Reflect.Set(live_round, "_exploded", false);
            if (live_round.Info != null)
                ModUtil.Reflect.Set(live_round, "_heHitNormalRha", live_round.Info.RhaToFuse);

            ModUtil.Reflect.Call(live_round, "createExplosion", false, 0f, Vector3.zero, 0.03f,
                live_round.Info != null ? live_round.Info.DetonateSpallCount : 50);
            live_round.Detonate();

            if (ParticleEffectsManager.Instance != null && dummy_he != null)
            {
                ParticleEffectsManager.Instance.CreateImpactEffectOfType(dummy_he,
                    ParticleEffectsManager.FusedStatus.Fuzed, ParticleEffectsManager.SurfaceMaterial.None, false, burstPoint);
            }

            for (int i = 0; i < 25; i++)
            {
                GHPC.Weapons.LiveRound frag;
                frag = LiveRoundMarshaller.Instance.GetRoundOfVisualType(LiveRoundMarshaller.LiveRoundVisualType.Spall)
                    .GetComponent<GHPC.Weapons.LiveRound>();
                frag.Info = Ammo_120mm.m830a1_forward_frag;
                frag.CurrentSpeed = 600f;
                frag.MaxSpeed = 600f;
                frag.IsSpall = false;
                frag.Shooter = live_round.Shooter;
                frag.transform.position = burstPoint;
                frag.transform.forward = Quaternion.Euler(
                    UnityEngine.Random.Range(-5f, 5f),
                    UnityEngine.Random.Range(-5f, 5f),
                    UnityEngine.Random.Range(-5f, 5f)
                ) * live_round.transform.forward;
                frag.Init(live_round, null);
                frag.name = "mpat forward frag " + i;
            }
        }

        void Update()
        {
            if (live_round == null || live_round.IsDestroyed || live_round.Info != Ammo_120mm.ammo_m830a1)
            { enabled = false; return; }
            if (detonated) return;

            Vector3 position = transform.position;
            if (!started)
            {
                started = true;
                lastPosition = position;
                return;
            }

            Vector3 previous = lastPosition;
            lastPosition = position;

            float clearance = Mathf.Max(FuzeRadius, live_round.Info.ArmingDistance);
            if ((position - launchPosition).sqrMagnitude < clearance * clearance) return;

            Vector3 burstPoint;
            if (ProximityFuze.TryGetBurstPoint(live_round, launchPosition, FuzeRadius, previous, position, out burstPoint))
            {
                Detonate(burstPoint);
                return;
            }

            if (TryGetBurstPointBelow(position)) Detonate(position);
        }

        private bool TryGetBurstPointBelow(Vector3 position)
        {
            ConstantsAndInfoManager constants = ConstantsAndInfoManager.Instance;
            if (constants == null) return false;

            int mask = constants.LaserRangefinderLayerMask;
            RaycastHit below;
            if (!RaycastColliderUtils.Raycast(new Ray(position, Vector3.down), out below, VerticalReach,
                    mask | RaycastColliderUtils.RAYCAST_LAYER_MASK)) return false;
            return ProximityFuze.IsTarget(below.collider, live_round);
        }
    }

    [HarmonyPatch(typeof(WeaponHud), "Update")]
    public static class AbramsAmmoHud
    {
        private static void Prefix(WeaponHud __instance, out bool __state)
        {
            __state = __instance.IsInitialized && ModUtil.Reflect.Get<float>(__instance, "_timeLeft") <= Time.deltaTime;
        }

        private static void Postfix(WeaponHud __instance, bool __state)
        {
            if (!__state) return;
            PlayerInput player_input = ModUtil.Reflect.Get<PlayerInput>(__instance, "_playerInput");
            var playerUnit = player_input?.CurrentPlayerUnit;
            WeaponSystem weapon = player_input?.CurrentPlayerWeapon?.Weapon;
            if (playerUnit == null || weapon == null) return;
            Text hud_text = ModUtil.Reflect.Get<Text>(__instance, "_hudText");
            StringBuilder hud_builder = ModUtil.Reflect.Get<StringBuilder>(__instance, "_sb");

            if (hud_text == null || hud_builder == null) return;

            bool proximity = false;
            if (weapon.CurrentAmmoType == Ammo_120mm.ammo_m830a1)
            {
                MPATManager mpat = playerUnit.GetComponent<MPATManager>();
                proximity = mpat != null && mpat.ProximityActive;
            }
            if (weapon.CurrentAmmoType == AmpAmmo.ammo_xm1147)
            {
                AmpProxyManager amp = playerUnit.GetComponent<AmpProxyManager>();
                proximity = amp != null && amp.CurrentMode == AmpFuzeMode.Proximity;
            }
            if (!proximity || weapon.Feed == null || weapon.Feed.LoadedClipType == null) return;
            // Match AMP's README: only suffix the loaded ammunition name. Keep
            // shared clips unchanged so another vehicle's fuze mode cannot leak in.
            string nativeText = hud_builder.ToString();
            string ammoLine = "\n" + weapon.Feed.LoadedClipType.Name + " (";
            int start = nativeText.IndexOf(ammoLine, System.StringComparison.Ordinal);
            if (start >= 0)
                hud_text.text = nativeText.Insert(start + ammoLine.Length - 2, " [Proximity]");
        }
    }
}
