namespace M1A1Abrams
{
    internal enum AmpFuzeMode { Impact, Proximity, Timed }

    // Shared by input, HUD and the firing callback. A press may be observed by
    // all three in one Unity frame, but must change the selected mode only once.
    internal sealed class AmpFuzeState
    {
        private AmpFuzeMode mode = AmpFuzeMode.Impact;
        private int lastToggleFrame = -1;

        internal AmpFuzeMode Read(bool proxyEnabled, bool middleMouseDown, int frame)
        {
            if (!proxyEnabled)
            {
                // The proxy entry forces the timed airburst; middle mouse is ignored.
                mode = AmpFuzeMode.Impact;
                return AmpFuzeMode.Timed;
            }
            if (middleMouseDown && frame != lastToggleFrame)
            {
                lastToggleFrame = frame;
                mode = Next(mode);
            }
            return mode;
        }

        // Impact <-> Proximity, matching upstream AMP (ProxySwitchAMP.activated): the middle
        // mouse toggles point-detonate and proximity. A third "Timed" step in the cycle made
        // the round airburst with no [Proximity] suffix, which reads as the two modes being
        // swapped, and left proximity two presses away whenever the cycle stopped on it.
        private static AmpFuzeMode Next(AmpFuzeMode current)
        {
            return current == AmpFuzeMode.Impact ? AmpFuzeMode.Proximity : AmpFuzeMode.Impact;
        }
    }
}
