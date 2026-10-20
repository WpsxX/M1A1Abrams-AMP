using System;
using System.Collections.Generic;
using System.Reflection;
using GHPC.Weapons;
using GHPC.Weaponry;
using UnityEngine;
using ModUtil;

namespace M1A1Abrams
{
    /// <summary>
    /// Ammunition added by the AMP mod.  This is deliberately separate from Ammo_120mm:
    /// the latter contains the maintained M829/M829A4 implementation used by the target build.
    /// </summary>
    public sealed class AmpAmmo : ModUtil.Module
    {
        public static AmmoType ammo_m830a2;
        public static AmmoType ammo_m830a3;
        public static AmmoType ammo_xm1147;
        public static AmmoType ammo_lahat;
        public static AmmoType ammo_m908;

        public static AmmoClipCodexScriptable clip_m830a2;
        public static AmmoClipCodexScriptable clip_m830a3;
        public static AmmoClipCodexScriptable clip_xm1147;
        public static AmmoClipCodexScriptable clip_lahat;
        public static AmmoClipCodexScriptable clip_m908;

        public static readonly Dictionary<string, AmmoClipCodexScriptable> All =
            new Dictionary<string, AmmoClipCodexScriptable>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<GameObject> visuals = new List<GameObject>();
        private static readonly List<ScriptableObject> codexes = new List<ScriptableObject>();

        public override void UnloadDynamicAssets()
        {
            foreach (GameObject visual in visuals)
            {
                if (visual != null) GameObject.DestroyImmediate(visual);
            }
            visuals.Clear();
            foreach (ScriptableObject codex in codexes)
                if (codex != null) ScriptableObject.DestroyImmediate(codex);
            codexes.Clear();
            All.Clear();
            ammo_m830a2 = ammo_m830a3 = ammo_xm1147 = ammo_lahat = ammo_m908 = null;
            clip_m830a2 = clip_m830a3 = clip_xm1147 = clip_lahat = clip_m908 = null;
        }

        public override void LoadDynamicAssets()
        {
            UnloadDynamicAssets();
            AmmoType he = LoadHeDonor();
            // Reuse the maintained project's donor assets instead of old AMP object names.
            ammo_m830a2 = CreateIheat("M830A2 IHEAT-FS-T", 700f, 1410f, 200, 35f);
            ammo_m830a3 = CreateIheat("M830A3 IHEAT-FS-T", 1000f, 1310f, 100, 25f);
            ammo_xm1147 = Create("XM1147 AMP-T", Assets.ammo_m456, 100f, 1410f, 11.4f, 4.14f);
            ammo_lahat = CreateLahat();
            // Original AMP inherits the full HE behavior/effects, not M456's HEAT descriptors.
            ammo_m908 = Create("M908 HE-OR-T", he, 100f, 1410f, 11.4f, 4.8f);
            // 3OF26 has a separate propellant charge; Abrams slots have no paired
            // charge slot. Retain its HE effects but store a one-piece cartridge.
            ammo_m908.VisualModel = Assets.ammo_m456.VisualModel;
            ammo_m908.CertainRicochetAngle = 13f;

            ConfigureExplosive(ammo_xm1147, AmpFeatures.FragmentCount(AmpFeatures.AmpFragments, 600), 50f, 120f);
            ConfigureExplosive(ammo_m908, AmpFeatures.FragmentCount(AmpFeatures.HeorFragments, 300), 25f, 75f);
            ammo_xm1147.SpallMultiplier = 3f;
            ConfigureAmpExplosion(he);
            RefreshEraOptimizations();

            clip_m830a2 = CreateClip("M830A2", ammo_m830a2);
            clip_m830a3 = CreateClip("M830A3", ammo_m830a3);
            clip_xm1147 = CreateClip("XM1147", ammo_xm1147);
            clip_lahat = CreateClip("LAHAT", ammo_lahat);
            clip_m908 = CreateClip("M908", ammo_m908);

            Add("M830A2", clip_m830a2);
            Add("M830A3", clip_m830a3);
            Add("XM1147", clip_xm1147);
            Add("LAHAT", clip_lahat);
            Add("M908", clip_m908);
        }

        private static AmmoType FindAmmo(string codexName, string ammoName)
        {
            foreach (AmmoCodexScriptable codex in Resources.FindObjectsOfTypeAll<AmmoCodexScriptable>())
                if (codex != null && codex.AmmoType != null &&
                    ((codexName != null && codex.name == codexName) ||
                     (ammoName != null && codex.AmmoType.Name == ammoName))) return codex.AmmoType;
            return null;
        }

        /// <summary>
        /// Missile donor lookup that refuses entries whose in-flight visual is not loaded yet.
        /// A codex can be resident while its ShotVisual prefab still lives in a bundle nobody
        /// loaded, and a fake-null visual is how the LAHAT previously fell back to the TOW.
        /// </summary>
        private static AmmoType FindMissileDonor(params string[] codex_names)
        {
            for (int i = 0; i < codex_names.Length; i++)
            {
                AmmoType found = FindAmmo(codex_names[i], null);
                if (found != null && found.ShotVisual != null) return found;
            }

            // Last resort: any guided round already in memory that has a flight visual.
            foreach (AmmoCodexScriptable codex in Resources.FindObjectsOfTypeAll<AmmoCodexScriptable>())
            {
                if (codex == null || codex.AmmoType == null) continue;
                AmmoType candidate = codex.AmmoType;
                if (candidate.ShotVisual == null) continue;
                if (candidate.Guidance == AmmoType.GuidanceType.Unguided) continue;
                return candidate;
            }

            return null;
        }

        private static AmmoType LoadHeDonor()
        {
            AmmoType he = FindAmmo(null, "3OF26 HEF-FS-T");
            if (he == null)
            {
                AssetUtil.LoadVanillaVehicle("T80B");
                he = FindAmmo(null, "3OF26 HEF-FS-T");
            }
            if (he == null) throw new InvalidOperationException("XM1147/M908: native 3OF26 donor missing.");
            return he;
        }

        private static void ConfigureAmpExplosion(AmmoType he)
        {
            // AMP combines M456 ballistics with 3OF26 explosion/flight visuals.
            ammo_xm1147.DetonateEffect = he.DetonateEffect;
            ammo_xm1147.ImpactAudio = he.ImpactAudio;
            ammo_xm1147.ImpactEffectDescriptor = he.ImpactEffectDescriptor;
            ammo_xm1147.ShotVisual = he.ShotVisual;
            ammo_xm1147.VisualType = he.VisualType;
        }

        private static AmmoType CreateLahat()
        {
            // Everything the player sees and hears is the I-TOW's, on purpose: in-flight
            // model, motor sound, exhaust trail, detonation, impact effects and decals. Only
            // the flight profile and the warhead numbers below are the LAHAT's own, so the
            // round looks and sounds like a TOW while it flies the top-attack geometry.
            AmmoType tow = FindAmmo(null, "BGM-71C I-TOW");
            if (tow == null)
            {
                AssetUtil.LoadVanillaVehicle("M2BRADLEY");
                tow = FindAmmo(null, "BGM-71C I-TOW");
            }

            // Fallback only: if the TOW is somehow absent, borrow another guided round so the
            // LAHAT still has a flight visual instead of throwing.
            AmmoType donor = tow;
            if (donor == null || donor.ShotVisual == null)
            {
                AmmoType missile = FindMissileDonor("ammo_9M112M");
                if (missile == null)
                {
                    AssetUtil.LoadVanillaVehicle("T80B");
                    missile = FindMissileDonor("ammo_9M112M");
                }
                if (missile != null) donor = missile;
            }
            if (donor == null) throw new InvalidOperationException("LAHAT: no usable missile donor (I-TOW or 9M112M).");

            AmmoType ammo = new AmmoType();
            Util.ShallowCopy(ammo, donor);
            ammo.DetonateEffect = donor.DetonateEffect;
            ammo.TerrainImpactEffect = donor.TerrainImpactEffect;
            ammo.ImpactEffectDescriptor = donor.ImpactEffectDescriptor;
            ammo.ImpactDecalDescriptor = donor.ImpactDecalDescriptor;
            ammo.Name = "LAHAT";
            ammo.Caliber = 120f;
            ammo.ArmingDistance = 50f;
            ammo.CertainRicochetAngle = 3f;
            ammo.Guidance = AmmoType.GuidanceType.Laser;
            ammo.MaximumRange = 6000f;
            ammo.MinSpallRha = 5f;
            ammo.MaxSpallRha = 15f;
            ammo.MuzzleVelocity = 300f;
            ammo.Mass = 13f;
            ammo.RhaPenetration = 800f;
            ammo.SpallMultiplier = 2f;
            // Guidance numbers for the engine's own AmmoType.FlightPattern.TopAttack
            // autopilot (MissileGuidanceUnit.PerformGuidance). LahatTopAttack flips
            // ammo.Flight between Direct and TopAttack on the middle mouse button and
            // re-applies these values, so keep the two in step.
            //   TurnSpeed 2.5 rad/s: fast enough to convert the cruise into the dive in time
            //   ClimbAngle 20 deg / LoiterAltitude 140 m: climb-out then cruise height
            //   DiveAngle 34 deg: threshold the autopilot uses to enter the dive, which
            //                     produces a ~30 degree impact beyond 800 m
            // Measured against the decompiled autopilot: within 2.5 m of the designator
            // from 400 m to 3000 m.
            //   TurnSpeed 6.0 rad/s: strong tracking. At 300 m/s the minimum turn radius drops
            //                     to 50 m, so the last-second corrections in the dive can actually
            //                     be flown instead of being limited by the airframe. LahatTopAttack
            //                     drops it back to the original 1.5 rad/s while a direct round is in
            //                     the air, because both modes share this one AmmoType.
            ammo.TurnSpeed = 6f;
            ammo.ClimbAngle = 20f;
            ammo.DiveAngle = 34f;
            ammo.LoiterAltitude = 140f;
            ammo.AimPointMarch = 2f;
            // The level run before the climb scales with the range, so it cannot live in the
            // AmmoType (one shared value): LahatTopAttack holds the round straight for a
            // distance proportional to the shot and then commands the climb itself.
            ammo.GuidanceLockoutTime = 0f;
            ammo.GuidanceNoLockoutRange = 0f;
            ammo.RangedFuseTime = 20f;
            ammo.Tandem = true;
            ammo.TntEquivalentKg = 4.5f;
            ammo.UseErrorCorrection = false;
            ammo.UseTracer = true;
            ammo.NoLeadCompensation = true;
            ammo.ImpactAudio = GHPC.Audio.ImpactAudioType.Missile;
            ammo.ShortName = AmmoType.AmmoShortName.Missile;
            ammo.ShotVisual = donor.ShotVisual;
            ammo.VisualType = donor.VisualType;
            // The in-flight model carries the TOW's motor flame and its audio, so the LAHAT has
            // to use the donor's visual as well. M456's model is a bare 105 mm round with no
            // motor emitter, which is why the launch sound went missing. Fall back only if the
            // donor has no visual at all, so the missile never turns invisible.
            // The stored (ammo rack) model stays a 105 mm round: the rack lays every round down by
            // rotating its visual 90 degrees about X, which only reads correctly for a model
            // authored standing on its base. The TOW donor is modelled in flight orientation (long
            // axis on Z), so using it here made the missile stand on its tail in the bustle. The
            // TOW look belongs to the round in flight, and ShotVisual above already carries it.
            ammo.VisualModel = Assets.ammo_m456.VisualModel;
            ammo.Flight = AmmoType.FlightPattern.Direct;
            ammo.CachedIndex = -1;
            return ammo;
        }

        private static AmmoType CreateIheat(string name, float penetration, float velocity, int fragments, float maxSpall)        {
            AmmoType ammo = Create(name, Assets.ammo_m456, penetration, velocity, 13.5f, 2.721f);
            ammo.DetonateSpallCount = fragments;
            ammo.MinSpallRha = 5f;
            ammo.MaxSpallRha = maxSpall;
            ammo.SpallMultiplier = 2f;
            return ammo;
        }

        private static readonly HashSet<string> eraNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "kontakt-1 armour", "kontakt-5 armour", "relikt armour",
            "ARAT-1 Armor Codex", "BRAT-M3 Armor Codex", "BRAT-M5 Armor Codex"
        };

        internal static void RefreshEraOptimizations()
        {
            // PIL capitalizes its current codex names. Repeat at vehicle conversion so
            // optional mods can finish creating their armor after our ammo assets load.
            // LiveRound reads these arrays on impact; the visual cache needs no rebuild.
            var iheat = new List<AmmoType.ArmorOptimization>();
            var lahat = new List<AmmoType.ArmorOptimization>();
            var armors = new HashSet<ArmorCodexScriptable>();
            foreach (ArmorCodexScriptable armor in Resources.FindObjectsOfTypeAll<ArmorCodexScriptable>())
            {
                if (armor == null || !eraNames.Contains(armor.name)) continue;
                armors.Add(armor);
            }
            // PIL names its codex only when fitting ERA to a vehicle, which can
            // happen after our PlayerReady callback. Bind its shared schema directly.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (string name in new[] { "Kontakt1", "Kontakt5", "Relikt" })
                {
                    Type type = assembly.GetType("PactIncreasedLethality." + name, false);
                    if (type == null) continue;
                    try
                    {
                        object schema = type.GetField("schema", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                        ArmorCodexScriptable armor = schema?.GetType().GetField("era_so")?.GetValue(schema) as ArmorCodexScriptable;
                        if (armor != null) armors.Add(armor);
                    }
                    catch (Exception error)
                    {
                        MelonLoader.MelonLogger.Warning("[AMP] Optional PIL ERA binding failed for " + name + ": " + error.Message);
                    }
                }
            }
            foreach (ArmorCodexScriptable armor in armors)
            {
                iheat.Add(new AmmoType.ArmorOptimization { Armor = armor, RhaRatio = 0.15f });
                lahat.Add(new AmmoType.ArmorOptimization { Armor = armor, RhaRatio = 0.2f });
            }
            if (ammo_m830a2 != null) ammo_m830a2.ArmorOptimizations = iheat.ToArray();
            if (ammo_m830a3 != null) ammo_m830a3.ArmorOptimizations = iheat.ToArray();
            if (ammo_lahat != null) ammo_lahat.ArmorOptimizations = lahat.ToArray();
        }

        private static AmmoType Create(string name, AmmoType donor, float penetration,
            float velocity, float mass, float tnt)
        {
            AmmoType ammo = new AmmoType();
            Util.ShallowCopy(ammo, donor);
            ammo.Name = name;
            ammo.Caliber = 120;
            ammo.MuzzleVelocity = velocity;
            ammo.Mass = mass;
            ammo.RhaPenetration = penetration;
            ammo.TntEquivalentKg = tnt;
            ammo.CertainRicochetAngle = 0f;
            ammo.ShatterOnRicochet = false;
            ammo.CachedIndex = -1;
            return ammo;
        }

        private static void ConfigureExplosive(AmmoType ammo, int fragments, float minSpall, float maxSpall)
        {
            ammo.Category = AmmoType.AmmoCategory.Explosive;
            ammo.ShortName = AmmoType.AmmoShortName.He;
            ammo.Coeff = 0.16f;
            ammo.ImpactFuseTime = 0.002f;
            // Load-bearing: this is one AmmoType shared by every round of this type in the game,
            // and LiveRound.Start seeds the native timed fuze from it. The AMP fuze runs its own
            // per-round timer, so this has to stay 0 - a nonzero value here would arm the native
            // fuze on every round at once and could not be programmed per shot.
            ammo.RangedFuseTime = 0f;
            ammo.DetonateSpallCount = fragments;
            ammo.MinSpallRha = minSpall;
            ammo.MaxSpallRha = maxSpall;
        }

        private static AmmoClipCodexScriptable CreateClip(string key, AmmoType ammo)
        {
            // Cache only after the final explosive/guidance descriptors are set.
            Util.CacheAmmo(ammo);
            AmmoCodexScriptable codex = ScriptableObject.CreateInstance<AmmoCodexScriptable>();
            codexes.Add(codex);
            codex.name = "ammo_" + key.ToLowerInvariant();
            codex.AmmoType = ammo;

            AmmoType.AmmoClip clip = new AmmoType.AmmoClip();
            clip.Capacity = 1;
            clip.Name = ammo.Name;
            clip.MinimalPattern = new[] { codex };

            AmmoClipCodexScriptable clipCodex = ScriptableObject.CreateInstance<AmmoClipCodexScriptable>();
            codexes.Add(clipCodex);
            clipCodex.name = "clip_" + key.ToLowerInvariant();
            clipCodex.ClipType = clip;
            clipCodex.CompatibleWeaponSystems = new WeaponSystemCodexScriptable[0];

            GameObject donorVisual = ammo.VisualModel;
            if (donorVisual != null)
            {
                GameObject visual = GameObject.Instantiate(donorVisual);
                visual.name = key + " visual";
                AmmoStoredVisual stored = visual.GetComponent<AmmoStoredVisual>();
                if (stored == null) stored = visual.AddComponent<AmmoStoredVisual>();
                stored.AmmoType = ammo;
                stored.AmmoScriptable = codex;
                ammo.VisualModel = visual;
                visuals.Add(visual);
            }
            return clipCodex;
        }

        private static void Add(string key, AmmoClipCodexScriptable clip)
        {
            All[key] = clip;
        }

    }
}

