using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using GHPC.Weapons;
using GHPC.Weaponry;
using M1A1Abrams;
using ModUtil;
using UnityEngine;

internal static class AmmoIntegrationTests
{
    private static int checks;
    private static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(message);
    }
    private static T Component<T>() where T : UnityEngine.Component, new() => new GameObject().AddComponent<T>();
    private static AmmoType Donor(string name, bool charge = false)
    {
        var visual = new GameObject();
        var stored = visual.AddComponent<AmmoStoredVisual>();
        if (charge) stored.ChargeVisual = new GameObject().transform;
        return new AmmoType { Name = name, VisualModel = visual, ShotVisual = new GameObject() };
    }
    private static void Register(AmmoType ammo, string codexName = null)
    {
        Resources.Assets.Add(new AmmoCodexScriptable { name = codexName, AmmoType = ammo });
    }
    private static AmmoClipCodexScriptable Clip(AmmoType ammo) => new AmmoClipCodexScriptable
    {
        ClipType = new AmmoType.AmmoClip { Name = ammo.Name, Capacity = 1,
            MinimalPattern = new[] { new AmmoCodexScriptable { AmmoType = ammo } } }
    };
    private static LoadoutManager Loadout(int[] capacities, AmmoClipCodexScriptable old)
    {
        return new LoadoutManager
        {
            LoadedAmmoList = new VehicleAmmoListScriptable { AmmoClips = new[] { old } },
            RackLoadouts = capacities.Select(capacity =>
            {
                var rack = new AmmoRack { ClipCapacity = capacity };
                for (int i = 0; i < capacity; i++) rack.VisualSlots.Add(new GameObject().transform);
                rack.StoredClips.Add(old.ClipType);
                return new LoadoutManager.RackLoadout { Rack = rack,
                    AmmoCounts = new[] { capacity }, OverrideInitialClips = new[] { old },
                    ForbiddenAmmoIndices = new[] { 2, 3 }, FixedChoices = new LoadoutManager.RackLoadoutFixedChoice[0] };
            }).ToArray()
        };
    }
    private static void Inventory(AmmoClipCodexScriptable[] clips, AmmoClipCodexScriptable old,
        int[] requested, bool vanilla)
    {
        var manager = Loadout(new[] { 22, 22, 8 }, old);
        var feed = new AmmoFeed { ReadyRack = manager.RackLoadouts[0].Rack };
        feed.SeedOldAmmo(old.ClipType);
        // Repeat conversion to detect retained rounds, stale timers, and duplicated stock.
        for (int pass = 0; pass < 2; pass++)
        {
            MainGunLoadout.Apply(manager, clips, requested, vanilla);
            MainGunLoadout.Refill(feed);
            Check(!manager.IsRestocking, "Restock not cancelled");
            Check(manager.LoadedAmmoTypes.SequenceEqual(clips), "Old ammo metadata remains");
            Check(!feed.Reloading && !feed.Cycling && !feed.WaitingOnMissile, "Stale loader state remains");
            Check(feed.AmmoTypeInBreech == clips[0].ClipType.MinimalPattern[0].AmmoType, "M774 remains in breech");
            Check(Reflect.Get<Queue<AmmoType>>(feed, "_feedClipAux").Count == 0, "Old auxiliary feed remains");
            Check(Reflect.Get<float>(feed, "_clipFeedTime") == 0 && Reflect.Get<int>(feed, "_roundFeedStage") == 0, "Old reload timers remain");
            Check(manager.GetCurrentAmmoCount(old.ClipType) == 0, "M774 remains in racks");
            foreach (var clip in clips.Select(c => c.ClipType).Distinct())
            {
                int expected = Enumerable.Range(0, clips.Length).Where(i => clips[i].ClipType == clip).Sum(i => manager.TotalAmmoCounts[i]);
                int loaded = feed.LoadedClipType == clip ? feed.CurrentClipRemainingCount : 0;
                Check(manager.GetCurrentAmmoCount(clip) + loaded == expected, "Lost/extra rounds: " + clip.Name);
            }
            Check(manager.RackLoadouts.All(e => e.AmmoCounts.Length == 4 && e.Rack.StoredClips.Count <= e.Rack.ClipCapacity), "Bad four-type rack capacity");
            MelonLoader.MelonLogger.Warnings.Clear();
            MainGunLoadout.Verify(manager, feed);
            Check(MelonLoader.MelonLogger.Warnings.Count == 0, "Conversion inventory verification failed");
        }
        feed.ReadyRack.StoredClips.Add(old.ClipType);
        MainGunLoadout.Verify(manager, feed);
        Check(MelonLoader.MelonLogger.Warnings.Count == 1, "Extra M774 escapes inventory verification");
        feed.ReadyRack.StoredClips.Remove(old.ClipType);
        // Native restock retrieves every selected type from stowage into the ready rack.
        foreach (var clip in clips.Select(c => c.ClipType).Distinct())
        {
            var reserve = manager.RackLoadouts.Skip(1).FirstOrDefault(e => e.Rack.StoredClips.Contains(clip));
            if (reserve == null) continue;
            int before = manager.GetCurrentAmmoCount(clip);
            Check(reserve.Rack.RetrieveClipWithPattern(clip), "Reserve cannot retrieve " + clip.Name);
            feed.ReadyRack.AddVisibleClip(clip);
            Check(manager.GetCurrentAmmoCount(clip) == before, "Restock lost ammo");
        }
    }
    private static void Guidance(AmmoClipCodexScriptable[] clips)
    {
        var gun = Component<WeaponSystem>();
        gun.FCS = Component<FireControlSystem>();
        var day = Component<Optic>(); var thermal = Component<Optic>();
        gun.FCS.MainOptic = day; gun.FCS.CurrentRange = 1400;
        var manager = Loadout(new[] { 22, 22, 8 }, Clip(Donor("M774")));
        gun.Feed = new AmmoFeed { ReadyRack = manager.RackLoadouts[0].Rack };
        MainGunLoadout.Apply(manager, clips, new[] { 10, 10, 10, 10 }, true);
        var unit = Component<MissileGuidanceUnit>();
        var guidance = unit.gameObject.AddComponent<LahatGuidance>();
        guidance.Bind(gun, unit); guidance.Bind(gun, unit);
        Check(unit.CallbackCount == 1, "Guidance-stop subscribed twice");
        Check(!gun.FireWhileGuidingMissile && !gun.Feed.ReloadDuringMissileTracking, "PIL reload/fire interlock missing");
        Check(unit.AimElement == day.transform && unit.RangeSetting == 1400, "Initial aim/range not bound");
        var patch = typeof(LahatGuidance).GetNestedType("AimPatch", BindingFlags.NonPublic);
        var targets = (MethodBase[])patch.GetMethod("TargetMethods", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        Check(targets.Select(m => m.Name).OrderBy(n => n).SequenceEqual(new[] { "AddMissile", "PerformGuidance" }), "Launch/guidance patches not both selected");
        Check(patch.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic) == null, "Custom steering overrides native guidance");
        foreach (var optic in new[] { thermal, day, thermal, day })
        {
            gun.FCS.MainOptic = optic; gun.FCS.CurrentRange += 100;
            patch.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { unit });
            Check(unit.AimElement == optic.transform && unit.RangeSetting == gun.FCS.CurrentRange, "Day/thermal switch keeps stale guidance aim");
        }
        var round = Component<LiveRound>(); round.GoalVector = new Vector3(1, 0, 0);
        gun.Feed.Select(AmpAmmo.clip_lahat.ClipType);
        gun.EmitFire(AmpAmmo.ammo_lahat, round);
        Check(round.GoalVector.z == 1 && round.GoalVector.x == 0, "Pooled missile retains old launch steering");
        Check(gun.Feed.WaitingOnMissile, "Reload starts during guidance");
        unit.Stop();
        Check(!gun.Feed.WaitingOnMissile && gun.Feed.GuidanceStoppedCalls == 1, "Native loader not released");
        Check(gun.Feed.AmmoTypeInBreech == AmpAmmo.ammo_lahat, "Next LAHAT not loaded");
        Reflect.Call(guidance, "OnDestroy");
        Check(unit.CallbackCount == 0, "Guidance callback leaked");
    }
    private static void Main()
    {
        Assets.ammo_m456 = Donor("M456");
        var he = Donor("3OF26 HEF-FS-T", true);
        he.DetonateEffect = new GameObject(); he.ImpactEffectDescriptor = new object();
        var tow = Donor("BGM-71C I-TOW"); tow.DetonateEffect = new GameObject(); tow.GuidanceLeadDistance = 777;
        var kobra = Donor("9M112M"); kobra.GuidanceLeadDistance = 300; kobra.GuidanceLockoutTime = 0.2f;
        kobra.NoisePowerX = 0.1f; kobra.Flight = AmmoType.FlightPattern.Hump;
        Register(he); Register(tow); Register(kobra, "ammo_9M112M");
        var module = new AmpAmmo(); module.LoadDynamicAssets();
        new AmmoRack().AddVisibleClip(AmpAmmo.clip_m908.ClipType);
        Check(AmpAmmo.ammo_m908.VisualModel.GetComponent<AmmoStoredVisual>().ChargeVisual == null, "M908 still has a separate charge");
        Check(AmpAmmo.ammo_m908.DetonateEffect == he.DetonateEffect, "M908 lost native HE effects");
        // The LAHAT borrows the TOW for warhead and flight visuals, but its guidance numbers are
        // its own: they drive LahatTopAttack and must not be whatever the donor missile had.
        Check(AmpAmmo.ammo_lahat.TurnSpeed == 6f && AmpAmmo.ammo_lahat.ClimbAngle == 20f
            && AmpAmmo.ammo_lahat.DiveAngle == 34f && AmpAmmo.ammo_lahat.LoiterAltitude == 140f
            && AmpAmmo.ammo_lahat.AimPointMarch == 2f
            && AmpAmmo.ammo_lahat.GuidanceLockoutTime == 0f
            && AmpAmmo.ammo_lahat.GuidanceNoLockoutRange == 0f
            && AmpAmmo.ammo_lahat.NoLeadCompensation,
            "LAHAT does not carry its own gun-launched guidance numbers");
        Check(AmpAmmo.ammo_lahat.Guidance == AmmoType.GuidanceType.Laser && AmpAmmo.ammo_lahat.Flight == AmmoType.FlightPattern.Direct, "Wrong LAHAT guidance/flight");
        Check(AmpAmmo.ammo_lahat.DetonateEffect == tow.DetonateEffect && AmpAmmo.ammo_lahat.ShotVisual == tow.ShotVisual, "Warhead/flight donors changed incorrectly");
        foreach (var clip in AmpAmmo.All.Values)
        {
            var ammo = clip.ClipType.MinimalPattern[0].AmmoType;
            var stored = ammo.VisualModel.GetComponent<AmmoStoredVisual>();
            Check(stored.AmmoType == ammo && stored.AmmoScriptable == clip.ClipType.MinimalPattern[0], "Wrong stored visual identity");
            new AmmoRack().AddVisibleClip(clip.ClipType);
        }
        var ap = Clip(Donor("M829A4")); var old = Clip(Donor("M774"));
        var clips = new[] { ap, AmpAmmo.clip_m908, AmpAmmo.clip_xm1147, AmpAmmo.clip_lahat };
        Inventory(clips, old, new[] { 10, 10, 10, 10 }, true);
        Inventory(clips, old, new[] { 10, 10, 10, 10 }, false);
        Inventory(clips, old, new[] { 22, 18, 12, 6 }, false);
        Inventory(new[] { ap, AmpAmmo.clip_m908, AmpAmmo.clip_lahat, AmpAmmo.clip_lahat }, old, new[] { 10, 10, 10, 10 }, false);
        Inventory(clips, old, new[] { 1, 0, 0, 0 }, false);
        Guidance(clips);
        Console.WriteLine("PASS: " + checks + " ammo conversion/guidance lifecycle checks using production modules and offline API substitutes.");
    }
}
