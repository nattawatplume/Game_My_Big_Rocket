using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IslandRocketRumble.EditorTools
{
    /// <summary>
    /// Renders the main camera of Scenes/Main.unity to Logs/capture.png (edit mode, no UI).
    /// Batchmode (needs graphics, so no -nographics): -executeMethod IslandRocketRumble.EditorTools.SceneCapture.Capture
    /// </summary>
    public static class SceneCapture
    {
        [MenuItem("IRR/Capture Main Camera")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main.unity");
            var cam = Camera.main;
            var rt = new RenderTexture(1920, 1080, 24) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/capture.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Debug.Log("[IRR] Captured Logs/capture.png");
        }
    }
}
