using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace IslandRocketRumble
{
    /// <summary>
    /// Life-size rocket being assembled on a player's launch pad (CraftZone.LaunchPad, the ring's back edge).
    /// Shows the craft-menu selection while the menu is open, otherwise the recipe closest to completion.
    /// Each piece group (Gear = body, Nut = fins, Fuel = boosters/nozzle, Chip = nose/face, Gem = crystal trim) is a
    /// pale blueprint ghost until its cost line is covered, then pops in with sparkles (GameEvents.RocketPieceBuilt).
    /// A complete rocket glows gold, puffs steam and shows "READY!" (GameEvents.RocketReady).
    /// Added at runtime to every CraftZone; no scene setup needed.
    /// </summary>
    [DisallowMultipleComponent]
    public class RocketWorkshop : MonoBehaviour
    {
        [Tooltip("Height of the tallest (T5) preview on the pad, meters. Lower tiers scale down from this.")]
        public float maxPadHeight = 2.4f;
        public float bobHeight = 0.035f;
        public float bobSpeed = 2.2f;
        [Tooltip("Seconds after crafting before the next preview appears (the real rocket launches from the pad meanwhile).")]
        public float rebuildDelay = 0.8f;
        public Color ghostColor = new Color(0.45f, 0.78f, 1f, 0.5f);

        const float PopTime = 0.35f;

        // ---------------------------------------------------------------- bootstrap

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            AttachAll();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene s, LoadSceneMode mode) => AttachAll();

        static void AttachAll()
        {
            foreach (var zone in FindObjectsByType<CraftZone>(FindObjectsSortMode.None))
                if (zone != null && zone.GetComponent<RocketWorkshop>() == null) zone.gameObject.AddComponent<RocketWorkshop>();
        }

        // ---------------------------------------------------------------- data

        class Group
        {
            public PartType type;
            public readonly List<Renderer> renderers = new List<Renderer>();
            public readonly List<Material[]> materials = new List<Material[]>();
            public readonly List<Color> baseColors = new List<Color>();
            public readonly List<ShadowCastingMode> shadows = new List<ShadowCastingMode>();
            public readonly List<Transform> tops = new List<Transform>();
            public readonly List<Vector3> topScales = new List<Vector3>();
            public bool known;
            public bool built;
            public float popTimer = -1f;
        }

        class Preview
        {
            public RocketRecipe recipe;
            public GameObject holder;
            public float height;
            public readonly Dictionary<PartType, Group> groups = new Dictionary<PartType, Group>();
        }

        static Material ghostMaterial;
        static readonly Dictionary<int, Material[]> ghostArrays = new Dictionary<int, Material[]>();
        static MaterialPropertyBlock glowBlock;

        CraftZone zone;
        Transform root, bob, labelRoot;
        TextMesh label, ready;
        ParticleSystem steam;

        readonly Dictionary<RocketRecipe, Preview> cache = new Dictionary<RocketRecipe, Preview>();
        readonly Dictionary<RocketRecipe, int> lastMask = new Dictionary<RocketRecipe, int>();
        Preview current;
        bool currentComplete;
        bool glowing;
        float hideTimer;
        float readyPop = -1f;
        float resubTimer;

        MatchContext boundCtx;
        Inventory boundInventory;
        CraftMenuState boundMenu;

        PlayerId Owner => zone != null ? zone.owner : PlayerId.P1;

        // ---------------------------------------------------------------- lifecycle

        void Awake()
        {
            zone = GetComponent<CraftZone>();
            Transform island = transform.parent != null ? transform.parent : transform;
            root = new GameObject("RocketWorkshop").transform;
            root.SetParent(island, false);
            root.SetPositionAndRotation(zone != null ? zone.LaunchPad : transform.position, Quaternion.identity);
            Vector3 lossy = root.lossyScale;
            root.localScale = new Vector3(SafeInv(lossy.x), SafeInv(lossy.y), SafeInv(lossy.z));

            bob = new GameObject("Bob").transform;
            bob.SetParent(root, false);

            labelRoot = new GameObject("Labels").transform;
            labelRoot.SetParent(root, false);
            label = MakeText("ProgressLabel", labelRoot, Vector3.zero, 0.075f, Color.white);
            ready = MakeText("ReadyLabel", labelRoot, new Vector3(0f, 0.8f, 0f), 0.11f, Palette.HelmetYellow);
            SetText(ready, "READY!\n<size=40>พร้อมยิง!</size>", Palette.HelmetYellow);
            ready.gameObject.SetActive(false);

            steam = MakeSteam(root);
            root.gameObject.SetActive(false);
        }

        static float SafeInv(float v) => Mathf.Abs(v) > 0.0001f ? 1f / v : 1f;

        void OnEnable() { AttachGameEvents(); }

        void OnDisable()
        {
            DetachGameEvents();
            Unbind();
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        void AttachGameEvents()
        {
            DetachGameEvents();
            GameEvents.PhaseChanged += OnPhaseChanged;
            GameEvents.TierChanged += OnTierChanged;
        }

        void DetachGameEvents()
        {
            GameEvents.PhaseChanged -= OnPhaseChanged;
            GameEvents.TierChanged -= OnTierChanged;
        }

        void Bind(MatchContext ctx)
        {
            Unbind();
            boundCtx = ctx;
            if (ctx == null) return;
            boundInventory = ctx.Inventory(Owner);
            boundMenu = ctx.CraftMenu(Owner);
            boundInventory.Changed += OnInventoryChanged;
            boundMenu.Changed += OnMenuChanged;
            boundMenu.Crafted += OnCrafted;
            lastMask.Clear();
            Refresh(false);
        }

        void Unbind()
        {
            if (boundInventory != null) boundInventory.Changed -= OnInventoryChanged;
            if (boundMenu != null)
            {
                boundMenu.Changed -= OnMenuChanged;
                boundMenu.Crafted -= OnCrafted;
            }
            boundInventory = null;
            boundMenu = null;
            boundCtx = null;
        }

        // ---------------------------------------------------------------- events

        void OnInventoryChanged() => Refresh(true);
        void OnMenuChanged() => Refresh(true);
        void OnTierChanged(PartTier t) => Refresh(true);

        void OnCrafted(RocketRecipe recipe)
        {
            // the real rocket takes off from this pad: clear the stage, rebuild shortly after
            hideTimer = Mathf.Max(0.05f, rebuildDelay);
            Refresh(false);
        }

        void OnPhaseChanged(GamePhase p)
        {
            if (p == GamePhase.Countdown)
            {
                hideTimer = 0f;
                lastMask.Clear();
                Refresh(false);
                return;
            }
            Refresh(true);
        }

        // ---------------------------------------------------------------- update

        void Update()
        {
            var ctx = MatchContext.Instance;
            if (!ReferenceEquals(ctx, boundCtx)) Bind(ctx);
            if (ctx == null) return;

            resubTimer += Time.unscaledDeltaTime;
            if (resubTimer >= 1f)
            {
                resubTimer = 0f;
                AttachGameEvents(); // safety net against GameEvents.Reset()
            }

            if (hideTimer > 0f)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0f) Refresh(false);
            }

            bool want = ctx.IsPlaying && hideTimer <= 0f;
            if (want != root.gameObject.activeSelf) Refresh(false);
            if (!root.gameObject.activeSelf || current == null) return;

            float t = Time.time;
            bob.localPosition = Vector3.up * (bobHeight * (1f + Mathf.Sin(t * bobSpeed)));
            Animate(t);
        }

        void LateUpdate()
        {
            if (root == null || !root.gameObject.activeSelf) return;
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
                if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
                // rocket faces are on local +Z: turn them to the camera, with a lazy sway
                bob.rotation = Quaternion.LookRotation(-fwd.normalized, Vector3.up) * Quaternion.Euler(0f, 6f * Mathf.Sin(Time.time * 0.9f), 0f);
            }
            GameplayUtil.Billboard(labelRoot);
        }

        void Animate(float t)
        {
            if (ghostMaterial != null)
            {
                Color g = ghostColor;
                g.a *= 0.8f + 0.3f * Mathf.Sin(t * 3f);
                SetMatColor(ghostMaterial, g);
            }

            foreach (var g in current.groups.Values)
            {
                if (g.popTimer < 0f) continue;
                g.popTimer += Time.deltaTime;
                float k = Mathf.Clamp01(g.popTimer / PopTime);
                float s = 1f + 0.3f * Mathf.Sin(k * Mathf.PI) * (1f - 0.5f * k);
                for (int i = 0; i < g.tops.Count; i++)
                    if (g.tops[i] != null) g.tops[i].localScale = g.topScales[i] * s;
                if (k >= 1f) g.popTimer = -1f;
            }

            if (currentComplete)
            {
                if (glowBlock == null) glowBlock = new MaterialPropertyBlock();
                float k = 0.22f + 0.18f * Mathf.Sin(t * 4.5f);
                foreach (var g in current.groups.Values)
                {
                    for (int i = 0; i < g.renderers.Count; i++)
                    {
                        var r = g.renderers[i];
                        if (r == null) continue;
                        Color c = Color.Lerp(g.baseColors[i], Palette.HelmetYellow, k);
                        glowBlock.Clear();
                        glowBlock.SetColor(GameplayUtil.BaseColorId, c);
                        glowBlock.SetColor(GameplayUtil.ColorId, c);
                        r.SetPropertyBlock(glowBlock);
                    }
                }
                glowing = true;

                float p = readyPop >= 0f ? Mathf.Clamp01((readyPop += Time.deltaTime) / 0.4f) : 1f;
                if (p >= 1f) readyPop = -1f;
                float pop = readyPop >= 0f ? GameplayUtil.EaseOutBack(p) : 1f;
                ready.transform.localScale = Vector3.one * (pop * (1f + 0.06f * Mathf.Sin(t * 6f)));
            }
        }

        // ---------------------------------------------------------------- refresh

        void Refresh(bool fx)
        {
            var ctx = MatchContext.Instance;
            if (ctx == null || root == null || ctx.recipes == null) return;
            var inv = ctx.Inventory(Owner);

            // remember every recipe's built pieces so flips are only celebrated when parts arrive
            RocketRecipe shown = PickRecipe(ctx, inv);
            int oldMask = 0, newMask = 0;
            bool hadOld = false;
            foreach (var r in ctx.recipes)
            {
                if (r == null) continue;
                int m = BuiltMask(r, inv);
                if (r == shown)
                {
                    hadOld = lastMask.TryGetValue(r, out oldMask);
                    newMask = m;
                }
                lastMask[r] = m;
            }

            bool visible = ctx.IsPlaying && hideTimer <= 0f && shown != null;
            if (!visible)
            {
                if (root.gameObject.activeSelf) root.gameObject.SetActive(false);
                return;
            }
            if (!root.gameObject.activeSelf)
            {
                root.position = zone != null ? zone.LaunchPad : transform.position;
                root.gameObject.SetActive(true);
                fx = false; // re-appearing (after a craft or at match start) is not a "build" moment
            }

            var preview = GetPreview(shown);
            if (current != preview)
            {
                if (current != null) { ClearGlow(current); current.holder.SetActive(false); }
                current = preview;
                current.holder.SetActive(true);
                glowing = false;
            }

            foreach (var g in current.groups.Values)
            {
                bool want = (newMask & (1 << (int)g.type)) != 0;
                if (!g.known || g.built != want) SetBuilt(g, want);
            }

            int full = TypeMask(shown);
            bool complete = newMask == full;
            int flipped = hadOld && fx ? newMask & ~oldMask : 0;
            Color tierColor = Palette.Tier(shown.tier);
            for (int i = 0; i < 5; i++)
            {
                if ((flipped & (1 << i)) == 0) continue;
                var type = (PartType)i;
                if (current.groups.TryGetValue(type, out var g)) Pop(g, tierColor);
                GameEvents.RaiseRocketPieceBuilt(Owner, type);
            }
            if (complete && hadOld && fx && oldMask != full)
            {
                readyPop = 0f;
                Burst(root.position + Vector3.up * (current.height * 0.5f), Palette.HelmetYellow, 34, current.height * 0.3f);
                GameEvents.RaiseRocketReady(Owner, shown);
            }

            if (!complete && glowing) { ClearGlow(current); glowing = false; }
            currentComplete = complete;
            ready.gameObject.SetActive(complete);
            if (!complete) readyPop = -1f;
            SetSteam(complete);
            UpdateLabel(shown, inv, complete);
        }

        RocketRecipe PickRecipe(MatchContext ctx, Inventory inv)
        {
            var recipes = ctx.recipes;
            var menu = ctx.CraftMenu(Owner);
            if (menu.IsOpen && menu.Selected >= 0 && menu.Selected < CraftMenuState.CloseSlot && menu.Selected < recipes.Length
                && recipes[menu.Selected] != null)
                return recipes[menu.Selected];

            RocketRecipe best = null;
            float bestRatio = 0f;
            foreach (var r in recipes)
            {
                if (r == null) continue;
                float ratio = Ratio(r, inv);
                if (ratio <= 0f) continue;
                if (best == null || ratio > bestRatio + 0.0001f || (Mathf.Abs(ratio - bestRatio) <= 0.0001f && r.tier > best.tier))
                {
                    best = r;
                    bestRatio = ratio;
                }
            }
            if (best != null) return best;

            // nothing collected yet: empty blueprint of the current tier's rocket
            PartTier tier = ctx.CurrentTier;
            foreach (var r in recipes) if (r != null && r.tier == tier) return r;
            int idx = Mathf.Clamp((int)tier - 1, 0, recipes.Length - 1);
            if (recipes.Length > 0 && recipes[idx] != null) return recipes[idx];
            foreach (var r in recipes) if (r != null) return r;
            return null;
        }

        static float Ratio(RocketRecipe r, Inventory inv)
        {
            int have = 0, need = 0;
            Count(r, inv, ref have, ref need);
            return need > 0 ? (float)have / need : 0f;
        }

        static void Count(RocketRecipe r, Inventory inv, ref int have, ref int need)
        {
            foreach (var c in r.costs)
            {
                if (c.count <= 0) continue;
                need += c.count;
                have += Mathf.Min(inv.Count(c.Key), c.count);
            }
        }

        /// <summary>Bit per PartType whose cost lines are all covered.</summary>
        static int BuiltMask(RocketRecipe r, Inventory inv)
        {
            int types = TypeMask(r), missing = 0;
            foreach (var c in r.costs)
                if (inv.Count(c.Key) < c.count) missing |= 1 << (int)c.type;
            return types & ~missing;
        }

        static int TypeMask(RocketRecipe r)
        {
            int m = 0;
            foreach (var c in r.costs) if (c.count > 0) m |= 1 << (int)c.type;
            return m;
        }

        void UpdateLabel(RocketRecipe r, Inventory inv, bool complete)
        {
            int have = 0, need = 0;
            Count(r, inv, ref have, ref need);
            string name = string.IsNullOrEmpty(r.displayName) ? "T" + (int)r.tier : r.displayName.ToUpperInvariant();
            string text = $"{name} {have}/{need}";
            if (!complete)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var c in r.costs)
                {
                    int miss = c.count - Mathf.Min(inv.Count(c.Key), c.count);
                    if (miss <= 0) continue;
                    sb.Append(sb.Length == 0 ? "need " : "  ").Append(miss).Append(' ').Append(c.type.ToString().ToUpperInvariant());
                }
                if (sb.Length > 0) text += $"\n<size=40>{sb}</size>";
            }
            SetText(label, text, Palette.Tier(r.tier));
            labelRoot.localPosition = Vector3.up * (current.height + 0.45f);
        }

        // ---------------------------------------------------------------- previews

        Preview GetPreview(RocketRecipe r)
        {
            if (cache.TryGetValue(r, out var p) && p.holder != null) return p;
            p = BuildPreview(r);
            cache[r] = p;
            return p;
        }

        Preview BuildPreview(RocketRecipe r)
        {
            var p = new Preview { recipe = r };
            p.holder = new GameObject("Preview_" + r.name);
            p.holder.SetActive(false); // instantiate under an inactive parent: no Awake/OnEnable on copied scripts
            p.holder.transform.SetParent(bob, false);

            GameObject inst;
            if (r.flightPrefab != null)
            {
                inst = Instantiate(r.flightPrefab, p.holder.transform, false);
                inst.name = r.flightPrefab.name;
            }
            else
            {
                inst = new GameObject("Placeholder");
                inst.transform.SetParent(p.holder.transform, false);
                var visual = new GameObject("Visual").transform;
                visual.SetParent(inst.transform, false);
                var body = GameplayUtil.CreatePrimitive(PrimitiveType.Capsule, "Body");
                body.transform.SetParent(visual, false);
                body.transform.localScale = new Vector3(0.3f, 0.45f, 0.3f);
                body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                GameplayUtil.Tint(body, Palette.Tier(r.tier));
            }
            Strip(inst);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;

            // same size as in flight (RocketLauncher), but capped so the big ones fit on the pad
            float raw = Mathf.Max(0.05f, MeasureHeight(inst.transform, p.holder.transform));
            int step = Mathf.Clamp((int)r.tier, 1, 5) - 1;
            float cap = maxPadHeight * (0.42f + 0.145f * step);
            float k = Mathf.Min(FlightScale(r.tier), cap / raw);
            inst.transform.localScale *= k;
            p.height = raw * k;

            BuildGroups(p, inst.transform, r);
            return p;
        }

        /// <summary>Mirror of RocketLauncher's flight scale table.</summary>
        static float FlightScale(PartTier tier) => tier switch
        {
            PartTier.Scrap => 1.6f,
            PartTier.Copper => 1.7f,
            PartTier.Silver => 1.9f,
            PartTier.Gold => 2.3f,
            _ => 3.6f
        };

        static void Strip(GameObject go)
        {
            var behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = behaviours.Length - 1; i >= 0; i--) if (behaviours[i] != null) DestroyImmediate(behaviours[i]);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
            foreach (var c in go.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(c);
            foreach (var c in go.GetComponentsInChildren<Animator>(true)) DestroyImmediate(c);
            foreach (var c in go.GetComponentsInChildren<AudioSource>(true)) DestroyImmediate(c);
        }

        static float MeasureHeight(Transform t, Transform space)
        {
            float min = float.MaxValue, max = float.MinValue;
            Matrix4x4 toSpace = space.worldToLocalMatrix;
            foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Bounds b = mf.sharedMesh.bounds;
                Matrix4x4 m = toSpace * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    float y = m.MultiplyPoint3x4(corner).y;
                    min = Mathf.Min(min, y);
                    max = Mathf.Max(max, y);
                }
            }
            return max > min ? max - Mathf.Min(0f, min) : 1f;
        }

        void BuildGroups(Preview p, Transform inst, RocketRecipe r)
        {
            Transform visual = inst.Find("Visual");
            if (visual == null) visual = inst;
            int types = TypeMask(r);
            bool hasFins = false, hasNose = false;
            foreach (Transform c in visual)
            {
                string n = c.name.ToLowerInvariant();
                if (n.Contains("fin")) hasFins = true;
                if (n.Contains("nose")) hasNose = true;
            }

            foreach (var rend in inst.GetComponentsInChildren<Renderer>(true))
            {
                if (rend == null || rend is ParticleSystemRenderer || rend.GetComponent<TextMesh>() != null) continue;
                Transform top = rend.transform;
                while (top.parent != null && top.parent != visual && top.parent != inst) top = top.parent;
                if (top.parent != visual) top = rend.transform; // not under Visual: belongs to the body
                PartType type = top.parent == visual ? MapPiece(top.name.ToLowerInvariant(), types, hasFins, hasNose) : Fallback(types);

                if (!p.groups.TryGetValue(type, out var g))
                {
                    g = new Group { type = type };
                    p.groups[type] = g;
                }
                g.renderers.Add(rend);
                g.materials.Add(rend.sharedMaterials);
                g.baseColors.Add(GameplayUtil.GetTint(rend));
                g.shadows.Add(rend.shadowCastingMode);
                if (!g.tops.Contains(top))
                {
                    g.tops.Add(top);
                    g.topScales.Add(top.localScale);
                }
            }
        }

        /// <summary>
        /// Rocket child name → part type. Candidates are tried in order; the first one the recipe actually uses wins,
        /// else the piece belongs to the body. T3's "Trim" and T4's "Gold" carry the nose/fins/bands meshes.
        /// </summary>
        static PartType MapPiece(string n, int types, bool hasFins, bool hasNose)
        {
            if (n.Contains("booster") || n.Contains("nozzle") || n.Contains("fuse") || n.Contains("flame"))
                return Pick(types, PartType.Fuel);
            if (n.Contains("nose") || n.StartsWith("eye") || n.Contains("pupil") || n.StartsWith("brow") || n.Contains("mouth")
                || n.Contains("sunglass") || n.Contains("face"))
                return Pick(types, PartType.Chip);
            if (n.Contains("fin") || n.Contains("paper"))
                return Pick(types, PartType.Nut);
            if (n.Contains("trim") || n.Contains("gold") || n.Contains("band") || n.Contains("crystal"))
            {
                if (!hasFins && Has(types, PartType.Nut)) return PartType.Nut;
                if (Has(types, PartType.Gem)) return PartType.Gem;
                if (!hasNose && Has(types, PartType.Chip)) return PartType.Chip;
                return Fallback(types);
            }
            return Fallback(types);
        }

        static bool Has(int types, PartType t) => (types & (1 << (int)t)) != 0;
        static PartType Pick(int types, PartType t) => Has(types, t) ? t : Fallback(types);

        static PartType Fallback(int types)
        {
            if (Has(types, PartType.Gear) || types == 0) return PartType.Gear;
            for (int i = 0; i < 5; i++) if (Has(types, (PartType)i)) return (PartType)i;
            return PartType.Gear;
        }

        // ---------------------------------------------------------------- look

        void SetBuilt(Group g, bool built)
        {
            g.known = true;
            g.built = built;
            for (int i = 0; i < g.renderers.Count; i++)
            {
                var r = g.renderers[i];
                if (r == null) continue;
                r.SetPropertyBlock(null);
                r.sharedMaterials = built ? g.materials[i] : GhostArray(g.materials[i].Length);
                r.shadowCastingMode = built ? g.shadows[i] : ShadowCastingMode.Off;
            }
            for (int i = 0; i < g.tops.Count; i++)
                if (g.tops[i] != null) g.tops[i].localScale = g.topScales[i];
            g.popTimer = -1f;
        }

        void Pop(Group g, Color tierColor)
        {
            g.popTimer = 0f;
            Bounds b = default;
            bool any = false;
            foreach (var r in g.renderers)
            {
                if (r == null) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            Vector3 at = any ? b.center : root.position + Vector3.up * (current.height * 0.5f);
            float spread = any ? Mathf.Clamp(b.extents.magnitude * 0.5f, 0.1f, 0.6f) : 0.3f;
            Burst(at, tierColor, 18, spread);
        }

        void ClearGlow(Preview p)
        {
            foreach (var g in p.groups.Values)
                foreach (var r in g.renderers)
                    if (r != null) r.SetPropertyBlock(null);
        }

        Material[] GhostArray(int length)
        {
            if (ghostMaterial == null) ghostMaterial = CreateGhostMaterial(ghostColor);
            length = Mathf.Max(1, length);
            if (!ghostArrays.TryGetValue(length, out var arr) || arr[0] == null)
            {
                arr = new Material[length];
                for (int i = 0; i < length; i++) arr[i] = ghostMaterial;
                ghostArrays[length] = arr;
            }
            return arr;
        }

        static Material CreateGhostMaterial(Color c)
        {
            var sh = Shader.Find("IRR/Unlit Transparent");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var m = new Material(sh) { name = "IRR_WorkshopGhost" };
            if (m.HasProperty("_EdgeBoost")) m.SetFloat("_EdgeBoost", 0f);
            if (m.HasProperty("_VerticalFade")) m.SetFloat("_VerticalFade", 0f);
            SetMatColor(m, c);
            return m;
        }

        static void SetMatColor(Material m, Color c)
        {
            if (m.HasProperty(GameplayUtil.BaseColorId)) m.SetColor(GameplayUtil.BaseColorId, c);
            if (m.HasProperty(GameplayUtil.ColorId)) m.SetColor(GameplayUtil.ColorId, c);
        }

        void SetSteam(bool on)
        {
            if (steam == null) return;
            if (on && !steam.isPlaying) steam.Play();
            else if (!on && steam.isPlaying) steam.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        static ParticleSystem MakeSteam(Transform parent)
        {
            var go = new GameObject("Steam");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, Palette.Cream);
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, 4, 6, 1, 0.01f) }); // a puff every second

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;
            shape.rotation = new Vector3(90f, 0f, 0f); // flat on the pad, puffs spread outward

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(0.45f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.7f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade(Color.white, Palette.Cream, 0.85f));

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = FxFactory.GetMaterial();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        static void Burst(Vector3 position, Color color, int count, float spread)
        {
            var go = new GameObject("BuildSparkle");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.2f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.17f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.gravityModifier = 0.35f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;

            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 100)) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = spread;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(Fade(Color.white, Color.white, 1f));

            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.sharedMaterial = FxFactory.GetMaterial();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            ps.Play();
        }

        static Gradient Fade(Color a, Color b, float alpha)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha * 0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        /// <summary>Legacy TextMesh with a soft ink drop shadow (billboarded by the parent).</summary>
        static TextMesh MakeText(string name, Transform parent, Vector3 localPos, float size, Color color)
        {
            var tm = GameplayUtil.CreateTextMesh(parent, localPos, size, color);
            tm.gameObject.name = name;
            tm.richText = true;
            tm.lineSpacing = 0.85f;
            var shadow = GameplayUtil.CreateTextMesh(tm.transform, new Vector3(0.04f, -0.04f, 0.02f), size, new Color(Palette.Ink.r, Palette.Ink.g, Palette.Ink.b, 0.85f));
            shadow.gameObject.name = "Shadow";
            shadow.richText = true;
            shadow.lineSpacing = tm.lineSpacing;
            shadow.transform.localScale = Vector3.one;
            return tm;
        }

        static void SetText(TextMesh tm, string text, Color color)
        {
            tm.text = text;
            tm.color = color;
            var shadow = tm.transform.Find("Shadow");
            if (shadow != null && shadow.TryGetComponent<TextMesh>(out var s)) s.text = text;
        }
    }
}
