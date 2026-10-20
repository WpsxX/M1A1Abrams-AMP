using System.Collections.Generic;
using GHPC.Effects;
using GHPC.Equipment;
using GHPC.Utility;
using GHPC.Vehicle;
using GHPC.Weaponry;
using ModUtil;
using UnityEngine;

namespace M1A1Abrams
{
    internal static class AmpSmoke
    {
        // Private clones: never mutate the shared vanilla smoke effect or grenade prefab.
        private static GameObject templates;
        private static readonly Dictionary<GameObject, Dictionary<AmpSmokeMode, GameObject>> grenades =
            new Dictionary<GameObject, Dictionary<AmpSmokeMode, GameObject>>();

        internal static void Reset()
        {
            grenades.Clear();
            if (templates != null) Object.Destroy(templates);
            templates = null;
        }

        internal static void Apply(Vehicle vehicle, bool m1a1)
        {
            AmpSmokeMode mode = AmpUpgrades.Smoke(m1a1);
            if (mode == AmpSmokeMode.Vanilla) return;
            bool rosy = mode == AmpSmokeMode.Rosy || mode == AmpSmokeMode.RosyPlus;
            bool plus = mode == AmpSmokeMode.RosyPlus;
            foreach (VehicleSmokeManager manager in vehicle.GetComponentsInChildren<VehicleSmokeManager>(true))
            {
                if (!manager.HasSmokeLauncher) continue;
                GameObject original = manager.PoolObject;
                GameObject configured = GetGrenade(original, mode);
                if (configured == null) continue;
                // New prefabs must be registered in the current game's object pool.
                ObjectPoolManager.Instance.Register(configured);
                Reflect.Set(manager, "_smokePrefab", configured);
                Reflect.Set(manager, "_smokeGrenadeRequiresCrewBrain", false);
                Reflect.Set(manager, "_smokeScreenRequiresCrewBrain", false);
                Reflect.Set(manager, "_launchAngle", plus ? 12f : 20f);
                // AMP ROSY+ used 500 m here to get a fast launch. With its short fuze,
                // the cloud forms tens of metres from the tank, not 500 m away.
                float distance = plus ? 500f : rosy ? 50f : 40f;
                Reflect.Set(manager, "_distanceRange", new Vector2(distance, distance));
                VehicleSmokeManager.SmokeSlot[] slots = Reflect.Get<VehicleSmokeManager.SmokeSlot[]>(manager, "_smokeSlots");
                if (slots == null) continue;
                foreach (VehicleSmokeManager.SmokeSlot slot in slots)
                    if (slot != null) slot.Rounds = Mathf.Max(slot.Rounds, rosy ? 4 : 2);
                if (rosy)
                {
                    // AMP's original twelve-tube order, angles and simultaneous salvo.
                    int[] order = { 2, 4, 5, 1, 3, 0, 6, 9, 7, 11, 10, 8 };
                    float[] angles = { -82, -67, -52, -37, -22, -7, 7, 22, 37, 52, 67, 82 };
                    if (slots.Length == 12)
                        for (int i = 0; i < order.Length; i++)
                            if (slots[order[i]] != null) slots[order[i]].Angle = angles[i];
                    // Each ROSY activation produces a broad screen from all available launchers.
                    var pattern = new List<VehicleSmokeManager.SmokePatternData>();
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] == null || slots[i].SpawnLocation == null) continue;
                        pattern.Add(new VehicleSmokeManager.SmokePatternData { SmokeSlotIndex = i });
                    }
                    Reflect.Set(manager, "_smokeGroups", new[] {
                        new VehicleSmokeManager.SmokePattern { SmokePatternData = pattern.ToArray() }
                    });
                    Reflect.Set(manager, "_currentSmokeGroupIndex", 0);
                }
            }
        }

        private static GameObject GetGrenade(GameObject original, AmpSmokeMode mode)
        {
            if (original == null) return null;
            bool rosy = mode == AmpSmokeMode.Rosy || mode == AmpSmokeMode.RosyPlus;
            bool plus = mode == AmpSmokeMode.RosyPlus;
            Dictionary<AmpSmokeMode, GameObject> variants;
            if (!grenades.TryGetValue(original, out variants))
                grenades[original] = variants = new Dictionary<AmpSmokeMode, GameObject>();
            GameObject cached;
            if (variants.TryGetValue(mode, out cached) && cached != null) return cached;
            Grenade donor = original.GetComponent<Grenade>();
            if (donor == null) return null;
            GameObject effectSource = Reflect.Get<GameObject>(donor, "_effectPrefab");
            if (effectSource == null) return null;
            if (templates == null)
            {
                templates = new GameObject("AMP smoke templates");
                templates.SetActive(false);
            }
            // Both roots retain activeSelf=true so their spawned copies run. The parent
            // stays inactive to prevent template grenades exploding during configuration.
            GameObject grenade = Object.Instantiate(original, templates.transform);
            GameObject effect = Object.Instantiate(effectSource, templates.transform);
            grenade.name = "AMP " + mode + " grenade";
            effect.name = "AMP " + mode + " multispectral cloud";
            grenade.SetActive(true);
            effect.SetActive(true);

            // A donor observed while thermals are active can have disabled particle renderers.
            // LightBandExclusiveItem snapshots that state in Awake; enable the private clone
            // before spawning so ShowInThermal can actually reveal the cloud.
            foreach (ParticleSystemRenderer renderer in effect.GetComponentsInChildren<ParticleSystemRenderer>(true))
                renderer.enabled = true;
            foreach (LightBandExclusiveItem band in effect.GetComponentsInChildren<LightBandExclusiveItem>(true))
            {
                band.ShowInStandard = true;
                band.ShowInNightVision = true;
                band.ShowInThermal = true;
            }
            if (plus)
            {
                foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (particles.name.IndexOf("Cloud", System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                    ParticleSystem.MainModule main = particles.main;
                    main.startSizeMultiplier *= 2f;
                    // Increase the existing budget moderately; avoid AMP's 12000-particle
                    // allocation on every grenade in a full salvo.
                    main.maxParticles = Mathf.Min(4000, Mathf.Max(main.maxParticles, 1000) * 2);
                }
            }
            Grenade clone = grenade.GetComponent<Grenade>();
            Reflect.Set(clone, "_effectPrefab", effect);
            if (rosy) Reflect.Set(clone, "_fuseTimeRange", new Vector2(1.3f, 1.7f));
            if (plus) Reflect.Set(clone, "_fuseTimeRange", new Vector2(0.325f, 0.375f));
            variants[mode] = grenade;
            return grenade;
        }
    }
}
