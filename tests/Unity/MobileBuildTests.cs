using System;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class MobileBuildTests
    {
        [TestCase(BuildTarget.iOS)]
        [TestCase(BuildTarget.Android)]
        public void TemporaryPlaytestSettingsRestoreAfterFailure(BuildTarget target)
        {
            var group = target == BuildTarget.iOS ? BuildTargetGroup.iOS : BuildTargetGroup.Android;
            var playerSettingsAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
            string serializedSettings = EditorJsonUtility.ToJson(playerSettingsAsset);
            string applicationId = PlayerSettings.GetApplicationIdentifier(group);
            ScriptingImplementation scriptingBackend = PlayerSettings.GetScriptingBackend(group);
            UIOrientation orientation = PlayerSettings.defaultInterfaceOrientation;
            bool portrait = PlayerSettings.allowedAutorotateToPortrait;
            bool upsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            bool left = PlayerSettings.allowedAutorotateToLandscapeLeft;
            bool right = PlayerSettings.allowedAutorotateToLandscapeRight;
            int width = PlayerSettings.defaultScreenWidth;
            int height = PlayerSettings.defaultScreenHeight;
            string productName = PlayerSettings.productName;
            FullScreenMode fullScreenMode = PlayerSettings.fullScreenMode;
            var scenes = EditorBuildSettings.scenes;
            bool automaticSigning = PlayerSettings.iOS.appleEnableAutomaticSigning;
            string developerTeam = PlayerSettings.iOS.appleDeveloperTeamID;
            string provisioningProfile = PlayerSettings.iOS.iOSManualProvisioningProfileID;
            bool appBundle = EditorUserBuildSettings.buildAppBundle;
            bool exportAndroidProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            bool customKeystore = PlayerSettings.Android.useCustomKeystore;
            iOSSdkVersion iosSdk = PlayerSettings.iOS.sdkVersion;
            AndroidArchitecture androidArchitecture = PlayerSettings.Android.targetArchitectures;

            Assert.Throws<InvalidOperationException>(() =>
                MobileBuildPipeline.WithTemporaryPlaytestSettings(target, () =>
                {
                    Assert.That(PlayerSettings.GetApplicationIdentifier(group), Is.EqualTo(MobileBuildPipeline.PlaytestBundleId));
                    Assert.That(PlayerSettings.GetScriptingBackend(group), Is.EqualTo(ScriptingImplementation.IL2CPP));
                    Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.AutoRotation));
                    Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.False);
                    Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown, Is.False);
                    Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft, Is.True);
                    Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight, Is.True);
                    if (target == BuildTarget.iOS)
                    {
                        Assert.That(PlayerSettings.iOS.appleEnableAutomaticSigning, Is.False);
                        Assert.That(PlayerSettings.iOS.sdkVersion, Is.EqualTo(iOSSdkVersion.DeviceSDK));
                        Assert.That(PlayerSettings.iOS.appleDeveloperTeamID, Is.Empty);
                        Assert.That(PlayerSettings.iOS.iOSManualProvisioningProfileID, Is.Empty);
                    }
                    else
                    {
                        Assert.That(EditorUserBuildSettings.buildAppBundle, Is.False);
                        Assert.That(EditorUserBuildSettings.exportAsGoogleAndroidProject, Is.False);
                        Assert.That(PlayerSettings.Android.useCustomKeystore, Is.False);
                        Assert.That(PlayerSettings.Android.targetArchitectures, Is.EqualTo(AndroidArchitecture.ARM64));
                    }
                    PlayerSettings.defaultScreenWidth = width + 1;
                    PlayerSettings.defaultScreenHeight = height + 1;
                    PlayerSettings.productName = "Temporary playtest";
                    EditorBuildSettings.scenes = Array.Empty<EditorBuildSettingsScene>();
                    throw new InvalidOperationException("Simulated export failure");
                }));

            Assert.That(PlayerSettings.GetApplicationIdentifier(group), Is.EqualTo(applicationId));
            Assert.That(PlayerSettings.GetScriptingBackend(group), Is.EqualTo(scriptingBackend));
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(orientation));
            Assert.That(PlayerSettings.allowedAutorotateToPortrait, Is.EqualTo(portrait));
            Assert.That(PlayerSettings.allowedAutorotateToPortraitUpsideDown, Is.EqualTo(upsideDown));
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeLeft, Is.EqualTo(left));
            Assert.That(PlayerSettings.allowedAutorotateToLandscapeRight, Is.EqualTo(right));
            Assert.That(PlayerSettings.defaultScreenWidth, Is.EqualTo(width));
            Assert.That(PlayerSettings.defaultScreenHeight, Is.EqualTo(height));
            Assert.That(PlayerSettings.productName, Is.EqualTo(productName));
            Assert.That(PlayerSettings.fullScreenMode, Is.EqualTo(fullScreenMode));
            Assert.That(EditorBuildSettings.scenes.Length, Is.EqualTo(scenes.Length));
            for (int i = 0; i < scenes.Length; i++)
            {
                Assert.That(EditorBuildSettings.scenes[i].path, Is.EqualTo(scenes[i].path));
                Assert.That(EditorBuildSettings.scenes[i].enabled, Is.EqualTo(scenes[i].enabled));
            }
            Assert.That(PlayerSettings.iOS.appleEnableAutomaticSigning, Is.EqualTo(automaticSigning));
            Assert.That(PlayerSettings.iOS.appleDeveloperTeamID, Is.EqualTo(developerTeam));
            Assert.That(PlayerSettings.iOS.iOSManualProvisioningProfileID, Is.EqualTo(provisioningProfile));
            Assert.That(EditorUserBuildSettings.buildAppBundle, Is.EqualTo(appBundle));
            Assert.That(EditorUserBuildSettings.exportAsGoogleAndroidProject, Is.EqualTo(exportAndroidProject));
            Assert.That(PlayerSettings.Android.useCustomKeystore, Is.EqualTo(customKeystore));
            Assert.That(PlayerSettings.iOS.sdkVersion, Is.EqualTo(iosSdk));
            Assert.That(PlayerSettings.Android.targetArchitectures, Is.EqualTo(androidArchitecture));
            Assert.That(EditorJsonUtility.ToJson(playerSettingsAsset) == serializedSettings, Is.True,
                "The serialized PlayerSettings asset changed after temporary mobile settings were restored.");
        }
    }
}
