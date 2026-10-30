using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// One per player island, next to its PartSpawner. Every GameConfig.powerUpInterval seconds places one random
    /// power-up on the island (at most one at a time).
    /// </summary>
    public class PowerUpSpawner : MonoBehaviour
    {
        [Tooltip("Prefabs indexed by PowerUpType: Shield, Speed, Magnet.")]
        public GameObject[] prefabs = new GameObject[3];

        PartSpawner parts;
        PowerUpPickup current;
        float timer;

        void Awake()
        {
            parts = GetComponent<PartSpawner>();
        }

        void OnEnable()
        {
            GameEvents.PhaseChanged += OnPhaseChanged;
        }

        void OnDisable()
        {
            GameEvents.PhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Playing || phase == GamePhase.SuddenDeath) return;
            if (current != null) Destroy(current.gameObject);
            current = null;
            timer = 0f;
        }

        void Update()
        {
            var ctx = MatchContext.Instance;
            if (ctx == null || !ctx.IsPlaying || ctx.config == null || current != null) return;

            timer += Time.deltaTime;
            if (timer >= ctx.config.powerUpInterval)
            {
                timer = Random.Range(-3f, 3f);
                SpawnNow();
            }
        }

        public void SpawnNow()
        {
            if (current != null || parts == null || !parts.TryFindFreeSpot(out Vector3 spot)) return;
            var type = (PowerUpType)Random.Range(0, 3);
            var prefab = prefabs != null && (int)type < prefabs.Length ? prefabs[(int)type] : null;
            if (prefab == null) return;

            var go = Instantiate(prefab, spot, Quaternion.identity, transform);
            go.transform.localScale *= 1.5f;
            current = go.AddComponent<PowerUpPickup>();
            current.Init(type, parts.owner, spot);
        }
    }
}
