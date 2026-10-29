using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Dresses a character from <see cref="CosmeticsStore"/>: hides/shows the built-in Visual/Helmet, spawns hat, face
    /// and back prefabs under "Visual" (so they squash with the body) and tints Visual/Body + feet via _BaseColor.
    /// Accessory renderers named "BodyTint…" follow the body color too (cat ears).
    /// In <see cref="CosmeticsStore.PreviewMode"/> the character floats up, grows and spins slowly. That motion lives on
    /// an extra "LookPivot" inserted between the root and "Visual", so PlayerController (root + Visual) and
    /// SquashStretch (Visual) are never fought.
    /// </summary>
    [DisallowMultipleComponent]
    public class CharacterLook : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly CharacterLook[] active = new CharacterLook[2];

        public PlayerId id;
        [Tooltip("World meters the character floats up while customizing.")]
        public float previewLift = 1.5f;
        [Tooltip("Extra scale while customizing.")]
        public float previewScale = 1.5f;
        [Tooltip("Turntable speed while customizing (degrees/second).")]
        public float spinSpeed = 45f;

        Transform visual, pivot, helmet;
        Renderer[] bodyRenderers = new Renderer[0];
        readonly GameObject[] spawned = new GameObject[4];
        readonly int[] shown = { -1, -1, -1, -1 };
        MaterialPropertyBlock block;
        SquashStretch squash;
        float blend, spin;
        bool started;

        /// <summary>The look component of a player, if any.</summary>
        public static CharacterLook For(PlayerId id) => active[(int)id];

        /// <summary>0 = standing on the island, 1 = fully floating in preview.</summary>
        public float PreviewBlend => blend;

        /// <summary>World-space middle of the (possibly floating, enlarged) body — handy for UI sparkles.</summary>
        public Vector3 BodyCenter
        {
            get
            {
                var t = pivot != null ? pivot : transform;
                return t.TransformPoint(new Vector3(0f, 0.5f, 0f));
            }
        }

        /// <summary>Adds the component to characters in scenes built before it existed.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoAttach()
        {
            foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                if (pc.GetComponent<CharacterLook>() == null)
                    pc.gameObject.AddComponent<CharacterLook>().id = pc.id;
        }

        void Awake()
        {
            visual = transform.Find("Visual");
            squash = GetComponent<SquashStretch>();
            if (visual == null) return;
            helmet = visual.Find("Helmet");
            var list = new System.Collections.Generic.List<Renderer>();
            foreach (var n in new[] { "Body", "FootL", "FootR" })
            {
                var c = visual.Find(n);
                if (c != null && c.TryGetComponent(out Renderer r)) list.Add(r);
            }
            bodyRenderers = list.ToArray();
        }

        void OnEnable()
        {
            CosmeticsStore.Changed += OnLookChanged;
            if (started) Apply(false);
        }

        void OnDisable()
        {
            CosmeticsStore.Changed -= OnLookChanged;
            if (active[(int)id] == this) active[(int)id] = null;
        }

        void Start()
        {
            // Runs after every Awake, so PlayerController/SquashStretch have already cached "Visual".
            if (visual != null && visual.parent == transform)
            {
                pivot = new GameObject("LookPivot").transform;
                pivot.SetParent(transform, false);
                pivot.SetSiblingIndex(visual.GetSiblingIndex());
                visual.SetParent(pivot, false);
            }
            started = true;
            Apply(false);
        }

        void OnLookChanged(PlayerId who)
        {
            if (who == id && started) Apply(true);
        }

        /// <summary>Re-reads the stored look and updates accessories and colors.</summary>
        public void Apply(bool celebrate)
        {
            if (visual == null) return;
            var look = CosmeticsStore.Get(id);
            var lib = CosmeticLibrary.Instance;

            if (helmet != null) helmet.gameObject.SetActive(look.hat == CosmeticCatalog.HelmetHat);
            bool changed = false;
            changed |= Swap(CosmeticSlot.Hat, look.hat, lib);
            changed |= Swap(CosmeticSlot.Face, look.face, lib);
            changed |= Swap(CosmeticSlot.Back, look.back, lib);
            changed |= shown[(int)CosmeticSlot.Color] != look.color;
            shown[(int)CosmeticSlot.Color] = look.color;
            Tint(CosmeticCatalog.BodyColor(look.color));

            if (celebrate && changed && squash != null) squash.Hop();
        }

        bool Swap(CosmeticSlot slot, int index, CosmeticLibrary lib)
        {
            int s = (int)slot;
            if (shown[s] == index && (spawned[s] != null || lib == null || lib.Prefab(slot, index) == null)) return false;
            shown[s] = index;
            if (spawned[s] != null) Destroy(spawned[s]);
            spawned[s] = null;
            var prefab = lib != null ? lib.Prefab(slot, index) : null;
            if (prefab != null)
            {
                spawned[s] = Instantiate(prefab, visual, false);
                spawned[s].name = slot + "_" + prefab.name;
                foreach (var c in spawned[s].GetComponentsInChildren<Collider>(true)) Destroy(c);
            }
            return true;
        }

        void Tint(Color c)
        {
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var r in bodyRenderers) SetColor(r, c);
            for (int i = 0; i < spawned.Length; i++)
            {
                if (spawned[i] == null) continue;
                foreach (var r in spawned[i].GetComponentsInChildren<Renderer>(true))
                    if (r.name.StartsWith("BodyTint")) SetColor(r, c);
            }
        }

        void SetColor(Renderer r, Color c)
        {
            if (r == null) return;
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(block);
        }

        void LateUpdate()
        {
            if (!enabled) return;
            active[(int)id] = this;
            if (pivot == null) return;

            float dt = Time.unscaledDeltaTime;
            bool preview = CosmeticsStore.PreviewMode;
            blend = Mathf.MoveTowards(blend, preview ? 1f : 0f, dt * 1.6f);
            if (preview) spin += spinSpeed * dt;
            else spin = Mathf.MoveTowardsAngle(spin, 0f, 240f * dt);
            spin = Mathf.Repeat(spin, 360f);

            float e = blend * blend * (3f - 2f * blend); // smoothstep
            float rootScale = Mathf.Max(0.01f, transform.lossyScale.y);
            float bob = Mathf.Sin(Time.unscaledTime * 2.2f + (int)id) * 0.08f;
            pivot.localPosition = Vector3.up * (e * (previewLift + bob) / rootScale);
            pivot.localScale = Vector3.one * Mathf.Lerp(1f, previewScale, e);
            pivot.localRotation = Quaternion.Euler(0f, spin, 0f);
        }
    }
}
