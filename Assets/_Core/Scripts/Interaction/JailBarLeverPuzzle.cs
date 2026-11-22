using System.Collections;
using UnityEngine;
using OneLastTime.Player;
using OneLastTime.Pet;
using OneLastTime.Systems;

namespace OneLastTime.Interaction
{
    public class JailBarLeverPuzzle : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private InteractionPromptView promptView;
        [SerializeField] private float interactDistance = 3.5f;

        [Header("Puzzle Targets")]
        [SerializeField] private SpriteRenderer leverRenderer;
        [SerializeField] private Sprite pulledLeverSprite;
        [SerializeField] private Transform slidingDoor;
        [SerializeField] private string targetDoorName = "Tileset_10 (5)";

        [Header("Movement & Shake")]
        [SerializeField] private float doorTargetX = 63.0f;
        [SerializeField] private float doorMoveDuration = 1.3f;
        [SerializeField] private float shakeMagnitude = 0.2f;

        private bool playerInRange;
        private bool playerInTrigger;
        private bool isActivated;
        private Transform playerTransform;
        private PlayerMovement playerMovement;
        private Rigidbody2D playerRb;
        private Animator playerAnimator;
        private PetFollower petFollower;

        void Awake()
        {
            if (promptView == null)
            {
                promptView = GetComponentInChildren<InteractionPromptView>(true);
            }

            if (interactionPrompt == null && promptView != null)
            {
                interactionPrompt = promptView.gameObject;
            }

            // hide prompt at runtime start until approached
            HidePrompt(true);

            if (leverRenderer == null)
            {
                var leverObj = GameObject.Find("Pixel Art Wooden Lever Switches_1");
                if (leverObj != null)
                {
                    leverRenderer = leverObj.GetComponent<SpriteRenderer>();
                }
            }

            if (pulledLeverSprite == null)
            {
#if UNITY_EDITOR
                var allSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Level1_Museum/Art/Feudal_Japan_Interior/Color_1/Props/Pixel Art Wooden Lever Switches.png");
                foreach (var obj in allSprites)
                {
                    if (obj is Sprite s && s.name == "Pixel Art Wooden Lever Switches_0")
                    {
                        pulledLeverSprite = s;
                        break;
                    }
                }
#endif
            }
        }

        void Start()
        {
            FindPlayer();
            FindDoor();
            HidePrompt(true);
        }

        void Update()
        {
            if (isActivated) return;

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            // check proximity: ryu is in front of the jail bars (around x=58.5, y=-45.5)
            float dx = Mathf.Abs(playerTransform.position.x - 58.5f);
            float dy = Mathf.Abs(playerTransform.position.y - (-45.5f));
            bool inRange = playerInTrigger || (dx <= interactDistance && dy <= 3.0f);

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
                StartCoroutine(PerformPuzzleSequence());
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (isActivated) return;
            if (IsPlayer(other))
            {
                playerInTrigger = true;
                if (!playerInRange)
                {
                    playerInRange = true;
                    ShowPrompt();
                }
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (isActivated) return;
            if (IsPlayer(other))
            {
                playerInTrigger = false;
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

        IEnumerator PerformPuzzleSequence()
        {
            isActivated = true;
            HidePrompt(true);

            // freeze player
            if (playerMovement != null) playerMovement.canMove = false;
            if (playerRb != null) playerRb.velocity = new Vector2(0f, playerRb.velocity.y);
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("isWalking", false);
                playerAnimator.SetFloat("moveSpeed", 0f);
            }

            // find companion raccoon
            if (petFollower == null) petFollower = FindObjectOfType<PetFollower>();

            if (petFollower == null)
            {
                yield return StartCoroutine(SlideDoorAndShake());
                if (playerMovement != null) playerMovement.canMove = true;
                yield break;
            }

            // take over pet
            petFollower.enabled = false;
            var petRb = petFollower.GetComponent<Rigidbody2D>();
            var petAnim = petFollower.GetComponentInChildren<Animator>();
            var petTransform = petFollower.transform;
            var petSr = petFollower.GetComponentInChildren<SpriteRenderer>();

            // ensure raccoon is on higher sorting layer so it is clearly visible
            if (petSr != null)
            {
                petSr.sortingLayerName = "ground";
                if (petSr.sortingOrder < 10) petSr.sortingOrder = 10;
            }

            // ignore collision with jail bars
            var barColliders = GetComponentsInChildren<Collider2D>();
            var petColliders = petFollower.GetComponentsInChildren<Collider2D>();
            foreach (var bc in barColliders)
            {
                foreach (var pc in petColliders)
                {
                    if (bc != null && pc != null) Physics2D.IgnoreCollision(bc, pc, true);
                }
            }

            // keep raccoon on its current floor y so movement is purely horizontal
            Vector2 petStartPos = (petRb != null) ? petRb.position : (Vector2)petTransform.position;
            float floorY = petStartPos.y;

            Vector2 leverPos = (leverRenderer != null) ? (Vector2)leverRenderer.transform.position : new Vector2(64.08f, -45.15f);
            float targetX = leverPos.x - 0.45f;
            Vector2 arriveSpot = new Vector2(targetX, floorY);

            if (petRb != null)
            {
                petRb.velocity = Vector2.zero;
                petRb.isKinematic = true;
            }

            // face right towards lever
            Vector3 s = petTransform.localScale;
            s.x = Mathf.Abs(s.x);
            petTransform.localScale = s;

            // run horizontally to underneath lever
            if (petAnim != null)
            {
                petAnim.SetBool("isMoving", true);
                petAnim.SetFloat("moveSpeed", 6f);
            }

            float runDist = Mathf.Abs(targetX - petStartPos.x);
            float runDuration = Mathf.Max(0.7f, runDist / 5.5f);
            float elapsed = 0f;

            while (elapsed < runDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / runDuration);
                float curX = Mathf.Lerp(petStartPos.x, targetX, t);
                Vector2 curPos = new Vector2(curX, floorY);
                if (petRb != null) petRb.position = curPos;
                else petTransform.position = curPos;
                yield return null;
            }

            if (petRb != null) petRb.position = arriveSpot;
            else petTransform.position = arriveSpot;

            // arrive under lever, pause briefly
            if (petAnim != null)
            {
                petAnim.SetBool("isMoving", false);
                petAnim.SetFloat("moveSpeed", 0f);
            }
            yield return new WaitForSeconds(0.12f);

            // hop up to pull lever
            Vector2 jumpApex = new Vector2(targetX + 0.12f, leverPos.y + 0.15f);
            float jumpUpTime = 0.24f;
            elapsed = 0f;
            while (elapsed < jumpUpTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / jumpUpTime);
                Vector2 curPos = Vector2.Lerp(arriveSpot, jumpApex, t);
                if (petRb != null) petRb.position = curPos;
                else petTransform.position = curPos;
                yield return null;
            }

            // switch lever sprite at apex
            if (leverRenderer != null && pulledLeverSprite != null)
            {
                leverRenderer.sprite = pulledLeverSprite;
            }

            // enable lower trench camera angle after lever changes
            var cam = Camera.main;
            var camFollow = (cam != null ? cam.GetComponent<CameraFollow>() : null) ?? FindObjectOfType<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.SetTrenchZoneEnabled(true);
            }

            // land back down on floor
            float jumpDownTime = 0.22f;
            elapsed = 0f;
            while (elapsed < jumpDownTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / jumpDownTime);
                Vector2 curPos = Vector2.Lerp(jumpApex, arriveSpot, t);
                if (petRb != null) petRb.position = curPos;
                else petTransform.position = curPos;
                yield return null;
            }

            if (petRb != null)
            {
                petRb.position = arriveSpot;
                petRb.isKinematic = false;
                petRb.velocity = Vector2.zero;
            }
            else
            {
                petTransform.position = arriveSpot;
            }

            // restore pet follow ai right away
            petFollower.enabled = true;

            // slide door and shake camera
            yield return StartCoroutine(SlideDoorAndShake());

            // restore player movement immediately (no wait, no return animation)
            if (playerMovement != null) playerMovement.canMove = true;
        }

        IEnumerator SlideDoorAndShake()
        {
            FindDoor();

            var camFollow = FindObjectOfType<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.StartCoroutine(camFollow.Shake(doorMoveDuration, shakeMagnitude));
            }

            if (slidingDoor != null)
            {
                Vector3 startPos = slidingDoor.position;
                Vector3 targetPos = new Vector3(doorTargetX, startPos.y, startPos.z);
                float elapsed = 0f;

                while (elapsed < doorMoveDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / doorMoveDuration);
                    slidingDoor.position = Vector3.Lerp(startPos, targetPos, t);
                    yield return null;
                }

                slidingDoor.position = targetPos;
            }

            yield return new WaitForSeconds(0.2f);
        }

        void FindPlayer()
        {
            if (playerTransform != null) return;

            var pm = FindObjectOfType<PlayerMovement>();
            if (pm != null)
            {
                playerTransform = pm.transform;
                playerMovement = pm;
                playerRb = pm.GetComponent<Rigidbody2D>();
                playerAnimator = pm.GetComponentInChildren<Animator>();
                return;
            }

            var player = GameObject.Find("Player_Ryu");
            if (player == null) player = GameObject.FindWithTag("Player");

            if (player != null)
            {
                playerTransform = player.transform;
                playerMovement = player.GetComponent<PlayerMovement>();
                playerRb = player.GetComponent<Rigidbody2D>();
                playerAnimator = player.GetComponentInChildren<Animator>();
            }
        }

        void FindDoor()
        {
            if (slidingDoor != null) return;

            var doorObj = GameObject.Find(targetDoorName);
            if (doorObj != null)
            {
                slidingDoor = doorObj.transform;
                return;
            }

            var allTransforms = FindObjectsOfType<Transform>();
            foreach (var t in allTransforms)
            {
                if (t != null && t.name.Contains("Tileset_10") && Mathf.Abs(t.position.x - 55.7448f) < 1.0f)
                {
                    slidingDoor = t;
                    break;
                }
            }
        }

        bool IsPlayer(Collider2D other)
        {
            if (other == null) return false;
            if (other.CompareTag("Player")) return true;
            if (other.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (other.GetComponentInParent<PlayerMovement>() != null) return true;
            if (other.GetComponent<PlayerMovement>() != null) return true;
            return false;
        }
    }
}
