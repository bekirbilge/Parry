using System.Reflection;
using Game.Core;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class NinjaAnimationAtlasTests
    {
        [Test]
        public void SampleLoadsWithMeasuredFramesAndPixelImportSettings()
        {
            var atlas = NinjaAnimationAtlas.Load();
            Assert.That(atlas, Is.Not.Null);
            Assert.That(atlas.Layout.frames.Length, Is.EqualTo(24));
            Assert.That(atlas.Texture.width, Is.EqualTo(1024));
            Assert.That(atlas.Texture.height, Is.EqualTo(1536));
            var importer = (TextureImporter)AssetImporter.GetAtPath(
                "Assets/Game/Resources/Art/char_ninja_animation_sample_v01.png");
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
        }

        [Test]
        public void EveryPoseKeepsGroundAnchorAndMirrorsWithoutChangingScale()
        {
            var atlas = NinjaAnimationAtlas.Load();
            var arena = new Rect(40, 20, 1200, 675);
            for (int row = 0; row < 6; row++)
                for (int col = 0; col < 4; col++)
                {
                    Rect right = atlas.FighterRect(arena, new Vec2(25, 2.5), 100, 56.25f, 2.5f, row, col, true);
                    Rect left = atlas.FighterRect(arena, new Vec2(25, 2.5), 100, 56.25f, 2.5f, row, col, false);
                    Assert.That(right.yMax, Is.EqualTo(arena.yMax).Within(.001));
                    Assert.That(left.size, Is.EqualTo(right.size));
                    Assert.That(left.xMin + right.xMax, Is.EqualTo(2 * (arena.xMin + arena.width * .25f)).Within(.001));
                    Rect uv = atlas.Uv(row, col, true), mirrored = atlas.Uv(row, col, false);
                    Assert.That(uv.xMin, Is.GreaterThanOrEqualTo(0));
                    Assert.That(uv.yMin, Is.GreaterThanOrEqualTo(0));
                    Assert.That(uv.xMax, Is.LessThanOrEqualTo(1));
                    Assert.That(uv.yMax, Is.LessThanOrEqualTo(1));
                    Assert.That(mirrored.width, Is.EqualTo(-uv.width));
                }
        }

        [Test]
        public void InvalidMeasuredLayoutIsRejected()
        {
            var atlas = NinjaAnimationAtlas.Load();
            var altered = atlas.Layout;
            altered.frames[0].right = atlas.Texture.width + 1;
            Assert.That(NinjaAnimationAtlas.Valid(altered, atlas.Texture.width, atlas.Texture.height), Is.False);
            Assert.That(atlas.Uv(0, 0, true).xMax, Is.LessThanOrEqualTo(1),
                "Editing an exported metadata copy must not invalidate the loaded atlas.");
        }

        [Test]
        public void PresentationObserversLeaveAuthoritativeReplayUnchanged()
        {
            BootstrapPipeline.EnsureBootstrapScene();
            var scene = EditorSceneManager.OpenScene(BootstrapPipeline.ScenePath, OpenSceneMode.Single);
            GameController controller = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<GameController>() != null) controller = root.GetComponent<GameController>();
            Assert.That(controller, Is.Not.Null);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(GameController).GetField("_referenceCharacter", flags).SetValue(controller, null);
            typeof(GameController).GetField("_animationAtlas", flags).SetValue(controller, NinjaAnimationAtlas.Load());
            Assert.That(controller.HasAnimationSample && controller.HasCharacterPresentation, Is.True,
                "Atlas-only characters must remain drawable without the static reference.");
            controller.RestartMatch();
            var expected = new MatchSimulation(controller.Sim.Config, controller.Tuning);
            for (int tick = 0; tick < 600; tick++)
            {
                var before = expected.CaptureSnapshot();
                var command = new SimulationInput(expected.Tick,
                    DebugBot.Decide(before, 0, 0, true, controller.Tuning),
                    DebugBot.Decide(before, 1, 0, true, controller.Tuning));
                expected.Step(command);
                controller.ReferenceArtEnabled = (tick % 2) == 0;
                controller.StepOnce(command);
                Assert.That(controller.CaptureSnapshot().ComputeChecksum(),
                    Is.EqualTo(expected.CaptureSnapshot().ComputeChecksum()), "Tick " + tick);
            }
        }
    }
}
