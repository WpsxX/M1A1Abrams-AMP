using System;
using System.Collections.Generic;

namespace M1A1Abrams
{
    internal enum AmpSmokeMode { Vanilla, Smoke, Rosy, RosyPlus }

    internal static class AmpUpgradeRules
    {
        internal static string Armor(string value)
        {
            string name = (value ?? "").Trim().ToUpperInvariant();
            switch (name)
            {
                case "": case "HA": case "HC": case "SA": case "HU": case "L": return name;
                default: return "LEGACY";
            }
        }

        internal static int Skill(string value)
        {
            switch ((value ?? "").Trim().ToUpperInvariant())
            {
                case "CADET": return 0;
                case "VETERAN": return 2;
                case "ACE": return 3;
                default: return 1;
            }
        }

        internal static AmpSmokeMode SmokeMode(bool smoke, bool rosy, bool plus)
        {
            if (!smoke) return AmpSmokeMode.Vanilla;
            return !rosy ? AmpSmokeMode.Smoke : (plus ? AmpSmokeMode.RosyPlus : AmpSmokeMode.Rosy);
        }

        internal static float AddedMass(string armor, bool uap)
        {
            switch (armor)
            {
                case "HA": return 5261f;
                case "HC": return 6078f;
                case "SA": return 6894f;
                case "HU": return uap ? 5261f : 17327f;
                case "L": return -10500f;
                default: return 0f;
            }
        }

        // AMP enabled Traverse+ only with an APU and a T64/T55-class engine.
        internal static bool Traverse(bool enabled, bool apu, float power)
        {
            return enabled && apu && power > 3300f;
        }

        internal struct Resistance
        {
            internal float CE, KE;
            internal Resistance(float ce, float ke) { CE = ce; KE = ke; }
        }

        private static readonly Dictionary<string, Resistance> hu =
            new Dictionary<string, Resistance>(StringComparer.OrdinalIgnoreCase)
        {
            { "composite side skirt", new Resistance(11f, 6f) },
            { "turret cheek special armor array", new Resistance(2.2f, 1.25f) },
            { "hull front special armor array", new Resistance(2.2f, 1.2f) },
            { "gun mantlet special armor array", new Resistance(2.15f, 1.65f) },
            { "turret side special armor array", new Resistance(4.4f, 2.21f) },
            { "turret roof", new Resistance(7f, 5.9f) },
            { "upper glacis", new Resistance(5.5f, 4.7f) },
            { "commander's hatch", new Resistance(3.6f, 3f) },
            { "loader's hatch", new Resistance(3.6f, 3f) },
            { "driver's hatch", new Resistance(3.6f, 3f) },
            { "turret ring", new Resistance(5f, 5f) },
            { "gun mantlet face", new Resistance(23f, 18f) },
            { "left track", new Resistance(10f, 10f) },
            { "right track", new Resistance(10f, 10f) },
            { "main gun barrel", new Resistance(7f, 7f) },
            { "bustle racks firewall", new Resistance(2.667f, 2.667f) },
            { "center blowout panel", new Resistance(18f, 15f) },
            { "left blowout panel", new Resistance(18f, 15f) },
            { "right blowout panel", new Resistance(18f, 15f) },
            { "turret rear face", new Resistance(40f, 20f) },
            { "GPS doghouse", new Resistance(5f, 5f) },
            { "GPS doghouse door", new Resistance(5f, 5f) },
            { "turret bottom", new Resistance(14f, 12f) },
            { "trunnion", new Resistance(1.5f, 1.5f) },
            { "semi-ready rack door", new Resistance(5f, 5f) },
            { "ready rack door", new Resistance(5f, 5f) },
            { "lower front plate", new Resistance(3.333f, 3.333f) },
            { "hull front backing plate", new Resistance(2.5f, 2.5f) },
            { "turret cheek face", new Resistance(1.6f, 1.6f) },
            { "turret cheek backing plate", new Resistance(3f, 3f) },
            { "turret side backing plate", new Resistance(3f, 3f) },
            { "turret side face", new Resistance(2.667f, 2.667f) },
        };

        private static readonly Dictionary<string, Resistance> uniformHu =
            new Dictionary<string, Resistance>(StringComparer.OrdinalIgnoreCase)
        {
            { "hull side", new Resistance(300, 200) },
            { "hull rear", new Resistance(300, 200) },
            { "hull floor", new Resistance(300, 200) },
            { "hull floor rear", new Resistance(300, 200) },
            { "hull roof", new Resistance(300, 200) },
            { "firewall", new Resistance(80, 80) },
            { "hull ammo rack door", new Resistance(100, 100) },
            { "engine deck", new Resistance(350, 250) },
            { "fender", new Resistance(100, 50) },
            { "side skirt", new Resistance(550, 300) },
            { "gunner's primary sight", new Resistance(50, 50) },
            { "sprocket wheel", new Resistance(50, 50) },
            { "road wheel", new Resistance(50, 50) },
            { "engine", new Resistance(150, 150) }
        };

        internal static bool Uniform(string armor, string part, bool demigod, out Resistance value)
        {
            value = default(Resistance);
            if (armor != "HU" || !uniformHu.TryGetValue(part ?? "", out value)) return false;
            if (demigod) value = new Resistance(10000f, 10000f);
            return true;
        }

        internal static bool Variable(string armor, string part, bool demigod, out Resistance value)
        {
            value = default(Resistance);
            part = (part ?? "").Trim().ToLowerInvariant();
            if (armor == "HU")
            {
                if (!hu.TryGetValue(part, out value)) return false;
                // The original AMP leaves the driver's hatch at its ordinary HU values.
                if (demigod && part != "driver's hatch") value = new Resistance(100f, 100f);
                return true;
            }
            if (armor != "HA" && armor != "HC" && armor != "SA") return false;
            bool ha = armor == "HA", sa = armor == "SA";
            switch (part)
            {
                case "composite side skirt": value = new Resistance(ha ? 1.95f : 2.175f, ha ? 1.04f : 1.16f); break;
                case "turret cheek special armor array": value = new Resistance(ha ? 1.69f : 1.885f, ha ? 0.715f : sa ? 1.045f : 0.7975f); break;
                case "hull front special armor array": value = new Resistance(ha ? 1.69f : 1.885f, ha ? 0.65f : sa ? 0.925f : 0.87f); break;
                case "gun mantlet special armor array": value = new Resistance(ha ? 1.43f : 1.495f, ha ? 1.54f : 1.61f); break;
                case "turret side special armor array": value = new Resistance(ha ? 1.69f : 1.885f, ha ? 1.82f : 2.03f); break;
                default: return false;
            }
            return true;
        }

        internal static bool RemoveForLightArmor(string part)
        {
            return string.Equals(part, "turret side special armor array", StringComparison.OrdinalIgnoreCase)
                || string.Equals(part, "hull front special armor array", StringComparison.OrdinalIgnoreCase);
        }
    }
}
