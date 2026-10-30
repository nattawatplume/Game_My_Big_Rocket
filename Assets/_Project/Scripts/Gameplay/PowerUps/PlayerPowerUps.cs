using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Timed power-up effects on one player (added the first time they collect one). Speed raises the walk speed,
    /// magnet pulls nearby parts in, shield goes on the island. Shows what is active in a label above the head.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerPowerUps : MonoBehaviour
    {
        PlayerController player;
        TextMesh label;
        float speedLeft;
        float magnetLeft;
        IslandHealth island;

        void Awake()
        {
            player = GetComponent<PlayerController>();
            label = GameplayUtil.CreateTextMesh(null, Vector3.zero, 0.06f, Palette.Ink);
            label.gameObject.name = "PowerUpLabel_" + player.id;
            label.text = "";
        }

        void OnEnable() { GameEvents.PhaseChanged += OnPhaseChanged; }
        void OnDisable() { GameEvents.PhaseChanged -= OnPhaseChanged; }

        void OnDestroy()
        {
            if (label != null) Destroy(label.gameObject);
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Playing || phase == GamePhase.SuddenDeath) return;
            speedLeft = magnetLeft = 0f;
            player.SpeedMultiplier = 1f;
            if (label != null) label.text = "";
        }

        public void Apply(PowerUpType type)
        {
            var cfg = GameplayUtil.Config;
            if (cfg == null) return;
            switch (type)
            {
                case PowerUpType.Shield:
                    if (MatchContext.Instance.Island(player.id) is IslandHealth own) island = own;
                    if (island != null) island.GiveShield();
                    break;
                case PowerUpType.Speed:
                    speedLeft = cfg.speedBoostDuration;
                    player.SpeedMultiplier = cfg.speedBoostMultiplier;
                    break;
                case PowerUpType.Magnet:
                    magnetLeft = cfg.magnetDuration;
                    break;
            }
            string name = type == PowerUpType.Shield ? "SHIELD!" : type == PowerUpType.Speed ? "SPEED UP!" : "MAGNET!";
            RobotFloatingText.Spawn(name, transform.position + Vector3.up * 2.6f, Palette.HelmetOrange, 0.07f, 1f, 0.8f);
            RobotFx.Burst("PowerUpGet", transform.position + Vector3.up, RobotFx.DotMaterial, 30,
                Palette.HelmetYellow, Palette.Cream, 2f, 4f, 0.1f, 0.25f, 0.4f, 0.8f, 0.3f);
        }

        void Update()
        {
            var ctx = MatchContext.Instance;
            if (ctx == null || !ctx.IsPlaying) return;
            float dt = Time.deltaTime;

            if (speedLeft > 0f)
            {
                speedLeft -= dt;
                if (speedLeft <= 0f) player.SpeedMultiplier = 1f;
            }
            if (magnetLeft > 0f)
            {
                magnetLeft -= dt;
                PullParts(ctx.config);
            }

            UpdateLabel();
        }

        void PullParts(GameConfig cfg)
        {
            foreach (var pickup in FindObjectsByType<PartPickup>(FindObjectsSortMode.None))
                if (GameplayUtil.DistanceXZ(pickup.transform.position, transform.position) <= cfg.magnetRadius)
                    pickup.PullToward(transform.position, cfg.magnetPullSpeed * Time.deltaTime);
        }

        void UpdateLabel()
        {
            string text = "";
            if (speedLeft > 0f) text += "SPEED " + Mathf.CeilToInt(speedLeft);
            if (magnetLeft > 0f) text += (text.Length > 0 ? "  " : "") + "MAGNET " + Mathf.CeilToInt(magnetLeft);
            label.text = text;
            label.transform.position = transform.position + Vector3.up * 2.3f;
            GameplayUtil.Billboard(label.transform);
        }
    }
}
