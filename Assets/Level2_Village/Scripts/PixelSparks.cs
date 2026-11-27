using UnityEngine;

public sealed class PixelSparks : MonoBehaviour
{
    private static Sprite sparkle;
    private readonly SpriteRenderer[] flecks = new SpriteRenderer[10];
    private float elapsed;

    public static void Spawn(SpriteRenderer source, Vector3 origin)
    {
        var root = new GameObject("Rabbit puff");
        root.transform.position = origin;
        var effect = root.AddComponent<PixelSparks>();
        if (sparkle == null)
        {
            // One stepped, translucent halo around a four-point pixel star.
            var tex = new Texture2D(7, 7, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < 7; y++)
                for (int x = 0; x < 7; x++)
                {
                    int dx = Mathf.Abs(x - 3), dy = Mathf.Abs(y - 3);
                    float a = (dx == 0 || dy == 0) && dx + dy < 3 ? 1f : dx + dy <= 3 ? 0.25f : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply(false, true);
            sparkle = Sprite.Create(tex, new Rect(0, 0, 7, 7), Vector2.one * 0.5f, 32f);
        }
        for (int i = 0; i < effect.flecks.Length; i++)
        {
            var fleck = new GameObject("Spark").AddComponent<SpriteRenderer>();
            fleck.transform.SetParent(root.transform, false);
            fleck.sprite = sparkle;
            fleck.sortingLayerID = source.sortingLayerID;
            fleck.sortingOrder = source.sortingOrder + 2;
            effect.flecks[i] = fleck;
        }
        effect.Animate();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= 0.4f) Destroy(gameObject);
        else Animate();
    }

    private void Animate()
    {
        for (int i = 0; i < flecks.Length; i++)
        {
            float angle = i * 2.4f;
            float t = elapsed / 0.4f;
            Vector2 position = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Sqrt(t) * (0.5f + i % 3 * 0.15f);
            position.y += 0.28f * t;
            Color color = new Color(1f, 1f, 1f, 1f - t);
            flecks[i].transform.localPosition = position;
            flecks[i].transform.localScale = Vector3.one * (0.55f + color.a * 0.65f);
            flecks[i].color = color;
        }
    }
}
