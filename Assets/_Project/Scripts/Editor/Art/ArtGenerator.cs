using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace IslandRocketRumble.EditorTools
{
    /// <summary>
    /// Generates every low-poly mesh, material and art prefab listed in CONTRACTS.md. Safe to re-run: existing
    /// mesh/material assets are overwritten in place (GUIDs kept) and prefabs are re-saved.
    /// Batchmode: -executeMethod IslandRocketRumble.EditorTools.ArtGenerator.GenerateAll
    /// </summary>
    public static partial class ArtGenerator
    {
        const string Root = "Assets/_Project";
        const string ModelsDir = Root + "/Art/Models";
        const string MaterialsDir = Root + "/Art/Materials";
        const string ShadersDir = Root + "/Art/Shaders";
        const string PrefabsDir = Root + "/Prefabs";

        const float Outline = 1.6f;

        static int meshCount, materialCount, prefabCount;
        static Shader toonShader, unlitShader, seaShader;

        [MenuItem("IRR/Art/Generate All")]
        public static void GenerateAll()
        {
            meshCount = materialCount = prefabCount = 0;
            EnsureFolders();
            LoadShaders();
            CreateMaterials();

            BuildProps();       // first: islands nest these prefabs
            BuildCharacters();
            BuildRobot();
            BuildRockets();
            BuildParts();
            BuildIslands();
            BuildFx();
            BuildSea();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[IRR] Art generated: {meshCount} meshes, {materialCount} materials, {prefabCount} prefabs.");
        }

        // ------------------------------------------------------------------ setup

        static void EnsureFolders()
        {
            string[] dirs =
            {
                ModelsDir, MaterialsDir, ShadersDir,
                PrefabsDir + "/Characters", PrefabsDir + "/Robot", PrefabsDir + "/Islands", PrefabsDir + "/Rockets",
                PrefabsDir + "/Parts", PrefabsDir + "/Props", PrefabsDir + "/FX", PrefabsDir + "/Environment",
            };
            foreach (var d in dirs) Directory.CreateDirectory(d);
            AssetDatabase.Refresh();
        }

        static void LoadShaders()
        {
            toonShader = FindShader("IRR/Toon", "IRR_Toon.shader");
            unlitShader = FindShader("IRR/Unlit Transparent", "IRR_UnlitTransparent.shader");
            seaShader = FindShader("IRR/Sea", "IRR_Sea.shader");
        }

        static Shader FindShader(string shaderName, string fileName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                string path = ShadersDir + "/" + fileName;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                shader = Shader.Find(shaderName);
                if (shader == null) shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            }
            if (shader == null)
            {
                Debug.LogWarning($"[IRR] Shader '{shaderName}' not found, falling back to URP Lit.");
                shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
            }
            return shader;
        }

        // ------------------------------------------------------------------ assets

        /// <summary>Saves (or overwrites in place) a mesh asset in Art/Models.</summary>
        static Mesh SaveMesh(string name, MeshBuilder mb)
        {
            string path = ModelsDir + "/" + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = name };
                mb.Fill(mesh);
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                mb.Fill(mesh);
                mesh.name = name;
                EditorUtility.SetDirty(mesh);
            }
            meshCount++;
            return mesh;
        }

        static Material GetMaterial(string name, Shader shader)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            materialCount++;
            return mat;
        }

        static void SetColor(Material m, string prop, Color c)
        {
            if (m.HasProperty(prop)) m.SetColor(prop, c);
        }

        static void SetFloat(Material m, string prop, float v)
        {
            if (m.HasProperty(prop)) m.SetFloat(prop, v);
        }

        /// <summary>IRR/Toon material. Outline width in pixels (0 = none).</summary>
        static Material Toon(string name, Color color, float outline = Outline)
        {
            var m = GetMaterial(name, toonShader);
            SetColor(m, "_BaseColor", color);
            SetColor(m, "_ShadeColor", new Color(0.8f, 0.76f, 0.9f, 1f));
            SetFloat(m, "_ShadeThreshold", 0.45f);
            SetFloat(m, "_ShadeSoftness", 0.04f);
            SetFloat(m, "_AmbientStrength", 0.3f);
            SetColor(m, "_RimColor", new Color(1f, 1f, 1f, 0.2f));
            SetFloat(m, "_RimPower", 4f);
            SetColor(m, "_OutlineColor", OutlineColor);
            SetFloat(m, "_OutlineWidth", outline);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Unlit(string name, Color color, bool verticalFade)
        {
            var m = GetMaterial(name, unlitShader);
            SetColor(m, "_BaseColor", color);
            SetFloat(m, "_VerticalFade", verticalFade ? 1f : 0f);
            SetFloat(m, "_FadeStart", 0.05f);
            SetFloat(m, "_FadePower", 1.4f);
            SetFloat(m, "_EdgeBoost", verticalFade ? 0.35f : 0f);
            if (unlitShader != null && unlitShader.name != "IRR/Unlit Transparent")
                m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ scene graph helpers

        static GameObject Node(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        static GameObject Node(string name, Transform parent) => Node(name, parent, Vector3.zero);

        /// <summary>Adds MeshFilter + MeshRenderer to <paramref name="go"/>.</summary>
        static MeshRenderer AddMesh(GameObject go, Mesh mesh, Material mat, bool castShadows = true)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            r.receiveShadows = true;
            return r;
        }

        static GameObject MeshNode(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos)
            => MeshNode(name, parent, mesh, mat, localPos, Quaternion.identity);

        static GameObject MeshNode(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos, Quaternion localRot, bool castShadows = true)
        {
            var go = Node(name, parent, localPos);
            go.transform.localRotation = localRot;
            AddMesh(go, mesh, mat, castShadows);
            return go;
        }

        static void StripColliders(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c);
        }

        /// <summary>Saves the temp hierarchy as a prefab under Prefabs/ and destroys the temp object.</summary>
        static void SavePrefab(GameObject root, string relativePath)
        {
            StripColliders(root);
            string path = PrefabsDir + "/" + relativePath + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (success) prefabCount++;
            else Debug.LogError("[IRR] Failed to save prefab " + path);
            Object.DestroyImmediate(root);
        }

        /// <summary>Instantiates a previously generated prefab as a nested prefab child.</summary>
        static GameObject PlacePrefab(string relativePath, Transform parent, Vector3 localPos, float yaw, float scale = 1f)
        {
            string path = PrefabsDir + "/" + relativePath + ".prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject go;
            if (asset != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            }
            else
            {
                Debug.LogWarning("[IRR] Missing prefab " + path + " (placing empty).");
                go = new GameObject(Path.GetFileName(relativePath));
                go.transform.SetParent(parent, false);
            }
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }

        static MeshBuilder MB() => new MeshBuilder();

        static List<Vector2> Pts(params float[] xy)
        {
            var list = new List<Vector2>(xy.Length / 2);
            for (int i = 0; i + 1 < xy.Length; i += 2) list.Add(new Vector2(xy[i], xy[i + 1]));
            return list;
        }
    }
}
