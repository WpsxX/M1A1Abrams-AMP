using Presets;

namespace M1A1Abrams
{
    internal class M1Preset : PresetTemplate
    {
        public bool M256Gun { get; set; }
        public int APCount { get; set; } = 22;
        public int HEATCount { get; set; } = 18;
        public string APRound { get; set; } = "M829";
        public string HEATRound { get; set; } = "M830";
        public bool RotateAzimuth { get; set; }
        public int FLIRGeneration { get; set; } = 1;
        // Retained for old preset JSON; zooms are now owned by FLIR+/Daysight+.
        public bool DigitalEnhancement { get; set; }
        public bool CITV { get; set; }
        public bool DUArmour { get; set; }
        public int DUGeneration { get; set; } = 1;
        public bool VanillaAmmoCapacity { get; set; }
        public bool ConvertToM1IP { get; set; }
    }
}
