using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Volume-preserving squash &amp; stretch on a "Visual" child: a damped spring always pulls the scale back to rest,
    /// plus a gentle idle breathing. Call <see cref="Squash"/> on landings/pickups and <see cref="Hop"/> for a little jump.
    /// </summary>
    public class SquashStretch : MonoBehaviour
    {
        [Tooltip("Transform to deform. Defaults to the child named 'Visual'.")]
        public Transform visual;

        [Header("Spring")]
        public float stiffness = 220f;
        public float damping = 14f;
        [Tooltip("Clamp for the stretch amount (1 = double height).")]
        public float maxStretch = 0.6f;

        [Header("Idle breathing")]
        public float breatheAmount = 0.025f;
        public float breatheSpeed = 2.4f;

        [Header("Hop")]
        public float hopHeight = 0.18f;
        public float hopDuration = 0.28f;

        Vector3 restScale = Vector3.one;
        Vector3 restPosition;
        float stretch;      // + taller/thinner, - shorter/wider
        float velocity;
        float hopTime = -1f;
        float phase;

        void Awake()
        {
            if (visual == null) visual = transform.Find("Visual");
            if (visual == null) visual = transform;
            restScale = visual.localScale;
            restPosition = visual.localPosition;
            phase = Random.value * 10f;
        }

        /// <summary>Squash flat by <paramref name="amount"/> (0.2 = 20% shorter) and spring back.</summary>
        public void Squash(float amount)
        {
            stretch = Mathf.Clamp(stretch - Mathf.Abs(amount), -0.6f, maxStretch);
            velocity = 0f;
        }

        /// <summary>Stretch tall by <paramref name="amount"/> and spring back.</summary>
        public void Stretch(float amount)
        {
            stretch = Mathf.Clamp(stretch + Mathf.Abs(amount), -0.6f, maxStretch);
            velocity = 0f;
        }

        /// <summary>Small happy hop: stretch up, arc, squash on landing.</summary>
        public void Hop()
        {
            hopTime = 0f;
            velocity += 6f;
        }

        public bool IsHopping => hopTime >= 0f;

        void LateUpdate()
        {
            if (visual == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            // Damped spring toward 0.
            float accel = -stiffness * stretch - damping * velocity;
            velocity += accel * dt;
            stretch = Mathf.Clamp(stretch + velocity * dt, -0.6f, maxStretch);

            float breathe = Mathf.Sin((Time.time + phase) * breatheSpeed) * breatheAmount;
            float sy = 1f + stretch + breathe;
            float sxz = 1f / Mathf.Sqrt(Mathf.Max(sy, 0.2f));
            visual.localScale = new Vector3(restScale.x * sxz, restScale.y * sy, restScale.z * sxz);

            float yOffset = 0f;
            if (hopTime >= 0f)
            {
                hopTime += dt;
                float t = hopTime / Mathf.Max(hopDuration, 0.01f);
                if (t >= 1f)
                {
                    hopTime = -1f;
                    Squash(0.25f);
                }
                else
                {
                    yOffset = 4f * t * (1f - t) * hopHeight;
                }
            }
            visual.localPosition = restPosition + Vector3.up * yOffset;
        }

        void OnDisable()
        {
            if (visual == null) return;
            visual.localScale = restScale;
            visual.localPosition = restPosition;
        }
    }
}
