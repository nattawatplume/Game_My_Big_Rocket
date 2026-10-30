using UnityEngine;

namespace IslandRocketRumble
{
    /// <summary>
    /// Kinematic isometric walker for one player. W/Up moves "up-screen" relative to the main camera.
    /// Clamped to the island square, faces its move direction, squashes child "Visual" while walking.
    /// Does not walk while stunned, while its craft menu is open, or outside the Playing/SuddenDeath phases.
    /// </summary>
    public class PlayerController : MonoBehaviour, IStunnable
    {
        [Tooltip("Which keyboard half controls this character.")]
        public PlayerId id;
        [Tooltip("Top-surface center of this player's island. If empty, the spawn position is used.")]
        public Transform islandCenter;
        [Tooltip("Max distance from the island center on X and Z.")]
        public float walkLimit = 3.6f;
        [Tooltip("Optional spawn point. If empty, the position at Awake is used.")]
        public Transform spawnPoint;
        [Tooltip("Child scaled for squash & stretch. Defaults to child 'Visual'.")]
        public Transform visual;
        [Tooltip("Degrees per second when turning to face the move direction.")]
        public float turnSpeed = 720f;
        [Tooltip("Walking hops per second.")]
        public float hopRate = 4.5f;
        [Tooltip("Hop height of the visual while walking.")]
        public float hopHeight = 0.08f;

        float stunTimer;
        float hopPhase;
        float moveBlend;
        Vector3 spawnPos;
        Quaternion spawnRot;
        Vector3 visualBaseScale = Vector3.one;
        Vector3 visualBasePos;

        /// <summary>Set by the speed power-up.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        public bool IsStunned => stunTimer > 0f;
        public float StunRemaining => Mathf.Max(0f, stunTimer);
        public bool IsMoving { get; private set; }

        /// <summary>True while this player's craft menu is open (movement keys drive the menu instead).</summary>
        public bool MenuOpen => MatchContext.Instance != null && MatchContext.Instance.CraftMenu(id).IsOpen;

        void Awake()
        {
            spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
            spawnRot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
            if (visual == null) visual = transform.Find("Visual");
            if (visual != null)
            {
                visualBaseScale = visual.localScale;
                visualBasePos = visual.localPosition;
            }
        }

        void OnEnable() { Register(); }
        void Start() { Register(); }

        void Register()
        {
            if (MatchContext.Instance != null) MatchContext.Instance.RegisterPlayer(id, transform);
        }

        public void Stun(float seconds)
        {
            if (seconds <= 0f) return;
            stunTimer = Mathf.Max(stunTimer, seconds);
            GameEvents.RaisePlayerStunned(id, seconds);
        }

        /// <summary>Back to spawn, not stunned, visual reset.</summary>
        public void ResetToSpawn()
        {
            stunTimer = 0f;
            SpeedMultiplier = 1f;
            hopPhase = 0f;
            moveBlend = 0f;
            IsMoving = false;
            Vector3 p = spawnPoint != null ? spawnPoint.position : spawnPos;
            Quaternion r = spawnPoint != null ? spawnPoint.rotation : spawnRot;
            transform.SetPositionAndRotation(p, r);
            if (visual != null)
            {
                visual.localScale = visualBaseScale;
                visual.localPosition = visualBasePos;
            }
        }

        void Update()
        {
            if (stunTimer > 0f) stunTimer -= Time.deltaTime;

            var ctx = MatchContext.Instance;
            bool canMove = ctx != null && ctx.IsPlaying && !IsStunned && !MenuOpen;

            Vector3 dir = Vector3.zero;
            if (canMove)
            {
                Vector2 input = PlayerInput2P.Move(id);
                if (input.sqrMagnitude > 0.0001f) dir = CameraRelative(input);
            }

            IsMoving = dir.sqrMagnitude > 0.0001f;
            if (IsMoving)
            {
                float speed = ctx != null && ctx.config != null ? ctx.config.moveSpeed : 4.5f;
                speed *= SpeedMultiplier;
                Vector3 p = transform.position + dir * (speed * Time.deltaTime);
                Vector3 c = islandCenter != null ? islandCenter.position : spawnPos;
                p.x = Mathf.Clamp(p.x, c.x - walkLimit, c.x + walkLimit);
                p.z = Mathf.Clamp(p.z, c.z - walkLimit, c.z + walkLimit);
                p.y = c.y;
                transform.position = p;

                Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
            }

            AnimateVisual();
        }

        Vector3 CameraRelative(Vector2 input)
        {
            Vector3 fwd = Vector3.forward;
            Vector3 right = Vector3.right;
            var cam = Camera.main;
            if (cam != null)
            {
                Transform ct = cam.transform;
                fwd = ct.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.0001f)
                {
                    // camera looking straight down: use its up vector as "up-screen"
                    fwd = ct.up;
                    fwd.y = 0f;
                }
                right = ct.right;
                right.y = 0f;
                if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
                if (right.sqrMagnitude < 0.0001f) right = Vector3.Cross(Vector3.up, fwd);
                fwd.Normalize();
                right.Normalize();
            }
            Vector3 dir = right * input.x + fwd * input.y;
            return Vector3.ClampMagnitude(dir, 1f);
        }

        void AnimateVisual()
        {
            if (visual == null) return;

            moveBlend = Mathf.MoveTowards(moveBlend, IsMoving ? 1f : 0f, Time.deltaTime * 6f);
            if (IsMoving) hopPhase += Time.deltaTime * hopRate * Mathf.PI;
            else if (moveBlend <= 0f) hopPhase = 0f;

            float bounce = Mathf.Abs(Mathf.Sin(hopPhase)) * moveBlend;
            float sy = 1f + 0.12f * bounce;
            float sxz = 1f - 0.06f * bounce;

            if (IsStunned)
            {
                // dizzy wobble
                float w = Mathf.Sin(Time.time * 14f) * 0.05f;
                sy *= 1f - 0.08f;
                sxz *= 1f + w;
            }

            visual.localScale = new Vector3(visualBaseScale.x * sxz, visualBaseScale.y * sy, visualBaseScale.z * sxz);
            visual.localPosition = visualBasePos + Vector3.up * (hopHeight * bounce);
        }
    }
}
