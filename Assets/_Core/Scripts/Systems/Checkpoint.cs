using UnityEngine;

namespace OneLastTime.Systems
{
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("If true, automatically becomes the active checkpoint upon scene start")]
        [SerializeField] private bool isDefaultOnStart = false;

        [Tooltip("Optional spawn offset from this transform (e.g. to spawn on the ground if the object is floating)")]
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;

        public Vector3 SpawnPosition => transform.position + spawnOffset;

        private void Start()
        {
            if (isDefaultOnStart && CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.SetCheckpoint(this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                if (CheckpointManager.Instance != null)
                {
                    CheckpointManager.Instance.SetCheckpoint(this);
                    Debug.Log($"[Checkpoint] Activated checkpoint '{gameObject.name}' at {SpawnPosition}");
                }
            }
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other == null) return false;
            if (other.CompareTag("Player")) return true;
            if (other.transform.root != null && other.transform.root.CompareTag("Player")) return true;
            if (other.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (other.name.IndexOf("ryu", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (other.transform.root != null && (other.transform.root.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0 || other.transform.root.name.IndexOf("ryu", System.StringComparison.OrdinalIgnoreCase) >= 0)) return true;
            if (other.GetComponentInParent<OneLastTime.Player.PlayerMovement>() != null) return true;
            if (other.GetComponentInParent<OneLastTime.Player.PlayerHealth>() != null) return true;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(SpawnPosition, 0.4f);
        }
    }
}

