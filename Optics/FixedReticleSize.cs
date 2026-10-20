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
    // used, including its auxiliary reticle meshes, rather than dropping an early
    // conversion request when PairedOptic is still null.
    internal sealed class FixedReticleBinding : MonoBehaviour
    {
        internal CameraSlot Slot;
        internal float ReferenceFov;
        private readonly List<FixedReticleSize> meshes = new List<FixedReticleSize>();
        private UsableOptic bound_optic;
        private bool bound;

        private void OnEnable() { UnityEngine.Camera.onPreCull += ApplyBeforeRender; }
        private void OnDisable() { UnityEngine.Camera.onPreCull -= ApplyBeforeRender; }

        private void LateUpdate()
        {
            if (Slot == null || !Slot.IsActive) return;
            if (!bound)
            {
                var optic = Slot.PairedOptic;
                if (optic == null || optic.reticleMesh == null) return;
                // A camera slot keeps its identity while the optic behind it changes: the
                // commander's head slots are shared by the night sight, the CITV and the
                // binocular rig, whose B15/M19 reticles hang off the player camera instead of
                // the vehicle. Binding there scales the binocular graduations by the ratio
                // between the sight's narrowest zoom and the binocular field of view - several
                // times too large - so only vehicle sights may be bound, and only once.
                if (optic.GetComponentInParent<Vehicle>() == null)
                {
                    MelonLoader.MelonLogger.Warning("[M1A1] Fixed reticle size ignored a sight outside the vehicle: " + optic.name);
                    return;
                }
                if (bound_optic == null) bound_optic = optic;
                if (optic != bound_optic) return;
                // This is the same scope used by UsableOptic.AdditionalReticleMeshes.
                Transform root = optic.reticleMesh.transform.parent ?? optic.reticleMesh.transform;
                foreach (ReticleMesh mesh in root.GetComponentsInChildren<ReticleMesh>(true))
                {
                    // The native WFOV corner box marks the narrow field of view.
                    // Keep its original angular size instead of enlarging it with the aiming reticle.
                    if (mesh.reticleSO != null && mesh.reticleSO.name == "WFOV") continue;
                    FixedReticleSize scaler = mesh.GetComponent<FixedReticleSize>();
                    if (scaler == null) scaler = mesh.gameObject.AddComponent<FixedReticleSize>();
                    scaler.Bind(mesh);
                    meshes.Add(scaler);
                }
                bound = meshes.Count > 0;
            }
        }

        // The game rewrites reticle state from Update and LateUpdate, so a size applied there
        // can be replaced before the frame is drawn - the same one-frame flash the CROWS sight
        // had. Applying from onPreCull runs after every writer and before the draw, and Apply
        // returns immediately unless the zoom changed, so this is not per-frame work.
        private void ApplyBeforeRender(UnityEngine.Camera camera)
        {
            if (Slot == null || !Slot.IsActive) return;
            foreach (FixedReticleSize mesh in meshes)
                if (mesh != null) mesh.Apply(Slot, ReferenceFov);
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
        private float appliedScale = float.NaN;
        private readonly List<ReticleElementSize> sizes = new List<ReticleElementSize>();

        internal static void Attach(CameraSlot slot)
        {
            if (slot == null || slot.GetComponent<FixedReticleBinding>() != null) return;
            FixedReticleBinding component = slot.gameObject.AddComponent<FixedReticleBinding>();
            component.Slot = slot;
            // Use the original narrow sight as the familiar reference size, before AMP adds zooms.
            component.ReferenceFov = slot.DefaultFov;
            if (slot.OtherFovs != null)
                foreach (float fov in slot.OtherFovs)
                    if (fov > 0) component.ReferenceFov = Mathf.Min(component.ReferenceFov, fov);
        }

        internal void Bind(ReticleMesh mesh) { reticle = mesh; }

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
            if (slot == null || reticle == null || !slot.IsActive) return;
            bool fixedSize = M1A1.de_fixed_reticle_size != null && M1A1.de_fixed_reticle_size.Value;
            if (!fixedSize && owned == null) return;
            if (owned == null)
            {
                original = reticle.reticleSO;
                if (original == null) return;
                // Unity clones the serialized tree too: other vehicles and sights keep their own sizes.
                owned = Object.Instantiate(original);
                owned.name = original.name + " fixed-size " + GetInstanceID();
                foreach (ReticleTree.FocalPlane plane in owned.planes)
                    if (plane != null && plane.type == ReticleTree.FocalPlane.Type.First && plane.elements != null) Capture(plane.elements);
                renderer = reticle.GetComponent<SkinnedMeshRenderer>();
            }
            if (renderer == null || sizes.Count == 0) return;
            float rawFov = slot.FovIndex == 0 ? slot.DefaultFov : slot.OtherFovs[slot.FovIndex - 1];
            float reference = referenceFov * slot.CurrentFov / Mathf.Max(rawFov, 0.01f);
            float scale = fixedSize ? ReticleSizeRules.Scale(slot.CurrentFov, reference) : 1f;
            if (renderer.sharedMesh != null && Mathf.Abs(scale - appliedScale) < 0.00001f) return;
            foreach (ReticleElementSize size in sizes) size.Apply(scale);
            // Load recreates the light state as well as the mesh. Preserve the
            // optic's ready/laser/illumination indications across a zoom change.
            var lightTypes = (ReticleTree.Light.Type[])System.Enum.GetValues(typeof(ReticleTree.Light.Type));
            float[] lightValues = new float[lightTypes.Length];
            for (int i = 0; i < lightTypes.Length; i++) reticle.GetLight(lightTypes[i], out lightValues[i]);
            float brightness = Reflect.Get<float>(reticle, "brightness");
            Vector3 position = reticle.transform.localPosition;
            reticle.reticleSO = owned;
            renderer.sharedMesh = null;
            // Native Load normalizes an offset reticle to zero. Some GPS reticles
            // already sit at their projection distance (B15/M19); keep that placement.
            reticle.transform.localPosition = Vector3.zero;
            try { reticle.Load(); }
            finally { reticle.transform.localPosition = position; }
            if (renderer.sharedMesh == null) return;
            if (generated != null) Object.Destroy(generated);
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

        private void OnDestroy()
        {
            if (owned == null) return;
            IDictionary cache = Reflect.GetStatic<object>(typeof(ReticleMesh), "cachedReticles") as IDictionary;
            if (cache != null) cache.Remove(owned.name);
            if (reticle != null) reticle.reticleSO = original;
            if (generated != null) Object.Destroy(generated);
            Object.Destroy(owned);
        }
    }
}
