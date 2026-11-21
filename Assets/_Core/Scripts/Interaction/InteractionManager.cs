using System.Collections.Generic;
using UnityEngine;
using OneLastTime.Player;

namespace OneLastTime.Interaction
{
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        [Header("Input")]
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

        void Awake()
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

            var existing = FindObjectsOfType<InteractableTarget>();
            foreach (var t in existing)
            {
                RegisterTarget(t);
            }
        }

        void Start()
        {
            FindPlayer();
        }

        void FindPlayer()
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

        void Update()
        {
            if (playerTransform == null || playerMovement == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            // hide prompts if player cannot move
            bool isControlDisabled = (playerMovement != null && !playerMovement.canMove);
            if (isControlDisabled)
            {
                HideAllPrompts();
                currentFocusedTarget = null;
                return;
            }

            // find nearest valid interactable
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

                bool inRange = target.CheckIsInRange(playerPos);

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

            // show prompt for closest target only
            for (int i = 0; i < registeredTargets.Count; i++)
            {
                var target = registeredTargets[i];
                if (target == null || target.PromptView == null) continue;

                if (target == currentFocusedTarget)
                {
                    target.PromptView.transform.position = target.GetWorldPromptPosition();
                    target.PromptView.Show();
                }
                else
                {
                    target.PromptView.Hide();
                }
            }

            // trigger interaction when key pressed
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

        void HideAllPrompts()
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
