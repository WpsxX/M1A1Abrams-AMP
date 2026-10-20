using GHPC.Camera;
using GHPC.Equipment.Optics;
using GHPC.Vehicle;
using UnityEngine;

namespace M1A1Abrams
{
    internal static class AmpOptics
    {
        // CROWS owns its 1/5/10/15x zooms; AMP enhancement applies to the main sights.
        internal static void ApplyPair(CameraSlot day, CameraSlot thermal, bool crows)
        {
            // Its thermal image is copied from the configured main gun separately.
            if (crows) return;
            FixedReticleSize.Attach(day);
            FixedReticleSize.Attach(thermal);
            if (day != null && AmpFeatures.BetterDaysight.Value)
                Improve(day, false);
            if (thermal != null && AmpFeatures.BetterFlir.Value)
                Improve(thermal, true);
        }

        private static void Improve(CameraSlot slot, bool thermal)
        {
            slot.DefaultFov = 12.5f;
            slot.OtherFovs = new[] { 6.5f, 3.472f, 1.204f, 0.488f };
            slot.BaseBlur = 0f;
            if (!thermal) return;
            slot.OverrideFLIRResolution = true;
            // Do not downgrade a generation-3 sight. Preserve a 16:9 texture for CROWS.
            slot.FLIRWidth = Mathf.Max(slot.FLIRWidth, 1280);
            slot.FLIRHeight = Mathf.Max(slot.FLIRHeight, 720);
            slot.FLIRFilterMode = FilterMode.Trilinear;
            if (Assets.flir_blit_mat_green_no_scan != null)
                slot.FLIRBlitMaterialOverride = Assets.flir_blit_mat_green_no_scan;
        }

        internal static void ApplyVehicle(Vehicle vehicle)
        {
            Transform gps = vehicle.transform.Find("IPM1_rig/HULL/TURRET/Turret Scripts/GPS/Optic");
            UsableOptic day = gps == null ? null : gps.GetComponent<UsableOptic>();
            CameraSlot thermal = day == null || day.slot == null ? null : day.slot.LinkedNightSight;
            ApplyPair(day == null ? null : day.slot, thermal, false);
            if (!AmpFeatures.BetterGas.Value) return;
            Transform gas = vehicle.transform.Find("IPM1_rig/HULL/TURRET/GUN/Gun Scripts/Aux sight (GAS)");
            CameraSlot slot = gas == null ? null : gas.GetComponent<CameraSlot>();
            if (slot == null) return;
            slot.DefaultFov = 6.5f;
            slot.OtherFovs = new[] { 4.2f, 2.716f, 1.588f };
            slot.VibrationBlurScale = 0.1f;
            slot.VibrationShakeMultiplier = 0.2f;
        }
    }
}
