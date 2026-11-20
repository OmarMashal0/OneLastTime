using System.Collections;
using UnityEngine;
using OneLastTime.Player;
using OneLastTime.Systems;
using OneLastTime.UI;

namespace OneLastTime.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class NPCConfrontation : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private GameObject interactionPrompt;

        [Header("NPC References")]
        [SerializeField] private Animator npcAnimator;
        [SerializeField] private GameObject[] speechBubbles;

        [Header("Cinematic Settings")]
        [SerializeField] private float zoomedOrthoSize = 3.85f;
        [SerializeField] private float normalOrthoSize = 5.0f;
        [SerializeField] private float zoomDuration = 0.5f;

        [Header("Dialogue Content & Pacing")]
        [Tooltip("The dialogue revealed word-by-word. The final entry triggers the special climax sound.")]
        [TextArea(2, 3)]
        [SerializeField] private string[] dialogueSteps = new string[]
        {
            "\"One...",
            "\"One more...",
            "\"One more whisper...",
            "\"One more whisper... and it's your...",
            "\"One more whisper... and it's your <color=#FF2A2A><size=115%><b>LAST.</b></size></color>\""
        };
        [Tooltip("Pause duration in seconds after each step")]
        [SerializeField] private float[] stepPauses = new float[] { 0.35f, 0.35f, 0.65f, 0.45f, 1.6f };

        [Header("Post-Interaction Replacement")]
        [Tooltip("Scared NPCs prefab or GameObject to show in the same place after interaction ends")]
        [SerializeField] private GameObject scaredNpcPrefab;
        [Tooltip("Custom scale for the scared NPCs when spawned. Leave at 0 to use prefab's scale.")]
        [SerializeField] private Vector3 scaredNpcScale = new Vector3(0.163f, 0.163f, 1f);

        private bool isPlayerInZone;
        private bool hasTriggered;
        private GameObject playerObj;
        private PlayerMovement playerMovement;

        public GameObject InteractionPrompt { get => interactionPrompt; set => interactionPrompt = value; }
        public GameObject[] SpeechBubbles { get => speechBubbles; set => speechBubbles = value; }
        public GameObject ScaredNpcPrefab { get => scaredNpcPrefab; set => scaredNpcPrefab = value; }
        public Animator NpcAnimator { get => npcAnimator; set => npcAnimator = value; }
        public bool HasTriggered => hasTriggered;

        private InteractableTarget interactableTarget;

        private void Awake()
        {
            if (npcAnimator == null)
            {
                npcAnimator = GetComponent<Animator>();
            }

            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }

            interactableTarget = GetComponent<InteractableTarget>();
            if (interactableTarget != null)
            {
                interactableTarget.canInteractCheck = () => !hasTriggered;
            }
        }

        public void StartConfrontation()
        {
            if (hasTriggered) return;
            hasTriggered = true;

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
            StartCoroutine(ConfrontSequence());
        }

        private void Update()
        {
            // If managed by InteractableTarget, skip direct input polling
            if (interactableTarget != null) return;

            if (!isPlayerInZone || hasTriggered) return;

            if (Input.GetKeyDown(interactKey))
            {
                StartConfrontation();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered) return;

            if (IsPlayer(other))
            {
                isPlayerInZone = true;
                playerObj = other.gameObject;
                playerMovement = other.GetComponent<PlayerMovement>() ?? other.GetComponentInParent<PlayerMovement>();

                if (interactableTarget == null && interactionPrompt != null)
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

        private IEnumerator ConfrontSequence()
        {
            hasTriggered = true;
            if (interactionPrompt != null) interactionPrompt.SetActive(false);

            // 1. Freeze Ryu
            if (playerMovement != null) playerMovement.canMove = false;
            var rb = playerObj.GetComponent<Rigidbody2D>() ?? playerObj.GetComponentInParent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;

            // 2. Face the villagers
            if (playerObj != null)
            {
                float dir = Mathf.Sign(transform.position.x - playerObj.transform.position.x);
                if (dir != 0f)
                {
                    Vector3 s = playerObj.transform.localScale;
                    s.x = Mathf.Abs(s.x) * dir;
                    playerObj.transform.localScale = s;
                }
            }

            // 3. Immediately kill and disable gossip speech bubbles permanently
            var proxBubble = GetComponent<NPCProximityBubble>();
            if (proxBubble != null)
            {
                proxBubble.StopAndDisablePermanently();
            }

            if (speechBubbles != null)
            {
                foreach (var b in speechBubbles)
                {
                    if (b != null)
                    {
                        b.SetActive(false);
                        var rends = b.GetComponentsInChildren<Renderer>(true);
                        foreach (var r in rends) r.enabled = false;
                    }
                }
            }

            // Also search all child objects for any stray bubble objects and deactivate them
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name.IndexOf("bubble", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    child.gameObject.SetActive(false);
                    var rends = child.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in rends) r.enabled = false;
                }
            }

            // 4. Start Camera Zoom & Dark Vignette
            var cam = Camera.main;
            CameraFollow camFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            var overlay = ConfrontationOverlay.Instance;

            if (camFollow != null)
            {
                StartCoroutine(camFollow.ZoomTo(zoomedOrthoSize, zoomDuration));
            }

            if (overlay != null)
            {
                StartCoroutine(overlay.FadeVignette(0.75f, 0.4f));
            }

            yield return new WaitForSeconds(0.3f);

            // 5. Deliver line word-by-word with dramatic pacing
            string[] steps = (dialogueSteps != null && dialogueSteps.Length > 0) ? dialogueSteps : new string[]
            {
                "\"One...",
                "\"One more...",
                "\"One more whisper...",
                "\"One more whisper... and it's your...",
                "\"One more whisper... and it's your <color=#FF2A2A><size=115%><b>LAST.</b></size></color>\""
            };
            float[] pauses = (stepPauses != null && stepPauses.Length > 0) ? stepPauses : new float[] { 0.35f, 0.35f, 0.65f, 0.45f, 1.6f };

            if (overlay != null)
            {
                yield return overlay.PlayWordByWord(steps, pauses, null);
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }

            // 6. Fade out subtitle & vignette
            if (overlay != null)
            {
                StartCoroutine(overlay.FadeVignette(0f, 0.4f));
                yield return overlay.FadeOutSubtitle(0.35f);
            }

            // 7. Restore camera zoom
            if (camFollow != null)
            {
                yield return camFollow.ZoomTo(normalOrthoSize, zoomDuration);
            }

            // 8. Unfreeze Ryu
            if (playerMovement != null) playerMovement.canMove = true;

            // 9. Put the new scared NPCs animation in the exact same place and remove previous whispering one
            if (scaredNpcPrefab != null)
            {
                GameObject scaredNpc;
                if (scaredNpcPrefab.scene.IsValid())
                {
                    scaredNpc = scaredNpcPrefab;
                    scaredNpc.transform.position = transform.position;
                    scaredNpc.SetActive(true);
                }
                else
                {
                    scaredNpc = Instantiate(scaredNpcPrefab, transform.position, transform.rotation, transform.parent);
                    scaredNpc.name = "Villagers_Scared";
                }

                if (scaredNpcScale != Vector3.zero)
                {
                    scaredNpc.transform.localScale = scaredNpcScale;
                }
            }

            gameObject.SetActive(false);
        }

        private bool IsPlayer(Collider2D other)
        {
            return other.CompareTag("Player") || other.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

