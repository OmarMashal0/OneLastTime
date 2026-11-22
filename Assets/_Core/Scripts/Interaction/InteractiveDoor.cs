using System.Collections;
using UnityEngine;
using OneLastTime.Player;

namespace OneLastTime.Interaction
{
    public class InteractiveDoor : MonoBehaviour
    {
        [Header("Door State")]
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool startOpen = false;

        [Header("Animation")]
        [SerializeField] private SpriteRenderer doorSpriteRenderer;
        [SerializeField] private Sprite[] openFrames;
        [SerializeField] private float openFrameRate = 12f;
        [SerializeField] private Animator animator;

        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private InteractionPromptView promptView;
        [SerializeField] private float interactionDistance = 2.5f;

        [Header("Collision")]
        [SerializeField] private Collider2D obstacleCollider;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openClip;

        private Transform playerTransform;
        private bool playerInRange;
        private Coroutine openRoutine;

        public bool IsOpen => isOpen;

        void Awake()
        {
            if (doorSpriteRenderer == null)
            {
                doorSpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (promptView == null)
            {
                promptView = GetComponentInChildren<InteractionPromptView>(true);
            }

            if (interactionPrompt == null && promptView != null)
            {
                interactionPrompt = promptView.gameObject;
            }

            var target = GetComponent<InteractableTarget>();
            if (target != null)
            {
                target.canInteractCheck = () => !isOpen;
                target.OnInteract.AddListener(OpenDoor);
            }
        }

        void Start()
        {
            if (startOpen)
            {
                SetOpenedState();
            }
            else
            {
                HidePrompt(true);
                if (openFrames != null && openFrames.Length > 0 && doorSpriteRenderer != null)
                {
                    doorSpriteRenderer.sprite = openFrames[0];
                }
            }
        }

        void Update()
        {
            if (isOpen) return;

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float dist = Vector2.Distance(transform.position, playerTransform.position);
            bool inRange = dist <= interactionDistance;

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
                OpenDoor();
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (isOpen) return;
            if (IsPlayer(other))
            {
                playerInRange = true;
                ShowPrompt();
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (isOpen) return;
            if (IsPlayer(other))
            {
                playerInRange = false;
                HidePrompt(false);
            }
        }

        public void OpenDoor()
        {
            if (isOpen) return;
            isOpen = true;
            playerInRange = false;

            HidePrompt(true);

            // remove solid barrier once opened
            if (obstacleCollider != null)
            {
                obstacleCollider.enabled = false;
            }

            if (audioSource != null && openClip != null)
            {
                audioSource.PlayOneShot(openClip);
            }

            if (animator != null)
            {
                animator.SetTrigger("Open");
            }

            if (openFrames != null && openFrames.Length > 0 && doorSpriteRenderer != null)
            {
                if (openRoutine != null)
                {
                    StopCoroutine(openRoutine);
                }
                openRoutine = StartCoroutine(PlayOpenAnimation());
            }
        }

        void SetOpenedState()
        {
            isOpen = true;
            playerInRange = false;
            HidePrompt(true);

            if (obstacleCollider != null)
            {
                obstacleCollider.enabled = false;
            }

            if (openFrames != null && openFrames.Length > 0 && doorSpriteRenderer != null)
            {
                doorSpriteRenderer.sprite = openFrames[openFrames.Length - 1];
            }

            if (animator != null)
            {
                animator.Play("door_open", 0, 1f);
            }
        }

        IEnumerator PlayOpenAnimation()
        {
            float delay = 1f / Mathf.Max(1f, openFrameRate);

            for (int i = 0; i < openFrames.Length; i++)
            {
                if (openFrames[i] != null)
                {
                    doorSpriteRenderer.sprite = openFrames[i];
                }
                yield return new WaitForSeconds(delay);
            }

            // keep open on the last frame
            doorSpriteRenderer.sprite = openFrames[openFrames.Length - 1];
            openRoutine = null;
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
                playerTransform = pm.transform;
                return;
            }

            var p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Ryu");
            if (p != null)
            {
                playerTransform = p.transform;
            }
        }

        bool IsPlayer(Collider2D col)
        {
            return col.CompareTag("Player") || col.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
