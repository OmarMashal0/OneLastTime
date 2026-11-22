using System.Collections;
using UnityEngine;
using OneLastTime.Player;
using OneLastTime.Systems;
using OneLastTime.UI;

namespace OneLastTime.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class SwordDisplayCase : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private InteractionPromptView promptView;
        [SerializeField] private float interactDistance = 2.8f;

        [Header("Dialogue Content")]
        [TextArea(2, 3)]
        [SerializeField] private string dialogueText = "My sword... It needs two keys. I have to find them.";
        [SerializeField] private float dialogueDuration = 2.8f;

        [Header("Camera Settings")]
        [SerializeField] private float zoomedOrthoSize = 3.8f;
        [SerializeField] private float normalOrthoSize = 5.0f;
        [SerializeField] private float zoomDuration = 0.4f;

        private bool playerInRange;
        private bool isInspecting;
        private Transform playerTransform;
        private PlayerMovement playerMovement;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

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

            // freeze player movement
            if (playerMovement != null) playerMovement.canMove = false;
            if (playerTransform != null)
            {
                var rb = playerTransform.GetComponent<Rigidbody2D>() ?? playerTransform.GetComponentInParent<Rigidbody2D>();
                if (rb != null) rb.velocity = Vector2.zero;
            }

            // zoom camera in on case
            var cam = Camera.main;
            CameraFollow camFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (camFollow != null)
            {
                StartCoroutine(camFollow.ZoomTo(zoomedOrthoSize, zoomDuration));
            }

            var overlay = ConfrontationOverlay.Instance;
            if (overlay != null)
            {
                StartCoroutine(overlay.FadeVignette(0.6f, zoomDuration));
                overlay.ShowDirectSubtitle(dialogueText);
            }

            // show key badge on left side of screen
            var hud = KeyCollectionHUD.Instance;
            if (hud != null)
            {
                hud.ShowBadge(0);
            }

            // wait for read duration or player press
            yield return new WaitForSeconds(0.4f);
            float elapsed = 0f;
            while (elapsed < dialogueDuration)
            {
                elapsed += Time.deltaTime;
                if (elapsed > 0.4f && (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Space)))
                {
                    break;
                }
                yield return null;
            }

            // fade out text and vignette
            if (overlay != null)
            {
                yield return overlay.FadeOutSubtitle(0.3f);
                StartCoroutine(overlay.FadeVignette(0f, zoomDuration));
            }

            // zoom camera back to normal
            if (camFollow != null)
            {
                yield return camFollow.ZoomTo(normalOrthoSize, zoomDuration);
            }

            // unfreeze player
            if (playerMovement != null) playerMovement.canMove = true;
            isInspecting = false;
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
