using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OneLastTime.UI
{
    public class LevelTitleCard : MonoBehaviour
    {
        public static LevelTitleCard Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform containerRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Image backdropImage;

        [Header("Audio")]
        [SerializeField] private AudioClip soundEffect;
        [Range(0f, 1f)]
        [SerializeField] private float soundVolume = 0.75f;

        [Header("Animation Timings")]
        [SerializeField] private float fadeInDuration = 1.0f;
        [SerializeField] private float displayDuration = 3.5f;
        [SerializeField] private float fadeOutDuration = 1.2f;

        [Header("Default Text")]
        [TextArea(2, 4)]
        [SerializeField] private string defaultTitle = "Level 1 | The Return of The Greatest Samurai";

        private Coroutine activeRoutine;
        private AudioSource audioSource;
        private Vector2 originalPosition;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (containerRect == null) containerRect = GetComponent<RectTransform>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }

            if (containerRect != null)
            {
                originalPosition = containerRect.anchoredPosition;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        public void PlayTitle(string customText = null)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(TitleRoutine(string.IsNullOrEmpty(customText) ? defaultTitle : customText));
        }

        IEnumerator TitleRoutine(string text)
        {
            if (titleText != null)
            {
                if (text.Contains("|"))
                {
                    string[] parts = text.Split('|');
                    string chapter = parts[0].Trim();
                    string name = parts[1].Trim();
                    titleText.text = $"<size=65%><color=#E5A823><cspace=0.35em><b>— {chapter.ToUpper()} —</b></cspace></color></size>\n" +
                                     $"<size=110%><color=#FFFFFF><cspace=0.12em><b>{name}</b></cspace></color></size>\n" +
                                     $"<size=40%><color=#B22222>━━━━━━━ ❖ ━━━━━━━</color></size>";
                }
                else
                {
                    titleText.text = text;
                }
            }

            if (soundEffect != null && audioSource != null)
            {
                audioSource.PlayOneShot(soundEffect, soundVolume);
            }

            // fade in and float up
            float elapsed = 0f;
            Vector2 startPos = originalPosition - new Vector2(0f, 25f);
            Vector3 startScale = new Vector3(0.92f, 0.92f, 1f);

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (canvasGroup != null) canvasGroup.alpha = ease;
                if (containerRect != null)
                {
                    containerRect.anchoredPosition = Vector2.Lerp(startPos, originalPosition, ease);
                    containerRect.localScale = Vector3.Lerp(startScale, Vector3.one, ease);
                }
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (containerRect != null)
            {
                containerRect.anchoredPosition = originalPosition;
                containerRect.localScale = Vector3.one;
            }

            // hold on screen
            elapsed = 0f;
            while (elapsed < displayDuration)
            {
                elapsed += Time.deltaTime;
                if (containerRect != null)
                {
                    float floatY = Mathf.Sin(elapsed * 1.8f) * 3f;
                    containerRect.anchoredPosition = originalPosition + new Vector2(0f, floatY);
                }
                yield return null;
            }

            // fade out
            elapsed = 0f;
            Vector2 endPos = originalPosition + new Vector2(0f, 20f);
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                float ease = t * t;

                if (canvasGroup != null) canvasGroup.alpha = 1f - ease;
                if (containerRect != null)
                {
                    containerRect.anchoredPosition = Vector2.Lerp(originalPosition, endPos, ease);
                }
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (containerRect != null) containerRect.anchoredPosition = originalPosition;
            activeRoutine = null;
        }
    }
}
