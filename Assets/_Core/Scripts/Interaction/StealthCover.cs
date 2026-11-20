using System.Collections;
using UnityEngine;
using OneLastTime.Player;

namespace OneLastTime.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class StealthCover : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [Tooltip("The prompt that shows up when near the monument (e.g. '[F] Hide')")]
        [SerializeField] private GameObject interactionPrompt;

        [Tooltip("How much the player shrinks when hiding (0.95 = 5% smaller)")]
        [Range(0.8f, 1f)]
        [SerializeField] private float hiddenScaleMultiplier = 0.95f;

        [Tooltip("Shadow color tint when hidden (dark gray/slate)")]
        [SerializeField] private Color shadowColor = new Color(0.35f, 0.4f, 0.5f, 0.8f);

        [Tooltip("Sorting order to place player behind monument while hidden")]
        [SerializeField] private int hiddenSortingOrder = -1;

        [Tooltip("Duration of the duck/un-duck transition in seconds")]
        [SerializeField] private float transitionDuration = 0.15f;

        // Public state for AI guards to check
        public bool IsPlayerHidden { get; private set; }

        private Collider2D triggerCollider;
        private GameObject playerObj;
        private PlayerController playerController;
        private SpriteRenderer playerSprite;
        private Rigidbody2D playerRb;

        private Vector3 originalScale;
        private Color originalColor;
        private int originalSortingOrder;
        private string originalSortingLayer;
        private bool isPlayerInZone;
        private Coroutine transitionRoutine;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider2D>();
            triggerCollider.isTrigger = true;

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }

        private void Update()
        {
            if (!isPlayerInZone && !IsPlayerHidden) return;

            // 1. Press F to toggle hiding
            if (Input.GetKeyDown(KeyCode.F))
            {
                if (!IsPlayerHidden)
                {
                    EnterHiding();
                }
                else
                {
                    ExitHiding();
                }
            }
            // 2. Pressing movement keys (A/D or Space) also breaks cover smoothly
            else if (IsPlayerHidden)
            {
                if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Input.GetKeyDown(KeyCode.Space))
                {
                    ExitHiding();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                isPlayerInZone = true;
                playerObj = other.gameObject;
                playerController = other.GetComponent<PlayerController>();
                playerSprite = other.GetComponent<SpriteRenderer>();
                playerRb = other.GetComponent<Rigidbody2D>();

                if (interactionPrompt != null && !IsPlayerHidden)
                {
                    interactionPrompt.SetActive(true);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                isPlayerInZone = false;

                if (IsPlayerHidden)
                {
                    ExitHiding();
                }

                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(false);
                }
            }
        }

        private void EnterHiding()
        {
            if (playerObj == null || playerSprite == null) return;

            IsPlayerHidden = true;

            // Save original appearance
            originalScale = playerObj.transform.localScale;
            originalColor = playerSprite.color;
            originalSortingOrder = playerSprite.sortingOrder;
            originalSortingLayer = playerSprite.sortingLayerName;

            // Hide the prompt while hidden
            if (interactionPrompt != null) interactionPrompt.SetActive(false);

            // Freeze player physics velocity
            if (playerRb != null)
            {
                playerRb.velocity = Vector2.zero;
            }

            // Temporarily disable running/jumping in PlayerController
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // Place player behind monument
            playerSprite.sortingOrder = hiddenSortingOrder;

            // Animate scale & color
            Vector3 targetScale = originalScale * hiddenScaleMultiplier;
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(AnimateTransition(targetScale, shadowColor));
        }

        private void ExitHiding()
        {
            if (!IsPlayerHidden || playerObj == null || playerSprite == null) return;

            IsPlayerHidden = false;

            // Restore player controller movement
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // Restore sorting layer & order
            playerSprite.sortingLayerName = originalSortingLayer;
            playerSprite.sortingOrder = originalSortingOrder;

            // Show prompt again if still in zone
            if (interactionPrompt != null && isPlayerInZone)
            {
                interactionPrompt.SetActive(true);
            }

            // Animate scale & color back to normal
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(AnimateTransition(originalScale, originalColor));
        }

        private IEnumerator AnimateTransition(Vector3 targetScale, Color targetColor)
        {
            Vector3 startScale = playerObj.transform.localScale;
            Color startColor = playerSprite.color;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);

                playerObj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                playerSprite.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            playerObj.transform.localScale = targetScale;
            playerSprite.color = targetColor;
        }

        private bool IsPlayer(Collider2D other)
        {
            return other.CompareTag("Player") || other.name.Contains("Player");
        }
    }
}

