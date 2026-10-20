using System.Collections;
using System.Reflection;
using GHPC;
using GHPC.Vehicle;
using HarmonyLib;
using ModUtil;
using UnityEngine;

namespace M1A1Abrams
{
    internal sealed class AmpDesignation : MonoBehaviour
    {
        private Vehicle vehicle;
        private string designation;

        internal static string Name(bool m1a1, bool citv, string armor)
        {
            string first = m1a1 ? "M1A1" : "M1E1";
            if (!citv) return first + armor;
            string second = m1a1 ? "M1A2" : "M1E2";
            // Match AMP source, including its separate experimental M1E1 family.
            switch (armor)
            {
                case "HA": return first + " AIM";
                case "HC": return second;
                case "SA": return second + " SEP";
                case "HU": return second + "U";
                case "L": return second + "L";
                default: return first;
            }
        }

        internal static void Bind(Vehicle vehicle, string designation)
        {
            var state = vehicle.GetComponent<AmpDesignation>();
            if (state == null) state = vehicle.gameObject.AddComponent<AmpDesignation>();
            state.StopAllCoroutines();
            state.vehicle = vehicle;
            state.designation = designation;
            Reflect.Set(vehicle, "_friendlyName", designation);
            state.StartCoroutine(state.DetectEra());
        }

        private IEnumerator DetectEra()
        {
            // Other mods may attach ERA after our conversion. Check at low frequency,
            // then stop permanently once fitted; spent ERA does not remove the designation.
            var wait = new WaitForSeconds(2f);
            while (vehicle != null)
            {
                if (HasEra())
                {
                    Reflect.Set(vehicle, "_friendlyName", designation + " TUSK");
                    yield break;
                }
                yield return wait;
            }
        }

        private bool HasEra()
        {
            // The hull/turret armor can live outside the vehicle transform hierarchy.
            // Follow only this vehicle's own late-follow graph, never scan all vehicles.
            foreach (Transform root in AmpArmor.Roots(vehicle))
            {
                foreach (IArmor armor in root.GetComponentsInChildren<IArmor>(true))
                    if (armor.IsEra && armor.Unit != null && armor.Unit.gameObject == vehicle.gameObject) return true;

                // Original NATO ERA compatibility: AMP detects this installed hull array.
                // It may exist before the armor components have received their Unit owner.
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                {
                    if (item.name != "Hull ERA Array(Clone)" && item.name != "Hull ERA Array") continue;
                    var owner = item.GetComponentInParent<Vehicle>();
                    if (owner == null || owner == vehicle) return true;
                }
            }
            return false;
        }
    }

    // The game's first-person binoculars pick their faction model by matching Unit.FriendlyName
    // against UnitSpawner's prefab metadata. Our designations rewrite FriendlyName, so after
    // conversion that lookup finds nothing, the animator keeps a null army, and every faction
    // model and reticle is left deactivated (the reticle shown then depends on whatever the scene
    // left active, and the two binocular reticles sit at different depths, so the sight looks
    // wrong). Resolve the army from data the rename does not touch instead: UniqueName first,
    // allegiance as a fallback.
    //
    // Patched on ShowModel rather than on CameraSlotChanged: CameraSlotChanged resolves the army
    // and then calls ShowModel itself, so a postfix there would be too late for that first call.
    [HarmonyPatch(typeof(GHPC.Camera.FirstPersonBinocularsAnimator), "ShowModel")]
    internal static class BinocularArmyFix
    {
        private static readonly FieldInfo ArmyField = typeof(GHPC.Camera.FirstPersonBinocularsAnimator)
            .GetField("_army", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo UnitField = typeof(GHPC.Camera.FirstPersonBinocularsAnimator)
            .GetField("_unit", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void Prefix(GHPC.Camera.FirstPersonBinocularsAnimator __instance)
        {
            try
            {
                if (ArmyField == null || UnitField == null) return;
                if (ArmyField.GetValue(__instance) != null) return;

                var unit = UnitField.GetValue(__instance) as Unit;
                if (unit == null) return;

                var army = FindArmy(unit);
                if (army != null) ArmyField.SetValue(__instance, army);
            }
            catch (System.Exception)
            {
                // A patch that throws is worse than the defect it works around.
            }
        }

        private static GHPC.Mission.ArmyBasicInfoScriptable FindArmy(Unit unit)
        {
            var spawner = GHPC.Mission.UnitSpawner.Instance;
            if (spawner == null || spawner.PrefabLookup == null) return null;

            var units = spawner.PrefabLookup.AllUnits;
            if (units == null) return null;

            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] == null || units[i].Name != unit.UniqueName) continue;
                if (units[i].Army != null) return units[i].Army;
            }

            // A converted spawn can carry a unique name the table does not list; the unit's own
            // allegiance still identifies which binocular model belongs on screen.
            for (int i = 0; i < units.Length; i++)
            {
                if (units[i] == null || units[i].Army == null) continue;

                var info = units[i].Army.Info;
                if (info == null || info.Faction == null || info.Faction.Info == null) continue;
                if (info.Faction.Info.Allegiance == unit.Allegiance) return units[i].Army;
            }

            return null;
        }
    }
}
