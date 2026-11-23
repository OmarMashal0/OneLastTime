using UnityEngine;

namespace Level3Bamboo
{
    public class ForestHudArt : MonoBehaviour
    {
        public LogCollection logs;
        public RectTransform logTarget;
        public UnityEngine.UI.Image flyingLog;
        public AudioSource pickupAudio;
        public AudioClip pickupSound;

        private Vector3 pickupWorld;
        private float flightRemaining;
        private bool audioPaused;
        private const float FlightSeconds = 0.56f;

        private void OnEnable()
        {
            logs.LogCollected += OnLogCollected;
        }

        private void OnDisable()
        {
            logs.LogCollected -= OnLogCollected;
        }

        // Start is called before the first frame update
        void Start()
        {
            flyingLog.enabled = false;
        }

        // Update is called once per frame
        void Update()
        {
            bool paused = Time.timeScale <= 0f;
            if (pickupAudio != null && paused != audioPaused)
            {
                audioPaused = paused;
                if (paused)
                    pickupAudio.Pause();
                else
                    pickupAudio.UnPause();
            }
        }

        private void LateUpdate()
        {
            if (flightRemaining <= 0f)
                return;

            flightRemaining = Mathf.Max(0f, flightRemaining - Time.deltaTime);
            flyingLog.enabled = flightRemaining > 0f;
            if (!flyingLog.enabled)
                return;

            var parent = (RectTransform)flyingLog.transform.parent;
            Camera uiCamera = logs.hudCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : logs.hudCanvas.worldCamera;
            Vector2 screen = logs.gameplayCamera.WorldToScreenPoint(pickupWorld);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, uiCamera, out Vector2 origin);
            Vector2 destination = parent.InverseTransformPoint(logTarget.position);
            float progress = 1f - flightRemaining / FlightSeconds;
            flyingLog.rectTransform.localPosition = Vector2.Lerp(origin, destination, Mathf.SmoothStep(0f, 1f, progress))
                + Vector2.up * (Mathf.Sin(progress * Mathf.PI) * 46f);
            flyingLog.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, progress);
        }

        private void OnLogCollected(CollectibleLog log, int count, int total)
        {
            pickupWorld = log.InteractionPoint;
            flightRemaining = FlightSeconds;
            if (pickupSound != null)
                pickupAudio.PlayOneShot(pickupSound);
        }
    }
}
