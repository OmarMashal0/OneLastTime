using System.Collections;
using Level3Bamboo;
using UnityEngine;

// The Level 3 title card ("LEVEL 3 — WHISPERS IN THE BAMBOO").
//
// Built from separate sprites: LevelNumber ("LEVEL 3"), Title, BambooLeft/BambooRight behind the top
// corners, and TitleParticles (leaves and flecks, hidden until the fade-out).
//
// Hidden at the start. The first time Ryu actually runs (arrow keys / A-D and he is moving), one coroutine
// plays the card once: it fades in with a tiny scale-up, holds so it can be read, then fades out while the
// leaves and flecks appear and drift away. The bamboo on both sides sways gently the whole time.
// While the card is up, soft (pre-blurred) copies of the distant background layers fade in over the
// sharp ones, so only the far background looks slightly out of focus. Everything else stays sharp.
//
// Ryu is never locked. Pause (Time.timeScale = 0) freezes the card because it counts Time.deltaTime.
// The HUD survives checkpoint respawns, so "played" stays true and the card never repeats.
public class LevelTitleFade : MonoBehaviour
{
    [Header("Start")]
    public BambooPlayerController player;
    public Rigidbody2D playerBody;
    [Tooltip("The title waits while the tea-stop conversation is running.")]
    public TeaRestConversation conversation;

    [Header("Title panel")]
    public CanvasGroup titleGroup;
    public RectTransform titlePanel;
    public RectTransform bambooLeft;
    public RectTransform bambooRight;
    [Tooltip("Holds the leaves and flecks. Alpha 0 until the title starts to fade out.")]
    public CanvasGroup titleParticles;
    public RectTransform[] driftingPieces;

    [Header("Background blur")]
    [Tooltip("The soft copies of the distant background layers (alpha 0 = sharp background).")]
    public SpriteRenderer[] softBackground;
    [Range(0f, 1f)] public float blurStrength = 0.85f;

    [Header("Timing (seconds)")]
    public float fadeInSeconds = 1.6f;
    public float holdSeconds = 3.2f;
    public float fadeOutSeconds = 2.4f;

    private bool played;
    private Vector2[] pieceStarts;

    // Start is called before the first frame update
    void Start()
    {
        titleGroup.alpha = 0f;
        titleGroup.interactable = false;
        titleGroup.blocksRaycasts = false;
        titleParticles.alpha = 0f;
        SetBlur(0f);

        pieceStarts = new Vector2[driftingPieces.Length];
        for (int i = 0; i < driftingPieces.Length; i++)
            pieceStarts[i] = driftingPieces[i].anchoredPosition;
    }

    // Update is called once per frame
    void Update()
    {
        if (played || Time.timeScale <= 0f || player.IsDead || conversation.Running)
            return;

        // "Starts running": the player is pressing left/right and Ryu is really moving.
        bool pressingRun = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f;
        if (pressingRun && Mathf.Abs(playerBody.velocity.x) > 0.2f)
        {
            played = true;
            StartCoroutine(PlayTitle());
        }
    }

    private IEnumerator PlayTitle()
    {
        float total = fadeInSeconds + holdSeconds + fadeOutSeconds;
        float time = 0f;
        while (time < total)
        {
            // If the tea-stop conversation somehow begins, skip straight to the fade-out.
            if (conversation.Running && time < fadeInSeconds + holdSeconds)
                time = fadeInSeconds + holdSeconds;

            float fadeIn = Mathf.SmoothStep(0f, 1f, time / fadeInSeconds);
            float fadeOut = Mathf.SmoothStep(0f, 1f, (time - fadeInSeconds - holdSeconds) / fadeOutSeconds);
            float visible = fadeIn * (1f - fadeOut);

            titleGroup.alpha = visible;
            titlePanel.localScale = Vector3.one * (0.97f + 0.03f * fadeIn + 0.01f * fadeOut);
            SetBlur(visible * blurStrength);
            // The leaves and flecks only show once the title starts to fade (the group fades them out too).
            titleParticles.alpha = Mathf.Clamp01(fadeOut * 5f);
            Sway(time);
            Drift(time, fadeOut);

            time += Time.deltaTime;   // 0 while paused, so everything waits
            yield return null;
        }

        titleGroup.alpha = 0f;
        titleParticles.alpha = 0f;
        SetBlur(0f);
    }

    // Gentle bamboo sway: a small rotation around the bottom of each stalk.
    private void Sway(float time)
    {
        bambooLeft.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.4f) * 2f);
        bambooRight.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.4f + 1.3f) * -2f);
    }

    // During the fade-out the leaves and flecks float up and to the right, turning slowly.
    private void Drift(float time, float fadeOut)
    {
        for (int i = 0; i < driftingPieces.Length; i++)
        {
            float wobble = Mathf.Sin(time * 2f + i) * 8f * fadeOut;
            Vector2 away = new Vector2(70f + 22f * i, 90f + 20f * (i % 3)) * fadeOut;
            driftingPieces[i].anchoredPosition = pieceStarts[i] + away + new Vector2(wobble, 0f);
            driftingPieces[i].localRotation = Quaternion.Euler(0f, 0f, -60f * fadeOut + i * 15f);
        }
    }

    private void SetBlur(float amount)
    {
        foreach (SpriteRenderer soft in softBackground)
        {
            Color color = soft.color;
            color.a = amount;
            soft.color = color;
        }
    }

    // If the title is switched off mid-way (or the scene unloads), put the background back to sharp.
    private void OnDisable()
    {
        StopAllCoroutines();
        if (softBackground != null)
            foreach (SpriteRenderer soft in softBackground)
                if (soft != null)
                    soft.color = new Color(soft.color.r, soft.color.g, soft.color.b, 0f);
        if (titleGroup != null)
            titleGroup.alpha = 0f;
        if (titleParticles != null)
            titleParticles.alpha = 0f;
    }
}
