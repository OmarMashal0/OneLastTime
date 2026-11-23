using System.Collections;
using UnityEngine;
using OneLastTime.Player;
using OneLastTime.Systems;
using OneLastTime.UI;

namespace OneLastTime.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class PuzzleDrawer : MonoBehaviour
    {
        [Header("Chest Animation Frames")]
        [SerializeField] private Sprite[] animationFrames;
        [SerializeField] private float frameDuration = 0.12f;

        [Header("State")]
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool keyCollected = false;

        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private InteractionPromptView promptView;
        [SerializeField] private float interactDistance = 2.8f;

        [Header("Dialogue Content")]
        [TextArea(2, 3)]
        [SerializeField] private string lockedDialogue = "The chest is locked tightly. It seems linked to a mechanism above...";
        [TextArea(2, 3)]
        [SerializeField] private string openDialogue = "The chest is open. I already took the key.";
        [SerializeField] private float dialogueDuration = 2.8f;

        private SpriteRenderer spriteRenderer;
        private bool playerInRange;
        private bool isInteracting;
        private Transform playerTransform;
        private PlayerMovement playerMovement;

        public bool IsOpen => isOpen;
        public bool KeyCollected => keyCollected;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            spriteRenderer = GetComponent<SpriteRenderer>();

            if (promptView == null)
            {
                promptView = GetComponentInChildren<InteractionPromptView>(true);
            }

            if (interactionPrompt == null && promptView != null)
            {
                interactionPrompt = promptView.gameObject;
            }

            HidePrompt(true);
        }

        void Start()
        {
            FindPlayer();

            if (spriteRenderer != null && animationFrames != null && animationFrames.Length > 0)
            {
                if (isOpen && keyCollected)
                {
                    spriteRenderer.sprite = animationFrames.Length > 7 ? animationFrames[7] : animationFrames[animationFrames.Length - 1];
                }
                else
                {
                    spriteRenderer.sprite = animationFrames[0];
                }
            }
        }

        void Update()
        {
            if (isInteracting) return;

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float dist = Vector2.Distance(transform.position, playerTransform.position);
            bool inRange = dist <= interactDistance;

            if (inRange && !playerInRange)
            {
                playerInRange = true;
                ShowPrompt();
            }
            else if (!inRange && playerInRange)
            {
                playerInRange = false;
                HidePrompt(false);
            }

            if (playerInRange && Input.GetKeyDown(interactKey))
            {
                StartCoroutine(InspectSequence());
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (isInteracting) return;
            if (IsPlayer(other))
            {
                playerInRange = true;
                ShowPrompt();
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (isInteracting) return;
            if (IsPlayer(other))
            {
                playerInRange = false;
                HidePrompt(false);
            }
        }

        IEnumerator InspectSequence()
        {
            isInteracting = true;
            HidePrompt(true);

            string dialogue = (isOpen && keyCollected) ? openDialogue : lockedDialogue;

            var overlay = ConfrontationOverlay.Instance;
            if (overlay != null)
            {
                overlay.ShowDirectSubtitle(dialogue);
            }

            yield return WaitForDismissOrDuration(dialogueDuration);

            if (overlay != null)
            {
                yield return overlay.FadeOutSubtitle(0.3f);
            }

            isInteracting = false;
        }

        public IEnumerator OpenDrawerSequence(ConfrontationOverlay overlay, CameraFollow camFollow)
        {
            yield return StartCoroutine(OpenDrawerSequence(camFollow));
        }

        public IEnumerator OpenDrawerSequence(CameraFollow camFollow)
        {
            // wait a brief moment after the painting is complete
            yield return new WaitForSeconds(0.4f);

            // small shake to signal the mechanism unlocking
            Vector3 originalPos = transform.position;
            float shakeElapsed = 0f;
            float shakeDuration = 0.35f;
            while (shakeElapsed < shakeDuration)
            {
                shakeElapsed += Time.deltaTime;
                float offsetX = Random.Range(-0.04f, 0.04f);
                float offsetY = Random.Range(-0.02f, 0.02f);
                transform.position = originalPos + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }
            transform.position = originalPos;

            isOpen = true;

            // run opening animation from frame 0 up to frame 6 with no text shown
            if (animationFrames != null && animationFrames.Length >= 7)
            {
                for (int i = 0; i <= 6; i++)
                {
                    if (animationFrames[i] != null && spriteRenderer != null)
                    {
                        spriteRenderer.sprite = animationFrames[i];
                    }
                    yield return new WaitForSeconds(frameDuration);
                }
            }

            // frame 6 with floating key lasts a bit for the player to see
            yield return new WaitForSeconds(2.0f);
        }

        public void SetOpenEmptySprite()
        {
            // after the focus and camera return to its usual, the one that is kept there is frame 7
            if (animationFrames != null && animationFrames.Length > 7 && animationFrames[7] != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = animationFrames[7];
            }

            // when the last sheet is active, award the key and change key hud
            keyCollected = true;
            var keyHUD = KeyCollectionHUD.Instance != null ? KeyCollectionHUD.Instance : FindObjectOfType<KeyCollectionHUD>(true);
            if (keyHUD != null)
            {
                keyHUD.AddKey();
            }

            // disable interaction on both puzzle source paintings now that player has the key
            var pieceSources = FindObjectsOfType<PaintingPieceSource>();
            if (pieceSources != null)
            {
                foreach (var src in pieceSources)
                {
                    src.DisableInteractionPermanently();
                }
            }
        }

        IEnumerator WaitForDismissOrDuration(float maxDuration)
        {
            yield return new WaitForSeconds(0.4f);
            float elapsed = 0f;
            while (elapsed < maxDuration)
            {
                elapsed += Time.deltaTime;
                if (elapsed > 0.4f && (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Space)))
                {
                    break;
                }
                yield return null;
            }
        }

        void ShowPrompt()
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

        void HidePrompt(bool immediate)
        {
            if (promptView != null)
            {
                promptView.Hide(immediate);
            }
            else if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }

        void FindPlayer()
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement != null)
            {
                playerTransform = playerMovement.transform;
            }
            else
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                    playerMovement = player.GetComponent<PlayerMovement>();
                }
            }
        }

        bool IsPlayer(Collider2D other)
        {
            if (other.CompareTag("Player")) return true;
            if (playerTransform != null && (other.transform == playerTransform || other.transform.IsChildOf(playerTransform))) return true;
            return false;
        }
    }
}
