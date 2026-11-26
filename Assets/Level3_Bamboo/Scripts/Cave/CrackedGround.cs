using UnityEngine;

namespace Level3Bamboo
{
    // The cracked ground above the bear cave.
    // When Ryu steps on it, the "Crack" clip plays for a moment, then the solid collider is switched off
    // so he falls into the cave. CaveZone makes it whole again when the cave resets (death or climbing out).
    public class CrackedGround : MonoBehaviour
    {
        public BambooPlayerController player;
        [Tooltip("The solid collider Ryu stands on (child 'Support').")]
        public Collider2D support;
        [Tooltip("Plays the Intact, Crack and Collapse clips.")]
        public Animator animator;
        [Tooltip("The child 'Art' sprite, and the picture it shows while whole.")]
        public SpriteRenderer art;
        public Sprite wholeSprite;
        public AudioSource crackSound;
        [Tooltip("Played once when the ground gives way.")]
        public AudioClip collapseClip;
        [Tooltip("How long the cracks show before the ground gives way.")]
        public float crackSeconds = 0.3f;
        [Tooltip("Ryu's controls are released this long after the collapse, so he drops instead of flying across.")]
        public float dropSeconds = 0.4f;

        public bool Collapsed { get; private set; }

        private bool cracking;
        private float timer;

        void Start()
        {
            animator.Play("Intact");
        }

        // The trigger on this object sits just above the cracked surface.
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.tag != "Player" || cracking || Collapsed)
                return;

            cracking = true;
            timer = 0f;
            animator.Play("Crack");
            if (crackSound != null)
                crackSound.Play();
        }

        void Update()
        {
            // Time.deltaTime is 0 while paused, so the countdown waits.
            if (cracking)
            {
                timer += Time.deltaTime;
                if (timer >= crackSeconds)
                    Collapse();
            }
            else if (Collapsed && timer < dropSeconds)
            {
                timer += Time.deltaTime;
                if (timer >= dropSeconds)
                    player.LockControls(false);
            }
        }

        // Whole again, ready for the next fall. Called by CaveZone when the cave resets.
        public void ResetGround()
        {
            // If he was still in the short drop lock, give the controls back.
            if (Collapsed && timer < dropSeconds)
                player.LockControls(false);

            cracking = false;
            Collapsed = false;
            timer = 0f;
            support.enabled = true;

            // Picture back in place, fully visible, and the animator back on its first clip.
            art.sprite = wholeSprite;
            art.transform.localPosition = Vector3.zero;
            art.color = Color.white;
            animator.Play("Intact", 0, 0f);
            animator.Update(0f);
        }

        private void Collapse()
        {
            cracking = false;
            Collapsed = true;
            timer = 0f;
            support.enabled = false;
            animator.Play("Collapse");
            if (crackSound != null && collapseClip != null)
                crackSound.PlayOneShot(collapseClip, 0.8f);
            // Stop his run and lock the controls for a moment, so he drops into the hole
            // instead of sailing over it (and lands on the cave floor, not on the return ledges).
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.velocity = new Vector2(0f, body.velocity.y);
            player.LockControls(true);
        }
    }
}
