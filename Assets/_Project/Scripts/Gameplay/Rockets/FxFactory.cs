using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Builds simple code-only particle effects (smoke trail, sparks, explosions, sparkles).
    /// Assign <see cref="ParticleMaterial"/> (GameManager.particleMaterial does this) to override the auto-made material.
    /// </summary>
    public static class FxFactory
    {
        /// <summary>Optional override. When null a soft-circle material is created from a built-in particle shader.</summary>
        public static Material ParticleMaterial;

        static Material autoMaterial;
        static Texture2D softCircle;

        public static Material GetMaterial()
        {
            if (ParticleMaterial != null) return ParticleMaterial;
            if (autoMaterial != null) return autoMaterial;

            if (softCircle == null) softCircle = MakeSoftCircle(32);

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                var m = new Material(shader) { name = "IRR_AutoParticles" };
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", softCircle);
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", softCircle);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
                // Make it alpha-blended transparent.
                if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
                if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
                if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                autoMaterial = m;
                return autoMaterial;
            }

            shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                autoMaterial = new Material(shader) { name = "IRR_AutoParticles" };
                autoMaterial.mainTexture = softCircle;
            }
            return autoMaterial;
        }

        static Texture2D MakeSoftCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.name = "IRR_SoftCircle";
            float half = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = Mathf.SmoothStep(0f, 1f, a * 1.6f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return tex;
        }

        static ParticleSystem NewSystem(string name, Transform parent, Vector3 position)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (r != null)
            {
                r.sharedMaterial = GetMaterial();
                r.renderMode = ParticleSystemRenderMode.Billboard;
            }
            var shape = ps.shape;
            shape.enabled = false;
            return ps;
        }

        static Gradient FadeGradient(Color a, Color b)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        /// <summary>Pastel white puff trail (world space). Stays attached to <paramref name="parent"/>.</summary>
        public static ParticleSystem CreateSmokeTrail(Transform parent, Vector3 localPos, float scale)
        {
            var ps = NewSystem("SmokeTrail", parent, parent != null ? parent.TransformPoint(localPos) : localPos);
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f * scale, 0.5f * scale);
            main.startColor = Palette.Cream;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 40f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(FadeGradient(Color.white, Palette.Cream));

            ps.Play();
            return ps;
        }

        /// <summary>Small tier-colored sparks spat out of a rocket tail.</summary>
        public static ParticleSystem CreateSparks(Transform parent, Vector3 localPos, Color color, float scale)
        {
            var ps = NewSystem("Sparks", parent, parent != null ? parent.TransformPoint(localPos) : localPos);
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f * scale, 0.14f * scale);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 25f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f * scale;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(FadeGradient(color, Color.white));

            ps.Play();
            return ps;
        }

        /// <summary>Stops a looping system, detaches it so it can finish, and destroys it when done.</summary>
        public static void DetachAndStop(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.transform.SetParent(null, true);
            var main = ps.main;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Object.Destroy(ps.gameObject, 3f);
        }

        /// <summary>One-shot explosion: cream smoke puffs + tier-colored sparks, sized by <paramref name="scale"/>.</summary>
        public static void CreateExplosion(Vector3 position, Color tierColor, float scale)
        {
            scale = Mathf.Max(0.2f, scale);

            var puff = NewSystem("ExplosionPuff", null, position);
            var main = puff.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * scale, 3.5f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f * scale, 1.0f * scale);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Palette.Cream);
            main.gravityModifier = -0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.playOnAwake = false;
            var em = puff.emission;
            em.enabled = true;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(14 * scale + 6)) });
            var shape = puff.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.3f * scale;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var sol = puff.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.4f));
            var col = puff.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(FadeGradient(Color.white, Palette.Cream));
            var lim = puff.limitVelocityOverLifetime;
            lim.enabled = true;
            lim.limit = 0.5f;
            lim.dampen = 0.15f;
            puff.Play();

            var sparks = NewSystem("ExplosionSparks", null, position);
            var sm = sparks.main;
            sm.duration = 1f;
            sm.loop = false;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(3f * scale, 6f * scale);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.1f * scale, 0.22f * scale);
            sm.startColor = new ParticleSystem.MinMaxGradient(tierColor, Color.white);
            sm.gravityModifier = 1f;
            sm.simulationSpace = ParticleSystemSimulationSpace.World;
            sm.stopAction = ParticleSystemStopAction.Destroy;
            sm.playOnAwake = false;
            var sem = sparks.emission;
            sem.enabled = true;
            sem.rateOverTime = 0f;
            sem.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(20 * scale + 8)) });
            var sshape = sparks.shape;
            sshape.enabled = true;
            sshape.shapeType = ParticleSystemShapeType.Hemisphere;
            sshape.radius = 0.2f * scale;
            sshape.rotation = new Vector3(-90f, 0f, 0f);
            var scol = sparks.colorOverLifetime;
            scol.enabled = true;
            scol.color = new ParticleSystem.MinMaxGradient(FadeGradient(tierColor, Color.white));
            sparks.Play();

            Object.Destroy(puff.gameObject, 4f);
            Object.Destroy(sparks.gameObject, 4f);
        }

        /// <summary>Looping twinkle used over rare part beams. Parented, destroyed with its parent.</summary>
        public static ParticleSystem CreateSparkle(Transform parent, Vector3 localPos, Color color, float height)
        {
            var ps = NewSystem("Sparkle", parent, parent != null ? parent.TransformPoint(localPos) : localPos);
            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = 18f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.6f, height, 0.6f);
            shape.position = new Vector3(0f, height * 0.5f, 0f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(FadeGradient(Color.white, color));
            ps.Play();
            return ps;
        }
    }
}
