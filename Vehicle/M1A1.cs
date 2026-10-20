using System;
using MelonLoader;
using UnityEngine;
using GHPC.State;
using System.Collections;
using GHPC.AI;
using GHPC.Weapons;
using GHPC.Equipment.Optics;
using GHPC.Vehicle;
using GHPC.Utility;
using GHPC;
using GHPC.Camera;
using GHPC.Player;
using GHPC.Weaponry;
using ModUtil;
using Presets;
using M1A1Abrams.Presets;

namespace M1A1Abrams
{
    public class M1A1 : Module
    {
        static MelonPreferences_Entry<bool> use_preset_pool;

        internal static PresetManager<M1Preset> preset_manager_m1;
        static PresetManager<M1Preset> preset_manager_m1ip;

        static MelonPreferences_Entry<string> target_preset_m1;
        static MelonPreferences_Entry<string> target_preset_m1ip;

        static MelonPreferences_Entry<bool> has_m256_m1;
        static MelonPreferences_Entry<bool> has_m256_m1ip;

        static MelonPreferences_Entry<string> sabot_m1;
        static MelonPreferences_Entry<string> sabot_m1ip;
        static MelonPreferences_Entry<string> heat_m1;
        static MelonPreferences_Entry<string> heat_m1ip;

        static MelonPreferences_Entry<string> amp_round3_m1;
        static MelonPreferences_Entry<string> amp_round4_m1;
        static MelonPreferences_Entry<string> amp_round3_m1ip;
        static MelonPreferences_Entry<string> amp_round4_m1ip;
        static MelonPreferences_Entry<int> amp_count3_m1;
        static MelonPreferences_Entry<int> amp_count4_m1;
        static MelonPreferences_Entry<int> amp_count3_m1ip;
        static MelonPreferences_Entry<int> amp_count4_m1ip;
        public static MelonPreferences_Entry<string> AgtM1 { get; private set; }
        public static MelonPreferences_Entry<string> AgtM1e1 { get; private set; }

        static MelonPreferences_Entry<int> flir_gen_m1;
        static MelonPreferences_Entry<int> flir_gen_m1ip;

        static MelonPreferences_Entry<int> m829_count_m1;
        static MelonPreferences_Entry<int> m830_count_m1;

        static MelonPreferences_Entry<int> m829_count_m1ip;
        static MelonPreferences_Entry<int> m830_count_m1ip;

        static MelonPreferences_Entry<bool> rotate_azimuth_m1;
        static MelonPreferences_Entry<bool> rotate_azimuth_m1ip;

        static MelonPreferences_Entry<bool> citv_m1a1;
        static MelonPreferences_Entry<bool> citv_m1e1;

        static MelonPreferences_Entry<bool> du_package_m1;
        static MelonPreferences_Entry<int> du_gen_m1;
        static MelonPreferences_Entry<bool> du_package_m1ip;
        static MelonPreferences_Entry<int> du_gen_m1ip;

        public static MelonPreferences_Entry<bool> citv_smooth;

        public static MelonPreferences_Entry<bool> de_fixed_reticle_size;

        public static MelonPreferences_Entry<bool> crows_m1e1;
        public static MelonPreferences_Entry<bool> crows_m1a1;
        public static MelonPreferences_Entry<string> crows_ammo;

        public static MelonPreferences_Entry<bool> crows_mk19;

        public static MelonPreferences_Entry<string> crows_mk19_ammo;
        public static MelonPreferences_Entry<bool> m2_coax;
        public static MelonPreferences_Entry<bool> crows_alt_placement;

        public static MelonPreferences_Entry<bool> m1_to_m1ip;

        // Upstream-compatible switches (Cyances/M1A1AbramsAMP): identical identifiers and defaults
        // so a config carried over from that mod keeps working unchanged.
        public static MelonPreferences_Entry<bool> m1e1_ip_model;
        public static MelonPreferences_Entry<bool> random_conversion;
        public static MelonPreferences_Entry<int> conversion_chance;
        public static MelonPreferences_Entry<bool> m829_spall;

        public static MelonPreferences_Entry<bool> original_rack_size_m1;
        public static MelonPreferences_Entry<bool> original_rack_size_m1ip;

        static WeaponSystemCodexScriptable gun_m256;

        public static Vector2Int flir_gen2_res = new Vector2Int(1024, 576);
        public static Vector2Int flir_gen3_res = new Vector2Int(1366, 768);

        /// <summary>
        /// Hot reload runs OnInitializeMelon again while the previous preference entries are still
        /// registered, and CreateEntry throws in that case, which aborts the rest of the setup.
        /// Reusing an existing entry keeps the reload path working and is a no-op on a cold start.
        /// </summary>
        private static MelonPreferences_Entry<T> Entry<T>(MelonPreferences_Category cfg, string identifier, T default_value)
        {
            MelonPreferences_Entry<T> existing = cfg.GetEntry<T>(identifier);
            if (existing != null) return existing;
            return Prefs.Entry(cfg, identifier, default_value);
        }

        public static void Config(MelonPreferences_Category cfg)
        {
            PresetManager.ClearAll();
            preset_manager_m1 = null;
            preset_manager_m1ip = null;

            use_preset_pool = Entry<bool>(cfg, "Use Preset Bundle", false);
            target_preset_m1 = Entry<string>(cfg, "Preset Bundle Path (M1)", "m1a1amp_assets/PresetBundles/ExampleM1");
            target_preset_m1ip = Entry<string>(cfg, "Preset Bundle Path (M1IP)", "m1a1amp_assets/PresetBundles/ExampleM1IP");

            // This build uses its own asset folder so it can sit next to the released CROWS fix build
            // and the vanilla M1A1 Abrams mod without any of them colliding. Configs written by older
            // versions still point at one of the previous names, so rewrite them here instead of
            // leaving those users with a preset path that no longer exists.
            target_preset_m1.Value = target_preset_m1.Value
                .Replace("m1a1crowsfix_assets/", "m1a1amp_assets/")
                .Replace("m1a1assets/", "m1a1amp_assets/");
            target_preset_m1ip.Value = target_preset_m1ip.Value
                .Replace("m1a1crowsfix_assets/", "m1a1amp_assets/")
                .Replace("m1a1assets/", "m1a1amp_assets/");

            has_m256_m1 = Entry<bool>(cfg, "M256 Gun (M1)", false);
            has_m256_m1.Description = "120mm cannon (Note: anything relating to ammo is ignored if this is false)";
            has_m256_m1ip = Entry<bool>(cfg, "M256 Gun (M1IP)", true);

            m829_count_m1 = Entry<int>(cfg, "AP Round Count (M1E1)", 22);
            m829_count_m1.Description = "Maximum of 40 rounds total. Bring in at least one AP round.";
            m829_count_m1.Comment = "Counts are for the whole vehicle, including stowed racks. Native HUD shows loaded and ready-rack ammo.";
            m830_count_m1 = Entry<int>(cfg, "HEAT Round Count (M1E1)", 18);

            m829_count_m1ip = Entry<int>(cfg, "AP Round Count (M1A1)", 22);
            m830_count_m1ip = Entry<int>(cfg, "HEAT Round Count (M1A1)", 18);

            original_rack_size_m1 = Entry<bool>(cfg, "Vanilla Ammo Capacity (M1E1)", false);
            original_rack_size_m1.Description = "Carry a total of 52 rounds instead of 40.";
            original_rack_size_m1ip = Entry<bool>(cfg, "Vanilla Ammo Capacity (M1A1)", false);

            sabot_m1 = Entry<string>(cfg, "AP Round (M1E1)", "M827");
            sabot_m1.Description = "Customize which rounds M1A1s/M1E1s use";
            sabot_m1.Comment = "M827, M829, M829A1, M829A2, M829A3, M829A4";
            sabot_m1ip = Entry<string>(cfg, "AP Round (M1A1)", "M829");
            sabot_m1ip.Description = "Customize which rounds M1A1s/M1E1s use";
            sabot_m1ip.Comment = "M827, M829, M829A1, M829A2, M829A3, M829A4";

            heat_m1 = Entry<string>(cfg, "HEAT Round (M1E1)", "M830");
            heat_m1.Comment = "M830, M830A1 (has proximity fuse that can be toggled by double tapping the ammo 2 key)";
            heat_m1ip = Entry<string>(cfg, "HEAT Round (M1A1)", "M830");

            amp_round3_m1 = Entry<string>(cfg, "3rd Round (M1E1)", "M830A2");
            amp_round4_m1 = Entry<string>(cfg, "4th Round (M1E1)", "XM1147");
            amp_round3_m1ip = Entry<string>(cfg, "3rd Round (M1A1)", "M830A2");
            amp_round4_m1ip = Entry<string>(cfg, "4th Round (M1A1)", "XM1147");
            amp_count3_m1 = Entry<int>(cfg, "3rd Round Count (M1E1)", 0);
            amp_count4_m1 = Entry<int>(cfg, "4th Round Count (M1E1)", 0);
            amp_count3_m1ip = Entry<int>(cfg, "3rd Round Count (M1A1)", 0);
            amp_count4_m1ip = Entry<int>(cfg, "4th Round Count (M1A1)", 0);

            AgtM1e1 = Entry<string>(cfg, "M1E1 Engine", "AGT1500");
            AgtM1 = Entry<string>(cfg, "M1A1 Engine", "AGT1500");
            AgtM1.Comment = "Native M1IP uses this setting (also after CITV renaming): AGT1500 / AGT2000 / AGT2500 / AGT3000 / T64 / T55. Edit while game is closed, then restart.";
            AgtM1e1.Comment = "Original M1 uses this setting, including M1 -> M1IP conversion. Same engine choices as M1A1 Engine; edit while game is closed, then restart.";

            rotate_azimuth_m1 = Entry<bool>(cfg, "Rotate Azimuth (M1E1)", false);
            rotate_azimuth_m1.Description = "Horizontal stabilization of sights when applying lead.";
            rotate_azimuth_m1ip = Entry<bool>(cfg, "Rotate Azimuth (M1A1)", false);

            flir_gen_m1 = Entry<int>(cfg, "FLIR Generation (M1E1)", 1);
            flir_gen_m1.Description = "Settings for the gunner's thermals";
            flir_gen_m1.Comment = "Higher generation = higher resolution (1-3, integer)";
            flir_gen_m1ip = Entry<int>(cfg, "FLIR Generation (M1A1)", 1);

            cfg.DeleteEntry("Digital Enhancement (M1E1)");
            cfg.DeleteEntry("Digital Enhancement (M1A1)");
            de_fixed_reticle_size = Entry<bool>(cfg, "Fixed Reticle Size (Global)", false);
            de_fixed_reticle_size.Comment = "Keep GPS day/thermal reticle size constant across all zoom levels, including FLIR+/Daysight+.";

            citv_m1e1 = Entry<bool>(cfg, "CITV (M1E1)", false);
            citv_m1e1.Description = "Replaces commander's NVGs with variable-zoom thermals.";
            citv_m1a1 = Entry<bool>(cfg, "CITV (M1A1)", false);

            // Retained so existing config files keep their shape, but it no longer has any effect:
            // the damping it used to drive wrote BufferedCameraFollow.aimSensitivity3P, which is the
            // commander's third-person turn rate, not a CITV-only value - softening it slowed the
            // commander's traverse to a quarter of vanilla. The CITV no longer touches that field.
            citv_smooth = Entry<bool>(cfg, "Smooth CITV Panning (Global)", true);
            citv_smooth.Comment = "No longer has any effect; retained for compatibility.";

            du_package_m1ip = Entry<bool>(cfg, "DU Armour (M1A1)", false);
            du_package_m1ip.Description = "DU inserts for the composite turret cheeks: increased KE protection. M1A1/M1IP exclusive. Increased weight.";
            du_gen_m1ip = Entry<int>(cfg, "DU Generation (M1A1)", 1);
            du_gen_m1ip.Comment = "Higher generation = more KE protection (1-3, integer)";

            du_package_m1 = Entry<bool>(cfg, "DU Armour (M1E1)", false);
            du_gen_m1 = Entry<int>(cfg, "DU Generation (M1E1)", 1);
            du_package_m1.Comment = "Doesn't apply to M1/M1E1, only those converted to M1A1/M1IP";

            m1_to_m1ip = Entry<bool>(cfg, "M1 -> M1IP", false);
            m1_to_m1ip.Description = "Convert all M1s to M1IPs (will still use M1E1 settings)";

            m1e1_ip_model = Entry<bool>(cfg, "M1E1 IP Model", false);
            m1e1_ip_model.Description = "Gives the M1IP model to base M1";

            random_conversion = Entry<bool>(cfg, "Random", true);
            random_conversion.Description = "M1IPs/M1s will have a random chance of being converted to M1A1s/M1E1s.";

            conversion_chance = Entry<int>(cfg, "ConversionChance", 100);

            m829_spall = Entry<bool>(cfg, "M829 Spall+", false);
            m829_spall.Description = "Enhanced spalling for all M829s.";

            // Keep installation switches compatible; migrate the old ammo bool below.
            crows_m1e1 = Entry<bool>(cfg, "CROWS (M1E1)", false);
            crows_m1e1.Description = "Replaces the commander's exposed .50 cal with the M153 CROWS II remote weapon station.";
            crows_m1a1 = Entry<bool>(cfg, "CROWS (M1A1)", false);

            // Saved new selections take precedence over old boolean switches.
            string legacyAmmo = LegacyFlagInFile("Use Mk 211 Mod 0")
                || LegacyFlagInFile("CROWS Raufoss") || LegacyFlagInFile("CROWS: Raufoss Ammo (Global)")
                ? CrowsAmmoRules.Mk211
                : LegacyFlagInFile("M2 SLAP") ? CrowsAmmoRules.M903 : CrowsAmmoRules.M8;
            crows_ammo = Entry<string>(cfg, "CROWS/M2 Ammo", legacyAmmo);

            crows_mk19 = Entry<bool>(cfg, "CROWS/MK19 Instead Of M2", false);

            crows_mk19.Description = "Mounts an Mk 19 grenade launcher on the CROWS instead of the .50 cal. The coaxial M2 is not affected.";

            crows_mk19_ammo = Entry<string>(cfg, "CROWS/MK19 Ammo", Mk19Rules.M430);

            crows_mk19_ammo.Comment = "M430 (51 mm RHA, 32 g Comp A5), M430A1 (76 mm RHA, 39 g Comp A5). Only used when the CROWS carries the Mk 19.";
            crows_ammo.Comment = "M8 API, Mk 211 Mod 0, M903 SLAP (4 M903 + 1 M962 SLAP-T). Shared with M2 Coax; reload mission after changing.";
            cfg.DeleteEntry("Use Mk 211 Mod 0");
            cfg.DeleteEntry("CROWS Raufoss");
            cfg.DeleteEntry("CROWS: Raufoss Ammo (Global)");
            cfg.DeleteEntry("M2 SLAP");
            m2_coax = Entry<bool>(cfg, "M2 Coax", false);
            m2_coax.Comment = "Replace the coaxial machine gun with an M2HB and bind it to the CROWS/M2 ammunition selection.";

            crows_alt_placement = Entry<bool>(cfg, "Alternative Position", false);
            crows_alt_placement.Comment = "Moves the CROWS to the right side of the commander instead of directly in front.";

            // The station's manual slew - traverse and elevation with nothing locked, per magnification -
            // is not a setting. It is fixed in CrowsAimLock: the rated 90/50 at 1X, scaled by the sight's
            // field of view above it so the four magnifications move the picture at the same speed.

            // Keys that this build does not declare itself are not exposed as entries by
            // MelonLoader, so the ones from other builds are read straight out of the file.
            AdoptLegacyFlag(crows_m1e1, "CROWS (M1E1) <DISABLED>");
            AdoptLegacyFlag(crows_m1a1, "CROWS (M1A1) <DISABLED>");
            AdoptLegacyFlag(crows_alt_placement, "CROWS Alt Placement", "CROWS: Alternate Placement (Global)");


            if (use_preset_pool.Value)
            {
                preset_manager_m1 = new PresetManager<M1Preset>(target_preset_m1.Value);
                preset_manager_m1ip = new PresetManager<M1Preset>(target_preset_m1ip.Value);
            }
        }

        /// <summary>
        /// Reads a CROWS switch left behind by another build of this mod and turns the current
        /// entry on when that old key was enabled.
        ///
        /// MelonLoader keeps keys it does not know about in MelonPreferences.cfg but does not
        /// expose them as entries (HasEntry returns false), so the file itself is parsed.
        /// The old keys are never modified, which makes this idempotent and reversible.
        /// </summary>
        private static void AdoptLegacyFlag(MelonPreferences_Entry<bool> target, params string[] legacy_names)
        {
            if (target == null || target.Value)
            {
                return;
            }

            foreach (string legacy_name in legacy_names)
            {
                try
                {
                    if (!LegacyFlagInFile(legacy_name))
                    {
                        continue;
                    }

                    target.Value = true;
                    return;
                }
                catch (System.Exception)
                {
                    // A leftover key must never stop the mod from loading.
                }
            }
        }

        private static bool LegacyFlagInFile(string name)
        {
            string path = System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "MelonPreferences.cfg");

            if (!System.IO.File.Exists(path))
            {
                return false;
            }

            bool in_category = false;

            foreach (string raw_line in System.IO.File.ReadAllLines(path))
            {
                string line = raw_line.Trim();

                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith("["))
                {
                    in_category = line == "[M1A1Config]";
                    continue;
                }

                if (!in_category)
                {
                    continue;
                }

                int separator = line.IndexOf('=');

                if (separator < 0)
                {
                    continue;
                }

                string key = line.Substring(0, separator).Trim().Trim('"');

                if (!string.Equals(key, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string value = line.Substring(separator + 1).Trim();
                int comment = value.IndexOf('#');

                if (comment >= 0)
                {
                    value = value.Substring(0, comment).Trim();
                }

                return value.Equals("true", System.StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static readonly System.Collections.Generic.HashSet<int> converted_vehicle_ids = new System.Collections.Generic.HashSet<int>();

        private static T FindSpawnMarker<T>(Vehicle vic) where T : Component
        {
            if (vic == null)
            {
                return null;
            }

            T marker = vic.GetComponent<T>() ?? vic.GetComponentInParent<T>();
            return marker ?? vic.GetComponentInChildren<T>(true);
        }

        internal static void ResetConversionState()
        {
            converted_vehicle_ids.Clear();
            CrowsAmmo.Reset();
            Mk19.Reset();
            AmpSmoke.Reset();
        }

        /// <summary>
        /// The Abrams prefab already contains an upright two-antenna decoration. Select that
        /// configuration at runtime when CROWS is installed so the rear whips do not intersect
        /// the station. This deliberately changes only the spawned vehicle instance; the unpacked
        /// prefab remains untouched and no per-frame component or static reference is added.
        /// </summary>
        private static void RaiseAbramsAntennas(Vehicle vic)
        {
            if (vic == null)
            {
                return;
            }

            Transform antennas = vic.transform.Find("IPM1_rig/HULL/TURRET/Turret Scripts/antennas")
                ?? vic.transform.Find("IPM1_rig/HULL/TURRET/antennas");

            if (antennas == null)
            {
                return;
            }

            for (int i = 0; i < antennas.childCount; i++)
            {
                Transform configuration = antennas.GetChild(i);
                bool upright = configuration.name == "both untied";

                if (upright)
                {
                    configuration.gameObject.SetActive(true);
                }
                else if (configuration.name == "both tied" || configuration.name == "left tied right not")
                {
                    configuration.gameObject.SetActive(false);
                }
            }
        }

        private static bool IsConvertibleAbrams(Vehicle vic, out string reason)
        {
            reason = null;

            string friendly = vic.FriendlyName == null ? "" : vic.FriendlyName;
            string unique = vic.UniqueName == null ? "" : vic.UniqueName;

            bool named = friendly.Contains("M1IP") || friendly.Contains("M1 Abrams")
                || unique == "M1" || unique == "M1IP Abrams";

            if (!named)
            {
                reason = "neither an M1 nor an M1IP (friendly '" + friendly + "', unique '" + unique + "')";
                return false;
            }

            // A name is not a hull. Everything below reaches into the Abrams hierarchy, so require
            // it to be there before touching a single component.
            if (vic.transform.Find("IPM1_rig/HULL/TURRET") == null)
            {
                reason = "named like an Abrams (friendly '" + friendly + "') but has no IPM1_rig/HULL/TURRET";
                return false;
            }

            return true;
        }

        private static void HandleConversion(Vehicle vic)
        {
            if (vic == null) return;
            int vehicle_id = vic.GetInstanceID();
            if (!converted_vehicle_ids.Add(vehicle_id)) return;

            // Guard against repeated PlayerReady callbacks converting one vehicle twice.
            if (vic.GetComponentInChildren<CrowsAutoTrack>(true) != null) return;

            GameObject vic_go = vic.gameObject;

            string skip_reason;

            if (!IsConvertibleAbrams(vic, out skip_reason))
            {
                return;
            }

            // Upstream "Random" / "ConversionChance": a convertible Abrams may be left as it is, so a
            // mission can mix stock M1IPs and M1s in with the converted ones. The roll is only taken
            // once per vehicle because converted_vehicle_ids already guards re-entry.
            int roll = random_conversion.Value ? UnityEngine.Random.Range(1, 100) : 0;
            if (roll > conversion_chance.Value) return;

            // A converted M1 uses the M1IP prefab, but it remains an M1E1 for configuration.
            // UnitSpawner may put its spawn marker on the unit root instead of the Vehicle object.
            bool is_m1ip = vic.UniqueName == "M1IP Abrams" && FindSpawnMarker<PreviouslyM1>(vic) == null;
            // Chassis upgrades do not depend on sight, model or ammunition assets.
            AmpFeatures.ApplyMobility(vic, is_m1ip);

            WeaponsManager weapons_manager = vic.GetComponent<WeaponsManager>();
            WeaponSystemInfo main_gun_info = weapons_manager.Weapons[0];
            WeaponSystem main_gun = main_gun_info.Weapon;
            UsableOptic optic = vic.transform.Find("IPM1_rig/HULL/TURRET/Turret Scripts/GPS/Optic").GetComponent<UsableOptic>();
            UsableOptic night_optic = optic.slot.LinkedNightSight.PairedOptic;
            Transform gas = vic.transform.Find("IPM1_rig/HULL/TURRET/GUN/Gun Scripts/Aux sight (GAS)");

            optic.slot.ExclusiveWeapons = new WeaponSystem[] { weapons_manager.Weapons[0].Weapon, weapons_manager.Weapons[1].Weapon };
            night_optic.slot.ExclusiveWeapons = new WeaponSystem[] { weapons_manager.Weapons[0].Weapon, weapons_manager.Weapons[1].Weapon };

            MPATManager mpat_manager = vic_go.AddComponent<MPATManager>();
            mpat_manager.AmmoCachedIdx = Ammo_120mm.ammo_m830a1.CachedIndex;
            vic_go.AddComponent<AmpProxyManager>();

            var playerUnit = PlayerInput.Instance?.CurrentPlayerUnit;
            bool player_controlled = playerUnit != null && vic.gameObject == playerUnit.gameObject;

            int cfg_flir_gen = is_m1ip ? flir_gen_m1ip.Value : flir_gen_m1.Value;
            bool cfg_du_package = is_m1ip ? du_package_m1ip.Value : du_package_m1.Value;
            int cfg_du_gen = is_m1ip ? du_gen_m1ip.Value : du_gen_m1.Value;
            bool cfg_rotate_azimuth = is_m1ip ? rotate_azimuth_m1ip.Value : rotate_azimuth_m1.Value;
            bool cfg_citv = is_m1ip ? citv_m1a1.Value : citv_m1e1.Value;
            bool cfg_m256 = is_m1ip ? has_m256_m1ip.Value : has_m256_m1.Value;
            bool cfg_vanilla_ammo_cap = is_m1ip ? original_rack_size_m1ip.Value : original_rack_size_m1.Value;
            int cfg_m829_count = is_m1ip ? m829_count_m1ip.Value : m829_count_m1.Value;
            int cfg_m830_count = is_m1ip ? m830_count_m1ip.Value : m830_count_m1.Value;
            string cfg_ap_type = is_m1ip ? sabot_m1ip.Value : sabot_m1.Value;
            string cfg_heat_type = is_m1ip ? heat_m1ip.Value : heat_m1.Value;
            string cfg_round3_type = is_m1ip ? amp_round3_m1ip.Value : amp_round3_m1.Value;
            string cfg_round4_type = is_m1ip ? amp_round4_m1ip.Value : amp_round4_m1.Value;
            int cfg_round3_count = is_m1ip ? amp_count3_m1ip.Value : amp_count3_m1.Value;
            int cfg_round4_count = is_m1ip ? amp_count4_m1ip.Value : amp_count4_m1.Value;
            string cfg_armor = AmpUpgrades.Armor(is_m1ip);

            if (use_preset_pool.Value)
            {
                PresetManager<M1Preset> preset_manager = is_m1ip ? preset_manager_m1ip : preset_manager_m1;
                M1Preset preset = preset_manager == null ? null : preset_manager.ChoosePreset();
                PresetMarker marker = FindSpawnMarker<PresetMarker>(vic);

                if (marker != null)
                {
                    preset = (M1Preset)marker.Preset;
                }

                if (preset_manager != null && preset_manager.HasPlayerReservedPreset && player_controlled)
                {
                    preset = preset_manager.PlayerReservedPreset;
                }

                if (preset != null)
                {
                    cfg_flir_gen = preset.FLIRGeneration;
                    cfg_du_gen = preset.DUGeneration;
                    cfg_du_package = preset.DUArmour;
                    cfg_citv = preset.CITV;
                    cfg_m256 = preset.M256Gun;
                    cfg_vanilla_ammo_cap = preset.VanillaAmmoCapacity;
                    cfg_m829_count = preset.APCount;
                    cfg_m830_count = preset.HEATCount;
                    cfg_ap_type = preset.APRound;
                    cfg_heat_type = preset.HEATRound;
                    cfg_rotate_azimuth = preset.RotateAzimuth;
                }

                Component.Destroy(marker);
            }

            // Resolve Legacy against the DU package actually applied below, including presets.
            string designationArmor = cfg_armor == "LEGACY"
                ? (cfg_du_package && vic.UniqueName == "M1IP Abrams" ? (cfg_du_gen > 1 ? "HC" : "HA") : "")
                : cfg_armor;

            if (cfg_flir_gen > 1)
            {
                Vector2Int resolution = cfg_flir_gen == 2 ? flir_gen2_res : flir_gen3_res;
                night_optic.slot.VibrationShakeMultiplier = 0.0f;
                night_optic.slot.FLIRWidth = resolution.x;
                night_optic.slot.FLIRHeight = resolution.y;
                night_optic.slot.FLIRBlitMaterialOverride = Assets.flir_blit_mat_green_no_scan;
            }

            // AMP owns the zoom list. Legacy Digital Enhancement must not append extra levels.
            AmpOptics.ApplyVehicle(vic);

            if (cfg_armor == "LEGACY" && cfg_du_package && vic.UniqueName == "M1IP Abrams")
            {
                vic_go.GetComponent<Rigidbody>().mass = 62781.3776f;

                GameObject turret_cheeks = vic.transform.Find("IPM1_rig/HULL/TURRET").GetComponent<LateFollowTarget>()
                    .LateFollowers[0].transform.Find("Turret_Armor/cheeks composite arrays").gameObject;

                VariableArmor var_armour = turret_cheeks.GetComponent<VariableArmor>();
                int duIndex = Mathf.Clamp(cfg_du_gen - 1, 0, DUArmour.du_armor_codexes.Length - 1);
                Reflect.Set(var_armour, "_armorType", DUArmour.du_armor_codexes[duIndex]);

                AarVisual cheek_visual = turret_cheeks.GetComponent<AarVisual>();

                cheek_visual.AarMaterial = DUArmour.du_aar_mats[duIndex];
            }

            AmpArmor.Apply(vic, cfg_armor);

            if (cfg_rotate_azimuth)
            {
                optic.RotateAzimuth = true;
                optic.slot.LinkedNightSight.PairedOptic.RotateAzimuth = true;
                optic.slot.VibrationShakeMultiplier = 0f;
                optic.slot.VibrationPreBlur = false;
                optic.Alignment = OpticAlignment.BoresightStabilized;
                optic.slot.LinkedNightSight.PairedOptic.Alignment = OpticAlignment.BoresightStabilized;
            }

            if (cfg_citv)
            {
                GameObject c = GameObject.Instantiate(Assets.citv_obj, vic.transform.Find("IPM1_rig/HULL/TURRET"));
                c.transform.localPosition = new Vector3(-0.6794f, 0.9341f, 0.4348f);
                c.transform.localEulerAngles = new Vector3(0f, 0f, 0f);

                c.transform.SetParent(vic.transform.Find("IPM1_rig/HULL/TURRET").GetComponent<LateFollowTarget>()
                    .LateFollowers[0].transform, true);

                CITV citv_component = vic.DesignatedCameraSlots[0].LinkedNightSight.gameObject.AddComponent<CITV>();
                citv_component.model = c;

                if (player_controlled && CameraManager.Instance != null && CameraSlot.ActiveInstance != null)
                {
                    CameraManager.Instance.UpdateLightMode(mode: NightVisionType.Thermal);
                    Reflect.Call(CameraSlot.ActiveInstance, "ThisActiveChanged", true);
                }

                c.transform.Find("assembly").GetComponent<UniformArmor>().Unit = vic;
                c.transform.Find("glass").GetComponent<UniformArmor>().Unit = vic;

                TargetSpotterSettings spotter = vic.TargetSpotterSettings;
                Reflect.Set(spotter, "_periscopeFOV", 120f);
                Reflect.Set(spotter, "_sightDistance", 4500f);
                Reflect.Set(spotter, "_nightSightDistanceIdeal", 4500f);
                Reflect.Set(spotter, "_nightSightDistancePassive", 4500f);

                if (vic.UniqueName == "M1IP Abrams")
                {
                    vic_go.transform.Find("IPM1_rig/HULL/TURRET/sparewheel_roof").gameObject.SetActive(false);
                } 
                else
                { 
                    vic_go.transform.Find("IPM1_rig/HULL/TURRET/M1A0 sparewheel_roof").gameObject.SetActive(false);
                }
            }

            if (cfg_m256)
            {
                main_gun_info.Name = "120mm gun M256";
                main_gun.Impulse = 68000;
                Reflect.Set(main_gun, "CodexEntry", gun_m256);
                main_gun.WeaponSound.SingleShotEventPaths[0] = "event:/Weapons/canon_120mm-RH120";

                GameObject dummy_tube = new GameObject("dummy tube");
                dummy_tube.transform.parent = vic_go.transform.Find("IPM1_rig/HULL/TURRET/GUN");
                dummy_tube.transform.localScale = new Vector3(0f, 0f, 0f);

                Transform smr_path = (vic.UniqueName == "M1") ? vic.transform.Find("M1A0_mesh/M1A0 turret") : vic.transform.Find("M1IP_mesh/M1IP_turret");
                int gun_recoil_idx = (vic.UniqueName == "M1") ? 13 : 9;
                SkinnedMeshRenderer smr = smr_path.GetComponent<SkinnedMeshRenderer>();
                Transform[] bones = smr.bones;
                bones[gun_recoil_idx] = dummy_tube.transform;
                smr.bones = bones;

                GameObject gunTube = vic_go.transform.Find("IPM1_rig/HULL/TURRET/GUN/gun_recoil").gameObject;
                gunTube.transform.Find("GUN/Gun Breech.001").GetComponent<MeshRenderer>().enabled = false;

                GameObject _m256_obj = GameObject.Instantiate(Assets.m256_obj, gunTube.transform);
                _m256_obj.transform.localPosition = new Vector3(0f, 0.0064f, -1.9416f);

                Transform muzzleFlashes = main_gun.MuzzleEffects[1].transform;
                muzzleFlashes.GetChild(1).transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                muzzleFlashes.GetChild(2).transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                muzzleFlashes.GetChild(4).transform.localScale = new Vector3(1.3f, 1.3f, 1f);

                // convert ammo
                LoadoutManager loadoutManager = vic.GetComponent<LoadoutManager>();
                AmmoClipCodexScriptable clip1 = AmpFeatures.Clip(cfg_ap_type, is_m1ip ? "M829" : "M827");
                AmmoClipCodexScriptable clip2 = AmpFeatures.Clip(cfg_heat_type, "M830");
                AmmoClipCodexScriptable clip3 = AmpFeatures.Clip(cfg_round3_type, "M830A1");
                AmmoClipCodexScriptable clip4 = AmpFeatures.Clip(cfg_round4_type, "M830A1");
                MainGunLoadout.Apply(loadoutManager, new[] { clip1, clip2, clip3, clip4 },
                    new[] { Math.Max(1, cfg_m829_count), cfg_m830_count, cfg_round3_count, cfg_round4_count },
                    cfg_vanilla_ammo_cap);
                MainGunLoadout.Refill(main_gun.Feed);
                MainGunLoadout.Verify(loadoutManager, main_gun.Feed);
                Reflect.Call(loadoutManager, "RefreshSnapshot");
                Reflect.Call(loadoutManager, "RegisterAllBallistics");

                // Hiding the 105 mm tube and the camo-net mount is cosmetic, but the lookups
                // used to throw on a hull without those nodes and take the CROWS mount, the
                // designation and everything after this block down with them.
                Transform turret_gun = vic_go.transform.Find("IPM1_rig/HULL/TURRET/GUN/turret_gun");
                if (turret_gun != null)
                {
                    turret_gun.gameObject.SetActive(false);
                }
                else
                {
                    MelonLogger.Warning("[M1A1] this hull has no 'GUN/turret_gun' node to hide.");
                }

                if (vic.UniqueName == "M1IP Abrams")
                {
                    Transform camo_net_gun = vic_go.transform.Find("IPM1_rig/HULL/TURRET/M1 camo net/turret_gun");
                    if (camo_net_gun != null)
                    {
                        camo_net_gun.gameObject.SetActive(false);
                    }
                    else
                    {
                        MelonLogger.Warning("[M1A1] this hull has no 'M1 camo net/turret_gun' node to hide.");
                    }
                }

                // The auxiliary sight is cosmetic next to the smoke launchers, the crew
                // upgrades and the CROWS mount that run after this block. Keep a failure here
                // from aborting the rest of the conversion.
                try
                {
                    AmmoCodexScriptable[] gas_rounds =
                    {
                        clip1.ClipType.MinimalPattern[0],
                        clip2.ClipType.MinimalPattern[0],
                        clip3.ClipType.MinimalPattern[0],
                        clip4.ClipType.MinimalPattern[0]
                    };
                    GAS.ReticleSet gas_reticles = GAS.Create(gas_rounds);
                    GAS.Add(gas, optic.slot.ExclusiveWeapons);
                    // One reticle tree per round: ReticleTree.VerticalBallistic bakes each range
                    // mark into the mesh through the tree's own projectile, so the sight has to
                    // swap trees with the round in the breech rather than reusing the 105mm ladder.
                    GAS.AttachAuxFix(gas, main_gun, gas_reticles);
                }
                catch (System.Exception error)
                {
                    MelonLogger.Error("[M1A1] GAS auxiliary sight failed; continuing conversion: " + error);
                }

            }

            try
            {
                AmpFeatures.Apply(vic, main_gun, is_m1ip, cfg_m256);
            }
            catch (System.Exception error)
            {
                MelonLogger.Error("[M1A1] Feature setup failed; continuing conversion: " + error);
            }
            // The coax belt is a nice-to-have next to the CROWS mount that follows; an
            // unexpected vehicle layout must not abort the rest of the conversion.
            try
            {
                CrowsAmmo.ConfigureVehicleCoax(vic);
            }
            catch (System.Exception error)
            {
                MelonLogger.Error("[M1A1] coax belt setup failed; continuing conversion: " + error);
            }

            bool cfg_crows = is_m1ip ? crows_m1a1.Value : crows_m1e1.Value;

            if (cfg_crows)
            {
                Vector3 crows_pos = crows_alt_placement.Value
                    ? new Vector3(1.4f, 1.1164f, -0.5873f)
                    : new Vector3(0.7855f, 1.2855f, 0.5182f);

                if (CROWS.Mount(vic, vic.transform.Find("IPM1_rig/HULL/TURRET"), crows_pos, crows_alt_placement.Value, night_optic))
                {
                    RaiseAbramsAntennas(vic);

                    // The original M48 cupola gun is skinned geometry bound to a bone, so zeroing the
                    // bone's scale is what actually hides it. The rig carries more than one copy of
                    // that bone (the mission rig, the M1IP_mesh hierarchy and a nested TURRET/TURRET
                    // branch), and which copy drives the visible mesh differs between variants, so
                    // every CUPOLA_GUN under the vehicle is scaled down, and the renderers hanging off
                    // one are switched off too in case they are not skinned to the bone.
                    Transform[] rig_nodes = vic.GetComponentsInChildren<Transform>(true);

                    for (int i = 0; i < rig_nodes.Length; i++)
                    {
                        if (rig_nodes[i].name != "CUPOLA_GUN")
                        {
                            continue;
                        }

                        rig_nodes[i].localScale = Vector3.zero;

                        Renderer[] gun_parts = rig_nodes[i].GetComponentsInChildren<Renderer>(true);

                        for (int j = 0; j < gun_parts.Length; j++)
                        {
                            gun_parts[j].enabled = false;
                        }
                    }
                }
            }

            string designation = cfg_m256
                ? AmpDesignation.Name(is_m1ip, cfg_citv, designationArmor)
                : vic.FriendlyName + designationArmor;
            AmpDesignation.Bind(vic, designation);
        }

        public static IEnumerator Convert(GameState _)
        {
            // Nothing below runs until GHPC's instanced-geometry pipeline is up and serving cameras.
            //
            // Building the station instantiates and destroys renderers - the donors it borrows from,
            // the borrowed parts it switches on, the two donor components it deletes - and loading the
            // donors does the same. GHPC is collecting its instanced geometry on worker threads at the
            // same moment, and that collection has a deadline. Miss it and the renderer prints
            //
            //   Timeout waiting for instance collection threads to complete! Not processing any
            //   instance logic for this collection.
            //
            // and stops submitting batches altogether - "Thread wait timed out. Not submitting instance
            // batches for this frame." - which is the whole map's vegetation gone, not just the sight's.
            // No amount of downstream repair can put back a collection that never finished, so the
            // station waits for the pipeline to report itself healthy before it starts instantiating
            // anything, with a hard ceiling so a pipeline that never reports cannot hold the station
            // back. This is the one failure of the two that has to be prevented rather than
            // compensated: CrowsInstancingAssist can submit batches that the renderer dropped, but it
            // cannot revive a dead collection thread.
            const int MAX_FRAMES = 180;

            for (int frame = 0; frame < MAX_FRAMES; frame++)
            {
                // A pipeline this assembly cannot read is not a pipeline to wait for: if the
                // reflection ever fails to find the renderer, the wait is skipped rather than paid in
                // full on every mission.
                if (!CrowsInstancing.Usable || CrowsInstancing.PipelineServing())
                {
                    break;
                }

                yield return null;
            }

            // Donors (M60A3 cupola parts, T55A reticle tree, Bradley GPS canvas) and the
            // game's .50 cal ammo entries all exist by the time the mission is playable.
            try { CROWS.Init(); }
            catch (System.Exception error)
            {
                MelonLogger.Error("[M1A1] CROWS assets failed; continuing vehicle upgrades: " + error);
            }

            AmpAmmo.RefreshEraOptimizations();

            foreach (Vehicle vic in M1A1AbramsMod.vics ?? new Vehicle[0])
            {
                try { HandleConversion(vic); }
                catch (System.Exception error)
                {
                    MelonLogger.Error("[M1A1] Conversion incomplete for "
                        + (vic == null ? "missing vehicle" : vic.name + " / " + vic.UniqueName) + ": " + error);
                }
            }

            yield break;
        }

        public static void Init()
        {            
            if (gun_m256 == null)
            {
                gun_m256 = ScriptableObject.CreateInstance<WeaponSystemCodexScriptable>();
                gun_m256.name = "gun_m256";
                gun_m256.CaliberMm = 120;
                gun_m256.FriendlyName = "120mm Gun M256";
                gun_m256.Type = WeaponSystemCodexScriptable.WeaponType.LargeCannon;
            }

            StateController.RunOrDefer(GameState.PlayerReady, new GameStateEventHandler(Convert), GameStatePriority.Medium);
        }
    }
}






