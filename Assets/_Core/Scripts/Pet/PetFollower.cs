using System.Collections;
using UnityEngine;

namespace OneLastTime.Pet
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PetFollower : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [SerializeField] private float followDistance = 1.4f;
        [SerializeField] private float stopDistance = 0.35f;
        [SerializeField] private float walkSpeed = 6.5f;
        [SerializeField] private float sprintSpeed = 11.0f;
        [SerializeField] private float teleportDistance = 15.0f;

        [Header("Jump / Obstacle Handling")]
        [SerializeField] private float jumpForce = 10.0f;
        [SerializeField] private float wallCheckDistance = 0.4f;
        [SerializeField] private LayerMask groundLayer;

        private Rigidbody2D rb;
        private Collider2D col;
        private Animator anim;
        private SpriteRenderer spriteRenderer;
        private Color originalColor = Color.white;
        private bool isGrounded;
        private bool isMoving;
        private bool isHiding;
        private Vector3 initialScale;
        private PlayerMovement targetMovement;
        private Coroutine hidingRoutine;

        public bool IsHiding => isHiding;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            anim = GetComponentInChildren<Animator>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            initialScale = transform.localScale;

            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
                spriteRenderer.sortingLayerName = "ground";
                if (spriteRenderer.sortingOrder < 10)
                {
                    spriteRenderer.sortingOrder = 10;
                }
            }

            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (groundLayer.value == 0)
            {
                groundLayer = LayerMask.GetMask("ground", "Default");
            }
        }

        void Start()
        {
            // find ryu if target is not manually assigned
            if (target == null)
            {
                var player = GameObject.Find("Player_Ryu");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (target != null)
            {
                targetMovement = target.GetComponent<PlayerMovement>();

                if (rb != null)
                {
                    // snap right behind player on start
                    float playerFacing = Mathf.Sign(target.localScale.x);
                    float behindDirection = (playerFacing > 0f) ? -1f : 1f;
                    rb.position = (Vector2)target.position + new Vector2(behindDirection * followDistance, 0f);
                }

                IgnorePlayerCollisions();
            }

            IgnoreGuardCollisions();
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            // pass through player or guards if contact occurs
            if (collision.gameObject.CompareTag("Player") || (target != null && collision.transform.IsChildOf(target)))
            {
                if (col != null && collision.collider != null)
                {
                    Physics2D.IgnoreCollision(col, collision.collider, true);
                }
            }
            else if (collision.gameObject.GetComponent<OneLastTime.Enemies.MuseumGuard>() != null)
            {
                if (col != null && collision.collider != null)
                {
                    Physics2D.IgnoreCollision(col, collision.collider, true);
                }
            }
        }

        void Update()
        {
            // update animation parameters
            if (anim != null)
            {
                anim.SetBool("isMoving", isMoving && !isHiding);
                anim.SetFloat("moveSpeed", isHiding ? 0f : Mathf.Abs(rb.velocity.x));
            }
        }

        void FixedUpdate()
        {
            if (target == null || rb == null) return;

            // stay still in cover while hiding
            if (isHiding)
            {
                isMoving = false;
                rb.velocity = new Vector2(0f, rb.velocity.y);
                return;
            }

            CheckGrounded();

            // determine target follow spot behind ryu
            float playerFacing = Mathf.Sign(target.localScale.x);
            float behindDirection = (playerFacing > 0f) ? -1f : 1f;
            Vector2 targetPosition = (Vector2)target.position + new Vector2(behindDirection * followDistance, 0f);

            float distToTarget = Vector2.Distance(rb.position, targetPosition);
            float distToPlayer = Vector2.Distance(rb.position, target.position);

            // teleport if too far away (e.g. door transitions or large drops)
            if (distToPlayer > teleportDistance)
            {
                rb.position = targetPosition;
                rb.velocity = Vector2.zero;
                return;
            }

            float horizontalDelta = targetPosition.x - rb.position.x;
            float absX = Mathf.Abs(horizontalDelta);

            if (absX > stopDistance)
            {
                isMoving = true;
                float moveDir = Mathf.Sign(horizontalDelta);

                float speed = walkSpeed;
                if (distToPlayer > 4.5f)
                {
                    speed = sprintSpeed;
                }
                else if (targetMovement != null && targetMovement.IsSneaking)
                {
                    speed = targetMovement.sneakSpeed;
                }

                rb.velocity = new Vector2(moveDir * speed, rb.velocity.y);

                // jump over low ledges or when player is on a higher platform
                bool playerHigher = (target.position.y - rb.position.y > 1.0f) && (absX < 3.5f);
                if (isGrounded && (playerHigher || IsObstacleAhead(moveDir)))
                {
                    rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                }

                // sprite frames face right naturally:
                // moving right: positive scale.x; moving left: negative scale.x
                Vector3 s = transform.localScale;
                s.x = (moveDir > 0f) ? Mathf.Abs(initialScale.x) : -Mathf.Abs(initialScale.x);
                transform.localScale = s;
            }
            else
            {
                // idle behind player
                isMoving = false;
                rb.velocity = new Vector2(Mathf.MoveTowards(rb.velocity.x, 0f, 25f * Time.fixedDeltaTime), rb.velocity.y);

                // face same direction as player when stationary
                Vector3 s = transform.localScale;
                s.x = (playerFacing > 0f) ? Mathf.Abs(initialScale.x) : -Mathf.Abs(initialScale.x);
                transform.localScale = s;
            }
        }

        public void SetHiding(bool hiding, Color shadowCol, float scaleMult, float duration)
        {
            isHiding = hiding;

            if (hidingRoutine != null)
            {
                StopCoroutine(hidingRoutine);
            }
            hidingRoutine = StartCoroutine(AnimateHiding(hiding, shadowCol, scaleMult, duration));
        }

        private IEnumerator AnimateHiding(bool hiding, Color shadowCol, float scaleMult, float duration)
        {
            if (spriteRenderer == null) yield break;

            Color startColor = spriteRenderer.color;
            Color targetColor = hiding ? shadowCol : originalColor;

            Vector3 startScale = transform.localScale;
            float currentSign = Mathf.Sign(transform.localScale.x);
            if (currentSign == 0f) currentSign = 1f;

            Vector3 targetScale = initialScale * (hiding ? scaleMult : 1f);
            targetScale.x = Mathf.Abs(targetScale.x) * currentSign;

            Vector2 startPos = rb.position;
            Vector2 targetPos = startPos;
            if (hiding && target != null)
            {
                float playerFacing = Mathf.Sign(target.localScale.x);
                float behindDir = (playerFacing > 0f) ? -1f : 1f;
                // tuck in close behind player in cover
                targetPos = new Vector2(target.position.x + behindDir * 0.45f, rb.position.y);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                if (hiding)
                {
                    rb.position = Vector2.Lerp(startPos, targetPos, t);
                }
                yield return null;
            }

            spriteRenderer.color = targetColor;
            transform.localScale = targetScale;
            if (hiding)
            {
                rb.position = targetPos;
                rb.velocity = Vector2.zero;
            }
        }

        private void IgnorePlayerCollisions()
        {
            if (target == null) return;
            var playerColliders = target.GetComponentsInChildren<Collider2D>();
            var petColliders = GetComponentsInChildren<Collider2D>();
            foreach (var pc in playerColliders)
            {
                if (pc == null) continue;
                foreach (var mc in petColliders)
                {
                    if (mc == null) continue;
                    Physics2D.IgnoreCollision(pc, mc, true);
                }
            }
        }

        private void IgnoreGuardCollisions()
        {
            var guards = FindObjectsOfType<OneLastTime.Enemies.MuseumGuard>();
            var petCols = GetComponentsInChildren<Collider2D>();
            foreach (var g in guards)
            {
                if (g == null) continue;
                var gCol = g.GetComponent<Collider2D>();
                if (gCol != null)
                {
                    foreach (var pCol in petCols)
                    {
                        if (pCol != null)
                        {
                            Physics2D.IgnoreCollision(pCol, gCol, true);
                        }
                    }
                }
            }
        }

        void CheckGrounded()
        {
            Vector2 origin = (col != null) ? (Vector2)col.bounds.center : (Vector2)transform.position;
            float dist = (col != null) ? (col.bounds.extents.y + 0.15f) : 0.5f;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, dist, groundLayer);
            isGrounded = hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger;
        }

        bool IsObstacleAhead(float dir)
        {
            Vector2 origin = (col != null) ? (Vector2)col.bounds.center : (Vector2)transform.position;
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * dir, wallCheckDistance, groundLayer);
            return hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger;
        }
    }
}
