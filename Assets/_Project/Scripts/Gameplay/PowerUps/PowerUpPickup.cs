using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>A floating power-up icon. Collected by its island's player; blinks before it disappears.</summary>
    public class PowerUpPickup : MonoBehaviour
    {
        const float CollectRadius = 0.85f;
        const float BlinkTime = 3f;

        PowerUpType type;
        PlayerId owner;
        Vector3 rest;
        float lifeLeft;
        float age;
        Renderer[] renderers;

        public void Init(PowerUpType powerUp, PlayerId islandOwner, Vector3 position)
        {
            type = powerUp;
            owner = islandOwner;
            rest = position;
            var cfg = GameplayUtil.Config;
            lifeLeft = cfg != null ? cfg.powerUpLifetime : 12f;
            renderers = GetComponentsInChildren<Renderer>(true);
            RobotFx.Burst("PowerUpAppear", position + Vector3.up, RobotFx.RingMaterial, 8,
                Palette.Cream, Palette.HelmetYellow, 0.6f, 1.4f, 0.25f, 0.45f, 0.4f, 0.7f, -0.05f);
        }

        void Update()
        {
            var ctx = MatchContext.Instance;
            if (ctx == null || !ctx.IsPlaying) return;

            age += Time.deltaTime;
            transform.position = rest + Vector3.up * (0.12f * Mathf.Sin(age * 3f));
            transform.Rotate(0f, 140f * Time.deltaTime, 0f, Space.World);

            Transform player = ctx.Player(owner);
            if (player != null && GameplayUtil.DistanceXZ(player.position, rest) <= CollectRadius * ctx.Upgrades(owner).PickupRadiusMultiplier)
            {
                var effects = player.GetComponent<PlayerPowerUps>();
                if (effects == null) effects = player.gameObject.AddComponent<PlayerPowerUps>();
                effects.Apply(type);
                GameEvents.RaisePowerUpCollected(owner, type);
                Destroy(gameObject);
                return;
            }

            lifeLeft -= Time.deltaTime;
            if (lifeLeft <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            if (lifeLeft < BlinkTime)
            {
                bool on = Mathf.Repeat(Time.time * 7f, 1f) > 0.35f;
                foreach (var r in renderers)
                    if (r != null) r.enabled = on;
            }
        }
    }
}
