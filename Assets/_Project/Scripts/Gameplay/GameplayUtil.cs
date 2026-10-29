using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>Small shared helpers for the gameplay module (tinting, prefab-or-primitive spawning, safe child lookup).</summary>
    public static class GameplayUtil
    {
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static readonly int ColorId = Shader.PropertyToID("_Color");

        static MaterialPropertyBlock block;

        static MaterialPropertyBlock Block
        {
            get
            {
                if (block == null) block = new MaterialPropertyBlock();
                return block;
            }
        }

        /// <summary>Tints every renderer under <paramref name="root"/> via MaterialPropertyBlock (_BaseColor and _Color).</summary>
        public static void Tint(GameObject root, Color color)
        {
            if (root == null) return;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                if (r.GetComponent<TextMesh>() != null) continue;
                TintRenderer(r, color);
            }
        }

        public static void TintRenderer(Renderer r, Color color)
        {
            if (r == null) return;
            var b = Block;
            r.GetPropertyBlock(b);
            b.SetColor(BaseColorId, color);
            b.SetColor(ColorId, color);
            r.SetPropertyBlock(b);
        }

        /// <summary>Current tint of a renderer: its property block color if set, else its material color, else white.</summary>
        public static Color GetTint(Renderer r)
        {
            if (r == null) return Color.white;
            var b = Block;
            r.GetPropertyBlock(b);
            if (!b.isEmpty)
            {
                Color c = b.GetColor(BaseColorId);
                if (c.r + c.g + c.b + c.a > 0.0001f) return c;
            }
            var mat = r.sharedMaterial;
            if (mat != null)
            {
                if (mat.HasProperty(BaseColorId)) return mat.GetColor(BaseColorId);
                if (mat.HasProperty(ColorId)) return mat.GetColor(ColorId);
            }
            return Color.white;
        }

        /// <summary>Creates a primitive without its collider (gameplay here is purely kinematic).</summary>
        public static GameObject CreatePrimitive(PrimitiveType type, string name)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            return go;
        }

        /// <summary>Instantiates <paramref name="prefab"/>, or a collider-less primitive when the prefab is missing.</summary>
        public static GameObject SpawnOrPrimitive(GameObject prefab, PrimitiveType fallback, Vector3 fallbackScale,
            Vector3 position, Quaternion rotation, Transform parent, string name)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, position, rotation, parent);
                go.name = name;
            }
            else
            {
                go = CreatePrimitive(fallback, name);
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(position, rotation);
                go.transform.localScale = fallbackScale;
            }
            return go;
        }

        /// <summary>transform.Find guarded against a null root.</summary>
        public static Transform FindChild(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path)) return null;
            return root.Find(path);
        }

        public static float DistanceXZ(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static GameConfig Config
            => MatchContext.Instance != null ? MatchContext.Instance.config : null;

        static Font cachedFont;

        /// <summary>Creates a world-space legacy TextMesh child (used for countdown numbers over warnings).</summary>
        public static TextMesh CreateTextMesh(Transform parent, Vector3 localPos, float characterSize, Color color)
        {
            var go = new GameObject("CountdownText");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            if (cachedFont == null)
            {
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            if (cachedFont != null)
            {
                tm.font = cachedFont;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = cachedFont.material;
            }
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = characterSize;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            return tm;
        }

        /// <summary>Rotates a transform so it faces the main camera like a billboard.</summary>
        public static void Billboard(Transform t)
        {
            if (t == null) return;
            var cam = Camera.main;
            if (cam != null) t.rotation = cam.transform.rotation;
        }

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
