// Standalone harness for the CROWS box-sizing algorithm (CrowsAutoTrack.AxisWindow + the part
// plausibility filters). Unity types are replaced by plain structs, but the arithmetic is the same:
// per-axis min/max of each part in the unit frame, volume weights, and the narrowest sampled window
// holding BOX_COVERAGE of the total weight.
using System;
using System.Collections.Generic;

internal struct Box
{
    public double minx, miny, minz, maxx, maxy, maxz;

    public Box(double minx, double miny, double minz, double maxx, double maxy, double maxz)
    {
        this.minx = minx; this.miny = miny; this.minz = minz;
        this.maxx = maxx; this.maxy = maxy; this.maxz = maxz;
    }

    public double GetMin(int axis) { return axis == 0 ? minx : axis == 1 ? miny : minz; }
    public double GetMax(int axis) { return axis == 0 ? maxx : axis == 1 ? maxy : maxz; }
}

internal static class Program
{
    private const double MAX_PART_SIZE = 25.0;
    private const double MAX_PART_DISTANCE = 20.0;
    private const double BOX_COVERAGE = 0.995;
    private const int BOX_SAMPLES = 384;

    private static readonly List<Box> parts = new List<Box>();
    private static readonly List<double> weights = new List<double>();
    private static readonly double[] samples = new double[BOX_SAMPLES];

    // Mirrors GatherParts: a part is dropped when its own transformed bounds are larger than a
    // vehicle, or sit further from the unit origin than a vehicle reaches.
    private static int Gather(string name, Bounds3[] raw, out int rejected)
    {
        parts.Clear();
        weights.Clear();
        rejected = 0;

        foreach (Bounds3 b in raw)
        {
            double sx = Math.Abs(b.maxx - b.minx);
            double sy = Math.Abs(b.maxy - b.miny);
            double sz = Math.Abs(b.maxz - b.minz);
            double cx = (b.minx + b.maxx) * 0.5;
            double cy = (b.miny + b.maxy) * 0.5;
            double cz = (b.minz + b.maxz) * 0.5;

            double size = Math.Sqrt(sx * sx + sy * sy + sz * sz);
            double dist = Math.Sqrt(cx * cx + cy * cy + cz * cz);

            if (size > MAX_PART_SIZE || dist > MAX_PART_DISTANCE)
            {
                rejected++;
                continue;
            }

            parts.Add(new Box(b.minx, b.miny, b.minz, b.maxx, b.maxy, b.maxz));
            weights.Add(Math.Max(sx * sy * sz, 1e-4));
        }

        return parts.Count;
    }

    private static bool AxisWindow(int axis, int count, out double min, out double max)
    {
        min = 0; max = 0;

        if (count <= 0) return false;

        double lowest = double.MaxValue, highest = double.MinValue;

        for (int i = 0; i < count; i++)
        {
            lowest = Math.Min(lowest, parts[i].GetMin(axis));
            highest = Math.Max(highest, parts[i].GetMax(axis));
        }

        double span = highest - lowest;

        if (span <= 1e-5) { min = lowest; max = highest; return true; }

        double total = 0;
        for (int i = 0; i < count; i++) total += weights[i];

        if (count == 1 || total <= 0) { min = lowest; max = highest; return true; }

        Array.Clear(samples, 0, BOX_SAMPLES);

        double per_sample = span / (BOX_SAMPLES - 1);
        double inverse = (BOX_SAMPLES - 1) / span;

        for (int i = 0; i < count; i++)
        {
            int first = Clamp((int)Math.Floor((parts[i].GetMin(axis) - lowest) * inverse), 0, BOX_SAMPLES - 1);
            int last = Clamp((int)Math.Ceiling((parts[i].GetMax(axis) - lowest) * inverse), 0, BOX_SAMPLES - 1);

            double density = weights[i] / (last - first + 1);

            for (int s = first; s <= last; s++) samples[s] += density;
        }

        double wanted = total * BOX_COVERAGE;
        double running = 0;
        int head = 0, best_head = 0, best_tail = BOX_SAMPLES - 1, best_width = BOX_SAMPLES;

        for (int tail = 0; tail < BOX_SAMPLES; tail++)
        {
            running += samples[tail];

            while (head <= tail && running - samples[head] >= wanted)
            {
                running -= samples[head];
                head++;
            }

            if (running >= wanted && tail - head + 1 < best_width)
            {
                best_width = tail - head + 1;
                best_head = head;
                best_tail = tail;
            }
        }

        if (best_width >= BOX_SAMPLES) { min = lowest; max = highest; return true; }

        min = lowest + best_head * per_sample;
        max = lowest + best_tail * per_sample;

        if (max - min < per_sample * 2.0) { min = lowest; max = highest; }

        return true;
    }

    private static int Clamp(int v, int lo, int hi) { return v < lo ? lo : (v > hi ? hi : v); }

    private struct Bounds3
    {
        public double minx, miny, minz, maxx, maxy, maxz;
        public Bounds3(double a, double b, double c, double d, double e, double f)
        { minx = a; miny = b; minz = c; maxx = d; maxy = e; maxz = f; }
    }

    private static int failures;

    private static void Check(string what, double actual, double expected, double tolerance)
    {
        bool ok = Math.Abs(actual - expected) <= tolerance;
        if (!ok) failures++;
        Console.WriteLine("  {0} {1}: {2:F3} (expected {3:F3} +/- {4:F3})",
            ok ? "PASS" : "FAIL", what, actual, expected, tolerance);
    }

    private static void Run(string title, List<Bounds3> raw, double[] expected_min, double[] expected_max,
        double tolerance, int expected_rejected)
    {
        Console.WriteLine(title);

        int rejected;
        int count = Gather(title, raw.ToArray(), out rejected);

        Check("rejected parts", rejected, expected_rejected, 0);

        string[] axis_name = { "x (width)", "y (height)", "z (length)" };

        for (int axis = 0; axis < 3; axis++)
        {
            double min, max;
            AxisWindow(axis, count, out min, out max);
            Check(axis_name[axis] + " min", min, expected_min[axis], tolerance);
            Check(axis_name[axis] + " max", max, expected_max[axis], tolerance);
        }

        Console.WriteLine();
    }

    private static List<Bounds3> Tank()
    {
        var list = new List<Bounds3>();

        // hull: 3.5 wide, 1.4 tall, 7 long, centred on the vehicle
        list.Add(new Bounds3(-1.75, 0.5, -3.5, 1.75, 1.9, 3.5));
        // turret: 3 wide, 0.9 tall, 3 long, on top
        list.Add(new Bounds3(-1.5, 1.9, -1.5, 1.5, 2.8, 1.5));
        // two tracks
        list.Add(new Bounds3(-1.95, 0.0, -3.4, -1.55, 0.9, 3.4));
        list.Add(new Bounds3(1.55, 0.0, -3.4, 1.95, 0.9, 3.4));
        // barrel: reaches 3 m past the hull nose, tiny volume
        list.Add(new Bounds3(-0.06, 1.9, 1.5, 0.06, 2.02, 7.0));
        // radio aerial: 4 m straight up, almost no volume
        list.Add(new Bounds3(1.4, 2.8, -1.5, 1.42, 6.8, -1.48));

        // a hundred brackets, lamps, bolts and mirrors scattered over the hull
        var random = new Random(20260915);
        for (int i = 0; i < 100; i++)
        {
            double x = -1.7 + random.NextDouble() * 3.4;
            double y = 0.5 + random.NextDouble() * 2.2;
            double z = -3.4 + random.NextDouble() * 6.8;
            double s = 0.05 + random.NextDouble() * 0.15;
            list.Add(new Bounds3(x, y, z, x + s, y + s, z + s));
        }

        return list;
    }

    private static int Main()
    {
        Console.WriteLine("CROWS box sizing harness");
        Console.WriteLine();

        // 1. A tank: the box must be the hull and turret, not the barrel or the aerial.
        Run("tank (barrel and aerial must not set the box)",
            Tank(),
            new[] { -1.95, 0.0, -3.5 },
            new[] { 1.95, 2.8, 3.5 },
            0.15, 0);

        // 2. The live failure: a part whose bind pose sits hundreds of metres up.
        List<Bounds3> ghost = Tank();
        ghost.Add(new Bounds3(-1, 328.5, -1, 1, 330.5, 1));
        Run("tank + skinned-mesh ghost at y=329 m (must be rejected)",
            ghost,
            new[] { -1.95, 0.0, -3.5 },
            new[] { 1.95, 2.8, 3.5 },
            0.15, 1);

        // 3. A part a few metres across under a transform scaled by 300: localBounds is small, the
        //    transformed bounds are not.
        List<Bounds3> scaled = Tank();
        scaled.Add(new Bounds3(-150, 0, -150, 150, 400, 150));
        Run("tank + 300x-scaled part (must be rejected)",
            scaled,
            new[] { -1.95, 0.0, -3.5 },
            new[] { 1.95, 2.8, 3.5 },
            0.15, 1);

        // 4. A small scout car: the same rule has to size it without a per-size constant.
        var car = new List<Bounds3>
        {
            new Bounds3(-0.9, 0.35, -2.0, 0.9, 1.15, 2.0),
            new Bounds3(-0.8, 1.15, -0.6, 0.8, 1.75, 0.8),
            new Bounds3(-1.0, 0.0, -1.9, -0.75, 0.7, 1.9),
            new Bounds3(0.75, 0.0, -1.9, 1.0, 0.7, 1.9),
            new Bounds3(-0.8, 1.75, -0.6, -0.78, 3.9, -0.58)
        };
        Run("scout car (small vehicle, same rule)",
            car,
            new[] { -1.0, 0.0, -2.0 },
            new[] { 1.0, 1.75, 2.0 },
            0.1, 0);

        // 5. A single-part model: nothing to sample, the part's own extent is the answer.
        Run("single-part model",
            new List<Bounds3> { new Bounds3(-2, -1, -4, 2, 1, 4) },
            new[] { -2.0, -1.0, -4.0 },
            new[] { 2.0, 1.0, 4.0 },
            0.01, 0);

        // 6. A model that is one big mesh plus a solid block hung off the back: the block is a real
        //    fraction of the volume, so the box has to keep it.
        var doubled = new List<Bounds3>
        {
            new Bounds3(-1.5, 0.0, -3.0, 1.5, 2.0, 3.0),
            new Bounds3(-1.0, 0.0, 3.0, 1.0, 2.0, 4.2)
        };
        Run("hull plus a solid rear block (block is 20% of the volume and stays)",
            doubled,
            new[] { -1.5, 0.0, -3.0 },
            new[] { 1.5, 2.0, 4.2 },
            0.05, 0);

        Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : failures + " CHECK(S) FAILED");

        return failures == 0 ? 0 : 1;
    }
}
