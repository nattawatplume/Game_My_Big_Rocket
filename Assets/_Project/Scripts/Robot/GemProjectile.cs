using System.Globalization;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// A crystal gem lobbed by the robot. The root object sits at the landing point and owns the
    /// red warning ring + countdown; the gem visual is a child that flies a high arc and lands
    /// exactly when the countdown (GameConfig.gemWarningTime) reaches zero. On landing every player
    /// within GameConfig.gemHitRadius (XZ) is stunned for GameConfig.stunDuration.
    /// Create with <see cref="Launch"/>.
    /// </summary>
    public class GemProjectile : MonoBehaviour
    {
        Vector3 from;
        Vector3 target;
        float flightTime;
        float hitRadius;
        float stunDuration;
        float arcHeight;
        float elapsed;
        bool landed;

        Transform gem;
        Vector3 spinAxis;
        float spinSpeed;

        Transform ring;
        Vector3 ringBaseScale;
        Renderer[] ringPrefabRenderers;
        Material ringMat;
        Transform fill;
        Material fillMat;
        TextMesh countdown;

        /// <summary>Spawns a gem flying from <paramref name="from"/> to the ground point <paramref name="target"/>.</summary>
        public static GemProjectile Launch(Vector3 from, Vector3 target, GameObject gemPrefab, GameObject warningRingPrefab, float arcHeight)
        {
            var go = new GameObject("GemProjectile");
            go.transform.position = target;
            var gp = go.AddComponent<GemProjectile>();
            gp.Init(from, target, gemPrefab, warningRingPrefab, arcHeight);
            GameEvents.RaiseGemIncoming(target);
            return gp;
        }

        void Init(Vector3 fromPos, Vector3 targetPos, GameObject gemPrefab, GameObject warningRingPrefab, float arc)
        {
            var cfg = RobotFx.Config;
            from = fromPos;
            target = targetPos;
            flightTime = Mathf.Max(0.1f, cfg.gemWarningTime);
            hitRadius = Mathf.Max(0.05f, cfg.gemHitRadius);
            stunDuration = cfg.stunDuration;
            arcHeight = Mathf.Max(0.5f, arc);

            BuildWarning(warningRingPrefab);
            BuildGem(gemPrefab);
            UpdateVisuals();
        }

        void BuildWarning(GameObject warningRingPrefab)
        {
            Vector3 ground = target + Vector3.up * 0.03f;
            ringBaseScale = new Vector3(hitRadius, 1f, hitRadius); // ring art has radius 1

            if (warningRingPrefab != null)
            {
                var r = Instantiate(warningRingPrefab, transform);
                r.name = "WarningRing";
                r.transform.position = ground;
                r.transform.rotation = Quaternion.identity;
                RobotFx.StripPhysics(r);
                ring = r.transform;
                ringPrefabRenderers = r.GetComponentsInChildren<Renderer>(true);
            }
            else
            {
                var r = new GameObject("WarningRing");
                r.transform.SetParent(transform, false);
                r.transform.position = ground;
                r.AddComponent<MeshFilter>().sharedMesh = RobotFx.RingMesh;
                var mr = r.AddComponent<MeshRenderer>();
                ringMat = RobotFx.CreateTransparentMaterial(null);
                mr.sharedMaterial = ringMat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                ring = r.transform;
            }
            ring.localScale = ringBaseScale;

            // Filling disc: grows from the centre to the full danger radius as the gem approaches.
            var f = new GameObject("WarningFill");
            f.transform.SetParent(transform, false);
            f.transform.position = target + Vector3.up * 0.025f;
            f.AddComponent<MeshFilter>().sharedMesh = RobotFx.DiscMesh;
            var fmr = f.AddComponent<MeshRenderer>();
            fillMat = RobotFx.CreateTransparentMaterial(null);
            fillMat.renderQueue = fillMat.renderQueue - 1;
            fmr.sharedMaterial = fillMat;
            fmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fmr.receiveShadows = false;
            fill = f.transform;
            fill.localScale = new Vector3(0.01f, 1f, 0.01f);

            countdown = RobotFx.NewText("Countdown", transform, "", Palette.Danger, 0.05f);
            countdown.transform.position = target + Vector3.up * 0.75f;
        }

        void BuildGem(GameObject gemPrefab)
        {
            var holder = new GameObject("Gem").transform;
            holder.SetParent(transform, false);
            holder.position = from;
            gem = holder;

            GameObject visual;
            if (gemPrefab != null)
            {
                visual = Instantiate(gemPrefab, holder);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                RobotFx.StripPhysics(visual);
                // Art pivots are bottom-centre: recentre so the spin looks right.
                var rs = visual.GetComponentsInChildren<Renderer>(true);
                if (rs.Length > 0)
                {
                    Bounds b = rs[0].bounds;
                    for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                    visual.transform.position += holder.position - b.center;
                }
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var col = visual.GetComponent<Collider>();
                if (col != null) Destroy(col);
                visual.transform.SetParent(holder, false);
                visual.transform.localScale = Vector3.one * 0.4f;
            }
            visual.name = "GemVisual";
            RobotFx.TintAll(visual, Palette.Tier(PartTier.Crystal));

            spinAxis = new Vector3(Random.Range(-0.4f, 0.4f), 1f, Random.Range(-0.4f, 0.4f)).normalized;
            spinSpeed = Random.Range(540f, 800f);

            // Little pink sparkle trail.
            var trail = RobotFx.NewSystem("GemTrail", holder, Vector3.zero, RobotFx.DotMaterial);
            var main = trail.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(Palette.Tier(PartTier.Crystal), Palette.Cream);
            var shape = trail.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            RobotFx.SetRate(trail, 40f);
            RobotFx.SetColorOverLifetime(trail, RobotFx.Gradient3(Color.white, Color.white, Color.white, 1f, 0.8f, 0f));
            RobotFx.SetSizeOverLifetime(trail, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            trail.Play(true);
        }

        void Update()
        {
            if (landed) return;
            float dt = Time.deltaTime;
            elapsed += dt;

            if (gem != null) gem.Rotate(spinAxis, spinSpeed * dt, Space.World);
            UpdateVisuals();

            if (elapsed >= flightTime) Land();
        }

        void UpdateVisuals()
        {
            float t = Mathf.Clamp01(elapsed / flightTime);

            if (gem != null)
            {
                Vector3 p = Vector3.Lerp(from, target, t);
                p.y += arcHeight * 4f * t * (1f - t);
                gem.position = p;
            }

            float pulse = Mathf.Sin(elapsed * (14f + 14f * t));
            if (ring != null) ring.localScale = ringBaseScale * (1f + 0.07f * pulse);

            Color ringColor = Palette.Danger;
            ringColor.a = 0.7f + 0.3f * pulse;
            if (ringMat != null) RobotFx.SetMaterialColor(ringMat, ringColor);
            if (ringPrefabRenderers != null)
                for (int i = 0; i < ringPrefabRenderers.Length; i++) RobotFx.Tint(ringPrefabRenderers[i], ringColor);

            if (fill != null)
            {
                float r = Mathf.Max(0.01f, hitRadius * t);
                fill.localScale = new Vector3(r, 1f, r);
            }
            if (fillMat != null)
            {
                Color fc = Palette.Danger;
                fc.a = 0.22f + 0.18f * t;
                RobotFx.SetMaterialColor(fillMat, fc);
            }

            if (countdown != null)
            {
                float remaining = Mathf.Max(0f, flightTime - elapsed);
                countdown.text = remaining.ToString("0.0", CultureInfo.InvariantCulture);
                countdown.transform.localScale = Vector3.one * (1f + 0.15f * Mathf.Abs(pulse));
                RobotFx.FaceCamera(countdown.transform);
            }
        }

        void Land()
        {
            if (landed) return;
            landed = true;

            var ctx = MatchContext.Instance;
            if (ctx != null)
            {
                float r2 = hitRadius * hitRadius;
                for (int i = 0; i < 2; i++)
                {
                    Transform p = ctx.Player((PlayerId)i);
                    if (p == null) continue;
                    Vector3 d = p.position - target;
                    d.y = 0f;
                    if (d.sqrMagnitude > r2) continue;

                    IStunnable s = p.GetComponent<IStunnable>();
                    if (s == null) s = p.GetComponentInChildren<IStunnable>();
                    if (s == null) s = p.GetComponentInParent<IStunnable>();
                    if (s != null && !s.IsStunned) s.Stun(stunDuration);
                }
            }

            Vector3 at = target + Vector3.up * 0.2f;
            // Pink crystal shards
            RobotFx.Burst("GemShatter", at, RobotFx.DotMaterial, 30,
                Palette.Tier(PartTier.Crystal), Palette.Cream, 2.5f, 5f, 0.08f, 0.2f, 0.4f, 0.8f, 1.4f);
            // Soft pastel poof
            RobotFx.Burst("GemPoof", at, RobotFx.DotMaterial, 12,
                Palette.Cream, Palette.Tier(PartTier.Crystal), 0.6f, 1.4f, 0.3f, 0.55f, 0.35f, 0.6f, -0.1f);

            var cam = Camera.main;
            if (cam != null) cam.SendMessage("Shake", 0.15f, SendMessageOptions.DontRequireReceiver);

            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (ringMat != null) Destroy(ringMat);
            if (fillMat != null) Destroy(fillMat);
        }
    }
}
