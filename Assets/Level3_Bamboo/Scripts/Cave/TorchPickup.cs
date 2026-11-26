using UnityEngine;

namespace Level3Bamboo
{
    // The torch on its wall bracket beside the cave landing. Shows the E prompt when Ryu is close.
    // Pressing E hides the wall torch and switches on the torch in Ryu's hand (PlayerTorch).
    // Runs after ChimePuzzle so its "E is in use here" message to LogCollection is not overwritten.
    [DefaultExecutionOrder(10)]
    public class TorchPickup : MonoBehaviour
    {
        public BambooPlayerController player;
        public Collider2D playerCollider;
        public LogCollection logCollection;
        [Tooltip("The burning torch, its glow and its light mask on the bracket.")]
        public GameObject wallTorch;
        [Tooltip("The same E key sprite the chimes use.")]
        public SpriteRenderer keyPrompt;
        public PlayerTorch handTorch;
        public AudioSource sound;
        public AudioClip pickupClip;
        public float interactionDistance = 1.3f;
        public Vector2 promptOffset = new Vector2(0f, 0.9f);

        public bool PickedUp { get; private set; }

        void Start()
        {
            keyPrompt.enabled = false;
            handTorch.gameObject.SetActive(false);
        }

        // Torch back on the bracket and nothing left in Ryu's hand (no flame, no glow).
        // Called by CaveZone on death and when Ryu climbs out of the cave.
        public void ResetTorch()
        {
            PickedUp = false;
            keyPrompt.enabled = false;
            wallTorch.SetActive(true);
            handTorch.SetLit(false);
            handTorch.gameObject.SetActive(false);
        }

        void Update()
        {
            if (PickedUp)
                return;

            bool near = Time.timeScale > 0f && !player.IsDead && !logCollection.BusyWithLog
                && Vector2.Distance(playerCollider.bounds.center, transform.position) <= interactionDistance;
            keyPrompt.enabled = near;
            if (!near)
                return;

            // Stops the camera's E zoom while E is used here.
            logCollection.OtherInteractionInRange = true;
            keyPrompt.transform.position = transform.position + (Vector3)promptOffset;

            if (Input.GetKeyDown(KeyCode.E))
            {
                PickedUp = true;
                keyPrompt.enabled = false;
                wallTorch.SetActive(false);
                handTorch.gameObject.SetActive(true);
                handTorch.SetLit(true);
                handTorch.ShowHint();
                if (sound != null && pickupClip != null)
                    sound.PlayOneShot(pickupClip, 0.6f);
            }
        }
    }
}
