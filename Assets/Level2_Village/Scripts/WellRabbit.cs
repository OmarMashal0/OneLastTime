using UnityEngine;

public sealed class WellRabbit : MonoBehaviour
{
    private int hitsRemaining = 2;

    private void Awake()
    {
        var hitbox = gameObject.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.size = new Vector2(0.85f, 0.65f);
        hitbox.offset = new Vector2(0f, 0.33f);
    }

    public void TakeHit()
    {
        if (!gameObject.activeSelf) return;
        if (--hitsRemaining > 0) return;
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        PixelSparks.Spawn(sprite, sprite.bounds.center);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
