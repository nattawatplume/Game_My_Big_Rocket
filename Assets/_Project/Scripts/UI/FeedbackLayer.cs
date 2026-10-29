using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace IslandRocketRumble
{
    /// <summary>
    /// World-anchored feedback: damage popups, "ว้ายยย!" speech bubbles, stun overlay (spinning stars + timer bar).
    /// Always active; clears itself when leaving the playing phases.
    /// </summary>
    public class FeedbackLayer : MonoBehaviour
    {
        class StunOverlay
        {
            public RectTransform root;
            public RectTransform starsRoot;
            public RectTransform[] stars;
            public RectTransform barFill;
            public UText label;
            public float duration;
            public float remaining;
            public bool active;
        }

        class Bubble
        {
            public RectTransform root;
            public CanvasGroup group;
            public float time;
            public float life;
            public bool active;
        }

        UIRoot root;
        RectTransform space;
        RectTransform popupLayer;
        readonly StunOverlay[] stuns = new StunOverlay[2];
        readonly Bubble[] bubbles = new Bubble[2];
        readonly List<GameObject> popups = new List<GameObject>();

        static readonly string[] Shouts = { "ว้ายยย!", "ว้ายยย!", "โอ๊ยยย!", "ว้ายยย!!" };

        public void Init(UIRoot owner)
        {
            root = owner;
            space = (RectTransform)transform;
            UIFactory.Group(this);

            var overlays = UIFactory.Stretch(space, "Overlays");
            for (int i = 0; i < 2; i++)
            {
                stuns[i] = BuildStun(overlays, (PlayerId)i);
                bubbles[i] = BuildBubble(overlays, (PlayerId)i);
            }
            popupLayer = UIFactory.Stretch(space, "Popups");

            Subscribe();
        }

        /// <summary>Idempotent (re)subscription to the global event bus.</summary>
        public void Subscribe()
        {
            GameEvents.IslandDamaged -= OnIslandDamaged;
            GameEvents.PlayerStunned -= OnPlayerStunned;
            GameEvents.IslandDamaged += OnIslandDamaged;
            GameEvents.PlayerStunned += OnPlayerStunned;
        }

        void OnDestroy()
        {
            GameEvents.IslandDamaged -= OnIslandDamaged;
            GameEvents.PlayerStunned -= OnPlayerStunned;
        }

        // ---------------------------------------------------------------- build

        StunOverlay BuildStun(Transform parent, PlayerId id)
        {
            var s = new StunOverlay();
            s.root = UIFactory.Rect(parent, "Stun_" + UIText.PlayerTag(id), UIFactory.Center, Vector2.zero, new Vector2(200f, 110f));
            s.starsRoot = UIFactory.Rect(s.root, "Stars", UIFactory.Center, new Vector2(0f, 30f), new Vector2(160f, 60f));
            s.stars = new RectTransform[5];
            for (int i = 0; i < s.stars.Length; i++)
            {
                var img = UIFactory.Icon(s.starsRoot, "Star" + i, UISprites.Star, i % 2 == 0 ? Palette.HelmetYellow : Color.white,
                    UIFactory.Center, Vector2.zero, 30f, 2f);
                s.stars[i] = img.rectTransform;
            }

            var track = UIFactory.Img(s.root, "Track", UISprites.Rounded, Palette.Ink, UIFactory.Center, new Vector2(0f, -14f),
                new Vector2(170f, 22f));
            UIFactory.SetRadius(track, 11f);
            var inner = UIFactory.Stretch(track.transform, "Inner", 3f);
            var fill = UIFactory.StretchImg(inner, "Fill", UISprites.Rounded, Palette.HelmetYellow);
            UIFactory.SetRadius(fill, 8f);
            s.barFill = fill.rectTransform;

            s.label = UIFactory.Txt(s.root, "Label", "STUN 2.5 วิ", 22, Color.white, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, -42f), new Vector2(220f, 30f), true);
            UIFactory.AddOutline(s.label, Palette.Ink, 2.5f);

            s.root.gameObject.SetActive(false);
            return s;
        }

        Bubble BuildBubble(Transform parent, PlayerId id)
        {
            var b = new Bubble();
            b.root = UIFactory.Rect(parent, "Bubble_" + UIText.PlayerTag(id), UIFactory.Center, Vector2.zero, new Vector2(190f, 74f));
            b.group = UIFactory.Group(b.root);
            float sx = id == PlayerId.P1 ? 1f : -1f; // P1 bubble sits right of the head (tail points down-left), P2 mirrored
            var tailInk = UIFactory.Img(b.root, "TailInk", UISprites.TriangleDown, Palette.Ink, UIFactory.BottomCenter, new Vector2(-40f * sx, -10f),
                new Vector2(32f, 26f));
            tailInk.preserveAspect = false;
            tailInk.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -25f * sx);
            UIFactory.Panel(b.root, "Panel", UIFactory.Center, Vector2.zero, new Vector2(190f, 74f), Color.white, 30f, 4f, true);
            var tailFill = UIFactory.Img(b.root, "TailFill", UISprites.TriangleDown, Color.white, UIFactory.BottomCenter, new Vector2(-39f * sx, -4f),
                new Vector2(22f, 18f));
            tailFill.preserveAspect = false;
            tailFill.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -25f * sx);
            var t = UIFactory.Txt(b.root, "Text", "ว้ายยย!", 36, Palette.Danger, TextAnchor.MiddleCenter, UIFactory.Center, new Vector2(0f, 2f),
                new Vector2(190f, 70f), true);
            t.name = "Text";
            b.root.gameObject.SetActive(false);
            return b;
        }

        // ---------------------------------------------------------------- phase

        public void OnPhase(GamePhase p)
        {
            if (p == GamePhase.Playing || p == GamePhase.SuddenDeath || p == GamePhase.Countdown) return;
            if (p == GamePhase.Result) return; // let the last popups finish over the result
            ClearAll();
        }

        void ClearAll()
        {
            StopAllCoroutines();
            for (int i = 0; i < popups.Count; i++)
                if (popups[i] != null) Destroy(popups[i]);
            popups.Clear();
            for (int i = 0; i < 2; i++)
            {
                stuns[i].active = false;
                stuns[i].root.gameObject.SetActive(false);
                bubbles[i].active = false;
                bubbles[i].root.gameObject.SetActive(false);
            }
        }

        // ---------------------------------------------------------------- events

        void OnIslandDamaged(PlayerId owner, int damage, int hpLeft, Vector3 point)
        {
            Vector2 local;
            if (UIWorld.WorldToCanvas(point, space, out local) && isActiveAndEnabled)
                StartCoroutine(DamagePopup(local, damage));

            // Speech bubble over the island owner.
            var b = bubbles[(int)owner];
            b.root.Find("Text").GetComponent<UText>().text = Shouts[UnityEngine.Random.Range(0, Shouts.Length)];
            b.active = true;
            b.time = 0f;
            b.life = 1.3f;
            b.group.alpha = 1f;
            b.root.gameObject.SetActive(true);
            UITween.PopIn(b.root, 0.3f, 0f);
            UpdateBubble(owner, b, 0f);
        }

        void OnPlayerStunned(PlayerId id, float duration)
        {
            var s = stuns[(int)id];
            s.duration = Mathf.Max(0.1f, duration);
            s.remaining = s.duration;
            s.active = true;
            s.root.gameObject.SetActive(true);
            UITween.PopIn(s.root, 0.3f, 0f);
            UpdateStun(id, s, 0f);
        }

        IEnumerator DamagePopup(Vector2 start, int damage)
        {
            int size = Mathf.Clamp(46 + damage / 6, 46, 110);
            var t = UIFactory.Txt(popupLayer, "Dmg", "-" + damage, size, damage >= 150 ? Palette.Danger : Palette.HelmetOrange,
                TextAnchor.MiddleCenter, UIFactory.Center, start, new Vector2(400f, 140f), true);
            UIFactory.AddShadow(t, Palette.Ink, new Vector2(0f, -size * 0.08f));
            UIFactory.AddOutline(t, Color.white, size * 0.06f);
            var go = t.gameObject;
            popups.Add(go);
            var rt = t.rectTransform;
            float side = UnityEngine.Random.Range(-50f, 50f);
            const float dur = 1.15f;
            float time = 0f;
            Color baseColor = t.color;
            while (time < dur && go != null)
            {
                time += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(time / dur);
                float up = UITween.EaseOutBack(Mathf.Clamp01(k * 2.2f), 2.5f) * 120f;
                rt.anchoredPosition = start + new Vector2(side * k, up);
                float s = k < 0.12f ? Mathf.Lerp(0.3f, 1.4f, k / 0.12f) : Mathf.Lerp(1.4f, 1f, Mathf.Clamp01((k - 0.12f) / 0.2f));
                rt.localScale = new Vector3(s, s, 1f);
                rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 12f) * 6f * (1f - k));
                float a = k > 0.7f ? 1f - (k - 0.7f) / 0.3f : 1f;
                t.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
            popups.Remove(go);
            if (go != null) Destroy(go);
        }

        // ---------------------------------------------------------------- per-frame

        void LateUpdate()
        {
            float dt = Time.deltaTime; // stun follows game time
            for (int i = 0; i < 2; i++)
            {
                var id = (PlayerId)i;
                var s = stuns[i];
                if (s.active)
                {
                    s.remaining -= dt;
                    if (s.remaining <= 0f)
                    {
                        s.active = false;
                        s.root.gameObject.SetActive(false);
                    }
                    else UpdateStun(id, s, Time.unscaledTime);
                }

                var b = bubbles[i];
                if (b.active)
                {
                    b.time += Time.unscaledDeltaTime;
                    if (b.time >= b.life)
                    {
                        b.active = false;
                        b.root.gameObject.SetActive(false);
                    }
                    else UpdateBubble(id, b, b.time);
                }
            }
        }

        void UpdateStun(PlayerId id, StunOverlay s, float time)
        {
            Vector3 head;
            Vector2 local;
            if (UIWorld.TryGetHead(id, out head) && UIWorld.WorldToCanvas(head, space, out local))
                s.root.anchoredPosition = local + new Vector2(0f, 60f);

            for (int i = 0; i < s.stars.Length; i++)
            {
                float ang = time * 4f + i * Mathf.PI * 2f / s.stars.Length;
                float depth = Mathf.Sin(ang);
                s.stars[i].anchoredPosition = new Vector2(Mathf.Cos(ang) * 62f, depth * 16f);
                float sc = 0.75f + 0.3f * (1f - (depth + 1f) * 0.5f);
                s.stars[i].localScale = new Vector3(sc, sc, 1f);
                s.stars[i].localRotation = Quaternion.Euler(0f, 0f, time * 360f + i * 40f);
            }

            float v = Mathf.Clamp01(s.remaining / s.duration);
            s.barFill.anchorMin = new Vector2(0.5f - v * 0.5f, 0f);
            s.barFill.anchorMax = new Vector2(0.5f + v * 0.5f, 1f);
            s.barFill.offsetMin = Vector2.zero;
            s.barFill.offsetMax = Vector2.zero;
            s.label.text = "STUN " + Mathf.Max(0f, s.remaining).ToString("0.0") + " วิ";
        }

        void UpdateBubble(PlayerId id, Bubble b, float time)
        {
            Vector3 head;
            Vector2 local;
            if (UIWorld.TryGetHead(id, out head) && UIWorld.WorldToCanvas(head, space, out local))
                b.root.anchoredPosition = local + new Vector2(id == PlayerId.P1 ? 110f : -110f, 70f + time * 18f);
            b.root.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 18f) * 6f * Mathf.Clamp01(1f - time));
            if (time > b.life - 0.3f) b.group.alpha = Mathf.Clamp01((b.life - time) / 0.3f);
        }
    }
}
