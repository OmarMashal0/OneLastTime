using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    // How many seconds each fade (out and in) takes.
    [SerializeField] private float fadeDuration = 0.6f;

    private CanvasGroup group;

    // Runs automatically before the first scene loads, so you don't need to place anything in the scene.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateFader()
    {
        if (Instance != null) return;
        new GameObject("SceneFader").AddComponent<SceneFader>();
    }

    private void Awake()
    {
        // Keep only one fader alive.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // survives scene changes

        BuildBlackScreen();
    }

    // Creates a full-screen black overlay in code.
    private void BuildBlackScreen()
    {
        GameObject canvasGO = new GameObject("FadeCanvas");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // draw above everything else

        group = canvasGO.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        GameObject imageGO = new GameObject("Black");
        imageGO.transform.SetParent(canvasGO.transform, false);

        Image image = imageGO.AddComponent<Image>();
        image.color = Color.black;

        RectTransform rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Called by other scripts to change scenes with a fade.
    public void FadeToScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        group.blocksRaycasts = true; // block clicks during the transition

        // 1. Fade to black.
        yield return Fade(1f);

        // 2. Load the new scene while the screen is black.
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        yield return null; // give the new scene one frame to start up

        // 3. Fade back in.
        yield return Fade(0f);

        group.blocksRaycasts = false;
    }

    private IEnumerator Fade(float target)
    {
        float start = group.alpha;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // works even if Time.timeScale is 0
            group.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }

        group.alpha = target;
    }
}