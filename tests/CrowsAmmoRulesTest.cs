using System;
using M1A1Abrams;

internal static class CrowsAmmoRulesTest
{
    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void RequireClose(double actual, double expected, double tolerance, string message)
    {
        if (Math.Abs(actual - expected) > tolerance)
        {
            throw new Exception(string.Format("{0}: expected {1:F3} +/- {2:F3}, got {3:F3}", message, expected, tolerance, actual));
        }
    }

    // GHPC's drag model is purely quadratic, so speed against distance is hyperbolic
    // (1/v = 1/v0 + k*x) and penetration follows P = P0 / (1 + k*v0*x)^2. CrowsAmmoRules
    // hands out k (DragPerMetre) and the muzzle figure (MuzzlePenetration) directly, and
    // CrowsAmmo.Coeff feeds the very same k to the round, so the gun and the CROWS
    // predictor share one curve. This test replays that closed form -- no integration.
    private static double Attenuation(double range)
    {
        return 1.0 + (double)CrowsAmmoRules.DragPerMetre * CrowsAmmoRules.Velocity * range;
    }

    private static double PenetrationAt(double range)
    {
        double a = Attenuation(range);
        return CrowsAmmoRules.MuzzlePenetration / (a * a);
    }

    private static double SpeedAt(double range)
    {
        return CrowsAmmoRules.Velocity / Attenuation(range);
    }

    private static void CheckRound(float mass)
    {
        Require(mass > 0.0226 && mass < 0.0234, "Use the flying core mass, not the 35 g sabot assembly.");

        // The rule table and the round must agree on the same drag constant.
        RequireClose((double)CrowsAmmoRules.DragCoeff,
                     (double)CrowsAmmoRules.DragPerMetre * CrowsAmmoRules.CoreMass / ((double)CrowsAmmoRules.Area * CrowsAmmoRules.ReferenceDensity),
                     1e-10,
                     "DragCoeff must be DragPerMetre * CoreMass / (Area * ReferenceDensity)");

        // 34 mm at 500 m is the document anchor the muzzle figure is solved from.
        RequireClose(PenetrationAt(500.0), 34.0, 0.5, "500 m penetration (document anchor)");

        // The curve is anchored on 500 m and 1,500 m, which lands 1,000 m at 26.0 mm
        // against the document's approximately 25 mm.
        RequireClose(PenetrationAt(1000.0), 26.0, 0.5, "1,000 m penetration (interpolated)");

        double[] ranges = { 500, 1000, 1500 };
        double[] expectedPen = { 34.0, 26.0, 20.5 };
        double[] expectedSpeed = { 1043, 911, 809 };
        for (int i = 0; i < ranges.Length; i++)
        {
            double speed = SpeedAt(ranges[i]);
            double pen = PenetrationAt(ranges[i]);
            RequireClose(pen, expectedPen[i], 0.5, string.Format("{0} m penetration", ranges[i]));
            RequireClose(speed, expectedSpeed[i], 1.0, string.Format("{0} m retained speed", ranges[i]));
            Console.WriteLine("{0:F2} g: {1} m -> {2:F2} mm RHA, {3:F1} m/s", mass * 1000, ranges[i], pen, speed);
        }
    }

    public static void Main()
    {
        Require(CrowsAmmoRules.Normalize(" M8 API ") == CrowsAmmoRules.M8, "M8 selection");
        Require(CrowsAmmoRules.Normalize("Mk 211 Mod 0") == CrowsAmmoRules.Mk211, "Full Mk211 name must work.");
        Require(CrowsAmmoRules.Normalize("mk211") == CrowsAmmoRules.Mk211, "Legacy shorthand");
        Require(CrowsAmmoRules.Normalize("M903 SLAP") == CrowsAmmoRules.M903, "M903 selection");
        Require(CrowsAmmoRules.Normalize("M903") == CrowsAmmoRules.M903, "M903 shorthand");
        // 1.3.5 promotes the tracer round to its own selectable entry (it also stays in the
        // M903 belt as the fifth round), so Normalize must resolve all three spellings.
        Require(CrowsAmmoRules.Normalize("M962") == CrowsAmmoRules.M962, "M962 must be separately selectable.");
        Require(CrowsAmmoRules.Normalize("M962 SLAP-T") == CrowsAmmoRules.M962, "Full M962 name must work.");
        Require(CrowsAmmoRules.Normalize("m962 slapt") == CrowsAmmoRules.M962, "M962 shorthand");
        Require(CrowsAmmoRules.Normalize("M903/M962 (4:1)") == null, "No combined option.");
        Require(CrowsAmmoRules.Normalize(null) == null && CrowsAmmoRules.Normalize("bad value") == null, "Invalid selection must use caller's M8 fallback.");
        Require(CrowsAmmoRules.Velocity == 1219 && CrowsAmmoRules.TracerRange == 1500, "Document speed and tracer range.");
        CheckRound(CrowsAmmoRules.CoreMass);
        Console.WriteLine("PASS: four selections, aliases, invalid input, shared core mass, drag constant and document penetration samples.");
    }
}
