using System;
using M1A1Abrams;

internal static class AmpFuzeStateTest
{
    private static int checks;
    private static void Expect(AmpFuzeMode actual, AmpFuzeMode expected)
    {
        if (actual != expected) throw new Exception("Expected " + expected + ", got " + actual);
        checks++;
    }

    private static void Main()
    {
        // HUD, firing callback and Update can run in any order in a frame.
        var state = new AmpFuzeState();
        Expect(state.Read(true, false, 0), AmpFuzeMode.Impact);
        AmpFuzeMode shot = state.Read(true, true, 1);
        Expect(shot, AmpFuzeMode.Proximity);
        Expect(state.Read(true, true, 1), shot);
        Expect(state.Read(true, true, 1), shot);
        Expect(state.Read(true, false, 2), AmpFuzeMode.Proximity);
        Expect(state.Read(true, true, 3), AmpFuzeMode.Impact);
        Expect(shot, AmpFuzeMode.Proximity); // already fired round retains its snapshot
        Expect(state.Read(true, true, 4), AmpFuzeMode.Proximity);
        Expect(state.Read(true, false, 5), AmpFuzeMode.Proximity);
        Expect(state.Read(false, true, 6), AmpFuzeMode.Timed);
        Expect(state.Read(false, true, 7), AmpFuzeMode.Timed);
        Expect(state.Read(true, false, 8), AmpFuzeMode.Impact);
        Expect(new AmpFuzeState().Read(true, false, 9), AmpFuzeMode.Impact);
        Console.WriteLine("PASS: " + checks + " AMP fuze mode checks.");
    }
}
