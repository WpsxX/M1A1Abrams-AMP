// Harness for the CROWS tracking-box table's name matching.
//
// The real source file (CrowsTrackingBoxes.cs) is compiled in unchanged against stub UnityEngine and
// GHPC types, so this tests the table and the matcher that actually ship rather than a copy of them.
// Every vehicle prefab name in the game's exported project is listed, plus the mod vehicles seen in
// this install's log, and each one is asserted to land on a family - or to deliberately land on none.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public static Vector3 zero => new Vector3(0f, 0f, 0f);
        public static Vector3 one => new Vector3(1f, 1f, 1f);

        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y + z * z); } }

        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3 operator *(Vector3 a, float s)
        {
            return new Vector3(a.x * s, a.y * s, a.z * s);
        }

        public static Vector3 Scale(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        }
    }

    public class Transform
    {
        public Vector3 lossyScale = new Vector3(1f, 1f, 1f);
        public Vector3 InverseTransformPoint(Vector3 v) { return v; }
        public Vector3 TransformPoint(Vector3 v) { return v; }
    }

    public static class Mathf
    {
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static float Abs(float a) { return a < 0f ? -a : a; }
        public static float Clamp(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }

    public class GameObject
    {
        public string name;
    }
}

namespace GHPC
{
    using UnityEngine;

    public class Unit
    {
        public GameObject gameObject = new GameObject();
        public string FriendlyName;
        public string UniqueName;
    }
}

internal static class Program
{
    private static int failures;
    private static int matched;

    private static void Check(string unit_name, string friendly, string expected)
    {
        GHPC.Unit unit = new GHPC.Unit();
        unit.gameObject.name = unit_name;
        unit.FriendlyName = friendly;
        unit.UniqueName = unit_name;

        M1A1Abrams.CrowsTrackingBoxes.Box box;
        string label;

        bool found = M1A1Abrams.CrowsTrackingBoxes.TryGet(unit, out box, out label);

        string actual = found ? label : "(none)";
        bool ok = actual == expected;

        if (!ok) failures++;
        if (found) matched++;

        Console.WriteLine("  {0} {1,-24} -> {2,-18} {3}",
            ok ? "PASS" : "FAIL", unit_name, actual, ok ? "" : "(expected " + expected + ")");
    }

    private static int Main()
    {
        Console.WriteLine("CROWS tracking-box table: name matching");
        Console.WriteLine();

        Console.WriteLine("vanilla vehicles (prefab names from the exported project)");

        Check("_M1 (Clean)", "M1", "M1");
        Check("_M1IP (variant)", "M1IP", "M1");
        Check("M1A2 SEP", "M1A2 SEP", "M1");
        Check("M60A3 TTS", "M60A3 TTS", "M60/Leopard 1");
        Check("M60A1 AOS", "M60A1 AOS", "M60/Leopard 1");
        Check("LEO1A1A1", "Leopard 1A1A1", "M60/Leopard 1");
        Check("LEO1A4", "Leopard 1A4", "M60/Leopard 1");
        Check("M113", "M113", "M113/M901");
        Check("M113G", "M113G", "M113/M901");
        Check("M901", "M901", "M113/M901");
        Check("M151", "M151", "M151");
        Check("M151_M232", "M151 M232", "M151");
        Check("M923", "M923", "M923");
        Check("Luchs A1 Variant", "Luchs A1", "Luchs");
        Check("Luchs A2", "Luchs A2", "Luchs");
        Check("Marder 1A2", "Marder 1A2", "Marder/Bradley");
        Check("Marder A1A no MILAN", "Marder A1A", "Marder/Bradley");
        Check("M2 Bradley", "M2 Bradley", "Marder/Bradley");
        Check("BMP1", "BMP-1", "BMP");
        Check("BMP2 Soviet", "BMP-2", "BMP");
        Check("BTR60PB", "BTR-60PB", "BTR");
        Check("BTR70", "BTR-70", "BTR");
        Check("BRDM2", "BRDM-2", "BRDM-2");
        Check("BRDM2 Soviet", "BRDM-2", "BRDM-2");
        Check("T54A", "T-54A", "T-54/T-55/T-62");
        Check("T55A", "T-55A", "T-54/T-55/T-62");
        Check("T62", "T-62", "T-54/T-55/T-62");
        Check("T64A 1974", "T-64A", "T-72/T-80/T-64");
        Check("T64B1 1981", "T-64B1", "T-72/T-80/T-64");
        Check("T64R", "T-64R", "T-72/T-80/T-64");
        Check("T72M1", "T-72M1", "T-72/T-80/T-64");
        Check("T72 Ural", "T-72 Ural", "T-72/T-80/T-64");
        Check("T80B", "T-80B", "T-72/T-80/T-64");
        Check("T-34-85", "T-34-85", "T-34-85");
        Check("PT76B", "PT-76B", "PT-76");
        Check("UAZ469", "UAZ-469", "UAZ-469");
        Check("UAZ469SPG", "UAZ-469 SPG-9", "UAZ-469");
        Check("Ural", "Ural-4320", "Ural");
        Check("Ural Soviet", "Ural-4320", "Ural");
        Check("Mi-2", "Mi-2", "Mi-2");
        Check("Mi-8", "Mi-8", "Mi-8");
        Check("Mi24", "Mi-24", "Mi-24");
        Check("Mi24V soviet Rockets", "Mi-24V", "Mi-24");
        Check("AH-1", "AH-1", "AH-1");
        Check("AH-1 rockets", "AH-1", "AH-1");
        Check("OH-58A", "OH-58A", "OH-58A");

        Console.WriteLine();
        Console.WriteLine("mod vehicles and units the table does not know");

        Check("T-72B3M(Clone)", "T-72B3M", "T-72/T-80/T-64");
        Check("9K111 Soviet Trench", "9K111", "(none)");
        Check("SPG9 Soviet", "SPG-9", "(none)");
        Check("TOW Trench", "TOW", "(none)");
        Check("tc", "tc", "(none)");

        Console.WriteLine();
        Console.WriteLine("collisions the table must not make");

        Check("M113", "M113", "M113/M901");          // not the M1 row
        Check("M901", "M901", "M113/M901");          // not the M1 row
        Check("M151", "M151", "M151");               // not the M1 row
        Check("Mi-2", "Mi-2", "Mi-2");               // not the Mi-24 row
        Check("T-34-85", "T-34-85", "T-34-85");      // not the T-55 row via "T34"
        Check("M923", "M923", "M923");               // not the M60 row

        Console.WriteLine();
        Console.WriteLine(matched + " names matched a family; "
            + (failures == 0 ? "ALL CHECKS PASSED" : failures + " CHECK(S) FAILED"));

        return failures == 0 ? 0 : 1;
    }
}
