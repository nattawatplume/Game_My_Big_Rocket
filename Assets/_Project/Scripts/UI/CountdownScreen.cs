using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace IslandRocketRumble
{
    /// <summary>Big "3, 2, 1, GO!" pops, timed locally from GameConfig.countdownSeconds.</summary>
    public class CountdownScreen : UIScreen
    {
        RectTransform holder;
        UText label;
        UText sub;
        Image burst;
        CanvasGroup holderGroup;
        Coroutine routine;

        protected override void Build()
        {
            burst = UIFactory.Img(rect, "Burst", UISprites.Circle, UIFactory.WithAlpha(Palette.Cream, 0.6f), UIFactory.Center,
                new Vector2(0f, 40f), new Vector2(420f, 420f));

            holder = UIFactory.Rect(rect, "Holder", UIFactory.Center, new Vector2(0f, 40f), new Vector2(800f, 400f));
            holderGroup = UIFactory.Group(holder);

            label = UIFactory.Txt(holder, "Number", "3", 260, Palette.HelmetYellow, TextAnchor.MiddleCenter, UIFactory.Center,
                Vector2.zero, new Vector2(800f, 340f), true);
            UIFactory.Chunky(label, 9f, 12f);

            sub = UIFactory.Txt(holder, "Sub", "พร้อมยัง?", 48, Palette.Ink, TextAnchor.MiddleCenter, UIFactory.Center,
                new Vector2(0f, -190f), new Vector2(800f, 70f), true);
            UIFactory.AddOutline(sub, Color.white, 3f);
        }

        public void Begin(float seconds)
        {
            Show();
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run(Mathf.Max(0.5f, seconds)));
        }

        protected override void OnHide()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
        }

        static IEnumerator Wait(float s)
        {
            float t = 0f;
            while (t < s)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        IEnumerator Run(float seconds)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(seconds - 0.001f));
            float step = seconds / n;
            holderGroup.alpha = 1f;

            for (int i = n; i >= 1; i--)
            {
                Color c = i == 1 ? Palette.HelmetYellow : (i == 2 ? Palette.HelmetOrange : (i == 3 ? Palette.Danger : Palette.PipBlue));
                label.text = i.ToString();
                label.color = c;
                sub.text = i == n ? "พร้อมยัง?" : (i == 1 ? "เตรียม..." : "");
                Pop(c);
                yield return Wait(step);
            }

            label.text = "GO!";
            label.color = Palette.HpP1;
            sub.text = "ลุยเลย!";
            Pop(Palette.HpP1);
            UITween.Shake(holder, 16f, 0.35f);
            yield return Wait(0.75f);
            UITween.Fade(holderGroup, 0f, 0.3f);
            yield return Wait(0.35f);
            routine = null;
            Hide();
        }

        void Pop(Color c)
        {
            UITween.PopIn(holder, 0.35f, 0f);
            burst.color = UIFactory.WithAlpha(Color.Lerp(c, Color.white, 0.55f), 0.7f);
            UITween.PopIn(burst, 0.4f, 0f);
        }

        void Update()
        {
            // gentle spin of the burst disc for life
            burst.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 40f);
        }
    }
}
