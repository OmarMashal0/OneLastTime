using UnityEngine;
using OneLastTime.Player;

namespace OneLastTime.Systems
{
    public class Checkpoint : MonoBehaviour
    {
        [Header("Checkpoint Settings")]
        public bool isDefaultOnStart = false;
        public Vector3 spawnOffset = Vector3.zero;

        public Vector3 SpawnPosition => transform.position + spawnOffset;

        void Start()
        {
            if (isDefaultOnStart && CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.SetCheckpoint(this);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                if (CheckpointManager.Instance != null)
                {
                    CheckpointManager.Instance.SetCheckpoint(this);
                    Debug.Log("Activated Checkpoint " + gameObject.name + " at " + SpawnPosition);
                }
            }
        }

        bool IsPlayer(Collider2D other)
        {
            if (other == null) return false;
            if (other.CompareTag("Player")) return true;
            if (other.transform.root != null && other.transform.root.CompareTag("Player")) return true;
            if (other.GetComponentInParent<PlayerMovement>() != null) return true;
            if (other.GetComponentInParent<PlayerHealth>() != null) return true;
            return false;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(SpawnPosition, 0.4f);
        }
    }
}
