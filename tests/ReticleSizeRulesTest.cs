using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using M1A1Abrams;
using Reticle;

internal static class ReticleSizeRulesTest
{
    private static int checks;
    private static void Near(float actual, float expected, string message)
    {
        if (Math.Abs(actual - expected) > Math.Max(0.0001f, Math.Abs(expected) * 0.0001f))
            throw new Exception(message + ": " + actual + " != " + expected);
        checks++;
    }

    private static void Main(string[] args)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, request) =>
            Assembly.LoadFrom(Path.Combine(args[0], new AssemblyName(request.Name).Name + ".dll"));
        Run();
        Console.WriteLine("PASS: " + checks + " reticle sizing checks against native GHPC reticle data types.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        var line = new ReticleTree.Line();
        line.position.x = 12; line.position.y = -8;
        line.length.MRADS = 20; line.thickness.MRADS = 0.6f;
        var circle = new ReticleTree.Circle();
        circle.position = new ReticleTree.Position(-5, 3);
        circle.radius.MRADS = 4; circle.thickness.MRADS = 0.4f;
        var text = new ReticleTree.Text();
        text.position.x = 7; text.position.y = -2; text.fontSize = 12;
        var snapshots = new[] { new ReticleElementSize(line), new ReticleElementSize(circle), new ReticleElementSize(text) };

        for (int cycle = 0; cycle < 10; cycle++)
        foreach (float fov in new[] { 12.5f, 6.5f, 3.472f, 1.204f, 0.488f, 6.5f, 12.5f })
        {
            float scale = ReticleSizeRules.Scale(fov, 3.472f);
            foreach (var snapshot in snapshots) snapshot.Apply(scale);
            Near(line.position.x, 12 * scale, "line position accumulates across zooms");
            Near(line.position.y, -8 * scale, "line Y");
            Near(line.length.MRADS, 20 * scale, "line length");
            Near(line.thickness.MRADS, 0.6f * scale, "line width");
            Near(circle.radius.MRADS, 4 * scale, "circle radius");
            Near(circle.thickness.MRADS, 0.4f * scale, "circle width");
            Near(text.fontSize, 12 * scale, "text size");
            double pixelsPerAngularUnit = 1 / Math.Tan(fov * Math.PI / 360);
            double referencePixels = 20 / Math.Tan(3.472 * Math.PI / 360);
            Near((float)(line.length.MRADS * pixelsPerAngularUnit), (float)referencePixels, "projected reference size");
            // Disable then re-enable: no loss of the original data or accumulated shrinkage.
            foreach (var snapshot in snapshots) snapshot.Apply(1);
            Near(line.position.x, 12, "restore line position");
            Near(circle.radius.MRADS, 4, "restore circle");
            Near(text.fontSize, 12, "restore text");
        }
    }
}
