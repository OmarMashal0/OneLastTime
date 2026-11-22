using System.Collections;
using UnityEngine;
using OneLastTime.Player;
using OneLastTime.Systems;
using OneLastTime.UI;

namespace OneLastTime.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class PaintingPieceSource : MonoBehaviour
    {
        [Header("Piece Configuration")]
        [SerializeField] private PuzzlePieceType pieceType = PuzzlePieceType.JapaneseWave;
        [SerializeField] private string pieceName = "Japanese Wave";

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] animationFrames;
        [SerializeField] private float frameDuration = 0.25f;

        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private InteractionPromptView promptView;
        [SerializeField] private float interactDistance = 2.8f;

        [Header("Camera Settings")]
        [SerializeField] private float zoomedOrthoSize = 2.8f;
        [SerializeField] private float normalOrthoSize = 5.0f;
        [SerializeField] private float zoomDuration = 0.4f;

        [Header("Player Side Staging")]
        [SerializeField] private float sideOffset = 1.65f;

        [Header("State")]
        [SerializeField] private bool pieceTaken = false;

        private SpriteRenderer spriteRenderer;
        private bool playerInRange;
        private bool isInteracting;
        private Transform playerTransform;
        private PlayerMovement playerMovement;

        public bool PieceTaken => pieceTaken;

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
                spriteRenderer.sprite = pieceTaken ? animationFrames[animationFrames.Length - 1] : animationFrames[0];
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
                StartCoroutine(InteractSequence());
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

        IEnumerator InteractSequence()
        {
            isInteracting = true;
            HidePrompt(true);

            // if piece already taken, check if player is returning it
            if (pieceTaken)
            {
                if (PuzzleInventoryHUD.HeldPiece == pieceType)
                {
                    yield return StartCoroutine(PutBackSequence());
                }
                else if (PuzzleInventoryHUD.HasPiece)
                {
                    yield return StartCoroutine(ShowQuickSubtitle("This piece doesn't belong to this painting."));
                }
                else
                {
                    yield return StartCoroutine(ShowQuickSubtitle("The puzzle piece has already been removed from this painting."));
                }

                isInteracting = false;
                yield break;
            }

            // player can only hold one piece at a time
            if (PuzzleInventoryHUD.HasPiece)
            {
                yield return StartCoroutine(ShowQuickSubtitle("I can only carry one puzzle piece at a time."));
                isInteracting = false;
                yield break;
            }

            // take piece
            yield return StartCoroutine(TakePieceSequence());
            isInteracting = false;
        }

        IEnumerator TakePieceSequence()
        {
            FreezePlayer(true);

            // stage player to side so painting is not covered
            yield return StartCoroutine(StagePlayerBesidePainting(0.25f));

            var cam = Camera.main;
            CameraFollow camFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (camFollow != null)
            {
                camFollow.FocusOnTarget(transform);
                StartCoroutine(camFollow.ZoomTo(zoomedOrthoSize, zoomDuration));
            }

            var overlay = ConfrontationOverlay.Instance;
            if (overlay != null)
            {
                StartCoroutine(overlay.FadeVignette(0.5f, zoomDuration));
            }

            yield return new WaitForSeconds(zoomDuration * 0.5f);

            // play forward animation 0 -> last frame
            if (spriteRenderer != null && animationFrames != null && animationFrames.Length > 0)
            {
                for (int i = 0; i < animationFrames.Length; i++)
                {
                    if (animationFrames[i] != null)
                    {
                        spriteRenderer.sprite = animationFrames[i];
                    }
                    yield return new WaitForSeconds(frameDuration);
                }
            }

            pieceTaken = true;

            var hud = PuzzleInventoryHUD.Instance;
            if (hud != null)
            {
                hud.SetHoldingPiece(true, pieceType);
            }
            else
            {
                // update fallback static state
                PuzzleInventoryHUD.Instance?.SetHoldingPiece(true, pieceType);
            }

            string message = "Acquired the " + pieceName + " puzzle piece.";
            if (overlay != null)
            {
                overlay.ShowDirectSubtitle(message);
            }

            yield return WaitForDismissOrDuration(2.0f);

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

        IEnumerator PutBackSequence()
        {
            FreezePlayer(true);

            // stage player to side so painting is not covered
            yield return StartCoroutine(StagePlayerBesidePainting(0.25f));

            var cam = Camera.main;
            CameraFollow camFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (camFollow != null)
            {
                camFollow.FocusOnTarget(transform);
                StartCoroutine(camFollow.ZoomTo(zoomedOrthoSize, zoomDuration));
            }

            var overlay = ConfrontationOverlay.Instance;
            if (overlay != null)
            {
                StartCoroutine(overlay.FadeVignette(0.5f, zoomDuration));
            }

            yield return new WaitForSeconds(zoomDuration * 0.5f);

            // play reverse animation from last frame back to 0
            if (spriteRenderer != null && animationFrames != null && animationFrames.Length > 0)
            {
                for (int i = animationFrames.Length - 1; i >= 0; i--)
                {
                    if (animationFrames[i] != null)
                    {
                        spriteRenderer.sprite = animationFrames[i];
                    }
                    yield return new WaitForSeconds(frameDuration);
                }
            }

            pieceTaken = false;

            var hud = PuzzleInventoryHUD.Instance;
            if (hud != null)
            {
                hud.SetHoldingPiece(false, PuzzlePieceType.None);
            }

            if (overlay != null)
            {
                overlay.ShowDirectSubtitle("Returned the puzzle piece to the painting.");
            }

            yield return WaitForDismissOrDuration(2.0f);

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

        IEnumerator StagePlayerBesidePainting(float duration)
        {
            if (playerTransform == null) yield break;

            // cherry blossom stands left; japanese crane stands right
            float chosenSide = (pieceType == PuzzlePieceType.CherryBlossom) ? -Mathf.Abs(sideOffset) : Mathf.Abs(sideOffset);
            float targetX = transform.position.x + chosenSide;
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

            // face towards the painting
            float dir = transform.position.x - targetX;
            if (Mathf.Abs(dir) > 0.01f)
            {
                Vector3 ls = playerTransform.localScale;
                ls.x = Mathf.Sign(dir) * Mathf.Abs(ls.x);
                playerTransform.localScale = ls;
            }
        }

        IEnumerator ShowQuickSubtitle(string text)
        {
            var overlay = ConfrontationOverlay.Instance;
            if (overlay != null)
            {
                overlay.ShowDirectSubtitle(text);
                yield return WaitForDismissOrDuration(2.0f);
                yield return overlay.FadeOutSubtitle(0.3f);
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

        void FreezePlayer(bool freeze)
        {
            if (playerMovement != null) playerMovement.canMove = !freeze;
            if (freeze && playerTransform != null)
            {
                var rb = playerTransform.GetComponent<Rigidbody2D>() ?? playerTransform.GetComponentInParent<Rigidbody2D>();
                if (rb != null) rb.velocity = Vector2.zero;
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
            var pm = FindObjectOfType<PlayerMovement>();
            if (pm != null)
            {
                playerMovement = pm;
                playerTransform = pm.transform;
                return;
            }

            var p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Ryu");
            if (p != null)
            {
                playerMovement = p.GetComponentInChildren<PlayerMovement>();
                playerTransform = playerMovement != null ? playerMovement.transform : p.transform;
            }
        }

        bool IsPlayer(Collider2D col)
        {
            return col.CompareTag("Player") || col.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
