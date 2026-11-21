using System.Collections;
using UnityEngine;
using OneLastTime.Player;

namespace OneLastTime.Enemies
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public class MuseumGuard : MonoBehaviour
    {
        public enum GuardState
        {
            Sleeping,
            Patrolling,
            PausingAtEndpoint,
            AlertNotice,
            Aiming,
            Shooting,
            Cooldown
        }

        [Header("Sleeping Behavior")]
        [SerializeField] private bool isSleeping = false;
        [SerializeField] private float hearingRadius = 5.5f;

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
        private bool initialIsSleeping;

        private Transform playerTransform;

        public GuardState CurrentState => currentState;

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
            if (playerTransform == null)
            {
                FindPlayer();
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
            if (currentState != GuardState.Sleeping) return;
            if (alertRoutine != null) StopCoroutine(alertRoutine);
            alertRoutine = StartCoroutine(WakeUpAndShootRoutine());
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
