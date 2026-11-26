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
            if (!CountdownStarted || Broken || !crossing.ActiveAttempt || Time.timeScale <= 0f)
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
                surface.enabled = false;
                animator.Play("Break");
                ripple.SetActive(false);
                crossing.PlayBreak(transform.position);
            }
        }
    }
}
