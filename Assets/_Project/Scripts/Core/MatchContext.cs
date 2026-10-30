using System.Collections.Generic;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Scene registry so independent modules can find each other without hard references.
    /// Islands, players and the robot register themselves in OnEnable.
    /// </summary>
    public class MatchContext : MonoBehaviour
    {
        public static MatchContext Instance { get; private set; }

        public GameConfig config;
        public RocketRecipe[] recipes = new RocketRecipe[5];

        readonly Dictionary<PlayerId, IRocketTarget> islands = new Dictionary<PlayerId, IRocketTarget>();
        readonly Dictionary<PlayerId, Transform> players = new Dictionary<PlayerId, Transform>();
        readonly Dictionary<PlayerId, Inventory> inventories = new Dictionary<PlayerId, Inventory>
        {
            { PlayerId.P1, new Inventory() },
            { PlayerId.P2, new Inventory() },
        };

        readonly Dictionary<PlayerId, CraftMenuState> craftMenus = new Dictionary<PlayerId, CraftMenuState>
        {
            { PlayerId.P1, new CraftMenuState(PlayerId.P1) },
            { PlayerId.P2, new CraftMenuState(PlayerId.P2) },
        };
        readonly Dictionary<PlayerId, PlayerUpgrades> upgrades = new Dictionary<PlayerId, PlayerUpgrades>
        {
            { PlayerId.P1, new PlayerUpgrades() },
            { PlayerId.P2, new PlayerUpgrades() },
        };
        readonly Dictionary<PlayerId, IPartSpawner> partSpawners = new Dictionary<PlayerId, IPartSpawner>();

        public IRocketTarget Robot { get; private set; }
        public Transform RobotTransform { get; private set; }
        public GamePhase Phase { get; set; } = GamePhase.Title;
        public float Elapsed { get; set; }
        public PartTier CurrentTier => config != null ? config.TierAt(Elapsed) : PartTier.Scrap;
        public bool IsPlaying => Phase == GamePhase.Playing || Phase == GamePhase.SuddenDeath;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void RegisterIsland(PlayerId owner, IRocketTarget island) => islands[owner] = island;
        public void RegisterPlayer(PlayerId id, Transform player) => players[id] = player;
        public void RegisterRobot(IRocketTarget robot, Transform robotTransform)
        {
            Robot = robot;
            RobotTransform = robotTransform;
        }
        public void RegisterPartSpawner(PlayerId island, IPartSpawner spawner) => partSpawners[island] = spawner;

        public IRocketTarget Island(PlayerId owner) => islands.TryGetValue(owner, out var i) ? i : null;
        public Transform Player(PlayerId id) => players.TryGetValue(id, out var p) ? p : null;
        public Inventory Inventory(PlayerId id) => inventories[id];
        public CraftMenuState CraftMenu(PlayerId id) => craftMenus[id];
        public PlayerUpgrades Upgrades(PlayerId id) => upgrades[id];
        public IPartSpawner PartSpawner(PlayerId island) => partSpawners.TryGetValue(island, out var s) ? s : null;

        public IRocketTarget Target(PlayerId shooter, TargetKind kind)
            => kind == TargetKind.Robot ? Robot : Island(shooter.Opponent());

        public void ResetMatch()
        {
            Elapsed = 0f;
            foreach (var inv in inventories.Values) inv.Clear();
            foreach (var menu in craftMenus.Values) menu.Reset();
            foreach (var u in upgrades.Values) u.Reset();
        }
    }
}
