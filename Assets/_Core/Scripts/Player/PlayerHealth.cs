using System.Collections;
using UnityEngine;
using OneLastTime.Systems;

namespace OneLastTime.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Death Settings")]
        public float holdDeadPoseDuration = 0.8f;

        private Rigidbody2D rb;
        private Animator anim;
        private PlayerMovement playerMovement;
        private bool isDead;
        private bool deathAnimComplete;

        public bool IsDead => isDead;

        void Awake()
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

        // animation event called at the end of the death clip
        public void OnDeathAnimationComplete()
        {
            deathAnimComplete = true;
        }

        IEnumerator DeathRoutine()
        {
            deathAnimComplete = false;

            // stop movement and physics
            if (playerMovement != null)
            {
                playerMovement.canMove = false;
            }
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            // play death animation
            if (anim != null)
            {
                anim.SetTrigger("die");
            }

            // wait for animation to complete
            float timer = 2.0f;
            while (!deathAnimComplete && timer > 0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(holdDeadPoseDuration);

            // respawn at last checkpoint
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
