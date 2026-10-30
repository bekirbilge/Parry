using System.IO;
using Game.Core;
using Game.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class FloorFirePresentationTests
    {
        private static GameController Open()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            var scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<GameController>() != null) return root.GetComponent<GameController>();
            Assert.Fail("Playable controller missing");
            return null;
        }

        [Test]
        public void TrialStartsEnabledAndToggleRestartsWithCapturedConfiguration()
        {
            var controller = Open();
            controller.RestartMatch();
            Assert.That(controller.Tuning.FloorHazard.Enabled, Is.True);
            controller.StepOnce(new SimulationInput(controller.Sim.Tick, default, default));
            controller.SetFloorFireMode(false);
            Assert.That(controller.Sim.Tick, Is.Zero);
            Assert.That(controller.CaptureSnapshot().FloorHazard.ElapsedActiveTicks, Is.Zero);
            Assert.That(controller.Tuning.FloorHazard.Enabled, Is.False);
            string capture = File.ReadAllText(Path.Combine(Application.persistentDataPath, "Playtests", "tuning.json"));
            Assert.That(capture, Does.Contain("\"floorFireEnabled\": false"));
            controller.SetFloorFireMode(true);
            Assert.That(controller.Tuning.FloorHazard.WarningSeconds, Is.EqualTo(2));
            Assert.That(controller.Tuning.FloorHazard.ActiveSeconds, Is.EqualTo(1.25));
        }

        [Test]
        public void EnabledPlayableFireEliminatesBothGuardingPlayersAsDraw()
        {
            var controller = Open();
            controller.RestartMatch();
            var setup = controller.CaptureSnapshot();
            setup.NextSpawnTick = 100000;
            setup.FloorHazard.ElapsedActiveTicks = SimMath.SecondsToTicks(
                controller.Tuning.FloorHazard.InitialDelaySeconds + controller.Tuning.FloorHazard.WarningSeconds,
                controller.Sim.Config.TickRateHz);
            controller.Sim.RestoreSnapshot(setup);
            var guard = new FighterInput(Vec2.Zero, defensePressed: true, defenseHeld: true);
            var events = controller.StepOnce(new SimulationInput(controller.Sim.Tick, guard, guard));
            int hits = 0;
            foreach (var item in events) if (item.Type == SimulationEventType.FloorHazardHit) hits++;
            Assert.That(hits, Is.EqualTo(2));
            var after = controller.CaptureSnapshot();
            foreach (var fighter in after.Fighters)
            {
                Assert.That(fighter.Eliminated, Is.True);
                Assert.That(fighter.Posture, Is.Zero);
                Assert.That(fighter.Velocity.Y, Is.Zero);
            }
            ulong checksum = after.ComputeChecksum();
            Assert.That(FloorFirePresentation.DisplayFrame(after, controller.Tuning, controller.Sim.Config).Phase,
                Is.EqualTo(FloorHazardPhase.Active), "The result should still show what burned the players.");
            Assert.That(after.ComputeChecksum(), Is.EqualTo(checksum));
            Assert.That(after.Phase, Is.EqualTo(MatchPhase.RoundResult));
            Assert.That(after.RoundWinnerId, Is.EqualTo(-1));
            Assert.That(after.Fighter(0).RoundWins + after.Fighter(1).RoundWins, Is.Zero);
        }

        [Test]
        public void TwoWarningFlashesPrecedeActiveFire()
        {
            int starts = 0;
            bool previous = false;
            for (int elapsed = 0; elapsed < 120; elapsed++)
            {
                bool lit = FloorFirePresentation.WarningFlashLit(120 - elapsed, 120);
                if (lit && !previous) starts++;
                previous = lit;
            }
            Assert.That(starts, Is.EqualTo(2));
            Assert.That(FloorFirePresentation.WarningFlashLit(0, 120), Is.False);
        }

        [Test]
        public void FinalActiveTickStillShowsFireOnTheResultFrame()
        {
            var controller = Open();
            controller.RestartMatch();
            var state = controller.CaptureSnapshot();
            state.Phase = MatchPhase.RoundResult;
            state.FloorHazard.ElapsedActiveTicks = SimMath.SecondsToTicks(
                controller.Tuning.FloorHazard.InitialDelaySeconds + controller.Tuning.FloorHazard.WarningSeconds +
                controller.Tuning.FloorHazard.ActiveSeconds, controller.Sim.Config.TickRateHz);
            ulong checksum = state.ComputeChecksum();
            Assert.That(FloorFirePresentation.DisplayFrame(state, controller.Tuning, controller.Sim.Config).Phase,
                Is.EqualTo(FloorHazardPhase.Active));
            Assert.That(state.ComputeChecksum(), Is.EqualTo(checksum));
        }

        [Test]
        public void FireImpactSelectsRecoilWithoutChangingCombatState()
        {
            var state = new SimulationState();
            var fighter = state.Fighter(0);
            fighter.Grounded = false;
            fighter.Velocity = new Vec2(0, 36);
            ulong before = state.ComputeChecksum();
            var animation = new FighterAnimationPresentation(0);
            animation.Observe(new[] { new SimulationEvent(10, SimulationEventType.FloorHazardHit, 0) }, 60);
            Assert.That(animation.Sample(fighter, 11, 60, true, .1).Pose, Is.EqualTo(FighterAnimationPose.Recoil));
            Assert.That(state.ComputeChecksum(), Is.EqualTo(before));
        }
    }
}
