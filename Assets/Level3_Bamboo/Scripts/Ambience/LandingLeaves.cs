using UnityEngine;

namespace Level3Bamboo
{
    // Put on the "Landing Leaf Burst" particle system. When Ryu lands on the forest ground after a real drop
    // (not a small hop), a few leaves burst out from under his feet.
    public class LandingLeaves : MonoBehaviour
    {
        public BambooPlayerController player;
        public ParticleSystem leaves;
        [Tooltip("Only landings on this ground object (or its children) make leaves.")]
        public Transform forestGround;
        public float minDropHeight = 1.2f;
        public float strongLandingSpeed = 13.5f;
        public float cooldown = 0.9f;
        public Vector2Int leafCount = new Vector2Int(3, 5);

        private Rigidbody2D playerBody;
        private Collider2D playerCollider;
        private bool inAir;
        private float takeoffY;
        private float fastestFall;
        private float readyTime;

        private void OnEnable()
        {
            player.Respawned += OnRespawned;
        }

        private void OnDisable()
        {
            player.Respawned -= OnRespawned;
        }

        // Start is called before the first frame update
        void Start()
        {
            playerBody = player.GetComponent<Rigidbody2D>();
            playerCollider = player.GetComponent<Collider2D>();
            readyTime = Time.time + 0.75f;
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f)
                return;
            if (player.IsDead)
            {
                inAir = false;
                return;
            }

            float y = player.transform.position.y;
            if (!player.IsGrounded)
            {
                // Remember where the jump or fall started and how fast Ryu fell.
                if (!inAir)
                {
                    inAir = true;
                    takeoffY = y;
                    fastestFall = 0f;
                }
                fastestFall = Mathf.Min(fastestFall, playerBody.velocity.y);
                return;
            }

            if (!inAir)
                return;

            // Ryu has just landed.
            inAir = false;
            bool bigLanding = takeoffY - y >= minDropHeight || -fastestFall >= strongLandingSpeed;
            if (!bigLanding || Time.time < readyTime || !StandingOnForestGround())
                return;

            readyTime = Time.time + cooldown;
            leaves.transform.position = new Vector3(playerCollider.bounds.center.x, playerCollider.bounds.min.y + 0.04f, 0f);
            leaves.Emit(Random.Range(leafCount.x, leafCount.y + 1));
        }

        private bool StandingOnForestGround()
        {
            Bounds feet = playerCollider.bounds;
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(feet.center.x, feet.min.y + 0.05f), Vector2.down, 0.4f);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger || hit.transform.IsChildOf(player.transform))
                    continue;
                return hit.transform.IsChildOf(forestGround);
            }
            return false;
        }

        private void OnRespawned()
        {
            inAir = false;
            readyTime = Time.time + 0.75f;
        }
    }
}
