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
            if (spriteRenderer != null && horizontalInput != 0f)
            {
                spriteRenderer.flipX = horizontalInput < 0f;
            }

            CheckGrounded();

            if (animator != null)
            {
                animator.SetFloat("moveSpeed", Mathf.Abs(rb.velocity.x));
                animator.SetFloat("jumpHeight", rb.velocity.y);
                animator.SetBool("grounded", isGrounded);
            }

            // jump when space is pressed and player is on ground
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            }
        }

        void FixedUpdate()
        {
            // apply horizontal velocity
            rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
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
