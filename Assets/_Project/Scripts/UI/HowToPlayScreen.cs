using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace IslandRocketRumble
{
    /// <summary>How to play: keycaps for both players, the 4 steps, grey-slot rule, rocket tiers, red-circle warning.</summary>
    public class HowToPlayScreen : UIScreen
    {
        RectTransform card;
        RectTransform startHint;
        readonly List<RectTransform> popTargets = new List<RectTransform>();

        // keycap faces for live press feedback: [player][0=up,1=left,2=down,3=right,4=confirm]
        readonly RectTransform[,] keys = new RectTransform[2, 5];

        readonly UText[] tierNames = new UText[5];
        readonly UText[] tierDmg = new UText[5];
        UText warnText;
        float lastConfirm = -10f;

        protected override void Build()
        {
            UIFactory.StretchImg(rect, "Bg", null, UIFactory.WithAlpha(Palette.Sea, 0.94f));

            Image cardFill;
            card = UIFactory.Panel(rect, "Card", UIFactory.Center, new Vector2(0f, -20f), new Vector2(1660f, 900f), Palette.Cream,
                out cardFill, 44f, 6f, true);

            // Title ribbon
            var ribbon = UIFactory.Panel(card, "Ribbon", UIFactory.TopCenter, new Vector2(0f, 6f), new Vector2(680f, 104f),
                Palette.HelmetYellow, 52f, 6f, true);
            var title = UIFactory.Txt(ribbon, "Title", "HOW TO PLAY · วิธีเล่น", 56, Color.white, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 2f), new Vector2(660f, 100f), true);
            UIFactory.Chunky(title, 3f, 4f);
            popTargets.Add(ribbon);

            BuildPlayerKeys();
            BuildSteps();
            BuildRules();

            // Start hint
            Image hintFill;
            startHint = UIFactory.Panel(card, "StartHint", UIFactory.BottomCenter, new Vector2(0f, 0f), new Vector2(560f, 84f),
                Palette.CraftRing, out hintFill, 42f, 5f, true);
            var hint = UIFactory.Txt(startHint, "Label", "SPACE / ENTER เพื่อเริ่ม", 36, Color.white, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 2f), new Vector2(540f, 80f), true);
            UIFactory.AddOutline(hint, UIFactory.WithAlpha(Palette.Ink, 0.7f), 2f);
        }

        // ------------------------------------------------------------------ keycaps

        void BuildPlayerKeys()
        {
            // P1 block
            PlayerTag(PlayerId.P1, "P1 · ปิ๊บ", new Vector2(-560f, 300f));
            keys[0, 0] = Keycap("W", new Vector2(-650f, 215f), new Vector2(66f, 66f));
            keys[0, 1] = Keycap("A", new Vector2(-724f, 141f), new Vector2(66f, 66f));
            keys[0, 2] = Keycap("S", new Vector2(-650f, 141f), new Vector2(66f, 66f));
            keys[0, 3] = Keycap("D", new Vector2(-576f, 141f), new Vector2(66f, 66f));
            keys[0, 4] = Keycap("SPACE", new Vector2(-420f, 141f), new Vector2(200f, 66f));
            SmallLabel("เดิน / เลื่อนกรอบ", new Vector2(-650f, 84f));
            SmallLabel("คราฟ + ยิง", new Vector2(-420f, 84f));

            // P2 block
            PlayerTag(PlayerId.P2, "P2 · โมโม่", new Vector2(-560f, 10f));
            keys[1, 0] = Keycap("↑", new Vector2(-650f, -75f), new Vector2(66f, 66f));
            keys[1, 1] = Keycap("←", new Vector2(-724f, -149f), new Vector2(66f, 66f));
            keys[1, 2] = Keycap("↓", new Vector2(-650f, -149f), new Vector2(66f, 66f));
            keys[1, 3] = Keycap("→", new Vector2(-576f, -149f), new Vector2(66f, 66f));
            keys[1, 4] = Keycap("ENTER", new Vector2(-420f, -149f), new Vector2(200f, 66f));
            SmallLabel("เดิน / เลื่อนกรอบ", new Vector2(-650f, -206f));
            SmallLabel("คราฟ + ยิง", new Vector2(-420f, -206f));
        }

        void PlayerTag(PlayerId id, string text, Vector2 pos)
        {
            var tag = UIFactory.Panel(card, "Tag_" + UIText.PlayerTag(id), UIFactory.Center, pos, new Vector2(300f, 60f),
                Palette.Player(id), 30f, 4f, false);
            var t = UIFactory.Txt(tag, "Label", text, 32, Color.white, TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 1f),
                new Vector2(290f, 56f), true);
            UIFactory.AddOutline(t, UIFactory.WithAlpha(Palette.Ink, 0.7f), 2f);
            popTargets.Add(tag);
        }

        RectTransform Keycap(string label, Vector2 pos, Vector2 size)
        {
            var keyRoot = UIFactory.Rect(card, "Key_" + label, UIFactory.Center, pos, size);
            var depth = UIFactory.Img(keyRoot, "Depth", UISprites.Rounded, Palette.Ink, UIFactory.Center, new Vector2(0f, -6f), size);
            UIFactory.SetRadius(depth, 14f);
            var face = UIFactory.Panel(keyRoot, "Face", UIFactory.Center, Vector2.zero, size, Color.white, 14f, 4f, false);
            int fs = label.Length > 1 ? 26 : 34;
            UIFactory.Txt(face, "L", label, fs, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 1f), size, true);
            popTargets.Add(keyRoot);
            return face;
        }

        void SmallLabel(string text, Vector2 pos)
        {
            UIFactory.Txt(card, "Lbl", text, 22, UIFactory.WithAlpha(Palette.Ink, 0.75f), TextAnchor.MiddleCenter, UIFactory.Center, pos,
                new Vector2(260f, 30f));
        }

        // ------------------------------------------------------------------ steps

        void BuildSteps()
        {
            string[] steps =
            {
                "เดินเข้าวงคราฟ → เมนูเด้งเหนือหัว",
                "A/D หรือ ←/→ = เลื่อนกรอบเลือกจรวด\nW/S หรือ ↑/↓ = สลับเป้า ศัตรู/หุ่นยนต์",
                "เลื่อนไปช่อง ✕ = ปิดเมนู",
                "SPACE / ENTER = คราฟ + ยิงทันที",
            };
            float[] ys = { 300f, 205f, 110f, 30f };
            for (int i = 0; i < steps.Length; i++)
            {
                var row = UIFactory.Rect(card, "Step" + (i + 1), UIFactory.Center, new Vector2(300f, ys[i]), new Vector2(980f, 84f));
                var num = UIFactory.Img(row, "Num", UISprites.Circle, Palette.CraftRing, UIFactory.MidLeft, new Vector2(34f, 0f),
                    new Vector2(62f, 62f));
                UIFactory.AddOutline(num, Palette.Ink, 3f);
                var nt = UIFactory.Txt(num.transform, "N", (i + 1).ToString(), 36, Color.white, TextAnchor.MiddleCenter, UIFactory.Center,
                    new Vector2(0f, 1f), new Vector2(62f, 62f), true);
                UIFactory.AddShadow(nt, UIFactory.WithAlpha(Palette.Ink, 0.5f), new Vector2(0f, -2f));
                UIFactory.WrapTxt(row, "Text", steps[i], 29, Palette.Ink, TextAnchor.MiddleLeft, UIFactory.MidLeft, new Vector2(80f + 445f, 0f),
                    new Vector2(890f, 84f));
                popTargets.Add(row);
            }
        }

        // ------------------------------------------------------------------ rules / tiers / warning

        void BuildRules()
        {
            // "ของไม่พอ = ช่องเทา" with an affordable and a greyed sample slot
            var row = UIFactory.Rect(card, "GreyRule", UIFactory.Center, new Vector2(300f, -65f), new Vector2(980f, 80f));
            SampleSlot(row, new Vector2(34f, 0f), true);
            SampleSlot(row, new Vector2(110f, 0f), false);
            UIFactory.Txt(row, "Text", "ของไม่พอ = ช่องเทา  (กดคราฟไม่ได้)", 29, Palette.Ink, TextAnchor.MiddleLeft, UIFactory.MidLeft,
                new Vector2(160f + 400f, 0f), new Vector2(800f, 60f));
            popTargets.Add(row);

            // Rocket tier strip
            var strip = UIFactory.Panel(card, "TierStrip", UIFactory.Center, new Vector2(300f, -230f), new Vector2(980f, 190f),
                Color.white, 26f, 4f, false);
            for (int i = 0; i < 5; i++)
            {
                var tier = (PartTier)(i + 1);
                float x = -392f + i * 196f;
                var icon = UIFactory.Icon(strip, "Rocket" + (i + 1), UISprites.Rocket(i + 1), Palette.Tier(tier), UIFactory.Center,
                    new Vector2(x, 34f), 60f + i * 9f, 3f);
                icon.name = "Rocket" + (i + 1);
                var tl = UIFactory.Txt(strip, "T" + (i + 1), "T" + (i + 1) + " " + UIText.TierThai(tier), 22, UIText.Darker(Palette.Tier(tier), 0.7f),
                    TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(x, -38f), new Vector2(190f, 28f), true);
                tl.name = "Tier" + (i + 1);
                tierNames[i] = UIFactory.Txt(strip, "Name" + (i + 1), UIText.DefaultRocketNames[i], 19, Palette.Ink, TextAnchor.MiddleCenter,
                    UIFactory.Center, new Vector2(x, -62f), new Vector2(190f, 26f));
                tierDmg[i] = UIFactory.Txt(strip, "Dmg" + (i + 1), "DMG " + UIText.DefaultRocketDamage[i], 19, Palette.Danger,
                    TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(x, -84f), new Vector2(190f, 26f), true);
                if (i < 4)
                    UIFactory.Txt(strip, "Arrow", "›", 40, UIFactory.WithAlpha(Palette.Ink, 0.4f), TextAnchor.MiddleCenter, UIFactory.Center,
                        new Vector2(x + 98f, 30f), new Vector2(30f, 50f), true);
            }
            popTargets.Add(strip);

            // Red circle warning (left column bottom)
            var warn = UIFactory.Panel(card, "Warn", UIFactory.Center, new Vector2(-560f, -320f), new Vector2(480f, 130f),
                UIFactory.WithAlpha(Palette.Danger, 0.12f), 30f, 4f, false);
            var ring = UIFactory.Img(warn, "Ring", UISprites.CircleRing, Palette.Danger, UIFactory.MidLeft, new Vector2(66f, 0f),
                new Vector2(96f, 96f));
            UIFactory.Icon(ring.transform, "Gem", UISprites.Part(PartType.Gem), Palette.Tier(PartTier.Crystal), UIFactory.Center,
                Vector2.zero, 46f, 2f);
            warnText = UIFactory.WrapTxt(warn, "Text", "หลบวงแดง!\nโดนอัญมณี = สตั้น 2.5 วิ", 28, Palette.Danger, TextAnchor.MiddleLeft,
                UIFactory.MidLeft, new Vector2(128f + 166f, 0f), new Vector2(332f, 120f), true);
            popTargets.Add(warn);
        }

        void SampleSlot(Transform parent, Vector2 pos, bool affordable)
        {
            Image fill;
            var slot = UIFactory.Panel(parent, affordable ? "SlotOk" : "SlotGrey", UIFactory.MidLeft, pos, new Vector2(66f, 66f),
                affordable ? Color.white : new Color(0.78f, 0.78f, 0.8f), out fill, 14f, 3f, false);
            var col = affordable ? Palette.Tier(PartTier.Silver) : new Color(0.6f, 0.6f, 0.62f);
            var icon = UIFactory.Icon(slot, "Icon", UISprites.Rocket(3), col, UIFactory.Center, Vector2.zero, 52f, affordable ? 2f : 0f);
            if (!affordable) icon.color = new Color(col.r, col.g, col.b, 0.7f);
        }

        // ------------------------------------------------------------------ lifecycle

        protected override void OnShow()
        {
            for (int i = 0; i < 5; i++)
            {
                var r = root.Recipe(i);
                if (r != null)
                {
                    tierNames[i].text = r.displayName;
                    tierDmg[i].text = "DMG " + r.damage;
                }
            }
            warnText.text = "หลบวงแดง!\nโดนอัญมณี = สตั้น " + root.StunDuration.ToString("0.#") + " วิ";

            UITween.PopIn(card, 0.4f, 0f);
            for (int i = 0; i < popTargets.Count; i++) UITween.PopIn(popTargets[i], 0.35f, 0.12f + i * 0.025f);
            UITween.PopIn(startHint, 0.4f, 0.6f);
        }

        void Update()
        {
            float t = Time.unscaledTime;
            float s = 1f + Mathf.Sin(t * 5f) * 0.04f;
            // pulse the start hint only when no tween is driving it
            if (t - shownAt > 1.1f) startHint.localScale = new Vector3(s, s, 1f);

            for (int i = 0; i < 2; i++)
            {
                var id = (PlayerId)i;
                if (PlayerInput2P.UpPressed(id)) UITween.Punch(keys[i, 0], 0.25f, 0.3f);
                if (PlayerInput2P.LeftPressed(id)) UITween.Punch(keys[i, 1], 0.25f, 0.3f);
                if (PlayerInput2P.DownPressed(id)) UITween.Punch(keys[i, 2], 0.25f, 0.3f);
                if (PlayerInput2P.RightPressed(id)) UITween.Punch(keys[i, 3], 0.25f, 0.3f);
            }

            if (!InputReady()) return;
            for (int i = 0; i < 2; i++)
            {
                var id = (PlayerId)i;
                if (PlayerInput2P.ConfirmPressed(id) && Time.unscaledTime - lastConfirm > 0.5f)
                {
                    lastConfirm = Time.unscaledTime;
                    UITween.Punch(keys[i, 4], 0.3f, 0.3f);
                    UITween.Punch(startHint, 0.2f, 0.3f);
                    GameEvents.RaiseRequestStartMatch();
                    break;
                }
            }
        }
    }
}
