using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Accessory prefabs indexed exactly like <see cref="CosmeticCatalog"/> (null = nothing to spawn, e.g. the
    /// safety helmet or "None"). Built by IRR/Art/Generate All into Resources/CosmeticLibrary.asset.
    /// Prefabs are authored in character-root space, so they parent straight under the character's "Visual".
    /// </summary>
    public class CosmeticLibrary : ScriptableObject
    {
        public GameObject[] hats = new GameObject[0];
        public GameObject[] faces = new GameObject[0];
        public GameObject[] backs = new GameObject[0];

        static CosmeticLibrary cached;

        /// <summary>The library from Resources (cached), or null if the art has not been generated yet.</summary>
        public static CosmeticLibrary Instance
        {
            get
            {
                if (cached == null) cached = Resources.Load<CosmeticLibrary>("CosmeticLibrary");
                return cached;
            }
        }

        /// <summary>Prefab for a slot/index, or null. Body color has no prefab.</summary>
        public GameObject Prefab(CosmeticSlot slot, int index)
        {
            var list = slot switch
            {
                CosmeticSlot.Hat => hats,
                CosmeticSlot.Face => faces,
                CosmeticSlot.Back => backs,
                _ => null,
            };
            return list != null && index >= 0 && index < list.Length ? list[index] : null;
        }
    }
}
