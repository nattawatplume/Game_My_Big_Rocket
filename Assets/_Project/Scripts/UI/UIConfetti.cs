using UnityEngine;
using UnityEngine.UI;

namespace IslandRocketRumble
{
    /// <summary>Simple UI confetti: colored rounded rects falling, swaying and spinning.</summary>
    public class UIConfetti : MonoBehaviour
    {
        class Piece
        {
            public RectTransform rt;
            public Vector2 pos;
            public Vector2 vel;
            public float rot;
            public float spin;
            public float sway;
            public float phase;
            public float flip;
            public bool alive;
        }

        readonly System.Collections.Generic.List<Piece> pieces = new System.Collections.Generic.List<Piece>();
        RectTransform area;

        static Color[] colors;

        void Awake()
        {
            area = (RectTransform)transform;
        }

        static Color[] Colors()
        {
            if (colors == null)
                colors = new[]
                {
                    Palette.HelmetYellow, Palette.MomoPink, Palette.PipBlue, Palette.HpP1, Palette.HelmetOrange,
                    Palette.Tier(PartTier.Crystal), Palette.Tier(PartTier.Silver), Palette.Cream
                };
            return colors;
        }

        public void Burst(int count)
        {
            if (area == null) area = (RectTransform)transform;
            var r = area.rect;
            float w = r.width > 10f ? r.width : 1920f;
            float h = r.height > 10f ? r.height : 1080f;
            var cols = Colors();
            for (int i = 0; i < count; i++)
            {
                Piece p = null;
                for (int j = 0; j < pieces.Count; j++)
                    if (!pieces[j].alive) { p = pieces[j]; break; }
                if (p == null)
                {
                    p = new Piece();
                    bool round = pieces.Count % 3 == 0;
                    var img = UIFactory.Img(area, "Confetti", round ? UISprites.Circle : UISprites.Rounded, Color.white, UIFactory.Center,
                        Vector2.zero, round ? new Vector2(16f, 16f) : new Vector2(14f, 26f));
                    if (!round) UIFactory.SetRadius(img, 4f);
                    p.rt = img.rectTransform;
                    pieces.Add(p);
                }
                p.rt.GetComponent<Image>().color = cols[UnityEngine.Random.Range(0, cols.Length)];
                p.pos = new Vector2(UnityEngine.Random.Range(-w * 0.5f, w * 0.5f), h * 0.5f + UnityEngine.Random.Range(20f, 400f));
                p.vel = new Vector2(UnityEngine.Random.Range(-60f, 60f), -UnityEngine.Random.Range(160f, 340f));
                p.rot = UnityEngine.Random.Range(0f, 360f);
                p.spin = UnityEngine.Random.Range(-360f, 360f);
                p.sway = UnityEngine.Random.Range(20f, 70f);
                p.phase = UnityEngine.Random.Range(0f, 6.28f);
                p.flip = UnityEngine.Random.Range(3f, 9f);
                p.alive = true;
                p.rt.gameObject.SetActive(true);
                p.rt.anchoredPosition = p.pos;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                pieces[i].alive = false;
                if (pieces[i].rt != null) pieces[i].rt.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;
            float bottom = -(area.rect.height > 10f ? area.rect.height : 1080f) * 0.5f - 60f;
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                if (!p.alive) continue;
                p.pos += p.vel * dt;
                p.rot += p.spin * dt;
                p.rt.anchoredPosition = p.pos + new Vector2(Mathf.Sin(t * 2f + p.phase) * p.sway, 0f);
                p.rt.localRotation = Quaternion.Euler(0f, 0f, p.rot);
                p.rt.localScale = new Vector3(Mathf.Cos(t * p.flip + p.phase), 1f, 1f); // paper flip
                if (p.pos.y < bottom)
                {
                    p.alive = false;
                    p.rt.gameObject.SetActive(false);
                }
            }
        }
    }
}
