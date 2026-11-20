using System.Collections;
using UnityEngine;

namespace OneLastTime.Interaction
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class InteractionPromptView : MonoBehaviour
    {
        [Header("Fade Settings")]
        [Tooltip("Seconds to smoothly fade in/out")]
        [SerializeField] private float fadeDuration = 0.08f;

        private SpriteRenderer spriteRenderer;
        private Coroutine fadeRoutine;
        private bool isVisible = false;
        private bool hasInitializedAtStart = false;
        private float targetAlpha = 0f;

        public bool IsVisible => isVisible;

        private void Awake()
        {
            Init();
        }

        private void Start()
        {
            Init();
            if (!hasInitializedAtStart && !isVisible)
            {
                hasInitializedAtStart = true;
                SetAlpha(0f);
                gameObject.SetActive(false);
            }
        }

        private void Init()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void Show()
        {
            Init();
            if (!gameObject.activeSelf)
            {
                SetAlpha(0f);
                gameObject.SetActive(true);
            }
            isVisible = true;

            // If already fading to or reached 1f, do not restart coroutine every frame
            if (targetAlpha == 1f && (fadeRoutine != null || (spriteRenderer != null && spriteRenderer.color.a >= 0.99f)))
            {
                return;
            }

            targetAlpha = 1f;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeTo(1f));
        }

        public void Hide(bool immediate = false)
        {
            Init();
            if (!isVisible && !gameObject.activeSelf) return;
            isVisible = false;

            if (immediate)
            {
                if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                fadeRoutine = null;
                targetAlpha = 0f;
                SetAlpha(0f);
                gameObject.SetActive(false);
                return;
            }

            // If already fading to 0f, do not restart
            if (targetAlpha == 0f && fadeRoutine != null)
            {
                return;
            }

            targetAlpha = 0f;
            if (gameObject.activeInHierarchy)
            {
                if (fadeRoutine != null) StopCoroutine(fadeRoutine);
                fadeRoutine = StartCoroutine(FadeTo(0f, () => gameObject.SetActive(false)));
            }
            else
            {
                SetAlpha(0f);
            }
        }

        private IEnumerator FadeTo(float targetAlpha, System.Action onComplete = null)
        {
            Init();
            if (spriteRenderer == null) yield break;

            float startAlpha = spriteRenderer.color.a;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                SetAlpha(a);
                yield return null;
            }

            SetAlpha(targetAlpha);
            fadeRoutine = null;
            onComplete?.Invoke();
        }

        private void SetAlpha(float a)
        {
            Init();
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = a;
                spriteRenderer.color = c;
            }
        }
    }
}
