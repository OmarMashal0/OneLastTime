using UnityEngine;

namespace OneLastTime.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 7f;
        public float jumpForce = 12f;

        [Header("Wall Mechanics")]
        public float wallSlideSpeed = 2.5f;
        public Vector2 wallJumpForce = new Vector2(9.5f, 14.5f);
        public float wallJumpDuration = 0.2f;
        public float wallCheckDistance = 0.2f;

        [Header("Ground Detection")]
        public LayerMask groundLayer;
        public Transform groundCheck;
        public float groundCheckRadius = 0.2f;

        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private float horizontalInput;
        private bool isGrounded;
        private bool isTouchingWall;
        private int wallDirection;
        private bool isWallSliding;
        private float wallJumpTimer;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            boxCollider = GetComponent<BoxCollider2D>();
            animator = GetComponentInChildren<Animator>();
            spriteRenderer = animator != null ? animator.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();

            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (groundLayer.value == 0)
            {
                groundLayer = ~LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer));
            }
        }

        void Update()
        {
            // read horizontal input
            horizontalInput = Input.GetAxisRaw("Horizontal");

            if (wallJumpTimer > 0f)
            {
                wallJumpTimer -= Time.deltaTime;
            }

            CheckGrounded();
            CheckWall();

            if (animator != null)
            {
                animator.SetFloat("moveSpeed", Mathf.Abs(rb.velocity.x));
                animator.SetFloat("jumpHeight", rb.velocity.y);
                animator.SetBool("grounded", isGrounded);
                animator.SetBool("isWallSliding", isWallSliding);
                animator.SetBool("isWallContact", isTouchingWall && !isWallSliding && !isGrounded);
            }

            // jump when space is pressed
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (isGrounded)
                {
                    rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                }
                else if (isTouchingWall)
                {
                    wallJumpTimer = wallJumpDuration;
                    rb.velocity = new Vector2(-wallDirection * wallJumpForce.x, wallJumpForce.y);
                    isWallSliding = false;

                    if (spriteRenderer != null)
                    {
                        spriteRenderer.flipX = (-wallDirection < 0);
                    }
                    if (animator != null)
                    {
                        animator.SetTrigger("wallJump");
                    }
                }
            }

            // flip sprite based on movement or wall contact
            if (spriteRenderer != null)
            {
                if (isTouchingWall && !isGrounded && wallJumpTimer <= 0f)
                {
                    // when on a right wall face left, when on a left wall face right
                    spriteRenderer.flipX = (wallDirection > 0);
                }
                else if (horizontalInput != 0f && wallJumpTimer <= 0f)
                {
                    spriteRenderer.flipX = (horizontalInput < 0f);
                }
            }
        }

        void FixedUpdate()
        {
            if (wallJumpTimer <= 0f)
            {
                rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
            }

            if (isWallSliding && rb.velocity.y < -wallSlideSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
            }
        }

        void CheckWall()
        {
            if (isGrounded)
            {
                isTouchingWall = false;
                wallDirection = 0;
                isWallSliding = false;
                return;
            }

            Vector2 origin = boxCollider != null ? (Vector2)boxCollider.bounds.center : (Vector2)transform.position;
            float halfWidth = boxCollider != null ? boxCollider.bounds.extents.x : 0.425f;
            float checkDist = halfWidth + wallCheckDistance;
            float vOffset = boxCollider != null ? (boxCollider.bounds.extents.y * 0.5f) : 0.5f;

            RaycastHit2D hitRight = Physics2D.Raycast(origin, Vector2.right, checkDist, groundLayer);
            if (hitRight.collider == null || hitRight.collider.gameObject == gameObject || hitRight.collider.isTrigger)
            {
                hitRight = Physics2D.Raycast(origin + Vector2.up * vOffset, Vector2.right, checkDist, groundLayer);
                if (hitRight.collider == null || hitRight.collider.gameObject == gameObject || hitRight.collider.isTrigger)
                {
                    hitRight = Physics2D.Raycast(origin + Vector2.down * vOffset, Vector2.right, checkDist, groundLayer);
                }
            }

            RaycastHit2D hitLeft = Physics2D.Raycast(origin, Vector2.left, checkDist, groundLayer);
            if (hitLeft.collider == null || hitLeft.collider.gameObject == gameObject || hitLeft.collider.isTrigger)
            {
                hitLeft = Physics2D.Raycast(origin + Vector2.up * vOffset, Vector2.left, checkDist, groundLayer);
                if (hitLeft.collider == null || hitLeft.collider.gameObject == gameObject || hitLeft.collider.isTrigger)
                {
                    hitLeft = Physics2D.Raycast(origin + Vector2.down * vOffset, Vector2.left, checkDist, groundLayer);
                }
            }

            bool rightValid = hitRight.collider != null && hitRight.collider.gameObject != gameObject && !hitRight.collider.isTrigger;
            bool leftValid = hitLeft.collider != null && hitLeft.collider.gameObject != gameObject && !hitLeft.collider.isTrigger;

            if (rightValid)
            {
                isTouchingWall = true;
                wallDirection = 1;
            }
            else if (leftValid)
            {
                isTouchingWall = true;
                wallDirection = -1;
            }
            else
            {
                isTouchingWall = false;
                wallDirection = 0;
                isWallSliding = false;
            }

            if (isTouchingWall)
            {
                isWallSliding = (rb.velocity.y <= 0.1f);
            }
            else
            {
                isWallSliding = false;
            }
        }

        void CheckGrounded()
        {
            if (groundCheck != null)
            {
                isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            }
            else if (boxCollider != null)
            {
                // check slightly below the collider
                Vector2 boxCenter = new Vector2(boxCollider.bounds.center.x, boxCollider.bounds.min.y);
                Vector2 boxSize = new Vector2(boxCollider.bounds.size.x * 0.9f, 0.1f);
                isGrounded = Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundLayer);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            if (groundCheck != null)
            {
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
            else
            {
                var col = GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    Vector2 boxCenter = new Vector2(col.bounds.center.x, col.bounds.min.y);
                    Vector2 boxSize = new Vector2(col.bounds.size.x * 0.9f, 0.1f);
                    Gizmos.DrawWireCube(boxCenter, boxSize);
                }
            }
        }
    }
}
