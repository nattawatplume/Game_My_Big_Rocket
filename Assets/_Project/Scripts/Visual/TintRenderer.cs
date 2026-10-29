using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>Tints renderers by overriding _BaseColor through a MaterialPropertyBlock (keeps shared materials untouched).</summary>
    public class TintRenderer : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Renderers to tint. Empty = every renderer in children.")]
        public Renderer[] targets;
        public bool applyOnAwake;
        public Color color = Color.white;

        MaterialPropertyBlock block;

        public Color CurrentColor => color;

        void Awake()
        {
            if (applyOnAwake) SetColor(color);
        }

        public void SetColor(Color c)
        {
            color = c;
            if (block == null) block = new MaterialPropertyBlock();
            if (targets == null || targets.Length == 0) targets = GetComponentsInChildren<Renderer>(true);
            foreach (var r in targets)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, c);
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>Changes only the alpha (handy for fading beams/rings).</summary>
        public void SetAlpha(float a)
        {
            var c = color;
            c.a = a;
            SetColor(c);
        }

        /// <summary>Removes the override so the material color shows again.</summary>
        public void ClearColor()
        {
            if (targets == null) return;
            foreach (var r in targets)
                if (r != null) r.SetPropertyBlock(null);
        }
    }
}
