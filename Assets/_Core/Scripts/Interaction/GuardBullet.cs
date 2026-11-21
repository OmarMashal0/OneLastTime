using UnityEngine;
using OneLastTime.Player;
using OneLastTime.Interaction;

namespace OneLastTime.Enemies
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class GuardBullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        public float speed = 38f;
        public float lifetime = 3.5f;

        private Rigidbody2D rb;
        private Collider2D bulletCol;
        private Vector2 travelDirection;
        private bool hasHit;
        private Collider2D firingGuardCollider;

        void Awake()
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

            // rotate bullet to match flight direction
            float angle = Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            rb.velocity = travelDirection * speed;
            Destroy(gameObject, lifetime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            HandleCollision(other);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            HandleCollision(collision.collider);
        }

        void HandleCollision(Collider2D other)
        {
            if (hasHit) return;
            if (other == firingGuardCollider) return;

            // check if player is safe behind cover
            var cover = other.GetComponent<StealthCover>() ?? other.GetComponentInParent<StealthCover>();
            if (cover != null)
            {
                if (cover.IsPlayerHidden)
                {
                    hasHit = true;
                    DestroyBullet();
                    return;
                }
                return;
            }

            // hit player
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

            // ignore other trigger zones
            if (other.isTrigger) return;

            // hit wall or floor
            hasHit = true;
            DestroyBullet();
        }

        void DestroyBullet()
        {
            rb.velocity = Vector2.zero;
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
