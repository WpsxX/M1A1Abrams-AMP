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

[assembly: MelonInfo(typeof(M1A1AbramsMod), "ZZ M1A1 Abrams AMP", "2.7.6", "Cyance, ATLAS, Schweiz, WPSX")]
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
            // Keep 测试2's own preference category so the existing AMP/引信 settings are preserved
            // across this port (CROWSFIX 1.3.8 renamed it to "M1A1-CROWSFIX").
            MelonPreferences_Category cfg = MelonPreferences.CreateCategory("M1A1Config");
            M1A1.Config(cfg);
            AmpFeatures.Config(cfg);
            AmpUpgrades.Config(cfg);
            MelonLogger.Msg("[M1A1 AMP] fixed build 2.7.6 "
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

        public override void OnSceneWasLoaded(int idx, string scene_name) {
            if (Util.MenuScreens.Contains(scene_name))
            {
                ResetMissionAssets();
                valid_scene_count = 0;
                module_manager.LoadAllStaticAssets();

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

            // A mission loads in two stages; an additive scene must not tear down
            // the dynamic assets loaded for the mission's PlayerReady callback.
            if (valid_scene_count == 1)
            {
                ResetMissionAssets();
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
