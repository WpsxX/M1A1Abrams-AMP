using MelonLoader;

namespace M1A1Abrams
{
    internal static class AmpUpgrades
    {
        internal static MelonPreferences_Entry<string> ArmorM1A1, ArmorM1E1;
        internal static MelonPreferences_Entry<string> GunnerM1A1, GunnerM1E1, LoaderM1A1, LoaderM1E1, CommanderM1A1, CommanderM1E1;
        internal static MelonPreferences_Entry<bool> Uap, Demigod, Traverse;
        internal static MelonPreferences_Entry<bool> NoBlurCitv, CitvReticle, PerfectCitvOverride;
        internal static MelonPreferences_Entry<bool> SmokeM1A1, SmokeM1E1, RosyM1A1, RosyM1E1;

        internal static void Config(MelonPreferences_Category cfg)
        {
            ArmorM1A1 = Prefs.Entry(cfg, "M1A1 Armor", "Legacy");
            ArmorM1E1 = Prefs.Entry(cfg, "M1E1 Armor", "Legacy");
            ArmorM1A1.Comment = ArmorM1E1.Comment = "HA / HC / SA / HU / L; blank = base armor; Legacy = existing DU/preset settings. Explicit choice overrides DU.";
            Uap = Prefs.Entry(cfg, "UAP", false);
            Uap.Comment = "HU only: use HA package mass (+5261 kg) instead of HU (+17327 kg).";
            Demigod = Prefs.Entry(cfg, "Demigod Armor", false);
            Demigod.Comment = "HU only: AMP's extreme armor values; not unconditional invulnerability.";
            Traverse = Prefs.Entry(cfg, "Traverse+", false);
            Traverse.Comment = "AMP rule: 60 deg/s main turret with APU and engine power >3300 kW (T64/T55).";
            // Preserve the maintained test2 CITV defaults while exposing AMP's controls.
            NoBlurCitv = Prefs.Entry(cfg, "No Blur CITV", true);
            CitvReticle = Prefs.Entry(cfg, "CITV Reticle", true);
            PerfectCitvOverride = Prefs.Entry(cfg, "Perfect CITV Override", true);
            PerfectCitvOverride.Comment = "While using CITV, hold Smooth Aim to slave the current weapon to the sight point.";
            GunnerM1A1 = Skill(cfg, "M1A1 Gunner"); GunnerM1E1 = Skill(cfg, "M1E1 Gunner");
            LoaderM1A1 = Skill(cfg, "M1A1 Loader"); LoaderM1E1 = Skill(cfg, "M1E1 Loader");
            CommanderM1A1 = Skill(cfg, "M1A1 Commander"); CommanderM1E1 = Skill(cfg, "M1E1 Commander");

            // CreateEntry loads an explicit existing per-model value in preference to this
            // legacy default. A disabled model must stay disabled even if ROSY+ is true.
            bool legacyPlus = AmpFeatures.Rosy.Value;
            bool legacySmoke = AmpFeatures.BetterSmoke.Value || legacyPlus;
            SmokeM1A1 = Prefs.Entry(cfg, "M1A1 Smoke+", legacySmoke);
            SmokeM1E1 = Prefs.Entry(cfg, "M1E1 Smoke+", legacySmoke);
            RosyM1A1 = Prefs.Entry(cfg, "M1A1 ROSY", legacyPlus);
            RosyM1E1 = Prefs.Entry(cfg, "M1E1 ROSY", legacyPlus);
            SmokeM1A1.Comment = SmokeM1E1.Comment = "Per-model enhanced multispectral smoke. Required for ROSY.";
            RosyM1A1.Comment = RosyM1E1.Comment = "Requires this model's Smoke+. ROSY+ selects the faster/larger variant.";
            AmpFeatures.BetterSmoke.Comment = "Legacy migration default only. Use M1A1 Smoke+ / M1E1 Smoke+.";
            AmpFeatures.Rosy.Comment = "Upgrade enabled per-model ROSY to fast/large ROSY+. Does not enable smoke on disabled models.";
        }

        private static MelonPreferences_Entry<string> Skill(MelonPreferences_Category cfg, string key)
        {
            var entry = Prefs.Entry(cfg, key, "Regular");
            entry.Comment = "Cadet / Regular / Veteran / Ace; applied on mission load.";
            return entry;
        }

        internal static string Armor(bool m1a1)
        {
            return AmpUpgradeRules.Armor((m1a1 ? ArmorM1A1 : ArmorM1E1).Value);
        }

        internal static AmpSmokeMode Smoke(bool m1a1)
        {
            return AmpUpgradeRules.SmokeMode((m1a1 ? SmokeM1A1 : SmokeM1E1).Value,
                (m1a1 ? RosyM1A1 : RosyM1E1).Value, AmpFeatures.Rosy.Value);
        }
    }
}
