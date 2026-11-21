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
            if (other.CompareTag("Player") || other.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (CheckpointManager.Instance != null)
                {
                    CheckpointManager.Instance.SetCheckpoint(this);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(SpawnPosition, 0.4f);
        }
    }
}

