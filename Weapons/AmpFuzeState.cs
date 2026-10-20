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

        // Impact -> Proximity -> Timed -> Impact. The timed step is what makes the
        // airburst reachable with "AMP Proxy Fuze" enabled; without it the state
        // machine could only ever return Impact or Proximity.
        private static AmpFuzeMode Next(AmpFuzeMode current)
        {
            switch (current)
            {
                case AmpFuzeMode.Impact:
                    return AmpFuzeMode.Proximity;
                case AmpFuzeMode.Proximity:
                    return AmpFuzeMode.Timed;
                default:
                    return AmpFuzeMode.Impact;
            }
        }
    }
}
