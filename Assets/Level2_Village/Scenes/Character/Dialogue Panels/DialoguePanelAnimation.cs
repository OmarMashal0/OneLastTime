using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DialoguePanelAnimation : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;

    private Image panel;
    private float elapsed;

    private void OnEnable()
    {
        panel = GetComponent<Image>();
        elapsed = 0f;
        if (frames != null && frames.Length > 0)
            panel.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0)
            return;

        elapsed = Mathf.Repeat(elapsed + Time.unscaledDeltaTime * framesPerSecond, frames.Length);
        panel.sprite = frames[(int)elapsed];
    }
}
