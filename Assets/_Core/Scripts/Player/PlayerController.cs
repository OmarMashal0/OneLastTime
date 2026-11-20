using UnityEngine;

namespace OneLastTime.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Horizontal running speed")]
        [SerializeField] private float moveSpeed = 7f;

        [Tooltip("Upward velocity applied when jumping")]
        [SerializeField] private float jumpForce = 12f;

        [Header("Ground Detection")]
        [Tooltip("Which layers count as solid ground. If set to Nothing, detects all non-player layers.")]
        [SerializeField] private LayerMask groundLayer;

        [Tooltip("Optional ground check point. If empty, automatically checks below player's collider.")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;

        private Rigidbody2D rb;
        private BoxCollider2D boxCollider;
        private float horizontalInput;
        private bool isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            boxCollider = GetComponent<BoxCollider2D>();

            // Ensure physics settings are configured for a 2D platformer
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Default groundLayer if left unassigned
            if (groundLayer.value == 0)
            {
                groundLayer = ~LayerMask.GetMask(LayerMask.LayerToName(gameObject.layer));
            }
        }

        private void Update()
        {
            // 1. Read player input (A/D or Left/Right Arrow keys)
            horizontalInput = Input.GetAxisRaw("Horizontal");

            // 2. Ground detection
            CheckGrounded();

            // 3. Jump (Spacebar)
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            }
        }

        private void FixedUpdate()
        {
            // 4. Move horizontally (keeps vertical velocity untouched for smooth gravity & jumping)
            rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
        }

        private void CheckGrounded()
        {
            if (groundCheck != null)
            {
                isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            }
            else if (boxCollider != null)
            {
                // Auto-detect ground slightly below the bottom of the player's box collider
                Vector2 boxCenter = new Vector2(boxCollider.bounds.center.x, boxCollider.bounds.min.y);
                Vector2 boxSize = new Vector2(boxCollider.bounds.size.x * 0.9f, 0.1f);
                isGrounded = Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundLayer);
            }
        }

        private void OnDrawGizmosSelected()
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

