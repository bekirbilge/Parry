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
    /// <summary>Creates, validates and builds the playable graybox scene.</summary>
    public static class BootstrapPipeline
    {
        public const string ScenePath = "Assets/Game/Unity/Scenes/Bootstrap.unity";
        private const string BuildPath = "artifacts/build/ElasticArena.app";

        public static void Validate()
        {
            RunOrExit(() => { EnsureBootstrapScene(); ValidateBootstrapScene(); Debug.Log("Playable scene validation passed."); });
        }

        public static void BuildMacOS()
        {
            RunOrExit(() =>
            {
                EnsureBootstrapScene();
                ValidateBootstrapScene();
                var outputPath = Path.Combine(ProjectPath, BuildPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.StrictMode
                });
                if (report == null || report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                    throw new InvalidOperationException("macOS player build failed; inspect Unity build errors.");
                Debug.Log("macOS player build passed: " + outputPath);
            });
        }

        public static void EnsureBootstrapScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AbsoluteScenePath));
            var scene = File.Exists(AbsoluteScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var driver = FindOneOrAdd<FixedTickDriver>(scene, "Game");
            var controller = FindOne<GameController>(scene);
            if (controller != null && driver.gameObject != controller.gameObject)
            {
                UnityEngine.Object.DestroyImmediate(controller);
                controller = null;
            }
            if (controller == null) driver.gameObject.AddComponent<GameController>();

            var camera = FindOneOrAdd<Camera>(scene, "Arena Camera");
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.09f, 0.14f);
            camera.tag = "MainCamera";
            if (camera.GetComponent<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>();

            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.productName = "ElasticArena";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            EnsureBothInputBackends();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save scene at " + ScenePath);
            AssetDatabase.SaveAssets();
        }

        public static void ValidateBootstrapScene()
        {
            if (!File.Exists(AbsoluteScenePath))
                throw new FileNotFoundException("Bootstrap scene is missing", ScenePath);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("Bootstrap scene could not be loaded: " + ScenePath);
            if (Count<FixedTickDriver>(scene) != 1 || Count<GameController>(scene) != 1 ||
                Count<Camera>(scene) != 1 || Count<AudioListener>(scene) != 1)
                throw new InvalidOperationException("Scene requires exactly one clock, controller, camera and audio listener.");
            if (FindOne<FixedTickDriver>(scene).gameObject != FindOne<GameController>(scene).gameObject)
                throw new InvalidOperationException("Clock and controller must share the Game object.");
            var camera = FindOne<Camera>(scene);
            if (!camera.orthographic || !camera.CompareTag("MainCamera"))
                throw new InvalidOperationException("Arena camera must be orthographic and tagged MainCamera.");
            var enabled = EditorBuildSettings.scenes;
            if (enabled.Length != 1 || !enabled[0].enabled || enabled[0].path != ScenePath)
                throw new InvalidOperationException("Playable scene must be the only enabled build scene.");
            if (PlayerSettings.productName != "ElasticArena" ||
                PlayerSettings.defaultScreenWidth != 1280 || PlayerSettings.defaultScreenHeight != 720 ||
                PlayerSettings.fullScreenMode != FullScreenMode.Windowed ||
                PlayerSettings.allowedAutorotateToPortrait || PlayerSettings.allowedAutorotateToPortraitUpsideDown ||
                !PlayerSettings.allowedAutorotateToLandscapeLeft || !PlayerSettings.allowedAutorotateToLandscapeRight)
                throw new InvalidOperationException("Landscape player settings are invalid.");
            if (InputSettings().FindProperty("activeInputHandler").intValue != 2)
                throw new InvalidOperationException("Both input backends must be enabled for gamepad and legacy keyboard/touch input.");
        }

        private static T FindOneOrAdd<T>(Scene scene, string name) where T : Component
        {
            var existing = FindOne<T>(scene);
            return existing != null ? existing : new GameObject(name).AddComponent<T>();
        }

        private static T FindOne<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var item = root.GetComponentInChildren<T>(true);
                if (item != null) return item;
            }
            return null;
        }

        private static int Count<T>(Scene scene) where T : Component
        {
            var count = 0;
            foreach (var root in scene.GetRootGameObjects())
                count += root.GetComponentsInChildren<T>(true).Length;
            return count;
        }

        private static SerializedObject InputSettings()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0) throw new InvalidOperationException("Player settings unavailable.");
            return new SerializedObject(assets[0]);
        }

        private static void EnsureBothInputBackends()
        {
            var settings = InputSettings();
            var handler = settings.FindProperty("activeInputHandler");
            if (handler == null) throw new InvalidOperationException("Active input handler setting unavailable.");
            handler.intValue = 2; // Both: semantic gamepad controls plus existing keyboard/touch APIs.
            settings.ApplyModifiedProperties();
        }

        private static string ProjectPath => Directory.GetParent(Application.dataPath).FullName;
        private static string AbsoluteScenePath => Path.Combine(ProjectPath, ScenePath);

        private static void RunOrExit(Action action)
        {
            try { action(); }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
                throw;
            }
        }
    }
}
