using GHPC.Camera;
using GHPC.Weapons;
using Reticle;
using UnityEngine;
using GHPC.Weaponry;
using ModUtil;
using GHPC.Vehicle;
using GHPC.Equipment.Optics;
using System.Collections;
using System.Collections.Generic;

namespace M1A1Abrams
{
    /// <summary>
    /// Gunner's Auxiliary Sight reticles for the 120mm conversion: the vanilla 105mm
    /// reticle trees are cloned, re-pointed at each configured projectile, and handed to
    /// the sight's primary ReticleMesh as the round in the breech changes.
    /// </summary>
    public class GAS : Module
    {
        /// <summary>One built reticle tree, ready to be handed to a ReticleMesh.</summary>
        internal sealed class Choice
        {
            internal AmmoType ammo;
            internal ReticleSO tree;
            internal object cache; // boxed ReticleMesh.CachedReticle
            internal bool sabot;
        }

        /// <summary>The trees built for one set of configured rounds.</summary>
        internal sealed class ReticleSet
        {
            internal string key;
            internal readonly List<Choice> choices = new List<Choice>();
            internal Choice sabot_fallback;
            internal Choice heat_fallback;
        }

        private static readonly List<ReticleSet> sets = new List<ReticleSet>();

        public override void UnloadDynamicAssets()
        {
            IDictionary cache = CachedReticles() as IDictionary;

            foreach (ReticleSet set in sets)
            {
                foreach (Choice choice in set.choices)
                {
                    if (choice.tree == null) continue;
                    if (cache != null) cache.Remove(choice.tree.name);
                    ScriptableObject.DestroyImmediate(choice.tree);
                }
            }

            sets.Clear();
        }

        public static void Add(Transform gas, WeaponSystem[] exclusives)
        {
            if (gas == null) return;

            CameraSlot slot = gas.GetComponent<CameraSlot>();

            if (slot != null)
            {
                slot.ExclusiveWeapons = exclusives;
            }
        }

        /// <summary>
        /// Builds one reticle tree per distinct round among the four configured slots.
        ///
        /// The ladder is baked into the mesh: ReticleTree.VerticalBallistic turns each range
        /// mark into a superelevation angle through the tree's own projectile, so two rounds
        /// with different ballistics cannot share a tree.
        /// </summary>
        internal static ReticleSet Create(AmmoCodexScriptable[] rounds)
        {
            if (rounds == null) return null;

            List<AmmoCodexScriptable> unique = Unique(rounds);

            if (unique.Count == 0) return null;

            string key = Key(unique);

            foreach (ReticleSet cached in sets)
            {
                if (cached.key == key) return cached;
            }

            EnsureSourceReticles();

            object ap_source = Reflect.DictGet(CachedReticles(), "M1_105_GAS_APFSDS");
            object heat_source = Reflect.DictGet(CachedReticles(), "M1_105_GAS_HEAT");

            if (ap_source == null || heat_source == null)
            {
                MelonLoader.MelonLogger.Warning("[GAS] Vanilla M1IP reticles are not in ReticleMesh.cachedReticles; "
                    + "the auxiliary sight keeps its 105mm trees.");
                return null;
            }

            ReticleSet set = new ReticleSet { key = key };

            foreach (AmmoCodexScriptable round in unique)
            {
                bool sabot = round.AmmoType.Category == AmmoType.AmmoCategory.Penetrator;
                object source = sabot ? ap_source : heat_source;

                Choice choice = new Choice
                {
                    ammo = round.AmmoType,
                    sabot = sabot,
                    tree = ScriptableObject.Instantiate(Reflect.Get<ReticleSO>(source, "tree")),
                    cache = System.Runtime.CompilerServices.RuntimeHelpers.GetObjectValue(source)
                };

                // ReticleMesh keys its cache by tree name, so every tree needs its own.
                choice.tree.name = "120mm_gas_" + set.choices.Count + (sabot ? "_ap" : "_heat");
                Reflect.Set(choice.cache, "tree", choice.tree);
                Reflect.Set(choice.cache, "mesh", null);

                Configure(choice, round);
                set.choices.Add(choice);

                if (sabot && set.sabot_fallback == null) set.sabot_fallback = choice;
                if (!sabot && set.heat_fallback == null) set.heat_fallback = choice;
            }

            sets.Add(set);
            return set;
        }

        internal static void AttachAuxFix(Transform gas, WeaponSystem main_gun, ReticleSet set)
        {
            if (gas == null) return;

            // The prefab's second mesh holds the vanilla 105mm HEAT tree and nothing in the game
            // drives it. It is switched off even when the 120mm trees could not be built: left
            // alone it draws a second ladder over whichever round is loaded.
            Transform heat_child = gas.Find("Reticle Mesh HEAT");

            if (heat_child != null)
            {
                heat_child.gameObject.SetActive(false);
            }

            if (set == null) return;

            ReticleMesh canvas = ResolveCanvas(gas);

            if (canvas == null)
            {
                MelonLoader.MelonLogger.Warning("[GAS] Aux sight has no ReticleMesh; the 120mm reticles were not attached.");
                return;
            }

            AuxFix aux = gas.GetComponent<AuxFix>();

            if (aux == null)
            {
                aux = gas.gameObject.AddComponent<AuxFix>();
            }

            aux.Bind(canvas, main_gun, set);
        }

        private static ReticleMesh ResolveCanvas(Transform gas)
        {
            UsableOptic optic = gas.GetComponent<UsableOptic>();

            if (optic != null && optic.reticleMesh != null)
            {
                return optic.reticleMesh;
            }

            Transform child = gas.Find("Reticle Mesh");

            return child == null ? null : child.GetComponent<ReticleMesh>();
        }

        private static List<AmmoCodexScriptable> Unique(AmmoCodexScriptable[] rounds)
        {
            List<AmmoCodexScriptable> unique = new List<AmmoCodexScriptable>();

            foreach (AmmoCodexScriptable round in rounds)
            {
                if (round == null || round.AmmoType == null) continue;

                // A guided round has no ballistic ladder to draw: LAHAT is steered onto the
                // laser spot, so range marks on the auxiliary sight mean nothing for it.
                if (round.AmmoType.Guidance != AmmoType.GuidanceType.Unguided) continue;

                bool seen = false;

                foreach (AmmoCodexScriptable other in unique)
                {
                    if (ReferenceEquals(other.AmmoType, round.AmmoType))
                    {
                        seen = true;
                        break;
                    }
                }

                if (!seen) unique.Add(round);
            }

            return unique;
        }

        private static string Key(List<AmmoCodexScriptable> unique)
        {
            string key = "";

            foreach (AmmoCodexScriptable round in unique)
            {
                key += round.AmmoType.Name + "|";
            }

            return key;
        }

        private static void Configure(Choice choice, AmmoCodexScriptable round)
        {
            ReticleTree.FocalPlane plane = choice.tree.planes[0] as ReticleTree.FocalPlane;

            if (plane == null || plane.elements == null || plane.elements.Count == 0) return;

            ReticleTree.Angular boresight = plane.elements[0] as ReticleTree.Angular;

            if (boresight == null || boresight.elements == null) return;

            if (boresight.elements.Count > 4)
            {
                ReticleTree.VerticalBallistic ladder = boresight.elements[4] as ReticleTree.VerticalBallistic;

                if (ladder != null)
                {
                    ladder.projectile = round;
                    ladder.UpdateBC();
                }
                else
                {
                    MelonLoader.MelonLogger.Warning("[GAS] Reticle tree has no ballistic ladder at element 4; "
                        + round.AmmoType.Name + " will keep the donor ladder.");
                }
            }
            else
            {
                MelonLoader.MelonLogger.Warning("[GAS] Reticle tree has only " + boresight.elements.Count
                    + " boresight elements; " + round.AmmoType.Name + " will keep the donor ladder.");
            }

            ReticleTree.Text label = boresight.elements[0] as ReticleTree.Text;

            if (label != null)
            {
                label.text = round.AmmoType.Name + "\nMETERS";
            }
        }

        private static void ApplyReticle(Transform gas, string child_name, Choice choice)
        {
            Transform child = gas.Find(child_name);

            if (child == null) return;

            ReticleMesh mesh = child.GetComponent<ReticleMesh>();

            if (mesh == null) return;

            SkinnedMeshRenderer smr = child.GetComponent<SkinnedMeshRenderer>();

            // ReticleMesh.Load() dereferences its renderer before it enters its own try/catch.
            if (smr == null) return;

            mesh.reticleSO = choice.tree;
            Reflect.Set(mesh, "reticle", choice.cache);
            smr.sharedMesh = null;
            mesh.Load();
        }

        /// <summary>
        /// Rebuilds the sight's primary mesh from the chosen tree.
        ///
        /// Mirrors FixedReticleSize: Load() recreates the light state as well as the mesh,
        /// normalizes a non-zero localPosition to the origin, and leaves the previous mesh
        /// allocated, so all three are handled around the call.
        /// </summary>
        private static void Apply(ReticleMesh canvas, SkinnedMeshRenderer renderer, Choice choice,
            WeaponSystem gun, ref Mesh generated)
        {
            if (canvas == null || choice == null || choice.tree == null) return;

            var lightTypes = (ReticleTree.Light.Type[])System.Enum.GetValues(typeof(ReticleTree.Light.Type));
            float[] lightValues = new float[lightTypes.Length];

            for (int i = 0; i < lightTypes.Length; i++)
            {
                canvas.GetLight(lightTypes[i], out lightValues[i]);
            }

            float brightness = Reflect.Get<float>(canvas, "brightness");
            Vector3 position = canvas.transform.localPosition;

            canvas.reticleSO = choice.tree;
            Reflect.Set(canvas, "reticle", choice.cache);

            // ReticleMesh.Load() returns early while the renderer still holds a mesh.
            if (renderer != null)
            {
                renderer.sharedMesh = null;
            }

            canvas.transform.localPosition = Vector3.zero;

            try
            {
                canvas.Load();
            }
            finally
            {
                canvas.transform.localPosition = position;
            }

            if (renderer == null || renderer.sharedMesh == null) return;

            if (generated != null && !ReferenceEquals(generated, renderer.sharedMesh))
            {
                Object.Destroy(generated);
            }

            generated = renderer.sharedMesh;

            for (int i = 0; i < lightTypes.Length; i++)
            {
                if (float.IsNaN(lightValues[i])) continue;

                // GetLight already includes night brightness; SetLight applies it again.
                canvas.SetLight(lightTypes[i], brightness > 0f ? lightValues[i] / brightness : lightValues[i]);
            }

            FireControlSystem fcs = gun == null ? null : gun.FCS;

            if (fcs != null)
            {
                canvas.SetRange(choice.ammo, fcs.ForceReticleToZeroRange ? 0f : fcs.ReportedRange, true);
            }
            else
            {
                Reflect.Set(canvas, "CurrentAmmo", choice.ammo);
            }
        }

        /// <summary>
        /// ReticleMesh.cachedReticles is private and typed with a private nested struct, so
        /// it is read as object through the non-generic IDictionary view.
        /// </summary>
        private static object CachedReticles()
        {
            return Reflect.GetStatic<object>(typeof(ReticleMesh), "cachedReticles");
        }

        /// <summary>
        /// The vanilla reticles only enter ReticleMesh's cache when their optic is loaded.
        /// </summary>
        private static void EnsureSourceReticles()
        {
            if (!Reflect.DictHas(CachedReticles(), "M1_105_GAS_APFSDS")
                || !Reflect.DictHas(CachedReticles(), "M1_105_GAS_HEAT"))
            {
                Vehicle m1ip = AssetUtil.LoadVanillaVehicle("M1IP");

                if (m1ip == null)
                {
                    return;
                }

                Transform gas = m1ip.transform.Find("Gun Scripts/Aux sight (GAS)");

                if (gas == null)
                {
                    return;
                }

                UsableOptic optic = gas.GetComponent<UsableOptic>();

                if (optic != null && optic.reticleMesh != null)
                {
                    optic.reticleMesh.Load();
                }
            }
        }

        /// <summary>
        /// Hands the sight the reticle tree that matches the round in the breech.
        ///
        /// The vanilla M1IP GAS ships two reticle meshes - "Reticle Mesh" (APFSDS) and
        /// "Reticle Mesh HEAT" - and only the APFSDS one is active in the prefab. Nothing in
        /// the game switches between them: UsableOptic drives only its primary
        /// `reticleMesh`, and FCS_AmmoTypeChanged just re-ranges that one. A non-dart round
        /// therefore left the auxiliary sight blank, and a dart drew the 105mm ladder.
        /// </summary>
        public class AuxFix : MonoBehaviour
        {
            private ReticleMesh primary;
            private WeaponSystem main_gun;
            private ReticleSet set;

            private readonly List<Choice> built = new List<Choice>();
            private readonly List<GameObject> hosts = new List<GameObject>();
            private readonly List<ReticleMesh> canvases = new List<ReticleMesh>();
            private readonly List<Transform> strays = new List<Transform>();

            private int active = -1;
            private float last_brightness = float.NaN;

            internal void Bind(ReticleMesh mesh, WeaponSystem gun, ReticleSet reticles)
            {
                primary = mesh;
                main_gun = gun;
                set = reticles;

                if (hosts.Count == 0)
                {
                    Build();
                }

                Refresh();
            }

            private void OnEnable()
            {
                Refresh();
            }

            private void Update()
            {
                Refresh();
            }

            /// <summary>
            /// Builds one reticle mesh per round, once.
            ///
            /// Switching rounds is then a SetActive rather than a mesh rebuild. ReticleMesh.Load()
            /// bails out while the renderer still holds a mesh and Clear(true) destroys the
            /// transform's children, so rebuilding the same mesh on every ammo change left the
            /// sight blank; a mesh that is built once and only toggled cannot get into that state.
            /// </summary>
            private void Build()
            {
                if (primary == null || set == null) return;

                Transform parent = primary.transform.parent;

                for (int i = 0; i < set.choices.Count; i++)
                {
                    Choice choice = set.choices[i];
                    GameObject host;
                    ReticleMesh canvas;

                    if (i == 0)
                    {
                        host = primary.gameObject;
                        canvas = primary;
                    }
                    else
                    {
                        host = Object.Instantiate(primary.gameObject, parent);
                        host.name = primary.gameObject.name + " " + choice.ammo.Name;
                        canvas = host.GetComponent<ReticleMesh>();

                        if (canvas == null)
                        {
                            MelonLoader.MelonLogger.Warning("[GAS] Cloned sight lost its ReticleMesh for " + choice.ammo.Name + ".");
                            Object.Destroy(host);
                            continue;
                        }
                    }

                    SkinnedMeshRenderer renderer = host.GetComponent<SkinnedMeshRenderer>();

                    if (renderer != null)
                    {
                        renderer.sharedMesh = null;
                    }

                    canvas.reticleSO = choice.tree;
                    Reflect.Set(canvas, "reticle", choice.cache);
                    canvas.transform.localPosition = Vector3.zero;

                    try
                    {
                        canvas.Load();
                    }
                    catch (System.Exception error)
                    {
                        MelonLoader.MelonLogger.Error("[GAS] " + choice.ammo.Name + " reticle threw: " + error);
                    }

                    if (renderer != null && renderer.sharedMesh == null)
                    {
                        MelonLoader.MelonLogger.Warning("[GAS] " + choice.ammo.Name + " reticle did not build.");
                    }

                    host.SetActive(i == 0);
                    built.Add(choice);
                    hosts.Add(host);
                    canvases.Add(canvas);
                }

                // Whatever else hangs under the sight is a mesh the game shipped and no round of
                // ours drives. Suppress keeps those off; the prefab's "Reticle Mesh HEAT" in
                // particular is active by default and nothing in the game clears it.
                foreach (ReticleMesh mesh in parent.GetComponentsInChildren<ReticleMesh>(true))
                {
                    if (mesh == null || hosts.Contains(mesh.gameObject)) continue;

                    strays.Add(mesh.transform);
                }

                active = hosts.Count > 0 ? 0 : -1;
            }

            internal void Refresh()
            {
                if (set == null || built.Count == 0) return;

                Suppress();

                AmmoType ammo = main_gun == null ? null : main_gun.CurrentAmmoType;

                if (ammo == null) return;

                Show(IndexOf(ammo));
                Sync();
            }

            /// <summary>
            /// Switches off every reticle mesh under the sight that this component does not own.
            /// Re-asserted every frame because a second ladder drawn over the chosen one is
            /// exactly what a mesh reactivated by anything else looks like.
            /// </summary>
            private void Suppress()
            {
                for (int i = 0; i < strays.Count; i++)
                {
                    if (strays[i] != null && strays[i].gameObject.activeSelf)
                    {
                        strays[i].gameObject.SetActive(false);
                    }
                }
            }

            /// <summary>
            /// Exact round first; otherwise the nearest tree of the same family, so an unlisted
            /// round can never be drawn with the other family's ladder. Returns -1 for a guided
            /// round, which has no ladder at all and therefore shows none.
            /// </summary>
            private int IndexOf(AmmoType ammo)
            {
                for (int i = 0; i < built.Count; i++)
                {
                    if (ReferenceEquals(built[i].ammo, ammo)) return i;
                }

                if (ammo.Guidance != AmmoType.GuidanceType.Unguided) return -1;

                bool sabot = ammo.Category == AmmoType.AmmoCategory.Penetrator;

                for (int i = 0; i < built.Count; i++)
                {
                    if (built[i].sabot == sabot) return i;
                }

                return 0;
            }

            /// <summary>
            /// Shows one mesh and hides the rest; -1 hides them all, which is what a guided round
            /// gets because its flight path has no ballistic arc to measure against.
            ///
            /// The SetActive calls run every frame rather than only on a change, so nothing that
            /// re-enables a mesh behind our back can leave two ladders on the sight.
            /// </summary>
            private void Show(int index)
            {
                bool changed = index != active;

                for (int i = 0; i < hosts.Count; i++)
                {
                    if (hosts[i] == null) continue;

                    bool want = i == index;

                    if (hosts[i].activeSelf != want)
                    {
                        hosts[i].SetActive(want);
                    }
                }

                active = index;

                if (!changed || index < 0 || index >= canvases.Count) return;

                last_brightness = float.NaN;

                FireControlSystem fcs = main_gun == null ? null : main_gun.FCS;

                if (fcs != null && canvases[index] != null)
                {
                    canvases[index].SetRange(built[index].ammo,
                        fcs.ForceReticleToZeroRange ? 0f : fcs.ReportedRange, true);
                }
            }

            /// <summary>
            /// UsableOptic drives brightness and range on its own reticleMesh only, so the clone
            /// that is currently shown has to be kept in step with it.
            /// </summary>
            private void Sync()
            {
                if (active < 0 || active >= canvases.Count) return;

                ReticleMesh canvas = canvases[active];

                if (canvas == null || primary == null) return;

                float brightness = Reflect.Get<float>(primary, "brightness");

                if (brightness != last_brightness)
                {
                    last_brightness = brightness;
                    Reflect.Set(canvas, "brightness", brightness);

                    var lightTypes = (ReticleTree.Light.Type[])System.Enum.GetValues(typeof(ReticleTree.Light.Type));

                    for (int i = 0; i < lightTypes.Length; i++)
                    {
                        float value;
                        primary.GetLight(lightTypes[i], out value);

                        if (float.IsNaN(value)) continue;

                        // GetLight already includes the primary's brightness; SetLight applies it again.
                        canvas.SetLight(lightTypes[i], brightness > 0f ? value / brightness : value);
                    }
                }

                FireControlSystem fcs = main_gun == null ? null : main_gun.FCS;

                if (fcs != null)
                {
                    canvas.SetRange(built[active].ammo, fcs.ForceReticleToZeroRange ? 0f : fcs.ReportedRange, false);
                }
            }

            private void OnDestroy()
            {
                for (int i = 1; i < hosts.Count; i++)
                {
                    if (hosts[i] != null)
                    {
                        Object.Destroy(hosts[i]);
                    }
                }

                built.Clear();
                hosts.Clear();
                canvases.Clear();
                strays.Clear();
            }
        }
    }
}
