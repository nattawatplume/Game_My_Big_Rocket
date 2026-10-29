using System.Collections;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>Per-object tween state. Added automatically by UITween.</summary>
    public class UITweenRunner : MonoBehaviour
    {
        Coroutine scaleCo;
        Vector3 scaleFinal = Vector3.one;
        bool scaling;

        Coroutine shakeCo;
        Vector2 shakeOrigin;
        bool shaking;

        Coroutine fadeCo;
        CanvasGroup fadeGroup;
        float fadeFinal;
        bool fading;

        static float Dt => Time.unscaledDeltaTime;

        // ------------------------------------------------------------ scale

        void StopScale()
        {
            if (scaleCo != null) StopCoroutine(scaleCo);
            scaleCo = null;
            scaling = false;
        }

        Vector3 CurrentBaseScale() => scaling ? scaleFinal : transform.localScale;

        public void PopIn(float dur, float delay, Vector3 final)
        {
            StopScale();
            if (!isActiveAndEnabled) { transform.localScale = final; return; }
            scaleFinal = final;
            scaling = true;
            transform.localScale = Vector3.zero;
            scaleCo = StartCoroutine(PopCo(dur, delay, final));
        }

        IEnumerator PopCo(float dur, float delay, Vector3 final)
        {
            float t = 0f;
            while (t < delay) { t += Dt; yield return null; }
            t = 0f;
            while (t < dur)
            {
                t += Dt;
                float k = UITween.EaseOutBack(Mathf.Clamp01(t / dur), 2.2f);
                transform.localScale = final * k;
                yield return null;
            }
            transform.localScale = final;
            scaling = false;
            scaleCo = null;
        }

        public void Punch(float amount, float dur)
        {
            Vector3 baseScale = CurrentBaseScale();
            if (baseScale.sqrMagnitude < 0.0001f) baseScale = Vector3.one;
            StopScale();
            if (!isActiveAndEnabled) { transform.localScale = baseScale; return; }
            scaleFinal = baseScale;
            scaling = true;
            scaleCo = StartCoroutine(PunchCo(amount, dur, baseScale));
        }

        IEnumerator PunchCo(float amount, float dur, Vector3 baseScale)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Dt;
                float k = Mathf.Clamp01(t / dur);
                float s = 1f + amount * Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k);
                transform.localScale = baseScale * s;
                yield return null;
            }
            transform.localScale = baseScale;
            scaling = false;
            scaleCo = null;
        }

        public void Slam(float from, float dur, Vector3 final)
        {
            StopScale();
            if (!isActiveAndEnabled) { transform.localScale = final; return; }
            scaleFinal = final;
            scaling = true;
            scaleCo = StartCoroutine(SlamCo(from, dur, final));
        }

        IEnumerator SlamCo(float from, float dur, Vector3 final)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Dt;
                float k = Mathf.Clamp01(t / dur);
                float e = UITween.EaseInCubic(k);
                transform.localScale = Vector3.LerpUnclamped(final * from, final, e);
                yield return null;
            }
            // squash bounce on impact
            t = 0f;
            const float bounce = 0.3f;
            while (t < bounce)
            {
                t += Dt;
                float k = Mathf.Clamp01(t / bounce);
                float s = Mathf.Sin(k * Mathf.PI * 2f) * (1f - k) * 0.18f;
                transform.localScale = new Vector3(final.x * (1f + s), final.y * (1f - s), final.z);
                yield return null;
            }
            transform.localScale = final;
            scaling = false;
            scaleCo = null;
        }

        public void ScaleTo(Vector3 target, float dur)
        {
            Vector3 start = transform.localScale;
            StopScale();
            if (!isActiveAndEnabled || dur <= 0f) { transform.localScale = target; return; }
            scaleFinal = target;
            scaling = true;
            scaleCo = StartCoroutine(ScaleToCo(start, target, dur));
        }

        IEnumerator ScaleToCo(Vector3 start, Vector3 target, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Dt;
                transform.localScale = Vector3.Lerp(start, target, UITween.EaseOutCubic(t / dur));
                yield return null;
            }
            transform.localScale = target;
            scaling = false;
            scaleCo = null;
        }

        // ------------------------------------------------------------ shake

        public void Shake(float strength, float dur)
        {
            var rt = transform as RectTransform;
            if (rt == null) return;
            if (shaking)
            {
                if (shakeCo != null) StopCoroutine(shakeCo);
                shakeCo = null;
                rt.anchoredPosition = shakeOrigin;
                shaking = false;
            }
            if (!isActiveAndEnabled) return;
            shakeOrigin = rt.anchoredPosition;
            shaking = true;
            shakeCo = StartCoroutine(ShakeCo(rt, strength, dur));
        }

        IEnumerator ShakeCo(RectTransform rt, float strength, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Dt;
                float k = 1f - Mathf.Clamp01(t / dur);
                rt.anchoredPosition = shakeOrigin + UnityEngine.Random.insideUnitCircle * strength * k;
                yield return null;
            }
            rt.anchoredPosition = shakeOrigin;
            shaking = false;
            shakeCo = null;
        }

        // ------------------------------------------------------------ fade

        public void Fade(CanvasGroup g, float to, float dur, float delay)
        {
            if (fadeCo != null) StopCoroutine(fadeCo);
            fadeCo = null;
            fading = false;
            if (g == null) return;
            if (!isActiveAndEnabled || (dur <= 0f && delay <= 0f)) { g.alpha = to; return; }
            fadeGroup = g;
            fadeFinal = to;
            fading = true;
            fadeCo = StartCoroutine(FadeCo(g, to, dur, delay));
        }

        IEnumerator FadeCo(CanvasGroup g, float to, float dur, float delay)
        {
            float t = 0f;
            while (t < delay) { t += Dt; yield return null; }
            float from = g.alpha;
            t = 0f;
            while (t < dur)
            {
                t += Dt;
                g.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / dur));
                yield return null;
            }
            g.alpha = to;
            fading = false;
            fadeCo = null;
        }

        // ------------------------------------------------------------ lifecycle

        public void StopAllTweens()
        {
            if (scaling) { StopScale(); transform.localScale = scaleFinal; }
            if (shaking)
            {
                if (shakeCo != null) StopCoroutine(shakeCo);
                shakeCo = null;
                var rt = transform as RectTransform;
                if (rt != null) rt.anchoredPosition = shakeOrigin;
                shaking = false;
            }
            if (fading)
            {
                if (fadeCo != null) StopCoroutine(fadeCo);
                fadeCo = null;
                if (fadeGroup != null) fadeGroup.alpha = fadeFinal;
                fading = false;
            }
        }

        void OnDisable()
        {
            // Coroutines are already stopped by Unity; snap every channel to its final state.
            scaleCo = null;
            shakeCo = null;
            fadeCo = null;
            if (scaling) { transform.localScale = scaleFinal; scaling = false; }
            if (shaking)
            {
                var rt = transform as RectTransform;
                if (rt != null) rt.anchoredPosition = shakeOrigin;
                shaking = false;
            }
            if (fading)
            {
                if (fadeGroup != null) fadeGroup.alpha = fadeFinal;
                fading = false;
            }
        }
    }
}
