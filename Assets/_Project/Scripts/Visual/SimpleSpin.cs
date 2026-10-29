using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>Spins at a constant speed around an axis.</summary>
    public class SimpleSpin : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        [Tooltip("Degrees per second.")]
        public float speed = 90f;
        public Space space = Space.Self;

        void Update()
        {
            transform.Rotate(axis, speed * Time.deltaTime, space);
        }
    }
}
