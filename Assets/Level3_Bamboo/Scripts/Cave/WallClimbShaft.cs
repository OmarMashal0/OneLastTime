using UnityEngine;

namespace Level3Bamboo
{
    // Wall-slide and wall-jump, but only on the escape shaft's own walls.
    // Put this on a trigger that fills the shaft and list the shaft walls in Climb Walls.
    //   - In the air, pressing toward one of those walls: Ryu slides down slowly.
    //   - Pressing Jump (a new press each time) while touching one: he is pushed up and away from it.
    //   - For a moment after the push, left/right input cannot cancel it.
    //   - Let go and he simply falls.
    // Runs after the team's PlayerMovement each physics step, so its velocity change is the one that counts.
    [DefaultExecutionOrder(50)]
    public class WallClimbShaft : MonoBehaviour
    {
        public BambooPlayerController player;
        public Rigidbody2D body;
        public Collider2D playerCollider;
        [Tooltip("Only these colliders can be climbed.")]
        public Collider2D[] climbWalls;

        public float slideSpeed = 1.6f;
        public Vector2 jumpVelocity = new Vector2(6.5f, 13f);
        [Tooltip("Seconds after a wall jump during which left/right input is ignored.")]
        public float pushSeconds = 0.18f;
        public float wallCheckDistance = 0.12f;

        public bool PlayerInside { get; private set; }

        private int queuedJumpSide;   // -1 = jump off a left wall, 1 = off a right wall, 0 = none
        private int pushSide;
        private float pushTimer;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.tag == "Player")
                PlayerInside = true;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.tag == "Player")
                PlayerInside = false;
        }

        void Update()
        {
            if (!CanClimb())
                return;

            bool jumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump");
            int side = TouchingWallSide();
            if (jumpPressed && side != 0)
                queuedJumpSide = side;
        }

        void FixedUpdate()
        {
            if (!CanClimb())
            {
                queuedJumpSide = 0;
                pushTimer = 0f;
                return;
            }

            if (queuedJumpSide != 0)
            {
                // Push up and away from the wall, and face the way he is going.
                pushSide = -queuedJumpSide;
                body.velocity = new Vector2(pushSide * jumpVelocity.x, jumpVelocity.y);
                Vector3 scale = player.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * pushSide;
                player.transform.localScale = scale;
                pushTimer = pushSeconds;
                queuedJumpSide = 0;
                return;
            }

            if (pushTimer > 0f)
            {
                pushTimer -= Time.fixedDeltaTime;
                body.velocity = new Vector2(pushSide * jumpVelocity.x, body.velocity.y);
                return;
            }

            // Slow slide while pressing toward the wall he is touching.
            int side = TouchingWallSide();
            float input = Input.GetAxisRaw("Horizontal");
            if (side != 0 && input * side > 0f && body.velocity.y < -slideSpeed)
                body.velocity = new Vector2(body.velocity.x, -slideSpeed);
        }

        private bool CanClimb()
        {
            return PlayerInside && Time.timeScale > 0f && !player.IsDead && !player.ControlsLocked && !player.IsGrounded;
        }

        // -1 if a climbable wall is just left of Ryu, 1 if just right, 0 if none.
        private int TouchingWallSide()
        {
            if (IsClimbWall(Vector2.left)) return -1;
            if (IsClimbWall(Vector2.right)) return 1;
            return 0;
        }

        private bool IsClimbWall(Vector2 direction)
        {
            Bounds b = playerCollider.bounds;
            float distance = b.extents.x + wallCheckDistance;
            foreach (float height in new[] { 0f, b.extents.y * 0.5f, -b.extents.y * 0.5f })
            {
                Vector2 origin = (Vector2)b.center + Vector2.up * height;
                foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, direction, distance))
                {
                    if (System.Array.IndexOf(climbWalls, hit.collider) >= 0)
                        return true;
                }
            }
            return false;
        }
    }
}
