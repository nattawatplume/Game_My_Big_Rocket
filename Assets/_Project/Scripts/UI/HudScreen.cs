using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace IslandRocketRumble
{
    /// <summary>
    /// In-game HUD: HP bars with damage chip, inventory strips, center time/tier pill,
    /// robot rage meter, tier-up and sudden-death banners.
    /// </summary>
    public class HudScreen : UIScreen
    {
        // ---------------------------------------------------------------- per-player widgets
        class PlayerHud
        {
            public PlayerId id;
            public RectTransform panel;
            public UText label;
            public UIHpBar bar;
            public InventoryStrip strip;
            public Inventory boundInventory;
            public System.Action handler;
        }

        readonly PlayerHud[] players = new PlayerHud[2];

        RectTransform centerPill;
        UText timeText;
        UText tierThaiText;
        int lastShownSecond = -1;
        float elapsed;
        PartTier shownTier = PartTier.Scrap;

        RectTransform rageMeter;
        UText rageLabel;
        readonly Image[] ragePips = new Image[4];
        int rageLevel;

        RectTransform banner;
        CanvasGroup bannerGroup;
        UText bannerBig;
        UText bannerSmall;
        Coroutine bannerCo;

        MatchContext boundCtx;
        GamePhase phase = GamePhase.Title;

        static readonly Color[] rageColors =
        {
            Palette.Leaf, Palette.HelmetYellow, Palette.HelmetOrange, Palette.Danger
        };

        // ---------------------------------------------------------------- build

        protected override void Build()
        {
            players[0] = BuildPlayer(PlayerId.P1);
            players[1] = BuildPlayer(PlayerId.P2);
            BuildCenter();
            BuildRage();
            BuildBanner();

            Subscribe();
        }

        /// <summary>Idempotent (re)subscription to the global event bus.</summary>
        public void Subscribe()
        {
            GameEvents.ClockTick -= OnClockTick;
            GameEvents.TierChanged -= OnTierChanged;
            GameEvents.IslandDamaged -= OnIslandDamaged;
            GameEvents.RobotRageChanged -= OnRageChanged;
            GameEvents.ClockTick += OnClockTick;
            GameEvents.TierChanged += OnTierChanged;
            GameEvents.IslandDamaged += OnIslandDamaged;
            GameEvents.RobotRageChanged += OnRageChanged;
        }

        void OnDestroy()
        {
            GameEvents.ClockTick -= OnClockTick;
            GameEvents.TierChanged -= OnTierChanged;
            GameEvents.IslandDamaged -= OnIslandDamaged;
            GameEvents.RobotRageChanged -= OnRageChanged;
            UnbindInventories();
        }

        PlayerHud BuildPlayer(PlayerId id)
        {
            bool left = id == PlayerId.P1;
            var ph = new PlayerHud { id = id };
            Vector2 anchor = left ? UIFactory.TopLeft : UIFactory.TopRight;
            const float w = 640f, h = 112f;
            Vector2 pos = new Vector2(left ? 36f + w * 0.5f : -(36f + w * 0.5f), -(26f + h * 0.5f));

            Image fill;
            ph.panel = UIFactory.Panel(rect, "HP_" + UIText.PlayerTag(id), anchor, pos, new Vector2(w, h), Palette.Cream, out fill, 28f, 5f, true);

            // Player badge (colored circle with tag)
            var badge = UIFactory.Img(ph.panel, "Badge", UISprites.Circle, Palette.Player(id), left ? UIFactory.MidLeft : UIFactory.MidRight,
                new Vector2(left ? 28f : -28f, 4f), new Vector2(84f, 84f));
            UIFactory.AddOutline(badge, Palette.Ink, 4f);
            var bt = UIFactory.Txt(badge.transform, "Tag", UIText.PlayerTag(id), 34, Color.white, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 1f), new Vector2(84f, 84f), true);
            UIFactory.AddShadow(bt, UIFactory.WithAlpha(Palette.Ink, 0.6f), new Vector2(0f, -2f));

            // Label "P1 HP 520/600"
            ph.label = UIFactory.Txt(ph.panel, "Label", "", 30, Palette.Ink, left ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight,
                left ? UIFactory.TopLeft : UIFactory.TopRight, new Vector2(left ? 84f + 260f : -(84f + 260f), -30f), new Vector2(520f, 40f), true);

            var nameTxt = UIFactory.Txt(ph.panel, "Name", UIText.PlayerThai(id), 24, UIText.Darker(Palette.Player(id), 0.85f),
                left ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, left ? UIFactory.TopRight : UIFactory.TopLeft,
                new Vector2(left ? -24f - 100f : 24f + 100f, -30f), new Vector2(200f, 36f), true);
            nameTxt.name = "Name";

            // Bar track
            var track = UIFactory.Img(ph.panel, "Track", UISprites.Rounded, UIFactory.WithAlpha(Palette.Ink, 0.85f),
                left ? UIFactory.BottomLeft : UIFactory.BottomRight, new Vector2(left ? 84f + 266f : -(84f + 266f), 30f), new Vector2(532f, 34f));
            UIFactory.SetRadius(track, 17f);
            var inner = UIFactory.Stretch(track.transform, "Inner", 4f);

            var chip = UIFactory.StretchImg(inner, "Chip", UISprites.Rounded, Color.white);
            UIFactory.SetRadius(chip, 13f);
            var fillBar = UIFactory.StretchImg(inner, "Fill", UISprites.Rounded, id == PlayerId.P1 ? Palette.HpP1 : Palette.HpP2);
            UIFactory.SetRadius(fillBar, 13f);
            var gloss = UIFactory.Img(fillBar.transform, "Gloss", UISprites.Rounded, new Color(1f, 1f, 1f, 0.35f), UIFactory.TopCenter,
                new Vector2(0f, -6f), new Vector2(0f, 6f));
            gloss.rectTransform.anchorMin = new Vector2(0f, 1f);
            gloss.rectTransform.anchorMax = new Vector2(1f, 1f);
            gloss.rectTransform.offsetMin = new Vector2(8f, -10f);
            gloss.rectTransform.offsetMax = new Vector2(-8f, -4f);
            UIFactory.SetRadius(gloss, 3f);

            ph.bar = track.gameObject.AddComponent<UIHpBar>();
            ph.bar.Setup(fillBar, chip, !left);

            // Inventory strip below the panel
            var stripRect = UIFactory.Rect(rect, "Inv_" + UIText.PlayerTag(id), anchor,
                new Vector2(left ? 36f + 330f : -(36f + 330f), -(26f + h + 40f)), new Vector2(660f, 56f));
            ph.strip = new InventoryStrip(stripRect, !left);

            SetHpLabel(ph, root.MaxHp, root.MaxHp);
            return ph;
        }

        void BuildCenter()
        {
            Image fill;
            centerPill = UIFactory.Panel(rect, "CenterPill", UIFactory.TopCenter, new Vector2(0f, -(26f + 46f)), new Vector2(520f, 92f),
                Palette.Cream, out fill, 46f, 5f, true);
            timeText = UIFactory.Txt(centerPill, "Time", "05:00 · TIER 1 SCRAP", 36, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 10f), new Vector2(510f, 50f), true);
            tierThaiText = UIFactory.Txt(centerPill, "TierThai", "เกรด: เศษเหล็ก", 22, UIFactory.WithAlpha(Palette.Ink, 0.7f),
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, -24f), new Vector2(510f, 30f));
        }

        void BuildRage()
        {
            Image fill;
            // Bottom of the screen: at the top it covered the robot once rage made it grow.
            rageMeter = UIFactory.Panel(rect, "RageMeter", UIFactory.BottomCenter, new Vector2(0f, 24f + 34f),
                new Vector2(520f, 68f), Palette.Cream, out fill, 30f, 4f, true);
            var title = UIFactory.Txt(rageMeter, "Title", "ฝันร้าย cpe40 · ความโกรธ", 21, Palette.Ink, TextAnchor.MiddleLeft, UIFactory.MidLeft,
                new Vector2(22f + 160f, 12f), new Vector2(320f, 30f), true);
            title.name = "Title";
            rageLabel = UIFactory.Txt(rageMeter, "Level", "", 19, Palette.Ink, TextAnchor.MiddleLeft, UIFactory.MidLeft,
                new Vector2(22f + 160f, -13f), new Vector2(320f, 28f), true);
            for (int i = 0; i < 4; i++)
            {
                var pip = UIFactory.Img(rageMeter, "Pip" + i, UISprites.Rounded, Color.white, UIFactory.MidRight,
                    new Vector2(-24f - 15f - (3 - i) * 36f, 0f), new Vector2(28f, 38f));
                UIFactory.SetRadius(pip, 8f);
                UIFactory.AddOutline(pip, Palette.Ink, 2.5f);
                ragePips[i] = pip;
            }
            SetRage(0, false);
        }

        void BuildBanner()
        {
            banner = UIFactory.Rect(rect, "Banner", UIFactory.Center, new Vector2(0f, 200f), new Vector2(1200f, 220f));
            bannerGroup = UIFactory.Group(banner);
            var ribbon = UIFactory.Panel(banner, "Ribbon", UIFactory.Center, new Vector2(0f, -78f), new Vector2(560f, 56f),
                Palette.Cream, 28f, 4f, true);
            ribbon.name = "Ribbon";
            bannerBig = UIFactory.Txt(banner, "Big", "", 110, Palette.HelmetYellow, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 20f), new Vector2(1200f, 150f), true);
            UIFactory.Chunky(bannerBig, 6f, 9f);
            bannerSmall = UIFactory.Txt(ribbon, "Small", "", 30, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 1f), new Vector2(560f, 50f), true);
            banner.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- lifecycle

        protected override void OnShow()
        {
            TryBind();
            RefreshAll();
            UITween.PopIn(players[0].panel, 0.4f, 0f);
            UITween.PopIn(players[1].panel, 0.4f, 0.05f);
            UITween.PopIn(centerPill, 0.4f, 0.1f);
            UITween.PopIn(rageMeter, 0.4f, 0.15f);
        }

        protected override void OnHide()
        {
            if (bannerCo != null) StopCoroutine(bannerCo);
            bannerCo = null;
            banner.gameObject.SetActive(false);
        }

        void Update()
        {
            TryBind();
            // last 10 seconds: red pulsing time
            if (phase == GamePhase.Playing)
            {
                float remaining = root.MatchDuration - elapsed;
                if (remaining <= 10.5f && remaining > 0f)
                {
                    float s = 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f)) * 0.04f;
                    timeText.rectTransform.localScale = new Vector3(s, s, 1f);
                }
                else timeText.rectTransform.localScale = Vector3.one;
            }
            if (rageLevel >= 3)
            {
                float a = Mathf.Sin(Time.unscaledTime * 30f) * 1.5f;
                rageMeter.localRotation = Quaternion.Euler(0f, 0f, a);
            }
            else rageMeter.localRotation = Quaternion.identity;
        }

        public void OnPhase(GamePhase p, GamePhase prev)
        {
            phase = p;
            if (p == GamePhase.SuddenDeath && prev != GamePhase.SuddenDeath)
            {
                ShowBanner("SUDDEN DEATH!", "ใครยิงโดนก่อน ชนะ!", Palette.Danger, 2.2f);
                RefreshTime(true);
            }
            if (p == GamePhase.Result)
            {
                if (bannerCo != null) StopCoroutine(bannerCo);
                bannerCo = null;
                banner.gameObject.SetActive(false);
            }
        }

        public void ResetMatch()
        {
            elapsed = 0f;
            shownTier = PartTier.Scrap;
            lastShownSecond = -1;
            rageLevel = 0;
            SetRage(0, false);
            for (int i = 0; i < 2; i++)
            {
                players[i].bar.SetInstant(1f);
                SetHpLabel(players[i], root.MaxHp, root.MaxHp);
            }
            RefreshTime(true);
        }

        void RefreshAll()
        {
            int max = root.MaxHp;
            for (int i = 0; i < 2; i++)
            {
                int hp = root.Hp[i];
                players[i].bar.SetInstant(hp / (float)max);
                SetHpLabel(players[i], hp, max);
                RefreshInventory(players[i], false);
            }
            RefreshTime(true);
        }

        // ---------------------------------------------------------------- binding (lazy MatchContext)

        void TryBind()
        {
            var ctx = MatchContext.Instance;
            if (ctx == boundCtx) return;
            UnbindInventories();
            boundCtx = ctx;
            if (ctx == null) return;
            for (int i = 0; i < 2; i++)
            {
                var ph = players[i];
                ph.boundInventory = ctx.Inventory(ph.id);
                var captured = ph;
                ph.handler = () => RefreshInventory(captured, true);
                ph.boundInventory.Changed += ph.handler;
                RefreshInventory(ph, false);
            }
        }

        void UnbindInventories()
        {
            for (int i = 0; i < 2; i++)
            {
                var ph = players[i];
                if (ph == null) continue;
                if (ph.boundInventory != null && ph.handler != null) ph.boundInventory.Changed -= ph.handler;
                ph.boundInventory = null;
                ph.handler = null;
            }
            boundCtx = null;
        }

        void RefreshInventory(PlayerHud ph, bool animate)
        {
            if (ph.boundInventory == null) { ph.strip.Refresh(null, false); return; }
            ph.strip.Refresh(ph.boundInventory, animate && IsShown);
        }

        // ---------------------------------------------------------------- events

        void OnClockTick(float e)
        {
            elapsed = e;
            RefreshTime(false);
        }

        void RefreshTime(bool force)
        {
            float duration = root.MatchDuration;
            float remaining = Mathf.Max(0f, duration - elapsed);
            int sec = Mathf.CeilToInt(remaining);
            var cfg = root.Config;
            PartTier tier = cfg != null ? cfg.TierAt(elapsed) : shownTier;
            if (!force && sec == lastShownSecond && tier == shownTier) return;
            bool secondChanged = sec != lastShownSecond;
            lastShownSecond = sec;
            shownTier = tier;

            string tierStr = UIText.Colored("TIER " + (int)tier + " " + UIText.TierEn(tier), UIText.Darker(Palette.Tier(tier), 0.8f));
            string timeStr;
            if (phase == GamePhase.SuddenDeath)
                timeStr = UIText.Colored("SUDDEN DEATH", Palette.Danger);
            else
            {
                string t = (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00");
                timeStr = remaining <= 10.5f && phase == GamePhase.Playing ? UIText.Colored(t, Palette.Danger) : t;
            }
            timeText.text = timeStr + " · " + tierStr;
            tierThaiText.text = "เกรด: " + UIText.TierThai(tier);

            if (secondChanged && phase == GamePhase.Playing && remaining <= 10.5f && remaining > 0f && IsShown)
                UITween.Punch(centerPill, 0.08f, 0.25f);
        }

        void OnTierChanged(PartTier tier)
        {
            shownTier = tier;
            RefreshTime(true);
            if (tier == PartTier.Scrap || !IsShown) return;
            if (phase != GamePhase.Playing && phase != GamePhase.SuddenDeath) return;
            UITween.Punch(centerPill, 0.2f, 0.4f);
            ShowBanner("TIER " + (int)tier + " " + UIText.TierEn(tier) + "!", "ชิ้นส่วนเกรด " + UIText.TierThai(tier) + " ปลดล็อก!",
                Palette.Tier(tier), 1.8f);
        }

        void OnIslandDamaged(PlayerId owner, int damage, int hpLeft, Vector3 point)
        {
            var ph = players[(int)owner];
            int max = root.MaxHp;
            hpLeft = Mathf.Max(0, hpLeft);
            ph.bar.SetDamaged(hpLeft / (float)max);
            SetHpLabel(ph, hpLeft, max);
            if (IsShown)
            {
                UITween.Shake(ph.panel, Mathf.Clamp(6f + damage * 0.06f, 6f, 26f), 0.35f);
                UITween.Punch(ph.label, 0.15f, 0.3f);
            }
        }

        void OnRageChanged(int level, int accumulated)
        {
            bool up = level > rageLevel;
            rageLevel = Mathf.Clamp(level, 0, 3);
            SetRage(rageLevel, IsShown);
            if (IsShown)
            {
                UITween.Shake(rageMeter, up ? 18f : 7f, up ? 0.5f : 0.25f);
                if (up) UITween.Punch(rageMeter, 0.18f, 0.4f);
            }
        }

        // ---------------------------------------------------------------- helpers

        void SetHpLabel(PlayerHud ph, int hp, int max)
        {
            bool low = hp <= max * 0.25f;
            string hpStr = hp + "/" + max;
            if (low) hpStr = UIText.Colored(hpStr, Palette.Danger);
            ph.label.text = UIText.PlayerTag(ph.id) + " HP " + hpStr;
        }

        void SetRage(int level, bool animate)
        {
            rageLabel.text = "LV" + level + " " + UIText.Colored(UIText.RageThai(level), UIText.Darker(rageColors[level], 0.8f));
            for (int i = 0; i < 4; i++)
            {
                bool on = i <= level;
                ragePips[i].color = on ? rageColors[level] : UIFactory.WithAlpha(Palette.Ink, 0.12f);
                if (animate && on && i == level) UITween.Punch(ragePips[i], 0.4f, 0.35f);
            }
        }

        void ShowBanner(string big, string small, Color color, float hold)
        {
            if (!IsShown) return;
            if (bannerCo != null) StopCoroutine(bannerCo);
            bannerBig.text = big;
            bannerBig.color = color;
            bannerSmall.text = small;
            bannerCo = StartCoroutine(BannerRoutine(hold));
        }

        IEnumerator BannerRoutine(float hold)
        {
            banner.gameObject.SetActive(true);
            bannerGroup.alpha = 1f;
            UITween.PopIn(banner, 0.45f, 0f);
            float t = 0f;
            while (t < hold) { t += Time.unscaledDeltaTime; yield return null; }
            UITween.Fade(bannerGroup, 0f, 0.35f);
            t = 0f;
            while (t < 0.4f) { t += Time.unscaledDeltaTime; yield return null; }
            banner.gameObject.SetActive(false);
            bannerCo = null;
        }
    }

    /// <summary>Row of part icons + counts for one player, highest tier first.</summary>
    public class InventoryStrip
    {
        class Cell
        {
            public RectTransform rt;
            public Image icon;
            public UText count;
        }

        const int MaxCells = 8;
        const float CellW = 76f;
        const float TierGap = 14f;

        readonly RectTransform container;
        readonly bool rightAligned;
        readonly List<Cell> cells = new List<Cell>();
        readonly UText emptyText;
        readonly Dictionary<PartKey, int> lastCounts = new Dictionary<PartKey, int>();
        readonly List<KeyValuePair<PartKey, int>> scratch = new List<KeyValuePair<PartKey, int>>();

        public InventoryStrip(RectTransform container, bool rightAligned)
        {
            this.container = container;
            this.rightAligned = rightAligned;
            emptyText = UIFactory.Txt(container, "Empty", "ยังไม่มีชิ้นส่วน · เดินเก็บของที่ตกจากฟ้า!", 20,
                UIFactory.WithAlpha(Palette.Ink, 0.6f), rightAligned ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft,
                UIFactory.Center, Vector2.zero, container.sizeDelta);
            UIFactory.AddOutline(emptyText, UIFactory.WithAlpha(Color.white, 0.7f), 1.5f);
        }

        Cell MakeCell(int index)
        {
            var c = new Cell();
            Image bg;
            c.rt = UIFactory.Panel(container, "Cell" + index, rightAligned ? UIFactory.MidRight : UIFactory.MidLeft, Vector2.zero,
                new Vector2(CellW - 6f, 50f), UIFactory.WithAlpha(Color.white, 0.92f), out bg, 25f, 3f, false);
            c.icon = UIFactory.Icon(c.rt, "Icon", UISprites.Part(PartType.Gear), Color.white, UIFactory.MidLeft, new Vector2(24f, 0f), 34f, 1.5f);
            c.count = UIFactory.Txt(c.rt, "Count", "×1", 22, Palette.Ink, TextAnchor.MiddleLeft, UIFactory.MidLeft, new Vector2(42f + 18f, 0f),
                new Vector2(36f, 40f), true);
            cells.Add(c);
            return c;
        }

        public void Refresh(Inventory inv, bool animate)
        {
            scratch.Clear();
            if (inv != null)
                foreach (var kv in inv.All())
                    if (kv.Value > 0) scratch.Add(kv);

            scratch.Sort((a, b) =>
            {
                int t = ((int)b.Key.tier).CompareTo((int)a.Key.tier);
                return t != 0 ? t : ((int)a.Key.type).CompareTo((int)b.Key.type);
            });

            int n = Mathf.Min(scratch.Count, MaxCells);
            float x = 0f;
            PartTier prevTier = PartTier.Scrap;
            for (int i = 0; i < n; i++)
            {
                var kv = scratch[i];
                var c = i < cells.Count ? cells[i] : MakeCell(i);
                c.rt.gameObject.SetActive(true);
                if (i > 0 && kv.Key.tier != prevTier) x += TierGap;
                prevTier = kv.Key.tier;

                float cx = x + (CellW - 6f) * 0.5f;
                c.rt.anchoredPosition = new Vector2(rightAligned ? -cx : cx, 0f);
                c.icon.sprite = UISprites.Part(kv.Key.type);
                c.icon.color = Palette.Tier(kv.Key.tier);
                c.count.text = "×" + kv.Value;
                x += CellW;

                int old;
                lastCounts.TryGetValue(kv.Key, out old);
                if (animate && kv.Value > old) UITween.Punch(c.rt, 0.35f, 0.35f);
            }
            for (int i = n; i < cells.Count; i++) cells[i].rt.gameObject.SetActive(false);
            emptyText.gameObject.SetActive(n == 0);

            lastCounts.Clear();
            for (int i = 0; i < scratch.Count; i++) lastCounts[scratch[i].Key] = scratch[i].Value;
        }
    }
}
