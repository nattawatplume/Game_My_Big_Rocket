using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IslandRocketRumble.EditorTools
{
    /// <summary>Builds a playable Windows version into Builds/Windows (ignored by git).</summary>
    public static class BuildTool
    {
        const string OutDir = "Builds/Windows";
        const string ExeName = "IslandRocketRumble.exe";

        [MenuItem("IRR/Build Windows Game", priority = 200)]
        public static void BuildWindows()
        {
            string[] scenes = { "Assets/_Project/Scenes/Main.unity" };
            Directory.CreateDirectory(OutDir);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(OutDir, ExeName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            if (s.result == BuildResult.Succeeded)
                Debug.Log($"[IRR] Windows build OK: {Path.GetFullPath(options.locationPathName)} ({s.totalSize / (1024 * 1024)} MB, {s.totalTime.TotalSeconds:0}s)");
            else
                Debug.LogError($"[IRR] Windows build failed: {s.result}, errors {s.totalErrors}");
        }
    }
}
