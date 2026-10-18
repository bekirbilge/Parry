using System;
using System.IO;
using Game.Unity;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>Headless bootstrap scene validation and macOS player build entry points.</summary>
    public static class BootstrapPipeline
    {
        private const string ScenePath = "Assets/Game/Unity/Scenes/Bootstrap.unity";
        private const string BuildPath = "artifacts/build/ElasticArena.app";

        public static void Validate()
        {
            RunOrExit(() =>
            {
                EnsureBootstrapScene();
                ValidateBootstrapScene();
                Debug.Log("Bootstrap scene validation passed.");
            });
        }

        public static void BuildMacOS()
        {
            RunOrExit(() =>
            {
                EnsureBootstrapScene();
                ValidateBootstrapScene();

                var outputPath = Path.Combine(ProjectPath, BuildPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.StrictMode
                };
                var report = BuildPipeline.BuildPlayer(options);
                if (report == null || report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                {
                    throw new InvalidOperationException("macOS player build failed; inspect Unity build errors.");
                }

                Debug.Log("macOS player build passed: " + outputPath);
            });
        }

        private static void EnsureBootstrapScene()
        {
            if (File.Exists(AbsoluteScenePath))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(AbsoluteScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var clockObject = new GameObject("Fixed Tick Driver");
            clockObject.AddComponent<FixedTickDriver>();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException("Could not save bootstrap scene at " + ScenePath);
            }

            AssetDatabase.Refresh();
        }

        private static void ValidateBootstrapScene()
        {
            if (!File.Exists(AbsoluteScenePath))
            {
                throw new FileNotFoundException("Bootstrap scene is missing", ScenePath);
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("Bootstrap scene could not be loaded: " + ScenePath);
            }

            var driverCount = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                driverCount += root.GetComponentsInChildren<FixedTickDriver>(true).Length;
            }

            if (driverCount != 1)
            {
                throw new InvalidOperationException("Bootstrap scene must contain exactly one FixedTickDriver; found " + driverCount);
            }
        }

        private static string ProjectPath => Directory.GetParent(Application.dataPath).FullName;
        private static string AbsoluteScenePath => Path.Combine(ProjectPath, ScenePath);

        private static void RunOrExit(Action action)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
                throw;
            }
        }
    }
}
