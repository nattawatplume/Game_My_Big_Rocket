using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace IslandRocketRumble
{
    /// <summary>
    /// Compact craft menu that floats above one player's head (screen-space, follows HeadAnchor).
    /// Renders MatchContext.CraftMenu(id): 5 rocket slots, repair, upgrade and ✕, costs (have/need), DMG, target,
    /// key hint, and the upgrade cards while the player is choosing one.
    /// </summary>
    public class CraftMenuView : MonoBehaviour
    {
        const float SlotSize = 58f;
        const float SlotGap = 6f;
        const float Pad = 12f;
        const float PanelW = SlotSize * CraftMenuState.SlotCount + SlotGap * (CraftMenuState.SlotCount - 1) + Pad * 2f;
        const float PanelH = 240f;
        const float RowY = -(Pad + SlotSize * 0.5f);                      // slot row center (from top)

        class Slot
        {
            public RectTransform rt;
            public Image outline;
            public Image fill;
            public Image icon;
            public UText tierText;
        }

        class CostCell
        {
            public RectTransform rt;
            public Image icon;
            public UText text;
        }

        class Card
        {
            public RectTransform rt;
            public Image fill;
            public Image header;
            public UText branch;
            public UText name;
            public UText description;
            public UText stacks;
        }

        const float CardW = 166f;
        const float CardH = 164f;

        UIRoot root;
        PlayerId id;
        RectTransform space;
        RectTransform follow;
        RectTransform content;
        RectTransform frame;
        readonly Slot[] slots = new Slot[CraftMenuState.SlotCount];
        readonly CostCell[] costs = new CostCell[6];
        UText nameText;
        UText dmgText;
        UText infoText;
        UText targetText;
        UText hintText;
        RectTransform cardRoot;
        readonly Card[] cards = new Card[UpgradeCatalog.HandSize];
        int lastCard = -1;

        MatchContext boundCtx;
        CraftMenuState state;
        Inventory inventory;
        bool visible;
        int lastSelected = -1;
        TargetKind lastTarget = TargetKind.Opponent;

        static readonly Color GreyFill = new Color(0.8f, 0.8f, 0.82f, 1f);
        static readonly Color GreyIcon = new Color(0.58f, 0.58f, 0.62f, 0.75f);

        // ---------------------------------------------------------------- build

        public void Init(UIRoot owner, PlayerId player, RectTransform worldSpace)
        {
            root = owner;
            id = player;
            space = worldSpace;
            follow = (RectTransform)transform;
            follow.anchorMin = follow.anchorMax = UIFactory.Center;
            follow.pivot = UIFactory.Center;
            follow.sizeDelta = new Vector2(10f, 10f);

            content = UIFactory.Rect(follow, "Content", UIFactory.Center, Vector2.zero, new Vector2(PanelW, PanelH));
            content.pivot = new Vector2(0.5f, 0f); // grows upward from the head point

            // bubble tail (pointing down at the head)
            var tailOuter = UIFactory.Img(content, "TailInk", UISprites.TriangleDown, Palette.Ink, UIFactory.BottomCenter,
                new Vector2(0f, -9f), new Vector2(38f, 24f));
            tailOuter.preserveAspect = false;

            Image panelFill;
            var panel = UIFactory.Panel(content, "Panel", UIFactory.Center, Vector2.zero, new Vector2(PanelW, PanelH), Palette.Cream,
                out panelFill, 22f, 4f, true);
            UIFactory.StretchFill(panel);

            var tailInner = UIFactory.Img(content, "TailFill", UISprites.TriangleDown, Palette.Cream, UIFactory.BottomCenter,
                new Vector2(0f, -4f), new Vector2(28f, 17f));
            tailInner.preserveAspect = false;

            // player tag
            var tag = UIFactory.Panel(content, "Tag", UIFactory.TopLeft, new Vector2(36f, 4f), new Vector2(54f, 28f), Palette.Player(id),
                14f, 3f, false);
            var tagText = UIFactory.Txt(tag, "T", UIText.PlayerTag(id), 18, Color.white, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 1f), new Vector2(54f, 28f), true);
            UIFactory.AddOutline(tagText, UIFactory.WithAlpha(Palette.Ink, 0.6f), 1.5f);

            // selection frame (behind slots)
            var frameImg = UIFactory.Img(content, "Frame", UISprites.RoundedRing, Palette.HelmetOrange, UIFactory.TopCenter,
                new Vector2(SlotX(0), RowY), new Vector2(SlotSize + 14f, SlotSize + 14f));
            UIFactory.SetRadius(frameImg, 18f);
            frame = frameImg.rectTransform;

            for (int i = 0; i < CraftMenuState.SlotCount; i++) slots[i] = BuildSlot(i);
            frame.SetAsLastSibling(); // frame over slots so it reads clearly

            float y = -(Pad + SlotSize + 8f + 14f);
            nameText = UIFactory.Txt(content, "Name", "", 22, Palette.Ink, TextAnchor.MiddleLeft, UIFactory.TopCenter,
                new Vector2(-PanelW * 0.5f + Pad + 130f, y), new Vector2(260f, 28f), true);
            dmgText = UIFactory.Txt(content, "Dmg", "", 22, Palette.Danger, TextAnchor.MiddleRight, UIFactory.TopCenter,
                new Vector2(PanelW * 0.5f - Pad - 55f, y), new Vector2(110f, 28f), true);

            float costTop = -(Pad + SlotSize + 8f + 28f + 4f);
            for (int i = 0; i < costs.Length; i++)
            {
                int col = i % 2;
                int row = i / 2;
                var c = new CostCell();
                c.rt = UIFactory.Rect(content, "Cost" + i, UIFactory.TopCenter,
                    new Vector2(col == 0 ? -94f : 94f, costTop - 12f - row * 24f), new Vector2(184f, 24f));
                c.icon = UIFactory.Icon(c.rt, "Icon", UISprites.Part(PartType.Gear), Color.white, UIFactory.MidLeft, new Vector2(11f, 0f), 20f, 1f);
                c.text = UIFactory.Txt(c.rt, "Text", "", 18, Palette.Ink, TextAnchor.MiddleLeft, UIFactory.MidLeft, new Vector2(26f + 78f, 0f),
                    new Vector2(156f, 24f));
                costs[i] = c;
            }
            infoText = UIFactory.Txt(content, "Info", "", 19, UIFactory.WithAlpha(Palette.Ink, 0.75f), TextAnchor.MiddleCenter,
                UIFactory.TopCenter, new Vector2(0f, costTop - 36f), new Vector2(PanelW - 20f, 60f));

            float targetY = costTop - 72f - 14f;
            targetText = UIFactory.Txt(content, "Target", "", 21, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.TopCenter,
                new Vector2(0f, targetY), new Vector2(PanelW - 20f, 28f), true);
            hintText = UIFactory.Txt(content, "Hint", "", 19, Palette.CraftRing, TextAnchor.MiddleCenter, UIFactory.TopCenter,
                new Vector2(0f, targetY - 25f), new Vector2(PanelW - 20f, 26f), true);

            BuildCards();

            content.gameObject.SetActive(false);
            visible = false;
        }

        void BuildCards()
        {
            cardRoot = UIFactory.Stretch(content, "Cards");
            UIFactory.Txt(cardRoot, "Title", "เลือกการ์ดอัปเกรด 1 ใบ", 22, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.TopCenter,
                new Vector2(0f, -22f), new Vector2(PanelW - 20f, 28f), true);
            for (int i = 0; i < cards.Length; i++)
            {
                var c = new Card();
                c.rt = UIFactory.Panel(cardRoot, "Card" + i, UIFactory.TopCenter, new Vector2((i - 1) * (CardW + 6f), -42f - CardH * 0.5f),
                    new Vector2(CardW, CardH), Color.white, out c.fill, 14f, 3f, false);
                c.header = UIFactory.Img(c.rt, "Header", UISprites.Rounded, Color.white, UIFactory.TopCenter, new Vector2(0f, -17f), new Vector2(CardW - 12f, 26f));
                UIFactory.SetRadius(c.header, 9f);
                c.branch = UIFactory.Txt(c.rt, "Branch", "", 17, Color.white, TextAnchor.MiddleCenter, UIFactory.TopCenter,
                    new Vector2(0f, -17f), new Vector2(CardW - 12f, 26f), true);
                c.name = UIFactory.Txt(c.rt, "Name", "", 21, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.TopCenter,
                    new Vector2(0f, -50f), new Vector2(CardW - 8f, 28f), true);
                c.description = UIFactory.WrapTxt(c.rt, "Desc", "", 17, UIFactory.WithAlpha(Palette.Ink, 0.85f), TextAnchor.UpperCenter, UIFactory.TopCenter,
                    new Vector2(0f, -94f), new Vector2(CardW - 14f, 56f));
                c.stacks = UIFactory.Txt(c.rt, "Stacks", "", 15, Palette.CraftRing, TextAnchor.MiddleCenter, UIFactory.BottomCenter,
                    new Vector2(0f, 14f), new Vector2(CardW - 14f, 20f), true);
                cards[i] = c;
            }
            cardRoot.gameObject.SetActive(false);
        }

        void SetPickingLayout(bool picking)
        {
            cardRoot.gameObject.SetActive(picking);
            frame.gameObject.SetActive(!picking);
            for (int i = 0; i < slots.Length; i++) slots[i].rt.gameObject.SetActive(!picking);
            nameText.gameObject.SetActive(!picking);
            dmgText.gameObject.SetActive(!picking);
            infoText.gameObject.SetActive(!picking);
            targetText.gameObject.SetActive(!picking);
            if (picking)
                for (int i = 0; i < costs.Length; i++) costs[i].rt.gameObject.SetActive(false);
        }

        void RefreshCards()
        {
            var owned = MatchContext.Instance != null ? MatchContext.Instance.Upgrades(id) : null;
            for (int i = 0; i < cards.Length; i++)
            {
                var def = UpgradeCatalog.Get(state.Cards[i]);
                var c = cards[i];
                bool selected = i == state.CardIndex;
                Color branchColor = HudScreen.BranchColor(def.branch);
                c.header.color = branchColor;
                c.fill.color = selected ? Color.Lerp(Palette.HelmetYellow, Color.white, 0.55f) : Color.white;
                c.branch.text = UpgradeCatalog.BranchName(def.branch);
                c.name.text = def.name;
                c.description.text = def.description;
                int n = owned != null ? owned.Count(def.type) : 0;
                c.stacks.text = n > 0 ? "มีแล้ว " + n + "/" + UpgradeCatalog.MaxStacks : "";
                c.rt.localScale = selected ? Vector3.one * 1.04f : Vector3.one;
            }
            hintText.rectTransform.anchoredPosition = new Vector2(0f, -222f);
            hintText.text = "◀ ▶ เลือก · " + UIText.ConfirmKey(id) + " : รับการ์ด";
            if (visible && state.CardIndex != lastCard) UITween.Punch(cards[state.CardIndex].rt, 0.1f, 0.2f);
            lastCard = state.CardIndex;
        }

        static float SlotX(int i) => -PanelW * 0.5f + Pad + SlotSize * 0.5f + i * (SlotSize + SlotGap);

        Slot BuildSlot(int i)
        {
            var s = new Slot();
            s.rt = UIFactory.Rect(content, "Slot" + i, UIFactory.TopCenter, new Vector2(SlotX(i), RowY), new Vector2(SlotSize, SlotSize));
            s.outline = UIFactory.StretchImg(s.rt, "Ink", UISprites.Rounded, Palette.Ink);
            UIFactory.SetRadius(s.outline, 14f);
            s.fill = UIFactory.StretchImg(s.outline.transform, "Fill", UISprites.Rounded, Color.white, 3f);
            UIFactory.SetRadius(s.fill, 11f);

            if (i == CraftMenuState.CloseSlot)
            {
                s.fill.color = Palette.Danger;
                s.icon = UIFactory.Icon(s.rt, "Icon", UISprites.Cross, Color.white, UIFactory.Center, Vector2.zero, 30f, 1.5f);
            }
            else if (i == CraftMenuState.RepairSlot)
            {
                s.icon = UIFactory.Icon(s.rt, "Icon", UISprites.Heart, Palette.Danger, UIFactory.Center, Vector2.zero, 38f, 2f);
            }
            else if (i == CraftMenuState.UpgradeSlot)
            {
                s.icon = UIFactory.Icon(s.rt, "Icon", UISprites.Star, Palette.HelmetYellow, UIFactory.Center, Vector2.zero, 40f, 2f);
            }
            else
            {
                s.icon = UIFactory.Icon(s.rt, "Icon", UISprites.Rocket(i + 1), Palette.Tier((PartTier)(i + 1)), UIFactory.Center,
                    new Vector2(0f, 3f), 44f, 2f);
                s.tierText = UIFactory.Txt(s.rt, "Tier", "T" + (i + 1), 14, Palette.Ink, TextAnchor.LowerRight, UIFactory.BottomRight,
                    new Vector2(-14f, 10f), new Vector2(26f, 18f), true);
                UIFactory.AddOutline(s.tierText, UIFactory.WithAlpha(Color.white, 0.8f), 1f);
            }
            return s;
        }

        // ---------------------------------------------------------------- binding

        void TryBind()
        {
            var ctx = MatchContext.Instance;
            if (ctx == boundCtx) return;
            Unbind();
            boundCtx = ctx;
            if (ctx == null) return;
            state = ctx.CraftMenu(id);
            inventory = ctx.Inventory(id);
            state.Changed += OnStateChanged;
            state.Crafted += OnCrafted;
            state.Denied += OnDenied;
            inventory.Changed += OnInventoryChanged;
            lastSelected = state.Selected;
            lastTarget = state.Target;
            Refresh();
        }

        void Unbind()
        {
            if (state != null)
            {
                state.Changed -= OnStateChanged;
                state.Crafted -= OnCrafted;
                state.Denied -= OnDenied;
            }
            if (inventory != null) inventory.Changed -= OnInventoryChanged;
            state = null;
            inventory = null;
            boundCtx = null;
        }

        void OnDestroy() => Unbind();

        // ---------------------------------------------------------------- events

        void OnStateChanged()
        {
            if (state == null) return;
            if (visible && state.Selected != lastSelected)
            {
                UITween.Punch(frame, 0.18f, 0.25f);
                UITween.Punch(slots[state.Selected].rt, 0.15f, 0.25f);
            }
            if (visible && state.Target != lastTarget) UITween.Punch(targetText, 0.25f, 0.3f);
            lastSelected = state.Selected;
            lastTarget = state.Target;
            Refresh();
        }

        void OnInventoryChanged() => Refresh();

        void OnCrafted(RocketRecipe recipe)
        {
            if (!visible) return;
            UITween.Punch(content, 0.22f, 0.4f);
            int sel = state != null ? state.Selected : 0;
            if (sel >= 0 && sel < CraftMenuState.RocketSlots) UITween.Punch(slots[sel].rt, 0.4f, 0.4f);
            Vector2 origin = slots[Mathf.Clamp(sel, 0, CraftMenuState.SlotCount - 1)].rt.anchoredPosition;
            if (isActiveAndEnabled) StartCoroutine(SparkleBurst(origin, recipe != null ? Palette.Tier(recipe.tier) : Palette.HelmetYellow));
        }

        void OnDenied()
        {
            if (!visible) return;
            UITween.Shake(content, 12f, 0.35f);
            if (state != null && state.Selected < CraftMenuState.CloseSlot) UITween.Punch(slots[state.Selected].rt, -0.15f, 0.3f);
            for (int i = 0; i < costs.Length; i++)
                if (costs[i].rt.gameObject.activeSelf) UITween.Punch(costs[i].rt, 0.2f, 0.3f);
        }

        IEnumerator SparkleBurst(Vector2 origin, Color color)
        {
            const int count = 10;
            var parts = new RectTransform[count];
            var dirs = new Vector2[count];
            var imgs = new Image[count];
            for (int i = 0; i < count; i++)
            {
                var img = UIFactory.Img(content, "Sparkle", UISprites.Sparkle, i % 2 == 0 ? Color.white : color, UIFactory.TopCenter,
                    origin, new Vector2(26f, 26f));
                UIFactory.AddOutline(img, UIFactory.WithAlpha(Palette.Ink, 0.5f), 1f);
                imgs[i] = img;
                parts[i] = img.rectTransform;
                float a = (i / (float)count) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
                dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * UnityEngine.Random.Range(60f, 110f);
            }
            float t = 0f;
            const float dur = 0.6f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = UITween.EaseOutCubic(k);
                for (int i = 0; i < count; i++)
                {
                    if (parts[i] == null) continue;
                    parts[i].anchoredPosition = origin + dirs[i] * e;
                    float s = Mathf.Sin(k * Mathf.PI) * 1.2f;
                    parts[i].localScale = new Vector3(s, s, 1f);
                    parts[i].localRotation = Quaternion.Euler(0f, 0f, k * 180f);
                    var c = imgs[i].color;
                    imgs[i].color = new Color(c.r, c.g, c.b, 1f - k * k);
                }
                yield return null;
            }
            for (int i = 0; i < count; i++)
                if (parts[i] != null) Destroy(parts[i].gameObject);
        }

        // ---------------------------------------------------------------- render

        void Refresh()
        {
            if (state == null) return;
            bool picking = state.IsPicking;
            SetPickingLayout(picking);
            if (picking)
            {
                RefreshCards();
                return;
            }
            lastCard = -1;
            hintText.rectTransform.anchoredPosition = new Vector2(0f, -(Pad + SlotSize + 8f + 28f + 4f) - 72f - 14f - 25f);
            int sel = state.Selected;
            var cfg = root.Config;
            var island = MatchContext.Instance != null ? MatchContext.Instance.Island(id) as IslandHealth : null;
            var upgrades = MatchContext.Instance != null ? MatchContext.Instance.Upgrades(id) : null;
            int partsHeld = inventory != null ? inventory.Total : 0;
            int repairCost = cfg != null ? cfg.repairCost : 3;
            int upgradeCount = upgrades != null ? upgrades.Purchased : 0;
            int upgradeMax = cfg != null ? cfg.upgradeCosts.Length : 0;
            int upgradeCost = upgradeCount < upgradeMax ? cfg.upgradeCosts[upgradeCount] : 0;
            bool repairReady = state.RepairCooldown <= 0f && partsHeld >= repairCost && island != null && island.Hp < island.MaxHp;
            bool upgradeReady = upgradeCount < upgradeMax && partsHeld >= upgradeCost;

            slots[CraftMenuState.RepairSlot].fill.color = repairReady ? Color.white : GreyFill;
            slots[CraftMenuState.RepairSlot].icon.color = repairReady ? Palette.Danger : GreyIcon;
            slots[CraftMenuState.UpgradeSlot].fill.color = upgradeReady ? Color.white : GreyFill;
            slots[CraftMenuState.UpgradeSlot].icon.color = upgradeReady ? Palette.HelmetYellow : GreyIcon;

            for (int i = 0; i < CraftMenuState.RocketSlots; i++)
            {
                var r = root.Recipe(i);
                var s = slots[i];
                var tier = r != null ? r.tier : (PartTier)(i + 1);
                bool afford = r != null && inventory != null && inventory.CanAfford(r) && state.RocketCooldown <= 0f;
                s.icon.sprite = UISprites.Rocket(tier);
                s.icon.color = afford ? Palette.Tier(tier) : GreyIcon;
                s.fill.color = afford ? Color.white : GreyFill;
                if (s.tierText != null) s.tierText.text = "T" + (int)tier;
            }

            frame.anchoredPosition = new Vector2(SlotX(sel), RowY);
            frame.GetComponent<Image>().color = sel == CraftMenuState.CloseSlot ? Palette.Danger : Palette.HelmetOrange;

            string confirmKey = UIText.ConfirmKey(id);
            if (sel == CraftMenuState.CloseSlot)
            {
                nameText.text = UIText.Colored("✕ ปิดเมนู", Palette.Danger) + " (CLOSE)";
                dmgText.text = "";
                for (int i = 0; i < costs.Length; i++) costs[i].rt.gameObject.SetActive(false);
                infoText.text = "เลื่อน ◀ ▶ กลับไปเลือกจรวด";
                targetText.text = "";
                hintText.text = confirmKey + " : ปิดเมนู";
                return;
            }
            if (sel == CraftMenuState.RepairSlot)
            {
                int amount = Mathf.RoundToInt((cfg != null ? cfg.repairAmount : 100) * (upgrades != null ? upgrades.RepairMultiplier : 1f));
                nameText.text = "ซ่อมเกาะ (REPAIR)";
                dmgText.text = UIText.Colored("+" + amount + " HP", UIText.Darker(Palette.Leaf, 0.75f));
                for (int i = 0; i < costs.Length; i++) costs[i].rt.gameObject.SetActive(false);
                infoText.text = "ใช้ชิ้นส่วนอะไรก็ได้ " + repairCost + " ชิ้น (มี " + partsHeld + ")";
                targetText.text = "";
                if (state.RepairCooldown > 0f) hintText.text = "ซ่อมได้อีกใน " + state.RepairCooldown.ToString("0.0") + " วิ";
                else if (island != null && island.Hp >= island.MaxHp) hintText.text = "เลือดเต็มแล้ว";
                else hintText.text = confirmKey + " : ซ่อมเกาะ";
                return;
            }
            if (sel == CraftMenuState.UpgradeSlot)
            {
                nameText.text = "อัปเกรด (" + upgradeCount + "/" + upgradeMax + ")";
                dmgText.text = "";
                for (int i = 0; i < costs.Length; i++) costs[i].rt.gameObject.SetActive(false);
                targetText.text = "";
                if (upgradeCount >= upgradeMax)
                {
                    infoText.text = "อัปเกรดครบแล้ว";
                    hintText.text = "";
                }
                else
                {
                    infoText.text = "ใช้ชิ้นส่วนอะไรก็ได้ " + upgradeCost + " ชิ้น (มี " + partsHeld + ")";
                    hintText.text = upgradeReady ? confirmKey + " : เปิดการ์ด" : UIText.Colored("ชิ้นส่วนยังไม่พอ", UIFactory.WithAlpha(Palette.Ink, 0.45f));
                }
                return;
            }

            var recipe = root.Recipe(sel);
            if (recipe == null)
            {
                nameText.text = "—";
                dmgText.text = "";
                for (int i = 0; i < costs.Length; i++) costs[i].rt.gameObject.SetActive(false);
                infoText.text = "(ยังไม่มีสูตรจรวด)";
            }
            else
            {
                var tc = UIText.Darker(Palette.Tier(recipe.tier), 0.75f);
                nameText.text = UIText.Colored(recipe.displayName.ToUpperInvariant(), tc) + " (T" + (int)recipe.tier + ")";
                dmgText.text = "DMG " + Mathf.RoundToInt(recipe.damage * (upgrades != null ? upgrades.DamageMultiplier : 1f));
                infoText.text = "";
                int n = recipe.costs != null ? recipe.costs.Length : 0;
                for (int i = 0; i < costs.Length; i++)
                {
                    var cell = costs[i];
                    if (i >= n)
                    {
                        cell.rt.gameObject.SetActive(false);
                        continue;
                    }
                    var cost = recipe.costs[i];
                    int have = inventory != null ? inventory.Count(cost.Key) : 0;
                    bool shortOf = have < cost.count;
                    cell.rt.gameObject.SetActive(true);
                    cell.icon.sprite = UISprites.Part(cost.type);
                    cell.icon.color = Palette.Tier(cost.tier);
                    string label = UIText.PartThai(cost.type) + UIText.TierThai(cost.tier) + " " + have + "/" + cost.count;
                    cell.text.text = shortOf ? UIText.Colored(label, Palette.Danger) : UIText.Colored(label, UIText.Darker(Palette.Leaf, 0.75f));
                }
            }

            bool robot = state.Target == TargetKind.Robot;
            string targetName = robot ? "หุ่นยนต์" : "เกาะศัตรู";
            Color targetColor = robot ? Palette.HelmetOrange : Palette.Player(id.Opponent());
            targetText.text = "เป้า: ◀ " + UIText.Colored(targetName, UIText.Darker(targetColor, 0.85f)) + " ▶";

            bool canAfford = recipe != null && inventory != null && inventory.CanAfford(recipe);
            if (state.RocketCooldown > 0f)
                hintText.text = UIText.Colored("ชาร์จอีก " + state.RocketCooldown.ToString("0.0") + " วิ", Palette.Danger);
            else hintText.text = canAfford ? confirmKey + " : คราฟ + ยิง!" : UIText.Colored(confirmKey + " : คราฟ + ยิง! (ของไม่พอ)", UIFactory.WithAlpha(Palette.Ink, 0.45f));
        }

        // ---------------------------------------------------------------- follow

        void LateUpdate()
        {
            TryBind();
            bool shouldShow = state != null && state.IsOpen && root.IsPlayingPhase;

            if (shouldShow && !visible)
            {
                visible = true;
                content.gameObject.SetActive(true);
                Refresh();
                UITween.PopIn(content, 0.3f, 0f);
            }
            else if (!shouldShow && visible)
            {
                visible = false;
                UITween.ScaleTo(content, Vector3.zero, 0.12f);
            }

            if (!visible && content.gameObject.activeSelf && content.localScale.x < 0.02f)
                content.gameObject.SetActive(false);

            if (!content.gameObject.activeSelf) return;
            if (visible && (state.RocketCooldown > 0f || state.RepairCooldown > 0f)) Refresh();

            Vector3 head;
            Vector2 local;
            if (UIWorld.TryGetHead(id, out head) && UIWorld.WorldToCanvas(head, space, out local))
            {
                local += new Vector2(0f, 26f);
                Rect r = space.rect;
                float halfW = PanelW * 0.5f + 10f;
                local.x = Mathf.Clamp(local.x, r.xMin + halfW, r.xMax - halfW);
                local.y = Mathf.Clamp(local.y, r.yMin + 10f, r.yMax - PanelH - 10f);
                follow.anchoredPosition = local;
            }
        }
    }
}
