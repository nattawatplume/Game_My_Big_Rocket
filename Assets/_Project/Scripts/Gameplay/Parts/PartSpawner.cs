using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// One per player island. While playing, every GameConfig.partDropInterval it shows a tier-tinted warning ring +
    /// light beam with a countdown, then drops a part from the sky that becomes a <see cref="PartPickup"/>.
    /// Also receives parts flung by the robot (<see cref="SpawnFlyingPart"/>).
    /// </summary>
    public class PartSpawner : MonoBehaviour, IPartSpawner
    {
        [Tooltip("Which player's island this spawner drops parts on.")]
        public PlayerId owner;
        [Tooltip("Top-surface center of the island. Defaults to this transform.")]
        public Transform islandCenter;
        [Tooltip("Parts land within ± this many meters of the center (X and Z).")]
        public float spawnHalfExtent = 3.3f;
        [Tooltip("Craft ring center to keep clear. Defaults to child 'CraftRing', else center + (0,0,-1.8).")]
        public Transform craftRingCenter;
        [Tooltip("No parts land within this distance of the craft ring center.")]
        public float craftAvoidRadius = 1.6f;
        [Tooltip("Minimum distance between parts (and pending drops).")]
        public float partSpacing = 1.0f;

        [Header("Prefabs (index = PartType: Gear, Nut, Fuel, Chip, Gem). Primitive fallback if empty.")]
        public GameObject[] partPrefabs = new GameObject[5];
        [Tooltip("FX/WarningRing prefab (flat ring, radius 1).")]
        public GameObject warningRingPrefab;
        [Tooltip("FX/LightBeam prefab (tall translucent cylinder, height 1).")]
        public GameObject lightBeamPrefab;

        [Header("Look")]
        public float warningRingRadius = 0.55f;
        public float beamHeight = 5f;
        [Tooltip("Beam height for rare tiers (Gold, Crystal).")]
        public float rareBeamHeight = 10f;
        public float beamWidth = 0.5f;
        public float dropHeight = 8f;
        public float fallTime = 0.45f;
        [Tooltip("Flight time of parts flung by the robot.")]
        public float flyTime = 0.9f;
        public float flyArcHeight = 3.5f;

        readonly List<GameObject> tracked = new List<GameObject>();
        readonly List<Vector3> reserved = new List<Vector3>();
        float timer;

        Vector3 Center => islandCenter != null ? islandCenter.position : transform.position;

        Vector3 RingCenter
        {
            get
            {
                if (craftRingCenter != null) return craftRingCenter.position;
                return Center + new Vector3(0f, 0f, -1.8f);
            }
        }

        void Awake()
        {
            if (craftRingCenter == null)
            {
                var root = islandCenter != null ? islandCenter : transform;
                craftRingCenter = root.Find("CraftRing");
            }
        }

        void OnEnable() { Register(); }
        void Start() { Register(); }

        void Register()
        {
            if (MatchContext.Instance != null) MatchContext.Instance.RegisterPartSpawner(owner, this);
        }

        void Update()
        {
            var ctx = MatchContext.Instance;
            if (ctx == null || !ctx.IsPlaying || ctx.config == null) return;

            timer += Time.deltaTime;
            float interval = Mathf.Max(0.5f, ctx.config.partDropInterval);
            if (timer >= interval)
            {
                timer -= interval;
                if (TryFindFreeSpot(out Vector3 spot))
                {
                    var key = new PartKey(PickType(ctx.CurrentTier, ctx.config), ctx.CurrentTier);
                    StartCoroutine(DropRoutine(key, spot));
                }
            }
        }

        // ---------------- Public API ----------------

        public void SpawnFlyingPart(PartKey key, Vector3 from)
        {
            if (!TryFindFreeSpot(out Vector3 spot))
            {
                Vector3 c = Center;
                spot = new Vector3(c.x + Random.Range(-spawnHalfExtent, spawnHalfExtent), c.y,
                    c.z + Random.Range(-spawnHalfExtent, spawnHalfExtent));
            }
            StartCoroutine(FlyRoutine(key, from, spot));
        }

        /// <summary>Removes every part, warning and in-flight drop (match reset).</summary>
        public void ClearAll()
        {
            StopAllCoroutines();
            var copy = new List<GameObject>(tracked);
            tracked.Clear();
            reserved.Clear();
            foreach (var go in copy) if (go != null) Destroy(go);
            timer = 0f;
        }

        /// <summary>Called by PartPickup when it is destroyed.</summary>
        public void Forget(GameObject go)
        {
            tracked.Remove(go);
        }

        // ---------------- Spot / type selection ----------------

        bool TryFindFreeSpot(out Vector3 spot)
        {
            Vector3 c = Center;
            Vector3 ring = RingCenter;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                Vector3 p = new Vector3(c.x + Random.Range(-spawnHalfExtent, spawnHalfExtent), c.y,
                    c.z + Random.Range(-spawnHalfExtent, spawnHalfExtent));
                if (GameplayUtil.DistanceXZ(p, ring) < craftAvoidRadius) continue;
                if (!IsClear(p)) continue;
                spot = p;
                return true;
            }
            spot = c;
            return false;
        }

        bool IsClear(Vector3 p)
        {
            foreach (var r in reserved)
                if (GameplayUtil.DistanceXZ(p, r) < partSpacing) return false;
            foreach (var go in tracked)
            {
                if (go == null) continue;
                if (go.GetComponent<PartPickup>() == null) continue; // warnings/falls are covered by reserved
                if (GameplayUtil.DistanceXZ(p, go.transform.position) < partSpacing) return false;
            }
            return true;
        }

        static PartType PickType(PartTier tier, GameConfig cfg)
        {
            float[] w = cfg.partTypeWeights;
            int n = System.Enum.GetValues(typeof(PartType)).Length;
            float total = 0f;
            for (int i = 0; i < n; i++) total += Weight(w, i, tier);
            if (total <= 0f) return PartType.Gear;
            float roll = Random.value * total;
            for (int i = 0; i < n; i++)
            {
                roll -= Weight(w, i, tier);
                if (roll <= 0f) return (PartType)i;
            }
            return PartType.Gear;
        }

        static float Weight(float[] w, int i, PartTier tier)
        {
            if (w == null || i >= w.Length) return 0f;
            if ((PartType)i == PartType.Gem && tier != PartTier.Crystal) return 0f;
            return Mathf.Max(0f, w[i]);
        }

        // ---------------- Spawning ----------------

        GameObject CreatePart(PartKey key, Vector3 position)
        {
            var root = new GameObject("Part_" + key.tier + "_" + key.type);
            root.transform.SetParent(transform, true);
            root.transform.position = position;

            int idx = (int)key.type;
            GameObject prefab = partPrefabs != null && idx < partPrefabs.Length ? partPrefabs[idx] : null;
            if (prefab != null)
            {
                var vis = Instantiate(prefab, root.transform);
                vis.name = "Visual";
                vis.transform.localPosition = Vector3.zero;
                vis.transform.localRotation = Quaternion.identity;
            }
            else
            {
                PrimitiveType type;
                Vector3 scale;
                Quaternion rot = Quaternion.identity;
                switch (key.type)
                {
                    case PartType.Gear: type = PrimitiveType.Cylinder; scale = new Vector3(0.45f, 0.06f, 0.45f); break;
                    case PartType.Nut: type = PrimitiveType.Cube; scale = new Vector3(0.28f, 0.18f, 0.28f); rot = Quaternion.Euler(0f, 45f, 0f); break;
                    case PartType.Fuel: type = PrimitiveType.Capsule; scale = new Vector3(0.26f, 0.22f, 0.26f); break;
                    case PartType.Chip: type = PrimitiveType.Cube; scale = new Vector3(0.38f, 0.07f, 0.38f); break;
                    default: type = PrimitiveType.Sphere; scale = new Vector3(0.32f, 0.42f, 0.32f); break;
                }
                var vis = GameplayUtil.CreatePrimitive(type, "Visual");
                vis.transform.SetParent(root.transform, false);
                vis.transform.localScale = scale;
                vis.transform.localRotation = rot;
                // primitives pivot at their center; cylinder/capsule are 2 units tall, cube/sphere 1
                float halfHeight = (type == PrimitiveType.Cylinder || type == PrimitiveType.Capsule) ? scale.y : scale.y * 0.5f;
                vis.transform.localPosition = new Vector3(0f, halfHeight, 0f);
            }

            GameplayUtil.Tint(root, Palette.Tier(key.tier));
            tracked.Add(root);
            return root;
        }

        IEnumerator DropRoutine(PartKey key, Vector3 spot)
        {
            reserved.Add(spot);
            Color tierColor = Palette.Tier(key.tier);
            bool rare = key.tier == PartTier.Gold || key.tier == PartTier.Crystal;
            float height = rare ? rareBeamHeight : beamHeight;

            var warning = new GameObject("PartWarning_" + key);
            warning.transform.SetParent(transform, true);
            warning.transform.position = spot;
            tracked.Add(warning);

            // ring
            GameObject ring;
            if (warningRingPrefab != null)
            {
                ring = Instantiate(warningRingPrefab, warning.transform);
                ring.transform.localPosition = Vector3.up * 0.02f;
                ring.transform.localScale = Vector3.Scale(ring.transform.localScale, new Vector3(warningRingRadius, 1f, warningRingRadius));
            }
            else
            {
                ring = GameplayUtil.CreatePrimitive(PrimitiveType.Cylinder, "WarningRing");
                ring.transform.SetParent(warning.transform, false);
                ring.transform.localPosition = Vector3.up * 0.02f;
                ring.transform.localScale = new Vector3(warningRingRadius * 2f, 0.01f, warningRingRadius * 2f);
            }
            Color ringColor = tierColor; ringColor.a = 0.85f;
            GameplayUtil.Tint(ring, ringColor);
            Vector3 ringBaseScale = ring.transform.localScale;

            // beam
            GameObject beam;
            if (lightBeamPrefab != null)
            {
                beam = Instantiate(lightBeamPrefab, warning.transform);
                beam.transform.localPosition = Vector3.zero;
                beam.transform.localScale = new Vector3(beamWidth, height, beamWidth);
            }
            else
            {
                beam = GameplayUtil.CreatePrimitive(PrimitiveType.Cylinder, "LightBeam");
                beam.transform.SetParent(warning.transform, false);
                beam.transform.localScale = new Vector3(0.12f, height * 0.5f, 0.12f);
                beam.transform.localPosition = Vector3.up * (height * 0.5f);
            }
            Color beamColor = tierColor; beamColor.a = 0.45f;
            GameplayUtil.Tint(beam, beamColor);

            if (rare) FxFactory.CreateSparkle(warning.transform, Vector3.zero, tierColor, Mathf.Min(height, 4f));

            var text = GameplayUtil.CreateTextMesh(warning.transform, Vector3.up * 1.1f, 0.1f, Palette.Ink);

            var cfg = GameplayUtil.Config;
            float remaining = cfg != null ? cfg.partWarningTime : 2.5f;
            float pulse = 0f;
            while (remaining > 0f)
            {
                var ctx = MatchContext.Instance;
                if (ctx == null || ctx.IsPlaying) remaining -= Time.deltaTime;
                pulse += Time.deltaTime;
                if (text != null)
                {
                    text.text = Mathf.CeilToInt(Mathf.Max(0.01f, remaining)).ToString();
                    GameplayUtil.Billboard(text.transform);
                }
                if (ring != null)
                {
                    float s = 1f + 0.12f * Mathf.Sin(pulse * 10f);
                    ring.transform.localScale = new Vector3(ringBaseScale.x * s, ringBaseScale.y, ringBaseScale.z * s);
                }
                yield return null;
            }

            tracked.Remove(warning);
            Destroy(warning);

            // fall from the sky
            var part = CreatePart(key, spot + Vector3.up * dropHeight);
            float t = 0f;
            float dur = Mathf.Max(0.05f, fallTime);
            while (t < dur)
            {
                if (part == null) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                part.transform.position = spot + Vector3.up * (dropHeight * (1f - k * k));
                yield return null;
            }

            yield return Land(part, spot);
            reserved.Remove(spot);
            MakePickup(part, key, spot);
        }

        IEnumerator FlyRoutine(PartKey key, Vector3 from, Vector3 spot)
        {
            reserved.Add(spot);
            var part = CreatePart(key, from);
            float t = 0f;
            float dur = Mathf.Max(0.05f, flyTime);
            float arc = flyArcHeight + Mathf.Max(0f, from.y - spot.y) * 0.25f;
            while (t < dur)
            {
                if (part == null) { reserved.Remove(spot); yield break; }
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                part.transform.position = Vector3.Lerp(from, spot, k) + Vector3.up * (arc * 4f * k * (1f - k));
                part.transform.Rotate(200f * Time.deltaTime, 360f * Time.deltaTime, 0f, Space.Self);
                yield return null;
            }
            if (part != null) part.transform.rotation = Quaternion.identity;
            yield return Land(part, spot);
            reserved.Remove(spot);
            MakePickup(part, key, spot);
        }

        IEnumerator Land(GameObject part, Vector3 spot)
        {
            if (part == null) yield break;
            Vector3 baseScale = part.transform.localScale;
            float t = 0f;
            const float dur = 0.28f;
            while (t < dur)
            {
                if (part == null) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float hop = 0.3f * 4f * k * (1f - k);
                float squash = Mathf.Sin(k * Mathf.PI) * (k < 0.3f ? 0.3f : 0.1f);
                part.transform.position = spot + Vector3.up * hop;
                part.transform.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y * (1f - squash), baseScale.z * (1f + squash));
                yield return null;
            }
            if (part != null)
            {
                part.transform.position = spot;
                part.transform.localScale = baseScale;
            }
        }

        void MakePickup(GameObject part, PartKey key, Vector3 spot)
        {
            if (part == null) return;
            var pickup = part.AddComponent<PartPickup>();
            pickup.Init(key, this, spot);
        }
    }
}
