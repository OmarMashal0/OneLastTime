using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WellWarningConversation : MonoBehaviour
{
    [SerializeField] private Rigidbody2D player;
    [SerializeField] private GameObject samuraiPanel;
    [SerializeField] private Text samuraiLine;
    [SerializeField] private GameObject villagerPanel;
    [SerializeField] private Text villagerLine;
    [SerializeField] private AudioSource talkingEffect;
    [SerializeField, Min(1f)] private float charactersPerSecond = 24f;
    [SerializeField, Min(0f)] private float readingTime = 2f;
    private bool started;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (started || other.attachedRigidbody != player)
            return;

        started = true;
        var movement = player.GetComponent<DummyPlayer>();
        if (movement != null) movement.SetMovementLocked(true);
        StartCoroutine(Converse());
    }

    private IEnumerator Converse()
    {
        yield return Speak(samuraiPanel, samuraiLine);
        yield return Speak(villagerPanel, villagerLine);
        var movement = player.GetComponent<DummyPlayer>();
        if (movement != null) movement.SetMovementLocked(false);
    }

    private IEnumerator Speak(GameObject panel, Text label)
    {
        string line = label.text;
        label.text = string.Empty;
        panel.SetActive(true);
        talkingEffect.Play();
        float revealed = 0f;
        while (revealed < line.Length)
        {
            revealed += Time.unscaledDeltaTime * Mathf.Max(1f, charactersPerSecond);
            int count = Mathf.Min((int)revealed, line.Length);
            if (count != label.text.Length)
                label.text = line.Substring(0, count);
            yield return null;
        }
        talkingEffect.Stop();
        yield return new WaitForSecondsRealtime(readingTime);
        panel.SetActive(false);
    }
}
