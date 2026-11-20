using System.Collections.Generic;
using UnityEngine;
using OneLastTime.Player;

namespace OneLastTime.Interaction
{
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        [Header("Input Settings")]
        [Tooltip("Key to trigger the active interaction")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private static readonly List<InteractableTarget> registeredTargets = new List<InteractableTarget>();
        private Transform playerTransform;
        private PlayerMovement playerMovement;
        private InteractableTarget currentFocusedTarget;

        public static void RegisterTarget(InteractableTarget target)
        {
            if (target != null && !registeredTargets.Contains(target))
            {
                registeredTargets.Add(target);
            }
        }

        public static void UnregisterTarget(InteractableTarget target)
        {
            if (target != null)
            {
                registeredTargets.Remove(target);
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Ensure all interactables in the active scene are registered
            var existing = FindObjectsOfType<InteractableTarget>();
            foreach (var t in existing)
            {
                RegisterTarget(t);
            }
        }

        private void Start()
        {
            FindPlayer();
        }

        private void FindPlayer()
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement != null)
            {
                playerTransform = playerMovement.transform;
                return;
            }

            var p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player_Ryu");
            if (p != null)
            {
                playerMovement = p.GetComponentInChildren<PlayerMovement>();
                playerTransform = playerMovement != null ? playerMovement.transform : p.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null || playerMovement == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            // Global block condition:
            // Hide prompt during dialogue, scene transitions, or when player control is disabled
            bool isControlDisabled = (playerMovement != null && !playerMovement.canMove);
            if (isControlDisabled)
            {
                HideAllPrompts();
                currentFocusedTarget = null;
                return;
            }

            // Evaluate in-range status with 2D horizontal/vertical hysteresis
            Vector3 playerPos = playerTransform.position;
            InteractableTarget nearestTarget = null;
            float nearestDist = float.MaxValue;

            for (int i = registeredTargets.Count - 1; i >= 0; i--)
            {
                var target = registeredTargets[i];
                if (target == null)
                {
                    registeredTargets.RemoveAt(i);
                    continue;
                }

                if (!target.CanInteract())
                {
                    target.IsInRange = false;
                    if (target.PromptView != null) target.PromptView.Hide();
                    continue;
                }

                // Check in range with 2D bounds
                bool inRange = target.CheckIsInRange(playerPos);

                // If in range, select the nearest target
                if (inRange)
                {
                    float dist = target.GetInteractionDistance(playerPos);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestTarget = target;
                    }
                }
            }

            currentFocusedTarget = nearestTarget;

            // Show ONLY the nearest target's prompt, hide all others
            for (int i = 0; i < registeredTargets.Count; i++)
            {
                var target = registeredTargets[i];
                if (target == null || target.PromptView == null) continue;

                if (target == currentFocusedTarget)
                {
                    // Keep prompt anchored above target in world space
                    target.PromptView.transform.position = target.GetWorldPromptPosition();
                    target.PromptView.Show();
                }
                else
                {
                    target.PromptView.Hide();
                }
            }

            // Handle Interaction Input: single source of input handling
            if (Input.GetKeyDown(interactKey))
            {
                if (currentFocusedTarget != null && currentFocusedTarget.CanInteract())
                {
                    var targetToTrigger = currentFocusedTarget;
                    if (targetToTrigger.PromptView != null)
                    {
                        targetToTrigger.PromptView.Hide(true);
                    }
                    currentFocusedTarget = null;
                    targetToTrigger.Trigger();
                }
            }
        }

        private void HideAllPrompts()
        {
            for (int i = 0; i < registeredTargets.Count; i++)
            {
                var target = registeredTargets[i];
                if (target != null && target.PromptView != null)
                {
                    target.PromptView.Hide(true);
                }
            }
        }
    }
}
