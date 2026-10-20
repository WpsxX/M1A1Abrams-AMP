using System;
using M1A1Abrams;

internal static class AmpUpgradeRulesTest
{
    private static int checks;
    private static void Check(bool valid, string message)
    {
        checks++;
        if (!valid) throw new Exception(message);
    }
    private static void Equal(float actual, float expected, string label)
    {
        Check(Math.Abs(actual - expected) < 0.0001f, label + ": " + actual + " != " + expected);
    }
    private static void Armor(string profile, string part, float ce, float ke, bool demigod = false)
    {
        AmpUpgradeRules.Resistance value;
        Check(AmpUpgradeRules.Variable(profile, part, demigod, out value), profile + " missing " + part);
        Equal(value.CE, ce, profile + " CE " + part);
        Equal(value.KE, ke, profile + " KE " + part);
    }
    static void Main()
    {
        Check(AmpUpgradeRules.Armor("  hu ") == "HU", "Case/whitespace");
        Check(AmpUpgradeRules.Armor("") == "", "Explicit base armor");
        Check(AmpUpgradeRules.Armor("Legacy") == "LEGACY", "Keep legacy presets");
        Check(AmpUpgradeRules.Armor("misspelled") == "LEGACY", "Invalid armor fallback");
        Check(AmpUpgradeRules.Skill(" cadet ") == 0, "Cadet");
        Check(AmpUpgradeRules.Skill("Regular") == 1, "Regular");
        Check(AmpUpgradeRules.Skill("VETERAN") == 2, "Veteran");
        Check(AmpUpgradeRules.Skill("Ace") == 3, "Ace");
        Check(AmpUpgradeRules.Skill(null) == 1, "Invalid skill fallback");

        // Explicit expectations for the eight smoke flag combinations.
        var expected = new[] { AmpSmokeMode.Vanilla, AmpSmokeMode.Vanilla, AmpSmokeMode.Vanilla,
            AmpSmokeMode.Vanilla, AmpSmokeMode.Smoke, AmpSmokeMode.Smoke, AmpSmokeMode.Rosy, AmpSmokeMode.RosyPlus };
        for (int bits = 0; bits < 8; bits++)
            Check(AmpUpgradeRules.SmokeMode((bits & 4) != 0, (bits & 2) != 0, (bits & 1) != 0)
                == expected[bits], "Smoke combination " + bits);
        Check(AmpUpgradeRules.SmokeMode(false, true, true) == AmpSmokeMode.Vanilla,
            "Other model remains disabled with global ROSY+");

        Check(!AmpUpgradeRules.Traverse(true, false, 4474.2f), "Traverse needs APU");
        Check(!AmpUpgradeRules.Traverse(false, true, 4474.2f), "Traverse switch");
        Check(!AmpUpgradeRules.Traverse(true, true, 3300f), "Strict engine threshold");
        Check(AmpUpgradeRules.Traverse(true, true, 3303.45f), "T64 qualifies");
        Check(AmpUpgradeRules.Traverse(true, true, 4474.2f), "T55 qualifies");
        Equal(AmpUpgradeRules.AddedMass("HU", false), 17327f, "HU mass");
        Equal(AmpUpgradeRules.AddedMass("HU", true), 5261f, "UAP mass");
        Equal(AmpUpgradeRules.AddedMass("HA", true), 5261f, "HA mass");
        Equal(AmpUpgradeRules.AddedMass("HC", true), 6078f, "UAP must not change HC");
        Equal(AmpUpgradeRules.AddedMass("SA", false), 6894f, "SA mass");
        Equal(AmpUpgradeRules.AddedMass("L", true), -10500f, "Light package mass");
        Equal(AmpUpgradeRules.AddedMass("", true), 0, "Base mass");
        Check(AmpUpgradeRules.RemoveForLightArmor("turret side special armor array"), "L side arrays");
        Check(AmpUpgradeRules.RemoveForLightArmor("hull front special armor array"), "L front array");
        Check(!AmpUpgradeRules.RemoveForLightArmor("gun mantlet special armor array"), "L keeps mantlet");
        Check(!AmpUpgradeRules.RemoveForLightArmor("turret cheek special armor array"), "L keeps cheeks");

        // Golden values transcribed from AMP AmmoArmor.cs and M1A1AbramsAMPMod.cs.
        Armor("HU", "composite side skirt", 11f, 6f);
        Armor("HU", "composite side skirt", 100f, 100f, true);
        Armor("HU", "turret cheek special armor array", 2.2f, 1.25f);
        Armor("HU", "turret cheek special armor array", 100f, 100f, true);
        Armor("HU", "hull front special armor array", 2.2f, 1.2f);
        Armor("HU", "hull front special armor array", 100f, 100f, true);
        Armor("HU", "gun mantlet special armor array", 2.15f, 1.65f);
        Armor("HU", "gun mantlet special armor array", 100f, 100f, true);
        Armor("HU", "turret side special armor array", 4.4f, 2.21f);
        Armor("HU", "turret side special armor array", 100f, 100f, true);
        Armor("HU", "turret roof", 7f, 5.9f);
        Armor("HU", "turret roof", 100f, 100f, true);
        Armor("HU", "upper glacis", 5.5f, 4.7f);
        Armor("HU", "upper glacis", 100f, 100f, true);
        Armor("HU", "commander's hatch", 3.6f, 3f);
        Armor("HU", "commander's hatch", 100f, 100f, true);
        Armor("HU", "loader's hatch", 3.6f, 3f);
        Armor("HU", "loader's hatch", 100f, 100f, true);
        Armor("HU", "driver's hatch", 3.6f, 3f);
        Armor("HU", "driver's hatch", 3.6f, 3f, true);
        Armor("HU", "turret ring", 5f, 5f);
        Armor("HU", "turret ring", 100f, 100f, true);
        Armor("HU", "gun mantlet face", 23f, 18f);
        Armor("HU", "gun mantlet face", 100f, 100f, true);
        Armor("HU", "left track", 10f, 10f);
        Armor("HU", "left track", 100f, 100f, true);
        Armor("HU", "right track", 10f, 10f);
        Armor("HU", "right track", 100f, 100f, true);
        Armor("HU", "main gun barrel", 7f, 7f);
        Armor("HU", "main gun barrel", 100f, 100f, true);
        Armor("HU", "bustle racks firewall", 2.667f, 2.667f);
        Armor("HU", "bustle racks firewall", 100f, 100f, true);
        Armor("HU", "center blowout panel", 18f, 15f);
        Armor("HU", "center blowout panel", 100f, 100f, true);
        Armor("HU", "left blowout panel", 18f, 15f);
        Armor("HU", "left blowout panel", 100f, 100f, true);
        Armor("HU", "right blowout panel", 18f, 15f);
        Armor("HU", "right blowout panel", 100f, 100f, true);
        Armor("HU", "turret rear face", 40f, 20f);
        Armor("HU", "turret rear face", 100f, 100f, true);
        Armor("HU", "GPS doghouse", 5f, 5f);
        Armor("HU", "GPS doghouse", 100f, 100f, true);
        Armor("HU", "GPS doghouse door", 5f, 5f);
        Armor("HU", "GPS doghouse door", 100f, 100f, true);
        Armor("HU", "turret bottom", 14f, 12f);
        Armor("HU", "turret bottom", 100f, 100f, true);
        Armor("HU", "trunnion", 1.5f, 1.5f);
        Armor("HU", "trunnion", 100f, 100f, true);
        Armor("HU", "semi-ready rack door", 5f, 5f);
        Armor("HU", "semi-ready rack door", 100f, 100f, true);
        Armor("HU", "ready rack door", 5f, 5f);
        Armor("HU", "ready rack door", 100f, 100f, true);
        Armor("HU", "lower front plate", 3.333f, 3.333f);
        Armor("HU", "lower front plate", 100f, 100f, true);
        Armor("HU", "hull front backing plate", 2.5f, 2.5f);
        Armor("HU", "hull front backing plate", 100f, 100f, true);
        Armor("HU", "turret cheek face", 1.6f, 1.6f);
        Armor("HU", "turret cheek face", 100f, 100f, true);
        Armor("HU", "turret cheek backing plate", 3f, 3f);
        Armor("HU", "turret cheek backing plate", 100f, 100f, true);
        Armor("HU", "turret side backing plate", 3f, 3f);
        Armor("HU", "turret side backing plate", 100f, 100f, true);
        Armor("HU", "turret side face", 2.667f, 2.667f);
        Armor("HU", "turret side face", 100f, 100f, true);
        Armor("HA", "composite side skirt", 1.95f, 1.04f);
        Armor("HA", "turret cheek special armor array", 1.69f, 0.715f);
        Armor("HA", "hull front special armor array", 1.69f, 0.65f);
        Armor("HA", "gun mantlet special armor array", 1.43f, 1.54f);
        Armor("HA", "turret side special armor array", 1.69f, 1.82f);
        Armor("HC", "composite side skirt", 2.175f, 1.16f);
        Armor("HC", "turret cheek special armor array", 1.885f, 0.7975f);
        Armor("HC", "hull front special armor array", 1.885f, 0.87f);
        Armor("HC", "gun mantlet special armor array", 1.495f, 1.61f);
        Armor("HC", "turret side special armor array", 1.885f, 2.03f);
        Armor("SA", "composite side skirt", 2.175f, 1.16f);
        Armor("SA", "turret cheek special armor array", 1.885f, 1.045f);
        Armor("SA", "hull front special armor array", 1.885f, 0.925f);
        Armor("SA", "gun mantlet special armor array", 1.495f, 1.61f);
        Armor("SA", "turret side special armor array", 1.885f, 2.03f);
        Armor("SA", "turret cheek special armor array", 1.885f, 1.045f, true);
        AmpUpgradeRules.Resistance r;
        Check(!AmpUpgradeRules.Variable("HC", "turret roof", true, out r), "HU must not leak into HC");
        Check(!AmpUpgradeRules.Variable("", "turret cheek special armor array", true, out r), "Base untouched");
        Check(!AmpUpgradeRules.Variable("HU", "unknown component", true, out r), "Unknown part untouched");
        Check(AmpUpgradeRules.Uniform("HU", "hull side", false, out r), "HU uniform part");
        Equal(r.CE, 300, "HU hull CE"); Equal(r.KE, 200, "HU hull KE");
        Check(AmpUpgradeRules.Uniform("HU", "hull side", true, out r), "Demigod uniform part");
        Equal(r.CE, 10000, "Demigod CE"); Equal(r.KE, 10000, "Demigod KE");
        Check(!AmpUpgradeRules.Uniform("SA", "hull side", true, out r), "Demigod only HU");
        Console.WriteLine("PASS: " + checks + " AMP upgrade assertions.");
    }
}
