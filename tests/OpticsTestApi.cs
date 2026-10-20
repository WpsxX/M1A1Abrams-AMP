// Offline substitutes for native API contracts used by the real optics modules.
// ReticleMesh.Load's origin reset and SetLight's brightness multiplication are
// taken from the installed Assembly-CSharp. This does not simulate Unity rendering.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static T Instantiate<T>(T source) where T : Object => (T)((ICloneable)source).Clone();
        public static void Destroy(Object value) { }
        public int GetInstanceID() => GetHashCode();
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
    }
    public class MonoBehaviour : Component { }
    public class GameObject : Object
    {
        private readonly List<Component> components = new List<Component>();
        public readonly Transform transform;
        public GameObject() { transform = new Transform { gameObject = this }; }
        public T AddComponent<T>() where T : Component, new()
        {
            var value = new T { gameObject = this }; components.Add(value); return value;
        }
        public T GetComponent<T>() where T : Component => components.Find(c => c is T) as T;
        internal T[] Components<T>() where T : Component => components.FindAll(c => c is T).ConvertAll(c => (T)c).ToArray();
    }
    public class Transform : Component
    {
        public Transform parent;
        public Vector3 localPosition;
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component => gameObject.Components<T>();
        public Transform Find(string path) => null;
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3();
    }
    public class Mesh : Object { }
    public class Material : Object { }
    public class SkinnedMeshRenderer : Component { public Mesh sharedMesh; }
    public enum FilterMode { Trilinear }
    public static class Mathf
    {
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Abs(float value) => Math.Abs(value);
    }
}
namespace GHPC.Camera
{
    public class CameraSlot : UnityEngine.Component
    {
        public bool IsActive, OverrideFLIRResolution;
        public float DefaultFov, BaseBlur, VibrationBlurScale, VibrationShakeMultiplier;
        public float[] OtherFovs;
        public int FovIndex, FLIRWidth, FLIRHeight;
        public float CurrentFov => FovIndex == 0 ? DefaultFov : OtherFovs[FovIndex - 1];
        public UnityEngine.FilterMode FLIRFilterMode;
        public UnityEngine.Material FLIRBlitMaterialOverride;
        public GHPC.Equipment.Optics.UsableOptic PairedOptic;
        public CameraSlot LinkedNightSight;
    }
}
namespace GHPC.Equipment.Optics
{
    public class FireControlSystem
    {
        public object CurrentAmmoType;
        public bool ForceReticleToZeroRange;
        public float CurrentRange, ReportedRange;
    }
    public class UsableOptic : UnityEngine.Component
    {
        public Reticle.ReticleMesh reticleMesh;
        public GHPC.Camera.CameraSlot slot;
        public FireControlSystem FCS;
    }
}
namespace GHPC.Vehicle { public class Vehicle : UnityEngine.Component { } }
namespace ModUtil
{
    public static class Reflect
    {
        public static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(target);
        public static T GetStatic<T>(Type type, string name) => default;
    }
}
namespace Reticle
{
    public class ReticleTree
    {
        public class Position { public float x, y; }
        public class Dimension { public float MRADS; }
        public class TransformElement { public Position position = new Position(); }
        public class GroupBase : TransformElement { public List<TransformElement> elements; }
        public class Line : TransformElement { public Dimension length = new Dimension(), thickness = new Dimension(); }
        public class Circle : TransformElement { public Dimension radius = new Dimension(), thickness = new Dimension(); }
        public class Text : TransformElement { public float fontSize; }
        public class Light { public enum Type { Powered, NightIllumination } }
        public class FocalPlane
        {
            public enum Type { First, Second }
            public Type type;
            public List<TransformElement> elements = new List<TransformElement>();
        }
    }
    public class ReticleSO : UnityEngine.Object, ICloneable
    {
        public List<ReticleTree.FocalPlane> planes = new List<ReticleTree.FocalPlane>();
        public object Clone()
        {
            var clone = new ReticleSO { name = name };
            foreach (var plane in planes)
            {
                var copy = new ReticleTree.FocalPlane { type = plane.type };
                foreach (ReticleTree.Line line in plane.elements)
                {
                    var item = new ReticleTree.Line();
                    item.position.x = line.position.x; item.position.y = line.position.y;
                    item.length.MRADS = line.length.MRADS; item.thickness.MRADS = line.thickness.MRADS;
                    copy.elements.Add(item);
                }
                clone.planes.Add(copy);
            }
            return clone;
        }
    }
    public class ReticleMesh : UnityEngine.Component
    {
        public ReticleSO reticleSO;
        private float brightness = 0.5f;
        private readonly Dictionary<ReticleTree.Light.Type, float> lights = new Dictionary<ReticleTree.Light.Type, float>();
        public int Loads, OriginResets;
        public bool FailLoad;
        public float Range;
        public void Load()
        {
            Loads++;
            if (transform.localPosition.x != 0 || transform.localPosition.y != 0 || transform.localPosition.z != 0)
            { OriginResets++; transform.localPosition = UnityEngine.Vector3.zero; }
            if (!FailLoad) GetComponent<UnityEngine.SkinnedMeshRenderer>().sharedMesh = new UnityEngine.Mesh();
        }
        public void GetLight(ReticleTree.Light.Type type, out float value) => value = lights.TryGetValue(type, out var current) ? current : float.NaN;
        public void SetLight(ReticleTree.Light.Type type, float value) => lights[type] = value * brightness;
        public void SetRange(object ammo, float range, bool immediate) { Range = range; }
    }
}
namespace M1A1Abrams
{
    public class TestSetting { public bool Value; }
    public static class M1A1 { public static TestSetting de_fixed_reticle_size = new TestSetting { Value = true }; }
    internal static class AmpFeatures
    {
        internal static TestSetting BetterDaysight = new TestSetting(), BetterFlir = new TestSetting(), BetterGas = new TestSetting();
    }
    internal static class Assets { internal static UnityEngine.Material flir_blit_mat_green_no_scan = new UnityEngine.Material(); }
}
