using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DummyPlayer : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundLayer = ~0;

    private Rigidbody2D body;
    private Collider2D playerCollider;
    private Animator animator;
    private float moveInput;
    private bool jumpRequested;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput != 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(moveInput);
            transform.localScale = scale;
        }

        UpdateAnimation();

        if (Input.GetButtonDown("Jump"))
        {
            jumpRequested = true;
        }
    }

    private void UpdateAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("isRunning", Mathf.Abs(moveInput) > 0.01f);
        }
    }

    private void FixedUpdate()
    {
        body.velocity = new Vector2(moveInput * moveSpeed, body.velocity.y);

        if (jumpRequested && IsGrounded())
        {
            body.velocity = new Vector2(body.velocity.x, jumpForce);
        }

        jumpRequested = false;
    }

    private bool IsGrounded()
    {
        if (playerCollider == null)
        {
            return Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        }

        Bounds bounds = playerCollider.bounds;
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            new Vector2(bounds.center.x, bounds.min.y),
            Vector2.down,
            groundCheckDistance,
            groundLayer);

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != playerCollider && hit.normal.y > 0.5f)
            {
                return true;
            }
        }

        return false;
    }
}
