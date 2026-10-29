using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Sine bob on local Y with optional Y spin and a little rocking tilt (parts, ducks).
    /// Applied as a delta each frame, so other code can still move/rotate the same transform.
    /// </summary>
    public class Bobbing : MonoBehaviour
    {
        public float amplitude = 0.06f;
        [Tooltip("Cycles per second.")]
        public float frequency = 1.2f;
        [Tooltip("Constant lift added to the bob (keeps pickups above the ground).")]
        public float heightOffset = 0f;
        [Tooltip("Degrees per second around local Y.")]
        public float spinSpeed = 0f;
        [Tooltip("Rocking tilt in degrees.")]
        public float tiltAmount = 0f;
        public bool randomPhase = true;

        Vector3 lastOffset;
        Quaternion lastRot = Quaternion.identity;
        float phase;
        float spin;

        void Awake()
        {
            phase = randomPhase ? Random.value * Mathf.PI * 2f : 0f;
        }

        void Update()
        {
            float t = Time.time * frequency * Mathf.PI * 2f + phase;
            Vector3 offset = Vector3.up * (heightOffset + Mathf.Sin(t) * amplitude);
            transform.localPosition += offset - lastOffset;
            lastOffset = offset;

            spin = (spin + spinSpeed * Time.deltaTime) % 360f;
            Quaternion rot = Quaternion.Euler(0f, spin, 0f);
            if (tiltAmount != 0f)
                rot *= Quaternion.Euler(Mathf.Sin(t * 0.8f) * tiltAmount, 0f, Mathf.Cos(t * 0.6f) * tiltAmount * 0.6f);
            transform.localRotation = transform.localRotation * Quaternion.Inverse(lastRot) * rot;
            lastRot = rot;
        }

        void OnDisable()
        {
            // Remove what we added so the object rests where it was placed.
            transform.localPosition -= lastOffset;
            transform.localRotation = transform.localRotation * Quaternion.Inverse(lastRot);
            lastOffset = Vector3.zero;
            lastRot = Quaternion.identity;
        }
    }
}
