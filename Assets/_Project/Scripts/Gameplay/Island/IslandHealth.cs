using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// A player island's HP and rocket reactions (craters, flying props, scared crabs, KO shake + crack tint).
    /// Put it on the island root (top surface at the root's y). Registers with MatchContext.
    /// </summary>
    public class IslandHealth : MonoBehaviour, IRocketTarget
    {
        [Tooltip("Whose island this is.")]
        public PlayerId owner;
        [Tooltip("Top-surface center. Defaults to this transform.")]
        public Transform topCenter;
        [Tooltip("Rockets aim at a random point within ± this many meters of the center (X and Z).")]
        public float aimRadius = 3f;
        [Tooltip("FX/Crater prefab (flat disc, radius 1). Placeholder disc if empty.")]
        public GameObject craterPrefab;
        [Tooltip("Parent of decoration props. Defaults to child 'Props'.")]
        public Transform propsRoot;
        [Tooltip("Props within this distance of an impact get knocked.")]
        public float knockRadius = 2.5f;
        [Tooltip("Props closer than this to the impact tumble into the sea (back on reset).")]
        public float tumbleRadius = 0.8f;
        [Tooltip("Half size of the island top, used to keep knocked props on the island.")]
        public float topHalfExtent = 3.8f;
        [Tooltip("World height of the sea surface.")]
        public float seaY = -2.6f;
        [Tooltip("Transforms shaken when hit / KO'd. Defaults to children 'Top' and 'Side', else this transform.")]
        public Transform[] shakeTargets;
        [Tooltip("Renderers darkened at 0 HP. Defaults to renderers under 'Top' and 'Side'.")]
        public Renderer[] crackRenderers;

        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public bool IsDestroyed => Hp <= 0;
        /// <summary>The next rocket is absorbed instead of hurting the island.</summary>
        public bool ShieldActive => shieldDome != null;

        GameObject shieldDome;

        struct PropState
        {
            public Vector3 localPos;
            public Quaternion localRot;
            public Vector3 localScale;
            public bool active;
        }

        readonly Dictionary<Transform, PropState> propOriginals = new Dictionary<Transform, PropState>();
        readonly Dictionary<Transform, Coroutine> propAnims = new Dictionary<Transform, Coroutine>();
        readonly List<GameObject> craters = new List<GameObject>();
        readonly List<Color> crackOriginalColors = new List<Color>();
        Vector3[] shakeBase;
        Coroutine shakeRoutine;
        bool cracked;

        Vector3 Center => topCenter != null ? topCenter.position : transform.position;

        void Awake()
        {
            if (propsRoot == null) propsRoot = transform.Find("Props");
            if (shakeTargets == null || shakeTargets.Length == 0)
            {
                var list = new List<Transform>();
                var top = transform.Find("Top");
                var side = transform.Find("Side");
                if (top != null) list.Add(top);
                if (side != null) list.Add(side);
                if (list.Count == 0) list.Add(transform);
                shakeTargets = list.ToArray();
            }
            if (crackRenderers == null || crackRenderers.Length == 0)
            {
                var list = new List<Renderer>();
                var top = transform.Find("Top");
                var side = transform.Find("Side");
                if (top != null) list.AddRange(top.GetComponentsInChildren<Renderer>(true));
                if (side != null) list.AddRange(side.GetComponentsInChildren<Renderer>(true));
                crackRenderers = list.ToArray();
            }
            shakeBase = new Vector3[shakeTargets.Length];
            for (int i = 0; i < shakeTargets.Length; i++)
                if (shakeTargets[i] != null) shakeBase[i] = shakeTargets[i].localPosition;

            RecordProps();
            ResetHpOnly();
        }

        void OnEnable() { Register(); }
        void Start() { Register(); }

        void Register()
        {
            if (MatchContext.Instance != null) MatchContext.Instance.RegisterIsland(owner, this);
        }

        void ResetHpOnly()
        {
            var cfg = GameplayUtil.Config;
            MaxHp = cfg != null ? cfg.islandMaxHp : 600;
            Hp = MaxHp;
        }

        void RecordProps()
        {
            if (propsRoot == null) return;
            foreach (Transform child in propsRoot)
            {
                if (propOriginals.ContainsKey(child)) continue;
                propOriginals[child] = new PropState
                {
                    localPos = child.localPosition,
                    localRot = child.localRotation,
                    localScale = child.localScale,
                    active = child.gameObject.activeSelf
                };
            }
        }

        // ---------------- IRocketTarget ----------------

        public Vector3 GetAimPoint()
        {
            Vector3 c = Center;
            return new Vector3(c.x + Random.Range(-aimRadius, aimRadius), c.y, c.z + Random.Range(-aimRadius, aimRadius));
        }

        public void TakeRocketHit(RocketHit hit)
        {
            var ctx = MatchContext.Instance;
            if (ctx != null && !ctx.IsPlaying) return;
            if (Hp <= 0) return;

            if (ShieldActive)
            {
                RemoveShield();
                GameEvents.RaiseShieldBlocked(owner, hit.point);
                RobotFloatingText.Spawn("BLOCKED!", hit.point + Vector3.up * 1.5f, Palette.HelmetYellow, 0.1f, 1f, 0.9f);
                RobotFx.Burst("ShieldPop", hit.point + Vector3.up, RobotFx.RingMaterial, 24, Palette.Sea, Palette.Cream, 2f, 5f, 0.2f, 0.5f, 0.4f, 0.8f, 0.2f);
                return;
            }

            int dmg = hit.recipe != null ? Mathf.Max(0, hit.recipe.damage) : 0;
            float impact = hit.recipe != null ? hit.recipe.impactScale : 1f;
            Vector3 point = new Vector3(hit.point.x, Center.y, hit.point.z);

            Hp = Mathf.Max(0, Hp - dmg);

            SpawnCrater(point, impact);
            KnockProps(point);
            if (propsRoot != null)
                propsRoot.BroadcastMessage("Scare", point, SendMessageOptions.DontRequireReceiver);

            if (Hp <= 0)
            {
                ApplyCrackTint();
                StartShake(0.25f, 0.9f);
            }
            else
            {
                StartShake(0.05f + 0.04f * impact, 0.3f);
            }

            GameEvents.RaiseIslandDamaged(owner, dmg, Hp, point);
        }

        // ---------------- Shield ----------------

        public void GiveShield()
        {
            if (ShieldActive) return;
            shieldDome = GameplayUtil.CreatePrimitive(PrimitiveType.Sphere, "ShieldDome");
            shieldDome.transform.SetParent(transform, true);
            shieldDome.transform.position = Center;
            shieldDome.transform.localScale = new Vector3(topHalfExtent * 2.9f, 6f, topHalfExtent * 2.9f);
            var r = shieldDome.GetComponent<Renderer>();
            r.sharedMaterial = RobotFx.CreateTransparentMaterial(null);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            RobotFx.SetMaterialColor(r.sharedMaterial, new Color(Palette.Sea.r, 0.85f, 1f, 0.28f));
            StartCoroutine(PopIn(shieldDome.transform));
        }

        void RemoveShield()
        {
            if (shieldDome == null) return;
            Destroy(shieldDome.GetComponent<Renderer>().sharedMaterial);
            Destroy(shieldDome);
            shieldDome = null;
        }

        // ---------------- Reset ----------------

        /// <summary>Full HP, craters removed, props restored, crack tint and shake cleared.</summary>
        public void ResetIsland()
        {
            StopAllCoroutines();
            propAnims.Clear();
            shakeRoutine = null;

            ResetHpOnly();
            RemoveShield();

            foreach (var c in craters) if (c != null) Destroy(c);
            craters.Clear();

            foreach (var kv in propOriginals)
            {
                var t = kv.Key;
                if (t == null) continue;
                t.localPosition = kv.Value.localPos;
                t.localRotation = kv.Value.localRot;
                t.localScale = kv.Value.localScale;
                t.gameObject.SetActive(kv.Value.active);
            }

            for (int i = 0; i < shakeTargets.Length; i++)
                if (shakeTargets[i] != null) shakeTargets[i].localPosition = shakeBase[i];

            ClearCrackTint();
        }

        // ---------------- Craters ----------------

        void SpawnCrater(Vector3 point, float impact)
        {
            Vector3 pos = point + Vector3.up * 0.015f;
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject crater;
            if (craterPrefab != null)
            {
                crater = Instantiate(craterPrefab, pos, rot, transform);
                crater.transform.localScale = Vector3.Scale(crater.transform.localScale, new Vector3(impact, 1f, impact));
            }
            else
            {
                crater = GameplayUtil.CreatePrimitive(PrimitiveType.Cylinder, "Crater");
                crater.transform.SetParent(transform, true);
                crater.transform.SetPositionAndRotation(pos, rot);
                crater.transform.localScale = new Vector3(2f * impact, 0.01f, 2f * impact);
                GameplayUtil.Tint(crater, Color.Lerp(Palette.Ink, Palette.IslandTop, 0.35f));
            }
            craters.Add(crater);
            StartCoroutine(PopIn(crater.transform));
        }

        IEnumerator PopIn(Transform t)
        {
            if (t == null) yield break;
            Vector3 target = t.localScale;
            float time = 0f;
            const float dur = 0.18f;
            while (time < dur && t != null)
            {
                time += Time.deltaTime;
                float k = GameplayUtil.EaseOutBack(Mathf.Clamp01(time / dur));
                t.localScale = new Vector3(target.x * k, target.y, target.z * k);
                yield return null;
            }
            if (t != null) t.localScale = target;
        }

        // ---------------- Props ----------------

        void KnockProps(Vector3 point)
        {
            if (propsRoot == null) return;
            RecordProps();
            foreach (Transform child in propsRoot)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                if (propAnims.ContainsKey(child)) continue;
                float d = GameplayUtil.DistanceXZ(child.position, point);
                if (d > knockRadius) continue;
                bool tumble = d < tumbleRadius;
                propAnims[child] = StartCoroutine(tumble ? TumbleIntoSea(child, point) : HopProp(child, point, d));
            }
        }

        Vector3 AwayDir(Vector3 from, Vector3 point)
        {
            Vector3 dir = from - point;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            return dir.normalized;
        }

        IEnumerator HopProp(Transform prop, Vector3 point, float distance)
        {
            Vector3 c = Center;
            Vector3 startPos = prop.position;
            float strength = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, knockRadius));
            Vector3 endPos = startPos + AwayDir(startPos, point) * (0.4f + 1.2f * strength);
            endPos.x = Mathf.Clamp(endPos.x, c.x - topHalfExtent, c.x + topHalfExtent);
            endPos.z = Mathf.Clamp(endPos.z, c.z - topHalfExtent, c.z + topHalfExtent);
            endPos.y = startPos.y;
            float height = 0.6f + 1.4f * strength;
            float dur = 0.45f + 0.25f * strength;
            Quaternion startRot = prop.rotation;
            Vector3 spinAxis = Vector3.Cross(Vector3.up, AwayDir(startPos, point));
            float spinDeg = 360f * Mathf.Round(1f + strength);
            float yaw = Random.Range(-60f, 60f);

            float t = 0f;
            while (t < dur)
            {
                if (prop == null) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                prop.position = Vector3.Lerp(startPos, endPos, k) + Vector3.up * (height * 4f * k * (1f - k));
                prop.rotation = Quaternion.AngleAxis(spinDeg * k, spinAxis) * Quaternion.AngleAxis(yaw * k, Vector3.up) * startRot;
                yield return null;
            }
            if (prop == null) yield break;
            prop.position = endPos;
            prop.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * startRot;

            // little landing squash
            Vector3 baseScale = prop.localScale;
            t = 0f;
            const float squashDur = 0.2f;
            while (t < squashDur)
            {
                if (prop == null) yield break;
                t += Time.deltaTime;
                float s = Mathf.Sin(Mathf.Clamp01(t / squashDur) * Mathf.PI) * 0.25f;
                prop.localScale = new Vector3(baseScale.x * (1f + s * 0.5f), baseScale.y * (1f - s), baseScale.z * (1f + s * 0.5f));
                yield return null;
            }
            if (prop != null) prop.localScale = baseScale;
            propAnims.Remove(prop);
        }

        IEnumerator TumbleIntoSea(Transform prop, Vector3 point)
        {
            Vector3 c = Center;
            Vector3 startPos = prop.position;
            Vector3 dir = AwayDir(startPos, point);
            // fly over the island edge
            Vector3 edge = c + dir * (topHalfExtent + 2.5f);
            Vector3 endPos = new Vector3(edge.x, seaY, edge.z);
            float height = 3f;
            float dur = 1.1f;
            Quaternion startRot = prop.rotation;
            Vector3 spinAxis = Vector3.Cross(Vector3.up, dir);
            if (spinAxis.sqrMagnitude < 0.001f) spinAxis = Vector3.right;

            float t = 0f;
            while (t < dur)
            {
                if (prop == null) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                Vector3 flat = Vector3.Lerp(startPos, endPos, k);
                // arc relative to the start height, falling down to the sea
                float y = Mathf.Lerp(startPos.y, endPos.y, k * k) + height * 4f * k * (1f - k);
                prop.position = new Vector3(flat.x, y, flat.z);
                prop.rotation = Quaternion.AngleAxis(720f * k, spinAxis) * startRot;
                yield return null;
            }
            if (prop == null) yield break;
            // bob-sink into the sea
            Vector3 baseScale = prop.localScale;
            t = 0f;
            const float sinkDur = 0.5f;
            while (t < sinkDur)
            {
                if (prop == null) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / sinkDur);
                prop.localScale = baseScale * (1f - k);
                prop.position = endPos + Vector3.down * (0.5f * k);
                yield return null;
            }
            if (prop == null) yield break;
            prop.localScale = baseScale;
            prop.gameObject.SetActive(false);
            propAnims.Remove(prop);
        }

        // ---------------- Shake & crack ----------------

        void StartShake(float amount, float duration)
        {
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            for (int i = 0; i < shakeTargets.Length; i++)
                if (shakeTargets[i] != null) shakeTargets[i].localPosition = shakeBase[i];
            shakeRoutine = StartCoroutine(ShakeRoutine(amount, duration));
        }

        IEnumerator ShakeRoutine(float amount, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / duration);
                Vector3 off = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.3f, 0.3f), Random.Range(-1f, 1f)) * (amount * k);
                for (int i = 0; i < shakeTargets.Length; i++)
                    if (shakeTargets[i] != null) shakeTargets[i].localPosition = shakeBase[i] + off;
                yield return null;
            }
            for (int i = 0; i < shakeTargets.Length; i++)
                if (shakeTargets[i] != null) shakeTargets[i].localPosition = shakeBase[i];
            shakeRoutine = null;
        }

        void ApplyCrackTint()
        {
            if (cracked) return;
            cracked = true;
            crackOriginalColors.Clear();
            foreach (var r in crackRenderers)
            {
                Color orig = GameplayUtil.GetTint(r);
                crackOriginalColors.Add(orig);
                if (r == null) continue;
                Color crackColor = Color.Lerp(orig, Palette.Ink, 0.35f);
                crackColor.a = orig.a;
                GameplayUtil.TintRenderer(r, crackColor);
            }
        }

        void ClearCrackTint()
        {
            if (!cracked) return;
            cracked = false;
            for (int i = 0; i < crackRenderers.Length && i < crackOriginalColors.Count; i++)
                if (crackRenderers[i] != null) GameplayUtil.TintRenderer(crackRenderers[i], crackOriginalColors[i]);
            crackOriginalColors.Clear();
        }
    }
}
