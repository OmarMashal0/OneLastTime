using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DummyPlayer : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private LayerMask groundLayer = ~0;
    [SerializeField] private float slideSpeed = 10f;
    private const float SlideDuration = 10f / 18f;
    private const float AttackDuration = 12f / 18f;
    private enum Action { Free, Slide, Attack }
    private Action action;
    private float actionStart;
    private readonly HashSet<WellRabbit> struckThisAttack = new HashSet<WellRabbit>();
    private MamaRabbit struckBoss;
    private float facing = 1f;
    private string animationState;

    private Rigidbody2D body;
    private BoxCollider2D playerCollider;
    private Vector2 standingSize;
    private Vector2 standingOffset;
    private Animator animator;
    private float moveInput;
    private bool jumpRequested;
    private Vector2 initialSpawn;
    private SpriteRenderer playerSprite;
    private Color normalColor;
    private float flashEnds;
    private bool movementLocked;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
        standingSize = playerCollider.size;
        standingOffset = playerCollider.offset;
        animator = GetComponent<Animator>();
        playerSprite = GetComponent<SpriteRenderer>();
        normalColor = playerSprite.color;
        initialSpawn = body.position;
        Respawn();
    }

    public void Respawn()
    {
        action = Action.Free;
        RestoreHitbox();
        body.position = FlagCheckpoint.GetSpawn(gameObject.scene.path, initialSpawn);
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        jumpRequested = false;
    }

    private void Update()
    {
        if (movementLocked)
        {
            moveInput = 0f;
            jumpRequested = false;
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        float elapsed = Time.time - actionStart;
        if (action == Action.Slide && elapsed >= SlideDuration)
        {
            action = Action.Free;
            RestoreHitbox();
        }
        if (action == Action.Attack && elapsed >= AttackDuration) action = Action.Free;

        if (action == Action.Free && moveInput != 0f)
        {
            facing = Mathf.Sign(moveInput);
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * facing;
            transform.localScale = scale;
        }

        bool grounded = IsGrounded();
        if (action == Action.Free)
        {
            if (Input.GetMouseButtonDown(0))
            {
                action = Action.Attack;
                struckThisAttack.Clear();
                struckBoss = null;
                actionStart = Time.time;
                Play("Attack", true);
            }
            else if (grounded && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)))
            {
                action = Action.Slide;
                actionStart = Time.time;
                playerCollider.size = new Vector2(standingSize.x, standingSize.y * 0.5f);
                playerCollider.offset = standingOffset + Vector2.down * (standingSize.y * 0.25f);
                Play("Slide", true);
            }
            else
            {
                if (Input.GetButtonDown("Jump")) jumpRequested = true;
                Play(!grounded ? "Jump" : moveInput != 0f ? "Run" : "Idle");
            }
        }
        if (action == Action.Attack)
        {
            float frame = (Time.time - actionStart) * 18f;
            if ((frame >= 2f && frame < 6f) || (frame >= 8f && frame < 11f)) Strike();
        }
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
        if (!locked) return;

        moveInput = 0f;
        jumpRequested = false;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        action = Action.Free;
        RestoreHitbox();
        Play("Idle", true);
    }

    public void HitByCarrot()
    {
        playerSprite.color = Color.red;
        flashEnds = Time.time + 0.25f;
    }

    private void LateUpdate()
    {
        if (flashEnds <= 0f) return;
        float remaining = Mathf.Clamp01((flashEnds - Time.time) / 0.25f);
        playerSprite.color = Color.Lerp(normalColor, Color.red, remaining);
        if (remaining == 0f) flashEnds = 0f;
    }

    private void RestoreHitbox()
    {
        playerCollider.size = standingSize;
        playerCollider.offset = standingOffset;
    }

    private void Strike()
    {
        Bounds bounds = playerCollider.bounds;
        Vector2 center = (Vector2)bounds.center + Vector2.right * (facing * 1.1f);
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, new Vector2(2.3f, bounds.size.y), 0f))
        {
            if (hit.TryGetComponent(out WellRabbit rabbit) && struckThisAttack.Add(rabbit))
                rabbit.TakeHit();
            MamaRabbit boss = hit.GetComponentInParent<MamaRabbit>();
            if (boss != null && boss != struckBoss)
            {
                struckBoss = boss;
                boss.TakeHit();
            }
        }
    }

    private void Play(string state, bool restart = false)
    {
        if (animationState == state && !restart) return;
        animationState = state;
        animator.Play(state, 0, 0f);
    }

    private void FixedUpdate()
    {
        if (movementLocked)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            return;
        }

        if (body.position.y < initialSpawn.y - 10f)
        {
            Respawn();
            return;
        }

        float speed = action == Action.Slide ? facing * slideSpeed : action == Action.Attack ? 0f : moveInput * moveSpeed;
        body.velocity = new Vector2(speed, body.velocity.y);

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
            if (hit.collider != playerCollider && !hit.collider.isTrigger && hit.normal.y > 0.5f)
            {
                return true;
            }
        }

        return false;
    }
}
