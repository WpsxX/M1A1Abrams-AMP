using GHPC.AI;
using GHPC.Vehicle;
using GHPC.Weapons;
using HarmonyLib;
using ModUtil;
using UnityEngine;

namespace M1A1Abrams
{
    internal sealed class AmpCrew : MonoBehaviour
    {
        private int commander;
        private static readonly float[] ReloadSeconds = { 7f, 6f, 5f, 4f };
        private static readonly float[] ReadyDelay = { 5f, 4.5f, 3.5f, 2.5f };
        private static readonly float[] CombatSpeed = { 10f, 15f, 20f, 25f };
        private static readonly float[] FiringSpeed = { 7f, 10f, 15f, 20f };
        private static readonly float[] AccuracyDistance = { 2000f, 2000f, 2500f, 3000f };
        private static readonly float[] AccuracyMax = { 4.5f, 4f, 2.5f, 2f };
        private static readonly float[] AccuracyMin = { 2.5f, 2f, 1.5f, 1f };
        private static readonly float[] SpotDistance = { 2500f, 3000f, 3500f, 4000f };
        private static readonly float[] SpotSeconds = { 5f, 4f, 3f, 2f };
        private static readonly float[] SweepSeconds = { 5f, 5f, 4f, 3f };
        private static readonly Vector2[] IdentifySeconds =
            { new Vector2(3, 4), new Vector2(2, 3), new Vector2(1.5f, 2.5f), new Vector2(1, 2) };

        internal static void Apply(Vehicle vehicle, WeaponSystem weapon, bool m1a1)
        {
            var state = vehicle.gameObject.AddComponent<AmpCrew>();
            int gunner = AmpUpgradeRules.Skill((m1a1 ? AmpUpgrades.GunnerM1A1 : AmpUpgrades.GunnerM1E1).Value);
            int loader = AmpUpgradeRules.Skill((m1a1 ? AmpUpgrades.LoaderM1A1 : AmpUpgrades.LoaderM1E1).Value);
            state.commander = AmpUpgradeRules.Skill((m1a1 ? AmpUpgrades.CommanderM1A1 : AmpUpgrades.CommanderM1E1).Value);
            ApplyLoader(vehicle, weapon, loader);
            // AMP locates the AI below the vehicle; it need not share the root object.
            var ai = vehicle.GetComponentInChildren<UnitAI>(true);
            if (ai == null)
            {
                MelonLoader.MelonLogger.Warning("[AMP] " + vehicle.FriendlyName + " has no UnitAI; gunner/commander skills were not applied.");
                return;
            }
            ai.CombatSpeedLimit = CombatSpeed[gunner];
            ai.FiringSpeedLimit = FiringSpeed[gunner];

            // Clone nested accuracy records, so prefab data and other vehicles stay isolated.
            var source = ai.AccuracyModifiers;
            var accuracy = new AIAccuracyModifiers();
            if (source != null)
            {
                Util.ShallowCopy(accuracy.Angle, source.Angle);
                Util.ShallowCopy(accuracy.Velocity, source.Velocity);
                Util.ShallowCopy(accuracy.Range, source.Range);
            }
            accuracy.Angle.MaxDistance = AccuracyDistance[gunner];
            accuracy.Angle.MaxRadius = AccuracyMax[gunner];
            accuracy.Angle.MinRadius = AccuracyMin[gunner];
            accuracy.Angle.IncreaseAccuracyPerShot = gunner == 3;
            accuracy.Velocity.Max = 1.02f; accuracy.Velocity.Min = 0.98f;
            accuracy.Velocity.Target = 1f; accuracy.Velocity.IncreaseAccuracyPerShot = true;
            ai.AccuracyModifiers = accuracy;
            ai.AccuracyModifiers.Reset();

            ai.SpotTimeMin = 1f;
            ai.SpotTimeMax = SpotSeconds[state.commander];
            if (ai.CrewAI != null)
            {
                Reflect.Set(ai.CrewAI, "_identifyTargetDurationRange", IdentifySeconds[state.commander]);
                ResetIdentify(ai.CrewAI, state);
            }
            if (ai.GunnerAI != null && gunner == 1)
            {
                Reflect.Set(ai.GunnerAI, "_sweepAngle", 120f);
                Reflect.Set(ai.GunnerAI, "_sweepDirectionShiftSpeed", 0.25f);
                Reflect.Set(ai.GunnerAI, "_sweepSpeed", 20f);
            }
        }

        private static void ApplyLoader(Vehicle vehicle, WeaponSystem weapon, int skill)
        {
            AmmoFeed feed = weapon == null ? null : weapon.Feed;
            if (feed == null) return;
            var original = feed.ClipReloadStages;
            float total = 0f;
            if (original != null) foreach (var stage in original)
                if (stage != null) total += Reflect.Get<float>(stage, "Duration");
            if (total > 0f)
            {
                var stages = new AmmoFeed.ReloadStage[original.Length];
                for (int i = 0; i < original.Length; i++)
                {
                    stages[i] = new AmmoFeed.ReloadStage();
                    if (original[i] != null) Util.ShallowCopy(stages[i], original[i]);
                    Reflect.Set(stages[i], "Duration", original[i] == null ? 0f
                        : Reflect.Get<float>(original[i], "Duration") * ReloadSeconds[skill] / total);
                    stages[i].ResetLiveDuration();
                }
                // Changing only _totalReloadTime is lost on the next reload/health update.
                feed.ClipReloadStages = stages;
                feed.SlowReloadMultiplier = 4.5f;
                Reflect.Call(feed, "CalculateTotalTime", stages, false);
            }
            var loadout = vehicle.GetComponent<LoadoutManager>();
            if (loadout == null || loadout.RackLoadouts == null) return;
            SetRackDelay(loadout, 0, ReadyDelay[skill]);
            if (skill >= 2) SetRackDelay(loadout, 2, skill == 2 ? 7f : 6f);
        }

        private static void SetRackDelay(LoadoutManager manager, int index, float delay)
        {
            if (index >= manager.RackLoadouts.Length) return;
            var rack = manager.RackLoadouts[index].Rack;
            if (rack == null) return;
            Reflect.Set(rack, "_retrievalDelaySeconds", delay);
            Reflect.Set(rack, "_storageDelaySeconds", delay);
        }

        private static void ResetIdentify(CrewAI crew, AmpCrew state)
        {
            Vector2 range = IdentifySeconds[state.commander];
            Reflect.Set(crew, "_identifyTargetTimer", Random.Range(range.x, range.y));
        }

        [HarmonyPatch(typeof(UnitAI), "get_SpotTimeMaxDistance")]
        private static class SpotDistancePatch
        {
            private static void Postfix(UnitAI __instance, ref float __result)
            {
                var state = __instance.GetComponentInParent<AmpCrew>();
                if (state != null) __result = SpotDistance[state.commander];
            }
        }

        [HarmonyPatch(typeof(CrewAI), "RandomiseIdentifyTargetTimer")]
        private static class IdentifyPatch
        {
            private static void Postfix(CrewAI __instance)
            {
                var state = __instance.GetComponentInParent<AmpCrew>();
                if (state != null) ResetIdentify(__instance, state);
            }
        }

        [HarmonyPatch(typeof(CrewAI), "ScheduleCommsSweep")]
        private static class SweepPatch
        {
            private static void Postfix(CrewAI __instance)
            {
                var state = __instance.GetComponentInParent<AmpCrew>();
                if (state != null) Reflect.Set(__instance, "_sweepCommsCheckTimer", SweepSeconds[state.commander]);
            }
        }
    }
}
