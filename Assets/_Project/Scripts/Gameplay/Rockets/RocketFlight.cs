using System.Collections.Generic;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Drives one rocket: short wind-up on the pad, parabolic flight with the nose (+Y) following velocity,
    /// smoke trail, then impact (target hit, explosion, camera shake) and self-destroy.
    /// Created by <see cref="RocketLauncher"/>.
    /// </summary>
    public class RocketFlight : MonoBehaviour
    {
        public const float WindUpTime = 0.25f;
        const float WindUpRise = 0.3f;

        static readonly List<RocketFlight> active = new List<RocketFlight>();

        public PlayerId Shooter { get; private set; }
        public RocketRecipe Recipe { get; private set; }
        public TargetKind Kind { get; private set; }

        IRocketTarget target;
        Vector3 launchPoint;
        Vector3 fallbackPoint;
        Vector3 start;
        Vector3 end;
        float arcHeight;
        float flightTime;
        float timer;
        bool flying;
        bool done;

        Transform visual;
        Vector3 visualBaseScale = Vector3.one;
        ParticleSystem smoke;
        ParticleSystem sparks;

        public void Init(PlayerId shooter, RocketRecipe recipe, TargetKind kind, IRocketTarget target,
            Vector3 launchPoint, Vector3 fallbackPoint)
        {
            Shooter = shooter;
            Recipe = recipe;
            Kind = kind;
            this.target = target;
            this.launchPoint = launchPoint;
            this.fallbackPoint = fallbackPoint;

            var cfg = GameplayUtil.Config;
            int tierStep = recipe != null ? (int)recipe.tier - 1 : 0;
            float baseTime = cfg != null ? cfg.rocketFlightTime : 1.6f;
            float baseArc = cfg != null ? cfg.rocketArcHeight : 7f;
            flightTime = Mathf.Max(0.2f, baseTime * (1f + 0.07f * tierStep)); // bigger rockets are a bit slower
            arcHeight = baseArc * (1f + 0.04f * tierStep);

            visual = transform.Find("Visual");
            if (visual == null) visual = transform;
            visualBaseScale = visual.localScale;

            transform.position = launchPoint;
            transform.rotation = Quaternion.identity;
        }

        void OnEnable() { active.Add(this); }
        void OnDisable() { active.Remove(this); }

        /// <summary>Destroys all rockets currently in the air (match reset).</summary>
        public static void DestroyAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var r = active[i];
                if (r == null) continue;
                if (r.smoke != null) Destroy(r.smoke.gameObject);
                if (r.sparks != null) Destroy(r.sparks.gameObject);
                Destroy(r.gameObject);
            }
            active.Clear();
        }

        static bool IsAlive(IRocketTarget t)
        {
            if (t == null) return false;
            if (t is Object o && o == null) return false;
            return true;
        }

        void Update()
        {
            if (done) return;
            timer += Time.deltaTime;

            if (!flying)
            {
                float k = Mathf.Clamp01(timer / WindUpTime);
                // squash first, then stretch just before lift-off
                float squash = Mathf.Sin(k * Mathf.PI);
                float sy = k < 0.6f ? 1f - 0.25f * squash : 1f + 0.2f * squash;
                float sxz = 1f / Mathf.Sqrt(Mathf.Max(0.1f, sy));
                visual.localScale = new Vector3(visualBaseScale.x * sxz, visualBaseScale.y * sy, visualBaseScale.z * sxz);
                transform.position = launchPoint + Vector3.up * (WindUpRise * k * k);

                if (k >= 1f) BeginFlight();
                return;
            }

            float t = Mathf.Clamp01(timer / flightTime);
            transform.position = Evaluate(t);

            Vector3 vel = (end - start) + Vector3.up * (arcHeight * (4f - 8f * t));
            if (vel.sqrMagnitude > 0.0001f)
            {
                Quaternion want = Quaternion.FromToRotation(Vector3.up, vel.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-18f * Time.deltaTime));
            }

            if (t >= 1f) Arrive();
        }

        Vector3 Evaluate(float t)
        {
            return Vector3.Lerp(start, end, t) + Vector3.up * (arcHeight * 4f * t * (1f - t));
        }

        void BeginFlight()
        {
            flying = true;
            timer = 0f;
            visual.localScale = visualBaseScale;
            start = transform.position;
            end = IsAlive(target) ? target.GetAimPoint() : fallbackPoint;

            float tierScale = Recipe != null ? 0.8f + 0.15f * ((int)Recipe.tier - 1) : 1f;
            Transform tail = transform.Find("Flame");
            Vector3 local = tail != null ? transform.InverseTransformPoint(tail.position) : Vector3.zero;
            smoke = FxFactory.CreateSmokeTrail(transform, local, tierScale);
            Color tierColor = Recipe != null ? Palette.Tier(Recipe.tier) : Color.white;
            sparks = FxFactory.CreateSparks(transform, local, tierColor, tierScale);
        }

        void Arrive()
        {
            done = true;
            if (IsAlive(target) && Recipe != null)
            {
                target.TakeRocketHit(new RocketHit { shooter = Shooter, recipe = Recipe, point = end });
            }

            float impact = Recipe != null ? Recipe.impactScale : 1f;
            Color tierColor = Recipe != null ? Palette.Tier(Recipe.tier) : Color.white;
            FxFactory.CreateExplosion(end, tierColor, impact);
            CameraShake.Shake(0.06f + 0.12f * impact, 0.2f + 0.12f * impact);

            FxFactory.DetachAndStop(smoke);
            FxFactory.DetachAndStop(sparks);
            smoke = null;
            sparks = null;
            Destroy(gameObject);
        }
    }
}
