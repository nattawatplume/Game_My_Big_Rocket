using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Additive camera shake. Call <see cref="Shake"/> from anywhere; the component adds itself to Camera.main lazily.
    /// Works on top of other camera movement by removing last frame's offset before applying a new one.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraShake : MonoBehaviour
    {
        [Tooltip("Perlin noise speed of the shake.")]
        public float frequency = 28f;

        float intensity;
        float timeLeft;
        float totalTime;
        Vector3 lastOffset;
        float seed;

        public static void Shake(float intensity, float duration)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var shake = cam.GetComponent<CameraShake>();
            if (shake == null) shake = cam.gameObject.AddComponent<CameraShake>();
            shake.AddShake(intensity, duration);
        }

        /// <summary>Stops any shake on the main camera immediately.</summary>
        public static void StopAll()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var shake = cam.GetComponent<CameraShake>();
            if (shake != null) shake.StopShake();
        }

        void Awake()
        {
            seed = Random.value * 100f;
        }

        public void AddShake(float amount, float duration)
        {
            if (amount <= 0f || duration <= 0f) return;
            // Keep the stronger of the current and the new shake, extend time.
            float currentStrength = totalTime > 0f ? intensity * (timeLeft / totalTime) : 0f;
            intensity = Mathf.Max(currentStrength, amount);
            timeLeft = Mathf.Max(timeLeft, duration);
            totalTime = timeLeft;
        }

        public void StopShake()
        {
            transform.localPosition -= lastOffset;
            lastOffset = Vector3.zero;
            timeLeft = 0f;
            intensity = 0f;
        }

        void LateUpdate()
        {
            transform.localPosition -= lastOffset;
            lastOffset = Vector3.zero;
            if (timeLeft <= 0f) return;

            timeLeft -= Time.unscaledDeltaTime;
            float k = totalTime > 0f ? Mathf.Clamp01(timeLeft / totalTime) : 0f;
            float strength = intensity * k * k;
            float t = Time.unscaledTime * frequency;
            float x = (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(seed + 37.1f, t) - 0.5f) * 2f;

            Vector3 worldOffset = (transform.right * x + transform.up * y) * strength;
            Vector3 local = transform.parent != null ? transform.parent.InverseTransformVector(worldOffset) : worldOffset;
            lastOffset = local;
            transform.localPosition += lastOffset;
        }

        void OnDisable()
        {
            transform.localPosition -= lastOffset;
            lastOffset = Vector3.zero;
        }
    }
}
