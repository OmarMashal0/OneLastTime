using UnityEngine;

public sealed class RabbitCarrot : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var player = other.GetComponentInParent<DummyPlayer>();
        if (player == null) return;
        player.HitByCarrot();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
