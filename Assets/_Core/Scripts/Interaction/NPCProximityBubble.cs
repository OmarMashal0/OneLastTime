using System.Collections;
using UnityEngine;

namespace OneLastTime.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class NPCProximityBubble : MonoBehaviour
    {
        [Header("Speech Bubbles")]
        [Tooltip("Add your animated speech bubbles here (e.g. bubble_1, bubble_2)")]
        [SerializeField] private GameObject[] speechBubbles;

        [Header("Timing")]
        [Tooltip("Seconds to wait before showing the next bubble. Set to 0 to show all at once.")]
        [SerializeField] private float delayBetweenBubbles = 0.8f;

        [Header("Trigger Behavior")]
        [Tooltip("If checked, bubbles stay visible permanently once triggered, even if you walk away.")]
        [SerializeField] private bool stayActiveOnceTriggered = true;

        private Coroutine dialogueRoutine;
        private bool hasTriggered;

        private void Start()
        {
            // Ensure all speech bubbles start hidden
            HideAllBubbles();

            // Ensure this detection collider acts as a trigger
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered && stayActiveOnceTriggered) return;

            if (IsPlayer(other))
            {
                hasTriggered = true;
                if (dialogueRoutine != null) StopCoroutine(dialogueRoutine);
                dialogueRoutine = StartCoroutine(ShowBubblesSequence());

                // Disable trigger collider once activated so it doesn't re-trigger
                if (stayActiveOnceTriggered)
                {
                    var col = GetComponent<Collider2D>();
                    if (col != null) col.enabled = false;
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // If set to stay active permanently, don't hide when leaving
            if (stayActiveOnceTriggered) return;

            if (IsPlayer(other))
            {
                if (dialogueRoutine != null)
                {
                    StopCoroutine(dialogueRoutine);
                    dialogueRoutine = null;
                }
                HideAllBubbles();
            }
        }

        private IEnumerator ShowBubblesSequence()
        {
            if (speechBubbles == null) yield break;

            for (int i = 0; i < speechBubbles.Length; i++)
            {
                if (speechBubbles[i] != null)
                {
                    speechBubbles[i].SetActive(true);
                }

                if (delayBetweenBubbles > 0f && i < speechBubbles.Length - 1)
                {
                    yield return new WaitForSeconds(delayBetweenBubbles);
                }
            }
        }

        private void HideAllBubbles()
        {
            if (speechBubbles == null) return;
            foreach (var bubble in speechBubbles)
            {
                if (bubble != null)
                {
                    bubble.SetActive(false);
                }
            }
        }

        private bool IsPlayer(Collider2D other)
        {
            return other.CompareTag("Player") || other.name.Contains("Player");
        }
    }
}
