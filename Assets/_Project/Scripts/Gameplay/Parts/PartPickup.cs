using System.Collections;
using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// A collectible part resting on an island. Bobs and spins, blinks in its last 3 seconds, and is collected by
    /// any player standing within <see cref="collectRadius"/> on XZ. Added at runtime by <see cref="PartSpawner"/>.
    /// </summary>
    public class PartPickup : MonoBehaviour
    {
        public const float BlinkTime = 3f;

        public PartKey key;
        [Tooltip("XZ distance at which a player collects the part.")]
        public float collectRadius = 0.7f;
        public float bobHeight = 0.07f;
        public float hoverHeight = 0.15f;
        public float spinSpeed = 90f;

        PartSpawner spawner;
        Vector3 restPos;
        float lifeLeft;
        float age;
        bool finished;
        Renderer[] renderers;
        Vector3 baseScale;

        public bool IsFinished => finished;

        /// <summary>Slides the part toward a point (magnet power-up).</summary>
        public void PullToward(Vector3 point, float distance)
        {
            if (finished) return;
            point.y = restPos.y;
            restPos = Vector3.MoveTowards(restPos, point, distance);
        }

        public void Init(PartKey partKey, PartSpawner owner, Vector3 restPosition)
        {
            key = partKey;
            spawner = owner;
            restPos = restPosition;
            var cfg = GameplayUtil.Config;
            lifeLeft = cfg != null ? cfg.partLifetime : 15f;
            renderers = GetComponentsInChildren<Renderer>(true);
            baseScale = transform.localScale;
            age = Random.value * 10f;
        }

        void Update()
        {
            if (finished) return;
            var ctx = MatchContext.Instance;
            bool playing = ctx != null && ctx.IsPlaying;

            age += Time.deltaTime;
            transform.position = restPos + Vector3.up * (hoverHeight + bobHeight * Mathf.Sin(age * 2.5f));
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

            if (!playing) return;

            // collection
            for (int i = 0; i < 2; i++)
            {
                var pid = (PlayerId)i;
                Transform pt = ctx.Player(pid);
                if (pt == null) continue;
                if (Mathf.Abs(pt.position.y - restPos.y) > 2f) continue;
                if (GameplayUtil.DistanceXZ(pt.position, restPos) <= collectRadius * ctx.Upgrades(pid).PickupRadiusMultiplier)
                {
                    Collect(pid);
                    return;
                }
            }

            // lifetime + blink
            lifeLeft -= Time.deltaTime;
            if (lifeLeft <= 0f)
            {
                SetVisible(true);
                finished = true;
                StartCoroutine(ShrinkAway());
                return;
            }
            if (lifeLeft < BlinkTime)
            {
                float freq = Mathf.Lerp(10f, 4f, lifeLeft / BlinkTime);
                SetVisible(Mathf.Repeat(Time.time * freq, 1f) > 0.35f);
            }
        }

        void SetVisible(bool on)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
                if (r != null) r.enabled = on;
        }

        void Collect(PlayerId pid)
        {
            finished = true;
            SetVisible(true);
            var ctx = MatchContext.Instance;
            if (ctx != null) ctx.Inventory(pid).Add(key);
            GameEvents.RaisePartCollected(pid, key);
            StartCoroutine(Pop());
        }

        IEnumerator Pop()
        {
            Vector3 startPos = transform.position;
            float t = 0f;
            const float dur = 0.22f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float s = k < 0.4f ? Mathf.Lerp(1f, 1.45f, k / 0.4f) : Mathf.Lerp(1.45f, 0f, (k - 0.4f) / 0.6f);
                transform.localScale = baseScale * s;
                transform.position = startPos + Vector3.up * (0.6f * k);
                transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);
                yield return null;
            }
            Destroy(gameObject);
        }

        IEnumerator ShrinkAway()
        {
            float t = 0f;
            const float dur = 0.3f;
            while (t < dur)
            {
                t += Time.deltaTime;
                transform.localScale = baseScale * (1f - Mathf.Clamp01(t / dur));
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (spawner != null) spawner.Forget(gameObject);
        }
    }
}
