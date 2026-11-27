using UnityEngine;

public sealed class RabbitSpawnTrigger : MonoBehaviour
{
    [SerializeField] private WellRabbitSpawner rabbitWell;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && rabbitWell != null)
            rabbitWell.Begin(other);
    }
}
