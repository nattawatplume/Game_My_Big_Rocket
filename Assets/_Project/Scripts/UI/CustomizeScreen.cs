using UnityEngine;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace IslandRocketRumble
{
    /// <summary>
    /// Dress-up screen opened from the title (the phase stays Title). Two compact cream panels, one per player, each
    /// driven by that player's own keys at the same time: W/S ↑/↓ pick a row, A/D ←/→ change it (live on the floating
    /// character), SPACE / ENTER toggles ready. When both are ready the looks are saved and the title comes back.
    /// </summary>
    public class CustomizeScreen : UIScreen
    {
        const int RowCount = 4;
        const int SparkleCount = 7;

        class Row
        {
            public Image bar;
            public UText label, value, sub;
            public Image left, right, swatch;
        }

        class Side
        {
            public PlayerId id;
            public RectTransform panel;
            public readonly Row[] rows = new Row[RowCount];
            public int selected;
            public bool ready;
            public RectTransform readyPill;
            public Image readyFill;
            public UText readyText;
            public readonly RectTransform[] sparkles = new RectTransform[SparkleCount];
            public readonly Image[] sparkleImgs = new Image[SparkleCount];
        }

        readonly Side[] sides = new Side[2];
        RectTransform ribbon, hint, sparkleLayer;
        float finishAt = -1f;

        // ------------------------------------------------------------------ build

        protected override void Build()
        {
            sparkleLayer = UIFactory.Stretch(rect, "Sparkles");

            ribbon = UIFactory.Panel(rect, "Ribbon", UIFactory.TopCenter, new Vector2(0f, -78f), new Vector2(640f, 100f),
                Palette.HelmetYellow, 50f, 6f, true);
            var title = UIFactory.Txt(ribbon, "Title", "แต่งตัว · CUSTOMIZE", 52, Color.white, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 2f), new Vector2(620f, 96f), true);
            UIFactory.Chunky(title, 3f, 4f);

            for (int i = 0; i < 2; i++) sides[i] = BuildSide((PlayerId)i);

            hint = UIFactory.Panel(rect, "Hint", UIFactory.BottomCenter, new Vector2(0f, 52f), new Vector2(730f, 58f),
                UIFactory.WithAlpha(Palette.Cream, 0.92f), 29f, 4f, true);
            UIFactory.Txt(hint, "Label", "W/S ↑/↓ เลือก  ·  A/D ←/→ เปลี่ยน  ·  SPACE/ENTER พร้อม", 24, Palette.Ink,
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 1f), new Vector2(720f, 54f), true);
        }

        Side BuildSide(PlayerId id)
        {
            var s = new Side { id = id };
            bool p1 = id == PlayerId.P1;
            Color pc = Palette.Player(id);
            var size = new Vector2(540f, 370f);
            s.panel = UIFactory.Panel(rect, "Panel_" + UIText.PlayerTag(id), p1 ? UIFactory.BottomLeft : UIFactory.BottomRight,
                new Vector2(p1 ? 40f + size.x * 0.5f : -(40f + size.x * 0.5f), 30f + size.y * 0.5f), size, Palette.Cream, 32f, 5f, true);

            var header = UIFactory.Panel(s.panel, "Header", UIFactory.TopCenter, new Vector2(0f, 2f), new Vector2(300f, 62f), pc, 31f, 4f, false);
            var ht = UIFactory.Txt(header, "Label", UIText.PlayerTag(id) + " · " + UIText.PlayerThai(id), 32, Color.white,
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 1f), new Vector2(290f, 58f), true);
            UIFactory.AddOutline(ht, UIFactory.WithAlpha(Palette.Ink, 0.7f), 2f);

            float[] ys = { 105f, 47f, -11f, -69f };
            for (int r = 0; r < RowCount; r++) s.rows[r] = BuildRow(s.panel, (CosmeticSlot)r, ys[r], pc);

            s.readyPill = UIFactory.Panel(s.panel, "Ready", UIFactory.BottomCenter, new Vector2(0f, 46f), new Vector2(340f, 60f),
                Color.white, out s.readyFill, 30f, 4f, false);
            s.readyText = UIFactory.Txt(s.readyPill, "Label", "", 28, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, 1f), new Vector2(330f, 56f), true);

            for (int k = 0; k < SparkleCount; k++)
            {
                var img = UIFactory.Img(sparkleLayer, "Sparkle_" + UIText.PlayerTag(id) + k, UISprites.Sparkle,
                    k % 2 == 0 ? Color.white : Color.Lerp(pc, Color.white, 0.45f), UIFactory.Center, Vector2.zero, new Vector2(34f, 34f));
                s.sparkleImgs[k] = img;
                s.sparkles[k] = img.rectTransform;
            }
            return s;
        }

        Row BuildRow(RectTransform panel, CosmeticSlot slot, float y, Color pc)
        {
            var row = new Row();
            var rt = UIFactory.Rect(panel, "Row_" + slot, UIFactory.Center, new Vector2(0f, y), new Vector2(500f, 54f));
            row.bar = UIFactory.Img(rt, "Bar", UISprites.Rounded, Color.clear, UIFactory.Center, Vector2.zero, new Vector2(500f, 54f));
            UIFactory.SetRadius(row.bar, 27f);
            row.label = UIFactory.Txt(rt, "Label", CosmeticCatalog.SlotThai(slot), 27, Palette.Ink, TextAnchor.MiddleLeft,
                UIFactory.Center, new Vector2(-170f, 1f), new Vector2(130f, 50f), true);

            row.left = UIFactory.Icon(rt, "Left", UISprites.TriangleDown, pc, UIFactory.Center, new Vector2(-82f, 0f), 30f, 2f);
            row.left.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            row.right = UIFactory.Icon(rt, "Right", UISprites.TriangleDown, pc, UIFactory.Center, new Vector2(218f, 0f), 30f, 2f);
            row.right.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            row.swatch = UIFactory.Icon(rt, "Swatch", UISprites.Circle, Color.white, UIFactory.Center, new Vector2(-46f, 0f), 28f, 2.5f);
            row.swatch.gameObject.SetActive(slot == CosmeticSlot.Color);

            row.value = UIFactory.Txt(rt, "Value", "", 26, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(76f, 8f), new Vector2(230f, 34f), true);
            row.sub = UIFactory.Txt(rt, "Sub", "", 16, UIFactory.WithAlpha(Palette.Ink, 0.6f), TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(76f, -15f), new Vector2(230f, 22f));
            return row;
        }

        // ------------------------------------------------------------------ lifecycle

        protected override void OnShow()
        {
            finishAt = -1f;
            CosmeticsStore.PreviewMode = true;
            UITween.PopIn(ribbon, 0.4f, 0f);
            UITween.PopIn(hint, 0.35f, 0.3f);
            foreach (var s in sides)
            {
                s.selected = 0;
                s.ready = false;
                for (int r = 0; r < RowCount; r++) RefreshRow(s, r);
                RefreshReady(s);
                UITween.PopIn(s.panel, 0.4f, 0.1f + (int)s.id * 0.08f);
            }
        }

        protected override void OnHide()
        {
            CosmeticsStore.PreviewMode = false;
        }

        void RefreshRow(Side s, int r)
        {
            var slot = (CosmeticSlot)r;
            var row = s.rows[r];
            var look = CosmeticsStore.Get(s.id);
            var opt = CosmeticCatalog.Get(slot, look[slot]);
            row.value.text = opt.thai;
            row.sub.text = opt.english;
            if (slot == CosmeticSlot.Color) row.swatch.color = opt.color;

            bool sel = r == s.selected && !s.ready;
            Color pc = Palette.Player(s.id);
            row.bar.color = sel ? UIFactory.WithAlpha(pc, 0.22f) : Color.clear;
            float a = sel ? 1f : 0.3f;
            row.left.color = UIFactory.WithAlpha(pc, a);
            row.right.color = UIFactory.WithAlpha(pc, a);
        }

        void RefreshReady(Side s)
        {
            if (s.ready)
            {
                s.readyFill.color = Palette.HpP1;
                s.readyText.text = "พร้อม! ✓";
                s.readyText.color = Color.white;
            }
            else
            {
                s.readyFill.color = Color.white;
                s.readyText.text = UIText.ConfirmKey(s.id) + " = พร้อม";
                s.readyText.color = Palette.Ink;
            }
        }

        // ------------------------------------------------------------------ input

        void Update()
        {
            float t = Time.unscaledTime;
            foreach (var s in sides) TickSparkles(s, t);

            if (finishAt >= 0f)
            {
                if (t >= finishAt) Finish();
                return;
            }
            if (!InputReady()) return;

            foreach (var s in sides) HandleInput(s);

            if (sides[0].ready && sides[1].ready)
            {
                finishAt = t + 0.6f;
                UITween.Punch(ribbon, 0.25f, 0.4f);
                Blip(GameSfx.MenuOpen, PlayerId.P1, 0f);
            }
        }

        void HandleInput(Side s)
        {
            var id = s.id;
            if (PlayerInput2P.ConfirmPressed(id))
            {
                s.ready = !s.ready;
                RefreshReady(s);
                for (int r = 0; r < RowCount; r++) RefreshRow(s, r);
                UITween.Punch(s.readyPill, 0.25f, 0.35f);
                Blip(s.ready ? GameSfx.UiTarget : GameSfx.MenuClose, id);
                return;
            }
            if (s.ready) return;

            int prev = s.selected;
            if (PlayerInput2P.UpPressed(id)) s.selected = (s.selected + RowCount - 1) % RowCount;
            if (PlayerInput2P.DownPressed(id)) s.selected = (s.selected + 1) % RowCount;
            if (prev != s.selected)
            {
                RefreshRow(s, prev);
                RefreshRow(s, s.selected);
                UITween.Punch(s.rows[s.selected].bar, 0.12f, 0.25f);
                Blip(GameSfx.UiMove, id);
            }

            int dir = (PlayerInput2P.RightPressed(id) ? 1 : 0) - (PlayerInput2P.LeftPressed(id) ? 1 : 0);
            if (dir == 0) return;
            var slot = (CosmeticSlot)s.selected;
            var look = CosmeticsStore.Get(id);
            look[slot] += dir;
            CosmeticsStore.Set(id, look); // Set wraps the index and re-dresses the character live
            var row = s.rows[s.selected];
            RefreshRow(s, s.selected);
            UITween.Punch(row.value, 0.25f, 0.3f);
            UITween.Punch(dir > 0 ? row.right : row.left, 0.35f, 0.3f);
            if (slot == CosmeticSlot.Color) UITween.Punch(row.swatch, 0.3f, 0.3f);
            Blip(GameSfx.UiMove, id);
        }

        void Finish()
        {
            finishAt = -1f;
            CosmeticsStore.Save();
            root.CloseCustomize();
        }

        static void Blip(AudioClip clip, PlayerId id, float pan = float.NaN)
        {
            var am = AudioManager.Instance;
            if (am == null) return;
            am.Play(clip, 0.5f, float.IsNaN(pan) ? (id == PlayerId.P1 ? -0.4f : 0.4f) : pan, minInterval: 0.02f);
        }

        // ------------------------------------------------------------------ sparkles around the floating characters

        void TickSparkles(Side s, float t)
        {
            var look = CharacterLook.For(s.id);
            float blend = look != null ? look.PreviewBlend : 0f;
            Vector2 center = Vector2.zero;
            bool visible = blend > 0.01f && look != null && UIWorld.WorldToCanvas(look.BodyCenter, rect, out center);
            for (int k = 0; k < SparkleCount; k++)
            {
                var img = s.sparkleImgs[k];
                if (!visible)
                {
                    img.enabled = false;
                    continue;
                }
                img.enabled = true;
                float ph = k * Mathf.PI * 2f / SparkleCount + (int)s.id;
                float ang = ph + t * 0.6f;
                var offset = new Vector2(Mathf.Cos(ang) * 190f, Mathf.Sin(ang) * 210f + Mathf.Sin(t * 1.3f + k) * 18f);
                s.sparkles[k].anchoredPosition = center + offset;
                float tw = 0.5f + 0.5f * Mathf.Sin(t * 3.1f + k * 1.7f);
                float sc = (0.45f + 0.75f * tw) * blend;
                s.sparkles[k].localScale = new Vector3(sc, sc, 1f);
                s.sparkles[k].localRotation = Quaternion.Euler(0f, 0f, t * 40f + k * 25f);
                var c = img.color;
                c.a = (0.35f + 0.65f * tw) * blend;
                img.color = c;
            }
        }
    }
}
