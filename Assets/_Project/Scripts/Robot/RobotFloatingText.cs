using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>Self-animating, self-destroying world-space label ("BONK!", "RARE DROP!").</summary>
    public class RobotFloatingText : MonoBehaviour
    {
        TextMesh tm;
        Vector3 start;
        Color baseColor;
        float duration = 0.6f;
        float rise = 0.6f;
        float elapsed;

        public static RobotFloatingText Spawn(string text, Vector3 position, Color color, float characterSize, float duration, float rise)
        {
            var tm = RobotFx.NewText("FloatingText", null, text, color, characterSize);
            var ft = tm.gameObject.AddComponent<RobotFloatingText>();
            ft.tm = tm;
            ft.start = position;
            ft.baseColor = color;
            ft.duration = Mathf.Max(0.05f, duration);
            ft.rise = rise;
            tm.transform.position = position;
            tm.transform.localScale = Vector3.one * 0.5f;
            RobotFx.FaceCamera(tm.transform);
            return ft;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = start + Vector3.up * (rise * (1f - (1f - t) * (1f - t)));
            float s = t < 0.2f ? Mathf.Lerp(0.5f, 1.3f, t / 0.2f) : Mathf.Lerp(1.3f, 1f, (t - 0.2f) / 0.8f);
            transform.localScale = Vector3.one * s;
            if (tm != null)
            {
                var c = baseColor;
                c.a = baseColor.a * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, t)));
                tm.color = c;
            }
            RobotFx.FaceCamera(transform);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
