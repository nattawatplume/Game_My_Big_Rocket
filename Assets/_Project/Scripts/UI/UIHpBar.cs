using UnityEngine;
using UnityEngine.UI;

namespace IslandRocketRumble
{
    /// <summary>HP bar fill + white "damage chip" that trails behind.</summary>
    public class UIHpBar : MonoBehaviour
    {
        RectTransform fill;
        RectTransform chip;
        Image fillImg, chipImg;
        bool rightToLeft;
        float target = 1f;
        float chipValue = 1f;
        float chipDelay;

        public void Setup(Image fillImage, Image chipImage, bool rtl)
        {
            fillImg = fillImage;
            chipImg = chipImage;
            fill = fillImage.rectTransform;
            chip = chipImage.rectTransform;
            rightToLeft = rtl;
            SetInstant(1f);
        }

        public void SetInstant(float v)
        {
            target = chipValue = Mathf.Clamp01(v);
            chipDelay = 0f;
            Apply(fill, fillImg, target);
            Apply(chip, chipImg, chipValue);
        }

        public void SetDamaged(float v)
        {
            v = Mathf.Clamp01(v);
            if (v < target) chipDelay = 0.45f;
            target = v;
            if (chipValue < v) chipValue = v;
            Apply(fill, fillImg, target);
            Apply(chip, chipImg, chipValue);
        }

        void Update()
        {
            if (chipValue <= target) return;
            if (chipDelay > 0f) { chipDelay -= Time.unscaledDeltaTime; return; }
            chipValue = Mathf.MoveTowards(chipValue, target, Time.unscaledDeltaTime * 0.7f);
            Apply(chip, chipImg, chipValue);
        }

        void Apply(RectTransform r, Image img, float v)
        {
            if (r == null) return;
            if (!rightToLeft)
            {
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(v, 1f);
            }
            else
            {
                r.anchorMin = new Vector2(1f - v, 0f);
                r.anchorMax = new Vector2(1f, 1f);
            }
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            if (img != null) img.enabled = v > 0.004f;
        }
    }
}
