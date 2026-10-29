using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Code-only VFX helpers for the Robot module: particle systems, transparent materials,
    /// flat ring/disc meshes, tinting and legacy TextMesh labels. No assets required.
    /// </summary>
    public static class RobotFx
    {
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        static Texture2D softDot;
        static Texture2D ringTex;
        static Material dotMaterial;
        static Material ringMaterial;
        static Mesh ringMesh;
        static Mesh discMesh;
        static Font font;
        static bool fontTried;
        static GameConfig fallbackConfig;
        static MaterialPropertyBlock mpb;

        /// <summary>Config from MatchContext, or a default-valued instance (same numbers as GameConfig's defaults).</summary>
        public static GameConfig Config
        {
            get
            {
                var ctx = MatchContext.Instance;
                if (ctx != null && ctx.config != null) return ctx.config;
                if (fallbackConfig == null) fallbackConfig = ScriptableObject.CreateInstance<GameConfig>();
                return fallbackConfig;
            }
        }

        // ---------------------------------------------------------------- textures / materials

        public static Texture2D SoftDot
        {
            get
            {
                if (softDot == null) softDot = MakeRadialTexture(64, false);
                return softDot;
            }
        }

        public static Texture2D RingTexture
        {
            get
            {
                if (ringTex == null) ringTex = MakeRadialTexture(64, true);
                return ringTex;
            }
        }

        /// <summary>Shared alpha-blended particle material with a round cartoon dot.</summary>
        public static Material DotMaterial
        {
            get
            {
                if (dotMaterial == null) dotMaterial = CreateTransparentMaterial(SoftDot);
                return dotMaterial;
            }
        }

        /// <summary>Shared alpha-blended particle material with a hollow ring (soap bubbles).</summary>
        public static Material RingMaterial
        {
            get
            {
                if (ringMaterial == null) ringMaterial = CreateTransparentMaterial(RingTexture);
                return ringMaterial;
            }
        }

        static Texture2D MakeRadialTexture(int size, bool ring)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.name = ring ? "IRR_RobotRingTex" : "IRR_RobotDotTex";
            var px = new Color32[size * size];
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a;
                    if (ring)
                    {
                        float band = Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.12f);
                        float inner = d < 0.82f ? 0.18f : 0f;
                        float highlight = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(dx, dy), new Vector2(-0.35f, 0.35f)) / 0.18f);
                        a = Mathf.Max(band, Mathf.Max(inner, highlight));
                    }
                    else
                    {
                        a = Mathf.Clamp01((1f - d) * 5f); // crisp cartoon edge
                    }
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }

        /// <summary>
        /// New alpha-blended material using URP Particles/Unlit (vertex colour aware),
        /// falling back to Sprites/Default.
        /// </summary>
        public static Material CreateTransparentMaterial(Texture tex)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Material m;
            if (sh != null)
            {
                m = new Material(sh);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_Cull", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                if (tex != null) m.SetTexture("_BaseMap", tex);
                m.SetColor(BaseColorId, Color.white);
            }
            else
            {
                sh = Shader.Find("Sprites/Default");
                if (sh == null) sh = Shader.Find("Unlit/Transparent");
                m = new Material(sh);
            }
            if (tex != null && m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            m.name = "IRR_RobotFx";
            return m;
        }

        /// <summary>Sets the tint on a material created by <see cref="CreateTransparentMaterial"/>.</summary>
        public static void SetMaterialColor(Material m, Color c)
        {
            if (m == null) return;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
        }

        /// <summary>Tints a renderer through a MaterialPropertyBlock "_BaseColor" (keeps other block values).</summary>
        public static void Tint(Renderer r, Color c)
        {
            if (r == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, c);
            r.SetPropertyBlock(mpb);
        }

        public static void TintAll(GameObject go, Color c)
        {
            if (go == null) return;
            var rs = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
                if (!(rs[i] is ParticleSystemRenderer)) Tint(rs[i], c);
        }

        /// <summary>Removes colliders and freezes rigidbodies so art prefabs act as pure visuals.</summary>
        public static void StripPhysics(GameObject go)
        {
            if (go == null) return;
            var cols = go.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++) Object.Destroy(cols[i]);
            var rbs = go.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rbs.Length; i++)
            {
                rbs[i].isKinematic = true;
                rbs[i].detectCollisions = false;
            }
        }

        // ---------------------------------------------------------------- meshes

        /// <summary>Flat annulus on XZ (outer radius 1, facing +Y), shared.</summary>
        public static Mesh RingMesh
        {
            get
            {
                if (ringMesh == null) ringMesh = BuildRing(0.8f, 1f, 48);
                return ringMesh;
            }
        }

        /// <summary>Flat disc on XZ (radius 1, facing +Y), shared.</summary>
        public static Mesh DiscMesh
        {
            get
            {
                if (discMesh == null) discMesh = BuildDisc(1f, 48);
                return discMesh;
            }
        }

        static Mesh BuildRing(float inner, float outer, int seg)
        {
            var verts = new Vector3[seg * 2];
            var cols = new Color[seg * 2];
            var tris = new int[seg * 6];
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                float cx = Mathf.Cos(a), cz = Mathf.Sin(a);
                verts[i * 2] = new Vector3(cx * inner, 0f, cz * inner);
                verts[i * 2 + 1] = new Vector3(cx * outer, 0f, cz * outer);
                cols[i * 2] = Color.white;
                cols[i * 2 + 1] = Color.white;
            }
            for (int i = 0; i < seg; i++)
            {
                int n = (i + 1) % seg;
                int i0 = i * 2, o0 = i * 2 + 1, i1 = n * 2, o1 = n * 2 + 1;
                int t = i * 6;
                // clockwise when seen from above (+Y) => front face up
                tris[t] = i0; tris[t + 1] = o1; tris[t + 2] = o0;
                tris[t + 3] = i0; tris[t + 4] = i1; tris[t + 5] = o1;
            }
            var m = new Mesh { name = "IRR_RobotRing" };
            m.vertices = verts;
            m.colors = cols;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static Mesh BuildDisc(float radius, int seg)
        {
            var verts = new Vector3[seg + 1];
            var cols = new Color[seg + 1];
            var tris = new int[seg * 3];
            verts[0] = Vector3.zero;
            cols[0] = Color.white;
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                cols[i + 1] = Color.white;
            }
            for (int i = 0; i < seg; i++)
            {
                int n = (i + 1) % seg;
                tris[i * 3] = 0;
                tris[i * 3 + 1] = n + 1;
                tris[i * 3 + 2] = i + 1;
            }
            var m = new Mesh { name = "IRR_RobotDisc" };
            m.vertices = verts;
            m.colors = cols;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        // ---------------------------------------------------------------- particles

        /// <summary>
        /// Creates a stopped, looping, world-space ParticleSystem whose cone emits upward (+Y).
        /// Caller configures modules and calls Play().
        /// </summary>
        public static ParticleSystem NewSystem(string name, Transform parent, Vector3 localPos, Material mat)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // cone +Z -> world up
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 300;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.2f;

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sharedMaterial = mat;
            psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psr.receiveShadows = false;
            return ps;
        }

        public static Gradient Gradient3(Color a, Color b, Color c, float alphaStart, float alphaMid, float alphaEnd)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 0.45f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(alphaStart, 0f), new GradientAlphaKey(alphaMid, 0.5f), new GradientAlphaKey(alphaEnd, 1f) });
            return g;
        }

        public static void SetColorOverLifetime(ParticleSystem ps, Gradient g)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(g);
        }

        public static void SetSizeOverLifetime(ParticleSystem ps, AnimationCurve curve)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        public static void SetRate(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = rate;
        }

        /// <summary>Start/stop emitting without killing live particles.</summary>
        public static void Toggle(ParticleSystem ps, bool on)
        {
            if (ps == null) return;
            if (on)
            {
                if (!ps.isEmitting) ps.Play(true);
            }
            else if (ps.isEmitting)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>One-shot sphere burst that destroys itself when finished.</summary>
        public static ParticleSystem Burst(string name, Vector3 position, Material mat, int count,
            Color colorA, Color colorB, float speedMin, float speedMax, float sizeMin, float sizeMax,
            float lifeMin, float lifeMax, float gravity)
        {
            var ps = NewSystem(name, null, position, mat);
            var main = ps.main;
            main.loop = false;
            main.duration = 0.2f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.gravityModifier = gravity;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 300)) });

            SetColorOverLifetime(ps, Gradient3(Color.white, Color.white, Color.white, 1f, 1f, 0f));
            SetSizeOverLifetime(ps, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)));
            ps.Play(true);
            return ps;
        }

        // ---------------------------------------------------------------- text

        public static Font DefaultFont
        {
            get
            {
                if (font == null && !fontTried)
                {
                    fontTried = true;
                    try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                    catch { font = null; }
                }
                return font;
            }
        }

        public static TextMesh NewText(string name, Transform parent, string text, Color color, float characterSize)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = characterSize;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            var f = DefaultFont;
            if (f != null)
            {
                tm.font = f;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = f.material;
            }
            return tm;
        }

        public static void FaceCamera(Transform t)
        {
            if (t == null) return;
            var cam = Camera.main;
            if (cam != null) t.rotation = cam.transform.rotation;
        }
    }
}
