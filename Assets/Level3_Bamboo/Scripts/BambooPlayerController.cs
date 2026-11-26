using UnityEngine;

namespace Level3Bamboo
{
    // Level 3 rules for the team's Ryu (Assets/_Core/Prefabs/Characters/Player_Ryu.prefab).
    //
    // The team's PlayerMovement script does running, jumping and facing. This component adds what
    // Level 3 needs on top: health and sanity, taking damage, the katana attack (J), death, checkpoints
    // and respawning. It never moves Ryu itself; it only switches PlayerMovement off (canMove = false)
    // while Ryu is dead, mid-swing or the game is paused.
    //
    // The shared Animator has no attack or hurt animation, so those frames are shown here: after the
    // Animator has picked its sprite for this frame, LateUpdate swaps in the right attack or hurt sprite.
    // The frames come from Level 3 copies of the team's ATTACK 1, ATTACK 2 and HURT sheets.
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    public class BambooPlayerController : MonoBehaviour
    {
        [Header("Team character")]
        [SerializeField] private PlayerMovement movement;

        [Header("Ground Check (Lab 2)")]
        [Tooltip("An invisible point at Ryu's feet (the groundFlag child of the team prefab).")]
        public Transform groundCheck;
        [Tooltip("How big the circle around the feet is.")]
        public float groundCheckRadius = 0.15f;
        [Tooltip("Which layers count as ground. Trigger areas live on Ignore Raycast, so they are not included.")]
        public LayerMask whatIsGround = 1;

        [Header("Katana attack")]
        [SerializeField] private KeyCode attackKey = KeyCode.J;
        [SerializeField] private float attackDuration = 0.5f;
        [Tooltip("Swings alternate between these two frame lists.")]
        [SerializeField] private Sprite[] firstSwingFrames = new Sprite[0];
        [SerializeField] private Sprite[] secondSwingFrames = new Sprite[0];
        [Tooltip("Frame number where the blade lands. Enemies are hit on that frame, once per swing.")]
        [SerializeField] private int strikeFrame = 2;
        [SerializeField] private float attackFrameSeconds = 0.07f;
        [Tooltip("Played once at the start of each swing (Lab 7 style random clip, no immediate repeat).")]
        public AudioClip[] swingSounds = new AudioClip[3];
        public float swingVolume = 0.35f;

        [Header("Hurt")]
        [SerializeField] private Sprite[] hurtFrames = new Sprite[0];
        [SerializeField] private float hurtPoseSeconds = 0.24f;
        public AudioSource hurtAudio;
        public AudioClip hurtClip;
        public float damageInvulnerability = 0.9f;

        [Header("Death & Respawn")]
        [SerializeField] private float respawnDelay = 2.5f;
        [SerializeField] private float fallDeathY = -12f;
        [SerializeField] private KeyCode testDeathKey = KeyCode.K;
        public float respawnInvulnerability = 1.5f;

        [Header("Vitals")]
        public int maxHealth = 100;
        public int maxSanity = 100;

        public int Health { get; private set; }
        public int Sanity { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsAttacking { get; private set; }
        // True while a cutscene (the tea rest stop) has taken control: no running, jumping or attacking.
        public bool ControlsLocked { get; private set; }
        // Ryu faces left by flipping his transform (team PlayerMovement), not SpriteRenderer.flipX.
        public bool FacingLeft => transform.lossyScale.x < 0f;
        public float Facing => FacingLeft ? -1f : 1f;
        // Number of the latest swing (0, 1, 2...). Read by the project's shared editor test tool.
        public int LastAttackIndex { get; private set; } = -1;

        // Other scripts listen to these: checkpoints, enemies, quests, weather and wildlife reset on Respawned;
        // enemies, the vine rack and the rope check KatanaSwung (blade position, facing direction).
        public event System.Action Respawned;
        public event System.Action<Vector2, float> KatanaSwung;

        private static readonly int DieHash = Animator.StringToHash("die");
        private static readonly int RespawnHash = Animator.StringToHash("respawn");
        private static readonly int MoveSpeedHash = Animator.StringToHash("moveSpeed");

        private Rigidbody2D rb;
        private Collider2D body;
        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private Color normalColor;

        // Checkpoints store a feet position; Ryu's pivot is his body centre, so respawn lifts him by this.
        private Vector3 spawnFeetPosition;
        private float feetOffset;

        private Sprite[] currentSwing;
        private bool swingLocksFeet;
        private float attackTimer;
        private float attackClock;
        private bool strikeSent;
        private bool useSecondSwing;
        private bool testAttackQueued;
        private int lastSwingSound = -1;
        private float respawnTimer;
        private float protectedUntil;
        private float hurtFlashUntil;
        private float hurtPoseUntil;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            body = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            if (movement == null)
                movement = GetComponent<PlayerMovement>();
            normalColor = spriteRenderer.color;
            Health = maxHealth;
            Sanity = maxSanity;
        }

        // Start is called before the first frame update
        void Start()
        {
            feetOffset = Mathf.Max(0f, transform.position.y - body.bounds.min.y);
            spawnFeetPosition = transform.position + Vector3.down * feetOffset;
            UpdateGroundCheck();
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f)
            {
                movement.canMove = false;
                return;
            }

            if (IsDead)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f)
                    Respawn();
                UpdateMovementLock();
                return;
            }

            // Falling out of the level, or the K test key, kills Ryu.
            if (transform.position.y < fallDeathY || Input.GetKeyDown(testDeathKey))
            {
                TriggerDeath();
                return;
            }

            UpdateGroundCheck();
            UpdateAttack();

            bool attackPressed = testAttackQueued || Input.GetKeyDown(attackKey);
            testAttackQueued = false;
            if (attackPressed && !IsAttacking && !ControlsLocked)
                StartAttack();

            UpdateMovementLock();
        }

        void FixedUpdate()
        {
            // Stand still while dead or while swinging on the ground.
            if (IsDead || IsAttacking && swingLocksFeet)
                rb.velocity = new Vector2(0f, rb.velocity.y);
            // A cutscene lock slows Ryu to a stop over a few frames instead of snapping him still.
            else if (ControlsLocked)
                rb.velocity = new Vector2(Mathf.MoveTowards(rb.velocity.x, 0f, 25f * Time.fixedDeltaTime), rb.velocity.y);
            UpdateGroundCheck();
        }

        // Runs after the Animator, so the attack and hurt sprites replace whatever it chose this frame.
        void LateUpdate()
        {
            if (hurtAudio != null)
            {
                if (Time.timeScale <= 0f) hurtAudio.Pause(); else hurtAudio.UnPause();
            }

            if (!IsDead)
            {
                if (IsAttacking)
                    ShowFrame(currentSwing, attackClock / attackFrameSeconds);
                else if (Time.time < hurtPoseUntil)
                {
                    float progress = 1f - (hurtPoseUntil - Time.time) / hurtPoseSeconds;
                    ShowFrame(hurtFrames, progress * hurtFrames.Length);
                }
            }

            // Short red flash when hit.
            float flash = IsDead ? 0f : Mathf.Clamp01((hurtFlashUntil - Time.time) / 0.22f);
            spriteRenderer.color = Color.Lerp(normalColor, new Color(1f, 0.53f, 0.43f, normalColor.a), flash);
        }

        private void ShowFrame(Sprite[] frames, float frameNumber)
        {
            if (frames.Length == 0)
                return;
            int index = Mathf.Clamp(Mathf.FloorToInt(frameNumber), 0, frames.Length - 1);
            spriteRenderer.sprite = frames[index];
        }

        // ---------- Health, sanity and checkpoints ----------

        public bool TakeDamage(int amount)
        {
            if (IsDead || amount <= 0 || Time.time < protectedUntil)
                return false;

            Health = Mathf.Max(0, Health - amount);
            protectedUntil = Time.time + damageInvulnerability;
            hurtFlashUntil = Time.time + 0.22f;
            hurtPoseUntil = Time.time + hurtPoseSeconds;
            // Shake the camera a little when Ryu is hurt.
            CameraFollow2D cameraFollow = FindObjectOfType<CameraFollow2D>();
            if (cameraFollow != null)
                cameraFollow.Shake(0.25f);
            if (hurtAudio != null && hurtClip != null)
                hurtAudio.PlayOneShot(hurtClip);
            if (Health == 0)
                TriggerDeath();
            return true;
        }

        public void SetSanity(int value)
        {
            Sanity = Mathf.Clamp(value, 0, maxSanity);
        }

        // Used by RiverProgress to bring health and sanity from the bamboo trail into the river scene.
        internal void RestoreCarriedVitals(int health, int sanity, int carriedMaxHealth, int carriedMaxSanity)
        {
            if (carriedMaxHealth > 0)
                maxHealth = carriedMaxHealth;
            if (carriedMaxSanity > 0)
                maxSanity = carriedMaxSanity;
            Health = Mathf.Clamp(health, 1, maxHealth);
            SetSanity(sanity);
        }

        // Called by checkpoints with the feet position Ryu should respawn at.
        public void SetCheckpoint(Vector3 feetPosition)
        {
            spawnFeetPosition = feetPosition;
        }

        // ---------- Katana ----------

        private void StartAttack()
        {
            IsAttacking = true;
            swingLocksFeet = IsGrounded;
            attackTimer = attackDuration;
            attackClock = 0f;
            strikeSent = false;
            currentSwing = useSecondSwing ? secondSwingFrames : firstSwingFrames;
            useSecondSwing = !useSecondSwing;
            LastAttackIndex++;
            PlaySwingSound();
            if (swingLocksFeet)
                rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        // Lab 7's PlayRandomSFX idea: pick a random clip, but never the same one twice in a row.
        private void PlaySwingSound()
        {
            if (hurtAudio == null || swingSounds.Length == 0)
                return;
            int index = Random.Range(0, swingSounds.Length);
            if (index == lastSwingSound && swingSounds.Length > 1)
                index = (index + 1) % swingSounds.Length;
            lastSwingSound = index;
            if (swingSounds[index] != null)
                hurtAudio.PlayOneShot(swingSounds[index], swingVolume);
        }

        private void UpdateAttack()
        {
            if (!IsAttacking)
                return;

            attackTimer -= Time.deltaTime;
            attackClock += Time.deltaTime;
            if (!strikeSent && attackClock >= strikeFrame * attackFrameSeconds)
                SendStrike();

            if (attackTimer <= 0f)
            {
                if (!strikeSent)
                    SendStrike();
                IsAttacking = false;
                swingLocksFeet = false;
            }
        }

        // Tells listeners (enemies, vine rack, rope) that the blade has landed, exactly once per swing.
        private void SendStrike()
        {
            strikeSent = true;
            KatanaSwung?.Invoke(body.bounds.center, Facing);
        }

        // Called by TeaRestConversation: true at the start of the conversation, false at the end.
        public void LockControls(bool locked)
        {
            ControlsLocked = locked;
            if (locked)
                testAttackQueued = false;
            UpdateMovementLock();
        }

        // ---------- Death and respawn ----------

        public void TriggerDeath()
        {
            if (IsDead)
                return;

            IsDead = true;
            Health = 0;
            IsAttacking = false;
            swingLocksFeet = false;
            respawnTimer = respawnDelay;
            hurtPoseUntil = 0f;
            rb.velocity = new Vector2(0f, rb.velocity.y);
            UpdateMovementLock();

            animator.ResetTrigger(RespawnHash);
            animator.SetFloat(MoveSpeedHash, 0f);
            animator.SetTrigger(DieHash);
        }

        public void Respawn()
        {
            IsDead = false;
            ControlsLocked = false;
            IsAttacking = false;
            swingLocksFeet = false;
            respawnTimer = 0f;
            Health = maxHealth;
            protectedUntil = Time.time + respawnInvulnerability;
            hurtFlashUntil = 0f;
            hurtPoseUntil = 0f;
            testAttackQueued = false;
            if (hurtAudio != null)
                hurtAudio.Stop();

            Vector3 standing = spawnFeetPosition + Vector3.up * feetOffset;
            rb.position = standing;
            transform.position = standing;
            rb.velocity = Vector2.zero;

            animator.ResetTrigger(DieHash);
            animator.SetTrigger(RespawnHash);
            UpdateMovementLock();
            UpdateGroundCheck();
            Respawned?.Invoke();
        }

        // Receiver for the event at the end of the team's ryu_death clip. Level 3 uses respawnDelay
        // instead, so nothing happens here (the method must exist or Unity logs an error).
        public void OnDeathAnimationComplete()
        {
        }

        // ---------- Helpers ----------

        private void UpdateMovementLock()
        {
            movement.canMove = !IsDead && !ControlsLocked && !(IsAttacking && swingLocksFeet);
        }

        // Lab 2 ground check: is the small circle at Ryu's feet touching the ground layer?
        // While he is still moving upward from a jump he does not count as grounded.
        private void UpdateGroundCheck()
        {
            if (rb.velocity.y > 0.15f)
                IsGrounded = false;
            else
                IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);
        }

        // ---------- Test hooks ----------
        // The project's shared editor test tool (McpLocalBridge) calls these. Only SimulateAttack does
        // anything; the team's PlayerMovement reads the keyboard directly, so move/jump cannot be simulated.

        public void SimulateAttack()
        {
            testAttackQueued = true;
        }

        public void SetSimulatedHorizontal(float horizontal)
        {
        }

        public void SimulateJump()
        {
        }

        public void ClearSimulatedInput()
        {
            testAttackQueued = false;
        }
    }
}
