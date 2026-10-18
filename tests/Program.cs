using System;
using Game.Core;

internal static class Program
{
    private static int Main()
    {
        RejectInvalidConfiguration();
        RejectInvalidElapsedTime();
        ProgressesOnlyAtFixedTicks();
        BoundsCatchUpAfterStall();
        Console.WriteLine("Core smoke tests passed.");
        return 0;
    }

    private static void RejectInvalidConfiguration()
    {
        ExpectThrows<ArgumentOutOfRangeException>(() => new SimulationConfig(0));
        ExpectThrows<ArgumentOutOfRangeException>(() => new SimulationConfig(60, 0));
    }

    private static void ProgressesOnlyAtFixedTicks()
    {
        var clock = new FixedTickClock(new SimulationConfig());
        int observed = 0;
        Assert(clock.Advance(1.0 / 120.0, _ => observed++) == 0, "Half tick must not advance.");
        Assert(clock.Advance(1.0 / 120.0, tick =>
        {
            observed++;
            Assert(tick == 1, "First emitted tick must be numbered one.");
        }) == 1, "Two half ticks must emit one tick.");
        Assert(observed == 1 && clock.Tick == 1, "Clock and callback must agree on tick count.");
    }

    private static void RejectInvalidElapsedTime()
    {
        var clock = new FixedTickClock(new SimulationConfig());
        ExpectThrows<ArgumentOutOfRangeException>(() => clock.Advance(double.NaN, _ => { }));
        ExpectThrows<ArgumentOutOfRangeException>(() => clock.Advance(double.PositiveInfinity, _ => { }));
        ExpectThrows<ArgumentOutOfRangeException>(() => clock.Advance(-0.01, _ => { }));
        ExpectThrows<ArgumentNullException>(() => clock.Advance(0, null!));
    }

    private static void BoundsCatchUpAfterStall()
    {
        var clock = new FixedTickClock(new SimulationConfig(60, 3));
        int emitted = clock.Advance(1, _ => { });
        Assert(emitted == 3 && clock.Tick == 3, "Catch-up work must respect configured limit.");
        while (clock.Advance(0, _ => { }) > 0) { }
        Assert(clock.Tick == 60, "Catch-up limit must defer work without skipping authoritative ticks.");
        Assert(clock.InterpolationAlpha >= 0 && clock.InterpolationAlpha < 1, "Clock must retain only fractional time after catch-up.");
    }

    private static void ExpectThrows<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
