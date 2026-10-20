using System;
using System.Collections.Generic;
using GHPC.Camera;
using GHPC.Equipment.Optics;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace M1A1Abrams
{
    internal static partial class CROWS
    {
        /// <summary>
        /// Copies the configured gunner FLIR pipeline, keeping volume activation local to this optic.
        /// </summary>
        private static void ConfigureMainGunThermal(UsableOptic optic, UsableOptic main_gun)
        {
            Transform source_post = main_gun.transform.Find("FLIR Post Processing - Green");
            if (source_post == null)
                throw new InvalidOperationException("Abrams gunner FLIR post-processing is missing");

            CameraSlot source_slot = main_gun.slot;
            CameraSlot slot = optic.slot;
            slot.OverrideFLIRResolution = source_slot.OverrideFLIRResolution;
            slot.FLIRWidth = source_slot.FLIRWidth;
            slot.FLIRHeight = source_slot.FLIRHeight;
            slot.FLIRFilterMode = source_slot.FLIRFilterMode;
            slot.FLIRBlitMaterialOverride = source_slot.FLIRBlitMaterialOverride;
            slot.DoMechanicalFLIR = source_slot.DoMechanicalFLIR;
            slot.MechanicalScanWidth = source_slot.MechanicalScanWidth;
            slot.MechanicalScanHeight = source_slot.MechanicalScanHeight;
            slot.CanToggleFlirPolarity = source_slot.CanToggleFlirPolarity;
            slot.BaseBlur = source_slot.BaseBlur;
            slot.VibrationPreBlur = source_slot.VibrationPreBlur;

            // The M85 donor has a day-sight volume on the slot itself. It must not
            // be blended into the thermal camera or keep its old optic binding.
            foreach (PostProcessVolume donor in slot.GetComponentsInChildren<PostProcessVolume>(true))
                UnityEngine.Object.DestroyImmediate(donor);

            GameObject post = UnityEngine.Object.Instantiate(source_post.gameObject, slot.transform, false);
            post.SetActive(true);
            PostProcessVolume[] source_volumes = source_post.GetComponentsInChildren<PostProcessVolume>(true);
            PostProcessVolume[] volumes = post.GetComponentsInChildren<PostProcessVolume>(true);
            var volume_map = new Dictionary<PostProcessVolume, PostProcessVolume>();
            for (int i = 0; i < source_volumes.Length; i++)
            {
                // Include any runtime changes to the source profile. Each clone
                // owns its enabled state; profiles and materials are read-only here.
                volumes[i].sharedProfile = source_volumes[i].profile;
                volumes[i].enabled = false;
                volume_map.Add(source_volumes[i], volumes[i]);
            }

            // UsableOptic.Start subscribes to ActiveChanged and controls both
            // MainCam and FLIR Only volumes, exactly as it does for the main gun.
            optic.post = main_gun.post == null ? null : volume_map[main_gun.post];
            optic.AltNightPost = main_gun.AltNightPost == null ? null : volume_map[main_gun.AltNightPost];
            optic.postAdditional = Array.ConvertAll(main_gun.postAdditional,
                volume => volume_map[volume]);
        }
    }
}
