using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// "ฝันร้าย cpe40" — the cross-eyed bulldozer boss on the centre island.
    ///
    /// SETUP (scene builder):
    ///  - Put this component on the root of the Robot/Cpe40 prefab instance (placed on the robot island top).
    ///  - Assign <see cref="gemPrefab"/> = Prefabs/Parts/Gem and <see cref="warningRingPrefab"/> = Prefabs/FX/WarningRing
    ///    (both optional — code fallbacks exist).
    ///  - No config field: all numbers come from MatchContext.Instance.config (GameConfig.rageLevels, robotDrops,
    ///    gemWarningTime, gemHitRadius, stunDuration). Colours come from Palette.
    ///  - Expected children (all optional, found by name): Visual, Visual/Bucket, Visual/EyeL, Visual/EyeR, FireRoot, ThrowPoint.
    ///    Any child under Visual whose name contains "Wheel" or "Track" rocks gently while idle.
    ///
    /// Behaviour: registers itself as MatchContext.Robot; accumulates rocket damage into a rage level (raises
    /// GameEvents.RobotHit + RobotRageChanged on every hit), drops parts to the shooter's island through
    /// IPartSpawner, throws GemProjectiles at players while MatchContext.IsPlaying, resets on PhaseChanged(Countdown).
    /// </summary>
    [DisallowMultipleComponent]
    public class RobotBoss : MonoBehaviour, IRocketTarget
    {
        [Header("Prefabs (assigned by scene builder, optional)")]
        [Tooltip("Art prefab Parts/Gem. Tinted Palette.Tier(Crystal). Fallback: sphere.")]
        public GameObject gemPrefab;
        [Tooltip("Art prefab FX/WarningRing (radius 1, tintable). Fallback: ring mesh built in code.")]
        public GameObject warningRingPrefab;

        [Header("Feel (visual only, gameplay numbers live in GameConfig)")]
        public float bobAmplitude = 0.05f;
        public float bobSpeed = 2.2f;
        public float bucketWindUpAngle = 40f;
        public float windUpTime = 0.4f;
        public float rapidVolleySpacing = 0.25f;
        public float volleySpread = 0.6f;
        public float gemArcHeight = 4.5f;

        public int CurrentRageLevel => level;
        public int AccumulatedDamage => accumulated;

        // rig
        Transform visual, bucket, eyeL, eyeR, fireRoot, throwPoint;
        readonly List<Transform> wheels = new List<Transform>();
        Quaternion[] wheelBaseRot = new Quaternion[0];
        Vector3 baseScale = Vector3.one;
        Vector3 visualBasePos, visualBaseScale = Vector3.one, eyeLBasePos, eyeRBasePos;
        Quaternion visualBaseRot = Quaternion.identity, bucketBaseRot = Quaternion.identity;
        Quaternion eyeLBaseRot = Quaternion.identity, eyeRBaseRot = Quaternion.identity;

        // tint
        Renderer[] bodyRenderers = new Renderer[0];
        Color[] baseColors = new Color[0];
        bool[] isEye = new bool[0];
        bool colorsCaptured;
        bool tintDirty = true;
        float flashTimer;

        // state
        int level;
        int accumulated;
        float rageScale = 1f;
        float throwTimer = -1f;
        bool throwing;
        Coroutine throwRoutine;
        float hitTimer = 10f;
        float bucketTilt;
        int nextTarget;
        bool registered;
        bool rigReady;
        readonly List<GemProjectile> gems = new List<GemProjectile>();

        // fx
        ParticleSystem bubbles, flame, steam, smoke, sparks;

        GameConfig Config => RobotFx.Config;

        // ------------------------------------------------------------------ lifecycle

        void Awake()
        {
            baseScale = transform.localScale;
            CacheRig();
            BuildRageFx();
            nextTarget = Random.value < 0.5f ? 0 : 1;
        }

        void OnEnable()
        {
            GameEvents.PhaseChanged += OnPhaseChanged;
            TryRegister();
        }

        void OnDisable()
        {
            GameEvents.PhaseChanged -= OnPhaseChanged;
            var ctx = MatchContext.Instance;
            if (registered && ctx != null && ReferenceEquals(ctx.Robot, this)) ctx.RegisterRobot(null, null);
            registered = false;
            StopAllCoroutines();
            throwing = false;
            throwRoutine = null;
            bucketTilt = 0f;
        }

        void Start()
        {
            TryRegister();
            CaptureColors();
            if (throwTimer < 0f) throwTimer = CurrentInterval();
            ApplyRageVisuals(level);
            GameEvents.RaiseRobotRageChanged(level, accumulated);
        }

        void TryRegister()
        {
            if (registered) return;
            var ctx = MatchContext.Instance;
            if (ctx == null) return;
            ctx.RegisterRobot(this, transform);
            registered = true;
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Countdown)
            {
                ResetRobot();
            }
            else if (phase != GamePhase.Playing && phase != GamePhase.SuddenDeath)
            {
                StopThrowing();
                ClearProjectiles();
            }
        }

        void ResetRobot()
        {
            StopAllCoroutines();
            throwRoutine = null;
            throwing = false;
            bucketTilt = 0f;
            ClearProjectiles();

            accumulated = 0;
            level = 0;
            rageScale = 1f;
            transform.localScale = baseScale;
            throwTimer = CurrentInterval();
            flashTimer = 0f;
            hitTimer = 10f;
            tintDirty = true;
            ApplyRageVisuals(0);
            GameEvents.RaiseRobotRageChanged(0, 0);
        }

        void StopThrowing()
        {
            if (throwRoutine != null) StopCoroutine(throwRoutine);
            throwRoutine = null;
            throwing = false;
            bucketTilt = 0f;
        }

        void ClearProjectiles()
        {
            for (int i = 0; i < gems.Count; i++)
                if (gems[i] != null) Destroy(gems[i].gameObject);
            gems.Clear();
        }

        // ------------------------------------------------------------------ IRocketTarget

        public Vector3 GetAimPoint()
        {
            return transform.position + Vector3.up * (0.8f * rageScale) + Random.insideUnitSphere * 0.25f;
        }

        public void TakeRocketHit(RocketHit hit)
        {
            var cfg = Config;
            int dmg = hit.recipe != null ? Mathf.Max(0, hit.recipe.damage) : 0;
            accumulated += dmg;

            int newLevel = cfg.RageLevelFor(accumulated);
            if (newLevel != level)
            {
                level = newLevel;
                ApplyRageVisuals(level);
                throwTimer = Mathf.Min(throwTimer, CurrentInterval());
            }

            GameEvents.RaiseRobotHit(hit.shooter, hit.recipe);
            GameEvents.RaiseRobotRageChanged(level, accumulated);

            // hit reaction
            flashTimer = 0.1f;
            tintDirty = true;
            hitTimer = 0f;
            RobotFloatingText.Spawn("BONK!", transform.position + Vector3.up * (2.1f * rageScale) + new Vector3(Random.Range(-0.4f, 0.4f), 0f, 0f),
                Palette.Danger, 0.06f, 0.6f, 0.7f);

            DropParts(hit);
        }

        // ------------------------------------------------------------------ drops

        void DropParts(RocketHit hit)
        {
            var ctx = MatchContext.Instance;
            var cfg = Config;
            if (ctx == null || cfg.robotDrops == null) return;

            PartTier rocketTier = hit.recipe != null ? hit.recipe.tier : PartTier.Scrap;
            bool found = false;
            RobotDrop drop = default;
            for (int i = 0; i < cfg.robotDrops.Length; i++)
            {
                if (cfg.robotDrops[i].rocketTier == rocketTier)
                {
                    drop = cfg.robotDrops[i];
                    found = true;
                    break;
                }
            }
            if (!found || drop.count <= 0) return;

            IPartSpawner spawner = ctx.PartSpawner(hit.shooter);
            if (spawner == null) return;
            StartCoroutine(DropRoutine(spawner, drop, rocketTier));
        }

        IEnumerator DropRoutine(IPartSpawner spawner, RobotDrop drop, PartTier rocketTier)
        {
            for (int i = 0; i < drop.count; i++)
            {
                if (spawner is UnityEngine.Object uo && uo == null) yield break;
                var ctx = MatchContext.Instance;
                if (ctx == null) yield break;

                PartTier current = ctx.CurrentTier;
                var key = new PartKey((PartType)Random.Range(0, 4), current); // Gear/Nut/Fuel/Chip
                bool rare = drop.rareChance > 0f && Random.value < drop.rareChance;
                if (rare)
                {
                    if (rocketTier == PartTier.Crystal) key = new PartKey(PartType.Gem, PartTier.Crystal);
                    else key.tier = (PartTier)Mathf.Min((int)current + 1, (int)PartTier.Crystal);
                }

                Vector3 from = ThrowOrigin();
                spawner.SpawnFlyingPart(key, from);
                bucketTilt = Mathf.Max(bucketTilt, 0.35f);
                if (rare) RareMoment(from, Palette.Tier(key.tier));

                yield return new WaitForSeconds(0.12f);
            }
            if (!throwing) bucketTilt = 0f;
        }

        void RareMoment(Vector3 at, Color tierColor)
        {
            RobotFx.Burst("RareSparkle", at, RobotFx.DotMaterial, 45,
                tierColor, Palette.Cream, 2f, 4.5f, 0.12f, 0.32f, 0.6f, 1.1f, 0.25f);
            RobotFx.Burst("RareSparkleRing", at, RobotFx.RingMaterial, 10,
                tierColor, Palette.HelmetYellow, 0.8f, 1.6f, 0.35f, 0.6f, 0.5f, 0.8f, -0.05f);
            RobotFloatingText.Spawn("RARE DROP!", at + Vector3.up * 0.6f, tierColor, 0.055f, 1.0f, 0.9f);
        }

        // ------------------------------------------------------------------ throwing

        bool TryGetRage(int lv, out RageLevel data)
        {
            var cfg = Config;
            if (cfg.rageLevels == null || cfg.rageLevels.Length == 0)
            {
                data = default;
                return false;
            }
            data = cfg.rageLevels[Mathf.Clamp(lv, 0, cfg.rageLevels.Length - 1)];
            return true;
        }

        float CurrentInterval()
        {
            if (TryGetRage(level, out var rl)) return Mathf.Max(0.5f, rl.throwInterval);
            return Mathf.Max(0.5f, Config.gemWarningTime * 10f);
        }

        void UpdateThrowing(float dt)
        {
            var ctx = MatchContext.Instance;
            if (ctx == null || !ctx.IsPlaying) return;

            if (throwTimer < 0f) throwTimer = CurrentInterval();
            throwTimer -= dt;
            if (throwTimer <= 0f && !throwing)
            {
                throwTimer = CurrentInterval();
                throwRoutine = StartCoroutine(ThrowRoutine());
            }
        }

        IEnumerator ThrowRoutine()
        {
            throwing = true;
            int count = TryGetRage(level, out var rl) ? Mathf.Max(1, rl.gemsPerThrow) : 1;
            bool rapid = level >= 3;

            float wind = Mathf.Max(0.05f, windUpTime);
            for (float e = 0f; e < wind; e += Time.deltaTime)
            {
                bucketTilt = Mathf.SmoothStep(0f, 1f, e / wind);
                yield return null;
            }
            bucketTilt = 1f;

            for (int i = 0; i < count; i++)
            {
                var ctx = MatchContext.Instance;
                if (ctx == null || !ctx.IsPlaying) break;

                Vector3 offset = Vector3.zero;
                if (count > 1)
                {
                    Vector2 c = Random.insideUnitCircle * volleySpread;
                    offset = new Vector3(c.x, 0f, c.y);
                }
                LaunchGem(offset);

                if (rapid && i < count - 1)
                {
                    // little re-cock of the bucket between rapid throws
                    float gap = Mathf.Max(0.05f, rapidVolleySpacing);
                    for (float e = 0f; e < gap; e += Time.deltaTime)
                    {
                        bucketTilt = 0.75f + 0.25f * Mathf.Sin(e / gap * Mathf.PI);
                        yield return null;
                    }
                }
            }

            const float settle = 0.3f;
            for (float e = 0f; e < settle; e += Time.deltaTime)
            {
                bucketTilt = 1f - e / settle;
                yield return null;
            }
            bucketTilt = 0f;
            throwing = false;
            throwRoutine = null;
        }

        void LaunchGem(Vector3 offset)
        {
            var ctx = MatchContext.Instance;
            if (ctx == null) return;

            var id = (PlayerId)nextTarget;
            nextTarget = 1 - nextTarget;
            Transform p = ctx.Player(id);
            if (p == null) p = ctx.Player(id.Opponent());
            if (p == null) return;

            Vector3 targetPoint = p.position + offset;
            var gem = GemProjectile.Launch(ThrowOrigin(), targetPoint, gemPrefab, warningRingPrefab, gemArcHeight);
            gems.RemoveAll(g => g == null);
            if (gem != null) gems.Add(gem);
        }

        Vector3 ThrowOrigin()
        {
            if (throwPoint != null) return throwPoint.position;
            return transform.position + Vector3.up * (2f * rageScale) + transform.forward * 0.6f;
        }

        // ------------------------------------------------------------------ per frame

        void Update()
        {
            if (!registered) TryRegister();
            float dt = Time.deltaTime;
            float t = Time.time;
            hitTimer += dt;

            float targetScale = TryGetRage(level, out var rl) && rl.scale > 0f ? rl.scale : 1f;
            rageScale = Mathf.Lerp(rageScale, targetScale, 1f - Mathf.Exp(-4f * dt));
            transform.localScale = baseScale * rageScale;

            Animate(t);
            UpdateTint(dt);
            UpdateThrowing(dt);
        }

        void Animate(float t)
        {
            if (!rigReady) return;
            int lv = Mathf.Clamp(level, 0, 3);

            float bob = Mathf.Sin(t * bobSpeed) * bobAmplitude;
            float shakeAmt = lv >= 3 ? 0.045f : (lv == 2 ? 0.015f : 0f);
            Vector3 shake = Vector3.zero;
            if (shakeAmt > 0f)
            {
                shake = new Vector3(
                    (Mathf.PerlinNoise(t * 25f, 0.3f) - 0.5f) * 2f,
                    0f,
                    (Mathf.PerlinNoise(0.7f, t * 25f) - 0.5f) * 2f) * shakeAmt;
            }

            float squash = 0f, wobble = 0f;
            const float hitDur = 0.6f;
            if (hitTimer < hitDur)
            {
                float k = 1f - hitTimer / hitDur;
                squash = Mathf.Sin(hitTimer * 30f) * 0.15f * k;
                wobble = Mathf.Sin(hitTimer * 22f) * 8f * k;
            }
            float angry = lv >= 3 ? Mathf.Sin(t * 18f) * 2.5f : 0f;

            if (visual != null)
            {
                visual.localPosition = visualBasePos + new Vector3(0f, bob, 0f) + shake;
                visual.localScale = Vector3.Scale(visualBaseScale, new Vector3(1f + squash, 1f - squash, 1f + squash));
                visual.localRotation = visualBaseRot * Quaternion.Euler(0f, 0f, wobble + angry);
            }

            // cross-eyes wiggle: both eyes drift toward / away from the nose in opposite directions
            float eyeW = Mathf.Sin(t * 3.1f) * 0.6f + Mathf.Sin(t * 7.3f) * 0.3f;
            if (eyeL != null)
            {
                eyeL.localPosition = eyeLBasePos + new Vector3(eyeW * 0.015f, 0f, 0f);
                eyeL.localRotation = eyeLBaseRot * Quaternion.Euler(0f, eyeW * 12f, 0f);
            }
            if (eyeR != null)
            {
                eyeR.localPosition = eyeRBasePos + new Vector3(-eyeW * 0.015f, 0f, 0f);
                eyeR.localRotation = eyeRBaseRot * Quaternion.Euler(0f, -eyeW * 12f, 0f);
            }

            for (int i = 0; i < wheels.Count; i++)
            {
                if (wheels[i] == null) continue;
                wheels[i].localRotation = wheelBaseRot[i] * Quaternion.Euler(Mathf.Sin(t * 2.4f + i) * 6f, 0f, 0f);
            }

            if (bucket != null)
            {
                float idle = Mathf.Sin(t * 1.7f) * 3f;
                bucket.localRotation = bucketBaseRot * Quaternion.Euler(-bucketWindUpAngle * bucketTilt + idle, 0f, 0f);
            }
        }

        void UpdateTint(float dt)
        {
            if (flashTimer > 0f)
            {
                flashTimer -= dt;
                if (flashTimer <= 0f) tintDirty = true;
            }
            if (!tintDirty) return;
            tintDirty = false;
            if (!colorsCaptured) CaptureColors();

            bool flash = flashTimer > 0f;
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                var r = bodyRenderers[i];
                if (r == null) continue;
                Color c = flash ? Color.white : (isEye[i] && level >= 2 ? Palette.Danger : baseColors[i]);
                RobotFx.Tint(r, c);
            }
        }

        // ------------------------------------------------------------------ setup helpers

        void CacheRig()
        {
            visual = transform.Find("Visual");
            if (visual == null) visual = BuildFallbackVisual();

            bucket = FindChild("Visual/Bucket", "Bucket");
            eyeL = FindChild("Visual/EyeL", "EyeL");
            eyeR = FindChild("Visual/EyeR", "EyeR");
            fireRoot = FindChild("FireRoot", "FireRoot");
            throwPoint = FindChild("ThrowPoint", "ThrowPoint");

            if (fireRoot == null)
            {
                fireRoot = new GameObject("FireRoot").transform;
                fireRoot.SetParent(transform, false);
                fireRoot.localPosition = new Vector3(0f, 1.75f, 0f);
            }

            visualBasePos = visual.localPosition;
            visualBaseRot = visual.localRotation;
            visualBaseScale = visual.localScale;
            if (bucket != null) bucketBaseRot = bucket.localRotation;
            if (eyeL != null) { eyeLBasePos = eyeL.localPosition; eyeLBaseRot = eyeL.localRotation; }
            if (eyeR != null) { eyeRBasePos = eyeR.localPosition; eyeRBaseRot = eyeR.localRotation; }

            wheels.Clear();
            var all = visual.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].name.ToLowerInvariant();
                if (all[i] != visual && (n.Contains("wheel") || n.Contains("track"))) wheels.Add(all[i]);
            }
            wheelBaseRot = new Quaternion[wheels.Count];
            for (int i = 0; i < wheels.Count; i++) wheelBaseRot[i] = wheels[i].localRotation;

            var rs = visual.GetComponentsInChildren<Renderer>(true);
            var list = new List<Renderer>();
            for (int i = 0; i < rs.Length; i++)
                if (!(rs[i] is ParticleSystemRenderer)) list.Add(rs[i]);
            bodyRenderers = list.ToArray();
            isEye = new bool[bodyRenderers.Length];
            baseColors = new Color[bodyRenderers.Length];
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                Transform rt = bodyRenderers[i].transform;
                isEye[i] = (eyeL != null && rt.IsChildOf(eyeL)) || (eyeR != null && rt.IsChildOf(eyeR));
                baseColors[i] = Color.white;
            }
            rigReady = true;
        }

        /// <summary>Reads each renderer's current _BaseColor (block first, then material) so flashes restore it.</summary>
        void CaptureColors()
        {
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                var r = bodyRenderers[i];
                if (r == null) continue;
                Color c = Color.white;
                r.GetPropertyBlock(block);
                if (!block.isEmpty && block.HasColor(RobotFx.BaseColorId)) c = block.GetColor(RobotFx.BaseColorId);
                else
                {
                    var m = r.sharedMaterial;
                    if (m != null && m.HasProperty(RobotFx.BaseColorId)) c = m.GetColor(RobotFx.BaseColorId);
                }
                baseColors[i] = c;
            }
            colorsCaptured = true;
            tintDirty = true;
        }

        Transform FindChild(string path, string deepName)
        {
            var t = transform.Find(path);
            if (t != null) return t;
            return FindDeep(transform, deepName);
        }

        static Transform FindDeep(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i);
                if (c.name == name) return c;
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }

        Transform BuildFallbackVisual()
        {
            var v = new GameObject("Visual").transform;
            v.SetParent(transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            var col = body.GetComponent<Collider>();
            if (col != null) Destroy(col);
            body.transform.SetParent(v, false);
            body.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            body.transform.localScale = new Vector3(1.3f, 1.1f, 1.5f);
            RobotFx.Tint(body.GetComponent<Renderer>(), Palette.HelmetYellow);
            return v;
        }

        // ------------------------------------------------------------------ rage FX

        void BuildRageFx()
        {
            Material dot = RobotFx.DotMaterial;

            // LV0: soap bubbles drifting up
            bubbles = RobotFx.NewSystem("Bubbles", fireRoot, Vector3.zero, RobotFx.RingMaterial);
            {
                var m = bubbles.main;
                m.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
                m.startColor = new ParticleSystem.MinMaxGradient(Palette.Sea, Palette.Tier(PartTier.Silver));
                m.gravityModifier = -0.02f;
                var sh = bubbles.shape;
                sh.angle = 25f;
                sh.radius = 0.3f;
                var n = bubbles.noise;
                n.enabled = true;
                n.strength = 0.3f;
                n.frequency = 0.6f;
                RobotFx.SetRate(bubbles, 2.5f);
                RobotFx.SetColorOverLifetime(bubbles, RobotFx.Gradient3(Color.white, Palette.Cream, Color.white, 0f, 1f, 0.6f));
                RobotFx.SetSizeOverLifetime(bubbles, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.85f, 1f), new Keyframe(1f, 1.35f)));
            }

            // LV1+: flames (yellow -> orange -> danger)
            flame = RobotFx.NewSystem("Flame", fireRoot, Vector3.zero, dot);
            {
                var m = flame.main;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.22f);
                m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                m.startColor = new ParticleSystem.MinMaxGradient(Color.white);
                m.gravityModifier = -0.3f;
                var sh = flame.shape;
                sh.angle = 12f;
                sh.radius = 0.12f;
                RobotFx.SetRate(flame, 10f);
                RobotFx.SetColorOverLifetime(flame, RobotFx.Gradient3(Palette.HelmetYellow, Palette.HelmetOrange, Palette.Danger, 1f, 0.95f, 0f));
                RobotFx.SetSizeOverLifetime(flame, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.15f)));
            }

            // LV1: steam puffs
            steam = RobotFx.NewSystem("Steam", fireRoot, new Vector3(0f, -0.1f, 0f), dot);
            {
                var m = steam.main;
                m.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.6f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
                m.startColor = new ParticleSystem.MinMaxGradient(Palette.Cream, Color.white);
                var sh = steam.shape;
                sh.angle = 30f;
                sh.radius = 0.35f;
                RobotFx.SetRate(steam, 4f);
                RobotFx.SetColorOverLifetime(steam, RobotFx.Gradient3(Color.white, Color.white, Color.white, 0.7f, 0.45f, 0f));
                RobotFx.SetSizeOverLifetime(steam, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));
            }

            // LV2+: smoke
            smoke = RobotFx.NewSystem("Smoke", fireRoot, new Vector3(0f, 0.25f, 0f), dot);
            {
                var m = smoke.main;
                m.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.4f);
                m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                m.startColor = new ParticleSystem.MinMaxGradient(Palette.Tier(PartTier.Scrap), Palette.Cream);
                m.gravityModifier = -0.05f;
                var sh = smoke.shape;
                sh.angle = 18f;
                sh.radius = 0.25f;
                var n = smoke.noise;
                n.enabled = true;
                n.strength = 0.25f;
                n.frequency = 0.5f;
                RobotFx.SetRate(smoke, 6f);
                RobotFx.SetColorOverLifetime(smoke, RobotFx.Gradient3(Color.white, Color.white, Color.white, 0.85f, 0.6f, 0f));
                RobotFx.SetSizeOverLifetime(smoke, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.6f));
            }

            // LV3: sparks
            sparks = RobotFx.NewSystem("Sparks", fireRoot, Vector3.zero, dot);
            {
                var m = sparks.main;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(2f, 3.5f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
                m.startColor = new ParticleSystem.MinMaxGradient(Palette.HelmetYellow, Palette.Cream);
                m.gravityModifier = 1f;
                var sh = sparks.shape;
                sh.angle = 45f;
                sh.radius = 0.3f;
                RobotFx.SetRate(sparks, 18f);
                RobotFx.SetColorOverLifetime(sparks, RobotFx.Gradient3(Color.white, Palette.HelmetOrange, Palette.Danger, 1f, 1f, 0f));
                var psr = sparks.GetComponent<ParticleSystemRenderer>();
                psr.renderMode = ParticleSystemRenderMode.Stretch;
                psr.velocityScale = 0.06f;
                psr.lengthScale = 1.5f;
            }
        }

        void ApplyRageVisuals(int lv)
        {
            int v = Mathf.Clamp(lv, 0, 3);
            RobotFx.Toggle(bubbles, v == 0);
            RobotFx.Toggle(steam, v == 1);
            RobotFx.Toggle(flame, v >= 1);
            RobotFx.Toggle(smoke, v >= 2);
            RobotFx.Toggle(sparks, v >= 3);

            if (flame != null && v >= 1)
            {
                float rate = v == 1 ? 10f : (v == 2 ? 24f : 50f);
                float size = v == 1 ? 0.22f : (v == 2 ? 0.42f : 0.7f);
                float speed = v == 1 ? 0.9f : (v == 2 ? 1.3f : 2f);
                float radius = v == 1 ? 0.12f : (v == 2 ? 0.25f : 0.5f);
                var m = flame.main;
                m.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size);
                m.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed);
                var sh = flame.shape;
                sh.radius = radius;
                RobotFx.SetRate(flame, rate);
            }

            if (smoke != null && v >= 2)
            {
                var m = smoke.main;
                Color grey = Palette.Tier(PartTier.Scrap);
                if (v >= 3)
                {
                    m.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.7f);
                    m.startColor = new ParticleSystem.MinMaxGradient(grey, Color.Lerp(grey, Palette.Ink, 0.35f));
                    RobotFx.SetRate(smoke, 14f);
                }
                else
                {
                    m.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.4f);
                    m.startColor = new ParticleSystem.MinMaxGradient(grey, Palette.Cream);
                    RobotFx.SetRate(smoke, 6f);
                }
            }
            tintDirty = true; // eye colour depends on level
        }
    }
}
