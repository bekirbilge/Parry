using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Local, unsigned phone playtest exports of the bootstrap scene.</summary>
    public static class MobileBuildPipeline
    {
        public const string PlaytestBundleId = "com.bekirbilge.parry.playtest";
        public const string IOSOutput = "artifacts/build/ios";
        public const string AndroidOutput = "artifacts/build/Parry-playtest.apk";

        public static void BuildIOS() => Run(BuildTarget.iOS, IOSOutput);
        public static void BuildAndroid() => Run(BuildTarget.Android, AndroidOutput);

        private static void Run(BuildTarget target, string relativeOutput)
        {
            try
            {
                var group = target == BuildTarget.iOS ? BuildTargetGroup.iOS : BuildTargetGroup.Android;
                if (!BuildPipeline.IsBuildTargetSupported(group, target))
                    throw new InvalidOperationException(target + " build support is missing from Unity 6000.3.26f1. Install that platform module in Unity Hub, then retry.");

                WithTemporaryPlaytestSettings(target, () =>
                {
                    BootstrapPipeline.EnsureBootstrapScene();
                    BootstrapPipeline.ValidateBootstrapScene();
                    var output = Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativeOutput);
                    Directory.CreateDirectory(target == BuildTarget.iOS ? output : Path.GetDirectoryName(output));
                    var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                    {
                        scenes = new[] { BootstrapPipeline.ScenePath },
                        locationPathName = output,
                        target = target,
                        options = BuildOptions.StrictMode | BuildOptions.Development |
                                  (target == BuildTarget.Android ? BuildOptions.AllowDebugging : BuildOptions.None)
                    });
                    if (report == null || report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                        throw new InvalidOperationException(target + " playtest export failed; inspect the Unity build log.");
                    Debug.Log(target + " playtest export passed: " + output);
                });
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
                throw;
            }
        }

        // Also used by EditMode tests to verify restoration when export work throws.
        public static void WithTemporaryPlaytestSettings(BuildTarget target, Action work)
        {
            if (target != BuildTarget.iOS && target != BuildTarget.Android)
                throw new ArgumentOutOfRangeException(nameof(target));
            if (work == null) throw new ArgumentNullException(nameof(work));

            var snapshot = new SettingsSnapshot(target);
            try
            {
                PlayerSettings.SetApplicationIdentifier(snapshot.Group, PlaytestBundleId);
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
                PlayerSettings.allowedAutorotateToPortrait = false;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
                PlayerSettings.SetScriptingBackend(snapshot.Group, ScriptingImplementation.IL2CPP);
                if (target == BuildTarget.iOS)
                {
                    PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
                    PlayerSettings.iOS.appleEnableAutomaticSigning = false;
                    PlayerSettings.iOS.appleDeveloperTeamID = string.Empty;
                    PlayerSettings.iOS.iOSManualProvisioningProfileID = string.Empty;
                }
                else
                {
                    EditorUserBuildSettings.buildAppBundle = false;
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
                    PlayerSettings.Android.useCustomKeystore = false;
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                }
                work();
            }
            finally { snapshot.Restore(); }
        }

        private sealed class SettingsSnapshot
        {
            public readonly BuildTargetGroup Group;
            private readonly string applicationId;
            private readonly UIOrientation orientation;
            private readonly bool portrait, upsideDown, landscapeLeft, landscapeRight;
            private readonly int screenWidth, screenHeight;
            private readonly FullScreenMode fullScreenMode;
            private readonly string productName;
            private readonly EditorBuildSettingsScene[] scenes;
            private readonly bool automaticSigning, buildAppBundle, exportAndroidProject, customKeystore;
            private readonly string developerTeam, provisioningProfile;
            private readonly int activeInputHandler;
            private readonly ScriptingImplementation scriptingBackend;
            private readonly iOSSdkVersion iosSdk;
            private readonly AndroidArchitecture androidArchitecture;
            private readonly UnityEngine.Object playerSettingsAsset;
            private readonly string playerSettingsJson;

            public SettingsSnapshot(BuildTarget target)
            {
                Group = target == BuildTarget.iOS ? BuildTargetGroup.iOS : BuildTargetGroup.Android;
                playerSettingsAsset = LoadPlayerSettings();
                playerSettingsJson = EditorJsonUtility.ToJson(playerSettingsAsset);
                applicationId = PlayerSettings.GetApplicationIdentifier(Group);
                scriptingBackend = PlayerSettings.GetScriptingBackend(Group);
                orientation = PlayerSettings.defaultInterfaceOrientation;
                portrait = PlayerSettings.allowedAutorotateToPortrait;
                upsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
                landscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
                landscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;
                screenWidth = PlayerSettings.defaultScreenWidth;
                screenHeight = PlayerSettings.defaultScreenHeight;
                fullScreenMode = PlayerSettings.fullScreenMode;
                productName = PlayerSettings.productName;
                scenes = EditorBuildSettings.scenes;
                activeInputHandler = ReadInputHandler();
                if (target == BuildTarget.iOS)
                {
                    iosSdk = PlayerSettings.iOS.sdkVersion;
                    automaticSigning = PlayerSettings.iOS.appleEnableAutomaticSigning;
                    developerTeam = PlayerSettings.iOS.appleDeveloperTeamID;
                    provisioningProfile = PlayerSettings.iOS.iOSManualProvisioningProfileID;
                }
                else
                {
                    buildAppBundle = EditorUserBuildSettings.buildAppBundle;
                    exportAndroidProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
                    customKeystore = PlayerSettings.Android.useCustomKeystore;
                    androidArchitecture = PlayerSettings.Android.targetArchitectures;
                }
            }

            public void Restore()
            {
                PlayerSettings.SetApplicationIdentifier(Group, applicationId);
                PlayerSettings.SetScriptingBackend(Group, scriptingBackend);
                PlayerSettings.defaultInterfaceOrientation = orientation;
                PlayerSettings.allowedAutorotateToPortrait = portrait;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = upsideDown;
                PlayerSettings.allowedAutorotateToLandscapeLeft = landscapeLeft;
                PlayerSettings.allowedAutorotateToLandscapeRight = landscapeRight;
                PlayerSettings.defaultScreenWidth = screenWidth;
                PlayerSettings.defaultScreenHeight = screenHeight;
                PlayerSettings.fullScreenMode = fullScreenMode;
                PlayerSettings.productName = productName;
                EditorBuildSettings.scenes = scenes;
                WriteInputHandler(activeInputHandler);
                if (Group == BuildTargetGroup.iOS)
                {
                    PlayerSettings.iOS.sdkVersion = iosSdk;
                    PlayerSettings.iOS.appleEnableAutomaticSigning = automaticSigning;
                    PlayerSettings.iOS.appleDeveloperTeamID = developerTeam;
                    PlayerSettings.iOS.iOSManualProvisioningProfileID = provisioningProfile;
                }
                else
                {
                    EditorUserBuildSettings.buildAppBundle = buildAppBundle;
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = exportAndroidProject;
                    PlayerSettings.Android.useCustomKeystore = customKeystore;
                    PlayerSettings.Android.targetArchitectures = androidArchitecture;
                }
                // Typed setters turn inherited defaults into explicit entries. Restore the
                // serialized asset last so empty maps and unset values remain unchanged.
                EditorJsonUtility.FromJsonOverwrite(playerSettingsJson, playerSettingsAsset);
                EditorUtility.SetDirty(playerSettingsAsset);
                AssetDatabase.SaveAssets();
            }

            private static UnityEngine.Object LoadPlayerSettings()
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (assets.Length == 0) throw new InvalidOperationException("Player settings unavailable.");
                return assets[0];
            }

            private static int ReadInputHandler()
            {
                var handler = new SerializedObject(LoadPlayerSettings()).FindProperty("activeInputHandler");
                if (handler == null) throw new InvalidOperationException("Active input handler setting unavailable.");
                return handler.intValue;
            }

            private static void WriteInputHandler(int value)
            {
                var settings = new SerializedObject(LoadPlayerSettings());
                var handler = settings.FindProperty("activeInputHandler");
                if (handler == null) throw new InvalidOperationException("Active input handler setting unavailable.");
                if (handler.intValue == value) return;
                handler.intValue = value;
                settings.ApplyModifiedProperties();
            }
        }
    }
}
