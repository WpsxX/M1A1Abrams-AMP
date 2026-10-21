using System;
using System.Linq;
using MelonLoader;
using M1A1Abrams;      // the assembly attributes below name the mod type
using GHPC.State;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using GHPC.Vehicle;
using ModUtil;
using Presets;

[assembly: MelonInfo(typeof(M1A1AbramsMod), "ZZ M1A1 Abrams AMP", "2.7.7", "Cyance, ATLAS, Schweiz, WPSX")]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace M1A1Abrams
{
    public class M1A1AbramsMod : MelonMod {
        private ModuleManager module_manager;
        public static Vehicle[] vics;
        public static GameObject game_manager;
        private int valid_scene_count = 0;
        private object mission_watcher;
        private bool converted_this_mission;

        public IEnumerator OnGameReady(GameState _)
        {
            vics = GameObject.FindObjectsByType<Vehicle>(FindObjectsSortMode.None);
            game_manager = GameObject.Find("_APP_GHPC_");

            module_manager.LoadAllDynamicAssets();

            yield break;
        }

        public override void OnInitializeMelon()
        {
            // MelonLoader installs this assembly's Harmony patches once during initialization.

            module_manager = new ModuleManager();
            // Upstream AMP uses the "M1A1AMPConfig" category. Older builds of this fork wrote
            // "M1A1Config", so on the first launch after the rename the old values are copied
            // across (and the stale section removed) instead of silently resetting to defaults.
            bool fresh_category = MelonPreferences.GetCategory("M1A1AMPConfig") == null;
            MelonPreferences_Category cfg = MelonPreferences.CreateCategory("M1A1AMPConfig");
            M1A1.Config(cfg);
            AmpFeatures.Config(cfg);
            AmpUpgrades.Config(cfg);

            if (fresh_category)
            {
                MigrateLegacyConfig(cfg);
            }
            MelonLogger.Msg("[M1A1 AMP] fixed build 2.7.7 "
                + typeof(M1A1AbramsMod).Module.ModuleVersionId + "; DLL=" + typeof(M1A1AbramsMod).Assembly.Location);
            MelonLogger.Msg("[AMP] Loaded configuration: M1A1 Engine=" + M1A1.AgtM1.Value
                + ", M1E1 Engine=" + M1A1.AgtM1e1.Value + ", M1A1 Armor=" + AmpUpgrades.ArmorM1A1.Value
                + ", M1E1 Armor=" + AmpUpgrades.ArmorM1E1.Value + ", UAP=" + AmpUpgrades.Uap.Value
                + ", GovernorDelete=" + AmpFeatures.GovernorDelete.Value + ", StabilityControl=" + AmpFeatures.StabilityControl.Value);

            module_manager.Add("AMMO_120MM", new Ammo_120mm());
            module_manager.Add("AMMO_AMP", new AmpAmmo());
            module_manager.Add("DUArmour", new DUArmour());
            module_manager.Add("Assets", new Assets());
            module_manager.Add("GAS", new GAS());

            PresetManager.LoadAllPresets();
        }

        // Copies every value from the legacy "M1A1Config" section into the new "M1A1AMPConfig"
        // category. Only runs when the new category did not exist yet, so it can never overwrite
        // settings the player already made under the new name.
        private static void MigrateLegacyConfig(MelonPreferences_Category target)
        {
            MelonPreferences_Category legacy = MelonPreferences.GetCategory("M1A1Config");

            if (legacy == null)
            {
                return;
            }

            int copied = 0;

            foreach (MelonPreferences_Entry old_entry in legacy.Entries)
            {
                foreach (MelonPreferences_Entry new_entry in target.Entries)
                {
                    if (!string.Equals(new_entry.Identifier, old_entry.Identifier, System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    try
                    {
                        new_entry.BoxedValue = old_entry.BoxedValue;
                        copied++;
                    }
                    catch (System.Exception)
                    {
                        // Type mismatch between two builds: keep the new default.
                    }

                    break;
                }
            }

            ForgetCategory(legacy);
            MelonPreferences.Save();
            MelonLogger.Msg("[AMP] migrated " + copied + " setting(s) from [M1A1Config] to [M1A1AMPConfig]");
        }

        // MelonPreferences has no public "drop this category" call, so the legacy section is
        // unregistered from the static category list (guarded by reflection) before the next
        // save, otherwise every later save would keep rewriting the stale section.
        private static void ForgetCategory(MelonPreferences_Category category)
        {
            try
            {
                System.Reflection.FieldInfo field = typeof(MelonPreferences).GetField(
                    "Categories",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

                if (field == null)
                {
                    return;
                }

                System.Collections.IList categories = field.GetValue(null) as System.Collections.IList;

                if (categories == null)
                {
                    return;
                }

                categories.Remove(category);
            }
            catch (System.Exception exception)
            {
                MelonLogger.Warning("[AMP] could not drop the legacy preference category: " + exception.Message);
            }
        }

        // The M1 -> M1IP spawn hook only rewrites the unit name; the spawner then loads the M1IP
        // prefab synchronously inside its own spawn loop. Loading it here, one scene before the
        // spawns run, turns that internal load into an already-resident hit and keeps our code out
        // of the spawn loop entirely - doing the load from the spawn hook as well nested a second
        // blocking Addressables read inside the loop and left the shared load state broken for the
        // other mods (they then failed with "Unable to load asset" on their own donor prefabs).
        private static void PreloadTransmogPrefab()
        {
            if (!M1A1.m1_to_m1ip.Value && !M1A1.m1e1_ip_model.Value)
            {
                return;
            }

            try
            {
                AssetUtil.LoadVanillaVehicle("M1IP");
            }
            catch (Exception e)
            {
                MelonLogger.Warning("[M1A1] could not preload the M1IP prefab: " + e.Message);
            }
        }

        public override void OnSceneWasLoaded(int idx, string scene_name) {
            if (Util.MenuScreens.Contains(scene_name))
            {
                ResetMissionAssets();
                valid_scene_count = 0;

                // Static assets (our seven bundles, about 32 MB) are deliberately NOT loaded
                // here. Doing it in the menu scene put a long synchronous bundle read into the
                // same window in which every other mod loads its own assets, which is where the
                // occasional stalled loading screen came from. Every consumer of them runs
                // during a conversion, so they are loaded on the first mission scene instead.

                // A watcher left over from the previous mission would keep polling for
                // vehicles in the menu diorama and convert a display tank, so it is stopped
                // here as well as guarded inside its own loop.
                if (mission_watcher != null)
                {
                    MelonCoroutines.Stop(mission_watcher);
                    mission_watcher = null;
                }

                // Do NOT call AssetUtil.ReleaseVanillaAssets() here. It releases every vanilla
                // asset handle in VanillaAssetUtil's shared list, including the ones other mods
                // loaded. A later LoadVanillaVehicle from another mod (Fulda 1989's Leopard 2A4
                // module loads the vanilla LEO2A3 prefab when a mission spawns) then hits a
                // released Addressables handle and throws "Unable to load asset of type
                // UnityEngine.GameObject from location ...", which leaves the mission without a
                // player platoon and freezes the loading screen under a per-frame NRE flood.
                // Our own vanilla assets are few and are re-used across missions, so keeping
                // them resident is the cheaper price.
            }

            if (Util.MenuScreensWithLoader.Contains(scene_name)) return;

            valid_scene_count++;

            // Loaded here, not in the menu: this keeps the menu load cheap and stops our 32 MB of
            // bundles from competing with the other mods' asset loads. Modules cache the result,
            // so this costs one read per launch.
            module_manager.LoadAllStaticAssets();

            // A mission loads in two stages; an additive scene must not tear down
            // the dynamic assets loaded for the mission's PlayerReady callback.
            if (valid_scene_count == 1)
            {
                ResetMissionAssets();
                PreloadTransmogPrefab();
            }

            if (valid_scene_count == 2)
            {       
                // Nothing in this mod uses the temp vanilla pool any more (the M1 to M1IP transmog
                // loads the M1 prefab permanently), so the PlayerReady temp release that used to be
                // registered here has no work left to do and was removed.
                StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(OnGameReady), GameStatePriority.Medium);

                CITVManager.Init();
                M1A1.Init();
                valid_scene_count = 0;
            }

            // The registration above is the documented path, but it can fail without a word: RunOrDefer
            // returns Fail while the state machine does not exist yet, and a handler deferred before the
            // StateController is torn down is dropped by its OnDestroy. Either one leaves the station
            // unbuilt with the vanilla cupola gun still on the roof, and neither logs anything. This
            // watcher never touches the state machine: it waits for the mission's vehicles and for
            // PlayerReady, then converts once.
            if (!Util.MenuScreens.Contains(scene_name) && mission_watcher == null)
            {
                mission_watcher = MelonCoroutines.Start(MissionWatcher());
            }
        }

        private IEnumerator MissionWatcher()
        {
            const int MAX_FRAMES = 3600; // about a minute at 60 fps

            Vehicle[] found = null;
            bool player_ready = false;

            bool left_mission = false;

            for (int frame = 0; frame < MAX_FRAMES; frame++)
            {
                // Leaving the mission has to stop the poll: the menu diorama spawns vehicles
                // of its own and the state machine still reports PlayerReady from the last
                // mission, so the two conditions this loop waits for can both be true there.
                if (frame % 15 == 0 && Util.MenuScreens.Contains(SceneManager.GetActiveScene().name))
                {
                    left_mission = true;
                    break;
                }

                found = GameObject.FindObjectsByType<Vehicle>(FindObjectsSortMode.None);
                player_ready = StateController.IsGameStateTriggered(GameState.PlayerReady)
                    || StateController.IsGameStateComplete(GameState.PlayerReady);

                if (found != null && found.Length > 0 && player_ready)
                {
                    break;
                }

                yield return null;
            }

            mission_watcher = null;

            if (left_mission || found == null || found.Length == 0 || !player_ready || converted_this_mission)
            {
                yield break;
            }

            converted_this_mission = true;
            vics = found;

            if (game_manager == null)
            {
                game_manager = GameObject.Find("_APP_GHPC_");
            }
            module_manager.LoadAllDynamicAssets();
            yield return M1A1.Convert(GameState.PlayerReady);
        }

        private void ResetMissionAssets()
        {
            module_manager.UnloadAllDynamicAssets();
            M1A1.ResetConversionState();
            converted_this_mission = false;
            vics = null;
            game_manager = null;
        }
    }
}
