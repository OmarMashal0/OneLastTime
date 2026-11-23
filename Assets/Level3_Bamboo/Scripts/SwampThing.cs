using UnityEngine;

namespace Level3Bamboo
{
    public class SwampThing : MonoBehaviour
    {
        public enum State
        {
            Dormant, Wake, Ready, ArmWarning, ArmStrike, RetractAndBurrow,
            UndergroundTravel, EmergenceWarning, Emerge, Recovery, Dead
        }

        public BambooPlayerController player;
        public SwampDialogue dialogue;
        public Collider2D playerCollider;
        public SwampCageEncounter cage;
        public SpriteRenderer artwork;
        public Animator animator;
        public BoxCollider2D body;
        public BoxCollider2D arm;
        public BoxCollider2D breakout;
        public EnemyHealthBar healthBar;
        public ParticleSystem debris;
        public AudioSource voice;
        public AudioClip wakeSound, warningSound, swingSound, slamSound, pullFreeSound, hurtSound, deathSound, soilSound, emergeSound;

        public int maxHealth = 80;
        public int swordDamage = 10;
        public int armDamage = 24;
        public int emergenceDamage = 18;
        public float wakeRange = 6f;
        public float armReach = 3.38f;
        public float armWarningSeconds = 0.7f;
        public float emergenceWarningSeconds = 0.8f;
        public float recoverySeconds = 1.2f;
        public float undergroundSpeed = 2.4f;
        public float minEncounterX = 103f;
        public float maxEncounterX = 113f;
        public float emergenceClearance = 1.35f;

        public State CurrentState { get; private set; }
        public int Health { get; private set; }

        private const float StrikeSeconds = 0.38f;
        private const float EmergeSeconds = 0.52f;
        private readonly System.Collections.Generic.List<RaycastHit2D> casts = new System.Collections.Generic.List<RaycastHit2D>();
        private readonly Collider2D[] overlaps = new Collider2D[8];
        private Vector3 home;
        private Vector3 artOrigin;
        private Vector3 emergencePoint;
        private float elapsed;
        private float readyDelay;
        private float facing = -1f;
        private float hurtTime;
        private bool hitPlayer;
        private float nextSoilPuff;
        private bool collapseResolved;
        private bool audioPaused;
        private bool armRoute;
        private bool lastWasArm;
        private int repeatCount;
        private bool lastEmergenceLeft;
        private int sideRepeats;

        // Start is called before the first frame update
        void Start()
        {
            home = transform.position;
            artOrigin = artwork.transform.localPosition;
            player.KatanaSwung += OnSword;
            player.Respawned += OnRespawn;
            OnRespawn();
        }

        void OnDestroy()
        {
            if (player == null)
                return;

            player.KatanaSwung -= OnSword;
            player.Respawned -= OnRespawn;
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f)
            {
                if (!audioPaused)
                    voice.Pause();
                audioPaused = true;
                return;
            }
            if (audioPaused)
                voice.UnPause();
            audioPaused = false;

            if (player.IsDead && CurrentState != State.Dead)
            {
                DisableAttacks();
                return;
            }

            elapsed += Time.deltaTime;
            if (CurrentState == State.Dead)
            {
                UpdateCollapse();
                return;
            }

            UpdateHurtFlash();
            switch (CurrentState)
            {
                case State.Dormant:
                    if (PlayerNearby())
                    {
                        FacePlayer();
                        Enter(State.Wake, "Wake", wakeSound);
                    }
                    break;
                case State.Wake:
                    if (elapsed >= 0.64f)
                        BecomeReady();
                    break;
                case State.Ready:
                    FacePlayer();
                    if (elapsed < readyDelay)
                        break;
                    if (PlayerNearby())
                        PickAttack();
                    break;
                case State.ArmWarning:
                    if (elapsed >= armWarningSeconds)
                        Enter(State.ArmStrike, "ArmStrike", swingSound);
                    break;
                case State.ArmStrike:
                    UpdateArm();
                    break;
                case State.RetractAndBurrow:
                    if (elapsed >= (armRoute ? 0.72f : 0.86f))
                        BeginTravel();
                    break;
                case State.UndergroundTravel:
                    MoveUnderground();
                    break;
                case State.EmergenceWarning:
                    if (elapsed >= emergenceWarningSeconds)
                    {
                        if (HasRoom(emergencePoint))
                        {
                            Enter(State.Emerge, "Emerge", emergeSound);
                            EmitDebris(10);
                        }
                        else
                        {
                            BeginTravel();
                        }
                    }
                    break;
                case State.Emerge:
                    UpdateEmergence();
                    break;
                case State.Recovery:
                    if (elapsed >= recoverySeconds)
                        BecomeReady();
                    break;
            }
        }

        private bool PlayerNearby()
        {
            float x = player.transform.position.x;
            return x >= minEncounterX - 1.4f && x <= maxEncounterX + 1.4f
                && Mathf.Abs(x - transform.position.x) <= wakeRange;
        }

        private void FacePlayer()
        {
            facing = player.transform.position.x < transform.position.x ? -1f : 1f;
            artwork.flipX = facing < 0f;
        }

        private void BecomeReady()
        {
            readyDelay = Random.Range(0.36f, 0.69f);
            Enter(State.Ready, "Idle");
        }

        private void PickAttack()
        {
            bool useArm = repeatCount >= 2 ? !lastWasArm : Random.value < 0.65f;
            repeatCount = useArm == lastWasArm ? repeatCount + 1 : 1;
            lastWasArm = useArm;
            armRoute = useArm;
            FacePlayer();
            if (useArm)
            {
                hitPlayer = false;
                Enter(State.ArmWarning, "ArmWarning", warningSound);
            }
            else
            {
                Enter(State.RetractAndBurrow, "DirectBurrow", warningSound);
            }
        }

        private void UpdateArm()
        {
            if (elapsed >= StrikeSeconds)
            {
                Enter(State.RetractAndBurrow, "Retract", pullFreeSound);
                return;
            }

            // The three full-extension poses start after the first wind-up frame.
            string frame = artwork.sprite.name;
            if (frame != "Arm 03" && frame != "Arm 04" && frame != "Arm 05")
            {
                arm.enabled = false;
                return;
            }

            float reach = armReach * (frame == "Arm 03" ? 0.81f : frame == "Arm 04" ? 1f : 0.88f);
            Vector2 shoulder = (Vector2)transform.position + new Vector2(facing * 0.55f, 1.05f);
            var filter = new ContactFilter2D { useTriggers = false };
            float length = reach - 0.55f;
            int hitCount = Physics2D.BoxCast(shoulder, new Vector2(0.06f, 0.42f), 0f,
                Vector2.right * facing, filter, casts, length);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = casts[i];
                if (hit.collider != playerCollider)
                    length = Mathf.Min(length, Mathf.Max(0f, hit.distance));
            }

            arm.enabled = length > 0.02f;
            if (!arm.enabled)
                return;

            arm.size = new Vector2(length, 0.48f);
            arm.offset = new Vector2(facing * (0.55f + length * 0.5f), 1.05f);
            Physics2D.SyncTransforms();
            cage.TryBreakFromArm(this);

            if (!hitPlayer && arm.bounds.Intersects(playerCollider.bounds)
                && ClearLine(shoulder, playerCollider.bounds.center))
            {
                hitPlayer = true;
                player.TakeDamage(armDamage);
            }
        }

        private void BeginTravel()
        {
            body.enabled = false;
            healthBar.gameObject.SetActive(false);
            emergencePoint = ChooseEmergence();
            nextSoilPuff = Time.time;
            Enter(State.UndergroundTravel, "Underground", soilSound);
        }

        private Vector3 ChooseEmergence()
        {
            bool left = sideRepeats >= 2 ? !lastEmergenceLeft : Random.value < 0.5f;
            float px = player.transform.position.x;
            for (int i = 0; i < 8; i++)
            {
                bool useLeft = i % 2 == 0 ? left : !left;
                float offset = 1.48f + (i / 2) * 0.62f + Random.Range(0f, 0.26f);
                float x = Mathf.Clamp(px + offset * (useLeft ? -1f : 1f), minEncounterX, maxEncounterX);
                if (GroundAt(x, out Vector3 point) && HasRoom(point) && GroundPath(point.x))
                {
                    sideRepeats = useLeft == lastEmergenceLeft ? sideRepeats + 1 : 1;
                    lastEmergenceLeft = useLeft;
                    return point;
                }
            }
            return transform.position;
        }

        private bool GroundAt(float x, out Vector3 point)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            Physics2D.Raycast(new Vector2(x, home.y + 0.38f), Vector2.down, filter, casts, 0.95f);
            foreach (var hit in casts)
            {
                if (hit.collider == playerCollider || Mathf.Abs(hit.point.y - home.y) > 0.37f)
                    continue;
                point = new Vector3(x, hit.point.y + 0.025f, home.z);
                return true;
            }
            point = default;
            return false;
        }

        private bool GroundPath(float destination)
        {
            int samples = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(destination - transform.position.x) / 0.35f));
            for (int i = 1; i <= samples; i++)
            {
                float x = Mathf.Lerp(transform.position.x, destination, i / (float)samples);
                if (!GroundAt(x, out Vector3 point) || !TravelClear(point))
                    return false;
            }
            return true;
        }

        private bool TravelClear(Vector3 point)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            int count = Physics2D.OverlapBox((Vector2)point + Vector2.up * 0.31f,
                new Vector2(0.60f, 0.42f), 0f, filter, overlaps);
            for (int i = 0; i < count; i++)
                if (overlaps[i] != playerCollider && overlaps[i] != body)
                    return false;
            return true;
        }

        private bool HasRoom(Vector3 point)
        {
            if (Mathf.Abs(point.x - playerCollider.bounds.center.x) < emergenceClearance)
                return false;

            var filter = new ContactFilter2D { useTriggers = false };
            Vector2 center = (Vector2)point + Vector2.up * 0.94f;
            int count = Physics2D.OverlapBox(center, new Vector2(1.30f, 1.75f), 0f, filter, overlaps);
            if (count != 0)
                return false;

            return cage.Broken || !cage.chargeTarget.bounds.Intersects(new Bounds(center, new Vector3(1.40f, 1.78f, 0.3f)));
        }

        private void MoveUnderground()
        {
            float x = Mathf.MoveTowards(transform.position.x, emergencePoint.x, undergroundSpeed * Time.deltaTime);
            if (GroundAt(x, out Vector3 step) && TravelClear(step))
            {
                bool moved = Mathf.Abs(step.x - transform.position.x) > 0.005f;
                transform.position = step;
                if (moved && Time.time >= nextSoilPuff)
                {
                    nextSoilPuff = Time.time + 0.22f;
                    EmitDebris(2);
                }
            }
            else
                emergencePoint = transform.position;

            if (Mathf.Abs(transform.position.x - emergencePoint.x) > 0.02f)
                return;
            if (!HasRoom(emergencePoint))
            {
                // Retry without polling the player every frame or replaying the travel sound.
                if (elapsed > 0.4f)
                {
                    elapsed = 0f;
                    emergencePoint = ChooseEmergence();
                }
                return;
            }
            hitPlayer = false;
            Enter(State.EmergenceWarning, "Underground", warningSound);
        }

        private void UpdateEmergence()
        {
            breakout.enabled = artwork.sprite.name == "Burrow 12" || artwork.sprite.name == "Burrow 13";
            if (breakout.enabled && !hitPlayer && breakout.bounds.Intersects(playerCollider.bounds)
                && ClearLine(breakout.bounds.center, playerCollider.bounds.center))
            {
                hitPlayer = true;
                player.TakeDamage(emergenceDamage);
            }

            if (elapsed < EmergeSeconds)
                return;

            body.enabled = true;
            healthBar.SetHealth(Health, maxHealth);
            Enter(State.Recovery, "Idle");
        }

        private bool ClearLine(Vector2 from, Vector2 to)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            Physics2D.Linecast(from, to, filter, casts);
            foreach (var hit in casts)
                if (hit.collider != playerCollider)
                    return false;
            return true;
        }

        private void OnSword(Vector2 origin, float direction)
        {
            if (Health <= 0 || !body.enabled)
                return;

            Vector2 target = body.bounds.ClosestPoint(origin + Vector2.right * direction * 0.68f);
            Vector2 delta = target - origin;
            if (delta.x * direction < -0.10f || Mathf.Abs(delta.x) > 1.20f
                || Mathf.Abs(delta.y) > 0.83f || !ClearLine(origin, target))
                return;

            Health = Mathf.Max(0, Health - swordDamage);
            healthBar.SetHealth(Health, maxHealth);
            if (Health == 0)
            {
                voice.Stop();
                Enter(State.Dead, null, deathSound);
                animator.enabled = false;
                body.enabled = false;
                collapseResolved = false;
                EmitDebris(18);
                return;
            }
            hurtTime = 0.2f;
            Play(hurtSound);
        }

        private void UpdateHurtFlash()
        {
            hurtTime = Mathf.Max(0f, hurtTime - Time.deltaTime);
            float amount = hurtTime / 0.2f;
            artwork.color = Color.Lerp(Color.white, new Color(1f, 0.60f, 0.48f), amount);
            artwork.transform.localPosition = artOrigin + Vector3.left * facing * amount * 0.11f;
        }

        private void UpdateCollapse()
        {
            float progress = Mathf.Clamp01(elapsed / 0.65f);
            artwork.transform.localPosition = artOrigin + Vector3.down * (progress * 0.28f);
            artwork.color = new Color(0.63f, 0.57f, 0.48f, 1f - progress);
            if (!collapseResolved && elapsed >= 0.48f)
            {
                collapseResolved = true;
                float x = transform.position.x;
                Vector3? rewardPosition = GroundAt(x, out Vector3 ground) ? ground : (Vector3?)null;
                cage.ResolveDefeat(rewardPosition);
            }

            if (progress >= 1f && !voice.isPlaying && !debris.IsAlive())
                gameObject.SetActive(false);
        }

        private void OnRespawn()
        {
            if (CurrentState == State.Dead || cage.Defeated)
                return;

            voice.Stop();
            debris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.position = home;
            artwork.transform.localPosition = artOrigin;
            artwork.color = Color.white;
            animator.enabled = true;
            hurtTime = 0f;
            repeatCount = 0;
            sideRepeats = 0;
            Health = maxHealth;
            body.enabled = true;
            healthBar.SetHealth(Health, maxHealth);
            healthBar.gameObject.SetActive(false);
            Enter(State.Dormant, "Dormant");
            if (dialogue != null)
                dialogue.ResetAttempt();
        }

        private void Enter(State state, string pose, AudioClip sound = null)
        {
            CurrentState = state;
            if (dialogue != null)
            {
                if (state == State.RetractAndBurrow)
                    dialogue.FirstBurrow();
                if (state == State.Dead)
                    dialogue.Defeated();
            }
            elapsed = 0f;
            DisableAttacks();
            if (pose != null)
                animator.Play(pose, 0, 0f);
            if (state == State.Wake)
                healthBar.SetHealth(Health, maxHealth);
            Play(sound);
        }

        private void DisableAttacks()
        {
            arm.enabled = false;
            breakout.enabled = false;
        }

        private void EmitDebris(int count)
        {
            debris.Emit(count);
        }

        private void Play(AudioClip clip)
        {
            if (clip != null)
                voice.PlayOneShot(clip);
        }
    }
}
