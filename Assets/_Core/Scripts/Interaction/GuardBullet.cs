using UnityEngine;
using OneLastTime.Player;

namespace OneLastTime.AI
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class GuardBullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float speed = 38f;
        [SerializeField] private float lifetime = 3.5f;

        private Rigidbody2D rb;
        private Collider2D bulletCol;
        private Vector2 travelDirection;
        private bool hasHit;
        private Collider2D firingGuardCollider;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            bulletCol = GetComponent<CircleCollider2D>();

            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void Initialize(Vector2 direction, Collider2D guardCollider)
        {
            travelDirection = direction.normalized;
            firingGuardCollider = guardCollider;

            if (firingGuardCollider != null && bulletCol != null)
            {
                Physics2D.IgnoreCollision(bulletCol, firingGuardCollider, true);
            }

            // Align rotation with travel direction
            float angle = Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            rb.velocity = travelDirection * speed;
            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleCollision(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision.collider);
        }

        private void HandleCollision(Collider2D other)
        {
            if (hasHit) return;
            if (other == firingGuardCollider) return;

            // Check if stealth cover
            var cover = other.GetComponent<OneLastTime.Interaction.StealthCover>() ?? other.GetComponentInParent<OneLastTime.Interaction.StealthCover>();
            if (cover != null)
            {
                if (cover.IsPlayerHidden)
                {
                    // Player is hidden behind cover: bullet safely absorbed
                    hasHit = true;
                    DestroyBullet();
                    return;
                }
                // Player is NOT hidden: bullet passes through the cover trigger zone to reach target
                return;
            }

            // Check if player hit
            var health = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            if (health == null && (other.CompareTag("Player") || other.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                health = other.GetComponentInChildren<PlayerHealth>();
            }

            if (health != null)
            {
                hasHit = true;
                health.TakeFatalHit(transform.position);
                DestroyBullet();
                return;
            }

            if (other.isTrigger)
            {
                // Ignore non-player triggers (interaction zones, prompt triggers, etc.)
                return;
            }

            // Hit solid wall or obstacle
            hasHit = true;
            DestroyBullet();
        }

        private void DestroyBullet()
        {
            rb.velocity = Vector2.zero;
            // Detach trail so it fades out smoothly
            var trail = GetComponentInChildren<TrailRenderer>();
            if (trail != null)
            {
                trail.transform.SetParent(null);
                trail.autodestruct = true;
            }
            Destroy(gameObject);
        }
    }
}
