using System;

namespace IslandRocketRumble
{
    /// <summary>
    /// Global event bus. Systems raise, UI/VFX/audio listen. Keeps the parallel modules decoupled.
    /// Call Reset() when a match restarts if you keep static listeners alive.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<GamePhase> PhaseChanged;
        public static event Action<PartTier> TierChanged;
        /// <summary>Elapsed seconds since match start (raised every frame while playing).</summary>
        public static event Action<float> ClockTick;

        public static event Action<PlayerId, PartKey> PartCollected;
        public static event Action<PlayerId, RocketRecipe, TargetKind> RocketLaunched;
        /// <summary>(victim island owner, damage, hp left, impact point)</summary>
        public static event Action<PlayerId, int, int, UnityEngine.Vector3> IslandDamaged;
        public static event Action<PlayerId, float> PlayerStunned;

        /// <summary>(rage level 0-3, accumulated damage)</summary>
        public static event Action<int, int> RobotRageChanged;
        public static event Action<PlayerId, RocketRecipe> RobotHit;
        /// <summary>Robot started a gem throw at this world point; warning lasts GameConfig.gemWarningTime.</summary>
        public static event Action<UnityEngine.Vector3> GemIncoming;

        /// <summary>(winner, reason)</summary>
        public static event Action<PlayerId, MatchEndReason> MatchEnded;
        public static event Action<PlayerId, bool> CraftMenuToggled;
        /// <summary>A rocket piece on the owner's workshop pad flipped from blueprint to built (the part type that built it).</summary>
        public static event Action<PlayerId, PartType> RocketPieceBuilt;
        /// <summary>The rocket previewed on the owner's workshop pad just became fully buildable.</summary>
        public static event Action<PlayerId, RocketRecipe> RocketReady;

        /// <summary>The robot changed form (new rage level).</summary>
        public static event Action<int> RobotTransformed;
        /// <summary>The robot's weak point opened (true) or closed (false).</summary>
        public static event Action<bool> RobotWeakPoint;
        public static event Action<PlayerId, PowerUpType> PowerUpCollected;
        /// <summary>(island owner, impact point) A shield absorbed a rocket.</summary>
        public static event Action<PlayerId, UnityEngine.Vector3> ShieldBlocked;

        /// <summary>(island owner, amount healed, hp, max hp, shown to the players) HP or max HP went up.</summary>
        public static event Action<PlayerId, int, int, int, bool> IslandHealed;
        public static event Action<PlayerId, UpgradeType> UpgradePicked;

        // Flow requests: UI raises these, GameManager answers by changing phase.
        public static event Action RequestHowToPlay;
        public static event Action RequestStartMatch;
        public static event Action RequestRematch;
        public static event Action RequestTitle;

        public static void RaiseRequestHowToPlay() => RequestHowToPlay?.Invoke();
        public static void RaiseRequestStartMatch() => RequestStartMatch?.Invoke();
        public static void RaiseRequestRematch() => RequestRematch?.Invoke();
        public static void RaiseRequestTitle() => RequestTitle?.Invoke();

        public static void RaisePhaseChanged(GamePhase p) => PhaseChanged?.Invoke(p);
        public static void RaiseTierChanged(PartTier t) => TierChanged?.Invoke(t);
        public static void RaiseClockTick(float elapsed) => ClockTick?.Invoke(elapsed);
        public static void RaisePartCollected(PlayerId p, PartKey k) => PartCollected?.Invoke(p, k);
        public static void RaiseRocketLaunched(PlayerId p, RocketRecipe r, TargetKind t) => RocketLaunched?.Invoke(p, r, t);
        public static void RaiseIslandDamaged(PlayerId owner, int dmg, int hpLeft, UnityEngine.Vector3 point) => IslandDamaged?.Invoke(owner, dmg, hpLeft, point);
        public static void RaisePlayerStunned(PlayerId p, float duration) => PlayerStunned?.Invoke(p, duration);
        public static void RaiseRobotRageChanged(int level, int accumulated) => RobotRageChanged?.Invoke(level, accumulated);
        public static void RaiseRobotHit(PlayerId p, RocketRecipe r) => RobotHit?.Invoke(p, r);
        public static void RaiseGemIncoming(UnityEngine.Vector3 point) => GemIncoming?.Invoke(point);
        public static void RaiseMatchEnded(PlayerId winner, MatchEndReason reason) => MatchEnded?.Invoke(winner, reason);
        public static void RaiseCraftMenuToggled(PlayerId p, bool open) => CraftMenuToggled?.Invoke(p, open);
        public static void RaiseRocketPieceBuilt(PlayerId p, PartType type) => RocketPieceBuilt?.Invoke(p, type);
        public static void RaiseRocketReady(PlayerId p, RocketRecipe r) => RocketReady?.Invoke(p, r);

        public static void RaiseRobotTransformed(int level) => RobotTransformed?.Invoke(level);
        public static void RaiseRobotWeakPoint(bool open) => RobotWeakPoint?.Invoke(open);
        public static void RaisePowerUpCollected(PlayerId p, PowerUpType type) => PowerUpCollected?.Invoke(p, type);
        public static void RaiseShieldBlocked(PlayerId owner, UnityEngine.Vector3 point) => ShieldBlocked?.Invoke(owner, point);

        public static void RaiseIslandHealed(PlayerId owner, int amount, int hp, int maxHp, bool announced) => IslandHealed?.Invoke(owner, amount, hp, maxHp, announced);
        public static void RaiseUpgradePicked(PlayerId p, UpgradeType type) => UpgradePicked?.Invoke(p, type);

        public static void Reset()
        {
            PhaseChanged = null; TierChanged = null; ClockTick = null; PartCollected = null;
            RocketLaunched = null; IslandDamaged = null; PlayerStunned = null; RobotRageChanged = null;
            RobotHit = null; GemIncoming = null; MatchEnded = null; CraftMenuToggled = null;
            RequestHowToPlay = null; RequestStartMatch = null; RequestRematch = null; RequestTitle = null;
            RocketPieceBuilt = null; RocketReady = null;
            RobotTransformed = null; RobotWeakPoint = null; PowerUpCollected = null; ShieldBlocked = null;
            IslandHealed = null; UpgradePicked = null;
        }
    }
}
