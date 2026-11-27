using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NoticeboardRunes;

public class Inspect_to_another_scene_button  : MonoBehaviour
{
    // Drag "Canvas For Inspect Button" here. It must have a CanvasGroup component.
    [SerializeField] private CanvasGroup promptGroup;
    [SerializeField] private RawImage promptImage;

    private const int SheetColumns = 8;
    private const int SheetRows = 6;
    private const float FramesPerSecond = 24f;
    private float promptTime;

    // How many seconds the prompt takes to fade in or out.
    [SerializeField] private float promptFadeTime = 0.3f;

    // Scene to open when the player interacts.
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
        if (!ScrollHotbar.HasDecodedScroll && other.CompareTag(playerTag))
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
        if (promptImage != null && promptGroup != null && promptGroup.alpha > 0f)
        {
            promptTime = Mathf.Repeat(promptTime + Time.deltaTime, SheetColumns * SheetRows / FramesPerSecond);
            int frame = Mathf.FloorToInt(promptTime * FramesPerSecond);
            promptImage.uvRect = new Rect(
                (frame % SheetColumns) / (float)SheetColumns,
                (SheetRows - 1 - frame / SheetColumns) / (float)SheetRows,
                1f / SheetColumns, 1f / SheetRows);
        }
        else
        {
            promptTime = 0f;
        }

        if (playerInside && !transitioning && !ScrollHotbar.HasDecodedScroll &&
            (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F)))
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
