using System;
using System.IO;
using Game.Core;
using Game.Editor;
using Game.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Unity.Tests
{
    public sealed class PlayableSceneTests
    {
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
            controller.RestartMatch();
            Assert.That(controller.PrototypeInstantTestingStart, Is.True);
            Assert.That(controller.Tuning.Match.StartImmediatelyForTesting, Is.True);
            Assert.That(controller.Sim.Phase, Is.EqualTo(MatchPhase.Active));
            string capturePath = Path.Combine(Application.persistentDataPath, "Playtests", "tuning.json");
            Assert.That(File.ReadAllText(capturePath), Does.Contain("\"immediateTestStart\": true"));

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
        public void RopePreviewBendsBetweenFixedAnchorsAndMatchesCollisionSampling()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            Scene scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = Find<GameController>(scene);
            controller.RestartMatch();
            ulong liveChecksum = controller.CaptureSnapshot().ComputeChecksum();
            SimulationState preview = controller.CreateRopePreviewSnapshot();
            Assert.That(controller.CaptureSnapshot().ComputeChecksum(), Is.EqualTo(liveChecksum));
            double center = controller.Tuning.Arena.WidthUnits / 2.0;
            double bend = 0;
            for (int i = 0; i < DividerSystem.RopePointCount; i++)
            {
                Vec2 point = DividerSystem.GetRopePoint(preview, controller.Tuning, i);
                bend = Math.Max(bend, Math.Abs(point.X - center));
                Assert.That(DividerSystem.CenterXAtHeight(preview, controller.Tuning, point.Y),
                    Is.EqualTo(point.X).Within(1e-6));
                if (i == 0 || i == DividerSystem.RopePointCount - 1)
                    Assert.That(point.X, Is.EqualTo(center).Within(1e-6));
            }
            Assert.That(bend, Is.GreaterThanOrEqualTo(0.75));
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
        public void JoystickAxesUseFirstControllerAndCorrectVerticalDirection()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");
            Assert.That(assets, Is.Not.Empty);
            var axes = new SerializedObject(assets[0]).FindProperty("m_Axes");
            AssertAxis(axes, "ParryGamepadX", 0, false);
            AssertAxis(axes, "ParryGamepadY", 1, true);
        }

        private static void AssertAxis(SerializedProperty axes, string name, int index, bool inverted)
        {
            for (int i = 0; i < axes.arraySize; i++)
            {
                var axis = axes.GetArrayElementAtIndex(i);
                if (axis.FindPropertyRelative("m_Name").stringValue != name) continue;
                Assert.That(axis.FindPropertyRelative("type").intValue, Is.EqualTo(2));
                Assert.That(axis.FindPropertyRelative("axis").intValue, Is.EqualTo(index));
                Assert.That(axis.FindPropertyRelative("joyNum").intValue, Is.EqualTo(1));
                Assert.That(axis.FindPropertyRelative("invert").boolValue, Is.EqualTo(inverted));
                Assert.That(axis.FindPropertyRelative("positiveButton").stringValue, Is.Empty);
                Assert.That(axis.FindPropertyRelative("negativeButton").stringValue, Is.Empty);
                return;
            }
            Assert.Fail("Input axis is missing: " + name);
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
