using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Level3Bamboo
{
    // The two round HUD buttons. "Pause" freezes the game (Time.timeScale = 0) and "Mute Music"
    // mutes the level music. The artwork swaps between normal / hover / pressed sprites, and the
    // "alternate" sprites are shown while the game is paused or the music is muted.
    public class ForestHudButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        public enum Action { Pause, MuteMusic }

        public Action action;
        public Button button;
        public Image artwork;
        [Tooltip("Only used by the Mute Music button.")]
        public AudioSource music;
        [Tooltip("A short click. Played with PlayOneShot, which still works while the game is paused.")]
        public AudioSource clickSource;
        public AudioClip clickSound;

        [Header("Sprites")]
        public Sprite normal;
        public Sprite highlighted;
        public Sprite pressed;
        public Sprite alternate;
        public Sprite alternateHighlighted;
        public Sprite alternatePressed;

        private bool hovered;
        private bool held;
        private bool pausedByMe;
        private float clickedUntil;

        private void OnEnable()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClick);
            if (pausedByMe)
                Resume();
            hovered = false;
            held = false;
        }

        // Update is called once per frame
        void Update()
        {
            // Keyboard shortcuts do the same thing as clicking the button:
            // Esc or P pauses and resumes, M mutes and unmutes the music.
            if (action == Action.Pause && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
                OnClick();
            if (action == Action.MuteMusic && Input.GetKeyDown(KeyCode.M))
                OnClick();

            // Uses unscaled time so the button still animates while the game is paused.
            bool alternateState = action == Action.MuteMusic ? music.mute : Time.timeScale <= 0f;
            bool pressing = held && hovered || Time.unscaledTime < clickedUntil;

            Sprite idleSprite = alternateState ? alternate : normal;
            Sprite hoverSprite = alternateState ? alternateHighlighted : highlighted;
            Sprite pressSprite = alternateState ? alternatePressed : pressed;
            if (pressing)
                artwork.sprite = pressSprite;
            else if (hovered)
                artwork.sprite = hoverSprite;
            else
                artwork.sprite = idleSprite;

            float targetScale = pressing ? 0.93f : 1f;
            float scale = Mathf.MoveTowards(artwork.transform.localScale.x, targetScale, Time.unscaledDeltaTime * 2.4f);
            artwork.transform.localScale = Vector3.one * scale;
        }

        private void OnClick()
        {
            clickedUntil = Time.unscaledTime + 0.12f;
            if (clickSource != null && clickSound != null)
                clickSource.PlayOneShot(clickSound, 0.5f);
            if (action == Action.MuteMusic)
            {
                music.mute = !music.mute;
                return;
            }

            if (pausedByMe)
                Resume();
            else if (Time.timeScale > 0f)
                Pause();
        }

        private void Pause()
        {
            pausedByMe = true;
            Time.timeScale = 0f;
            EditorPauseFix.KeepPaused(true);
        }

        private void Resume()
        {
            pausedByMe = false;
            EditorPauseFix.KeepPaused(false);
            Time.timeScale = 1f;
        }

        public void OnPointerEnter(PointerEventData eventData) => hovered = true;
        public void OnPointerExit(PointerEventData eventData) => hovered = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                held = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                held = false;
        }
    }
}
