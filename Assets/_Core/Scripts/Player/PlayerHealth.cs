using System.Collections;
using UnityEngine;
using OneLastTime.Systems;

namespace OneLastTime.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Death Settings")]
        [Tooltip("Seconds to hold final dead pose before starting respawn transition")]
        [SerializeField] private float holdDeadPoseDuration = 0.8f;

        private Rigidbody2D rb;
        private Animator anim;
        private PlayerMovement playerMovement;
        private bool isDead;
        private bool deathAnimComplete;

        public bool IsDead => isDead;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            playerMovement = GetComponent<PlayerMovement>();
        }

        public void TakeFatalHit(Vector3 hitPoint)
        {
            if (isDead) return;
            isDead = true;

            StartCoroutine(DeathRoutine());
        }

        // Animation event called at the end of ryu_death.anim
        public void OnDeathAnimationComplete()
        {
            deathAnimComplete = true;
        }

        private IEnumerator DeathRoutine()
        {
            deathAnimComplete = false;

            // 1. Freeze movement & physics
            if (playerMovement != null)
            {
                playerMovement.canMove = false;
            }
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            // 2. Play death animation
            if (anim != null)
            {
                anim.SetTrigger("die");
            }

            // Wait for animation-completed event (with safety timeout)
            float timer = 2.0f;
            while (!deathAnimComplete && timer > 0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            // Hold final dead pose briefly before respawn
            yield return new WaitForSeconds(holdDeadPoseDuration);

            // 3. Trigger checkpoint respawn
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.RespawnPlayer();
            }
        }

        public void RestorePlayer()
        {
            isDead = false;

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            if (anim != null)
            {
                anim.ResetTrigger("die");
                anim.SetTrigger("respawn");
            }

            if (playerMovement != null)
            {
                playerMovement.canMove = true;
            }
        }
    }
}
