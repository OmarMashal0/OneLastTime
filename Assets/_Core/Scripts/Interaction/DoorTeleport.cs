using System.Collections;
using UnityEngine;
using OneLastTime.Player;
using OneLastTime.Systems;
using OneLastTime.UI;

namespace OneLastTime.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class DoorTeleport : MonoBehaviour
    {
        [Header("Teleport Target")]
        [Tooltip("The Transform where the player will be teleported")]
        [SerializeField] private Transform destination;

        [Header("Interaction")]
        [Tooltip("Key to press when standing in front of the door")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        [Tooltip("Optional prompt GameObject (e.g. '[E] Enter') to show when near the door")]
        [SerializeField] private GameObject interactionPrompt;

        [Header("Player Scaling")]
        [Tooltip("Scale multiplier for the player when teleported (e.g. 2 for 2x scale, 0 to leave unchanged)")]
        [SerializeField] private float targetPlayerScale = 2f;

        [Header("Camera Transition")]
        [Tooltip("Update camera limits when entering this zone")]
        [SerializeField] private bool updateCameraLimits = true;
        [SerializeField] private float newMinX = -2.5f;
        [SerializeField] private float newMaxX = 20.0f;
        [SerializeField] private float newMinY = -20.0f;
        [SerializeField] private float newMaxY = -18.0f;

        [Header("Fade Timings")]
        [SerializeField] private float fadeDuration = 0.4f;
        [SerializeField] private float blackoutHoldTime = 0.15f;

        [Header("Door Open Animation")]
        [Tooltip("SpriteRenderer displaying the door")]
        [SerializeField] private SpriteRenderer doorSpriteRenderer;
        [Tooltip("Frames of the door opening sequence")]
        [SerializeField] private Sprite[] openFrames;
        [Tooltip("Playback speed of the door opening animation in frames per second")]
        [SerializeField] private float openFrameRate = 12f;

        private bool isPlayerInZone;
        private GameObject playerObj;
        private PlayerMovement playerMovement;
        private bool isTransitioning;

        public Transform Destination { get => destination; set => destination = value; }
        public GameObject InteractionPrompt { get => interactionPrompt; set => interactionPrompt = value; }
        public bool IsTransitioning => isTransitioning;

        private InteractableTarget interactableTarget;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            if (doorSpriteRenderer == null)
                doorSpriteRenderer = GetComponent<SpriteRenderer>();

            if (openFrames != null && openFrames.Length > 0 && doorSpriteRenderer != null)
                doorSpriteRenderer.sprite = openFrames[0];

            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);

            interactableTarget = GetComponent<InteractableTarget>();
            if (interactableTarget != null)
            {
                interactableTarget.canInteractCheck = () => !isTransitioning;
            }
        }

        public void StartDoorTransition()
        {
            if (isTransitioning) return;
            if (playerObj == null)
            {
                var pm = FindObjectOfType<PlayerMovement>();
                if (pm != null)
                {
                    playerMovement = pm;
                    playerObj = pm.gameObject;
                }
                else
                {
                    var p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Ryu");
                    if (p != null)
                    {
                        playerMovement = p.GetComponentInChildren<PlayerMovement>();
                        playerObj = playerMovement != null ? playerMovement.gameObject : p;
                    }
                }
            }
            StartCoroutine(TransitionRoutine());
        }

        private void Update()
        {
            // If managed by InteractableTarget, skip direct input polling
            if (interactableTarget != null) return;

            if (!isPlayerInZone || isTransitioning) return;

            if (Input.GetKeyDown(interactKey))
            {
                StartDoorTransition();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                isPlayerInZone = true;
                playerObj = other.gameObject;
                playerMovement = other.GetComponent<PlayerMovement>() ?? other.GetComponentInParent<PlayerMovement>();

                if (interactableTarget == null && interactionPrompt != null && !isTransitioning)
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
                if (interactableTarget == null && interactionPrompt != null)
                {
                    interactionPrompt.SetActive(false);
                }
            }
        }

        private IEnumerator TransitionRoutine()
        {
            if (destination == null || playerObj == null) yield break;

            isTransitioning = true;
            if (interactionPrompt != null) interactionPrompt.SetActive(false);

            // 1. Freeze player
            if (playerMovement != null) playerMovement.canMove = false;
            var rb = playerObj.GetComponent<Rigidbody2D>() ?? playerObj.GetComponentInParent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;

            // 2. Play door opening animation if frames provided
            if (doorSpriteRenderer == null)
                doorSpriteRenderer = GetComponent<SpriteRenderer>();

            if (openFrames != null && openFrames.Length > 0 && doorSpriteRenderer != null)
            {
                float delayPerFrame = 1f / Mathf.Max(1f, openFrameRate);
                for (int i = 0; i < openFrames.Length; i++)
                {
                    if (openFrames[i] != null)
                        doorSpriteRenderer.sprite = openFrames[i];
                    yield return new WaitForSeconds(delayPerFrame);
                }
            }

            // 3. Fade to black
            var fader = ScreenFader.Instance;
            if (fader != null)
            {
                yield return fader.FadeToBlack(fadeDuration);
            }
            else
            {
                yield return new WaitForSeconds(fadeDuration);
            }

            // Reset door back to closed state behind the blackout
            if (openFrames != null && openFrames.Length > 0 && doorSpriteRenderer != null)
            {
                doorSpriteRenderer.sprite = openFrames[0];
            }

            // 3. Teleport & scale player
            if (targetPlayerScale > 0f)
            {
                float facingSign = Mathf.Sign(playerObj.transform.localScale.x);
                if (facingSign == 0f) facingSign = 1f;
                playerObj.transform.localScale = new Vector3(targetPlayerScale * facingSign, targetPlayerScale, targetPlayerScale);
            }

            Transform rootPlayer = playerObj.transform.root != null && (playerObj.transform.root.name.IndexOf("ryu", System.StringComparison.OrdinalIgnoreCase) >= 0 || playerObj.transform.root.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0)
                ? playerObj.transform.root
                : playerObj.transform;

            rootPlayer.position = destination.position;
            playerObj.transform.position = destination.position;

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.position = destination.position;
            }

            // 4. Update Camera
            var cam = Camera.main;
            if (cam != null)
            {
                var cf = cam.GetComponent<CameraFollow>();
                if (cf != null)
                {
                    if (updateCameraLimits)
                    {
                        float finalMaxX = newMaxX;
                        // Only clamp to Museum_Right_Wall if teleporting into the upper museum corridor (-35 < Y < -15)
                        if (destination.position.y > -35f && destination.position.y < -15f)
                        {
                            var rightWall = GameObject.Find("Museum_Right_Wall");
                            if (rightWall != null)
                            {
                                float halfWidth = (cam != null && cam.orthographic) ? cam.orthographicSize * cam.aspect : 8.89f;
                                float calculatedMaxX = rightWall.transform.position.x - halfWidth;
                                if (calculatedMaxX > finalMaxX)
                                {
                                    finalMaxX = calculatedMaxX;
                                }
                            }
                        }

                        cf.SetLimits(newMinX, finalMaxX, newMinY, newMaxY);
                    }
                    cf.SnapToTarget();
                }
            }

            // 5. Short pause in blackout
            if (blackoutHoldTime > 0f)
            {
                yield return new WaitForSeconds(blackoutHoldTime);
            }

            // 6. Fade back in
            if (fader != null)
            {
                yield return fader.FadeFromBlack(fadeDuration);
            }

            // 7. Unfreeze player
            if (playerMovement != null) playerMovement.canMove = true;
            isTransitioning = false;
        }

        private bool IsPlayer(Collider2D other)
        {
            return other.CompareTag("Player") || other.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

