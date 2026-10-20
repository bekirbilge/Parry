using System;
using System.Collections.Generic;
using System.IO;
using Game.Core;
using Game.Editor;
using Game.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Game.Unity.Tests
{
    public sealed class PlayableSceneTests
    {
        [Test]
        public void PhoneMatchCompleteRestartUsesVisibleTargetOnly()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var staticFlags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            var preview = typeof(GameController).GetField("_forceTouchPreview", flags);
            var handle = typeof(GameController).GetMethod("HandleToolbar", flags);
            var restartTarget = typeof(GameController).GetMethod("PhoneRestartButton", staticFlags);
            preview.SetValue(controller, true);
            controller.RestartMatch();
            SimulationState complete = controller.CaptureSnapshot();
            complete.Phase = MatchPhase.MatchComplete;
            complete.MatchWinnerId = 0;
            controller.Sim.RestoreSnapshot(complete);
            Rect arena = new Rect(0, 0, 1280, 590);
            Rect target = (Rect)restartTarget.Invoke(null, new object[] { arena });
            Assert.That(target.width, Is.GreaterThan(100));
            Assert.That(target.height, Is.GreaterThan(40));
            Assert.That(arena.Contains(target.center), Is.True);
            Vector2 unrelated = new Vector2(arena.center.x, arena.yMax - 30);
            Assert.That(target.Contains(unrelated), Is.False);
            handle.Invoke(controller, new object[] { arena,
                new Vector2(unrelated.x, Screen.height - unrelated.y) });
            Assert.That(controller.Sim.Phase, Is.EqualTo(MatchPhase.MatchComplete));
            Vector2 tap = target.center;
            var tapInput = new object[] { arena, new Vector2(tap.x, Screen.height - tap.y) };
            try
            {
                typeof(GameController).GetMethod("TogglePause", flags).Invoke(controller, null);
                handle.Invoke(controller, tapInput);
                Assert.That(controller.Sim.Phase, Is.EqualTo(MatchPhase.MatchComplete));
                typeof(GameController).GetMethod("ToggleTuning", flags).Invoke(controller, null);
                handle.Invoke(controller, tapInput);
                Assert.That(controller.Sim.Phase, Is.EqualTo(MatchPhase.MatchComplete));
                typeof(GameController).GetMethod("TogglePause", flags).Invoke(controller, null);
                handle.Invoke(controller, tapInput);
                Assert.That(controller.Sim.Phase, Is.EqualTo(MatchPhase.Active));
                Assert.That(controller.CaptureSnapshot().Tick, Is.EqualTo(0));
            }
            finally { Time.timeScale = 1; }
        }

        [Test]
        public void PlayableParryRadiusIsSixWithUnchangedWindow()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.RestartMatch();
            Assert.That(controller.Tuning.Arena.FighterRadiusUnits, Is.EqualTo(2.5));
            Assert.That(controller.Tuning.Defense.ParryExtraReachUnits, Is.EqualTo(3.5));
            Assert.That(controller.Tuning.Arena.FighterRadiusUnits +
                controller.Tuning.Defense.ParryExtraReachUnits, Is.EqualTo(6));
            Assert.That(controller.Tuning.Movement.PerfectParryWindowSeconds, Is.EqualTo(.16));
        }

        [Test]
        public void HighDensityPhoneTuningKeepsReadableControlsAndScrollArea()
        {
            Rect arena = new Rect(70, 35, 2800, 975);
            var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            Type controller = typeof(GameController);
            Rect back = (Rect)controller.GetMethod("PhoneTuningBack", flags).Invoke(null, new object[] { arena });
            Rect apply = (Rect)controller.GetMethod("PhoneTuningApply", flags).Invoke(null, new object[] { arena });
            Rect scroll = (Rect)controller.GetMethod("PhoneTuningScrollArea", flags).Invoke(null, new object[] { arena });
            int font = (int)controller.GetMethod("PhoneTuningFontSize", flags).Invoke(null, new object[] { arena });
            float scale = arena.height / 360f;
            Assert.That(font, Is.GreaterThanOrEqualTo(54));
            Assert.That(back.height, Is.GreaterThanOrEqualTo(44 * scale));
            Assert.That(apply.height, Is.GreaterThanOrEqualTo(44 * scale));
            Assert.That(scroll.height, Is.GreaterThan(0));
            Assert.That(scroll.yMin, Is.GreaterThan(back.yMax));
            Assert.That(scroll.yMax, Is.LessThan(apply.yMin));
            Assert.That(ArenaLayout.TuningPanel(arena, true).Contains(back.center), Is.True);
            Assert.That(ArenaLayout.TuningPanel(arena, true).Contains(apply.center), Is.True);
        }

        [Test]
        public void RestartFromPausedTuningResumesAndResetsMatch()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.RestartMatch();
            controller.StepOnce(new SimulationInput(0, default(FighterInput), default(FighterInput)));
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(GameController).GetMethod("TogglePause", flags).Invoke(controller, null);
            typeof(GameController).GetMethod("ToggleTuning", flags).Invoke(controller, null);
            try
            {
                Assert.That(Time.timeScale, Is.EqualTo(0));
                // Close tuning with the visible pause control, reopen the menu, then tap Restart.
                typeof(GameController).GetField("_forceTouchPreview", flags).SetValue(controller, true);
                Rect arena = new Rect(0, 0, 1280, 590);
                var handle = typeof(GameController).GetMethod("HandleToolbar", flags);
                Vector2 restart = ArenaLayout.PauseMenuButton(arena, 3).center;
                handle.Invoke(controller, new object[] { arena, new Vector2(restart.x, Screen.height - restart.y) });
                Assert.That(controller.CaptureSnapshot().Tick, Is.EqualTo(1), "Tuning must block hidden menu buttons.");
                Assert.That(typeof(GameController).GetField("_showTuning", flags).GetValue(controller), Is.True);
                Vector2 pause = ArenaLayout.PauseButton(arena).center;
                handle.Invoke(controller, new object[] { arena, new Vector2(pause.x, Screen.height - pause.y) });
                handle.Invoke(controller, new object[] { arena, new Vector2(pause.x, Screen.height - pause.y) });
                handle.Invoke(controller, new object[] { arena, new Vector2(restart.x, Screen.height - restart.y) });
                Assert.That(controller.CaptureSnapshot().Tick, Is.EqualTo(0));
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(typeof(GameController).GetField("_paused", flags).GetValue(controller), Is.False);
                Assert.That(typeof(GameController).GetField("_showTuning", flags).GetValue(controller), Is.False);
            }
            finally { Time.timeScale = 1; }
        }

        [Test]
        public void BootstrapSceneLoadsWithOneClockControllerAndOrthographicCamera()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            BootstrapPipeline.ValidateBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            Assert.That(Count<FixedTickDriver>(scene), Is.EqualTo(1));
            Assert.That(Count<GameController>(scene), Is.EqualTo(1));
            Assert.That(Count<Camera>(scene), Is.EqualTo(1));
            Assert.That(Count<AudioListener>(scene), Is.EqualTo(1));
            Assert.That(Find<Camera>(scene).orthographic, Is.True);
            Assert.That(EditorBuildSettings.scenes[0].enabled, Is.True);
        }

        [Test]
        public void SnapshotAdapterAdvancesExactlyOneAuthoritativeTick()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.RestartMatch();
            long before = controller.CaptureSnapshot().Tick;
            Assert.That(controller.Sim.Tick, Is.EqualTo(before));
            controller.StepOnce(new SimulationInput(before, default(FighterInput), default(FighterInput)));
            Assert.That(controller.CaptureSnapshot().Tick, Is.EqualTo(before + 1));
            Assert.That(controller.Sim.Tick, Is.EqualTo(before + 1));
        }

        [Test]
        public void ImmediateTestingStartsMovableAndCountdownModeAppliesOnRestart()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.PrototypeFuelBoost = false;
            controller.RestartMatch();
            Assert.That(controller.PrototypeInstantTestingStart, Is.True);
            Assert.That(controller.Tuning.Match.StartImmediatelyForTesting, Is.True);
            Assert.That(controller.Sim.Phase, Is.EqualTo(MatchPhase.Active));
            string capturePath = Path.Combine(Application.persistentDataPath, "Playtests", "tuning.json");
            string capture = File.ReadAllText(capturePath);
            Assert.That(capture, Does.Contain("\"immediateTestStart\": true"));
            Assert.That(capture, Does.Contain("\"boostDuration\""));
            Assert.That(capture, Does.Contain("\"boostInvulnerability\""));
            Assert.That(capture, Does.Contain("\"boostLoadDuration\""));
            Assert.That(capture, Does.Contain("\"projectileDecaySecondsPerLevel\""));
            Assert.That(capture, Does.Contain("\"projectileLevel3Speed\""));
            Assert.That(capture, Does.Contain("\"projectileSpeedDecay\": 12"));
            Assert.That(capture, Does.Contain("\"projectileRadius\": 0.9"));
            Assert.That(capture, Does.Contain("\"fighterRadius\": 2.5"));
            Assert.That(controller.Tuning.Arena.ProjectileRadiusUnits, Is.EqualTo(.9));
            Assert.That(controller.Tuning.Arena.FighterRadiusUnits, Is.EqualTo(2.5));
            Assert.That(controller.CaptureSnapshot().Fighter(0).Position.Y, Is.EqualTo(2.5));

            SimulationState before = controller.CaptureSnapshot();
            Assert.That(before.Fighter(0).DashCharges, Is.EqualTo(3));
            Assert.That(before.Fighter(1).DashCharges, Is.EqualTo(3));
            controller.StepOnce(new SimulationInput(before.Tick,
                new FighterInput(new Vec2(1, 0), dashPressed: true), default(FighterInput)));
            Assert.That(controller.CaptureSnapshot().Fighter(0).Position.X,
                Is.GreaterThan(before.Fighter(0).Position.X));
            Assert.That(controller.CaptureSnapshot().Fighter(0).DashCharges, Is.EqualTo(2));

            controller.PrototypeInstantTestingStart = false;
            Assert.That(controller.Tuning.Match.StartImmediatelyForTesting, Is.True);
            controller.RestartMatch();
            Assert.That(controller.Tuning.Match.StartImmediatelyForTesting, Is.False);
            Assert.That(controller.Sim.Phase, Is.EqualTo(MatchPhase.Countdown));
            SimulationState countdown = controller.CaptureSnapshot();
            Assert.That(countdown.Fighter(0).DashCharges, Is.EqualTo(3));
            Assert.That(countdown.Fighter(1).DashCharges, Is.EqualTo(3));
            controller.StepOnce(new SimulationInput(countdown.Tick,
                new FighterInput(new Vec2(1, 0)), default(FighterInput)));
            Assert.That(controller.CaptureSnapshot().Fighter(0).Position.X,
                Is.EqualTo(countdown.Fighter(0).Position.X));

            Assert.That(File.ReadAllText(capturePath), Does.Contain("\"immediateTestStart\": false"));
        }

        [Test]
        public void RopePreviewShowsLoadedBoostBetweenFixedAnchorsAndRebounds()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.RestartMatch();
            ulong liveChecksum = controller.CaptureSnapshot().ComputeChecksum();
            SimulationState preview = controller.CreateRopePreviewSnapshot();
            Assert.That(controller.CaptureSnapshot().ComputeChecksum(), Is.EqualTo(liveChecksum));
            double center = controller.Tuning.Arena.WidthUnits / 2.0;
            Assert.That(preview.Fighter(0).DashTicksRemaining > 0 ||
                preview.Fighter(0).DividerLoadTicksRemaining > 0, Is.True);
            Assert.That(preview.Fighter(0).DividerReboundDistanceRemaining, Is.EqualTo(0));
            Assert.That(preview.Fighter(0).Position.X - center, Is.GreaterThanOrEqualTo(4.0));
            Assert.That(controller.RopePreviewSawRebound, Is.True);
            Assert.That(controller.RopePreviewRecoilUnits, Is.GreaterThan(1));
            double bend = 0;
            for (int i = 0; i < DividerSystem.RopePointCount; i++)
            {
                Vec2 point = DividerSystem.GetRopePoint(preview, controller.Tuning, i);
                bend = Math.Max(bend, Math.Abs(point.X - center));
                if (i == 0 || i == DividerSystem.RopePointCount - 1)
                    Assert.That(point.X, Is.EqualTo(center).Within(1e-6));
                else
                    Assert.That(DividerSystem.CenterXAtHeight(preview, controller.Tuning, point.Y),
                        Is.EqualTo(point.X).Within(1e-6));
            }
            Assert.That(DividerSystem.GetRopePoint(preview, controller.Tuning, 1).Y, Is.EqualTo(0));
            Assert.That(DividerSystem.GetRopePoint(preview, controller.Tuning,
                DividerSystem.RopePointCount - 2).Y, Is.EqualTo(controller.Tuning.Arena.HeightUnits));
            Assert.That(bend, Is.GreaterThanOrEqualTo(4.0));
        }

        [Test]
        public void BotDifficultyChangesInputChoiceWithoutChangingSimulationStateOrTuning()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.RestartMatch();
            Assert.That(controller.BotDifficulty, Is.EqualTo(0));

            MatchSimulation simulation = controller.Sim;
            CombatTuning tuning = controller.Tuning;
            SimulationState before = controller.CaptureSnapshot();
            ulong checksum = before.ComputeChecksum();
            controller.SetBotDifficulty(2);

            Assert.That(controller.BotDifficulty, Is.EqualTo(2));
            Assert.That(controller.Sim, Is.SameAs(simulation));
            Assert.That(controller.Tuning, Is.SameAs(tuning));
            SimulationState after = controller.CaptureSnapshot();
            Assert.That(after.Tick, Is.EqualTo(before.Tick));
            Assert.That(after.ComputeChecksum(), Is.EqualTo(checksum));
            for (int i = 0; i < before.Fighters.Length; i++)
            {
                Assert.That(after.Fighters[i].Posture, Is.EqualTo(before.Fighters[i].Posture));
                Assert.That(after.Fighters[i].DashCharges, Is.EqualTo(before.Fighters[i].DashCharges));
                Assert.That(after.Fighters[i].RoundWins, Is.EqualTo(before.Fighters[i].RoundWins));
            }

            controller.SetBotDifficulty(-5);
            Assert.That(controller.BotDifficulty, Is.EqualTo(0));
            controller.SetBotDifficulty(99);
            Assert.That(controller.BotDifficulty, Is.EqualTo(2));
            Assert.That(controller.CaptureSnapshot().ComputeChecksum(), Is.EqualTo(checksum));
        }

        [Test]
        public void ArenaViewportStaysWithinSafeAreaAtBothLandscapeRatios()
        {
            AssertLayout(1920, 1080, new Rect(0, 0, 1920, 1080));
            AssertLayout(1280, 720, new Rect(0, 0, 1280, 720));
            AssertLayout(2535, 1170, new Rect(120, 30, 2295, 1100));
            AssertLayout(2535, 1170, new Rect(0, 0, 2535, 1170));
        }

        [Test]
        public void ReferenceFighterKeepsCenterAndGroundedSoleAcrossLandscapeViewports()
        {
            AssertArtLayout(1920, 1080, new Rect(0, 0, 1920, 1080));
            AssertArtLayout(2535, 1170, new Rect(120, 30, 2295, 1100));
            AssertArtLayout(1280, 720, new Rect(0, 0, 1280, 720));
        }

        [Test]
        public void ReferenceArtTogglePreservesAuthoritativeSnapshot()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.RestartMatch();
            MatchSimulation simulation = controller.Sim;
            ulong checksum = controller.CaptureSnapshot().ComputeChecksum();
            controller.ReferenceArtEnabled = false;
            Assert.That(controller.ReferenceArtEnabled, Is.False);
            controller.ReferenceArtEnabled = true;
            Assert.That(controller.ReferenceArtEnabled, Is.True);
            Assert.That(controller.Sim, Is.SameAs(simulation));
            Assert.That(controller.CaptureSnapshot().ComputeChecksum(), Is.EqualTo(checksum));
        }

        [Test]
        public void BothInputBackendsAreEnabled()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            Assert.That(assets, Is.Not.Empty);
            var inputHandler = new SerializedObject(assets[0]).FindProperty("activeInputHandler");
            Assert.That(inputHandler, Is.Not.Null);
            Assert.That(inputHandler.intValue, Is.EqualTo(2));
        }

        [Test]
        public void GamepadSouthDashesWithoutJumpingAndKeepsShortPressUntilTick()
        {
            Gamepad pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var adapter = new InputAdapter(0);
                Sample(adapter, pad, new GamepadState().WithButton(GamepadButton.South));
                Sample(adapter, pad, new GamepadState());
                FighterInput tick = adapter.ConsumeTick();
                Assert.That(tick.DashPressed, Is.True);
                Assert.That(tick.DashHeld, Is.False);
                Assert.That(tick.Move.LengthSquared, Is.EqualTo(0));
                Assert.That(tick.JumpPressed || tick.JumpHeld, Is.False);
                Assert.That(tick.DefensePressed || tick.DefenseHeld, Is.False);
                Assert.That(adapter.ConsumeTick().DashPressed, Is.False);
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [Test]
        public void BoostParryCatchesGrazingShotOutsideBodyWithoutChangingTiming()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.RestartMatch();
            SimulationState state = controller.CaptureSnapshot();
            Vec2 fighter = state.Fighter(0).Position;
            state.Projectiles.Add(new ProjectileState { Id = 951,
                Position = fighter + new Vec2(4, 2.8), Velocity = new Vec2(-96, 0),
                PowerLevel = 3, DecayTicksRemaining = 200 });
            controller.Sim.RestoreSnapshot(state);
            Gamepad pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var adapter = new InputAdapter(0);
                Sample(adapter, pad, new GamepadState { leftStick = Vector2.right }
                    .WithButton(GamepadButton.South).WithButton(GamepadButton.West));
                var events = controller.StepOnce(new SimulationInput(controller.Sim.Tick, adapter.ConsumeTick(), default));
                bool parried = false;
                foreach (SimulationEvent e in events)
                    if (e.Type == SimulationEventType.PerfectParry) parried = true;
                Assert.That(parried, Is.True, "Active boost immunity must not hide a larger-area parry.");
                Assert.That(controller.CaptureSnapshot().Projectiles[0].Velocity.X, Is.GreaterThan(0));
                Assert.That(controller.Tuning.Movement.PerfectParryWindowSeconds, Is.EqualTo(.16));
                Assert.That(controller.Tuning.Movement.DashInvulnerabilitySeconds, Is.EqualTo(.08));
                Assert.That(controller.Tuning.Defense.ParryExtraReachUnits, Is.EqualTo(3.5));
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [Test]
        public void GamepadLaunchKeepsFreeAngleAndOwnedSquareRedirectHasNoReward()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.PrototypeFuelBoost = false;
            controller.RestartMatch();
            Gamepad pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var adapter = new InputAdapter(0);
                Sample(adapter, pad, new GamepadState { leftStick = new Vector2(.9f, .3f) }
                    .WithButton(GamepadButton.South));
                controller.StepOnce(new SimulationInput(controller.Sim.Tick, adapter.ConsumeTick(), default));
                FighterState boosted = controller.CaptureSnapshot().Fighter(0);
                Assert.That(boosted.DashDirection.Y / boosted.DashDirection.X, Is.EqualTo(1.0 / 3).Within(.0001));

                controller.RestartMatch();
                adapter.Clear();
                SimulationState state = controller.CaptureSnapshot();
                state.Fighter(0).DashCharges = 1;
                state.Projectiles.Add(new ProjectileState { Id = 950,
                    Position = state.Fighter(0).Position + new Vec2(2.3, 0),
                    Velocity = new Vec2(-72, 0), PowerLevel = 1,
                    DecayTicksRemaining = 200, LastParrierId = 0 });
                controller.Sim.RestoreSnapshot(state);
                Sample(adapter, pad, new GamepadState().WithButton(GamepadButton.West));
                var events = controller.StepOnce(new SimulationInput(controller.Sim.Tick, adapter.ConsumeTick(), default));
                bool redirected = false;
                foreach (SimulationEvent e in events)
                {
                    Assert.That(e.Type, Is.Not.EqualTo(SimulationEventType.PerfectParry));
                    if (e.Type == SimulationEventType.OwnedProjectileRedirected) redirected = true;
                }
                SimulationState after = controller.CaptureSnapshot();
                Assert.That(redirected, Is.True);
                Assert.That(after.Projectiles[0].Velocity.X, Is.GreaterThan(0));
                Assert.That(after.Projectiles[0].PowerLevel, Is.EqualTo(1));
                Assert.That(after.Projectiles[0].DecayTicksRemaining, Is.EqualTo(199));
                Assert.That(after.Fighter(0).DashCharges, Is.EqualTo(1));
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [Test]
        public void GamepadCrossAndSquareInOneTickBoostsAndParriesWithoutJumping()
        {
            Gamepad pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var adapter = new InputAdapter(0);
                GamepadState boostAndParry = new GamepadState()
                    .WithButton(GamepadButton.South)
                    .WithButton(GamepadButton.West);
                Sample(adapter, pad, boostAndParry);
                FighterInput tick = adapter.ConsumeTick();
                Assert.That(tick.DashPressed, Is.True);
                Assert.That(tick.DashHeld, Is.True);
                Assert.That(tick.DefensePressed && tick.DefenseHeld, Is.True);
                Assert.That(tick.JumpPressed || tick.JumpHeld, Is.False);
                Assert.That(tick.Move.LengthSquared, Is.EqualTo(0));
                Sample(adapter, pad, boostAndParry);
                FighterInput held = adapter.ConsumeTick();
                Assert.That(held.DashPressed || held.DefensePressed || held.JumpPressed, Is.False);
                Assert.That(held.DashHeld, Is.True);
                Assert.That(held.DefenseHeld, Is.True);
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [Test]
        public void HeldCrossConsumesFuelAcrossTicksAndSquareParriesDuringThrust()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.SetFuelBoostMode(true);
            SimulationState setup = controller.CaptureSnapshot();
            FighterState fighter = setup.Fighter(0);
            fighter.Position = fighter.PreviousPosition = fighter.TickStartPosition = new Vec2(25, 10);
            fighter.Grounded = false;
            setup.NextSpawnTick = setup.Tick + 1000;
            controller.Sim.RestoreSnapshot(setup);
            double full = controller.Tuning.Movement.FuelCapacitySeconds;
            Gamepad pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var adapter = new InputAdapter(0);
                GamepadState cross = new GamepadState { leftStick = Vector2.right }
                    .WithButton(GamepadButton.South);
                for (int i = 0; i < 12; i++)
                {
                    Sample(adapter, pad, cross);
                    FighterInput tick = adapter.ConsumeTick();
                    Assert.That(tick.DashHeld, Is.True);
                    Assert.That(tick.DashPressed, Is.EqualTo(i == 0));
                    controller.StepOnce(new SimulationInput(controller.Sim.Tick, tick, default));
                }
                fighter = controller.CaptureSnapshot().Fighter(0);
                Assert.That(fighter.FuelBoostActive, Is.True);
                Assert.That(fighter.FuelSeconds, Is.LessThan(full - .08));
                double beforeSteerY = fighter.Velocity.Y;
                GamepadState steerUp = new GamepadState { leftStick = Vector2.up }
                    .WithButton(GamepadButton.South);
                for (int i = 0; i < 3; i++)
                {
                    Sample(adapter, pad, steerUp);
                    FighterInput steering = adapter.ConsumeTick();
                    Assert.That(steering.DashHeld && !steering.DashPressed, Is.True);
                    controller.StepOnce(new SimulationInput(controller.Sim.Tick, steering, default));
                }
                fighter = controller.CaptureSnapshot().Fighter(0);
                Assert.That(fighter.Velocity.Y, Is.GreaterThan(beforeSteerY));

                SimulationState beforeParry = controller.CaptureSnapshot();
                fighter = beforeParry.Fighter(0);
                Vec2 incoming = fighter.Position + new Vec2(2.5, 0);
                beforeParry.Projectiles.Add(new ProjectileState
                {
                    Id = 703, Position = incoming, PreviousPosition = incoming,
                    Velocity = new Vec2(-24, 0)
                });
                controller.Sim.RestoreSnapshot(beforeParry);
                Sample(adapter, pad, steerUp.WithButton(GamepadButton.West));
                FighterInput crossAndSquare = adapter.ConsumeTick();
                Assert.That(crossAndSquare.DashHeld && crossAndSquare.DefensePressed, Is.True);
                Assert.That(crossAndSquare.JumpPressed || crossAndSquare.JumpHeld, Is.False);
                IReadOnlyList<SimulationEvent> events = controller.StepOnce(new SimulationInput(
                    controller.Sim.Tick, crossAndSquare, default));
                bool parried = false;
                foreach (SimulationEvent item in events)
                    if (item.Type == SimulationEventType.PerfectParry) parried = true;
                Assert.That(parried, Is.True);

                Sample(adapter, pad, new GamepadState());
                FighterInput released = adapter.ConsumeTick();
                Assert.That(released.DashHeld || released.DashPressed, Is.False);
                controller.StepOnce(new SimulationInput(controller.Sim.Tick, released, default));
                Assert.That(controller.CaptureSnapshot().Fighter(0).FuelBoostActive, Is.False);
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [Test]
        public void SwitchingToChargesRestartsRoundWithThreeCharges()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.SetFuelBoostMode(true);
            Assert.That(controller.Tuning.Movement.UseFuelBoost, Is.True);
            string capturePath = Path.Combine(Application.persistentDataPath, "Playtests", "tuning.json");
            string fuelCapture = File.ReadAllText(capturePath);
            Assert.That(fuelCapture, Does.Contain("\"boostMode\": \"fuel\""));
            Assert.That(fuelCapture, Does.Contain("\"fuelCapacitySeconds\""));
            controller.SetFuelBoostMode(false);
            Assert.That(controller.Tuning.Movement.UseFuelBoost, Is.False);
            Assert.That(File.ReadAllText(capturePath), Does.Contain("\"boostMode\": \"charges\""));
            Assert.That(controller.CaptureSnapshot().Fighter(0).DashCharges, Is.EqualTo(3));
            Assert.That(controller.CaptureSnapshot().Fighter(1).DashCharges, Is.EqualTo(3));
            controller.StepOnce(new SimulationInput(controller.Sim.Tick,
                new FighterInput(new Vec2(1, 0), dashPressed: true), default));
            Assert.That(controller.CaptureSnapshot().Fighter(0).DashCharges, Is.EqualTo(2));
            controller.RestartMatch();
            Assert.That(controller.CaptureSnapshot().Fighter(0).DashCharges, Is.EqualTo(3));
        }

        [Test]
        public void ControllerRoutesMidBoostSquareParryAfterInvulnerabilityEnds()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.PrototypeFuelBoost = false;
            controller.RestartMatch();
            SimulationState setup = controller.CaptureSnapshot();
            FighterState fighter = setup.Fighter(0);
            fighter.Position = fighter.PreviousPosition = fighter.TickStartPosition = new Vec2(25, 10);
            fighter.Grounded = false;
            setup.NextSpawnTick = setup.Tick + 1000;
            controller.Sim.RestoreSnapshot(setup);

            controller.StepOnce(new SimulationInput(controller.Sim.Tick,
                new FighterInput(new Vec2(1, 0), dashPressed: true), default(FighterInput)));
            for (int i = 0; i < 20 && controller.CaptureSnapshot().Fighter(0).DashInvulnerabilityTicksRemaining > 0; i++)
                controller.StepOnce(new SimulationInput(controller.Sim.Tick,
                    new FighterInput(new Vec2(1, 0)), default(FighterInput)));
            SimulationState beforeParry = controller.CaptureSnapshot();
            fighter = beforeParry.Fighter(0);
            Assert.That(fighter.DashInvulnerabilityTicksRemaining, Is.EqualTo(0));
            Assert.That(fighter.DashTicksRemaining, Is.GreaterThan(0));
            Assert.That(fighter.DashCharges, Is.EqualTo(2));
            double contactRadius = controller.Tuning.Arena.FighterRadiusUnits +
                controller.Tuning.Arena.ProjectileRadiusUnits;
            Vec2 incoming = new Vec2(fighter.Position.X + contactRadius + .5, fighter.Position.Y);
            beforeParry.Projectiles.Add(new ProjectileState
            {
                Id = 7, Position = incoming, PreviousPosition = incoming,
                Velocity = new Vec2(-24, 0)
            });
            controller.Sim.RestoreSnapshot(beforeParry);
            IReadOnlyList<SimulationEvent> events;
            Gamepad pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var adapter = new InputAdapter(0);
                Sample(adapter, pad, new GamepadState { leftStick = new Vector2(1, 0) }
                    .WithButton(GamepadButton.West));
                FighterInput square = adapter.ConsumeTick();
                Assert.That(square.DefensePressed && square.DefenseHeld, Is.True);
                Assert.That(square.JumpPressed || square.DashPressed, Is.False);
                events = controller.StepOnce(new SimulationInput(controller.Sim.Tick,
                    square, default(FighterInput)));
            }
            finally { InputSystem.RemoveDevice(pad); }
            bool parried = false;
            foreach (SimulationEvent item in events)
                if (item.Type == SimulationEventType.PerfectParry) parried = true;
            Assert.That(parried, Is.True);
            SimulationState afterParry = controller.CaptureSnapshot();
            Assert.That(afterParry.Fighter(0).Eliminated, Is.False);
            Assert.That(afterParry.Fighter(0).DashCharges, Is.EqualTo(3));
            Assert.That(afterParry.Projectiles[0].PowerLevel, Is.EqualTo(1));
        }

        [Test]
        public void ControllerNeutralBoostLaunchesUpwardWithNoJumpInput()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.PrototypeFuelBoost = false;
            controller.RestartMatch();
            SimulationState before = controller.CaptureSnapshot();
            controller.StepOnce(new SimulationInput(before.Tick,
                new FighterInput(Vec2.Zero, dashPressed: true), default(FighterInput)));
            FighterState after = controller.CaptureSnapshot().Fighter(0);
            Assert.That(after.DashDirection.Y, Is.GreaterThan(0));
            Assert.That(after.Position.Y, Is.GreaterThan(before.Fighter(0).Position.Y));
            Assert.That(after.DashCharges, Is.EqualTo(2));
        }

        [Test]
        public void GamepadWestParriesOnceThenHoldsGuardUntilReleased()
        {
            Gamepad pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var adapter = new InputAdapter(0);
                GamepadState west = new GamepadState().WithButton(GamepadButton.West);
                Sample(adapter, pad, west);
                FighterInput first = adapter.ConsumeTick();
                Assert.That(first.DefensePressed && first.DefenseHeld, Is.True);
                Assert.That(first.DashPressed || first.JumpPressed, Is.False);
                Sample(adapter, pad, west);
                FighterInput held = adapter.ConsumeTick();
                Assert.That(held.DefensePressed, Is.False);
                Assert.That(held.DefenseHeld, Is.True);
                Sample(adapter, pad, new GamepadState());
                Assert.That(adapter.ConsumeTick().DefenseHeld, Is.False);
                Sample(adapter, pad, west);
                Assert.That(adapter.ConsumeTick().DefensePressed, Is.True);
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [Test]
        public void GamepadNorthIsUnassignedAndStickIsClamped()
        {
            Gamepad pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var adapter = new InputAdapter(0);
                GamepadState northAndStick = new GamepadState { leftStick = new Vector2(1, 1) }
                    .WithButton(GamepadButton.North);
                Sample(adapter, pad, northAndStick);
                FighterInput first = adapter.ConsumeTick();
                Assert.That(first.JumpPressed || first.JumpHeld || first.DashPressed ||
                    first.DefensePressed || first.DefenseHeld, Is.False);
                Assert.That(first.Move.X, Is.GreaterThan(0.5));
                Assert.That(first.Move.Y, Is.GreaterThan(0.5));
                Assert.That(first.Move.LengthSquared, Is.LessThanOrEqualTo(1.0001));
                Sample(adapter, pad, northAndStick);
                FighterInput held = adapter.ConsumeTick();
                Assert.That(held.JumpPressed || held.JumpHeld || held.DashPressed ||
                    held.DefensePressed || held.DefenseHeld, Is.False);
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        [Test]
        public void GamepadNeverControlsPlayerTwo()
        {
            Gamepad pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var playerTwo = new InputAdapter(1);
                GamepadState allButtons = new GamepadState { leftStick = new Vector2(1, 0) }
                    .WithButton(GamepadButton.South)
                    .WithButton(GamepadButton.West)
                    .WithButton(GamepadButton.North);
                Sample(playerTwo, pad, allButtons);
                FighterInput tick = playerTwo.ConsumeTick();
                Assert.That(tick.Move.X, Is.EqualTo(0));
                Assert.That(tick.JumpPressed || tick.JumpHeld || tick.DashPressed ||
                    tick.DefensePressed || tick.DefenseHeld, Is.False);
            }
            finally { InputSystem.RemoveDevice(pad); }
        }

        private static void Sample(InputAdapter adapter, Gamepad pad, GamepadState state)
        {
            InputSystem.QueueStateEvent(pad, state);
            InputSystem.Update();
            adapter.SampleFrame(false, pad);
        }

        private static void AssertLayout(int width, int height, Rect safe)
        {
            Rect gui = ArenaLayout.Compute(width, height, safe);
            Assert.That(gui.width, Is.GreaterThan(0));
            Assert.That(gui.height, Is.GreaterThan(0));
            Assert.That(gui.width / gui.height, Is.EqualTo(16f / 9f).Within(0.0001f));
            Assert.That(gui.xMin, Is.GreaterThanOrEqualTo(safe.xMin - 0.01f));
            Assert.That(gui.xMax, Is.LessThanOrEqualTo(safe.xMax + 0.01f));
            Assert.That(gui.yMin, Is.GreaterThanOrEqualTo(height - safe.yMax - 0.01f));
            Assert.That(gui.yMax, Is.LessThanOrEqualTo(height - safe.yMin + 0.01f));
            Rect footer = ArenaLayout.Footer(width, height, safe);
            Assert.That(footer.height, Is.GreaterThanOrEqualTo(40));
            Assert.That(footer.xMin, Is.EqualTo(gui.xMin).Within(0.01f));
            Assert.That(footer.width, Is.EqualTo(gui.width).Within(0.01f));
            Assert.That(footer.yMin, Is.GreaterThanOrEqualTo(gui.yMax + 1));
            Assert.That(footer.yMax, Is.LessThanOrEqualTo(height - safe.yMin + 0.01f));
        }

        private static void AssertArtLayout(int width, int height, Rect safe)
        {
            Rect arena = ArenaLayout.Compute(width, height, safe);
            float unit = arena.width / 100f;
            ArtPresentation.CharacterLayout layout = ArtPresentation.LoadCharacterLayout(out bool loaded);
            Assert.That(loaded, Is.True, "The playable proof must load its reference art layout metadata.");
            float projectedCenterX = arena.xMin + 20f * unit;
            float groundY = arena.yMax;
            Assert.That(layout.VisibleHeightFraction, Is.GreaterThan(0f));
            Assert.That(layout.VisibleHeightFraction + layout.SoleInsetFraction, Is.LessThanOrEqualTo(1f));
            Assert.That(layout.ArenaBottomCropFraction, Is.GreaterThan(0f).And.LessThanOrEqualTo(.25f));
            Rect arenaUv = layout.ArenaUvRect;
            Assert.That(arenaUv.xMin, Is.EqualTo(0f));
            Assert.That(arenaUv.xMax, Is.EqualTo(1f));
            Assert.That(arenaUv.yMin, Is.EqualTo(layout.ArenaBottomCropFraction).Within(1e-6f));
            Assert.That(arenaUv.yMax, Is.EqualTo(1f).Within(1e-6f));
            Rect fallbackUv = ArtPresentation.FallbackCharacterLayout.ArenaUvRect;
            Assert.That(fallbackUv, Is.EqualTo(new Rect(0f, 0f, 1f, 1f)));
            foreach (float radius in new[] { 1.5f, 2.5f })
            {
                var center = new Vec2(20, radius);
                Rect art = ArtPresentation.FighterRect(arena, center, 100f, 56.25f, radius, layout);
                Assert.That(art.center.x, Is.EqualTo(projectedCenterX).Within(.01f));
                Assert.That(art.width, Is.EqualTo(art.height).Within(.01f));
                float visibleHeight = art.height * layout.VisibleHeightFraction;
                float soleY = art.yMin + art.height * (1f - layout.SoleInsetFraction);
                Assert.That(soleY, Is.EqualTo(groundY).Within(.01f));
                Assert.That(visibleHeight, Is.EqualTo(2f * radius * unit).Within(.01f));
                Assert.That(soleY - visibleHeight,
                    Is.EqualTo(groundY - 2f * radius * unit).Within(.01f));
            }
        }

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T item = root.GetComponentInChildren<T>(true);
                if (item != null) return item;
            }
            return null;
        }

        private static int Count<T>(Scene scene) where T : Component
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
                count += root.GetComponentsInChildren<T>(true).Length;
            return count;
        }
    }
}
