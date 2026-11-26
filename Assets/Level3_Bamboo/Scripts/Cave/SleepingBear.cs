using UnityEngine;

namespace Level3Bamboo
{
    // The mother bear. Three simple states:
    //   Sleeping - loops the sleep clip. If Ryu RUNS or JUMPS on the cave's lower path, she wakes.
    //              Walking slowly with Shift, or standing still, never wakes her.
    //   Waking   - a short warning (wake clip + growl) before she moves.
    //   Charging - she keeps running at Ryu, even if he stops. Touching him kills him through the
    //              normal death system (once) and she stops on the spot.
    //   Stopped  - stands still after the kill until CaveZone resets the cave on respawn.
    // Nothing is heard during the fall, the landing and the camera introduction, nor in the escape shaft.
    // She cannot be attacked: she has no enemy script and only a trigger that is on while charging.
    public class SleepingBear : MonoBehaviour
    {
        public enum State { Sleeping, Waking, Charging, Stopped }

        public BambooPlayerController player;
        public Rigidbody2D playerBody;
        public PlayerMovement movement;
        [Tooltip("Trigger along the cave floor, from the landing to the tunnel. Running or jumping here wakes her.")]
        public PlayerTriggerZone lowerPath;
        [Tooltip("The wall-jump shaft. Jumps in here never wake her.")]
        public WallClimbShaft escapeShaft;
        public BearIntroduction introduction;
        [Tooltip("Plays the Sleep, Wake, Run and Stand clips (Animator triggers with the same names).")]
        public Animator animator;
        public SpriteRenderer art;
        public Rigidbody2D body;
        [Tooltip("Trigger that catches Ryu. Only enabled while charging.")]
        public Collider2D hitZone;
        public AudioSource sound;
        public AudioClip warningClip;
        public AudioClip chargeClip;

        public float wakeWarningSeconds = 0.9f;
        public float chargeSpeed = 6.5f;
        [Tooltip("She never runs past these x positions (the cave walls).")]
        public float minX = 134.4f;
        public float maxX = 146.2f;
        [Tooltip("Tick if the art should face right while she sleeps (the sheet faces left).")]
        public bool sleepFacingRight = true;

        public State CurrentState { get; private set; }

        private Vector3 home;
        private float timer;
        private bool wasGrounded = true;

        void Start()
        {
            home = transform.position;
            ResetBear();
        }

        void Update()
        {
            // Everything below uses Time.deltaTime, which is 0 while paused.
            bool heard = HeardRyu();

            switch (CurrentState)
            {
                case State.Sleeping:
                    if (heard)
                        StartWaking();
                    break;

                case State.Waking:
                    timer += Time.deltaTime;
                    if (timer >= wakeWarningSeconds)
                        StartCharge();
                    break;

                case State.Charging:
                    if (player.IsDead)
                    {
                        Stop();   // Ryu died some other way: stop anyway
                        break;
                    }
                    float targetX = Mathf.Clamp(player.transform.position.x, minX, maxX);
                    Face(targetX - transform.position.x);
                    float x = Mathf.MoveTowards(transform.position.x, targetX, chargeSpeed * Time.deltaTime);
                    transform.position = new Vector3(x, home.y, home.z);
                    break;
            }
        }

        // True on the frame Ryu really runs or jumps on the lower path (after the introduction).
        private bool HeardRyu()
        {
            bool grounded = player.IsGrounded;
            bool jumped = wasGrounded && !grounded && playerBody.velocity.y > 1f;
            wasGrounded = grounded;

            if (Time.timeScale <= 0f || player.IsDead || player.ControlsLocked)
                return false;
            if (!introduction.Listening || !lowerPath.PlayerInside || escapeShaft.PlayerInside)
                return false;

            // Real speed, so pushing against a wall does not count. Shift-walking stays below this.
            bool running = grounded && Mathf.Abs(playerBody.velocity.x) > movement.sneakSpeed + 0.5f;
            return running || jumped;
        }

        // Ryu touched her while she was charging: stop first (so this can only happen once), then kill him.
        void OnTriggerStay2D(Collider2D other)
        {
            if (CurrentState != State.Charging || other.tag != "Player" || player.IsDead)
                return;
            Stop();
            player.TriggerDeath();
        }

        // Stands still: no movement, no running clip, no more hits.
        private void Stop()
        {
            CurrentState = State.Stopped;
            hitZone.enabled = false;
            body.velocity = Vector2.zero;
            SetPose("Stand");   // one-frame clip: the last frame of the waking clip, standing still
        }

        private void StartWaking()
        {
            CurrentState = State.Waking;
            timer = 0f;
            SetPose("Wake");
            Play(warningClip, 0.9f);
        }

        private void StartCharge()
        {
            CurrentState = State.Charging;
            hitZone.enabled = true;
            SetPose("Run");
            Play(chargeClip, 1f);
        }

        // Back to the nest, asleep. Called at the start and by CaveZone when the cave resets.
        public void ResetBear()
        {
            transform.position = home;
            body.velocity = Vector2.zero;
            timer = 0f;
            CurrentState = State.Sleeping;
            hitZone.enabled = false;
            SetPose("Sleep");
            art.flipX = sleepFacingRight;
            wasGrounded = true;
        }

        // Lab 3 style: each clip is an Animator state reached from Any State by a Trigger of the same name.
        private void SetPose(string pose)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                animator.ResetTrigger(parameter.name);
            animator.SetTrigger(pose);
        }

        private void Face(float direction)
        {
            if (direction != 0f)
                art.flipX = direction > 0f;   // the sheet faces left
        }

        private void Play(AudioClip clip, float volume)
        {
            if (sound != null && clip != null)
                sound.PlayOneShot(clip, volume);
        }
    }
}
