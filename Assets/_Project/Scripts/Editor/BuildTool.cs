using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IslandRocketRumble.EditorTools
{
    /// <summary>Builds playable Windows (Builds/Windows) and web (Builds/Web) versions. Builds is ignored by git.</summary>
    public static class BuildTool
    {
        const string OutDir = "Builds/Windows";
        const string ExeName = "IslandRocketRumble.exe";
        const string WebDir = "Builds/Web";
        const string FontDir = "Assets/_Project/Resources/UI/Fonts";

        static readonly string[] Scenes = { "Assets/_Project/Scenes/Main.unity" };

        [MenuItem("IRR/Build Windows Game", priority = 200)]
        public static void BuildWindows()
        {
            Directory.CreateDirectory(OutDir);
            Build(Path.Combine(OutDir, ExeName), BuildTarget.StandaloneWindows64, "Windows");
        }

        /// <summary>Web build for itch.io: gzip with decompression fallback, since itch serves no compression headers.</summary>
        [MenuItem("IRR/Build Web Game", priority = 201)]
        public static void BuildWeb()
        {
            SetupWebFonts();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            if (Directory.Exists(WebDir)) Directory.Delete(WebDir, true);
            Build(WebDir, BuildTarget.WebGL, "Web");
        }

        /// <summary>The web has no OS fonts: Kanit (Thai + Latin) falls back to Noto Sans Symbols 2 for ✓ ✕.</summary>
        static void SetupWebFonts()
        {
            string mainPath = FontDir + "/Kanit-Medium.ttf";
            string symbolPath = FontDir + "/NotoSansSymbols2-Regular.ttf";
            var importer = AssetImporter.GetAtPath(mainPath) as TrueTypeFontImporter;
            var symbols = AssetDatabase.LoadAssetAtPath<Font>(symbolPath);
            if (importer == null || symbols == null)
            {
                Debug.LogError("[IRR] Web fonts missing in " + FontDir);
                return;
            }
            importer.fontNames = new[] { "Noto Sans Symbols 2" };
            importer.fontReferences = new[] { symbols };
            importer.SaveAndReimport();
        }

        static void Build(string location, BuildTarget target, string label)
        {
            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = location,
                target = target,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            if (s.result == BuildResult.Succeeded)
                Debug.Log($"[IRR] {label} build OK: {Path.GetFullPath(location)} ({s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:0}s)");
            else
                Debug.LogError($"[IRR] {label} build failed: {s.result}, errors {s.totalErrors}");
        }
    }
}
