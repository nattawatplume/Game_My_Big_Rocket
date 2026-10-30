using System;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>Every tunable number in the game lives here. One asset: ScriptableObjects/GameConfig.asset.</summary>
    [CreateAssetMenu(menuName = "IRR/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Match")]
        public float matchDuration = 300f;
        public float tierInterval = 45f;
        public int islandMaxHp = 600;
        public float countdownSeconds = 3f;

        [Header("Player")]
        public float moveSpeed = 4.5f;
        public float stunDuration = 2.5f;

        [Header("Part drops on islands")]
        public float partDropInterval = 2.5f;
        public float partWarningTime = 2.5f;
        public float partLifetime = 15f;
        [Tooltip("If fewer parts than this are on an island (including ones on the way), one drops at once with no warning.")]
        public int minPartsOnIsland = 4;
        [Tooltip("When the tier rises, this many old parts of a type become 1 new part (rounded up).")]
        public int tierConvertRatio = 2;
        [Tooltip("Relative weight of each PartType when a part drops (index = PartType). Gem only drops from Crystal tier onward.")]
        public float[] partTypeWeights = { 3f, 2f, 2f, 1.5f, 1f };

        [Header("Power-ups")]
        [Tooltip("Seconds between power-ups on each island (a little random is added).")]
        public float powerUpInterval = 20f;
        public float powerUpLifetime = 12f;
        public float speedBoostDuration = 8f;
        public float speedBoostMultiplier = 1.5f;
        public float magnetDuration = 8f;
        public float magnetRadius = 3.5f;
        public float magnetPullSpeed = 7f;

        [Header("Rockets")]
        public float rocketFlightTime = 1.6f;
        public float rocketArcHeight = 7f;

        [Header("Robot")]
        public RageLevel[] rageLevels =
        {
            new RageLevel { damageThreshold = 0, throwInterval = 12f, gemsPerThrow = 1, scale = 1f },
            new RageLevel { damageThreshold = 200, throwInterval = 9f, gemsPerThrow = 1, scale = 1.05f },
            new RageLevel { damageThreshold = 500, throwInterval = 6f, gemsPerThrow = 2, scale = 1.12f },
            new RageLevel { damageThreshold = 900, throwInterval = 3.5f, gemsPerThrow = 3, scale = 1.25f },
        };
        public float gemWarningTime = 1.2f;
        public float gemHitRadius = 0.9f;
        public float transformDuration = 1.2f;
        [Tooltip("Seconds between weak-point openings (robot at rage 1 or higher).")]
        public float weakPointInterval = 14f;
        public float weakPointDuration = 5f;
        [Tooltip("Drop count multiplier for a rocket that lands while the weak point is open.")]
        public int weakPointDropMultiplier = 2;
        public RobotDrop[] robotDrops =
        {
            new RobotDrop { rocketTier = PartTier.Scrap, count = 3, rareChance = 0f },
            new RobotDrop { rocketTier = PartTier.Copper, count = 3, rareChance = 0.05f },
            new RobotDrop { rocketTier = PartTier.Silver, count = 4, rareChance = 0.15f },
            new RobotDrop { rocketTier = PartTier.Gold, count = 4, rareChance = 0.30f },
            new RobotDrop { rocketTier = PartTier.Crystal, count = 5, rareChance = 0.50f },
        };

        public PartTier TierAt(float elapsed)
        {
            int t = 1 + Mathf.FloorToInt(elapsed / tierInterval);
            return (PartTier)Mathf.Clamp(t, 1, 5);
        }

        public int RageLevelFor(int accumulatedDamage)
        {
            int level = 0;
            for (int i = 0; i < rageLevels.Length; i++)
                if (accumulatedDamage >= rageLevels[i].damageThreshold) level = i;
            return level;
        }
    }

    [Serializable]
    public struct RageLevel
    {
        public int damageThreshold;
        public float throwInterval;
        public int gemsPerThrow;
        public float scale;
    }

    [Serializable]
    public struct RobotDrop
    {
        public PartTier rocketTier;
        public int count;
        [Range(0, 1)] public float rareChance;
    }
}
