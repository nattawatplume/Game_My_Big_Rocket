using UnityEngine;
using UnityEngine.UI;

namespace IslandRocketRumble
{
    /// <summary>
    /// uGUI Text that stacks Thai marks correctly. Legacy Text ignores the font's mark positioning, so a tone mark
    /// after an upper vowel (ปิ๊, เพื่อ, ตั้ง) or before sara am (น้ำ) is drawn on top of the vowel and disappears.
    /// After the mesh is built, those tone-mark quads are lifted just above the vowel's actual glyph quad.
    /// Relies on legacy Text emitting one quad per visible character (no quads for whitespace or rich-text tags).
    /// </summary>
    public class ThaiText : Text
    {
        /// <summary>Gap left between the vowel and the lifted tone mark, as a fraction of the tone mark's height.</summary>
        const float GapRatio = 0.12f;

        readonly UIVertex[] quad = new UIVertex[4];

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);

            string s = text;
            if (string.IsNullOrEmpty(s) || !ContainsThaiTone(s)) return;

            int quadCount = toFill.currentVertCount / 4;
            int q = 0;
            int upperVowelQuad = -1;
            for (int i = 0; i < s.Length && q < quadCount; i++)
            {
                char c = s[i];
                if (supportRichText && c == '<')
                {
                    int close = s.IndexOf('>', i);
                    if (close > i && LooksLikeTag(s, i, close)) { i = close; continue; }
                }
                if (char.IsWhiteSpace(c)) { upperVowelQuad = -1; continue; }

                if (IsTone(c))
                {
                    // น้ำ: the ring of sara am (next quad) sits where the tone mark is.
                    int below = upperVowelQuad >= 0 ? upperVowelQuad
                        : (i + 1 < s.Length && s[i + 1] == 'ำ' && q + 1 < quadCount ? q + 1 : -1);
                    if (below >= 0) LiftAbove(toFill, q, below);
                    upperVowelQuad = -1;
                }
                else
                {
                    upperVowelQuad = IsUpperVowel(c) ? q : -1;
                }
                q++;
            }
        }

        void LiftAbove(VertexHelper vh, int toneQuad, int belowQuad)
        {
            QuadYRange(vh, belowQuad, out _, out float belowTop);
            QuadYRange(vh, toneQuad, out float toneBottom, out float toneTop);
            float dy = belowTop - toneBottom + (toneTop - toneBottom) * GapRatio;
            if (dy <= 0f) return;

            int start = toneQuad * 4;
            for (int k = 0; k < 4; k++)
            {
                vh.PopulateUIVertex(ref quad[k], start + k);
                quad[k].position.y += dy;
                vh.SetUIVertex(quad[k], start + k);
            }
        }

        void QuadYRange(VertexHelper vh, int quadIndex, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            for (int k = 0; k < 4; k++)
            {
                vh.PopulateUIVertex(ref quad[k], quadIndex * 4 + k);
                min = Mathf.Min(min, quad[k].position.y);
                max = Mathf.Max(max, quad[k].position.y);
            }
        }

        static bool ContainsThaiTone(string s)
        {
            foreach (char c in s)
                if (IsTone(c)) return true;
            return false;
        }

        // ่ ้ ๊ ๋ ์
        static bool IsTone(char c) => c >= '่' && c <= '์';

        // ั ิ ี ึ ื ็ ํ
        static bool IsUpperVowel(char c) => c == 'ั' || (c >= 'ิ' && c <= 'ื') || c == '็' || c == 'ํ';

        static bool LooksLikeTag(string s, int open, int close)
        {
            // Only the tags our UI uses; anything else is treated as visible text.
            string tag = s.Substring(open + 1, close - open - 1).TrimStart('/');
            return tag.StartsWith("color") || tag.StartsWith("size") || tag == "b" || tag == "i";
        }
    }
}
