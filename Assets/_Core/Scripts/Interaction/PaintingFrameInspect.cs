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
        [SerializeField] private float dialogueDuration = 3.2f;

        [Header("Camera Settings")]
        [SerializeField] private float zoomedOrthoSize = 3.5f;
        [SerializeField] private float normalOrthoSize = 5.0f;
        [SerializeField] private float zoomDuration = 0.4f;

        [Header("Left Piece Assembly")]
        [SerializeField] private Sprite[] leftPieceFrames;
        [SerializeField] private float frameDuration = 0.2f;

        [Header("State")]
        [SerializeField] private bool leftPiecePlaced = false;

        private SpriteRenderer frameRenderer;
        private Vector3 initialLocalScale;
        private float initialSpriteWidth = 1086f;
        private bool playerInRange;
        private bool isInspecting;
        private Transform playerTransform;
        private PlayerMovement playerMovement;

        public bool LeftPiecePlaced => leftPiecePlaced;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

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

            HidePrompt(true);
        }

        void Start()
        {
            FindPlayer();

            if (leftPiecePlaced && leftPieceFrames != null && leftPieceFrames.Length > 0)
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

            PuzzlePieceType heldPiece = PuzzleInventoryHUD.HeldPiece;

            if (leftPiecePlaced)
            {
                yield return StartCoroutine(RunDialogueRoutine("The left piece is in place. One more piece is still missing."));
                isInspecting = false;
                yield break;
            }

            if (heldPiece == PuzzlePieceType.CherryBlossom)
            {
                // place the correct piece
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
                camFollow.FocusOnTarget(transform);
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
                camFollow.FocusOnTarget(transform);
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
            }
        }

        IEnumerator StagePlayerBesideFrame(float duration)
        {
            if (playerTransform == null) yield break;

            float targetX = transform.position.x - 1.55f;
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
