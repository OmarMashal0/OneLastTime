using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OneLastTime.Combat;
using OneLastTime.Systems;

namespace OneLastTime.Player
{
    [RequireComponent(typeof(SpriteRenderer), typeof(PlayerMovement))]
    public class PlayerAttack : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private bool attacksUnlocked = false;

        [Header("Attack Controls")]
        [SerializeField] private KeyCode groundAttackKey = KeyCode.J;
        [SerializeField] private KeyCode airAttackKey = KeyCode.K;

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] attack1Frames;
        [SerializeField] private Sprite[] attack2Frames;
        [SerializeField] private Sprite[] attack3Frames;
        [SerializeField] private Sprite[] airAttackFrames;

        [Header("Timing")]
        [SerializeField] private float frameDuration = 0.065f;
        [SerializeField] private float comboResetDuration = 0.45f;

        [Header("Hit Detection")]
        [SerializeField] private Vector2 attackOffset = new Vector2(0.9f, 0f);
        [SerializeField] private float attackRadius = 1.1f;
        [SerializeField] private LayerMask enemyLayers = ~0;

        // impulses removed as attacks no longer slide the player forward

        private SpriteRenderer sr;
        private Animator anim;
        private Rigidbody2D rb;
        private PlayerMovement playerMovement;
        private PlayerHealth playerHealth;

        private int comboIndex = 0;
        private bool isAttacking = false;
        private bool comboBuffered = false;
        private float comboTimer = 0f;
        private bool hasAirAttacked = false;
        private Coroutine attackRoutine;
        private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        public bool AttacksUnlocked => attacksUnlocked;
        public bool IsAttacking => isAttacking;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            anim = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
            playerMovement = GetComponent<PlayerMovement>();
            playerHealth = GetComponent<PlayerHealth>();

            // populate frames if unassigned in inspector
            EnsureFramesPopulated();
        }

        void EnsureFramesPopulated()
        {
            if (attack1Frames != null && attack1Frames.Length > 0 &&
                attack2Frames != null && attack2Frames.Length > 0 &&
                attack3Frames != null && attack3Frames.Length > 0 &&
                airAttackFrames != null && airAttackFrames.Length > 0)
            {
                return;
            }

            var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
            if (allSprites == null || allSprites.Length == 0) return;

            if (attack1Frames == null || attack1Frames.Length == 0)
            {
                var list = new List<Sprite>();
                for (int i = 0; i < 7; i++)
                {
                    string target = "ATTACK 1_" + i;
                    var s = System.Array.Find(allSprites, x => x != null && x.name == target);
                    if (s != null) list.Add(s);
                }
                if (list.Count > 0) attack1Frames = list.ToArray();
            }

            if (attack2Frames == null || attack2Frames.Length == 0)
            {
                var list = new List<Sprite>();
                for (int i = 0; i < 7; i++)
                {
                    string target = "ATTACK 2_" + i;
                    var s = System.Array.Find(allSprites, x => x != null && x.name == target);
                    if (s != null) list.Add(s);
                }
                if (list.Count > 0) attack2Frames = list.ToArray();
            }

            if (attack3Frames == null || attack3Frames.Length == 0)
            {
                var list = new List<Sprite>();
                for (int i = 0; i < 6; i++)
                {
                    string target = "ATTACK 3_" + i;
                    var s = System.Array.Find(allSprites, x => x != null && x.name == target);
                    if (s != null) list.Add(s);
                }
                if (list.Count > 0) attack3Frames = list.ToArray();
            }

            if (airAttackFrames == null || airAttackFrames.Length == 0)
            {
                var list = new List<Sprite>();
                for (int i = 0; i < 6; i++)
                {
                    string target = "AIR ATTACK_" + i;
                    var s = System.Array.Find(allSprites, x => x != null && x.name == target);
                    if (s != null) list.Add(s);
                }
                if (list.Count > 0) airAttackFrames = list.ToArray();
            }
        }

        void Update()
        {
            if (!attacksUnlocked) return;
            if (playerHealth != null && playerHealth.IsDead) return;

            bool isGrounded = playerMovement != null && playerMovement.IsGrounded;

            // reset air attack counter on landing
            if (isGrounded)
            {
                hasAirAttacked = false;
            }

            // handle combo reset timer when idle
            if (!isAttacking && comboIndex > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f)
                {
                    comboIndex = 0;
                }
            }

            // check inputs
            if (isGrounded)
            {
                if (Input.GetKeyDown(groundAttackKey))
                {
                    if (!isAttacking)
                    {
                        StartGroundAttack(comboIndex);
                    }
                    else
                    {
                        // buffer combo input for the next attack step
                        comboBuffered = true;
                    }
                }
            }
            else
            {
                if (!isAttacking && !hasAirAttacked &&
                    (Input.GetKeyDown(groundAttackKey) || Input.GetKeyDown(airAttackKey)))
                {
                    StartAirAttack();
                }
            }
        }

        public void UnlockAttacks()
        {
            attacksUnlocked = true;
        }

        void StartGroundAttack(int step)
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }
            attackRoutine = StartCoroutine(GroundAttackRoutine(step));
        }

        void StartAirAttack()
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }
            attackRoutine = StartCoroutine(AirAttackRoutine());
        }

        IEnumerator GroundAttackRoutine(int step)
        {
            isAttacking = true;
            comboBuffered = false;
            hitTargets.Clear();

            // disable animator so it does not overwrite attack sprites
            if (anim != null) anim.enabled = false;

            Sprite[] frames = GetGroundAttackFrames(step);
            int hitFrame = GetGroundHitFrame(step);
            int damage = (step == 2) ? 2 : 1;

            if (frames != null && frames.Length > 0)
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    if (frames[i] != null && sr != null)
                    {
                        sr.sprite = frames[i];
                    }

                    // execute hit detection on the active frames
                    if (i == hitFrame || (step > 0 && i == hitFrame + 1))
                    {
                        PerformHitDetection(damage);
                    }

                    yield return new WaitForSeconds(frameDuration);
                }
            }

            // re-enable animator
            if (anim != null) anim.enabled = true;

            isAttacking = false;

            // combo progression logic
            if (comboBuffered && step < 2)
            {
                comboIndex = step + 1;
                StartGroundAttack(comboIndex);
            }
            else if (step < 2)
            {
                comboIndex = step + 1;
                comboTimer = comboResetDuration;
            }
            else
            {
                // combo finished, reset to step 0
                comboIndex = 0;
                comboTimer = 0f;
            }
        }

        IEnumerator AirAttackRoutine()
        {
            isAttacking = true;
            hasAirAttacked = true;
            hitTargets.Clear();

            if (anim != null) anim.enabled = false;

            Sprite[] frames = airAttackFrames;
            int damage = 2;

            if (frames != null && frames.Length > 0)
            {
                for (int i = 0; i < frames.Length; i++)
                {
                    if (frames[i] != null && sr != null)
                    {
                        sr.sprite = frames[i];
                    }

                    // active hit detection on frames 2 and 3
                    if (i == 2 || i == 3)
                    {
                        PerformHitDetection(damage);
                    }

                    yield return new WaitForSeconds(frameDuration);

                    // cancel early if landed
                    if (playerMovement != null && playerMovement.IsGrounded && i > 2)
                    {
                        break;
                    }
                }
            }

            if (anim != null) anim.enabled = true;
            isAttacking = false;
        }

        void PerformHitDetection(int damage)
        {
            float facingDir = Mathf.Sign(transform.localScale.x);
            Vector2 origin = (Vector2)transform.position + new Vector2(attackOffset.x * facingDir, attackOffset.y);

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRadius, enemyLayers);
            if (hits == null || hits.Length == 0) return;

            bool hitSomething = false;

            foreach (var col in hits)
            {
                if (col == null || col.isTrigger) continue;
                if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
                var damageable = col.GetComponent<IDamageable>() ?? col.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead && hitTargets.Add(damageable))
                {
                    damageable.TakeDamage(damage);
                    hitSomething = true;
                }
            }

            if (hitSomething)
            {
                StartCoroutine(HitStopRoutine(0.04f));
            }
        }

        IEnumerator HitStopRoutine(float duration)
        {
            // micro pause on successful attack hit for punchy game feel
            Time.timeScale = 0.1f;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1.0f;
        }

        Sprite[] GetGroundAttackFrames(int step)
        {
            switch (step)
            {
                case 0: return attack1Frames;
                case 1: return attack2Frames;
                case 2: return attack3Frames;
                default: return attack1Frames;
            }
        }

        int GetGroundHitFrame(int step)
        {
            switch (step)
            {
                case 0: return 4;
                case 1: return 3;
                case 2: return 2;
                default: return 3;
            }
        }
    }
}
