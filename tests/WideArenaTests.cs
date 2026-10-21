using System;
using System.Collections.Generic;
using Game.Core;

internal static class WideArenaTests
{
    private static readonly SimulationConfig Config = new SimulationConfig();
    public static void Run()
    {
        GeometryAndBotUseAppliedWidth();
        WideReplayAndBounds();
    }

    private static CombatTuning Tune(double width) => new CombatTuning(
        new MovementTuning(useFuelBoost: true, fuelCapacitySeconds: 1.8), new ProjectileTuning(),
        new DefenseTuning(parryExtraReachUnits: 3.5), new DividerTuning(),
        new ArenaTuning(widthUnits: width, fighterRadiusUnits: 2.5, projectileRadiusUnits: .9),
        new MatchTuning(roundCountdownSeconds: 0, startImmediatelyForTesting: true));

    private static void GeometryAndBotUseAppliedWidth()
    {
        var tuning = Tune(160);
        var sim = new MatchSimulation(Config, tuning);
        SimulationState s = sim.CaptureSnapshot();
        Check(s.Fighter(0).Position.X == 40 && s.Fighter(1).Position.X == 120,
            "Wide arena spawns must use quarter positions of the actual width.");
        Check(Math.Abs(DividerSystem.CenterXAtHeight(s, tuning, 20) - 80) < 1e-8,
            "Wide divider center must follow actual arena width.");
        s.Fighter(0).Position = s.Fighter(0).PreviousPosition = new Vec2(65, 2.5);
        s.Fighter(1).Position = s.Fighter(1).PreviousPosition = new Vec2(95, 2.5);
        s.Tick = 600;
        Check(DebugBot.Decide(s, 0, 0, true, tuning).Move.X == 0 &&
            DebugBot.Decide(s, 1, 0, true, tuning).Move.X == 0,
            "Bots must use the wide divider instead of drifting toward old width100 positions.");
        s.Projectiles.Add(new ProjectileState { Id = 1,
            Position = new Vec2(158.9, 30), Velocity = new Vec2(80, 0) });
        var events = new List<SimulationEvent>();
        CombatSystem.Advance(s, new SimulationInput(s.Tick, default, default), tuning, Config, events);
        Check(s.Projectiles[0].Velocity.X < 0 && s.Projectiles[0].Position.X <= 159.1,
            "Projectile ricochet must happen at the widened outer wall.");
    }

    private static void WideReplayAndBounds()
    {
        foreach (double width in new[] { 125.0, 150.0, 180.0 })
        {
            CombatTuning tuning = Tune(width);
            var first = new MatchSimulation(Config, tuning);
            var replay = new MatchSimulation(Config, tuning);
            int rounds = 0, ticks = 0;
            for (; rounds < 32 && ticks < 400000; ticks++)
            {
                SimulationState s = first.CaptureSnapshot();
                var input = new SimulationInput(s.Tick,
                    DebugBot.Decide(s, 0, 1, true, tuning), DebugBot.Decide(s, 1, 1, true, tuning));
                first.Step(input); replay.Step(input);
                SimulationState next = first.CaptureSnapshot();
                Check(next.ComputeChecksum() == replay.CaptureSnapshot().ComputeChecksum(),
                    "Wide arena repeated input must remain reproducible.");
                foreach (FighterState f in next.Fighters)
                    Check(Finite(f.Position.X) && Finite(f.Position.Y) &&
                        f.Position.X >= tuning.Arena.FighterRadiusUnits - 1e-6 &&
                        f.Position.X <= width - tuning.Arena.FighterRadiusUnits + 1e-6 &&
                        f.Position.Y >= tuning.Arena.FighterRadiusUnits - 1e-6 &&
                        f.Position.Y <= tuning.Arena.HeightUnits - tuning.Arena.FighterRadiusUnits + 1e-6,
                        "Wide fighter positions must remain finite and inside actual walls.");
                foreach (ProjectileState p in next.Projectiles)
                    Check(Finite(p.Position.X) && Finite(p.Position.Y) &&
                        p.Position.X >= tuning.Arena.ProjectileRadiusUnits - 1e-6 &&
                        p.Position.X <= width - tuning.Arena.ProjectileRadiusUnits + 1e-6,
                        "Wide projectiles must stay inside actual horizontal walls.");
                if (s.Phase == MatchPhase.Active && next.Phase == MatchPhase.RoundResult) rounds++;
                if (ticks % 1000 == 0) replay.RestoreSnapshot(next);
                if (next.Phase == MatchPhase.MatchComplete)
                {
                    first = new MatchSimulation(Config, tuning);
                    replay = new MatchSimulation(Config, tuning);
                }
            }
            Check(rounds == 32, "Wide bot trial must finish32 rounds at width" + width + " (completed " + rounds + ")");
            Console.WriteLine("Wide arena " + width + ": " + rounds + " rounds / " + ticks + " ticks passed.");
        }
    }
    private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
}
