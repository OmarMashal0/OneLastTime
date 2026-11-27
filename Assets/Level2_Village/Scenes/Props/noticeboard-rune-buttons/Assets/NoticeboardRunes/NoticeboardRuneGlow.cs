using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NoticeboardRunes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class NoticeboardRuneGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private Image glow;
        [SerializeField, Range(0f, 1f)] private float phase;

        private Button button;
        private bool pointerOver;
        private bool selected;
        private float pressTime;
        private const float PressDuration = 0.2f;

        public void Configure(Image overlay, float cycleOffset)
        {
            glow = overlay;
            phase = Mathf.Repeat(cycleOffset, 1f);
        }

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(AnimatePress);
        }

        private void AnimatePress()
        {
            pressTime = PressDuration;
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(AnimatePress);
            pointerOver = false;
            selected = false;
            pressTime = 0f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (glow == null)
                return;

            float cycle = Time.unscaledTime / 4f + phase;
            float wave = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * cycle);
            float intensity = 0.23f + 0.71f * Mathf.Pow(wave, 1.4f);
            if (!button.IsInteractable())
                intensity *= 0.25f;
            else if (pointerOver || selected)
                intensity = Mathf.Max(0.9f, intensity);

            if (pressTime > 0f)
            {
                pressTime = Mathf.Max(0f, pressTime - Time.unscaledDeltaTime);
                float pulse = Mathf.Sin(Mathf.PI * pressTime / PressDuration);
                transform.localScale = Vector3.one * (1f - 0.09f * pulse);
                intensity = Mathf.Lerp(intensity, 1f, pulse);
            }

            Color color = glow.color;
            color.a = intensity;
            glow.color = color;
        }

        public void OnPointerEnter(PointerEventData eventData) { pointerOver = true; }
        public void OnPointerExit(PointerEventData eventData) { pointerOver = false; }
        public void OnSelect(BaseEventData eventData) { selected = true; }
        public void OnDeselect(BaseEventData eventData) { selected = false; }
    }
}
