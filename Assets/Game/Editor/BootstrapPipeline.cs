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
            EnsureJoystickAxes();
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
            ValidateJoystickAxis("ParryGamepadX", 0);
            ValidateJoystickAxis("ParryGamepadY", 1);
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

        private static SerializedObject InputManager()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset");
            if (assets.Length == 0) throw new InvalidOperationException("InputManager asset unavailable.");
            return new SerializedObject(assets[0]);
        }

        private static void EnsureJoystickAxes()
        {
            var serialized = InputManager();
            var axes = serialized.FindProperty("m_Axes");
            EnsureAxis(axes, "ParryGamepadX", 0);
            EnsureAxis(axes, "ParryGamepadY", 1);
            serialized.ApplyModifiedProperties();
        }

        private static void EnsureAxis(SerializedProperty axes, string name, int index)
        {
            SerializedProperty axis = null;
            for (var i = 0; i < axes.arraySize; i++)
            {
                var candidate = axes.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("m_Name").stringValue == name) { axis = candidate; break; }
            }
            if (axis == null)
            {
                axes.InsertArrayElementAtIndex(axes.arraySize);
                axis = axes.GetArrayElementAtIndex(axes.arraySize - 1);
            }
            axis.FindPropertyRelative("m_Name").stringValue = name;
            foreach (var field in new[] { "descriptiveName", "descriptiveNegativeName", "negativeButton", "positiveButton", "altNegativeButton", "altPositiveButton" })
                axis.FindPropertyRelative(field).stringValue = "";
            axis.FindPropertyRelative("gravity").floatValue = 0;
            axis.FindPropertyRelative("dead").floatValue = 0.19f;
            axis.FindPropertyRelative("sensitivity").floatValue = 1;
            axis.FindPropertyRelative("snap").boolValue = false;
            axis.FindPropertyRelative("invert").boolValue = index == 1;
            axis.FindPropertyRelative("type").intValue = 2;
            axis.FindPropertyRelative("axis").intValue = index;
            axis.FindPropertyRelative("joyNum").intValue = 1;
        }

        private static void ValidateJoystickAxis(string name, int index)
        {
            var serialized = InputManager();
            var axes = serialized.FindProperty("m_Axes");
            for (var i = 0; i < axes.arraySize; i++)
            {
                var axis = axes.GetArrayElementAtIndex(i);
                if (axis.FindPropertyRelative("m_Name").stringValue != name) continue;
                if (axis.FindPropertyRelative("type").intValue == 2 && axis.FindPropertyRelative("axis").intValue == index &&
                    axis.FindPropertyRelative("joyNum").intValue == 1 &&
                    axis.FindPropertyRelative("invert").boolValue == (index == 1) &&
                    axis.FindPropertyRelative("negativeButton").stringValue == "" &&
                    axis.FindPropertyRelative("positiveButton").stringValue == "") return;
            }
            throw new InvalidOperationException("Joystick-only input axis invalid: " + name);
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
