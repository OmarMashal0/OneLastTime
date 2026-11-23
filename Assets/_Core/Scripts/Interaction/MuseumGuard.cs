using System.Collections;
using UnityEngine;
using OneLastTime.Combat;
using OneLastTime.Player;

namespace OneLastTime.Enemies
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public class MuseumGuard : MonoBehaviour, IDamageable
    {
        public enum GuardState
        {
            Sleeping,
            Patrolling,
            PausingAtEndpoint,
            AlertNotice,
            Aiming,
            Shooting,
            Cooldown,
            Surrendered
        }

        [Header("Sleeping Behavior")]
        [SerializeField] private bool isSleeping = false;
        [SerializeField] private float hearingRadius = 5.5f;
        [SerializeField] private bool wakeForSwordPickupOnly;

        [Header("Patrol Endpoints")]
        [SerializeField] private Transform endpointA;
        [SerializeField] private Transform endpointB;
        [SerializeField] private float patrolDistanceLeft = 4.0f;
        [SerializeField] private float patrolDistanceRight = 4.0f;

        [Header("Speeds & Timings")]
        [SerializeField] private float patrolSpeed = 2.0f;
        [SerializeField] private float pauseAtEndpointDuration = 1.0f;
        [SerializeField] private float noticeDuration = 0.25f;
        [SerializeField] private float aimDuration = 0.50f;
        [SerializeField] private float postShootCooldown = 1.2f;

        [Header("Vision & Combat")]
        [SerializeField] private GuardVisionCone visionCone;
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private GameObject bulletPrefab;

        [Header("Surrender Animation")]
        [SerializeField] private Sprite[] surrenderSprites;
        [SerializeField] private float surrenderFrameRate = 8f;
        [SerializeField] private Sprite surrenderSizeReference;
        private Sprite activeSurrenderSprite;

        [Header("Audio")]
        [SerializeField] private AudioClip gunshotSound;
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 0.9f;

        private Animator anim;
        private SpriteRenderer sr;
        private Collider2D guardCol;

        private GuardState currentState = GuardState.Patrolling;
        private Vector3 startPos;
        private float worldMinX;
        private float worldMaxX;
        private int movingDirection = 1;
        private float pauseTimer;
        private float originalScaleX;
        private Coroutine alertRoutine;
        private Coroutine swordPickupReaction;
        private bool initialIsSleeping;

        private Transform playerTransform;

        [Header("Health & Damage")]
        [SerializeField] private int maxHealth = 3;
        private int currentHealth = 3;
        private bool isDead = false;

        public bool IsDead => isDead;
        public GuardState CurrentState => currentState;
        public bool IsReactingToSwordPickup => swordPickupReaction != null;

        void Awake()
        {
            anim = GetComponent<Animator>();
            sr = GetComponent<SpriteRenderer>();
            guardCol = GetComponent<Collider2D>();

            startPos = transform.position;
            originalScaleX = Mathf.Abs(transform.localScale.x);
            if (originalScaleX < 0.01f)
            {
                originalScaleX = Mathf.Abs(transform.localScale.y) > 0.01f ? Mathf.Abs(transform.localScale.y) : 0.28f;
            }
            if (Mathf.Abs(transform.localScale.x) < 0.01f)
            {
                Vector3 curScale = transform.localScale;
                curScale.x = originalScaleX;
                transform.localScale = curScale;
            }

            // calculate patrol bounds
            worldMinX = (endpointA != null) ? endpointA.position.x : startPos.x - patrolDistanceLeft;
            worldMaxX = (endpointB != null) ? endpointB.position.x : startPos.x + patrolDistanceRight;

            if (worldMinX > worldMaxX)
            {
                float temp = worldMinX;
                worldMinX = worldMaxX;
                worldMaxX = temp;
            }

            if (visionCone == null)
            {
                visionCone = GetComponentInChildren<GuardVisionCone>();
            }

            initialIsSleeping = isSleeping;
            if (isSleeping)
            {
                currentState = GuardState.Sleeping;
                if (visionCone != null)
                {
                    visionCone.gameObject.SetActive(false);
                }
            }
        }

        void Start()
        {
            FindPlayer();
            SetFacingDirection(movingDirection);

            if (isSleeping && anim != null)
            {
                anim.SetBool("isSleeping", true);
                anim.Play("Sleep", 0, 0f);
            }
        }

        void FindPlayer()
        {
            var pm = FindObjectOfType<PlayerMovement>();
            if (pm != null)
            {
                playerTransform = pm.transform;
                return;
            }

            var ph = FindObjectOfType<PlayerHealth>();
            if (ph != null)
            {
                playerTransform = ph.transform;
                return;
            }

            var ryu = GameObject.Find("player_ryu");
            if (ryu != null)
            {
                playerTransform = ryu.transform;
                return;
            }

            var p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                var childCol = p.GetComponentInChildren<Collider2D>();
                playerTransform = (childCol != null) ? childCol.transform : p.transform;
            }
        }

        void Update()
        {
            if (isDead) return;

            if (playerTransform == null)
            {
                FindPlayer();
            }

            if (currentState == GuardState.Surrendered)
            {
                return;
            }

            if (currentState == GuardState.Sleeping)
            {
                CheckHearing();
                return;
            }

            if (currentState == GuardState.Patrolling || currentState == GuardState.PausingAtEndpoint)
            {
                CheckVision();
            }

            switch (currentState)
            {
                case GuardState.Patrolling:
                    ExecutePatrol();
                    break;

                case GuardState.PausingAtEndpoint:
                    ExecutePause();
                    break;
            }
        }

        void ExecutePatrol()
        {
            if (anim != null) anim.SetBool("isWalking", true);

            float targetX = (movingDirection > 0) ? worldMaxX : worldMinX;
            float step = patrolSpeed * Time.deltaTime;
            Vector3 pos = transform.position;

            pos.x = Mathf.MoveTowards(pos.x, targetX, step);
            transform.position = pos;

            // reached edge of patrol route
            if (Mathf.Abs(pos.x - targetX) < 0.05f)
            {
                currentState = GuardState.PausingAtEndpoint;
                pauseTimer = pauseAtEndpointDuration;
                if (anim != null) anim.SetBool("isWalking", false);
            }
        }

        void ExecutePause()
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                // turn around and continue walking
                movingDirection = -movingDirection;
                SetFacingDirection(movingDirection);
                currentState = GuardState.Patrolling;
            }
        }

        void CheckHearing()
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            var health = playerTransform.GetComponent<PlayerHealth>() ?? playerTransform.GetComponentInParent<PlayerHealth>();
            if (health != null && health.IsDead) return;

            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= hearingRadius)
            {
                var pm = playerTransform.GetComponent<PlayerMovement>() ?? playerTransform.GetComponentInParent<PlayerMovement>();
                if (pm != null && pm.IsMakingFootstepNoise)
                {
                    WakeUpAndAlert();
                }
            }
        }

        public void WakeUpAndAlert()
        {
            if (wakeForSwordPickupOnly || currentState != GuardState.Sleeping) return;
            if (alertRoutine != null) StopCoroutine(alertRoutine);
            alertRoutine = StartCoroutine(WakeUpAndShootRoutine());
        }

        public void ReactToSwordPickup(Transform player)
        {
            if (isDead || currentState == GuardState.Surrendered || swordPickupReaction != null) return;
            if (alertRoutine != null) StopCoroutine(alertRoutine);
            alertRoutine = null;
            playerTransform = player;
            swordPickupReaction = StartCoroutine(SwordPickupReaction());
        }

        IEnumerator SwordPickupReaction()
        {
            bool wasSleeping = currentState == GuardState.Sleeping;
            currentState = GuardState.AlertNotice;
            isSleeping = false;

            if (playerTransform != null)
            {
                SetFacingDirection(playerTransform.position.x < transform.position.x ? -1 : 1);
            }

            if (visionCone != null) visionCone.gameObject.SetActive(false);

            anim.ResetTrigger("wakeUp");
            anim.ResetTrigger("notice");
            anim.ResetTrigger("aim");
            anim.ResetTrigger("shoot");
            anim.ResetTrigger("returnToPatrol");
            anim.SetBool("isSleeping", false);
            anim.SetBool("isWalking", false);
            if (wasSleeping)
            {
                anim.Play("WakeUp", 0, 0f);
                yield return new WaitForSeconds(0.6f);
            }

            if (visionCone != null)
            {
                visionCone.SetAlert(false);
                visionCone.gameObject.SetActive(true);
            }

            anim.Play("Notice", 0, 0f);
            yield return new WaitForSeconds(0.6f);

            currentState = GuardState.Aiming;
            anim.Play("Aim", 0, 0f);
            if (visionCone != null) visionCone.SetAlert(true);
            yield return new WaitForSeconds(0.75f);
            swordPickupReaction = null;
        }

        public IEnumerator FireSwordPickupShot()
        {
            if (isDead || currentState != GuardState.Aiming) yield break;

            currentState = GuardState.Shooting;
            anim.Play("Shoot", 0, 0f);
            if (gunshotSound == null) yield break;

            Vector3 shotPosition = transform.position;
            AudioSource.PlayClipAtPoint(gunshotSound, shotPosition, soundVolume);
            yield return new WaitForSecondsRealtime(0.19f);
            AudioSource.PlayClipAtPoint(gunshotSound, shotPosition, soundVolume * 0.42f);
            yield return new WaitForSecondsRealtime(0.25f);
            AudioSource.PlayClipAtPoint(gunshotSound, shotPosition, soundVolume * 0.17f);
            yield return new WaitForSecondsRealtime(gunshotSound.length);
        }

        public void Surrender(Sprite[] overrideFrames = null)
        {
            if (currentState == GuardState.Surrendered || isDead) return;

            if (alertRoutine != null)
            {
                StopCoroutine(alertRoutine);
                alertRoutine = null;
            }

            StopAllCoroutines();
            swordPickupReaction = null;

            currentState = GuardState.Surrendered;
            isSleeping = false;

            if (visionCone != null)
            {
                visionCone.SetAlert(false);
                visionCone.gameObject.SetActive(false);
            }

            if (overrideFrames != null && overrideFrames.Length > 0)
            {
                surrenderSprites = overrideFrames;
            }

            // disable animator to take direct control over sprite rendering
            if (anim != null)
            {
                anim.enabled = false;
            }

            if (playerTransform == null) FindPlayer();
            if (playerTransform != null)
            {
                float dirToPlayer = Mathf.Sign(playerTransform.position.x - transform.position.x);
                if (dirToPlayer != 0) SetFacingDirection((int)dirToPlayer);
            }

            StartCoroutine(SurrenderAnimationRoutine());
        }

        IEnumerator SurrenderAnimationRoutine()
        {
            // load surrender sprites if not assigned
            if (surrenderSprites == null || surrenderSprites.Length == 0)
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                if (allSprites != null)
                {
                    var list = new System.Collections.Generic.List<Sprite>();
                    for (int i = 0; i < 4; i++)
                    {
                        var s = System.Array.Find(allSprites, sp => sp != null && sp.name == "guard_surrender_" + i);
                        if (s != null) list.Add(s);
                    }
                    if (list.Count > 0) surrenderSprites = list.ToArray();
                }
            }

            if (surrenderSprites == null || surrenderSprites.Length == 0)
            {
                yield break;
            }

            if (surrenderSizeReference != null && surrenderSprites[0] != null)
            {
                // Match the standing sprite even if Unity imports the surrender sheet at a different PPU.
                float scale = surrenderSizeReference.rect.height / surrenderSizeReference.pixelsPerUnit
                    * surrenderSprites[0].pixelsPerUnit / surrenderSprites[0].rect.height;
                Vector3 localScale = transform.localScale;
                transform.localScale = new Vector3(localScale.x * scale, localScale.y * scale, localScale.z);

                if (guardCol is CapsuleCollider2D capsule)
                {
                    capsule.size /= scale;
                    capsule.offset /= scale;
                }
            }

            // instantly set to first frame so aiming sprite is removed immediately
            activeSurrenderSprite = surrenderSprites[0];
            if (sr != null)
            {
                sr.sprite = activeSurrenderSprite;
            }

            float frameDelay = 1f / Mathf.Max(surrenderFrameRate, 1f);

            // play through frames 0 to last
            for (int i = 0; i < surrenderSprites.Length; i++)
            {
                activeSurrenderSprite = surrenderSprites[i];
                if (sr != null && activeSurrenderSprite != null)
                {
                    sr.sprite = activeSurrenderSprite;
                }
                yield return new WaitForSeconds(frameDelay);
            }

            // hold the last surrender frame (hands raised)
            if (surrenderSprites.Length > 0)
            {
                activeSurrenderSprite = surrenderSprites[surrenderSprites.Length - 1];
                if (sr != null)
                {
                    sr.sprite = activeSurrenderSprite;
                }
            }
        }

        IEnumerator WakeUpAndShootRoutine()
        {
            currentState = GuardState.AlertNotice;
            isSleeping = false;

            if (anim != null)
            {
                anim.SetBool("isSleeping", false);
                anim.SetTrigger("wakeUp");
            }

            if (visionCone != null)
            {
                visionCone.gameObject.SetActive(true);
                visionCone.SetAlert(true);
            }

            if (playerTransform != null)
            {
                float dirToPlayer = Mathf.Sign(playerTransform.position.x - transform.position.x);
                if (dirToPlayer != 0) SetFacingDirection((int)dirToPlayer);
            }

            // wake up animation
            yield return new WaitForSeconds(0.60f);

            // aim at player
            currentState = GuardState.Aiming;
            if (anim != null) anim.SetTrigger("aim");

            yield return new WaitForSeconds(aimDuration);

            // check if player moved into cover
            Vector3 targetTorso;
            if (visionCone != null && !visionCone.CanShootPlayer(playerTransform, out targetTorso))
            {
                yield return ReturnToPatrolRoutine();
                yield break;
            }

            // shoot
            currentState = GuardState.Shooting;
            if (anim != null) anim.SetTrigger("shoot");

            yield return new WaitForSeconds(0.12f);

            SpawnBullet();

            // recovery before returning to idle
            yield return new WaitForSeconds(postShootCooldown);

            yield return ReturnToPatrolRoutine();
        }

        void CheckVision()
        {
            if (visionCone == null) return;
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            var health = playerTransform.GetComponent<PlayerHealth>() ?? playerTransform.GetComponentInParent<PlayerHealth>();
            if (health != null && health.IsDead) return;

            Vector3 torsoPoint;
            if (visionCone.CanSeePlayer(playerTransform, out torsoPoint))
            {
                // player spotted
                if (alertRoutine != null) StopCoroutine(alertRoutine);
                alertRoutine = StartCoroutine(AlertAndShootRoutine());
            }
        }

        IEnumerator AlertAndShootRoutine()
        {
            currentState = GuardState.AlertNotice;
            if (anim != null)
            {
                anim.SetBool("isWalking", false);
                anim.SetTrigger("notice");
            }
            if (visionCone != null) visionCone.SetAlert(true);

            // face towards player
            float dirToPlayer = Mathf.Sign(playerTransform.position.x - transform.position.x);
            if (dirToPlayer != 0) SetFacingDirection((int)dirToPlayer);

            // brief notice delay
            yield return new WaitForSeconds(noticeDuration);

            // aim
            currentState = GuardState.Aiming;
            if (anim != null) anim.SetTrigger("aim");

            yield return new WaitForSeconds(aimDuration);

            // recheck line of sight
            Vector3 targetTorso;
            if (visionCone != null && !visionCone.CanShootPlayer(playerTransform, out targetTorso))
            {
                yield return ReturnToPatrolRoutine();
                yield break;
            }

            // shoot
            currentState = GuardState.Shooting;
            if (anim != null) anim.SetTrigger("shoot");

            yield return new WaitForSeconds(0.12f);

            SpawnBullet();

            // wait cooldown
            yield return new WaitForSeconds(postShootCooldown);

            yield return ReturnToPatrolRoutine();
        }

        IEnumerator ReturnToPatrolRoutine()
        {
            currentState = GuardState.Cooldown;
            if (visionCone != null) visionCone.SetAlert(false);
            if (anim != null) anim.SetTrigger("returnToPatrol");

            yield return new WaitForSeconds(0.4f);

            if (initialIsSleeping)
            {
                currentState = GuardState.PausingAtEndpoint;
                pauseTimer = 999999f;
                if (anim != null) anim.SetBool("isWalking", false);
            }
            else
            {
                currentState = GuardState.Patrolling;
            }

            alertRoutine = null;
        }

        void SpawnBullet()
        {
            if (gunshotSound != null)
            {
                AudioSource.PlayClipAtPoint(gunshotSound, transform.position, soundVolume);
            }

            // wake up nearby guards
            Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 12f);
            foreach (var c in nearby)
            {
                var otherGuard = c.GetComponent<MuseumGuard>();
                if (otherGuard != null && otherGuard != this && otherGuard.CurrentState == GuardState.Sleeping)
                {
                    otherGuard.WakeUpAndAlert();
                }
            }

            if (bulletPrefab == null) return;

            Vector3 spawnPos = (muzzlePoint != null) ? muzzlePoint.position : transform.position + Vector3.up * 1.5f;
            Vector3 targetTorso = (playerTransform != null) ? playerTransform.position : spawnPos + new Vector3(Mathf.Sign(transform.localScale.x) * 10f, 0f, 0f);

            Vector2 direction = (targetTorso - spawnPos).normalized;

            var bulletGo = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            var bullet = bulletGo.GetComponent<GuardBullet>();
            if (bullet != null)
            {
                bullet.Initialize(direction, guardCol);
            }
        }

        public void SetFacingDirection(int dir)
        {
            if (originalScaleX < 0.01f)
            {
                originalScaleX = Mathf.Abs(transform.localScale.y) > 0.01f ? Mathf.Abs(transform.localScale.y) : 0.28f;
            }
            Vector3 s = transform.localScale;
            s.x = originalScaleX * (dir >= 0 ? 1f : -1f);
            transform.localScale = s;
        }

        public void ResetGuard()
        {
            if (currentState == GuardState.Surrendered) return;

            if (swordPickupReaction != null)
            {
                StopCoroutine(swordPickupReaction);
                swordPickupReaction = null;
            }

            if (alertRoutine != null)
            {
                StopCoroutine(alertRoutine);
                alertRoutine = null;
            }

            transform.position = startPos;
            movingDirection = 1;
            SetFacingDirection(movingDirection);

            if (initialIsSleeping)
            {
                isSleeping = true;
                currentState = GuardState.Sleeping;

                if (visionCone != null)
                {
                    visionCone.SetAlert(false);
                    visionCone.gameObject.SetActive(false);
                }

                if (anim != null)
                {
                    anim.ResetTrigger("notice");
                    anim.ResetTrigger("wakeUp");
                    anim.ResetTrigger("aim");
                    anim.ResetTrigger("shoot");
                    anim.SetBool("isWalking", false);
                    anim.SetBool("isSleeping", true);
                    anim.Play("Sleep", 0, 0f);
                }
            }
            else
            {
                currentState = GuardState.Patrolling;

                if (visionCone != null) visionCone.SetAlert(false);

                if (anim != null)
                {
                    anim.ResetTrigger("notice");
                    anim.ResetTrigger("aim");
                    anim.ResetTrigger("shoot");
                    anim.SetTrigger("returnToPatrol");
                    anim.SetBool("isWalking", false);
                }
            }
        }

        public void TakeDamage(int damage)
        {
            if (isDead) return;

            currentHealth -= damage;

            // wake up if sleeping
            if (isSleeping)
            {
                isSleeping = false;
                WakeUpAndAlert();
            }

            StartCoroutine(DamageFeedbackRoutine());

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        IEnumerator DamageFeedbackRoutine()
        {
            if (sr == null) yield break;
            Color original = sr.color;
            sr.color = new Color(1f, 0.3f, 0.3f, 1f);
            yield return new WaitForSeconds(0.12f);
            if (sr != null && !isDead)
            {
                sr.color = original;
            }
        }

        void Die()
        {
            isDead = true;
            if (alertRoutine != null) StopCoroutine(alertRoutine);
            if (swordPickupReaction != null)
            {
                StopCoroutine(swordPickupReaction);
                swordPickupReaction = null;
            }

            if (visionCone != null)
            {
                visionCone.gameObject.SetActive(false);
            }

            if (guardCol != null)
            {
                guardCol.enabled = false;
            }

            StartCoroutine(DieRoutine());
        }

        IEnumerator DieRoutine()
        {
            if (sr != null)
            {
                sr.color = new Color(1f, 0.2f, 0.2f, 1f);
            }

            float elapsed = 0f;
            float duration = 0.45f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                if (sr != null)
                {
                    Color c = sr.color;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    sr.color = c;
                }
                yield return null;
            }

            gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            // enforce surrender sprite in case animator or other systems attempt override
            if (currentState == GuardState.Surrendered && activeSurrenderSprite != null && sr != null)
            {
                sr.sprite = activeSurrenderSprite;
            }
        }

        void OnDrawGizmosSelected()
        {
            if (isSleeping || initialIsSleeping)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.4f);
                Gizmos.DrawWireSphere(transform.position, hearingRadius);
            }

            Gizmos.color = Color.cyan;
            Vector3 pA = (endpointA != null) ? endpointA.position : transform.position + Vector3.left * patrolDistanceLeft;
            Vector3 pB = (endpointB != null) ? endpointB.position : transform.position + Vector3.right * patrolDistanceRight;

            Gizmos.DrawWireSphere(pA, 0.3f);
            Gizmos.DrawWireSphere(pB, 0.3f);
            Gizmos.DrawLine(pA, pB);
        }
    }
}
