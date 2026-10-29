using System.Collections;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Tiny tween helper. Tweens run as coroutines on a UITweenRunner that is added to the target itself,
    /// so they stop cleanly when the target is disabled (final state is restored).
    /// One channel each for scale, shake (anchoredPosition) and fade. All use unscaled time.
    /// </summary>
    public static class UITween
    {
        static UITweenRunner R(Component c)
        {
            var r = c.GetComponent<UITweenRunner>();
            if (r == null) r = c.gameObject.AddComponent<UITweenRunner>();
            return r;
        }

        /// <summary>Scale 0 → 1 with overshoot.</summary>
        public static void PopIn(Component c, float duration = 0.4f, float delay = 0f)
            => R(c).PopIn(duration, delay, Vector3.one);

        public static void PopIn(Component c, Vector3 finalScale, float duration, float delay)
            => R(c).PopIn(duration, delay, finalScale);

        /// <summary>Damped scale wobble around the current scale.</summary>
        public static void Punch(Component c, float amount = 0.2f, float duration = 0.35f)
            => R(c).Punch(amount, duration);

        /// <summary>Big → normal with overshoot (title slam).</summary>
        public static void Slam(Component c, float fromScale = 3f, float duration = 0.35f)
            => R(c).Slam(fromScale, duration, Vector3.one);

        public static void ScaleTo(Component c, Vector3 target, float duration)
            => R(c).ScaleTo(target, duration);

        /// <summary>Random anchoredPosition jitter, restored afterwards. Target must be a RectTransform object.</summary>
        public static void Shake(Component c, float strength = 10f, float duration = 0.3f)
            => R(c).Shake(strength, duration);

        public static void Fade(CanvasGroup g, float to, float duration, float delay = 0f)
            => R(g).Fade(g, to, duration, delay);

        public static void StopAll(Component c)
        {
            var r = c.GetComponent<UITweenRunner>();
            if (r != null) r.StopAllTweens();
        }

        public static float EaseOutBack(float t, float s = 1.70158f)
        {
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }

        public static float EaseOutCubic(float t)
        {
            t = 1f - Mathf.Clamp01(t);
            return 1f - t * t * t;
        }

        public static float EaseInCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }
    }

}
