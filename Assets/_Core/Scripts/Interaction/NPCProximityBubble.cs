using System.Collections;
using UnityEngine;

namespace OneLastTime.Interaction
{
    [RequireComponent(typeof(Collider2D))]
    public class NPCProximityBubble : MonoBehaviour
    {
        [Header("Speech Bubbles")]
        [SerializeField] private GameObject[] speechBubbles;

        [Header("Timing")]
        [SerializeField] private float delayBetweenBubbles = 0.8f;

        [Header("Trigger Settings")]
        [SerializeField] private bool stayActiveOnceTriggered = true;

        private Coroutine dialogueRoutine;
        private bool hasTriggered;

        void Start()
        {
            HideAllBubbles();

            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered && stayActiveOnceTriggered) return;

            if (IsPlayer(other))
            {
                hasTriggered = true;
                if (dialogueRoutine != null) StopCoroutine(dialogueRoutine);
                dialogueRoutine = StartCoroutine(ShowBubblesSequence());

                if (stayActiveOnceTriggered)
                {
                    var col = GetComponent<Collider2D>();
                    if (col != null) col.enabled = false;
                }
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
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

        IEnumerator ShowBubblesSequence()
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

        public void StopAndDisablePermanently()
        {
            if (dialogueRoutine != null)
            {
                StopCoroutine(dialogueRoutine);
                dialogueRoutine = null;
            }
            StopAllCoroutines();
            HideAllBubbles();
            hasTriggered = true;
            enabled = false;

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        public void HideAllBubbles()
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

        bool IsPlayer(Collider2D other)
        {
            return other.CompareTag("Player") || other.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
