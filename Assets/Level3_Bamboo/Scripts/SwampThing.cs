using UnityEngine;

namespace Level3Bamboo
{
    // Swamp Thing, the rooted forest creature guarding the log cage.
    // He sleeps as a mound until the entrance lines are over, then fights with three attacks:
    //   Long-arm strike - wind up, the arm shoots out (damage only on the stretched frames), pull back.
    //   Jump slam       - crouch while a mark shows where he will land, a real jump to that spot,
    //                     a small impact on landing (damage once), then a short recovery.
    //   Burrow          - sink into the ground, travel, a ground warning appears at a locked spot near Ryu,
    //                     then he bursts out there (never inside Ryu) and recovers.
    // Every attack has a warning and a recovery, and the same attack is never used more than twice in a row.
    // He can't be hurt until his own arm has broken the cage (the log puzzle). Defeat leaves his log behind.
    // Everything runs on Time.deltaTime, so pausing the game freezes him.
    public class SwampThing : MonoBehaviour
    {
        public enum State
        {
            Dormant, Wake, Ready, Run, Hop, Hurt, Recovery,
            ArmWindup, ArmStrike, ArmRetract,
            SlamCrouch, SlamAir, SlamImpact,
            Burrow, Underground, EmergeWarning, Emerge,
            Dead
        }

        private enum Attack { None, Arm, Slam, Burrow }

        [Header("References")]
        public BambooPlayerController player;
        public SwampDialogue dialogue;
        public Collider2D playerCollider;
        public SwampCageEncounter cage;
        public SpriteRenderer artwork;
        public Animator animator;
        public BoxCollider2D body;
        public BoxCollider2D arm;
        public BoxCollider2D breakout;
        [Tooltip("Hit area under him when he lands from the jump slam.")]
        public BoxCollider2D slamHitbox;
        public EnemyHealthBar healthBar;
        public ParticleSystem debris;
        public CameraFollow2D cameraFollow;
        public AudioSource voice;
        public EnemyHitEffect hitEffect;
        // Sounds. Each action has its own clip; actions that repeat a lot have two clips that take turns.
        [Header("Sounds")]
        public AudioClip wakeSound;        // entrance growl when he rises
        public AudioClip warningSound;     // crouch / wind-up / "I'm about to burst out" warning
        public AudioClip pullFreeSound;    // arm pulled back out of the ground
        public AudioClip hurtSound;        // used only if hitSounds is empty
        public AudioClip deathSound;       // collapsing wood and roots
        public AudioClip emergeSound;      // roots and earth breaking upward
        public AudioClip[] hitSounds = new AudioClip[2];    // katana hits him (bark crack + groan)
        public AudioClip[] armSounds = new AudioClip[2];    // stretching roots + whoosh
        public AudioClip[] jumpSounds = new AudioClip[2];   // effort + root creak as he jumps
        public AudioClip[] landSounds = new AudioClip[2];   // heavy earth impact
        public AudioClip[] soilSounds = new AudioClip[2];   // digging / soil shifting
        private int nextHitSound;
        private int nextVariation;

        [Header("Health and damage")]
        public int maxHealth = 80;
        public int swordDamage = 10;
        public int armDamage = 14;
        public int slamDamage = 16;
        public int emergenceDamage = 11;

        [Header("Arena")]
        public float wakeRange = 6f;
        public float minEncounterX = 103f;
        public float maxEncounterX = 113f;
        [Tooltip("Half his body width: he stops this far from the cage while it is intact.")]
        public float bodyHalfWidth = 0.7f;
        public float emergenceClearance = 1.35f;

        [Header("Long-arm strike")]
        public float armReach = 3.2f;
        public float armWindupSeconds = 0.65f;
        public float armStrikeSeconds = 0.36f;
        public float armRetractSeconds = 0.3f;

        [Header("Jump slam")]
        public float slamCrouchSeconds = 0.7f;
        public float slamFlightSeconds = 0.75f;
        public float slamGravity = 30f;
        public float slamMaxDistance = 5f;
        public float slamImpactSeconds = 0.5f;

        [Header("Burrow")]
        public float burrowSeconds = 0.6f;
        public float undergroundSpeed = 4f;
        public float emergenceWarningSeconds = 0.75f;
        public float emergeSeconds = 0.55f;
        [Tooltip("Distances from Ryu where he may burst out of the ground (left or right of Ryu).")]
        public float[] emergeOffsets = { 1.6f, 2.2f, 2.9f };

        [Header("Pacing")]
        public float recoverySeconds = 0.65f;
        [Tooltip("Shortest and longest idle time before he acts again.")]
        public Vector2 idleSeconds = new Vector2(0.15f, 0.4f);
        public float runSpeed = 2.4f;
        [Tooltip("If Ryu is farther than this he leaps toward him instead of running.")]
        public float hopDistance = 5f;
        public float hopFlightSeconds = 0.6f;

        public State CurrentState { get; private set; }
        public int Health { get; private set; }

        private readonly System.Collections.Generic.List<RaycastHit2D> casts = new System.Collections.Generic.List<RaycastHit2D>();
        private Vector3 home;
        private Vector3 artOrigin;
        private Vector3 target;          // locked landing / emergence spot
        private Vector2 velocity;        // used while he is in the air
        private float elapsed;
        private float readyDelay;
        private float facing = -1f;
        private float hurtTime;
        private bool hitPlayer;
        private bool collapseResolved;
        private bool audioPaused;
        private Attack lastAttack;
        private int repeatCount;
        private float nextSoilPuff;
        private bool fallPoseShown;

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
                UpdateDefeat();
                return;
            }

            UpdateHurtFlash();
            if (CurrentState == State.EmergeWarning)
                UpdateEmergeWarning();
            switch (CurrentState)
            {
                case State.Dormant:
                    if (PlayerNearby())
                    {
                        // The entrance lines play first; he only rises once they are finished.
                        dialogue.PlayEntrance();
                        if (dialogue.EntranceFinished)
                        {
                            FacePlayer();
                            Enter(State.Wake, "Wake", wakeSound);
                        }
                    }
                    break;
                case State.Wake:
                    if (elapsed >= 0.6f)
                        BecomeReady();
                    break;
                case State.Ready:
                    FacePlayer();
                    if (elapsed >= readyDelay && PlayerNearby())
                        ChooseAction();
                    break;
                case State.Run:
                    UpdateRun();
                    break;
                case State.Hop:
                    if (UpdateFlight())
                        Enter(State.Recovery, "Land");
                    break;
                case State.Hurt:
                    if (elapsed >= 0.25f)
                        BecomeReady();
                    break;
                case State.Recovery:
                    if (elapsed >= recoverySeconds)
                        BecomeReady();
                    break;

                case State.ArmWindup:
                    if (elapsed >= armWindupSeconds)
                        Enter(State.ArmStrike, "ArmStrike", Next(armSounds));
                    break;
                case State.ArmStrike:
                    UpdateArm();
                    if (elapsed >= armStrikeSeconds)
                        Enter(State.ArmRetract, "ArmRetract", pullFreeSound);
                    break;
                case State.ArmRetract:
                    if (elapsed >= armRetractSeconds)
                        Enter(State.Recovery, "Idle");
                    break;

                case State.SlamCrouch:
                    if (elapsed >= slamCrouchSeconds)
                        StartSlamJump();
                    break;
                case State.SlamAir:
                    if (velocity.y < 0f && !fallPoseShown)
                    {
                        fallPoseShown = true;
                        SetPose("SlamFall");
                    }
                    if (UpdateFlight())
                        Land();
                    break;
                case State.SlamImpact:
                    UpdateSlamHit();
                    if (elapsed >= slamImpactSeconds)
                        Enter(State.Recovery, "Idle");
                    break;

                case State.Burrow:
                    if (elapsed >= burrowSeconds)
                        GoUnderground();
                    break;
                case State.Underground:
                    MoveUnderground();
                    break;
                case State.EmergeWarning:
                    if (elapsed >= emergenceWarningSeconds)
                        TryEmerge();
                    break;
                case State.Emerge:
                    UpdateEmergence();
                    break;
            }
        }

        // ---------------------------------------------------------------- choosing what to do

        private bool PlayerNearby()
        {
            float x = player.transform.position.x;
            return x >= minEncounterX - 1.4f && x <= maxEncounterX + 1.4f
                && Mathf.Abs(x - transform.position.x) <= wakeRange;
        }

        // While the cage is intact he can't walk through it, so the arena starts just right of it.
        private float MinX()
        {
            return cage.Broken ? minEncounterX : cage.chargeTarget.bounds.max.x + bodyHalfWidth;
        }

        private void ChooseAction()
        {
            float distance = Mathf.Abs(player.transform.position.x - transform.position.x);
            bool inReach = distance <= armReach + 0.3f;
            bool behindCage = !cage.Broken && player.transform.position.x < cage.chargeTarget.bounds.max.x;

            // Too far to hit? Usually close the gap first (run, or leap if very far).
            if (!inReach && !behindCage && Random.Range(0f, 1f) < 0.4f)
            {
                StartMove(distance);
                return;
            }

            Attack attack;
            float roll = Random.Range(0f, 1f);
            if (behindCage)
                attack = roll < 0.75f ? Attack.Arm : Attack.Slam;     // the bait: his arm can reach the cage
            else if (inReach)
                attack = roll < 0.45f ? Attack.Arm : roll < 0.75f ? Attack.Slam : Attack.Burrow;
            else
                attack = roll < 0.55f ? Attack.Slam : Attack.Burrow;

            // Never the same attack twice in a row (behind the cage the arm may repeat once, it is the bait).
            int allowed = behindCage && attack == Attack.Arm ? 2 : 1;
            if (attack == lastAttack && repeatCount >= allowed)
                attack = attack == Attack.Slam ? (inReach || behindCage ? Attack.Arm : Attack.Burrow) : Attack.Slam;
            // Standing right on top of Ryu a slam would only hop on the spot, so do something else.
            if (attack == Attack.Slam && distance < 1.2f)
                attack = lastAttack == Attack.Arm ? Attack.Burrow : Attack.Arm;
            repeatCount = attack == lastAttack ? repeatCount + 1 : 1;
            lastAttack = attack;

            FacePlayer();
            hitPlayer = false;
            if (attack == Attack.Arm)
                StartArm();
            else if (attack == Attack.Slam)
                StartSlam();
            else
                StartBurrow();
        }

        private void BecomeReady()
        {
            readyDelay = Random.Range(idleSeconds.x, idleSeconds.y);
            Enter(State.Ready, "Idle");
        }

        private void FacePlayer()
        {
            facing = player.transform.position.x < transform.position.x ? -1f : 1f;
            artwork.flipX = facing < 0f;   // the drawings face right
        }

        // ---------------------------------------------------------------- moving

        private void StartMove(float distance)
        {
            FacePlayer();
            if (distance > hopDistance)
            {
                // Leap to a spot a little short of Ryu.
                float x = Mathf.Clamp(player.transform.position.x - facing * 2.4f, MinX(), maxEncounterX);
                Launch(x, hopFlightSeconds);
                Enter(State.Hop, "Jump", null);
            }
            else
            {
                Enter(State.Run, "Run");
            }
        }

        private void UpdateRun()
        {
            FacePlayer();
            float distance = Mathf.Abs(player.transform.position.x - transform.position.x);
            float x = Mathf.Clamp(transform.position.x + facing * runSpeed * Time.deltaTime, MinX(), maxEncounterX);
            bool stuck = Mathf.Approximately(x, transform.position.x);
            transform.position = new Vector3(x, home.y, home.z);
            if (distance <= armReach - 0.4f || stuck || elapsed >= 1.6f || !PlayerNearby())
            {
                readyDelay = 0.1f;
                Enter(State.Ready, "Idle");
            }
        }

        // Sets up a real jump (a parabola) that lands exactly on targetX after the given time.
        private void Launch(float targetX, float seconds)
        {
            fallPoseShown = false;
            target = new Vector3(targetX, home.y, home.z);
            velocity = new Vector2((targetX - transform.position.x) / seconds, slamGravity * seconds * 0.5f);
        }

        // Moves him through the air. Returns true on the frame he lands.
        private bool UpdateFlight()
        {
            velocity.y -= slamGravity * Time.deltaTime;
            Vector3 next = transform.position + (Vector3)(velocity * Time.deltaTime);
            if (velocity.y < 0f && next.y <= home.y)
            {
                transform.position = new Vector3(target.x, home.y, home.z);
                return true;
            }
            transform.position = next;
            return false;
        }

        // ---------------------------------------------------------------- long-arm strike

        private void StartArm()
        {
            dialogue.AttackLine();
            Enter(State.ArmWindup, "ArmWindup", warningSound);
        }

        private void UpdateArm()
        {
            // Damage only on the stretched frames.
            string frame = artwork.sprite.name;
            float stretch = frame == "Rooted Arm 3" ? 0.78f : frame == "Rooted Arm 4" ? 1f : 0f;
            if (stretch <= 0f)
            {
                arm.enabled = false;
                return;
            }

            float length = armReach * stretch - 0.4f;
            arm.size = new Vector2(length, 0.45f);
            arm.offset = new Vector2(facing * (0.4f + length * 0.5f), 1.0f);
            arm.enabled = true;
            Physics2D.SyncTransforms();

            cage.TryBreakFromArm(this);

            if (!hitPlayer && arm.bounds.Intersects(playerCollider.bounds))
            {
                hitPlayer = true;
                player.TakeDamage(armDamage);
            }
        }

        // ---------------------------------------------------------------- jump slam

        // The crouch and its sound are the warning (no floor circle any more).
        private void StartSlam()
        {
            dialogue.AttackLine();
            Enter(State.SlamCrouch, "SlamCrouch", warningSound);
        }

        // The landing spot is locked at the moment he jumps: where Ryu is now, within jump range.
        // He does not steer in the air, so Ryu can still run out of the way during the flight.
        private void StartSlamJump()
        {
            float px = player.transform.position.x;
            float x = Mathf.Clamp(px, transform.position.x - slamMaxDistance, transform.position.x + slamMaxDistance);
            x = Mathf.Clamp(x, MinX(), maxEncounterX);
            if (!GroundAt(x, out Vector3 ground))
                x = transform.position.x;
            target = new Vector3(x, home.y, home.z);
            Launch(target.x, slamFlightSeconds);
            Enter(State.SlamAir, "SlamJump", Next(jumpSounds));
        }

        private void Land()
        {
            EmitDebris(16);
            if (cameraFollow != null)
                cameraFollow.Shake(0.25f);
            Enter(State.SlamImpact, "SlamImpact", Next(landSounds));
        }

        private void UpdateSlamHit()
        {
            // The shockwave only hurts for a moment after touching down.
            slamHitbox.enabled = elapsed < 0.15f;
            if (slamHitbox.enabled && !hitPlayer && slamHitbox.bounds.Intersects(playerCollider.bounds))
            {
                hitPlayer = true;
                player.TakeDamage(slamDamage);
            }
        }

        // ---------------------------------------------------------------- burrow

        private void StartBurrow()
        {
            dialogue.AttackLine();
            Enter(State.Burrow, "Burrow", Next(soilSounds));
        }

        private void GoUnderground()
        {
            body.enabled = false;
            healthBar.gameObject.SetActive(false);
            artwork.enabled = false;
            target = ChooseEmergence();
            nextSoilPuff = 0f;
            Enter(State.Underground, null, Next(soilSounds));
        }

        private void MoveUnderground()
        {
            float x = Mathf.MoveTowards(transform.position.x, target.x, undergroundSpeed * Time.deltaTime);
            transform.position = new Vector3(x, home.y, home.z);
            if (elapsed >= nextSoilPuff)
            {
                nextSoilPuff = elapsed + 0.22f;
                EmitDebris(2);
            }
            if (Mathf.Abs(x - target.x) > 0.02f)
                return;

            // Arrived: this spot is now locked (target does not change), and he warns with sound and soil.
            Enter(State.EmergeWarning, null, warningSound);
            nextSoilPuff = 0f;
        }

        private void TryEmerge()
        {
            if (!HasRoom(target))
            {
                // Ryu is standing on the spot: pick another and warn again (never burst out inside him).
                target = ChooseEmergence();
                Enter(State.Underground, null);
                return;
            }
            artwork.enabled = true;
            EmitDebris(10);
            Enter(State.Emerge, "Emerge", emergeSound);
        }

        private void UpdateEmergence()
        {
            // The burst only hurts on the frames where he rises out of the soil.
            string frame = artwork.sprite.name;
            breakout.enabled = frame == "Rooted Emerge 2" || frame == "Rooted Emerge 3";
            if (breakout.enabled && !hitPlayer && breakout.bounds.Intersects(playerCollider.bounds))
            {
                hitPlayer = true;
                player.TakeDamage(emergenceDamage);
            }

            if (elapsed < emergeSeconds)
                return;
            body.enabled = true;
            healthBar.SetHealth(Health, maxHealth);
            Enter(State.Recovery, "Idle");
        }

        private Vector3 ChooseEmergence()
        {
            // Try the set distances on a random side of Ryu, then the other side. Must be on the
            // floor, inside the arena, and not inside Ryu.
            bool left = Random.Range(0f, 1f) < 0.5f;
            float px = player.transform.position.x;
            int first = Random.Range(0, emergeOffsets.Length);
            for (int i = 0; i < emergeOffsets.Length * 2; i++)
            {
                bool useLeft = i < emergeOffsets.Length ? left : !left;
                float offset = emergeOffsets[(first + i) % emergeOffsets.Length];
                float x = Mathf.Clamp(px + offset * (useLeft ? -1f : 1f), MinX(), maxEncounterX);
                if (GroundAt(x, out Vector3 point) && HasRoom(point))
                    return new Vector3(x, home.y, home.z);
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

        private bool HasRoom(Vector3 point)
        {
            if (Mathf.Abs(point.x - playerCollider.bounds.center.x) < emergenceClearance)
                return false;
            return cage.Broken || !cage.chargeTarget.bounds.Intersects(
                new Bounds((Vector2)point + Vector2.up * 0.94f, new Vector3(1.40f, 1.78f, 0.3f)));
        }

        // While he waits under the soil, small puffs of dirt rise from the locked spot (no floor circle).
        private void UpdateEmergeWarning()
        {
            if (elapsed < nextSoilPuff)
                return;
            nextSoilPuff = elapsed + 0.18f;
            debris.transform.position = target + Vector3.up * 0.3f;
            EmitDebris(2);
        }

        // ---------------------------------------------------------------- being hit

        private void OnSword(Vector2 origin, float direction)
        {
            if (Health <= 0 || !body.enabled)
                return;

            Vector2 hitPoint = body.bounds.ClosestPoint(origin + Vector2.right * direction * 0.68f);
            Vector2 delta = hitPoint - origin;
            if (delta.x * direction < -0.10f || Mathf.Abs(delta.x) > 1.20f
                || Mathf.Abs(delta.y) > 0.83f || !ClearLine(origin, hitPoint))
                return;

            // Swamp Thing can't be hurt until his own arm has broken the cage.
            if (!cage.Broken)
            {
                Play(warningSound);
                return;
            }

            Health = Mathf.Max(0, Health - swordDamage);
            healthBar.SetHealth(Health, maxHealth);
            if (hitEffect != null)
                Instantiate(hitEffect, hitPoint, Quaternion.identity).Play(new Color(0.48f, 0.67f, 0.20f), new Color(0.49f, 0.29f, 0.14f), true);
            AudioClip impact = hurtSound;
            if (hitSounds.Length == 2 && hitSounds[0] != null && hitSounds[1] != null)
            {
                impact = hitSounds[nextHitSound];
                nextHitSound = 1 - nextHitSound;
            }
            if (Health == 0)
            {
                Die();
                if (impact != null)
                    voice.PlayOneShot(impact, 0.45f);
                return;
            }
            if (impact != null)
                voice.PlayOneShot(impact, 0.45f);


            hurtTime = 0.2f;
            // Flinch only when he is not in the middle of an attack (attacks stay readable).
            if (CurrentState == State.Ready || CurrentState == State.Recovery || CurrentState == State.Run)
                Enter(State.Hurt, "Hurt");
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

        private void UpdateHurtFlash()
        {
            hurtTime = Mathf.Max(0f, hurtTime - Time.deltaTime);
            float amount = hurtTime / 0.2f;
            artwork.color = Color.Lerp(Color.white, new Color(1f, 0.60f, 0.48f), amount);
        }

        // ---------------------------------------------------------------- defeat

        private void Die()
        {
            voice.Stop();
            artwork.enabled = true;
            artwork.color = Color.white;
            transform.position = new Vector3(transform.position.x, home.y, home.z);   // never die in mid-air
            body.enabled = false;
            collapseResolved = false;
            Enter(State.Dead, "Defeat", deathSound);
            dialogue.Defeated();
            EmitDebris(18);
        }

        // The defeat clip ends with him lying as a log; then the real collectible log takes his place.
        private void UpdateDefeat()
        {
            if (!collapseResolved && elapsed >= 0.8f)
            {
                collapseResolved = true;
                Vector3? rewardPosition = GroundAt(transform.position.x, out Vector3 ground) ? ground : (Vector3?)null;
                cage.ResolveDefeat(rewardPosition);
            }

            float fade = Mathf.Clamp01((elapsed - 0.8f) / 0.4f);
            artwork.color = new Color(1f, 1f, 1f, 1f - fade);
            if (fade >= 1f && !voice.isPlaying && !debris.IsAlive())
                gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- respawn / helpers

        private void OnRespawn()
        {
            if (CurrentState == State.Dead || cage.Defeated)
                return;

            voice.Stop();
            debris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.position = home;
            artwork.transform.localPosition = artOrigin;
            artwork.enabled = true;
            artwork.color = Color.white;
            hurtTime = 0f;
            lastAttack = Attack.None;
            repeatCount = 0;
            velocity = Vector2.zero;
            Health = maxHealth;
            body.enabled = true;
            healthBar.SetHealth(Health, maxHealth);
            healthBar.gameObject.SetActive(false);
            Enter(State.Dormant, "Dormant");
            dialogue.ResetAttempt();
        }

        private void Enter(State state, string pose, AudioClip sound = null)
        {
            CurrentState = state;
            elapsed = 0f;
            DisableAttacks();
            if (pose != null)
                SetPose(pose);
            if (state == State.Wake)
                healthBar.SetHealth(Health, maxHealth);
            Play(sound);
        }

        // Lab 3 style: every pose is an Animator state reached from Any State by a Trigger with the same name
        // (no exit time, no blend). Old triggers are cleared first so only the newest pose is used.
        private void SetPose(string pose)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                animator.ResetTrigger(parameter.name);
            animator.SetTrigger(pose);
        }

        private void DisableAttacks()
        {
            arm.enabled = false;
            breakout.enabled = false;
            slamHitbox.enabled = false;
        }

        private void EmitDebris(int count)
        {
            if (CurrentState != State.EmergeWarning)
                debris.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            debris.Emit(count);
        }

        // Picks the clips of an array in turn (1, 2, 1, 2 ...), so a repeated action does not sound identical.
        private AudioClip Next(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;
            nextVariation++;
            return clips[nextVariation % clips.Length];
        }

        private void Play(AudioClip clip)
        {
            if (clip != null)
                voice.PlayOneShot(clip);
        }
    }
}
