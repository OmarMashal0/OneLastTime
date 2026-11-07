using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
     [Header("Movement")]
        public float moveSpeed = 6f;
        public float acceleration = 60f;    // speed gained per second while a direction is held
        public float deceleration = 80f;    // speed lost per second when released
        public float jumpVelocity = 12f;

        [Header("Ground check")]
        public Transform groundCheck;       // empty child object at the player's feet
        public float groundCheckRadius = 0.15f;
        public LayerMask whatIsGround;

        [Header("State flags")]
        public bool canMove = true;           // dash, parry recovery, stagger turn this off
        public bool controlsInverted = false; // hallucination mirrored controls

        private Rigidbody2D rb;
        private Animator anim;
        private bool grounded;
        private float moveInput;
        private bool jumpPressed;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>(); // can be missing, it's null-checked below
        }

        void Update()
        {
            // Read input every frame so no key press is missed
            moveInput = canMove ? Input.GetAxisRaw("Horizontal") : 0f;
            if (controlsInverted) moveInput = -moveInput;

            if (canMove && Input.GetButtonDown("Jump"))
                jumpPressed = true;

            // Animator hooks (only if an Animator with these parameters exists)
            if (anim != null)
            {
                anim.SetFloat("moveSpeed", Mathf.Abs(rb.velocity.x));
                anim.SetFloat("jumpHeight", rb.velocity.y);
                anim.SetBool("grounded", grounded);
            }
        }

        void FixedUpdate()
        {
            grounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

            // When canMove is false, this script doesn't touch velocity,
            // so dash/stagger scripts can control it.
            if (canMove)
            {
                // Accelerate toward the target speed, or decelerate when no input
                float targetSpeed = moveInput * moveSpeed;
                float rate = (moveInput != 0f) ? acceleration : deceleration;
                float newX = Mathf.MoveTowards(rb.velocity.x, targetSpeed, rate * Time.fixedDeltaTime);
                rb.velocity = new Vector2(newX, rb.velocity.y);

                // Jump
                if (jumpPressed && grounded)
                    rb.velocity = new Vector2(rb.velocity.x, jumpVelocity);

                // Face the direction of movement (flips children like hitboxes too)
                if (moveInput != 0f)
                {
                    Vector3 s = transform.localScale;
                    s.x = Mathf.Abs(s.x) * Mathf.Sign(moveInput);
                    transform.localScale = s;
                }
            }

            jumpPressed = false;
        }
}
