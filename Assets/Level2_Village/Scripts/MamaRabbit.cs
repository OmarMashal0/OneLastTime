using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class MamaRabbit : MonoBehaviour
{
    private const int BossHealth = 15;
    private const int PlayerHealth = 4;
    private static readonly Color HitTint = new Color32(211, 92, 108, 255);
    private static readonly Color WindupTint = new Color32(143, 100, 126, 255);

    [SerializeField] private DummyPlayer player;
    [SerializeField] private Sprite[] rabbitFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField, Min(0.1f)] private float walkSpeed = 3.8f;
    [SerializeField, Min(0.1f)] private float strideLength = 2.1f;
    [SerializeField] private AudioSource battleTheme;
    [SerializeField] private BoxCollider2D swipeHitbox;
    [SerializeField] private BoxCollider2D floor;
    [SerializeField] private BoxCollider2D leftWall;
    [SerializeField] private BoxCollider2D rightWall;
    [SerializeField] private RectTransform bossFill;
    [SerializeField] private Text title;

    private SpriteRenderer rabbit;
    private BoxCollider2D rabbitCollider;
    private BoxCollider2D playerCollider;
    private Sprite idleSprite;
    private Color idleColor;
    private Vector3 swipePosition;
    private int bossHits;
    private int playerHits;
    private bool finished;
    private bool windingUp;
    private float flashUntil;
    private float distanceWalked;
    private Coroutine themeFadeIn;

    private IEnumerator Start()
    {
        rabbit = GetComponent<SpriteRenderer>();
        rabbitCollider = GetComponent<BoxCollider2D>();
        playerCollider = player.GetComponent<BoxCollider2D>();
        idleSprite = rabbit.sprite;
        idleColor = rabbit.color;
        swipePosition = swipeHitbox.transform.localPosition;
        float themeVolume = battleTheme.volume;
        battleTheme.spatialBlend = 0f;
        battleTheme.volume = 0f;
        battleTheme.Play();
        themeFadeIn = StartCoroutine(FadeTheme(themeVolume, 1.5f));

        while (SceneFader.Instance.IsTransitioning) yield return null;
        yield return new WaitForSeconds(1f);

        while (!finished)
        {
            yield return WalkTowardPlayer();
            if (finished) yield break;
            float direction = player.transform.position.x < transform.position.x ? -1f : 1f;
            rabbit.flipX = direction < 0f;
            swipeHitbox.transform.localPosition = new Vector3(swipePosition.x * direction, swipePosition.y, swipePosition.z);
            rabbit.sprite = rabbitFrames[1];
            windingUp = true;
            yield return new WaitForSeconds(0.20f);
            if (finished) yield break;
            rabbit.sprite = rabbitFrames[2];
            yield return new WaitForSeconds(0.38f);
            windingUp = false;
            if (finished) yield break;

            rabbit.sprite = rabbitFrames[3];
            yield return new WaitForSeconds(0.05f);
            if (finished) yield break;
            rabbit.sprite = rabbitFrames[4];
            swipeHitbox.enabled = true;
            bool connected = false;
            float swipeStart = Time.time;
            while (Time.time - swipeStart < 0.22f && !finished)
            {
                if (Time.time - swipeStart >= 0.13f) rabbit.sprite = rabbitFrames[5];
                if (!connected && swipeHitbox.bounds.Intersects(playerCollider.bounds))
                {
                    connected = true;
                    HitPlayer();
                }
                yield return null;
            }
            swipeHitbox.enabled = false;
            if (finished) yield break;
            rabbit.sprite = rabbitFrames[6];
            yield return new WaitForSeconds(0.24f);
            if (finished) yield break;
            rabbit.sprite = rabbitFrames[7];
            yield return new WaitForSeconds(0.51f);
            rabbit.sprite = idleSprite;
        }
    }

    private IEnumerator WalkTowardPlayer()
    {
        const float swipeDistance = 2.9f;
        Vector3 position = transform.position;
        position.y += floor.bounds.max.y - rabbitCollider.bounds.min.y;
        transform.position = position;
        float halfWidth = rabbitCollider.bounds.extents.x;
        float leftEdge = leftWall.bounds.max.x + halfWidth;
        float rightEdge = rightWall.bounds.min.x - halfWidth;
        while (!finished)
        {
            float playerX = player.transform.position.x;
            float offset = playerX - position.x;
            if (Mathf.Abs(offset) <= swipeDistance + 0.04f) yield break;

            float targetX = Mathf.Clamp(playerX - Mathf.Sign(offset) * swipeDistance, leftEdge, rightEdge);
            float nextX = Mathf.MoveTowards(position.x, targetX, walkSpeed * Time.deltaTime);
            float step = nextX - position.x;
            position.x = nextX;
            transform.position = position;
            if (step != 0f)
            {
                rabbit.flipX = step < 0f;
                distanceWalked = Mathf.Repeat(distanceWalked + Mathf.Abs(step), strideLength);
                int frame = Mathf.Min(walkFrames.Length - 1, (int)(distanceWalked / strideLength * walkFrames.Length));
                rabbit.sprite = walkFrames[frame];
            }
            yield return null;
        }
    }

    public void TakeHit()
    {
        if (finished) return;
        bossHits++;
        Vector3 barScale = bossFill.localScale;
        barScale.x = (BossHealth - bossHits) / (float)BossHealth;
        bossFill.localScale = barScale;
        flashUntil = Time.time + 0.14f;
        if (bossHits < BossHealth) return;
        WellBossEntrance.LockAfterVictory();
        swipeHitbox.enabled = false;
        StartCoroutine(FadeDefeatedRabbit());
        rabbitCollider.enabled = false;
        Finish("MAMA RABBIT DEFEATED");
    }

    private IEnumerator FadeDefeatedRabbit()
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.45f)
        {
            rabbit.color = new Color(idleColor.r, idleColor.g, idleColor.b, idleColor.a * (1f - t));
            yield return null;
        }
        rabbit.enabled = false;
    }

    private void HitPlayer()
    {
        player.HitByCarrot();
        playerHits++;
        if (playerHits < PlayerHealth) return;
        player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        player.enabled = false;
        Finish("YOU FELL IN THE CISTERN");
    }

    private void Finish(string message)
    {
        finished = true;
        title.text = message;
        if (themeFadeIn != null) StopCoroutine(themeFadeIn);
        StartCoroutine(ReturnToVillage());
    }

    private IEnumerator ReturnToVillage()
    {
        yield return FadeTheme(0f, 1.5f);
        battleTheme.Stop();
        SceneFader.Instance.FadeToScene("L2_A_VillageChase");
    }

    private IEnumerator FadeTheme(float target, float duration)
    {
        float initial = battleTheme.volume;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            battleTheme.volume = Mathf.SmoothStep(initial, target, elapsed / duration);
            yield return null;
        }
        battleTheme.volume = target;
    }

    private void LateUpdate()
    {
        if (rabbit == null || finished) return;
        rabbit.color = Time.time < flashUntil ? HitTint : windingUp ? WindupTint : idleColor;
    }

}
