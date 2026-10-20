using System;
using GHPC.Camera;
using GHPC.Equipment.Optics;
using M1A1Abrams;
using Reticle;
using UnityEngine;

internal static class OpticsRegressionTests
{
    private static int checks;
    private static void Expect(bool result, string message)
    {
        if (!result) throw new Exception(message);
        checks++;
    }
    private static CameraSlot Slot(float defaultFov, params float[] otherFovs)
    {
        var slot = new GameObject().AddComponent<CameraSlot>();
        slot.DefaultFov = defaultFov; slot.OtherFovs = otherFovs; slot.IsActive = true;
        return slot;
    }
    private static void Main()
    {
        foreach (bool dayEnhancement in new[] { false, true })
        foreach (bool thermalEnhancement in new[] { false, true })
        {
            AmpFeatures.BetterDaysight.Value = dayEnhancement;
            AmpFeatures.BetterFlir.Value = thermalEnhancement;
            var day = Slot(30f, 6f, 3f, 2f);
            var thermal = Slot(30f, 6f, 3f, 2f);
            AmpOptics.ApplyPair(day, thermal, true);
            foreach (var slot in new[] { day, thermal })
            {
                Expect(slot.DefaultFov == 30 && slot.OtherFovs.Length == 3, "CROWS must have exactly four zoom levels");
                Expect(slot.OtherFovs[0] == 6 && slot.OtherFovs[1] == 3 && slot.OtherFovs[2] == 2, "CROWS zooms must remain 5/10/15x");
                Expect(slot.GetComponent<FixedReticleBinding>() == null, "CROWS uses its own reticle scaler");
            }
        }
        AmpFeatures.BetterDaysight.Value = AmpFeatures.BetterFlir.Value = true;
        var mainDay = Slot(9.52f, 3.472f);
        var mainThermal = Slot(9.52f, 3.472f);
        AmpOptics.ApplyPair(mainDay, mainThermal, false);
        Expect(mainDay.OtherFovs.Length == 4 && mainThermal.OtherFovs.Length == 4, "Keep main-gun enhanced zooms");
        Expect(mainThermal.FLIRWidth == 1280 && mainThermal.FLIRHeight == 720, "Keep main-gun FLIR enhancement");

        foreach (var position in new[] { new Vector3(0, 0, 1200), new Vector3(0, -20, 750) })
        {
            var obj = new GameObject();
            var renderer = obj.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = new Mesh();
            var reticle = obj.AddComponent<ReticleMesh>();
            var original = new ReticleSO { name = "GPS" };
            var plane = new ReticleTree.FocalPlane();
            var line = new ReticleTree.Line(); line.length.MRADS = 20;
            plane.elements.Add(line); original.planes.Add(plane); reticle.reticleSO = original;
            obj.transform.localPosition = position;
            reticle.SetLight(ReticleTree.Light.Type.NightIllumination, 1);
            var scaler = obj.AddComponent<FixedReticleSize>(); scaler.Bind(reticle);
            mainDay.PairedOptic = new UsableOptic { FCS = new FireControlSystem { ReportedRange = 800 } };
            for (int cycle = 0; cycle < 8; cycle++)
            {
                mainDay.IsActive = false;
                scaler.Apply(mainDay, 3.472f);
                mainDay.IsActive = true;
                mainDay.FovIndex = cycle % 2;
                scaler.Apply(mainDay, 3.472f);
                Expect(obj.transform.localPosition.y == position.y && obj.transform.localPosition.z == position.z, "Reticle placement lost after day/thermal switching");
                reticle.GetLight(ReticleTree.Light.Type.NightIllumination, out float light);
                Expect(light == 0.5f, "Night illumination must not repeatedly dim across rebuilds");
                Expect(reticle.Range == 800, "Restore fire-control range after rebuilding");
                Expect(renderer.sharedMesh != null && line.length.MRADS == 20, "Keep rendered mesh and donor geometry");
            }
            int loads = reticle.Loads;
            renderer.sharedMesh = null;
            scaler.Apply(mainDay, 3.472f);
            Expect(reticle.Loads == loads + 1 && renderer.sharedMesh != null, "Missing mesh must recover at an unchanged zoom");
            Expect(reticle.OriginResets == 0, "Do not allow native Load to reset a projected reticle");
            mainDay.FovIndex = 2;
            reticle.FailLoad = true;
            scaler.Apply(mainDay, 3.472f);
            Expect(obj.transform.localPosition.z == position.z, "Preserve placement even if native generation fails");
            reticle.FailLoad = false;
            scaler.Apply(mainDay, 3.472f);
            Expect(renderer.sharedMesh != null, "Retry failed native generation");
        }
        Console.WriteLine("PASS: " + checks + " optics regressions (native API substitutes; no Unity rendering).");
    }
}
