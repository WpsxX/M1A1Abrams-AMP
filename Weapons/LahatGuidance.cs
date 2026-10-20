using System;
using System.Reflection;
using GHPC.Weapons;
using HarmonyLib;
using ModUtil;
using UnityEngine;

namespace M1A1Abrams
{
    // PIL's CustomGuidanceComputer follows FCS.MainOptic and leaves steering to
    // MissileGuidanceUnit. Bind the converted gun/feed without repeating Awake/Start.
    public sealed class LahatGuidance : MonoBehaviour
    {
        private WeaponSystem weapon;
        private MissileGuidanceUnit unit;
        private AmmoFeed feed;
        private Action reloadAfterGuidance;

        internal void Bind(WeaponSystem gun, MissileGuidanceUnit guidance)
        {
            Unbind();
            weapon = gun;
            unit = guidance;
            feed = gun.Feed;
            weapon.Fired += Fired;
            weapon.FireWhileGuidingMissile = false;
            if (feed != null)
            {
                feed.ReloadDuringMissileTracking = false;
                // RegisterWeaponSystem already ran before the M256 conversion. Bind
                // just the missing handler, without duplicating firing/crew events.
                reloadAfterGuidance = (Action)Delegate.CreateDelegate(typeof(Action), feed,
                    AccessTools.Method(typeof(AmmoFeed), "MissileGuidanceStopped"));
                MissileGuidanceUnit previous = Reflect.Get<MissileGuidanceUnit>(feed, "_missileGuidance");
                if (previous != null) previous.GuidanceStopped -= reloadAfterGuidance;
                Reflect.Set(feed, "_missileGuidance", unit);
                unit.GuidanceStopped -= reloadAfterGuidance;
                unit.GuidanceStopped += reloadAfterGuidance;
            }
            RefreshAim();
        }

        private void Unbind()
        {
            if (weapon != null) weapon.Fired -= Fired;
            if (unit != null && reloadAfterGuidance != null) unit.GuidanceStopped -= reloadAfterGuidance;
            reloadAfterGuidance = null;
        }

        private void OnDestroy() { Unbind(); }

        private void Update() { RefreshAim(); }

        internal void RefreshAim()
        {
            // Like PIL's T-80 guidance computer, follow the active main-gun optic.
            // Switching GPS day/thermal must not leave the laser on the old transform.
            if (unit != null && weapon != null && weapon.FCS != null)
            {
                unit.AimElement = weapon.FCS.MainOptic == null
                    ? weapon.FCS.transform : weapon.FCS.MainOptic.transform;
                unit.RangeSetting = weapon.FCS.CurrentRange;
            }
        }

        private void Fired(AmmoType ammo, LiveRound round)
        {
            if (round == null || ammo != AmpAmmo.ammo_lahat) return;
            // Fired runs after muzzle placement and Init; AddMissile runs too early.
            // Never inherit a pooled round's old steering vector on the launch frame.
            round.GoalVector = round.transform.forward;
        }

        [HarmonyPatch]
        private static class AimPatch
        {
            private static MethodBase[] TargetMethods()
            {
                return new[] {
                    AccessTools.Method(typeof(MissileGuidanceUnit), "AddMissile"),
                    AccessTools.Method(typeof(MissileGuidanceUnit), "PerformGuidance")
                };
            }

            private static void Prefix(MissileGuidanceUnit __instance)
            {
                LahatGuidance guidance = __instance.GetComponent<LahatGuidance>();
                if (guidance != null) guidance.RefreshAim();
            }
        }
    }
}
