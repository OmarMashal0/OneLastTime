using UnityEngine;

namespace Level3Bamboo.River
{
    // One mossy log in the river. Ryu can stand on its Box Collider 2D.
    // The first time he lands on top, a countdown starts (only once). The crack warning shows straight away,
    // and after breakSeconds (1) it breaks, even if Ryu has already jumped away: the collider switches off,
    // the Break animation plays and the water ripple under it is hidden at once, so nothing is left
    // floating where the log used to be. (The logs have no Particle Systems of their own; the short
    // break splash is spawned by RiverCrossing and removed after 0.75 seconds or on respawn.)
    // The pictures come from three ordinary Animator clips on the Art child: Float, Crack and Break.
    // When it breaks, the picture also sinks and fades into the water.
    // Moving log (optional, Lab 5 RisingPlatform idea): if Point A and Point B are set, the log glides
    // between them, and Ryu is made a child of the log while he stands on it so he is carried along.
    // It stops moving when it breaks.
    // When Ryu respawns, RiverCrossing calls ResetLog so the log floats again for the next try.
    public class FloatingRiverLog : MonoBehaviour
    {
        public BoxCollider2D surface;
        [Tooltip("The Art child: SpriteRenderer + Animator with the Float, Crack and Break states.")]
        public Animator animator;
        public float crackSeconds = 0f;
        public float breakSeconds = 1f;
        [Tooltip("The Ripple child: the animated ring of water around the log. Hidden when the log breaks.")]
        public GameObject ripple;
        [Tooltip("How fast the broken log sinks (units per second). It also fades out while sinking.")]
        public float sinkSpeed = 1.6f;

        [Header("Moving log (leave Point A empty for a log that stays still)")]
        public Transform pointA;
        public Transform pointB;
        public float moveSpeed = 0.6f;
        private bool movingToB = true;
        private Transform rider;          // Ryu, while he stands on this log
        private Transform riderParent;    // Ryu's normal parent, to give him back

        public bool CountdownStarted { get; private set; }
        public bool Broken { get; private set; }
        public float Elapsed { get; private set; }
        public Transform art => animator.transform;

        private RiverCrossing crossing;
        private bool cracking;

        // Called once by RiverCrossing when the scene starts.
        public void Initialize(RiverCrossing owner)
        {
            crossing = owner;
            ResetLog(false);
        }

        // deployed = false: the log is hidden (logs not placed yet). true: it floats, ready to stand on.
        public void ResetLog(bool deployed)
        {
            DropRider();   // first, so moving the log back can never move Ryu
            if (pointA != null)
            {
                transform.position = pointA.position;
                movingToB = true;
            }
            art.GetComponent<SpriteRenderer>().color = Color.white;
            CountdownStarted = false;
            Broken = false;
            cracking = false;
            Elapsed = 0f;
            surface.enabled = deployed;
            art.localPosition = Vector3.zero;
            gameObject.SetActive(deployed);
            ripple.SetActive(true);
            if (deployed)
                animator.Play("Float", 0, Random.value);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            CheckLanding(collision);
            // Lab 5: the moving log becomes Ryu's parent while he stands on it, so he moves with it.
            if (pointA != null && !Broken && collision.gameObject.tag == "Player" && rider == null)
            {
                rider = collision.transform;
                riderParent = rider.parent;
                rider.SetParent(transform);
            }
        }

        void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.transform == rider)
                DropRider();
        }

        // Give Ryu back to his normal parent (he jumped off, the log broke, or the crossing reset).
        private void DropRider()
        {
            if (rider == null)
                return;
            if (rider.parent == transform)
                rider.SetParent(riderParent);
            rider = null;
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            CheckLanding(collision);
        }

        // Start the countdown only when Ryu is standing on top (not when he bumps the side).
        private void CheckLanding(Collision2D collision)
        {
            if (collision.gameObject.tag != "Player" || CountdownStarted || Broken || !crossing.ActiveAttempt)
                return;

            bool onTop = collision.collider.bounds.min.y >= surface.bounds.max.y - 0.12f;
            if (onTop)
                CountdownStarted = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f)
                return;   // paused: no movement, no timers

            if (Broken)
            {
                Sink();
                return;
            }

            if (pointA != null && crossing.ActiveAttempt)
                Move();

            if (!CountdownStarted || !crossing.ActiveAttempt)
                return;

            Elapsed += Time.deltaTime;

            if (!cracking && Elapsed >= crackSeconds)
            {
                cracking = true;
                animator.Play("Crack");
                crossing.PlayCrack();
            }

            if (Elapsed >= breakSeconds)
            {
                Broken = true;
                DropRider();
                surface.enabled = false;
                animator.Play("Break");
                ripple.SetActive(false);
                crossing.PlayBreak(transform.position);
            }
        }

        // Lab 5 RisingPlatform, sideways: move towards the current point, then turn round.
        private void Move()
        {
            Vector3 target = movingToB ? pointB.position : pointA.position;
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target) < 0.05f)
                movingToB = !movingToB;
        }

        // Once broken, the log sinks and fades out (while the short Break clip plays).
        private void Sink()
        {
            if (art.localPosition.y <= -1.2f)
                return;
            art.localPosition += Vector3.down * sinkSpeed * Time.deltaTime;
            float fade = Mathf.InverseLerp(-1.2f, -0.2f, art.localPosition.y);
            art.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, fade);
        }
    }
}
