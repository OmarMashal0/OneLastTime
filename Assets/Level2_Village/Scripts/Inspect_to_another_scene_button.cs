using System.Collections;
using UnityEngine;

public class Inspect_to_another_scene_button  : MonoBehaviour
{
    // Drag "Canvas For Inspect Button" here. It must have a CanvasGroup component.
    [SerializeField] private CanvasGroup promptGroup;

    // How many seconds the prompt takes to fade in or out.
    [SerializeField] private float promptFadeTime = 0.3f;

    // The exact name of the scene to load when F is pressed.
    [SerializeField] private string nextSceneName = "NextScene";

    [SerializeField] private string playerTag = "Player";

    private bool playerInside = false;
    private bool transitioning = false;
    private Coroutine promptRoutine;

    private void Start()
    {
        // Start invisible and non-interactive.
        if (promptGroup != null)
        {
            promptGroup.alpha = 0f;
            promptGroup.blocksRaycasts = false;
            promptGroup.interactable = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = true;
            StartPromptFade(1f); // fade in
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = false;
            StartPromptFade(0f); // fade out
        }
    }

    private void Update()
    {
        if (playerInside && !transitioning && Input.GetKeyDown(KeyCode.F))
        {
            transitioning = true;
            StartPromptFade(0f);
            SceneFader.Instance.FadeToScene(nextSceneName);
        }
    }

    private void StartPromptFade(float target)
    {
        if (promptGroup == null) return;

        // Stop any fade already running so the prompt doesn't fight itself.
        if (promptRoutine != null) StopCoroutine(promptRoutine);
        promptRoutine = StartCoroutine(FadePrompt(target));
    }

    private IEnumerator FadePrompt(float target)
    {
        float start = promptGroup.alpha;
        float t = 0f;

        while (t < promptFadeTime)
        {
            t += Time.deltaTime;
            promptGroup.alpha = Mathf.Lerp(start, target, t / promptFadeTime);
            yield return null;
        }

        promptGroup.alpha = target;
    }
}