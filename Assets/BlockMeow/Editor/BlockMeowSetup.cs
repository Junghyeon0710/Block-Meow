using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BlockMeow.EditorTools
{
    /// <summary>One-click project setup: main scene, build list and mobile player settings.</summary>
    public static class BlockMeowSetup
    {
        public const string ScenePath = "Assets/BlockMeow/Scenes/Main.unity";

        [MenuItem("BlockMeow/Setup Project (Scene + Player Settings)")]
        public static void Setup()
        {
            CreateScene();
            ConfigurePlayer();
            GenerateIcon();
            AssetDatabase.SaveAssets();
            Debug.Log("[BlockMeow] Project set up. Open " + ScenePath + " and press Play.");
        }

        [MenuItem("BlockMeow/Open Main Scene")]
        public static void OpenMain()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            EditorSceneManager.OpenScene(ScenePath);
        }

        public const string IconPath = "Assets/BlockMeow/Icon/AppIcon.png";

        [MenuItem("BlockMeow/Generate App Icon")]
        public static void GenerateIcon()
        {
            const int size = 1024;
            var pixels = Atlas.RenderAppIcon(size);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(IconPath));
            File.WriteAllBytes(IconPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            Debug.Log("[BlockMeow] App icon generated at " + IconPath);
        }

        public const string ApkPath = "Build/Android/BlockMeow.apk";

        /// <summary>
        /// Builds an installable test APK (IL2CPP, ARM64, debug-signed). Command line:
        /// Unity -batchmode -projectPath . -buildTarget Android -executeMethod BlockMeow.EditorTools.BlockMeowSetup.BuildAndroid
        /// </summary>
        [MenuItem("BlockMeow/Build Android APK")]
        public static void BuildAndroid()
        {
            bool ok = false;
            try
            {
                if (!File.Exists(ScenePath)) CreateScene();
                ConfigurePlayer();
                ApplyIcon();
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                EditorUserBuildSettings.buildAppBundle = false;
                AssetDatabase.SaveAssets();
                Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = ApkPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                });
                var summary = report.summary;
                ok = summary.result == BuildResult.Succeeded;
                Debug.Log($"[BlockMeow] Android build {summary.result}: {ApkPath} ({summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalSeconds:0} s)");
                if (ok && !Application.isBatchMode) EditorUtility.RevealInFinder(ApkPath);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
        }

        static void ApplyIcon()
        {
            if (!File.Exists(IconPath)) { GenerateIcon(); return; }
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }

        static void CreateScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.06f, 0.16f);
            camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camGo.AddComponent<AudioListener>();
            new GameObject("GameApp").AddComponent<GameApp>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "BlockMeow Studio";
            PlayerSettings.productName = "블록냥";
            PlayerSettings.bundleVersion = GameApp.Version;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.blockmeow.puzzle");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.blockmeow.puzzle");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.statusBarHidden = true;
        }
    }
}
