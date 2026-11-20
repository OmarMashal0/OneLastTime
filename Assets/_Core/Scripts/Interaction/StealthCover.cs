using System.Collections;
using UnityEngine;
using OneLastTime.Player;

namespace OneLastTime.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class StealthCover : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [Tooltip("The prompt instance (InteractionPromptView) floating on this monument. If unassigned, automatically loaded from Interaction_Prompt.prefab.")]
        [SerializeField] private InteractionPromptView promptView;

        [Tooltip("Optional GameObject reference for backwards compatibility")]
        [SerializeField] private GameObject interactionPrompt;

        [Tooltip("Local height offset above the monument for the floating [E] prompt")]
        [SerializeField] private float promptHeightOffset = 0.5f;

        [Tooltip("Scale for the instantiated Interaction_Prompt (0.22 matches door and NPC prompts)")]
        [SerializeField] private float promptScale = 0.22f;

        [Tooltip("How much the player shrinks when hiding (0.95 = 5% smaller)")]
        [Range(0.8f, 1f)]
        [SerializeField] private float hiddenScaleMultiplier = 0.95f;

        [Tooltip("Shadow color tint when hidden (dark gray/slate stealth tint)")]
        [SerializeField] private Color shadowColor = new Color(0.35f, 0.4f, 0.5f, 0.85f);

        [Tooltip("Duration of the duck/un-duck transition in seconds")]
        [SerializeField] private float transitionDuration = 0.15f;

        // Public state for AI guards to check
        public bool IsPlayerHidden { get; private set; }

        private Collider2D triggerCollider;
        private SpriteRenderer coverSprite;
        private GameObject playerObj;
        private PlayerController playerController;
        private PlayerMovement playerMovement;
        private SpriteRenderer playerSprite;
        private Rigidbody2D playerRb;

        private Vector3 originalScale;
        private Color originalColor;
        private bool isPlayerInZone;
        private Coroutine transitionRoutine;
        private Vector3 promptBaseLocalPos;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null) triggerCollider.isTrigger = true;
            coverSprite = GetComponent<SpriteRenderer>();

            SetupInteractionPrompt();

            if (promptView != null)
            {
                promptView.Hide();
            }
            else if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }

        private void SetupInteractionPrompt()
        {
            if (promptView == null)
            {
                promptView = GetComponentInChildren<InteractionPromptView>(true);
            }

            if (promptView == null && interactionPrompt != null)
            {
                promptView = interactionPrompt.GetComponent<InteractionPromptView>();
            }

            if (promptView == null)
            {
                // Instantiate the official game interaction prompt prefab
                GameObject promptPrefab = null;
#if UNITY_EDITOR
                promptPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Core/Prefabs/UI/Interaction_Prompt.prefab");
#endif
                if (promptPrefab == null)
                {
                    promptPrefab = Resources.Load<GameObject>("Interaction_Prompt");
                }

                if (promptPrefab != null)
                {
                    var promptInstance = Instantiate(promptPrefab, transform, false);
                    promptInstance.name = "Interaction_Prompt";

                    float topY = (coverSprite != null) ? (coverSprite.bounds.size.y * 0.5f + promptHeightOffset) : 2.5f;
                    promptInstance.transform.localPosition = new Vector3(0f, topY, 0f);
                    promptInstance.transform.localScale = new Vector3(promptScale, promptScale, 1f);

                    var sr = promptInstance.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingLayerName = (coverSprite != null) ? coverSprite.sortingLayerName : "ground";
                        sr.sortingOrder = 50;
                    }

                    promptView = promptInstance.GetComponent<InteractionPromptView>();
                    interactionPrompt = promptInstance;
                }
            }

            if (promptView != null)
            {
                promptBaseLocalPos = promptView.transform.localPosition;
            }
            else if (interactionPrompt != null)
            {
                promptBaseLocalPos = interactionPrompt.transform.localPosition;
            }
        }

        private void Update()
        {
            // Bob the floating E prompt gently while visible
            if (promptView != null && promptView.IsVisible)
            {
                float bobOffset = Mathf.Sin(Time.time * 4f) * 0.08f;
                promptView.transform.localPosition = promptBaseLocalPos + new Vector3(0f, bobOffset, 0f);
            }

            if (!isPlayerInZone && !IsPlayerHidden) return;

            // 1. Press E to toggle hiding
            if (Input.GetKeyDown(KeyCode.E))
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
                playerController = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
                playerMovement = other.GetComponent<PlayerMovement>() ?? other.GetComponentInParent<PlayerMovement>();
                Animator playerAnimator = other.GetComponentInChildren<Animator>() ?? other.GetComponentInParent<Animator>();
                playerSprite = playerAnimator != null
                    ? playerAnimator.GetComponent<SpriteRenderer>()
                    : other.GetComponent<SpriteRenderer>();
                playerRb = other.GetComponent<Rigidbody2D>() ?? other.GetComponentInParent<Rigidbody2D>();

                if (!IsPlayerHidden)
                {
                    ShowPrompt();
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

                HidePrompt();
            }
        }

        private void EnterHiding()
        {
            if (playerObj == null || playerSprite == null) return;

            IsPlayerHidden = true;

            // Save original appearance (scale and color only; do NOT change sorting layers)
            originalScale = playerObj.transform.localScale;
            originalColor = playerSprite.color;

            // Hide the prompt while hidden
            HidePrompt();

            // Freeze player physics velocity and make kinematic so guards cannot push the player
            if (playerRb != null)
            {
                playerRb.velocity = Vector2.zero;
                playerRb.isKinematic = true;
            }

            // Ignore physical collision with all patrolling guards while hidden
            var guards = FindObjectsOfType<OneLastTime.AI.MuseumGuard>();
            var playerCols = playerObj.GetComponentsInChildren<Collider2D>();
            foreach (var g in guards)
            {
                if (g == null) continue;
                var gCol = g.GetComponent<Collider2D>();
                if (gCol != null)
                {
                    foreach (var pCol in playerCols)
                    {
                        if (pCol != null && !pCol.isTrigger) Physics2D.IgnoreCollision(pCol, gCol, true);
                    }
                }
            }

            // Temporarily disable running/jumping in PlayerController or PlayerMovement
            if (playerController != null)
            {
                playerController.enabled = false;
            }
            if (playerMovement != null)
            {
                playerMovement.canMove = false;
            }

            // Animate scale & stealth shadow color tint (player remains on their own sorting layer)
            Vector3 targetScale = originalScale * hiddenScaleMultiplier;
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(AnimateTransition(targetScale, shadowColor));
        }

        private void ExitHiding()
        {
            if (!IsPlayerHidden || playerObj == null || playerSprite == null) return;

            IsPlayerHidden = false;

            // Restore player physics & collision
            if (playerRb != null)
            {
                playerRb.isKinematic = false;
            }

            var guards = FindObjectsOfType<OneLastTime.AI.MuseumGuard>();
            var playerCols = playerObj.GetComponentsInChildren<Collider2D>();
            foreach (var g in guards)
            {
                if (g == null) continue;
                var gCol = g.GetComponent<Collider2D>();
                if (gCol != null)
                {
                    foreach (var pCol in playerCols)
                    {
                        if (pCol != null && !pCol.isTrigger) Physics2D.IgnoreCollision(pCol, gCol, false);
                    }
                }
            }

            // Restore player controller movement
            if (playerController != null)
            {
                playerController.enabled = true;
            }
            if (playerMovement != null)
            {
                playerMovement.canMove = true;
            }

            // Show prompt again if still in zone
            if (isPlayerInZone)
            {
                ShowPrompt();
            }

            // Animate scale & color back to normal, maintaining current facing direction
            float currentSign = Mathf.Sign(playerObj.transform.localScale.x);
            Vector3 restoreScale = originalScale;
            restoreScale.x = Mathf.Abs(originalScale.x) * (currentSign == 0 ? 1 : currentSign);
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(AnimateTransition(restoreScale, originalColor));
        }

        private void ShowPrompt()
        {
            if (promptView != null)
            {
                promptView.Show();
            }
            else if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
        }

        private void HidePrompt()
        {
            if (promptView != null)
            {
                promptView.Hide();
            }
            else if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
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
            return other.CompareTag("Player") || other.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
