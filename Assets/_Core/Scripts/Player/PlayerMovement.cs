using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7.5f;
    public float acceleration = 60f;
    public float deceleration = 80f;
    public float jumpVelocity = 14.5f;

    [Header("Sneak")]
    public float sneakSpeed = 3.2f;
    public KeyCode sneakKey = KeyCode.LeftShift;
    public KeyCode altSneakKey = KeyCode.LeftControl;

    [Header("Jump Physics")]
    public float fallMultiplier = 2.2f;
    public float lowJumpMultiplier = 2.5f;

    [Header("Slopes")]
    public float maxSlopeAngle = 60f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckDistance = 0.1f;
    public LayerMask whatIsGround;

    [Header("Jump Timers")]
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.2f;

    public bool canMove = true;
    public bool controlsInverted = false;

    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;
    private bool grounded;
    private float moveInput;
    private float coyoteCounter;
    private float jumpBufferCounter;

    private bool isSneaking;
    public bool IsSneaking => isSneaking;
    public bool IsMakingFootstepNoise => grounded && Mathf.Abs(rb != null ? rb.velocity.x : 0f) > 0.5f && !isSneaking;

    private Vector2 slopeNormal = Vector2.up;
    private float slopeAngle;
    private bool isOnSlope;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();

        if (rb != null)
        {
            rb.freezeRotation = true;
            if (rb.gravityScale > 4f)
            {
                rb.gravityScale = 3f;
            }
        }
    }

    void Update()
    {
        // check sneak input
        isSneaking = canMove && (Input.GetKey(sneakKey) || Input.GetKey(altSneakKey));

        // horizontal input
        moveInput = canMove ? Input.GetAxisRaw("Horizontal") : 0f;
        if (controlsInverted) moveInput = -moveInput;

        // jump buffer lets players press jump slightly before hitting the ground
        bool jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
        if (canMove && jumpPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // coyote time lets players jump briefly after stepping off an edge
        if (grounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        if (anim != null && rb != null)
        {
            float animSpeed = (isOnSlope && grounded) ? rb.velocity.magnitude : Mathf.Abs(rb.velocity.x);
            anim.SetFloat("moveSpeed", animSpeed);
            anim.SetFloat("jumpHeight", rb.velocity.y);
            anim.SetBool("grounded", grounded);
            anim.SetBool("isWalking", isSneaking && animSpeed > 0.1f);
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        CheckGrounded();

        if (canMove)
        {
            float currentMaxSpeed = isSneaking ? sneakSpeed : moveSpeed;
            float targetSpeed = moveInput * currentMaxSpeed;
            float rate = (moveInput != 0f) ? acceleration : deceleration;

            if (jumpBufferCounter > 0f && coyoteCounter > 0f)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);
                jumpBufferCounter = 0f;
                coyoteCounter = 0f;
                grounded = false;
                isOnSlope = false;
            }
            else if (grounded && isOnSlope)
            {
                if (moveInput != 0f)
                {
                    Vector2 slopeDir = new Vector2(slopeNormal.y, -slopeNormal.x).normalized;
                    float currentSpeed = Vector2.Dot(rb.velocity, slopeDir);
                    float newSpeed = Mathf.MoveTowards(currentSpeed, moveInput * currentMaxSpeed, rate * Time.fixedDeltaTime);
                    rb.velocity = slopeDir * newSpeed;
                }
                else
                {
                    // stop sliding on stairs when idle
                    rb.velocity = Vector2.zero;
                }
            }
            else
            {
                float newX = Mathf.MoveTowards(rb.velocity.x, targetSpeed, rate * Time.fixedDeltaTime);
                rb.velocity = new Vector2(newX, rb.velocity.y);

                // faster fall and variable jump height
                bool jumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.Space);
                if (rb.velocity.y < 0)
                {
                    rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
                }
                else if (rb.velocity.y > 0 && !jumpHeld)
                {
                    rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
                }
            }

            // flip sprite based on movement direction
            if (moveInput != 0f)
            {
                Vector3 s = transform.localScale;
                s.x = Mathf.Abs(s.x) * Mathf.Sign(moveInput);
                transform.localScale = s;
            }
        }
    }

    void CheckGrounded()
    {
        Vector2 checkOrigin = (col != null) ? (Vector2)col.bounds.center : (Vector2)transform.position;
        float rayDistance = (col != null) ? (col.bounds.extents.y + groundCheckDistance) : (1.025f + groundCheckDistance);

        RaycastHit2D hit = Physics2D.Raycast(checkOrigin, Vector2.down, rayDistance, whatIsGround);

        // probe sides if center ray misses near edges
        if (hit.collider == null || hit.collider.gameObject == gameObject || hit.collider.isTrigger)
        {
            float probeOffset = (col != null) ? (col.bounds.extents.x * 0.6f) : 0.25f;
            RaycastHit2D hitL = Physics2D.Raycast(checkOrigin + Vector2.left * probeOffset, Vector2.down, rayDistance, whatIsGround);
            RaycastHit2D hitR = Physics2D.Raycast(checkOrigin + Vector2.right * probeOffset, Vector2.down, rayDistance, whatIsGround);

            if (hitL.collider != null && hitL.collider.gameObject != gameObject && !hitL.collider.isTrigger)
                hit = hitL;
            else if (hitR.collider != null && hitR.collider.gameObject != gameObject && !hitR.collider.isTrigger)
                hit = hitR;
        }

        if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
        {
            slopeNormal = hit.normal;
            slopeAngle = Vector2.Angle(slopeNormal, Vector2.up);

            if (slopeAngle <= maxSlopeAngle)
            {
                grounded = true;
                isOnSlope = slopeAngle > 0.1f;
            }
            else
            {
                grounded = false;
                isOnSlope = false;
            }
        }
        else
        {
            grounded = false;
            isOnSlope = false;
            slopeNormal = Vector2.up;
            slopeAngle = 0f;
        }
    }
}
