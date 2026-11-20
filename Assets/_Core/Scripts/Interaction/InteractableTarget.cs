using System;
using UnityEngine;
using UnityEngine.Events;

namespace OneLastTime.Interaction
{
    public class InteractableTarget : MonoBehaviour
    {
        [Header("Interaction Ranges (Hysteresis)")]
        [Tooltip("Horizontal distance at which the prompt appears")]
        [SerializeField] private float entryRangeX = 2.4f;

        [Tooltip("Vertical distance at which the prompt appears")]
        [SerializeField] private float entryRangeY = 3.2f;

        [Tooltip("Horizontal distance at which the prompt disappears")]
        [SerializeField] private float exitRangeX = 3.2f;

        [Tooltip("Vertical distance at which the prompt disappears")]
        [SerializeField] private float exitRangeY = 4.2f;

        [Header("Prompt Positioning")]
        [Tooltip("Offset in world space anchored above this target")]
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.8f, 0f);

        [Header("Prompt Instance")]
        [SerializeField] private InteractionPromptView promptView;

        [Header("Events")]
        [SerializeField] private UnityEvent onInteract;

        // Custom condition delegate (e.g. from NPCConfrontation or DoorTeleport)
        public Func<bool> canInteractCheck;

        public float EntryRangeX { get => entryRangeX; set => entryRangeX = value; }
        public float EntryRangeY { get => entryRangeY; set => entryRangeY = value; }
        public float ExitRangeX { get => exitRangeX; set => exitRangeX = value; }
        public float ExitRangeY { get => exitRangeY; set => exitRangeY = value; }

        public Vector3 PromptOffset { get => promptOffset; set => promptOffset = value; }
        public InteractionPromptView PromptView { get => promptView; set => promptView = value; }
        public UnityEvent OnInteract => onInteract;
        public bool IsInRange { get; set; }

        private void Awake()
        {
            // Automatically capture the exact position placed by the designer in the editor
            if (promptView != null)
            {
                promptOffset = promptView.transform.position - transform.position;
            }
        }

        private void OnValidate()
        {
            // Sync offset in editor as the user moves the prompt
            if (!Application.isPlaying && promptView != null)
            {
                promptOffset = promptView.transform.position - transform.position;
            }
        }

        private void OnEnable()
        {
            InteractionManager.RegisterTarget(this);
        }

        private void OnDisable()
        {
            InteractionManager.UnregisterTarget(this);
            if (promptView != null)
            {
                promptView.Hide(true);
            }
        }

        public Vector3 GetWorldPromptPosition()
        {
            return transform.position + promptOffset;
        }

        public bool CheckIsInRange(Vector3 playerPos)
        {
            float dx = Mathf.Abs(playerPos.x - transform.position.x);
            float dy = Mathf.Abs(playerPos.y - transform.position.y);

            if (IsInRange)
            {
                if (dx > exitRangeX || dy > exitRangeY)
                {
                    IsInRange = false;
                }
            }
            else
            {
                if (dx <= entryRangeX && dy <= entryRangeY)
                {
                    IsInRange = true;
                }
            }

            return IsInRange;
        }

        public float GetInteractionDistance(Vector3 playerPos)
        {
            float dx = Mathf.Abs(playerPos.x - transform.position.x);
            float dy = Mathf.Abs(playerPos.y - transform.position.y);
            return dx + dy * 0.3f;
        }

        public bool CanInteract()
        {
            if (!enabled || !gameObject.activeInHierarchy) return false;
            if (canInteractCheck != null)
            {
                return canInteractCheck();
            }
            return true;
        }

        public void Trigger()
        {
            onInteract?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(entryRangeX * 2f, entryRangeY * 2f, 0.1f));
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(exitRangeX * 2f, exitRangeY * 2f, 0.1f));

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(GetWorldPromptPosition(), 0.1f);
        }
    }
}
