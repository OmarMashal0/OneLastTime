using System.Collections;
using UnityEngine;
using TMPro;

namespace OneLastTime.UI
{
    public class ConfrontationOverlay : MonoBehaviour
    {
        public static ConfrontationOverlay Instance { get; private set; }

        [Header("UI Elements")]
        [SerializeField] private CanvasGroup vignetteCanvasGroup;
        [SerializeField] private CanvasGroup subtitleCanvasGroup;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip wordAppearClip;
        [SerializeField] private AudioClip wordFinalClimaxClip;

        public AudioSource OverlayAudioSource { get => audioSource; set => audioSource = value; }
        public AudioClip WordAppearClip { get => wordAppearClip; set => wordAppearClip = value; }
        public AudioClip WordFinalClimaxClip { get => wordFinalClimaxClip; set => wordFinalClimaxClip = value; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (vignetteCanvasGroup != null)
            {
                vignetteCanvasGroup.alpha = 0f;
                vignetteCanvasGroup.blocksRaycasts = false;
            }

            if (subtitleCanvasGroup != null)
            {
                subtitleCanvasGroup.alpha = 0f;
                subtitleCanvasGroup.blocksRaycasts = false;
            }

            if (subtitleText != null)
            {
                subtitleText.text = "";
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                }
            }
        }

        public IEnumerator FadeVignette(float targetAlpha, float duration)
        {
            if (vignetteCanvasGroup == null) yield break;

            float startAlpha = vignetteCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                vignetteCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            vignetteCanvasGroup.alpha = targetAlpha;
        }

        public IEnumerator PlayWordByWord(string[] steps, float[] pauses, System.Action onFinalWord)
        {
            if (subtitleText == null || subtitleCanvasGroup == null) yield break;

            subtitleCanvasGroup.alpha = 1f;
            subtitleText.text = "";

            for (int i = 0; i < steps.Length; i++)
            {
                subtitleText.text = steps[i];

                bool isFinalWord = (i == steps.Length - 1);

                // Play audio per step
                if (audioSource != null)
                {
                    if (isFinalWord)
                    {
                        if (wordFinalClimaxClip != null)
                        {
                            audioSource.pitch = 1.0f;
                            audioSource.PlayOneShot(wordFinalClimaxClip, 1.0f);
                        }
                    }
                    else
                    {
                        if (wordAppearClip != null)
                        {
                            // Subtle pitch variation for organic typewriter hits
                            audioSource.pitch = Random.Range(0.96f, 1.04f);
                            audioSource.PlayOneShot(wordAppearClip, 0.85f);
                        }
                    }
                }

                if (isFinalWord && onFinalWord != null)
                {
                    onFinalWord.Invoke();
                }

                float waitTime = (pauses != null && i < pauses.Length) ? pauses[i] : 0.35f;
                yield return new WaitForSeconds(waitTime);
            }
        }

        public IEnumerator FadeOutSubtitle(float duration)
        {
            if (subtitleCanvasGroup == null) yield break;

            float startAlpha = subtitleCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                subtitleCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }

            subtitleCanvasGroup.alpha = 0f;
            if (subtitleText != null) subtitleText.text = "";
        }
    }
}

