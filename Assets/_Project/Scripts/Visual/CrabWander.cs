using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Tiny crab that scuttles sideways around its start point (in the parent's space, so it rides along with its island).
    /// <see cref="Scare"/> makes it dash away from a world point for a moment.
    /// </summary>
    public class CrabWander : MonoBehaviour
    {
        public float radius = 0.8f;
        public float speed = 0.5f;
        public float fleeSpeed = 2.2f;
        public float fleeTime = 1.1f;
        [Tooltip("Never wander/flee farther than this from home.")]
        public float maxDistance = 1.6f;
        public float pauseMin = 0.4f;
        public float pauseMax = 1.8f;

        Vector3 home;
        Vector3 target;
        float pause;
        float fleeTimer;
        Vector3 fleeDir;
        Transform visual;
        float wiggle;

        void Start()
        {
            home = transform.localPosition;
            target = home;
            visual = transform.Find("Visual");
            pause = Random.Range(pauseMin, pauseMax);
        }

        /// <summary>Run away from <paramref name="from"/> (world space).</summary>
        public void Scare(Vector3 from)
        {
            Vector3 localFrom = transform.parent != null ? transform.parent.InverseTransformPoint(from) : from;
            Vector3 d = transform.localPosition - localFrom;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-4f) d = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            fleeDir = d.normalized;
            fleeTimer = fleeTime;
            pause = 0f;
        }

        public bool IsFleeing => fleeTimer > 0f;

        void Update()
        {
            float dt = Time.deltaTime;
            Vector3 pos = transform.localPosition;
            Vector3 move = Vector3.zero;

            if (fleeTimer > 0f)
            {
                fleeTimer -= dt;
                move = fleeDir * fleeSpeed;
                if (fleeTimer <= 0f)
                {
                    target = pos;
                    pause = Random.Range(pauseMin, pauseMax);
                }
            }
            else if (pause > 0f)
            {
                pause -= dt;
                if (pause <= 0f)
                {
                    Vector2 r = Random.insideUnitCircle * radius;
                    target = home + new Vector3(r.x, 0f, r.y);
                }
            }
            else
            {
                Vector3 to = target - pos;
                to.y = 0f;
                if (to.magnitude < 0.03f) pause = Random.Range(pauseMin, pauseMax);
                else move = to.normalized * speed;
            }

            if (move.sqrMagnitude > 1e-6f)
            {
                pos += move * dt;
                Vector3 off = pos - home;
                off.y = 0f;
                if (off.magnitude > maxDistance) off = off.normalized * maxDistance;
                pos = new Vector3(home.x + off.x, home.y, home.z + off.z);
                transform.localPosition = pos;

                // Crabs walk sideways: keep local X (right) along the movement direction.
                Vector3 dir = move.normalized;
                Vector3 fwd = Vector3.Cross(dir, Vector3.up);
                if (Vector3.Dot(fwd, transform.localRotation * Vector3.forward) < 0f) fwd = -fwd;
                Quaternion want = Quaternion.LookRotation(fwd, Vector3.up);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, want, 1f - Mathf.Exp(-8f * dt));

                wiggle += dt * (fleeTimer > 0f ? 40f : 22f);
            }

            if (visual != null)
            {
                float w = move.sqrMagnitude > 1e-6f ? Mathf.Abs(Mathf.Sin(wiggle)) * 0.025f : 0f;
                visual.localPosition = new Vector3(0f, w, 0f);
                visual.localRotation = Quaternion.Euler(0f, 0f, move.sqrMagnitude > 1e-6f ? Mathf.Sin(wiggle) * 6f : 0f);
            }
        }
    }
}
