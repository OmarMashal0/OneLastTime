using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NoticeboardRunes
{
    public sealed class NoticeboardCipher : MonoBehaviour
    {
        [Serializable]
        private sealed class Glyph
        {
            public char Letter => character[0];
            public string character;
            public RawImage rune;
            [NonSerialized] public Text revealedLetter;
            [NonSerialized] public Image[] motes;
            [NonSerialized] public float started;
        }

        [SerializeField] private Font inkFont;
        [SerializeField] private string inventoryItemName = "Tordemacjq";
        [SerializeField] private Font timerFont;
        [SerializeField] private AudioClip countdownClip;
        private AudioSource countdownAudio;
        [SerializeField] private Glyph[] glyphs;

        private const float TimeLimit = 50f;
        private const float MistakePenalty = 2f;
        private const string VillageScene = "L2_A_VillageChase";
        private int nextGlyph;
        private bool running;
        private bool finished;
        private float deadline;
        private Image timerFill;
        private Text timerLabel;
        private int displayedSeconds = -1;

        private void Start()
        {
            countdownAudio = gameObject.AddComponent<AudioSource>();
            countdownAudio.playOnAwake = false;
            countdownAudio.loop = true;
            countdownAudio.clip = countdownClip;
            var scroll = (RectTransform)transform;
            Image track = AddGraphic<Image>("Countdown track", scroll, new Vector2(296f, 44f));
            track.rectTransform.anchoredPosition = new Vector2(0f, -scroll.rect.height / 2f - 30f);
            track.color = new Color32(49, 29, 24, 255);
            timerFill = AddGraphic<Image>("Time remaining", track.rectTransform, new Vector2(288f, 12f));
            timerFill.rectTransform.anchorMin = timerFill.rectTransform.anchorMax = Vector2.zero;
            timerFill.rectTransform.pivot = Vector2.zero;
            timerFill.rectTransform.anchoredPosition = new Vector2(4f, 4f);
            timerFill.color = new Color32(255, 148, 53, 255);
            timerLabel = AddGraphic<Text>("Seconds", track.rectTransform, new Vector2(288f, 24f));
            timerLabel.rectTransform.anchoredPosition = new Vector2(0f, 9f);
            timerLabel.font = timerFont;
            timerLabel.fontSize = 16;
            timerLabel.alignment = TextAnchor.MiddleCenter;
            ShowTime(TimeLimit);
        }

        private void ShowTime(float remaining)
        {
            timerFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(remaining / TimeLimit), 1f, 1f);
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            if (seconds == displayedSeconds) return;
            displayedSeconds = seconds;
            timerLabel.text = seconds + " s";
        }

        private bool CheckClock()
        {
            if (finished || timerLabel == null) return false;
            if (!running)
            {
                if (SceneFader.Instance.IsTransitioning) return false;
                deadline = Time.unscaledTime + TimeLimit;
                running = true;
                countdownAudio.Play();
            }

            float remaining = deadline - Time.unscaledTime;
            ShowTime(remaining);
            if (remaining > 0f) return true;

            finished = true;
            StartCoroutine(ReturnToVillage(false));
            return false;
        }

        // The buttons supply their zero-based position in the rune alphabet.
        public void Decode(int runeIndex)
        {
            if (!CheckClock()) return;
            Glyph glyph = glyphs[nextGlyph];
            if (char.ToUpperInvariant(glyph.Letter) - 'A' != runeIndex)
            {
                deadline -= MistakePenalty;
                CheckClock();
                return;
            }

            Reveal(glyph);
            nextGlyph++;
            finished = nextGlyph == glyphs.Length;
            if (finished)
            {
                ScrollHotbar.Collect(ScrollHotbar.ScrollId, inventoryItemName, GetComponent<Image>().sprite);
                StartCoroutine(ReturnToVillage(true));
            }
        }

        private IEnumerator ReturnToVillage(bool solved)
        {
            float start = Time.unscaledTime;
            float volume = countdownAudio.volume;
            while (Time.unscaledTime - start < 1f)
            {
                countdownAudio.volume = Mathf.SmoothStep(volume, 0f, Time.unscaledTime - start);
                yield return null;
            }
            countdownAudio.volume = 0f;
            countdownAudio.Stop();
            if (solved) yield return new WaitForSecondsRealtime(0.25f);
            SceneFader.Instance.FadeToScene(VillageScene);
        }

        private void Reveal(Glyph glyph)
        {
            RectTransform slot = glyph.rune.rectTransform;
            Text letter = AddGraphic<Text>("Decoded " + glyph.character, slot, slot.sizeDelta);
            letter.font = inkFont;
            letter.fontSize = 32;
            letter.fontStyle = FontStyle.Bold;
            letter.alignment = TextAnchor.MiddleCenter;
            letter.text = glyph.character;
            letter.color = new Color(0.23f, 0.12f, 0.06f);
            letter.canvasRenderer.SetAlpha(0f);
            glyph.revealedLetter = letter;
            glyph.started = Time.unscaledTime;
            glyph.motes = new Image[7];
            for (int i = 0; i < glyph.motes.Length; i++)
            {
                Image mote = AddGraphic<Image>("Ink fleck", slot, Vector2.one * (2 + i % 3));
                mote.color = Color.Lerp(letter.color, new Color(0.85f, 0.53f, 0.17f), i / 7f);
                glyph.motes[i] = mote;
            }
        }

        private static T AddGraphic<T>(string objectName, RectTransform parent, Vector2 size) where T : Graphic
        {
            var item = new GameObject(objectName, typeof(RectTransform));
            item.layer = parent.gameObject.layer;
            item.transform.SetParent(parent, false);
            T graphic = item.AddComponent<T>();
            graphic.rectTransform.sizeDelta = size;
            graphic.raycastTarget = false;
            return graphic;
        }

        private void Update()
        {
            CheckClock();
            foreach (Glyph glyph in glyphs)
            {
                if (glyph.motes == null) continue;

                float t = Mathf.Clamp01((Time.unscaledTime - glyph.started) / 0.55f);
                glyph.rune.canvasRenderer.SetAlpha(1f - Mathf.SmoothStep(0f, 1f, t * 2f));
                glyph.revealedLetter.canvasRenderer.SetAlpha(Mathf.SmoothStep(0f, 1f, (t - 0.2f) / 0.8f));
                for (int i = 0; i < glyph.motes.Length; i++)
                {
                    Image mote = glyph.motes[i];
                    float angle = i * 2.4f;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    mote.rectTransform.anchoredPosition = direction * (4f + 14f * t) + Vector2.up * (8f * t);
                    mote.canvasRenderer.SetAlpha(1f - t);
                    if (t >= 1f) Destroy(mote.gameObject);
                }
                if (t >= 1f) glyph.motes = null;
            }
        }
    }
}
