using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>Seagull gliding in a lazy circle around its start position, banking and flapping its wings now and then.</summary>
    public class SeagullCircle : MonoBehaviour
    {
        public float radius = 6f;
        [Tooltip("Degrees per second around the circle (negative = clockwise).")]
        public float angularSpeed = 22f;
        public float bobAmplitude = 0.35f;
        public float bobSpeed = 0.7f;
        public float bankAngle = 15f;

        [Header("Wings")]
        public Transform wingL;
        public Transform wingR;
        public float flapSpeed = 9f;
        public float flapAngle = 32f;
        [Tooltip("Fraction of the time spent flapping (rest is gliding).")]
        [Range(0f, 1f)] public float flapDuty = 0.45f;

        Vector3 center;
        float angle;
        float phase;

        void Start()
        {
            // Start (not Awake) so a position set right after Instantiate becomes the circle center.
            center = transform.position;
            angle = Random.value * 360f;
            phase = Random.value * 10f;
            if (wingL == null) wingL = FindDeep(transform, "WingL");
            if (wingR == null) wingR = FindDeep(transform, "WingR");
        }

        static Transform FindDeep(Transform t, string name)
        {
            foreach (var c in t.GetComponentsInChildren<Transform>(true))
                if (c.name == name) return c;
            return null;
        }

        void Update()
        {
            angle += angularSpeed * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            float bob = Mathf.Sin((Time.time + phase) * bobSpeed) * bobAmplitude;
            transform.position = center + new Vector3(Mathf.Cos(rad) * radius, bob, Mathf.Sin(rad) * radius);

            float sign = Mathf.Sign(angularSpeed == 0f ? 1f : angularSpeed);
            Vector3 tangent = new Vector3(-Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * sign;
            transform.rotation = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0f, 0f, bankAngle * sign);

            float cycle = Mathf.Repeat((Time.time + phase) * 0.25f, 1f);
            float flap = cycle < flapDuty ? Mathf.Sin((Time.time + phase) * flapSpeed) * flapAngle : 4f;
            if (wingL != null) wingL.localRotation = Quaternion.Euler(0f, 0f, -flap);
            if (wingR != null) wingR.localRotation = Quaternion.Euler(0f, 0f, flap);
        }
    }
}
