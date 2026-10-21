using System;
using System.Collections;
using System.Collections.Generic;
using GHPC.Camera;
using GHPC.Equipment.Optics;
using GHPC.Vehicle;
using ModUtil;
using Reticle;
using UnityEngine;

namespace M1A1Abrams
{
    // Optics register their paired slot in Start. Bind when the sight is actually
    // used, rather than dropping an early conversion request while PairedOptic is
    // still null, and rebind when the slot is handed to a different optic.
    internal sealed class FixedReticleBinding : MonoBehaviour
    {
        internal CameraSlot Slot;
        internal float ReferenceFov;

        private readonly List<FixedReticleSize> meshes = new List<FixedReticleSize>();
        private UsableOptic bound_optic;
        private int bound_additional = -1;
        private int applied_frame = -1;

        private void OnEnable() { UnityEngine.Camera.onPreCull += ApplyBeforeRender; }
        private void OnDisable() { UnityEngine.Camera.onPreCull -= ApplyBeforeRender; }

        private void LateUpdate()
        {
            if (Slot == null || !Slot.IsActive) return;
            UsableOptic optic = Slot.PairedOptic;
            if (optic == null || optic.reticleMesh == null) return;
            // A camera slot keeps its identity while the optic behind it changes: the
            // commander's head slots are shared by the night sight, the CITV and the
            // binocular rig. Only sights that live on a vehicle may be bound.
            if (optic.GetComponentInParent<Vehicle>() == null) return;
            // UsableOptic fills AdditionalReticleMeshes in its own Start, which can run
            // after the first conversion request: rebind once that list grows.
            if (bound_optic == optic && AdditionalCount(optic) == bound_additional) return;
            BindOptic(optic);
        }

        private static int AdditionalCount(UsableOptic optic)
        {
            ReticleMesh[] additional = Reflect.Get<ReticleMesh[]>(optic, "AdditionalReticleMeshes");
            return additional == null ? 0 : additional.Length;
        }

        private void BindOptic(UsableOptic optic)
        {
            Release();
            Vehicle vehicle = optic.GetComponentInParent<Vehicle>();
            bound_optic = optic;
            // The sight's own reticle always belongs to this optic, even when the game
            // reparents it under the player camera for post processing.
            TryBind(optic.reticleMesh, optic, vehicle, true);
            ReticleMesh[] additional = Reflect.Get<ReticleMesh[]>(optic, "AdditionalReticleMeshes");
            bound_additional = additional == null ? 0 : additional.Length;
            if (additional != null)
                foreach (ReticleMesh mesh in additional)
                    if (mesh != null && mesh != optic.reticleMesh) TryBind(mesh, optic, vehicle, false);
        }

        private void TryBind(ReticleMesh mesh, UsableOptic optic, Vehicle vehicle, bool own)
        {
            if (mesh == null) return;
            // The native WFOV corner box marks the narrow field of view. Keep its
            // original angular size instead of enlarging it with the aiming reticle.
            if (mesh.reticleSO != null && mesh.reticleSO.name == "WFOV") return;
            if (!own)
            {
                // The reticle parent can also hold the reticles of a neighbouring optic
                // and the binocular rig that hangs off the player camera. Scaling those
                // by this sight's zoom is what made the binocular graduations huge.
                UsableOptic owner = mesh.GetComponentInParent<UsableOptic>();
                if (owner != null && owner != optic) return;
                // Follow-object roots (M1IP HULL FOLLOW ...) sit in the scene root, so the
                // vehicle test has to walk the real transform chain instead of the parent
                // components. Camera-side rigs fail it and are left alone.
                if (vehicle == null || !mesh.transform.IsChildOf(vehicle.transform)) return;
            }
            FixedReticleSize scaler = mesh.GetComponent<FixedReticleSize>();
            if (scaler == null) scaler = mesh.gameObject.AddComponent<FixedReticleSize>();
            if (!scaler.Bind(mesh, this)) return;
            meshes.Add(scaler);
        }

        private void Release()
        {
            for (int i = 0; i < meshes.Count; i++)
                if (meshes[i] != null) meshes[i].Release(this);
            meshes.Clear();
            bound_optic = null;
            bound_additional = -1;
        }

        private void OnDestroy() { Release(); }

        // The game rewrites reticle state from Update and LateUpdate, so a size applied there
        // can be replaced before the frame is drawn - the same one-frame flash the CROWS sight
        // had. Applying from onPreCull runs after every writer and before the draw, and Apply
        // returns immediately unless the zoom changed, so this is not per-frame work.
        private void ApplyBeforeRender(UnityEngine.Camera camera)
        {
            if (Slot == null || !Slot.IsActive || meshes.Count == 0) return;
            if (applied_frame == Time.frameCount) return;
            applied_frame = Time.frameCount;
            // CameraSlot.CurrentFov indexes OtherFovs unguarded, and AMP replaces the
            // array when it adds zooms: skip instead of throwing here.
            if (Slot.FovIndex < 0 || (Slot.FovIndex > 0 && (Slot.OtherFovs == null || Slot.FovIndex > Slot.OtherFovs.Length))) return;
            for (int i = 0; i < meshes.Count; i++)
                if (meshes[i] != null) meshes[i].Apply(Slot, ReferenceFov);
        }
    }

    // The current game draws baked reticle geometry with PostMeshComp. Scaling
    // the old FFP transform does not scale unskinned lines/text in that geometry.
    public sealed class FixedReticleSize : MonoBehaviour
    {
        private ReticleMesh reticle;
        private SkinnedMeshRenderer renderer;
        private ReticleSO original, owned;
        private Mesh generated;
        private FixedReticleBinding owner;
        private float appliedScale = float.NaN;
        private readonly List<ReticleElementSize> sizes = new List<ReticleElementSize>();

        internal static void Attach(CameraSlot slot)
        {
            if (slot == null || slot.GetComponent<FixedReticleBinding>() != null) return;
            FixedReticleBinding component = slot.gameObject.AddComponent<FixedReticleBinding>();
            component.Slot = slot;
            // Use the original narrow sight as the familiar reference size, before AMP adds zooms.
            // An unset field would drive the scale into the thousands, so keep it sane.
            float reference = Mathf.Max(slot.DefaultFov, 0.05f);
            if (slot.OtherFovs != null)
                foreach (float fov in slot.OtherFovs)
                    if (fov > 0.05f) reference = Mathf.Min(reference, fov);
            component.ReferenceFov = Mathf.Max(reference, 0.05f);
        }

        // Two optics can share a reticle parent; never let them drive the same mesh.
        internal bool Bind(ReticleMesh mesh, FixedReticleBinding binding)
        {
            if (mesh == null || binding == null) return false;
            if (owner != null && owner != binding) return false;
            owner = binding;
            reticle = mesh;
            return true;
        }

        internal void Release(FixedReticleBinding binding)
        {
            if (owner != binding) return;
            Restore(true);
            owner = null;
            reticle = null;
        }

        private void Capture(IEnumerable<ReticleTree.TransformElement> elements)
        {
            foreach (ReticleTree.TransformElement element in elements)
            {
                if (element == null) continue;
                sizes.Add(new ReticleElementSize(element));
                var group = element as ReticleTree.GroupBase;
                if (group != null && group.elements != null) Capture(group.elements);
            }
        }

        internal void Apply(CameraSlot slot, float referenceFov)
        {
            if (slot == null || reticle == null) return;
            if (slot.FovIndex < 0 || (slot.FovIndex > 0 && (slot.OtherFovs == null || slot.FovIndex > slot.OtherFovs.Length))) return;
            bool fixedSize = M1A1.de_fixed_reticle_size != null && M1A1.de_fixed_reticle_size.Value;
            if (!fixedSize && owned == null) return;
            if (owned == null)
            {
                original = reticle.reticleSO;
                if (original == null) return;
                // Unity clones the serialized tree too: other vehicles and sights keep their own sizes.
                owned = UnityEngine.Object.Instantiate(original);
                owned.name = original.name + " fixed-size " + GetInstanceID();
                if (owned.planes != null)
                    foreach (ReticleTree.FocalPlane plane in owned.planes)
                        if (plane != null && plane.elements != null) Capture(plane.elements);
                renderer = reticle.GetComponent<SkinnedMeshRenderer>();
            }
            if (renderer == null || sizes.Count == 0) return;
            float rawFov = slot.FovIndex == 0 ? slot.DefaultFov : slot.OtherFovs[slot.FovIndex - 1];
            float reference = Mathf.Clamp(referenceFov * slot.CurrentFov / Mathf.Max(rawFov, 0.01f), 0.05f, 170f);
            float scale = fixedSize ? ReticleSizeRules.Scale(slot.CurrentFov, reference) : 1f;
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f) scale = 1f;
            if (renderer.sharedMesh != null && Mathf.Abs(scale - appliedScale) < 0.00001f) return;
            foreach (ReticleElementSize size in sizes) size.Apply(scale);
            // Load recreates the light state as well as the mesh. Preserve the
            // optic's ready/laser/illumination indications across a zoom change.
            ReticleTree.Light.Type[] lightTypes = null;
            float[] lightValues = null;
            float brightness = 0f;
            Vector3 position = reticle.transform.localPosition;
            try
            {
                lightTypes = (ReticleTree.Light.Type[])Enum.GetValues(typeof(ReticleTree.Light.Type));
                lightValues = new float[lightTypes.Length];
                for (int i = 0; i < lightTypes.Length; i++) reticle.GetLight(lightTypes[i], out lightValues[i]);
                brightness = Reflect.Get<float>(reticle, "brightness");
                reticle.reticleSO = owned;
                renderer.sharedMesh = null;
                // Native Load normalizes an offset reticle to zero. Some GPS reticles
                // already sit at their projection distance (B15/M19); keep that placement.
                reticle.transform.localPosition = Vector3.zero;
                try { reticle.Load(); }
                finally { reticle.transform.localPosition = position; }
            }
            catch (Exception)
            {
                // Never leave the mesh pointing at a clone that may be destroyed later.
                reticle.reticleSO = original;
                return;
            }
            if (renderer.sharedMesh == null) return;
            if (generated != null) UnityEngine.Object.Destroy(generated);
            generated = renderer.sharedMesh;
            for (int i = 0; i < lightTypes.Length; i++)
                if (!float.IsNaN(lightValues[i]))
                    // GetLight already includes night brightness; SetLight applies it again.
                    reticle.SetLight(lightTypes[i], brightness > 0f ? lightValues[i] / brightness : lightValues[i]);
            // Reapply range after rebuilding the alignment bones.
            if (slot.PairedOptic != null && slot.PairedOptic.FCS != null)
            {
                var fcs = slot.PairedOptic.FCS;
                reticle.SetRange(fcs.CurrentAmmoType, fcs.ForceReticleToZeroRange ? 0 : fcs.ReportedRange, true);
            }
            appliedScale = scale;
        }

        // rebuild: when the mesh is handed to another optic the geometry has to go back to
        // its original size, otherwise it keeps the last zoomed shape. Teardown skips that.
        private void Restore(bool rebuild)
        {
            IDictionary cache = Reflect.GetStatic<object>(typeof(ReticleMesh), "cachedReticles") as IDictionary;
            if (owned != null && cache != null) cache.Remove(owned.name);
            ReticleMesh mesh = reticle;
            ReticleSO restore = original;
            if (mesh != null && restore != null) mesh.reticleSO = restore;
            if (generated != null) UnityEngine.Object.Destroy(generated);
            if (owned != null) UnityEngine.Object.Destroy(owned);
            generated = null;
            owned = null;
            original = null;
            renderer = null;
            appliedScale = float.NaN;
            sizes.Clear();
            if (!rebuild || mesh == null || restore == null) return;
            try
            {
                SkinnedMeshRenderer skin = mesh.GetComponent<SkinnedMeshRenderer>();
                if (skin == null) return;
                skin.sharedMesh = null;
                mesh.Load();
            }
            catch (Exception) { }
        }

        private void OnDestroy() { Restore(false); }
    }
}
