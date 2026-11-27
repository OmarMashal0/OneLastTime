using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level3Bamboo
{
    // Runs before the default scripts (camera, torch, ChimePuzzle at -40), so when E is pressed this
    // script claims it first and the others see BusyWithLog / ReservesInteractionKey on the same frame.
    // Without it one E press could pick up a log and also zoom the camera or solve the chimes.
    [DefaultExecutionOrder(-50)]
    public class LogCollection : MonoBehaviour
    {
        public Transform player;
        public Collider2D playerCollider;
        public Camera gameplayCamera;
        public Canvas hudCanvas;
        public Image prompt;
        public TMP_Text counter;
        public CollectibleLog[] logs;

        public Sprite idle;
        public Sprite highlighted;
        public Sprite pressed;
        public Sprite released;
        [Min(0f)] public float interactionDistance = 1.25f;
        public LayerMask obstructionLayers = ~0;
        [Min(0f)] public float promptHeight = 1.38f;
        [Min(0.01f)] public float pressedSeconds = 0.12f;
        [Min(0.01f)] public float releasedSeconds = 0.12f;

        private readonly HashSet<string> collectedIds = new HashSet<string>();
        private readonly List<RaycastHit2D> obstructionHits = new List<RaycastHit2D>();
        private CollectibleLog candidate;
        private Vector2 feedbackPosition;
        private float feedbackElapsed;
        private float promptBob;
        private float readyElapsed;
        private bool showingFeedback;
        private bool consumedKey;

        public event System.Action<CollectibleLog, int, int> LogCollected;

        public int CollectedCount => collectedIds.Count;
        public IEnumerable<string> CollectedIds => collectedIds;

        // True while E is being used for a log (a log is in reach, or the collect feedback is playing).
        public bool BusyWithLog => consumedKey || showingFeedback || candidate != null;
        // Set every frame by other E interactions (the bamboo chimes) while Ryu is in their range.
        public bool OtherInteractionInRange { get; set; }
        // The camera checks this so E does not also zoom in while it is used for an interaction.
        public bool ReservesInteractionKey => BusyWithLog || OtherInteractionInRange;

        // Start is called before the first frame update
        void Start()
        {
            prompt.enabled = false;
            UpdateCounter();
        }

        // Update is called once per frame
        void Update()
        {
            ProcessInteraction(Input.GetKeyDown(KeyCode.E), Input.GetKey(KeyCode.E), Time.timeScale <= 0f);
        }

        private void ProcessInteraction(bool keyPressed, bool keyHeld, bool isPaused)
        {
            if (!keyHeld)
                consumedKey = false;

            if (isPaused)
            {
                candidate = null;
                prompt.enabled = false;
                return;
            }

            if (showingFeedback)
            {
                consumedKey |= keyPressed;
                feedbackElapsed += Time.deltaTime;
                if (feedbackElapsed < pressedSeconds + releasedSeconds)
                {
                    prompt.sprite = feedbackElapsed < pressedSeconds ? pressed : released;
                    prompt.enabled = true;
                    return;
                }

                showingFeedback = false;
                prompt.enabled = false;
                return;
            }

            CollectibleLog nearest = FindClosestLog();
            readyElapsed = nearest != candidate ? 0f : readyElapsed + Time.deltaTime;
            candidate = nearest;
            prompt.enabled = candidate != null;
            if (candidate == null)
                return;

            if (keyPressed)
            {
                Collect(candidate);
                return;
            }

            // Keep the ready animation quiet until E is pressed.
            float wave = Mathf.Sin(readyElapsed * Mathf.PI * 2f / 1.45f);
            promptBob = wave * 3.5f;
            prompt.sprite = wave >= 0.15f ? highlighted : idle;
            float brightness = Mathf.Lerp(0.78f, 1f, (wave + 1f) * 0.5f);
            prompt.color = new Color(brightness, brightness, brightness, 1f);
        }

        private void OnEnable()
        {
            Canvas.willRenderCanvases += PositionPrompt;
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= PositionPrompt;
            if (prompt != null)
                prompt.enabled = false;
        }

        private void PositionPrompt()
        {
            if (!prompt.enabled)
                return;

            Vector2 target = showingFeedback ? feedbackPosition : candidate.InteractionPoint;
            Vector3 screen = gameplayCamera.WorldToScreenPoint(target + Vector2.up * promptHeight);
            screen.y += promptBob;
            var promptParent = (RectTransform)prompt.transform.parent;
            var uiCamera = hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : hudCanvas.worldCamera;
            if (screen.z <= 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(promptParent, screen, uiCamera, out Vector2 point))
            {
                prompt.enabled = false;
                return;
            }

            Rect bounds = promptParent.rect;
            Vector2 halfSize = prompt.rectTransform.rect.size * 0.5f;
            point.x = Mathf.Clamp(point.x, bounds.xMin + halfSize.x, bounds.xMax - halfSize.x);
            point.y = Mathf.Clamp(point.y, bounds.yMin + halfSize.y, bounds.yMax - halfSize.y);
            prompt.rectTransform.anchoredPosition = point;
        }

        private CollectibleLog FindClosestLog()
        {
            Vector2 from = playerCollider.bounds.center;
            float bestDistance = interactionDistance * interactionDistance;
            CollectibleLog closest = null;
            foreach (CollectibleLog log in logs)
            {
                if (!log.isActiveAndEnabled || log.locked || collectedIds.Contains(log.logId))
                    continue;

                float distance = (log.InteractionPoint - from).sqrMagnitude;
                if (distance > bestDistance || !CanSeeLog(from, log.InteractionPoint))
                    continue;

                closest = log;
                bestDistance = distance;
            }

            return closest;
        }

        private bool CanSeeLog(Vector2 from, Vector2 to)
        {
            // Ignore the player and trigger volumes. Solid surfaces block collection.
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(obstructionLayers);
            Physics2D.Linecast(from, to, filter, obstructionHits);
            foreach (RaycastHit2D hit in obstructionHits)
            {
                if (!hit.collider.transform.IsChildOf(player))
                    return false;
            }

            return true;
        }

        private void Collect(CollectibleLog log)
        {
            if (!collectedIds.Add(log.logId))
                return;

            feedbackPosition = log.InteractionPoint;
            log.MarkCollected();
            candidate = null;

            consumedKey = true;
            feedbackElapsed = 0f;
            showingFeedback = true;
            prompt.sprite = pressed;
            prompt.color = Color.white;
            UpdateCounter();

            LogCollected?.Invoke(log, collectedIds.Count, logs.Length);
        }

        // Puts a collected log back (used when Ryu dies in the cave before reaching the surface with it).
        // Remove() is only true once, so the counter goes down exactly once.
        public void Uncollect(CollectibleLog log)
        {
            if (!collectedIds.Remove(log.logId))
                return;
            log.gameObject.SetActive(true);
            UpdateCounter();
        }

        private void UpdateCounter()
        {
            counter.text = $"Logs: {collectedIds.Count}/{logs.Length}";
        }
    }
}
