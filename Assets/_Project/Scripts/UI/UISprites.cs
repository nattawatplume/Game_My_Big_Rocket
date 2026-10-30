using System.Collections.Generic;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Procedurally generated, cached UI sprites: rounded rects (9-slice), rings, circles,
    /// and small white icons (parts, rockets, stars...) meant to be tinted via Image.color.
    /// </summary>
    public static class UISprites
    {
        /// <summary>Corner radius (px) of the base rounded sprite.</summary>
        public const int RoundRadius = 32;
        /// <summary>9-slice border of the base rounded sprite (radius + 1px margin).</summary>
        public const float RoundBorder = RoundRadius + 1f;

        const int IconSize = 96;
        const int Sub = 4; // supersampling per axis for icons

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        delegate bool Shape(float x, float y);

        // ---------------------------------------------------------------- public sprites

        /// <summary>White rounded rectangle, 9-sliced. Use UIFactory.SetRadius to change the corner size.</summary>
        public static Sprite Rounded
        {
            get
            {
                const string key = "rounded";
                var s = Get(key);
                if (s != null) return s;
                s = BuildRounded(key, RoundRadius, -1f);
                cache[key] = s;
                return s;
            }
        }

        /// <summary>Hollow rounded rectangle (frame), 9-sliced, ring thickness 8px of the 32px radius.</summary>
        public static Sprite RoundedRing
        {
            get
            {
                const string key = "roundedRing";
                var s = Get(key);
                if (s != null) return s;
                s = BuildRounded(key, RoundRadius, 8f);
                cache[key] = s;
                return s;
            }
        }

        public static Sprite Circle => GetIcon("circle", (x, y) => x * x + y * y < 0.96f * 0.96f);

        public static Sprite CircleRing => GetIcon("circleRing", (x, y) =>
        {
            float r2 = x * x + y * y;
            return r2 < 0.96f * 0.96f && r2 > 0.7f * 0.7f;
        });

        public static Sprite Star => GetIcon("star", (x, y) => InPoly(x, y, StarPoly(5, 0.96f, 0.44f)));

        public static Sprite Sparkle => GetIcon("sparkle", (x, y) => InPoly(x, y, StarPoly(4, 0.96f, 0.24f)));

        public static Sprite Cross => GetIcon("cross", (x, y) =>
        {
            const float k = 0.70710678f;
            float u = (x + y) * k;
            float v = (x - y) * k;
            return (Mathf.Abs(u) < 0.78f && Mathf.Abs(v) < 0.2f) || (Mathf.Abs(v) < 0.78f && Mathf.Abs(u) < 0.2f);
        });

        /// <summary>Triangle pointing down (speech-bubble tail).</summary>
        public static Sprite Heart => GetIcon("heart", (x, y) =>
        {
            float a = x * 1.15f, b = y * 1.15f + 0.1f;
            float k = a * a + b * b - 1f;
            return k * k * k - a * a * b * b * b < 0f;
        });

        public static Sprite TriangleDown => GetIcon("triDown", (x, y) => InPoly(x, y, triDownPoly));

        static readonly Vector2[] triDownPoly = { new Vector2(-0.95f, 0.95f), new Vector2(0.95f, 0.95f), new Vector2(0f, -0.95f) };

        public static Sprite Part(PartType type)
        {
            switch (type)
            {
                case PartType.Gear: return GetIcon("part_gear", GearShape);
                case PartType.Nut: return GetIcon("part_nut", NutShape);
                case PartType.Fuel: return GetIcon("part_fuel", FuelShape);
                case PartType.Chip: return GetIcon("part_chip", ChipShape);
                default: return GetIcon("part_gem", GemShape);
            }
        }

        /// <summary>Rocket silhouette for tier 1..5 (escalating size/complexity). Nose points up.</summary>
        public static Sprite Rocket(int tier)
        {
            tier = Mathf.Clamp(tier, 1, 5);
            switch (tier)
            {
                case 1: return GetIcon("rocket1", (x, y) => RocketShape(x, y, 1));
                case 2: return GetIcon("rocket2", (x, y) => RocketShape(x, y, 2));
                case 3: return GetIcon("rocket3", (x, y) => RocketShape(x, y, 3));
                case 4: return GetIcon("rocket4", (x, y) => RocketShape(x, y, 4));
                default: return GetIcon("rocket5", (x, y) => RocketShape(x, y, 5));
            }
        }

        public static Sprite Rocket(PartTier tier) => Rocket((int)tier);

        // ---------------------------------------------------------------- builders

        static Sprite Get(string key)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            return null;
        }

        static Texture2D NewTex(int w, int h, string name)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = "UI_" + name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }

        static float SdRoundBox(float px, float py, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(px) - hx + r;
            float qy = Mathf.Abs(py) - hy + r;
            float ox = Mathf.Max(qx, 0f);
            float oy = Mathf.Max(qy, 0f);
            float outside = Mathf.Sqrt(ox * ox + oy * oy);
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outside + inside - r;
        }

        /// <param name="ringThickness">negative = filled, otherwise a hollow frame of that thickness.</param>
        static Sprite BuildRounded(string key, int radius, float ringThickness)
        {
            int size = radius * 2 + 10;
            var tex = NewTex(size, size, key);
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - half;
                    float dy = y + 0.5f - half;
                    float d = SdRoundBox(dx, dy, half - 1f, half - 1f, radius);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (ringThickness > 0f) a *= Mathf.Clamp01(0.5f + d + ringThickness);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            float b = radius + 1f;
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            sprite.name = key;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        static Sprite GetIcon(string key, Shape shape)
        {
            var s = Get(key);
            if (s != null) return s;

            int size = IconSize;
            var tex = NewTex(size, size, key);
            var px = new Color32[size * size];
            float inv = 1f / (Sub * Sub);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < Sub; sy++)
                    {
                        float fy = ((y + (sy + 0.5f) / Sub) / size) * 2f - 1f;
                        for (int sx = 0; sx < Sub; sx++)
                        {
                            float fx = ((x + (sx + 0.5f) / Sub) / size) * 2f - 1f;
                            if (shape(fx, fy)) hits++;
                        }
                    }
                    float a = hits * inv;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            s.name = key;
            s.hideFlags = HideFlags.DontSave;
            cache[key] = s;
            return s;
        }

        // ---------------------------------------------------------------- shape helpers

        static bool InPoly(float x, float y, Vector2[] p)
        {
            bool c = false;
            for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
            {
                if (((p[i].y > y) != (p[j].y > y)) &&
                    (x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x))
                    c = !c;
            }
            return c;
        }

        static readonly Dictionary<int, Vector2[]> starPolys = new Dictionary<int, Vector2[]>();

        static Vector2[] StarPoly(int points, float outer, float inner)
        {
            int key = points * 1000 + Mathf.RoundToInt(inner * 100f);
            if (starPolys.TryGetValue(key, out var poly)) return poly;
            poly = new Vector2[points * 2];
            for (int i = 0; i < points * 2; i++)
            {
                float ang = Mathf.PI * 0.5f + i * Mathf.PI / points;
                float r = (i % 2 == 0) ? outer : inner;
                poly[i] = new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
            }
            starPolys[key] = poly;
            return poly;
        }

        static bool InCircle(float x, float y, float cx, float cy, float r)
        {
            float dx = x - cx, dy = y - cy;
            return dx * dx + dy * dy < r * r;
        }

        static bool InRect(float x, float y, float cx, float cy, float hw, float hh)
            => Mathf.Abs(x - cx) < hw && Mathf.Abs(y - cy) < hh;

        static bool InRoundRect(float x, float y, float cx, float cy, float hw, float hh, float r)
            => SdRoundBox(x - cx, y - cy, hw, hh, Mathf.Min(r, Mathf.Min(hw, hh))) < 0f;

        // ---------------------------------------------------------------- part icons

        static bool GearShape(float x, float y)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            if (r < 0.28f) return false;          // hole
            if (r < 0.66f) return true;           // body
            if (r > 0.94f) return false;
            float a = Mathf.Atan2(y, x);
            float f = (a / (Mathf.PI * 2f)) * 8f;
            f -= Mathf.Floor(f);
            return f > 0.22f && f < 0.78f;        // 8 teeth
        }

        static bool NutShape(float x, float y)
        {
            if (InCircle(x, y, 0f, 0f, 0.36f)) return false;
            const float apothem = 0.82f;
            for (int i = 0; i < 6; i++)
            {
                float ang = i * Mathf.PI / 3f;
                if (x * Mathf.Cos(ang) + y * Mathf.Sin(ang) > apothem) return false;
            }
            return true;
        }

        static readonly Vector2[] dropPoly = { new Vector2(-0.18f, -0.22f), new Vector2(0.18f, -0.22f), new Vector2(0f, 0.12f) };

        static bool FuelShape(float x, float y)
        {
            bool can = InRoundRect(x, y, 0f, -0.15f, 0.58f, 0.72f, 0.22f);
            bool cap = InRoundRect(x, y, 0.22f, 0.7f, 0.18f, 0.18f, 0.06f);
            bool handle = InRoundRect(x, y, -0.22f, 0.62f, 0.26f, 0.08f, 0.06f);
            if (!(can || cap || handle)) return false;
            // drop cut-out in the middle
            bool drop = InCircle(x, y, 0f, -0.3f, 0.2f) || InPoly(x, y, dropPoly);
            return !drop;
        }

        static bool ChipShape(float x, float y)
        {
            bool body = InRoundRect(x, y, 0f, 0f, 0.58f, 0.58f, 0.1f);
            bool pins = false;
            for (int i = -1; i <= 1; i++)
            {
                float c = i * 0.36f;
                if (InRect(x, y, -0.76f, c, 0.2f, 0.07f) || InRect(x, y, 0.76f, c, 0.2f, 0.07f) ||
                    InRect(x, y, c, -0.76f, 0.07f, 0.2f) || InRect(x, y, c, 0.76f, 0.07f, 0.2f))
                    pins = true;
            }
            if (!(body || pins)) return false;
            // inner square frame cut-out
            bool frame = InRect(x, y, 0f, 0f, 0.32f, 0.32f) && !InRect(x, y, 0f, 0f, 0.2f, 0.2f);
            return !frame;
        }

        static readonly Vector2[] gemPoly =
        {
            new Vector2(-0.92f, 0.3f), new Vector2(-0.5f, 0.78f), new Vector2(0.5f, 0.78f),
            new Vector2(0.92f, 0.3f), new Vector2(0f, -0.92f)
        };

        static bool GemShape(float x, float y)
        {
            if (!InPoly(x, y, gemPoly)) return false;
            if (y > 0.24f && y < 0.34f) return false; // crown facet line
            return true;
        }

        // ---------------------------------------------------------------- rocket icons

        static bool Body(float x, float y, float cx, float w, float yb, float yt, float nose)
        {
            if (InRoundRect(x, y, cx, (yb + yt) * 0.5f, w, (yt - yb) * 0.5f, w * 0.45f)) return true;
            if (y >= yt - 0.02f && y <= yt + nose)
            {
                float t = Mathf.Clamp01((y - yt) / nose);
                return Mathf.Abs(x - cx) < w * (1f - t * t);
            }
            return false;
        }

        static bool Fins(float x, float y, float cx, float w, float yb, float finW, float finH, float finD)
        {
            if (y > yb + finH || y < yb - finD) return false;
            var left = new[]
            {
                new Vector2(cx - w + 0.02f, yb + finH), new Vector2(cx - w - finW, yb - finD),
                new Vector2(cx - w + 0.02f, yb - finD * 0.25f)
            };
            var right = new[]
            {
                new Vector2(cx + w - 0.02f, yb + finH), new Vector2(cx + w + finW, yb - finD),
                new Vector2(cx + w - 0.02f, yb - finD * 0.25f)
            };
            return InPoly(x, y, left) || InPoly(x, y, right);
        }

        static bool Nozzle(float x, float y, float cx, float w, float yb)
            => InRect(x, y, cx, yb - 0.05f, w * 0.55f, 0.07f);

        static bool RocketShape(float x, float y, int tier)
        {
            bool inside;
            switch (tier)
            {
                case 1:
                {
                    const float w = 0.2f, yb = -0.35f, yt = 0.12f;
                    inside = Body(x, y, 0f, w, yb, yt, 0.3f) || Fins(x, y, 0f, w, yb, 0.16f, 0.2f, 0.16f) || Nozzle(x, y, 0f, w, yb);
                    if (inside && InCircle(x, y, 0f, yt - 0.1f, 0.085f)) inside = false; // window
                    // little fuse spark on top
                    if (!inside) inside = InPoly(x, y, StarPolyOffset(0f, 0.6f, 0.14f));
                    return inside;
                }
                case 2:
                {
                    const float w = 0.15f, yb = -0.62f, yt = 0.4f;
                    inside = Body(x, y, 0f, w, yb, yt, 0.48f) || Fins(x, y, 0f, w, yb, 0.22f, 0.32f, 0.22f) || Nozzle(x, y, 0f, w, yb);
                    if (inside && InCircle(x, y, 0f, yt - 0.14f, 0.075f)) inside = false;
                    return inside;
                }
                case 3:
                {
                    const float w = 0.19f, yb = -0.66f, yt = 0.34f;
                    inside = Body(x, y, 0f, w, yb, yt, 0.52f) || Fins(x, y, 0f, w, yb, 0.1f, 0.2f, 0.16f) || Nozzle(x, y, 0f, w, yb)
                             || Body(x, y, -0.4f, 0.1f, -0.78f, -0.14f, 0.2f) || Body(x, y, 0.4f, 0.1f, -0.78f, -0.14f, 0.2f)
                             || InRect(x, y, 0f, -0.4f, 0.36f, 0.05f); // struts
                    if (inside && InCircle(x, y, 0f, yt - 0.16f, 0.09f)) inside = false;
                    return inside;
                }
                case 4:
                {
                    const float w = 0.3f, yb = -0.64f, yt = 0.28f;
                    inside = Body(x, y, 0f, w, yb, yt, 0.62f) || Fins(x, y, 0f, w, yb, 0.34f, 0.42f, 0.26f) || Nozzle(x, y, 0f, w, yb);
                    if (inside && InCircle(x, y, 0f, yt - 0.06f, 0.13f)) inside = false;  // big window
                    if (inside && Mathf.Abs(y + 0.26f) < 0.035f && Mathf.Abs(x) < w) inside = false; // gold stripe
                    return inside;
                }
                default:
                {
                    const float w = 0.26f, yb = -0.7f, yt = 0.34f;
                    inside = Body(x, y, 0f, w, yb, yt, 0.56f) || Fins(x, y, 0f, w, yb, 0.08f, 0.2f, 0.14f) || Nozzle(x, y, 0f, w, yb)
                             || Body(x, y, -0.44f, 0.11f, -0.82f, -0.06f, 0.2f) || Body(x, y, 0.44f, 0.11f, -0.82f, -0.06f, 0.2f)
                             || Body(x, y, -0.72f, 0.085f, -0.88f, -0.38f, 0.15f) || Body(x, y, 0.72f, 0.085f, -0.88f, -0.38f, 0.15f)
                             || InRect(x, y, 0f, -0.46f, 0.66f, 0.045f);
                    // sunglasses cut-out
                    if (inside && (InCircle(x, y, -0.1f, yt - 0.06f, 0.085f) || InCircle(x, y, 0.1f, yt - 0.06f, 0.085f) ||
                                   InRect(x, y, 0f, yt - 0.04f, 0.1f, 0.022f)))
                        inside = false;
                    return inside;
                }
            }
        }

        static readonly Dictionary<int, Vector2[]> offsetStars = new Dictionary<int, Vector2[]>();

        static Vector2[] StarPolyOffset(float cx, float cy, float r)
        {
            int key = Mathf.RoundToInt(cx * 100f) * 100000 + Mathf.RoundToInt(cy * 100f) * 100 + Mathf.RoundToInt(r * 100f);
            if (offsetStars.TryGetValue(key, out var p)) return p;
            var baseP = StarPoly(4, 1f, 0.3f);
            p = new Vector2[baseP.Length];
            for (int i = 0; i < p.Length; i++) p[i] = new Vector2(cx + baseP[i].x * r, cy + baseP[i].y * r);
            offsetStars[key] = p;
            return p;
        }
    }
}
