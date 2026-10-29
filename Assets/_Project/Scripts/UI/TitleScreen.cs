using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace IslandRocketRumble
{
    /// <summary>Title: wobbling two-line logo, START / HOW TO PLAY, corner key labels, drifting paper rockets.</summary>
    public class TitleScreen : UIScreen
    {
        class Drifter
        {
            public RectTransform rt;
            public Vector2 pos;
            public Vector2 vel;
            public float phase;
            public float bob;
        }

        readonly List<RectTransform> letters = new List<RectTransform>();
        readonly List<Vector2> letterBase = new List<Vector2>();
        readonly List<Drifter> drifters = new List<Drifter>();

        RectTransform logo;
        RectTransform tagline;
        UIPillButton[] buttons;
        UIMenuNav nav;
        RectTransform p1Label, p2Label;
        float lastConfirm = -10f;

        protected override void Build()
        {
            // Background: translucent sea so the 3D islands peek through.
            UIFactory.StretchImg(rect, "Bg", null, UIFactory.WithAlpha(Palette.Sea, 0.88f));

            // Soft cream "clouds".
            AddCloud(new Vector2(-700f, 380f), 260f);
            AddCloud(new Vector2(-560f, 420f), 180f);
            AddCloud(new Vector2(720f, 360f), 300f);
            AddCloud(new Vector2(560f, 300f), 170f);
            AddCloud(new Vector2(-820f, -260f), 200f);
            AddCloud(new Vector2(840f, -200f), 220f);

            // Drifting paper rockets (behind the logo).
            var drift = UIFactory.Stretch(rect, "Drifters");
            Color[] tints = { Palette.Cream, Palette.Tier(PartTier.Silver), Palette.Tier(PartTier.Gold), Palette.Tier(PartTier.Crystal), Palette.Tier(PartTier.Copper), Palette.Cream, Palette.HpP1 };
            for (int i = 0; i < 7; i++)
            {
                var img = UIFactory.Icon(drift, "PaperRocket" + i, UISprites.Rocket(1 + (i % 5)), tints[i], UIFactory.Center,
                    Vector2.zero, UnityEngine.Random.Range(70f, 120f), 3f);
                var d = new Drifter
                {
                    rt = img.rectTransform,
                    pos = new Vector2(UnityEngine.Random.Range(-1000f, 1000f), UnityEngine.Random.Range(-450f, 450f)),
                    vel = new Vector2(UnityEngine.Random.Range(50f, 110f), UnityEngine.Random.Range(8f, 30f)),
                    phase = UnityEngine.Random.Range(0f, 6.28f),
                    bob = UnityEngine.Random.Range(10f, 26f),
                };
                drifters.Add(d);
            }

            // Logo.
            logo = UIFactory.Rect(rect, "Logo", UIFactory.Center, new Vector2(0f, 250f), new Vector2(1500f, 380f));
            BuildWord(logo, "ISLAND", Palette.HelmetYellow, 170, new Vector2(0f, 95f));
            BuildWord(logo, "ROCKET RUMBLE", Palette.IslandSide, 128, new Vector2(0f, -70f));

            var tag = UIFactory.Txt(rect, "Tagline", "ยิงจรวดข้ามเกาะ!  ·  2 PLAYERS  ·  1 KEYBOARD", 34, Palette.Ink,
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 40f), new Vector2(1200f, 50f), true);
            UIFactory.AddShadow(tag, UIFactory.WithAlpha(Color.white, 0.6f), new Vector2(0f, -2f));
            tagline = tag.rectTransform;

            // Buttons.
            buttons = new[]
            {
                new UIPillButton(rect, "START GAME", Palette.HpP1, Color.white, UIFactory.Center, new Vector2(0f, -110f), new Vector2(480f, 104f)),
                new UIPillButton(rect, "HOW TO PLAY", Palette.Cream, Palette.Ink, UIFactory.Center, new Vector2(0f, -250f), new Vector2(480f, 104f)),
            };
            nav = new UIMenuNav(buttons.Length);

            var hint = UIFactory.Txt(rect, "NavHint", "W/S หรือ ↑/↓ เลือก  ·  SPACE / ENTER ตกลง", 26, UIFactory.WithAlpha(Palette.Ink, 0.75f),
                TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, -350f), new Vector2(900f, 40f));
            hint.name = "NavHint";

            // Corner key labels.
            p1Label = CornerLabel("P1 : W A S D + SPACE", Palette.PipBlue, UIFactory.BottomLeft, new Vector2(40f + 270f, 40f + 42f));
            p2Label = CornerLabel("P2 : ลูกศร + ENTER", Palette.MomoPink, UIFactory.BottomRight, new Vector2(-(40f + 270f), 40f + 42f));
        }

        void AddCloud(Vector2 pos, float size)
        {
            var c = UIFactory.Img(rect, "Cloud", UISprites.Circle, UIFactory.WithAlpha(Palette.Cream, 0.45f), UIFactory.Center, pos,
                new Vector2(size, size * 0.62f));
            c.preserveAspect = false;
        }

        RectTransform CornerLabel(string text, Color color, Vector2 anchor, Vector2 pos)
        {
            Image fill;
            var panel = UIFactory.Panel(rect, "Keys_" + text.Substring(0, 2), anchor, pos, new Vector2(540f, 84f), color, out fill, 42f, 5f, true);
            var t = UIFactory.Txt(panel, "Label", text, 34, Color.white, TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 2f),
                new Vector2(520f, 80f), true);
            UIFactory.AddOutline(t, UIFactory.WithAlpha(Palette.Ink, 0.8f), 2f);
            return panel;
        }

        void BuildWord(RectTransform parent, string word, Color color, int size, Vector2 center)
        {
            var created = new List<UText>();
            var widths = new List<float>();
            float total = 0f;
            float spacing = size * 0.04f;

            for (int i = 0; i < word.Length; i++)
            {
                string ch = word[i].ToString();
                var t = UIFactory.Txt(parent, "L_" + ch, ch, size, color, TextAnchor.MiddleCenter, UIFactory.Center, Vector2.zero,
                    new Vector2(size, size * 1.3f), true);
                float w = word[i] == ' ' ? size * 0.38f : t.preferredWidth;
                if (w <= 1f) w = size * 0.64f;
                created.Add(t);
                widths.Add(w);
                total += w;
            }
            total += spacing * (word.Length - 1);

            float x = -total * 0.5f;
            for (int i = 0; i < created.Count; i++)
            {
                var t = created[i];
                float w = widths[i];
                var rt = t.rectTransform;
                rt.sizeDelta = new Vector2(w + 30f, size * 1.3f);
                var p = new Vector2(x + w * 0.5f, center.y);
                rt.anchoredPosition = p;
                x += w + spacing;
                if (word[i] == ' ')
                {
                    t.enabled = false;
                    continue;
                }
                UIFactory.AddShadow(t, Palette.Ink, new Vector2(0f, -size * 0.07f));
                UIFactory.AddOutline(t, Palette.Ink, size * 0.04f);
                letters.Add(rt);
                letterBase.Add(p);
            }
        }

        protected override void OnShow()
        {
            nav.Index = 0;
            RefreshSelection();
            for (int i = 0; i < letters.Count; i++)
                UITween.PopIn(letters[i], 0.45f, 0.05f + i * 0.04f);
            UITween.PopIn(tagline, 0.4f, 0.5f);
            UITween.PopIn(buttons[0].Root, 0.4f, 0.6f);
            UITween.PopIn(buttons[1].Root, 0.4f, 0.7f);
            UITween.PopIn(p1Label, 0.4f, 0.8f);
            UITween.PopIn(p2Label, 0.4f, 0.85f);
        }

        void RefreshSelection()
        {
            for (int i = 0; i < buttons.Length; i++) buttons[i].SetSelected(i == nav.Index);
        }

        void Update()
        {
            float t = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            for (int i = 0; i < letters.Count; i++)
            {
                letters[i].anchoredPosition = letterBase[i] + new Vector2(0f, Mathf.Sin(t * 3f + i * 0.5f) * 12f);
                letters[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2.2f + i * 0.8f) * 5f);
            }
            float breathe = 1f + Mathf.Sin(t * 1.7f) * 0.025f;
            logo.localScale = new Vector3(breathe, breathe, 1f);
            logo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f) * 1.5f);

            float halfW = Mathf.Max(960f, rect.rect.width * 0.5f) + 120f;
            float halfH = Mathf.Max(540f, rect.rect.height * 0.5f);
            for (int i = 0; i < drifters.Count; i++)
            {
                var d = drifters[i];
                d.pos += d.vel * dt;
                if (d.pos.x > halfW)
                {
                    d.pos.x = -halfW;
                    d.pos.y = UnityEngine.Random.Range(-halfH * 0.85f, halfH * 0.85f);
                }
                if (d.pos.y > halfH + 100f) d.pos.y = -halfH - 100f;
                float bobV = Mathf.Cos(t * 1.8f + d.phase) * d.bob * 1.8f;
                d.rt.anchoredPosition = d.pos + new Vector2(0f, Mathf.Sin(t * 1.8f + d.phase) * d.bob);
                float ang = Mathf.Atan2(d.vel.y + bobV, d.vel.x) * Mathf.Rad2Deg - 90f;
                d.rt.localRotation = Quaternion.Euler(0f, 0f, ang);
            }

            for (int i = 0; i < buttons.Length; i++) buttons[i].Tick(t);

            if (!InputReady()) return;
            bool moved;
            int confirmed = nav.Poll(out moved);
            if (moved)
            {
                RefreshSelection();
                UITween.Punch(buttons[nav.Index].Root, 0.15f, 0.3f);
            }
            if (confirmed >= 0 && Time.unscaledTime - lastConfirm > 0.5f)
            {
                lastConfirm = Time.unscaledTime;
                UITween.Punch(buttons[confirmed].Root, 0.25f, 0.35f);
                if (confirmed == 0) GameEvents.RaiseRequestStartMatch();
                else GameEvents.RaiseRequestHowToPlay();
            }
        }
    }
}
