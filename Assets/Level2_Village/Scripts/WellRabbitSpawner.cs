using System.Collections;
using UnityEngine;

public sealed class WellRabbitSpawner : MonoBehaviour
{
    [SerializeField] private Collider2D ground;
    // Ready, crouch, push-off, extension, descent and landing.
    [SerializeField] private Sprite[] hopFrames;
    [SerializeField] private Sprite throwPose;
    [SerializeField] private Sprite carrot;
    [SerializeField, Min(0.1f)] private float spawnInterval = 5f;
    [SerializeField, Min(0.5f)] private float throwInterval = 4.5f;

    private Collider2D target;
    private int sortingLayer;

    public void Begin(Collider2D player)
    {
        if (target != null) return;
        target = player;
        sortingLayer = GetComponent<SpriteRenderer>().sortingLayerID;
        StartCoroutine(SpawnRabbits());
    }

    private IEnumerator SpawnRabbits()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (target != null)
        {
            yield return wait;
            if (target != null)
            {
                var rabbit = CreateSprite("Well rabbit", hopFrames[2], transform.position, 3);
                var enemy = rabbit.gameObject.AddComponent<WellRabbit>();
                enemy.StartCoroutine(RunRabbit(rabbit));
            }
        }
    }

    private SpriteRenderer CreateSprite(string objectName, Sprite sprite, Vector2 position, int order)
    {
        var item = new GameObject(objectName);
        item.transform.position = position;
        item.transform.SetParent(transform, true);
        var renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = order;
        return renderer;
    }

    private IEnumerator RunRabbit(SpriteRenderer rabbit)
    {
        Vector2 rim = transform.position;
        float floor = ground.bounds.max.y;
        float side = target.bounds.center.x < rim.x ? -1f : 1f;
        rabbit.flipX = side < 0f;
        float x = rim.x + side * 1.1f;
        float nextThrow = Time.time + throwInterval;
        float emerged = Time.time;
        while (Time.time - emerged < 0.55f)
        {
            float t = (Time.time - emerged) / 0.55f;
            rabbit.sprite = hopFrames[t < 0.18f ? 2 : t < 0.58f ? 3 : 4];
            rabbit.transform.position = Vector2.Lerp(rim, new Vector2(x, floor), t)
                + Vector2.up * (3.6f * t * (1f - t));
            yield return null;
        }

        rabbit.transform.position = new Vector2(x, floor);
        rabbit.sprite = hopFrames[5];
        yield return new WaitForSeconds(0.08f);

        const float strideDuration = 0.46f;
        const float windup = 0.32f;
        // A rabbit retires after six throws so an unattended well stays bounded.
        for (int shot = 0; shot < 6 && target != null; shot++)
        {
            while (Time.time < nextThrow && target != null)
            {
                float dx = target.bounds.center.x - x;
                if (Mathf.Abs(dx) > 0.05f) side = Mathf.Sign(dx);
                rabbit.flipX = side < 0f;
                float timeToThrow = nextThrow - Time.time;
                if (Mathf.Abs(dx) > 2.45f && timeToThrow >= strideDuration + windup)
                {
                    float landingX = Mathf.MoveTowards(x, target.bounds.center.x - side * 2.4f, 1.65f);
                    yield return Hop(rabbit, x, landingX, floor, strideDuration);
                    x = landingX;
                    continue;
                }
                rabbit.sprite = timeToThrow <= windup ? throwPose : hopFrames[0];
                yield return null;
            }
            if (target == null) break;
            Vector2 release = new Vector2(x + side * 0.38f, floor + 0.63f);
            ThrowCarrot(release, (Vector2)target.bounds.center - release);
            rabbit.sprite = hopFrames[1];
            nextThrow += throwInterval;
            yield return new WaitForSeconds(0.18f);
        }
        Destroy(rabbit.gameObject);
    }

    private IEnumerator Hop(SpriteRenderer rabbit, float startX, float endX, float floor, float duration)
    {
        float start = Time.time;
        float height = Mathf.Min(0.38f, Mathf.Abs(endX - startX) * 0.35f);
        while (Time.time - start < duration)
        {
            float phase = (Time.time - start) / duration;
            // Travel only between push-off and landing, keeping grounded paws planted.
            float flight = Mathf.Clamp01((phase - 0.24f) / 0.58f);
            rabbit.transform.position = new Vector2(Mathf.Lerp(startX, endX, flight),
                floor + 4f * height * flight * (1f - flight));
            int frame = Mathf.Min((int)(phase * 6f), 5);
            rabbit.sprite = hopFrames[frame];
            yield return null;
        }
        rabbit.transform.position = new Vector2(endX, floor);
        rabbit.sprite = hopFrames[5];
    }

    private void ThrowCarrot(Vector2 origin, Vector2 aim)
    {
        var projectile = CreateSprite("Thrown carrot", carrot, origin, 5);
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
        // Keep the physics body independent of the well's decorative transform.
        projectile.transform.SetParent(null, true);
        var hitbox = projectile.gameObject.AddComponent<BoxCollider2D>();
        hitbox.isTrigger = true;
        hitbox.size = carrot.bounds.size * 0.84f;
        var body = projectile.gameObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.velocity = aim.normalized * 8f;
        projectile.gameObject.AddComponent<RabbitCarrot>();
        Destroy(projectile.gameObject, 6f);
    }
}
