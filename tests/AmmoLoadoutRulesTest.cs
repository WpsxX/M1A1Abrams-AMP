using System;
using M1A1Abrams;

internal static class AmmoLoadoutRulesTest
{
    static void Check(bool valid, string message)
    {
        if (!valid) throw new Exception(message);
    }

    static void Verify(int[] requested, int capacity)
    {
        int[] input = (int[])requested.Clone();
        int[] fitted = AmmoLoadoutRules.Fit(input, capacity);
        long requestedTotal = 0, actualTotal = 0;
        int kinds = 0;
        for (int i = 0; i < input.Length; i++)
        {
            Check(input[i] == requested[i], "Input mutated");
            Check(fitted[i] >= 0 && fitted[i] <= Math.Max(0, input[i]), "Invalid per-type count");
            requestedTotal += Math.Max(0, input[i]);
            actualTotal += fitted[i];
            if (input[i] > 0) kinds++;
        }
        Check(actualTotal == Math.Min(Math.Max(0, capacity), requestedTotal), "Ammo lost or overfilled");
        for (int i = 0; i < input.Length; i++)
            if (capacity >= kinds && input[i] > 0) Check(fitted[i] > 0, "Selected type dropped");

        // Three real Abrams rack sizes. Explicit per-rack counts must preserve the fitted
        // totals, including four types, partial loads, zero slots and negative input.
        if (capacity != 40) return;
        int[] remaining = (int[])fitted.Clone();
        foreach (int rackCapacity in new[] { 18, 18, 4 })
        {
            int[] rack = AmmoLoadoutRules.Fit(remaining, rackCapacity);
            int sum = 0;
            for (int i = 0; i < rack.Length; i++) { remaining[i] -= rack[i]; sum += rack[i]; }
            Check(sum <= rackCapacity, "Rack overflow");
        }
        foreach (int count in remaining) Check(count == 0, "Unassigned rounds");
    }

    static void Main()
    {
        Verify(new[] { 10, 10, 10, 10 }, 40);
        Verify(new[] { 22, 18, 12, 6 }, 40);
        Verify(new[] { 22, 18 }, 40);
        Verify(new[] { 0, 0, 0, 0 }, 40);
        Verify(new[] { -1, 3, 1, 0 }, 40);
        Verify(new[] { int.MaxValue, int.MaxValue, 1, 1 }, 40);
        Verify(new[] { 1, 1, 1, 1 }, 2);
        Verify(new[] { 22, 18, 3, 9 }, 52);
        Verify(new[] { 1, 2 }, -1);
        var random = new Random(173);
        for (int n = 0; n < 5000; n++)
            Verify(new[] { random.Next(-5, 1000), random.Next(-5, 1000),
                random.Next(-5, 1000), random.Next(-5, 1000) }, 40);
        Console.WriteLine("AmmoLoadoutRules: 5009 scenarios passed (including four types x 10 rounds).");
    }
}
