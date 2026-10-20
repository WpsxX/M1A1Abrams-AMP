using System.Collections.Generic;
using GHPC;
using GHPC.Equipment;
using GHPC.Utility;
using GHPC.Vehicle;
using ModUtil;
using UnityEngine;

namespace M1A1Abrams
{
    // Lifetime belongs to one converted vehicle; codexes never mutate shared donor assets.
    internal sealed class AmpArmor : MonoBehaviour
    {
        private readonly List<ArmorCodexScriptable> owned = new List<ArmorCodexScriptable>();

        internal static void Apply(Vehicle vehicle, string choice)
        {
            if (AmpUpgrades.Uap.Value && choice != "HU")
                MelonLoader.MelonLogger.Warning("[AMP] " + vehicle.FriendlyName + " UAP requires HU armor; current armor is " + choice + ".");
            if (choice == "LEGACY" || choice == "") return;
            var state = vehicle.gameObject.AddComponent<AmpArmor>();
            bool demigod = choice == "HU" && AmpUpgrades.Demigod.Value;
            int matched = 0;
            foreach (Transform root in Roots(vehicle))
            {
                foreach (VariableArmor armor in root.GetComponentsInChildren<VariableArmor>(true))
                {
                    if (armor.Unit == null || armor.Unit.gameObject != vehicle.gameObject
                        || !state.seen.Add(armor.GetInstanceID())) continue;
                    if (choice == "L" && AmpUpgradeRules.RemoveForLightArmor(armor.Name))
                    {
                        armor.gameObject.SetActive(false);
                        matched++;
                        continue;
                    }
                    AmpUpgradeRules.Resistance value;
                    if (!AmpUpgradeRules.Variable(choice, armor.Name, demigod, out value)) continue;
                    var codex = ScriptableObject.CreateInstance<ArmorCodexScriptable>();
                    var type = new ArmorType();
                    if (armor.ArmorType != null && armor.ArmorType.ArmorType != null)
                        Util.ShallowCopy(type, armor.ArmorType.ArmorType);
                    type.Name = "AMP " + choice + " " + armor.Name;
                    type.ThicknessSource = ArmorType.RhaSource.Multipliers;
                    type.RhaeMultiplierCe = value.CE; type.RhaeMultiplierKe = value.KE;
                    if (choice == "HU" && armor.Name == "composite side skirt")
                    {
                        type.SpallAngleMultiplier = 0f; type.SpallPowerMultiplier = 0f;
                    }
                    codex.name = type.Name; codex.ArmorType = type;
                    state.owned.Add(codex);
                    Reflect.Set(armor, "_armorType", codex);
                    matched++;
                }
                foreach (UniformArmor armor in root.GetComponentsInChildren<UniformArmor>(true))
                {
                    if (armor.Unit == null || armor.Unit.gameObject != vehicle.gameObject
                        || !state.seen.Add(armor.GetInstanceID())) continue;
                    AmpUpgradeRules.Resistance value;
                    if (!AmpUpgradeRules.Uniform(choice, armor.Name, demigod, out value)) continue;
                    // AMP values are RHA equivalents, not physical thickness to multiply again.
                    armor.ThicknessListed = UniformArmor.ThicknessMode.RHAE;
                    armor.PrimaryHeatRha = value.CE; armor.PrimarySabotRha = value.KE;
                    matched++;
                }
            }
            Rigidbody body = vehicle.GetComponent<Rigidbody>();
            if (body != null)
            {
                float addedMass = AmpUpgradeRules.AddedMass(choice, AmpUpgrades.Uap.Value);
                float baseMass = body.mass;
                body.mass = baseMass + addedMass;
                MelonLoader.MelonLogger.Msg("[AMP] " + vehicle.FriendlyName + " mass: " + baseMass
                    + " + " + addedMass + " = " + body.mass + "kg; UAP=" + (choice == "HU" && AmpUpgrades.Uap.Value) + ".");
            }
            MelonLoader.MelonLogger.Msg("[AMP] " + vehicle.FriendlyName + " armor " + choice + ": " + matched + " pieces configured.");
            if (matched == 0)
                MelonLoader.MelonLogger.Warning("[AMP] No matching armor pieces; check the current vehicle hierarchy.");
        }

        private readonly HashSet<int> seen = new HashSet<int>();

        internal static IEnumerable<Transform> Roots(Vehicle vehicle)
        {
            // Armor followers may be outside the vehicle hierarchy in the current game.
            var pending = new Queue<Transform>();
            var visited = new HashSet<int>();
            pending.Enqueue(vehicle.transform);
            while (pending.Count > 0)
            {
                Transform root = pending.Dequeue();
                if (root == null || !visited.Add(root.GetInstanceID())) continue;
                yield return root;
                foreach (LateFollowTarget target in root.GetComponentsInChildren<LateFollowTarget>(true))
                {
                    if (target.LateFollowers == null) continue;
                    foreach (var follower in target.LateFollowers)
                        if (follower != null) pending.Enqueue(follower.transform);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var codex in owned) if (codex != null) Object.Destroy(codex);
            owned.Clear();
        }
    }
}
