// Offline API substitutes. Rack charge placement and clip retrieval follow the
// installed game's decompiled code. These tests do not simulate Unity flight/rendering.
#pragma warning disable CS0649, CS0169, CS0414 // Native private fields are accessed by reflection.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static T Instantiate<T>(T source) where T : Object => (T)((ICloneable)source).Clone();
        public static void DestroyImmediate(Object source) { }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
    }
    public class MonoBehaviour : Component { }
    public class Transform : Component
    {
        public Transform parent;
        public Vector3 forward = new Vector3(0, 0, 1);
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
    }
    public class GameObject : Object, ICloneable
    {
        private readonly List<Component> components = new List<Component>();
        public readonly Transform transform;
        public GameObject() { transform = new Transform { gameObject = this }; }
        public T AddComponent<T>() where T : Component, new()
        {
            var value = new T { gameObject = this }; components.Add(value); return value;
        }
        public T GetComponent<T>() where T : Component => components.OfType<T>().FirstOrDefault();
        public object Clone()
        {
            var copy = new GameObject();
            var stored = GetComponent<GHPC.Weapons.AmmoStoredVisual>();
            if (stored != null)
            {
                var value = copy.AddComponent<GHPC.Weapons.AmmoStoredVisual>();
                value.AmmoType = stored.AmmoType;
                value.AmmoScriptable = stored.AmmoScriptable;
                value.ChargeVisual = stored.ChargeVisual;
            }
            return copy;
        }
    }
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() => new T();
    }
    public static class Resources
    {
        public static readonly List<Object> Assets = new List<Object>();
        public static T[] FindObjectsOfTypeAll<T>() => Assets.OfType<T>().ToArray();
    }
}
namespace GHPC.Audio { public enum ImpactAudioType { Missile } }
public class AmmoType
{
    public enum GuidanceType { Unguided, Laser }
    public enum FlightPattern { Direct, Hump }
    public enum AmmoShortName { He, Missile }
    public enum AmmoCategory { Explosive }
    public sealed class ArmorOptimization { public GHPC.Weaponry.ArmorCodexScriptable Armor; public float RhaRatio; }
    public sealed class AmmoClip
    {
        public string Name;
        public int Capacity;
        public GHPC.Weaponry.AmmoCodexScriptable[] MinimalPattern;
    }
    public string Name;
    public float Caliber, ArmingDistance, CertainRicochetAngle, MaximumRange, MinSpallRha, MaxSpallRha,
        MuzzleVelocity, Mass, RhaPenetration, SpallMultiplier, TurnSpeed, RangedFuseTime, TntEquivalentKg,
        SpiralPower, SpiralAngularRate, NoisePowerX, NoisePowerY, NoiseTimeScale, GuidanceLockoutTime,
        GuidanceNoLockoutRange, GuidanceLeadDistance, Coeff, ImpactFuseTime,
        ClimbAngle, DiveAngle, LoiterAltitude, AimPointMarch;
    public bool Tandem, UseErrorCorrection, UseTracer, NoLeadCompensation, ShatterOnRicochet;
    public GuidanceType Guidance;
    public FlightPattern Flight;
    public AmmoShortName ShortName;
    public AmmoCategory Category;
    public GHPC.Audio.ImpactAudioType ImpactAudio;
    public UnityEngine.GameObject VisualModel, ShotVisual, DetonateEffect, TerrainImpactEffect;
    public object VisualType, ImpactEffectDescriptor, ImpactDecalDescriptor;
    public ArmorOptimization[] ArmorOptimizations;
    public int CachedIndex, DetonateSpallCount;
}
namespace GHPC.Weaponry
{
    public class AmmoCodexScriptable : UnityEngine.ScriptableObject { public AmmoType AmmoType; }
    public class AmmoClipCodexScriptable : UnityEngine.ScriptableObject
    {
        public AmmoType.AmmoClip ClipType;
        public WeaponSystemCodexScriptable[] CompatibleWeaponSystems;
    }
    public class WeaponSystemCodexScriptable : UnityEngine.ScriptableObject { }
    public class ArmorCodexScriptable : UnityEngine.ScriptableObject { }
    public class VehicleAmmoListScriptable { public AmmoClipCodexScriptable[] AmmoClips; }
}
namespace GHPC.Weapons
{
    using UnityEngine;
    using GHPC.Weaponry;
    public class AmmoStoredVisual : Component
    {
        public AmmoType AmmoType;
        public AmmoCodexScriptable AmmoScriptable;
        public Transform ChargeVisual;
    }
    public class AmmoRack
    {
        public int ClipCapacity;
        public bool UseVisibleRounds = true, RackSafeToUse = true;
        public readonly List<Transform> VisualSlots = new List<Transform>();
        public readonly List<AmmoType.AmmoClip> StoredClips = new List<AmmoType.AmmoClip>();
        public AmmoType.AmmoClip[] ClipTypes;
        public void SetUpVisualSlots() { ClipCapacity = VisualSlots.Count; }
        public void AddVisibleClip(AmmoType.AmmoClip clip)
        {
            var visual = Object.Instantiate(clip.MinimalPattern[0].AmmoType.VisualModel);
            // Native AmmoRack dereferences PairedChargeSlot when ChargeVisual exists.
            if (visual.GetComponent<AmmoStoredVisual>()?.ChargeVisual != null)
                throw new NullReferenceException("Abrams has no PairedChargeSlot");
            StoredClips.Add(clip);
        }
        public bool RetrieveClipWithPattern(AmmoType.AmmoClip clip)
        {
            int index = StoredClips.IndexOf(clip);
            if (index < 0) return false;
            StoredClips.RemoveAt(index); return true;
        }
    }
    public class LoadoutManager
    {
        public class RackLoadout
        {
            public AmmoRack Rack;
            public int[] AmmoCounts, ForbiddenAmmoIndices;
            public RackLoadoutFixedChoice[] FixedChoices;
            public AmmoClipCodexScriptable[] OverrideInitialClips;
        }
        public class RackLoadoutFixedChoice { }
        public RackLoadout[] RackLoadouts;
        public int[] TotalAmmoCounts;
        public AmmoClipCodexScriptable[] LoadedAmmoTypes;
        public VehicleAmmoListScriptable LoadedAmmoList = new VehicleAmmoListScriptable();
        private int _totalAmmoTypes, _totalAmmoCount;
        public bool IsRestocking = true;
        public void StopRestock() { IsRestocking = false; }
        public void SpawnCurrentLoadout()
        {
            if (_totalAmmoTypes != LoadedAmmoList.AmmoClips.Length || _totalAmmoCount != TotalAmmoCounts.Sum())
                throw new Exception("Inconsistent native loadout metadata");
            foreach (var entry in RackLoadouts)
            {
                if (entry.OverrideInitialClips != null) throw new Exception("Old overrides remain");
                entry.Rack.ClipTypes = LoadedAmmoList.AmmoClips.Select(c => c.ClipType).ToArray();
                for (int i = 0; i < _totalAmmoTypes; i++)
                    for (int j = 0; j < entry.AmmoCounts[i]; j++) entry.Rack.AddVisibleClip(entry.Rack.ClipTypes[i]);
            }
        }
        public int GetCurrentAmmoCount(AmmoType.AmmoClip clip) => RackLoadouts.Sum(e => e.Rack.StoredClips.Count(c => c == clip));
    }
    public class AmmoFeed
    {
        private Queue<AmmoType> _feedClipMain = new Queue<AmmoType>();
        private Queue<AmmoType> _feedClipAux = new Queue<AmmoType>();
        private AmmoType.AmmoClip _queuedClipTypeLockedIn;
        private MissileGuidanceUnit _missileGuidance;
        private float _clipFeedTime, _roundFeedTime;
        private int _clipFeedStage, _roundFeedStage;
        public AmmoRack ReadyRack;
        public bool ReloadDuringMissileTracking = true;
        public bool Reloading { get; private set; }
        public bool Cycling { get; private set; }
        public bool WaitingOnMissile { get; private set; }
        public AmmoType AmmoTypeInBreech { get; private set; }
        public AmmoType.AmmoClip LoadedClipType { get; private set; }
        public AmmoType.AmmoClip QueuedClipType { get; private set; }
        public int CurrentClipRemainingCount => _feedClipMain.Count + (AmmoTypeInBreech == null ? 0 : 1);
        public int GuidanceStoppedCalls;
        public void SeedOldAmmo(AmmoType.AmmoClip old)
        {
            AmmoTypeInBreech = old.MinimalPattern[0].AmmoType;
            LoadedClipType = QueuedClipType = _queuedClipTypeLockedIn = old;
            _feedClipMain.Enqueue(AmmoTypeInBreech); _feedClipAux.Enqueue(AmmoTypeInBreech);
            Reloading = Cycling = WaitingOnMissile = true;
            _clipFeedTime = _roundFeedTime = 5; _clipFeedStage = _roundFeedStage = 2;
        }
        private bool TryFinalFeedNewClip(int forcedSlotIndex)
        {
            if (!ReadyRack.RackSafeToUse) return false;
            _feedClipMain.Clear();
            if (!ReadyRack.RetrieveClipWithPattern(_queuedClipTypeLockedIn)) return false;
            for (int i = 0; i < _queuedClipTypeLockedIn.Capacity; i++)
                _feedClipMain.Enqueue(_queuedClipTypeLockedIn.MinimalPattern[i % _queuedClipTypeLockedIn.MinimalPattern.Length].AmmoType);
            if (AmmoTypeInBreech == null) AmmoTypeInBreech = _feedClipMain.Dequeue();
            LoadedClipType = _queuedClipTypeLockedIn; return true;
        }
        private void ToggleExclusiveItems(AmmoType.AmmoClip clip) { }
        public void Select(AmmoType.AmmoClip clip) { QueuedClipType = clip; }
        public void Fire(AmmoType ammo)
        {
            AmmoTypeInBreech = null;
            if (ammo.Guidance != AmmoType.GuidanceType.Unguided && !ReloadDuringMissileTracking)
                WaitingOnMissile = true;
            else Reload();
        }
        private void Reload()
        {
            _queuedClipTypeLockedIn = QueuedClipType;
            TryFinalFeedNewClip(-1);
        }
        private void MissileGuidanceStopped()
        {
            GuidanceStoppedCalls++; WaitingOnMissile = false;
            if (AmmoTypeInBreech == null) Reload();
        }
    }
    public class Optic : Component { }
    public class FireControlSystem : Component { public Optic MainOptic; public float CurrentRange; }
    public class WeaponSystem : Component
    {
        public AmmoFeed Feed;
        public FireControlSystem FCS;
        public bool FireWhileGuidingMissile = true;
        public event Action<AmmoType, LiveRound> Fired;
        public void EmitFire(AmmoType ammo, LiveRound round) { Feed.Fire(ammo); Fired?.Invoke(ammo, round); }
    }
    public class LiveRound : Component { public Vector3 GoalVector; }
    public class MissileGuidanceUnit : Component
    {
        public Transform AimElement;
        public float RangeSetting;
        public event Action GuidanceStopped;
        public int CallbackCount => GuidanceStopped?.GetInvocationList().Length ?? 0;
        public void AddMissile(LiveRound round) { }
        private void PerformGuidance() { }
        public void Stop() { GuidanceStopped?.Invoke(); }
    }
}
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public class HarmonyPatch : Attribute { }
    public static class AccessTools
    {
        public static MethodInfo Method(Type type, string name) => type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
}
namespace MelonLoader
{
    public static class MelonLogger
    {
        public static readonly List<string> Warnings = new List<string>();
        public static void Warning(string value) { Warnings.Add(value); }
        public static void Error(string value) { throw new Exception(value); }
        public static void Msg(string value) { }
    }
}
namespace ModUtil
{
    public abstract class Module { public abstract void LoadDynamicAssets(); public abstract void UnloadDynamicAssets(); }
    public static class AssetUtil { public static void LoadVanillaVehicle(string name) { } }
    public static class Util
    {
        public static void CacheAmmo(AmmoType value) { }
        public static void ShallowCopy(object target, object source)
        {
            foreach (var field in target.GetType().GetFields()) field.SetValue(target, field.GetValue(source));
        }
        public static void EmptyRack(GHPC.Weapons.AmmoRack rack) { rack.StoredClips.Clear(); }
    }
}
namespace M1A1Abrams
{
    public static class Assets { public static AmmoType ammo_m456; }
    public static class AmpFeatures
    {
        public static object AmpFragments, HeorFragments;
        public static int FragmentCount(object setting, int fallback) => fallback;
    }
}
