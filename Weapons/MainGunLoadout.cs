using GHPC.Weaponry;
using GHPC.Weapons;
using ModUtil;
using System.Text;
using System.Collections.Generic;

namespace M1A1Abrams
{
    internal static class MainGunLoadout
    {
        internal static void Apply(LoadoutManager manager, AmmoClipCodexScriptable[] clips,
            int[] requested, bool vanillaCapacity)
        {
            manager.StopRestock();
            int capacity = 0;
            for (int i = 0; i < manager.RackLoadouts.Length; i++)
            {
                GHPC.Weapons.AmmoRack rack = manager.RackLoadouts[i].Rack;
                if (!vanillaCapacity && i < 3) rack.ClipCapacity = i == 2 ? 4 : 18;
                if (rack.ClipCapacity < 0) rack.SetUpVisualSlots();
                if (rack.UseVisibleRounds)
                    rack.ClipCapacity = System.Math.Min(rack.ClipCapacity, rack.VisualSlots.Count);
                capacity += System.Math.Max(0, rack.ClipCapacity);
                Util.EmptyRack(rack);
            }
            int[] counts = AmmoLoadoutRules.Fit(requested, capacity);
            int total = 0;
            foreach (int count in counts) total += count;
            manager.TotalAmmoCounts = counts;
            manager.LoadedAmmoList.AmmoClips = clips;
            manager.LoadedAmmoTypes = clips;
            Reflect.Set(manager, "_totalAmmoTypes", clips.Length);
            Reflect.Set(manager, "_totalAmmoCount", total);

            // All converted main-gun racks accept the same 120 mm rounds. Replace the old
            // 105 mm distribution/overrides with explicit counts, including zero-filled slots.
            // This also avoids the game's automatic fill loop on intentionally partial loads.
            int[] remaining = (int[])counts.Clone();
            foreach (LoadoutManager.RackLoadout entry in manager.RackLoadouts)
            {
                entry.AmmoCounts = AmmoLoadoutRules.Fit(remaining, entry.Rack.ClipCapacity);
                for (int i = 0; i < remaining.Length; i++) remaining[i] -= entry.AmmoCounts[i];
                entry.FixedChoices = new LoadoutManager.RackLoadoutFixedChoice[0];
                entry.ForbiddenAmmoIndices = new int[0];
                entry.OverrideInitialClips = null;
            }
            manager.SpawnCurrentLoadout();
        }

        internal static void Refill(AmmoFeed feed)
        {
            // Re-run only the ammo setup, not Start(), which subscribes crew-health events.
            AmmoType.AmmoClip clip = feed.ReadyRack.ClipTypes[0];
            Reflect.Get<Queue<AmmoType>>(feed, "_feedClipMain").Clear();
            Reflect.Get<Queue<AmmoType>>(feed, "_feedClipAux").Clear();
            Reflect.SetProperty(feed, "LoadedClipType", null);
            Reflect.SetProperty(feed, "AmmoTypeInBreech", null);
            Reflect.SetProperty(feed, "Reloading", false);
            Reflect.SetProperty(feed, "Cycling", false);
            Reflect.SetProperty(feed, "WaitingOnMissile", false);
            Reflect.Set(feed, "_clipFeedTime", 0f);
            Reflect.Set(feed, "_clipFeedStage", 0);
            Reflect.Set(feed, "_roundFeedTime", 0f);
            Reflect.Set(feed, "_roundFeedStage", 0);
            Reflect.SetProperty(feed, "QueuedClipType", clip);
            Reflect.Set(feed, "_queuedClipTypeLockedIn", clip);
            if (!(bool)Reflect.Call(feed, "TryFinalFeedNewClip", -1))
                throw new System.InvalidOperationException("M256: failed to load the initial clip from the ready rack.");
            Reflect.Call(feed, "ToggleExclusiveItems", clip);
        }

        internal static void Verify(LoadoutManager manager, AmmoFeed feed)
        {
            var message = new StringBuilder("[M1A1] Main gun loaded: ");
            var clips = manager.LoadedAmmoList.AmmoClips;
            bool mismatch = false;
            int expectedTotal = 0;
            for (int i = 0; i < clips.Length; i++)
                expectedTotal += manager.TotalAmmoCounts[i] * clips[i].ClipType.Capacity;
            int actualTotal = feed.CurrentClipRemainingCount;
            foreach (LoadoutManager.RackLoadout entry in manager.RackLoadouts)
                foreach (AmmoType.AmmoClip stored in entry.Rack.StoredClips)
                    actualTotal += stored.Capacity;
            mismatch |= actualTotal != expectedTotal;
            for (int i = 0; i < clips.Length; i++)
            {
                AmmoType.AmmoClip clip = clips[i].ClipType;
                bool duplicate = false;
                for (int j = 0; j < i; j++) if (clip.Equals(clips[j].ClipType)) duplicate = true;
                if (duplicate) continue;
                int expected = 0;
                for (int j = 0; j < clips.Length; j++)
                    if (clip.Equals(clips[j].ClipType)) expected += manager.TotalAmmoCounts[j];
                int loaded = feed.LoadedClipType != null && feed.LoadedClipType.Equals(clip)
                    ? feed.CurrentClipRemainingCount : 0;
                int actual = loaded + manager.GetCurrentAmmoCount(clip);
                mismatch |= actual != expected;
                message.Append(clip.Name).Append(' ').Append(actual).Append('/').Append(expected).Append("; ");
            }
            message.Append("total ").Append(actualTotal).Append('/').Append(expectedTotal);
            if (mismatch) MelonLoader.MelonLogger.Warning(message.ToString());
            else MelonLoader.MelonLogger.Msg(message.ToString());
        }
    }

}
