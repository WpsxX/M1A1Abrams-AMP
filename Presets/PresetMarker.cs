using Presets;
using UnityEngine;

namespace M1A1Abrams.Presets
{
    internal class PresetMarker : MonoBehaviour
    {
        public PresetTemplate Preset { get; set; }

        void Awake()
        {
            this.enabled = false;
        }
    }
}
