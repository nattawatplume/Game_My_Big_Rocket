using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace IslandRocketRumble
{
    /// <summary>Thai/English strings shown by the UI.</summary>
    public static class UIText
    {
        public static string PartThai(PartType t)
        {
            switch (t)
            {
                case PartType.Gear: return "เกียร์";
                case PartType.Nut: return "น็อต";
                case PartType.Fuel: return "เชื้อเพลิง";
                case PartType.Chip: return "ชิป";
                default: return "อัญมณี";
            }
        }

        public static string TierThai(PartTier t)
        {
            switch (t)
            {
                case PartTier.Scrap: return "เศษเหล็ก";
                case PartTier.Copper: return "ทองแดง";
                case PartTier.Silver: return "เงิน";
                case PartTier.Gold: return "ทอง";
                default: return "คริสตัล";
            }
        }

        public static string TierEn(PartTier t)
        {
            switch (t)
            {
                case PartTier.Scrap: return "SCRAP";
                case PartTier.Copper: return "COPPER";
                case PartTier.Silver: return "SILVER";
                case PartTier.Gold: return "GOLD";
                default: return "CRYSTAL";
            }
        }

        public static string RageThai(int level)
        {
            switch (level)
            {
                case 0: return "ใจเย็น";
                case 1: return "หงุดหงิด";
                case 2: return "โกรธจัด";
                default: return "หัวร้อนไฟลุก!";
            }
        }

        public static string PlayerThai(PlayerId id) => id == PlayerId.P1 ? "ปิ๊บ" : "โมโม่";
        public static string PlayerTag(PlayerId id) => id == PlayerId.P1 ? "P1" : "P2";
        public static string ConfirmKey(PlayerId id) => id == PlayerId.P1 ? "SPACE" : "ENTER";

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
        public static string Colored(string s, Color c) => "<color=" + Hex(c) + ">" + s + "</color>";

        /// <summary>Default rocket data from the GDD, used when MatchContext.recipes are missing.</summary>
        public static readonly string[] DefaultRocketNames = { "Firecracker", "Copper Dart", "Silver Spear", "Golden Giant", "Mega Overkill" };
        public static readonly int[] DefaultRocketDamage = { 30, 60, 110, 180, 300 };

        public static Color Darker(Color c, float k = 0.75f) => new Color(c.r * k, c.g * k, c.b * k, c.a);
    }

    /// <summary>World → canvas helpers for world-anchored UI.</summary>
    public static class UIWorld
    {
        static readonly Dictionary<Transform, Transform> headCache = new Dictionary<Transform, Transform>();

        public static bool TryGetHead(PlayerId id, out Vector3 world)
        {
            world = Vector3.zero;
            var ctx = MatchContext.Instance;
            if (ctx == null) return false;
            var p = ctx.Player(id);
            if (p == null) return false;

            Transform head;
            if (!headCache.TryGetValue(p, out head) || head == null)
            {
                head = FindDeep(p, "HeadAnchor");
                headCache[p] = head;
            }
            world = head != null ? head.position : p.position + Vector3.up * 1.4f;
            return true;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>Converts a world point to the local space of <paramref name="space"/> (a full-screen rect, centered pivot).</summary>
        public static bool WorldToCanvas(Vector3 world, RectTransform space, out Vector2 local)
        {
            local = Vector2.zero;
            var cam = Camera.main;
            if (cam == null || space == null) return false;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(space, new Vector2(sp.x, sp.y), null, out local);
        }
    }

    /// <summary>Menu selection driven by both players' keys (up/left = previous, down/right = next, confirm).</summary>
    public class UIMenuNav
    {
        public int Index;
        readonly int count;

        public UIMenuNav(int count) { this.count = Mathf.Max(1, count); }

        /// <summary>Returns the confirmed index, or -1.</summary>
        public int Poll(out bool moved)
        {
            moved = false;
            for (int i = 0; i < 2; i++)
            {
                var id = (PlayerId)i;
                if (PlayerInput2P.UpPressed(id) || PlayerInput2P.LeftPressed(id))
                {
                    Index = (Index - 1 + count) % count;
                    moved = true;
                }
                if (PlayerInput2P.DownPressed(id) || PlayerInput2P.RightPressed(id))
                {
                    Index = (Index + 1) % count;
                    moved = true;
                }
            }
            for (int i = 0; i < 2; i++)
                if (PlayerInput2P.ConfirmPressed((PlayerId)i)) return Index;
            return -1;
        }
    }

    /// <summary>Pastel pill button with an orange selection ring and a bouncy selected state.</summary>
    public class UIPillButton
    {
        public readonly RectTransform Root;
        public readonly RectTransform Body;
        public readonly Image Fill;
        public readonly UText Label;
        readonly Image ring;
        readonly Color fillColor;
        bool selected;

        public UIPillButton(Transform parent, string label, Color fill, Color textColor, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            fillColor = fill;
            Root = UIFactory.Rect(parent, "Btn_" + label, anchor, pos, size);

            ring = UIFactory.Img(Root, "Ring", UISprites.Rounded, Palette.HelmetOrange, UIFactory.Center, Vector2.zero,
                size + new Vector2(22f, 22f));
            UIFactory.SetRadius(ring, (size.y + 22f) * 0.5f);

            Image fillImg;
            Body = UIFactory.Panel(Root, "Body", UIFactory.Center, Vector2.zero, size, fill, out fillImg, size.y * 0.5f, 5f, true);
            Fill = fillImg;

            // glossy highlight
            var gloss = UIFactory.Img(Body, "Gloss", UISprites.Rounded, new Color(1f, 1f, 1f, 0.35f), UIFactory.TopCenter,
                new Vector2(0f, -size.y * 0.24f), new Vector2(size.x - size.y * 0.8f, size.y * 0.22f));
            UIFactory.SetRadius(gloss, size.y * 0.11f);

            Label = UIFactory.Txt(Body, "Label", label, Mathf.RoundToInt(size.y * 0.42f), textColor, TextAnchor.MiddleCenter,
                UIFactory.Center, new Vector2(0f, 2f), size, true);
            UIFactory.AddShadow(Label, UIFactory.WithAlpha(Palette.Ink, 0.35f), new Vector2(0f, -3f));

            SetSelected(false);
        }

        public void SetSelected(bool s)
        {
            selected = s;
            ring.gameObject.SetActive(s);
            Fill.color = s ? fillColor : Color.Lerp(fillColor, Palette.Cream, 0.3f);
        }

        public void Tick(float time)
        {
            float target = selected ? 1.07f + Mathf.Sin(time * 6f) * 0.03f : 0.93f;
            float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            Body.localScale = Vector3.Lerp(Body.localScale, new Vector3(target, target, 1f), k);
            ring.rectTransform.localScale = Body.localScale;
        }
    }

    /// <summary>Base for full-screen UI screens built in code.</summary>
    public abstract class UIScreen : MonoBehaviour
    {
        protected UIRoot root;
        protected RectTransform rect;
        protected CanvasGroup group;
        protected float shownAt;

        public bool IsShown { get; private set; }

        public void Init(UIRoot owner)
        {
            root = owner;
            rect = (RectTransform)transform;
            group = UIFactory.Group(this);
            Build();
            IsShown = false;
            gameObject.SetActive(false);
        }

        protected abstract void Build();
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        public void Show()
        {
            if (IsShown) return;
            IsShown = true;
            gameObject.SetActive(true);
            shownAt = Time.unscaledTime;
            group.alpha = 0f;
            UITween.Fade(group, 1f, 0.2f);
            OnShow();
        }

        public void Hide()
        {
            if (!IsShown) return;
            IsShown = false;
            OnHide();
            gameObject.SetActive(false);
        }

        /// <summary>True once the screen has been visible long enough to accept input (avoids the key that opened it).</summary>
        protected bool InputReady(float delay = 0.3f) => Time.unscaledTime - shownAt >= delay;
    }
}
