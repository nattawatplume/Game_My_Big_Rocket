using System;
using UnityEngine;

namespace IslandRocketRumble
{
    public enum PlayerId { P1 = 0, P2 = 1 }

    public enum PartType { Gear, Nut, Fuel, Chip, Gem }

    /// <summary>Part/rocket grade. Numeric value = tier number shown in UI (T1..T5).</summary>
    public enum PartTier { Scrap = 1, Copper = 2, Silver = 3, Gold = 4, Crystal = 5 }

    public enum TargetKind { Opponent, Robot }

    public enum GamePhase { Title, HowToPlay, Countdown, Playing, SuddenDeath, Result }

    public enum MatchEndReason { KnockOut, TimeUp, SuddenDeath }

    [Serializable]
    public struct PartKey : IEquatable<PartKey>
    {
        public PartType type;
        public PartTier tier;

        public PartKey(PartType type, PartTier tier)
        {
            this.type = type;
            this.tier = tier;
        }

        public bool Equals(PartKey other) => type == other.type && tier == other.tier;
        public override bool Equals(object obj) => obj is PartKey other && Equals(other);
        public override int GetHashCode() => ((int)type * 16) + (int)tier;
        public override string ToString() => $"{tier} {type}";
    }

    /// <summary>A rocket landing on something. Passed to every IRocketTarget.</summary>
    public struct RocketHit
    {
        public PlayerId shooter;
        public RocketRecipe recipe;
        public Vector3 point;
    }

    /// <summary>Anything a rocket can land on (an island or the robot).</summary>
    public interface IRocketTarget
    {
        /// <summary>World position a rocket should aim at (a random point on the target is fine).</summary>
        Vector3 GetAimPoint();
        void TakeRocketHit(RocketHit hit);
    }

    /// <summary>Implemented by the player controller. The robot's gems call Stun().</summary>
    public interface IStunnable
    {
        bool IsStunned { get; }
        void Stun(float seconds);
    }

    /// <summary>Implemented by each island's part spawner. Robot drops call this to fling parts to a player.</summary>
    public interface IPartSpawner
    {
        /// <summary>Spawn a collectible part that flies from <paramref name="from"/> and lands on this island.</summary>
        void SpawnFlyingPart(PartKey key, Vector3 from);
    }

    public static class PlayerIdExtensions
    {
        public static PlayerId Opponent(this PlayerId id) => id == PlayerId.P1 ? PlayerId.P2 : PlayerId.P1;
    }
}
