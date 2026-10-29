using UnityEngine;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace IslandRocketRumble
{
    /// <summary>Helpers that build uGUI objects in code. All created elements use pivot = center.</summary>
    public static class UIFactory
    {
        public const int UILayer = 5;

        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 TopCenter = new Vector2(0.5f, 1f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        public static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        public static readonly Vector2 MidLeft = new Vector2(0f, 0.5f);
        public static readonly Vector2 MidRight = new Vector2(1f, 0.5f);

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = UILayer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = Center;
            rt.pivot = Center;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(100f, 100f);
            return rt;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Rect(parent, name);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name, float inset = 0f)
        {
            var rt = Rect(parent, name);
            StretchFill(rt, inset);
            return rt;
        }

        public static void StretchFill(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = Center;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Rect(parent, name, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            if (sprite != null && sprite.border.sqrMagnitude > 0f)
            {
                img.type = Image.Type.Sliced;
                img.fillCenter = true;
            }
            else
            {
                img.preserveAspect = sprite != null;
            }
            return img;
        }

        public static Image StretchImg(Transform parent, string name, Sprite sprite, Color color, float inset = 0f)
        {
            var img = Img(parent, name, sprite, color, Center, Vector2.zero, Vector2.zero);
            StretchFill(img.rectTransform, inset);
            return img;
        }

        public static UText Txt(Transform parent, string name, string content, int fontSize, Color color, TextAnchor align,
            Vector2 anchor, Vector2 pos, Vector2 size, bool bold = false)
        {
            var rt = Rect(parent, name, anchor, pos, size);
            var t = rt.gameObject.AddComponent<ThaiText>();
            t.font = UIFonts.Main;
            t.fontSize = fontSize;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color = color;
            t.alignment = align;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.lineSpacing = 1f;
            t.text = content;
            return t;
        }

        /// <summary>Text that wraps inside its box width.</summary>
        public static UText WrapTxt(Transform parent, string name, string content, int fontSize, Color color, TextAnchor align,
            Vector2 anchor, Vector2 pos, Vector2 size, bool bold = false)
        {
            var t = Txt(parent, name, content, fontSize, color, align, anchor, pos, size, bold);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        public static Outline AddOutline(Graphic g, Color c, float distance)
        {
            var o = g.gameObject.AddComponent<Outline>();
            o.effectColor = c;
            o.effectDistance = new Vector2(distance, -distance);
            o.useGraphicAlpha = true;
            return o;
        }

        public static Shadow AddShadow(Graphic g, Color c, Vector2 distance)
        {
            var s = g.gameObject.AddComponent<Shadow>();
            s.effectColor = c;
            s.effectDistance = distance;
            s.useGraphicAlpha = true;
            return s;
        }

        /// <summary>Chunky cute title text: thick ink outline + drop shadow.</summary>
        public static void Chunky(Graphic g, float outline = 5f, float shadow = 7f)
        {
            AddShadow(g, WithAlpha(Palette.Ink, 0.9f), new Vector2(0f, -shadow));
            AddOutline(g, Palette.Ink, outline);
        }

        /// <summary>Sets the visual corner radius (in canvas units) of an image using UISprites.Rounded.</summary>
        public static void SetRadius(Image img, float radius)
        {
            img.pixelsPerUnitMultiplier = UISprites.RoundBorder / Mathf.Max(1f, radius);
        }

        /// <summary>Cream-style panel: ink rounded outline + inner fill (+ optional soft drop shadow). Returns the outer rect.</summary>
        public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color fill,
            out Image fillImg, float radius = 24f, float outline = 4f, bool dropShadow = true)
        {
            var outer = Img(parent, name, UISprites.Rounded, Palette.Ink, anchor, pos, size);
            SetRadius(outer, radius);
            if (dropShadow) AddShadow(outer, WithAlpha(Palette.Ink, 0.22f), new Vector2(0f, -7f));
            fillImg = Img(outer.transform, "Fill", UISprites.Rounded, fill, Center, Vector2.zero, Vector2.zero);
            StretchFill(fillImg.rectTransform, outline);
            SetRadius(fillImg, Mathf.Max(2f, radius - outline));
            return outer.rectTransform;
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color fill,
            float radius = 24f, float outline = 4f, bool dropShadow = true)
            => Panel(parent, name, anchor, pos, size, fill, out _, radius, outline, dropShadow);

        /// <summary>Icon image (white procedural sprite) with an ink outline.</summary>
        public static Image Icon(Transform parent, string name, Sprite sprite, Color tint, Vector2 anchor, Vector2 pos, float size,
            float outline = 2f)
        {
            var img = Img(parent, name, sprite, tint, anchor, pos, new Vector2(size, size));
            img.preserveAspect = true;
            if (outline > 0f) AddOutline(img, Palette.Ink, outline);
            return img;
        }

        public static CanvasGroup Group(Component c)
        {
            var g = c.GetComponent<CanvasGroup>();
            if (g == null) g = c.gameObject.AddComponent<CanvasGroup>();
            g.blocksRaycasts = false;
            g.interactable = false;
            return g;
        }
    }
}
