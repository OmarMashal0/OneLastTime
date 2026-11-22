using System.Collections;
using UnityEngine;

public class TextAppearnce : MonoBehaviour
{
    // Added: tag of the object that can activate this trigger (your player)
    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";

    // Added: the PNG object that fades in and later fades out
    [Header("Floating Text")]
    [SerializeField] private GameObject textImage;
    // Added: how long the PNG takes to fade in
    [SerializeField] private float textFadeDuration = 1f;
    // Added: how long the PNG stays visible before it fades out (the 3 seconds)
    [SerializeField] private float textVisibleDuration = 1f;
    // Added: how long the PNG takes to fade out
    [SerializeField] private float textFadeOutDuration = 1f;
    // Added: render order of the PNG so it stays above the black fog
    [SerializeField] private int textSortingOrder = 200;

    // Added: the separate fog script (add FogController to an object and drag it here)
    [Header("Black Fog")]
    [SerializeField] private FogController fog;

    // Added: your particle system, emitted when the text disappears
    [Header("Dissolve Particles")]
    [SerializeField] private ParticleSystem dissolveParticles;
    // Added: how many particles burst out when the text fades out
    [SerializeField] private int particleBurstCount = 9999;

    // Added: makes sure the trigger only runs once
    private bool hasTriggered;

    // Added: detects the player entering the BoxCollider2D (Is Trigger = true)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered || !other.CompareTag(playerTag))
        {
            return;
        }

        hasTriggered = true;
        StartCoroutine(PlaySequence());
    }

    // Added: fade in the text, hold it, then fade it out into particles
    private IEnumerator PlaySequence()
    {
        // Added: start the black fog from the separate FogController script
        if (fog != null)
        {
            fog.FadeIn();
        }

        // Added: keep the text drawn above the fog
        SetSortingOrder();

        yield return FadeText(0f, 1f, textFadeDuration);

        // Added: wait before the text disappears
        yield return new WaitForSeconds(textVisibleDuration);

        // Added: fire the particle burst as the text starts to fade away
        if (dissolveParticles != null)
        {
            if (!dissolveParticles.gameObject.activeSelf)
            {
                dissolveParticles.gameObject.SetActive(true);
            }

            dissolveParticles.Emit(particleBurstCount);
        }

        yield return FadeText(1f, 0f, textFadeOutDuration);

        // Added: hide the PNG once it is fully transparent
        if (textImage != null)
        {
            textImage.SetActive(false);
        }
    }

    // Added: fades every sprite of the PNG object between two alpha values
    private IEnumerator FadeText(float fromAlpha, float toAlpha, float duration)
    {
        if (textImage == null)
        {
            yield break;
        }

        SpriteRenderer[] renderers = textImage.GetComponentsInChildren<SpriteRenderer>(true);
        textImage.SetActive(true);
        SetAlpha(renderers, fromAlpha);

        if (duration <= 0f)
        {
            SetAlpha(renderers, toAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration));
            SetAlpha(renderers, alpha);
            yield return null;
        }

        SetAlpha(renderers, toAlpha);
    }

    // Added: draws the PNG above the black fog
    private void SetSortingOrder()
    {
        if (textImage == null)
        {
            return;
        }

        foreach (SpriteRenderer renderer in textImage.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingOrder = textSortingOrder;
        }
    }

    // Added: helper that sets the alpha of a group of sprites
    private void SetAlpha(SpriteRenderer[] renderers, float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Color color = renderers[i].color;
            color.a = alpha;
            renderers[i].color = color;
        }
    }
}
