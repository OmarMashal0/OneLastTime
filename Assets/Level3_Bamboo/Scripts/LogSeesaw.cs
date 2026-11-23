using UnityEngine;

namespace Level3Bamboo
{
    // Rock and seesaw puzzle: Ryu pushes the rock to the right onto the low end of a tilting board.
    // The rock's weight tips the board, lifting the far end into a step Ryu can climb to reach Log 2.
    public class LogSeesaw : MonoBehaviour
    {
        public BambooPlayerController player;
        public Rigidbody2D playerBody;
        public Collider2D playerCollider;
        public CollectibleLog log;
        public Transform rock;
        [Tooltip("Kinematic, interpolated body on the rock, so physics sees it move instead of treating it as a wall.")]
        public Rigidbody2D rockBody;
        public Transform plank;
        public Transform loadedRock;
        public Collider2D raisedStep;
        public float pushSpeed = 2.7f;
        public float tiltDegrees = 15f;
        public float settleSeconds = 0.45f;

        private Vector3 loadedFrom;
        private float tilt;
        private bool weighted;

        // Start is called before the first frame update
        void Start()
        {
            // The rock has a Kinematic, interpolated Rigidbody2D (set up in the Inspector) and is moved with
            // MovePosition. A collider moved through its Transform has no velocity, so Ryu's contact used to
            // stop him every physics step and the push stuttered.
            raisedStep.enabled = false;
            raisedStep.GetComponent<SpriteRenderer>().enabled = false;
        }

        // Update is called once per frame
        void Update()
        {
        }

        void FixedUpdate()
        {
            if (!weighted)
            {
                // Ryu pushes from the left using the existing movement velocity.
                float gap = rockBody.position.x - playerCollider.bounds.max.x;
                // Ryu pushes while he is pressing toward the rock. His velocity can't be the test:
                // contact with the rock clamps it every step, which is what made the push stutter.
                bool pressing = Input.GetAxisRaw("Horizontal") > 0.1f || playerBody.velocity.x > 0.1f;
                if (!player.IsDead && !player.IsAttacking && player.IsGrounded && pressing
                    && gap >= -0.06f && gap <= 0.50f
                    && Mathf.Abs(playerCollider.bounds.min.y - rockBody.position.y) < 0.28f)
                {
                    Vector2 position = rockBody.position;
                    position.x = Mathf.MoveTowards(position.x, loadedRock.position.x, pushSpeed * Time.fixedDeltaTime);
                    rockBody.MovePosition(position);
                    weighted = position.x >= loadedRock.position.x;
                    loadedFrom = new Vector3(position.x, position.y, rock.position.z);
                }
                return;
            }

            if (tilt >= 1f)
                return;

            tilt = Mathf.MoveTowards(tilt, 1f, Time.fixedDeltaTime / Mathf.Max(0.01f, settleSeconds));
            float blend = Mathf.SmoothStep(0f, 1f, tilt);
            plank.localRotation = Quaternion.Euler(0f, 0f, tiltDegrees * blend);
            rockBody.MovePosition(Vector3.Lerp(loadedFrom, loadedRock.position, blend));
            if (tilt >= 1f)
            {
                raisedStep.enabled = true;
                raisedStep.GetComponent<SpriteRenderer>().enabled = true;
                log.locked = false;
            }
        }
    }
}
