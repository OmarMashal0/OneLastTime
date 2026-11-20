using UnityEngine;

namespace OneLastTime.Systems
{
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("If true, automatically becomes the active checkpoint upon scene start")]
        [SerializeField] private bool isDefaultOnStart = true;

        public Vector3 SpawnPosition => transform.position;

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
    }
}

