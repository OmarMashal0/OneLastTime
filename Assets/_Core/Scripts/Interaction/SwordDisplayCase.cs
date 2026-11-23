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
        [SerializeField] private string dialogueText = "This is my sword... I need a key to get it.";
        [TextArea(2, 3)]
        [SerializeField] private string hasKeyDialogue = "I have the key. Let's get my katana back...";
        [SerializeField] private float dialogueDuration = 2.8f;

        [Header("State")]
        [SerializeField] private bool hasInspectedOnce = false;
        [SerializeField] private bool katanaRestored = false;

        [Header("Restored Display Case")]
        [SerializeField] private Sprite openedCaseSprite;

        [Header("Target Guard")]
        [SerializeField] private Enemies.MuseumGuard targetGuard;

        [Header("Camera Settings")]
        [SerializeField] private float zoomedOrthoSize = 4.4f;
        [SerializeField] private float normalOrthoSize = 5.0f;
        [SerializeField] private float zoomDuration = 0.4f;
        [SerializeField] private Vector3 cameraFocusOffset = new Vector3(0f, -0.9f, 0f);

        [Header("UI Reference")]
        [SerializeField] private KeyCollectionHUD keyHUD;

        private bool playerInRange;
        private bool isInspecting;
        private Transform playerTransform;
        private PlayerMovement playerMovement;

        public bool KatanaRestored => katanaRestored;

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

            keyHUD = FindKeyHUD();

            if (openedCaseSprite == null)
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                if (allSprites != null)
                {
                    openedCaseSprite = System.Array.Find(allSprites, s => s != null && s.name.Contains("Display Case with Keyhole"));
                }
            }
        }

        void Start()
        {
            FindPlayer();

            if (katanaRestored)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null && openedCaseSprite != null)
                {
                    sr.sprite = openedCaseSprite;
                }
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
                HidePrompt(true);
            }
        }

        void Update()
        {
            if (katanaRestored || isInspecting) return;

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
            if (katanaRestored || isInspecting) return;
            if (IsPlayer(other))
            {
                playerInRange = true;
                ShowPrompt();
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (katanaRestored || isInspecting) return;
            if (IsPlayer(other))
            {
                playerInRange = false;
                HidePrompt(false);
            }
        }

        IEnumerator InspectSequence()
        {
            if (katanaRestored) yield break;

            isInspecting = true;
            HidePrompt(true);

            keyHUD = FindKeyHUD();

            if (openedCaseSprite == null)
            {
                var allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                if (allSprites != null)
                {
                    openedCaseSprite = System.Array.Find(allSprites, s => s != null && s.name.Contains("Display Case with Keyhole"));
                }
            }

            bool hasKey = (keyHUD != null && keyHUD.HasKey);

            if (hasKey)
            {
                yield return StartCoroutine(RestoreKatanaSequence());
            }
            else
            {
                yield return StartCoroutine(LockedInspectSequence());
            }

            isInspecting = false;
        }

        IEnumerator LockedInspectSequence()
        {
            FreezePlayer(true);

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
                overlay.ShowDirectSubtitle(dialogueText);
            }

            // show key badge on first inspect
            if (!hasInspectedOnce)
            {
                hasInspectedOnce = true;
                if (keyHUD != null)
                {
                    keyHUD.gameObject.SetActive(true);
                    keyHUD.ShowBadge(0);
                }
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

        IEnumerator RestoreKatanaSequence()
        {
            FreezePlayer(true);

            var cam = Camera.main;
            CameraFollow camFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (camFollow != null)
            {
                camFollow.FocusOnTarget(transform, cameraFocusOffset);
                StartCoroutine(camFollow.ZoomTo(zoomedOrthoSize, zoomDuration));
            }

            // first the hud is removed
            if (keyHUD != null)
            {
                keyHUD.gameObject.SetActive(false);
            }
            if (KeyCollectionHUD.Instance != null)
            {
                KeyCollectionHUD.Instance.gameObject.SetActive(false);
            }

            // show text
            var overlay = ConfrontationOverlay.Instance;
            if (overlay != null)
            {
                StartCoroutine(overlay.FadeVignette(0.6f, zoomDuration));
                overlay.ShowDirectSubtitle(hasKeyDialogue);
            }

            if (targetGuard != null) targetGuard.ReactToSwordPickup(playerTransform);

            yield return WaitForDismissOrDuration(dialogueDuration);
            while (targetGuard != null && targetGuard.IsReactingToSwordPickup)
            {
                yield return null;
            }

            if (overlay != null)
            {
                yield return overlay.FadeOutSubtitle(0.2f);
            }

            var fader = ScreenFader.Instance;
            if (fader != null) yield return fader.FadeToBlack(0f);

            if (targetGuard != null) yield return targetGuard.FireSwordPickupShot();
            yield return new WaitForSecondsRealtime(1f);

            // change sprite to Pixel Art Display Case with Keyhole
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && openedCaseSprite != null)
            {
                sr.sprite = openedCaseSprite;
            }

            katanaRestored = true;

            if (targetGuard != null) targetGuard.Surrender();

            // player cannot inspect it again
            HidePrompt(true);
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // allow player to hit using attacks (katana restored!)
            var attack = playerTransform != null ? playerTransform.GetComponent<PlayerAttack>() : null;
            if (attack == null) attack = FindObjectOfType<PlayerAttack>();
            if (attack == null && playerTransform != null)
            {
                attack = playerTransform.gameObject.AddComponent<PlayerAttack>();
            }
            if (attack != null)
            {
                attack.UnlockAttacks();
            }

            if (overlay != null)
            {
                yield return overlay.FadeVignette(0f, 0f);
            }
            if (fader != null) yield return fader.FadeFromBlack(0.9f);

            if (camFollow != null)
            {
                camFollow.ClearFocus();
                yield return camFollow.ZoomTo(normalOrthoSize, zoomDuration);
            }

            FreezePlayer(false);
        }

        void FreezePlayer(bool freeze)
        {
            if (playerMovement == null) FindPlayer();
            if (playerMovement != null) playerMovement.canMove = !freeze;

            if (freeze && playerTransform != null)
            {
                var rb = playerTransform.GetComponent<Rigidbody2D>() ?? playerTransform.GetComponentInParent<Rigidbody2D>();
                if (rb != null) rb.velocity = Vector2.zero;
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

        private KeyCollectionHUD FindKeyHUD()
        {
            if (keyHUD != null) return keyHUD;
            if (KeyCollectionHUD.Instance != null) return KeyCollectionHUD.Instance;

            // search under Cutscene_Canvas which is always active
            var canvas = GameObject.Find("Cutscene_Canvas");
            if (canvas != null)
            {
                var hudTrans = canvas.transform.Find("KeyCollection_HUD");
                if (hudTrans != null)
                {
                    keyHUD = hudTrans.GetComponent<KeyCollectionHUD>();
                    if (keyHUD != null) return keyHUD;
                }
            }

            var all = FindObjectsOfType<KeyCollectionHUD>(true);
            if (all != null && all.Length > 0)
            {
                keyHUD = all[0];
                return keyHUD;
            }

            return null;
        }
    }
}

