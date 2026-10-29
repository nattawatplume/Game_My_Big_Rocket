using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>Billboard: keeps this transform facing the main camera (optionally rotating around Y only).</summary>
    public class FaceCamera : MonoBehaviour
    {
        public bool yAxisOnly;
        public Camera targetCamera;

        void LateUpdate()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;

            if (yAxisOnly)
            {
                Vector3 fwd = cam.transform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
            else
            {
                // Match the camera orientation (works for orthographic and perspective cameras).
                transform.rotation = cam.transform.rotation;
            }
        }
    }
}
