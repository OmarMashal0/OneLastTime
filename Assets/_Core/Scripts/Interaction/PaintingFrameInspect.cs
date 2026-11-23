using System.Collections;
using UnityEngine;
using OneLastTime.Player;
using OneLastTime.Systems;
using OneLastTime.UI;

namespace OneLastTime.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class PaintingFrameInspect : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private InteractionPromptView promptView;
        [SerializeField] private float interactDistance = 2.8f;

        [Header("Dialogue Content")]
        [TextArea(2, 3)]
        [SerializeField] private string inspectDialogue = "This painting looks familiar... Two pieces are missing. \nMaybe there's something hidden behind it.";
        [TextArea(2, 3)]
        [SerializeField] private string wrongPieceDialogue = "This piece doesn't fit here... It belongs to a different painting.";
        [TextArea(2, 3)]
        [SerializeField] private string correctPieceDialogue = "It fits perfectly. One piece in place.";
        [TextArea(2, 3)]
        [SerializeField] private string clanDiscoveryDialogue = "Wait... this crest... it's my clan's emblem!";
        [TextArea(2, 3)]
        [SerializeField] private string pocketPieceDialogue = "The second piece... I've had it in my pocket this whole time.";
        [TextArea(2, 3)]
        [SerializeField] private string completedDialogue = "My clan's crest is restored.";
        [SerializeField] private float dialogueDuration = 3.2f;

        [Header("Camera Settings")]
        [SerializeField] private float zoomedOrthoSize = 3.5f;
        [SerializeField] private float normalOrthoSize = 5.0f;
        [SerializeField] private float zoomDuration = 0.4f;
        [SerializeField] private Vector3 cameraFocusOffset = new Vector3(0f, -1.25f, 0f);

        [Header("Left Piece Assembly")]
        [SerializeField] private Sprite[] leftPieceFrames;
        [SerializeField] private float frameDuration = 0.2f;

        [Header("Right Piece Assembly")]
        [SerializeField] private Sprite[] rightPieceFrames;

        [Header("Secret Drawer")]
        [SerializeField] private PuzzleDrawer secretDrawer;

        [Header("State")]
        [SerializeField] private bool leftPiecePlaced = false;
        [SerializeField] private bool bothPiecesPlaced = false;

        private SpriteRenderer frameRenderer;
        private Vector3 initialLocalScale;
        private float initialSpriteWidth = 1086f;
        private Vector3 initialPromptLocalScale;
        private Vector3 initialPromptLocalPosition;
        private BoxCollider2D frameCollider;
        private Vector2 initialColliderSize;
        private bool playerInRange;
        private bool isInspecting;
        private Transform playerTransform;
        private PlayerMovement playerMovement;

        public bool LeftPiecePlaced => leftPiecePlaced;
        public bool BothPiecesPlaced => bothPiecesPlaced;

        void Awake()
        {
            frameCollider = GetComponent<BoxCollider2D>();
            if (frameCollider != null)
            {
                frameCollider.isTrigger = true;
                initialColliderSize = frameCollider.size;
            }

            frameRenderer = GetComponent<SpriteRenderer>();
            initialLocalScale = transform.localScale;

            if (frameRenderer != null && frameRenderer.sprite != null)
            {
                initialSpriteWidth = frameRenderer.sprite.rect.width;
            }

            if (promptView == null)
            {
                promptView = GetComponentInChildren<InteractionPromptView>(true);
            }

            if (interactionPrompt == null && promptView != null)
            {
                interactionPrompt = promptView.gameObject;
            }

            if (interactionPrompt != null)
            {
                initialPromptLocalScale = interactionPrompt.transform.localScale;
                initialPromptLocalPosition = interactionPrompt.transform.localPosition;
            }

            HidePrompt(true);
        }

        void Start()
        {
            FindPlayer();

            if (bothPiecesPlaced && rightPieceFrames != null && rightPieceFrames.Length > 0)
            {
                SetFrameSprite(rightPieceFrames[rightPieceFrames.Length - 1]);
            }
            else if (leftPiecePlaced && leftPieceFrames != null && leftPieceFrames.Length > 0)
            {
                SetFrameSprite(leftPieceFrames[leftPieceFrames.Length - 1]);
            }
        }

        void Update()
        {
            if (isInspecting) return;

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
            if (isInspecting) return;
            if (IsPlayer(other))
            {
                playerInRange = true;
                ShowPrompt();
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (isInspecting) return;
            if (IsPlayer(other))
            {
                playerInRange = false;
                HidePrompt(false);
            }
        }

        IEnumerator InspectSequence()
        {
            isInspecting = true;
            HidePrompt(true);

            if (bothPiecesPlaced)
            {
                yield return StartCoroutine(RunDialogueRoutine(completedDialogue));
                isInspecting = false;
                yield break;
            }

            PuzzlePieceType heldPiece = PuzzleInventoryHUD.HeldPiece;

            if (leftPiecePlaced)
            {
                yield return StartCoroutine(RunDialogueRoutine("The left piece is in place. One more piece is still missing."));
                isInspecting = false;
                yield break;
            }

            if (heldPiece == PuzzlePieceType.CherryBlossom)
            {
                // place the correct piece and proceed into the dramatic revelation
                yield return StartCoroutine(PlaceCorrectPieceSequence());
            }
            else if (heldPiece == PuzzlePieceType.JapaneseWave)
            {
                // wrong piece
                yield return StartCoroutine(RunDialogueRoutine(wrongPieceDialogue));
            }
            else
            {
                // discover hud on first inspect
                var hud = PuzzleInventoryHUD.Instance;
                if (hud != null)
                {
                    hud.Discover();
                }

                // no piece held, inspect empty frame
                yield return StartCoroutine(RunDialogueRoutine(inspectDialogue));
            }

            isInspecting = false;
        }

        IEnumerator PlaceCorrectPieceSequence()
        {
            FreezePlayer(true);

            // stage player slightly to the left so painting is clearly visible
            yield return StartCoroutine(StagePlayerBesideFrame(0.2f));

            var cam = Camera.main;
            CameraFollow camFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (camFollow != null)
            {
                camFollow.FocusOnTarget(transform, cameraFocusOffset);
                StartCoroutine(camFollow.ZoomTo(zoomedOrthoSize, zoomDuration));
            }

            var overlay = ConfrontationOverlay.Instance;
            if (overlay != null)
            {
                StartCoroutine(overlay.FadeVignette(0.6f, zoomDuration));
            }

            yield return new WaitForSeconds(zoomDuration * 0.5f);

            // play left piece frames animation while matching scale
            if (leftPieceFrames != null && leftPieceFrames.Length > 0)
            {
                for (int i = 0; i < leftPieceFrames.Length; i++)
                {
                    if (leftPieceFrames[i] != null)
                    {
                        SetFrameSprite(leftPieceFrames[i]);
                    }
                    yield return new WaitForSeconds(frameDuration);
                }
            }

            leftPiecePlaced = true;

            // hide inventory hud completely after placing the piece
            var hud = PuzzleInventoryHUD.Instance;
            if (hud != null)
            {
                hud.HideHUD();
            }

            if (overlay != null)
            {
                overlay.ShowDirectSubtitle(correctPieceDialogue);
            }

            yield return WaitForDismissOrDuration(dialogueDuration);

            if (overlay != null)
            {
                yield return overlay.FadeOutSubtitle(0.3f);
                StartCoroutine(overlay.FadeVignette(0f, zoomDuration));
            }

            if (camFollow != null)
            {
                camFollow.ClearFocus();
                yield return camFollow.ZoomTo(normalOrthoSize, zoomDuration);
            }

            // unfreeze player briefly so player can move again
            FreezePlayer(false);

            // brief window where player can move
            yield return new WaitForSeconds(0.6f);

            // freeze again and run second piece revelation
            yield return StartCoroutine(PlaceSecondPocketPieceSequence());
        }

        IEnumerator PlaceSecondPocketPieceSequence()
        {
            FreezePlayer(true);

            // stage player beside painting so it is clearly visible
            yield return StartCoroutine(StagePlayerBesideFrame(0.2f));

            var cam = Camera.main;
            CameraFollow camFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (camFollow != null)
            {
                camFollow.FocusOnTarget(transform, cameraFocusOffset);
                StartCoroutine(camFollow.ZoomTo(zoomedOrthoSize, zoomDuration));
            }

            var overlay = ConfrontationOverlay.Instance;
            if (overlay != null)
            {
                StartCoroutine(overlay.FadeVignette(0.6f, zoomDuration));
            }

            yield return new WaitForSeconds(zoomDuration * 0.5f);

            // first realization dialogue
            if (overlay != null)
            {
                overlay.ShowDirectSubtitle(clanDiscoveryDialogue);
            }
            yield return WaitForDismissOrDuration(dialogueDuration);

            // second dialogue: taking the piece from pocket
            if (overlay != null)
            {
                overlay.ShowDirectSubtitle(pocketPieceDialogue);
            }
            yield return WaitForDismissOrDuration(dialogueDuration * 0.85f);

            // fade out subtitle before placing piece
            if (overlay != null)
            {
                yield return overlay.FadeOutSubtitle(0.2f);
            }

            yield return new WaitForSeconds(0.2f);

            // play right piece frames animation (second piece putting in place)
            if (rightPieceFrames != null && rightPieceFrames.Length > 0)
            {
                for (int i = 0; i < rightPieceFrames.Length; i++)
                {
                    if (rightPieceFrames[i] != null)
                    {
                        SetFrameSprite(rightPieceFrames[i]);
                    }
                    yield return new WaitForSeconds(frameDuration);
                }
            }

            bothPiecesPlaced = true;

            // show completed dialogue
            if (overlay != null)
            {
                overlay.ShowDirectSubtitle(completedDialogue);
            }
            yield return WaitForDismissOrDuration(dialogueDuration);

            // fade out subtitle and remove gray vignette before drawer opening animation
            if (overlay != null)
            {
                StartCoroutine(overlay.FadeVignette(0f, 0.3f));
                yield return overlay.FadeOutSubtitle(0.3f);
            }

            // trigger secret drawer opening (no text and no gray vignette during opening animation)
            if (secretDrawer == null)
            {
                secretDrawer = FindObjectOfType<PuzzleDrawer>();
            }

            if (secretDrawer != null)
            {
                yield return StartCoroutine(secretDrawer.OpenDrawerSequence(camFollow));
            }

            if (camFollow != null)
            {
                camFollow.ClearFocus();
                yield return camFollow.ZoomTo(normalOrthoSize, zoomDuration);
            }

            // after the focus and camera return to usual, the one kept is frame 7
            if (secretDrawer != null)
            {
                secretDrawer.SetOpenEmptySprite();
            }

            FreezePlayer(false);
        }

        IEnumerator RunDialogueRoutine(string text)
        {
            FreezePlayer(true);

            yield return StartCoroutine(StagePlayerBesideFrame(0.2f));

            var cam = Camera.main;
            CameraFollow camFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (camFollow != null)
            {
                camFollow.FocusOnTarget(transform, cameraFocusOffset);
                StartCoroutine(camFollow.ZoomTo(zoomedOrthoSize, zoomDuration));
            }

            var overlay = ConfrontationOverlay.Instance;
            if (overlay != null)
            {
                StartCoroutine(overlay.FadeVignette(0.6f, zoomDuration));
                overlay.ShowDirectSubtitle(text);
            }

            yield return WaitForDismissOrDuration(dialogueDuration);

            if (overlay != null)
            {
                yield return overlay.FadeOutSubtitle(0.3f);
                StartCoroutine(overlay.FadeVignette(0f, zoomDuration));
            }

            if (camFollow != null)
            {
                camFollow.ClearFocus();
                yield return camFollow.ZoomTo(normalOrthoSize, zoomDuration);
            }

            FreezePlayer(false);
        }

        void SetFrameSprite(Sprite s)
        {
            if (frameRenderer == null || s == null) return;
            frameRenderer.sprite = s;

            // maintain exact world dimensions
            if (s.rect.width > 0 && initialSpriteWidth > 0)
            {
                float scaleMultiplier = initialSpriteWidth / s.rect.width;
                transform.localScale = new Vector3(
                    initialLocalScale.x * scaleMultiplier,
                    initialLocalScale.y * scaleMultiplier,
                    initialLocalScale.z
                );

                // keep interaction prompt at original world size and position
                if (interactionPrompt != null)
                {
                    interactionPrompt.transform.localScale = new Vector3(
                        initialPromptLocalScale.x / scaleMultiplier,
                        initialPromptLocalScale.y / scaleMultiplier,
                        initialPromptLocalScale.z
                    );
                    interactionPrompt.transform.localPosition = new Vector3(
                        initialPromptLocalPosition.x,
                        initialPromptLocalPosition.y / scaleMultiplier,
                        initialPromptLocalPosition.z
                    );
                }

                // maintain exact world collider bounds
                if (frameCollider != null && initialColliderSize != Vector2.zero)
                {
                    frameCollider.size = initialColliderSize / scaleMultiplier;
                }
            }
        }

        IEnumerator StagePlayerBesideFrame(float duration)
        {
            if (playerTransform == null) yield break;

            float targetX = transform.position.x - 1.6f;
            float startX = playerTransform.position.x;
            float y = playerTransform.position.y;
            float z = playerTransform.position.z;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float curX = Mathf.Lerp(startX, targetX, elapsed / duration);
                playerTransform.position = new Vector3(curX, y, z);
                yield return null;
            }
            playerTransform.position = new Vector3(targetX, y, z);

            // face right towards the frame
            Vector3 ls = playerTransform.localScale;
            ls.x = Mathf.Abs(ls.x);
            playerTransform.localScale = ls;
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

        void FreezePlayer(bool freeze)
        {
            if (playerMovement == null) FindPlayer();

            if (playerMovement != null)
            {
                playerMovement.canMove = !freeze;
            }

            if (freeze && playerTransform != null)
            {
                var rb = playerTransform.GetComponent<Rigidbody2D>() ?? playerTransform.GetComponentInParent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                }

                var anim = playerTransform.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.SetFloat("moveSpeed", 0f);
                    anim.SetBool("isWalking", false);
                }
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
