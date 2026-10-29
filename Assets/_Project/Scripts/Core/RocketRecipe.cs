using System;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>One fixed rocket recipe. Five assets live in ScriptableObjects/Rockets.</summary>
    [CreateAssetMenu(menuName = "IRR/Rocket Recipe", fileName = "Rocket")]
    public class RocketRecipe : ScriptableObject
    {
        public PartTier tier = PartTier.Scrap;
        public string displayName = "Firecracker";
        public int damage = 30;
        public PartCost[] costs = Array.Empty<PartCost>();

        [Tooltip("Visual prefab used in flight (built by the art generator).")]
        public GameObject flightPrefab;
        [Tooltip("Icon shown in the craft menu.")]
        public Sprite icon;
        [Tooltip("Crater / explosion size multiplier.")]
        public float impactScale = 1f;
    }

    [Serializable]
    public struct PartCost
    {
        public PartType type;
        public PartTier tier;
        public int count;

        public PartKey Key => new PartKey(type, tier);
    }
}
