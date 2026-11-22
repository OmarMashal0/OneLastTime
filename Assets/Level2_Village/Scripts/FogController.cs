using System.Collections;
using UnityEngine;

// New script: covers the whole camera view with a fading black fog.
// Add this component to the trigger object (or any object) and drag it
// into the "Fog" slot of TextAppearnce.
public class FogController : MonoBehaviour
{
    // Added: how dark the fog gets (0 = clear, 1 = fully black)
    [SerializeField] private float targetAlpha = 0.7f;
    // Added: how long the fog takes to fade in
    [SerializeField] private float fadeDuration = 1.5f;
    // Added: render order of the fog so it draws over the rest of the scene
    [SerializeField] private int sortingOrder = 100;

    private SpriteRenderer fogRenderer;
    private Coroutine fadeRoutine;

    // Added: call this to fade the black fog in
    public void FadeIn()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeTo(targetAlpha));
    }

    // Added: call this to fade the black fog back out
    public void FadeOut()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeTo(0f));
    }

    // Added: creates the full screen black quad in front of the camera if it does not exist yet
    private void CreateOverlay()
    {
        if (fogRenderer != null)
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        GameObject fogObject = new GameObject("Black Fog Overlay");
        fogObject.transform.SetParent(cam.transform, false);
        // Added: small forward offset so the fog sits in front of the scene
        fogObject.transform.localPosition = new Vector3(0f, 0f, 1f);

        fogRenderer = fogObject.AddComponent<SpriteRenderer>();
        fogRenderer.sprite = CreateSolidSprite();
        fogRenderer.color = new Color(0f, 0f, 0f, 0f);
        fogRenderer.sortingOrder = sortingOrder;

        // Added: scale the quad to cover the whole camera view (orthographic 2D camera)
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        fogObject.transform.localScale = new Vector3(width, height, 1f);
    }

    // Added: fades the fog towards the given alpha
    private IEnumerator FadeTo(float alpha)
    {
        CreateOverlay();

        if (fogRenderer == null)
        {
            yield break;
        }

        float startAlpha = fogRenderer.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, alpha, Mathf.Clamp01(elapsed / fadeDuration)));
            yield return null;
        }

        SetAlpha(alpha);
        fadeRoutine = null;
    }

    // Added: 1x1 white sprite used for the black fog quad
    private Sprite CreateSolidSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    // Added: helper that sets the alpha of the fog
    private void SetAlpha(float alpha)
    {
        if (fogRenderer == null)
        {
            return;
        }

        Color color = fogRenderer.color;
        color.a = alpha;
        fogRenderer.color = color;
    }
}
