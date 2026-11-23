using UnityEngine;

namespace Level3Bamboo
{
    // A loose leaf that is invisible until a strong gust reaches it; then it tumbles across the screen and fades.
    public class GustLeaf : MonoBehaviour
    {
        public ForestWind wind;
        public SpriteRenderer leaf;
        public float phase;
        [Tooltip("How far the leaf flies.")]
        public float flightDistance = 4.5f;
        [Tooltip("Gust strength needed to lift the leaf.")]
        public float liftStrength = 0.45f;
        [Range(0f, 1f)] public float chancePerGust = 0.75f;

        private Vector3 startPosition;
        private Quaternion startRotation;
        private Color baseColor;
        private bool flying;
        private float progress;
        private float flightSeconds;
        private int lastGust = -1;

        // Start is called before the first frame update
        void Start()
        {
            startPosition = leaf.transform.localPosition;
            startRotation = leaf.transform.localRotation;
            baseColor = leaf.color;
            SetAlpha(0f);
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            if (!flying)
            {
                TryToLiftOff();
                return;
            }

            progress += Time.deltaTime / flightSeconds;
            if (progress >= 1f)
            {
                flying = false;
                leaf.transform.localPosition = startPosition;
                leaf.transform.localRotation = startRotation;
                SetAlpha(0f);
                return;
            }

            float distance = flightDistance * wind.windStrength;
            leaf.transform.localPosition = startPosition + new Vector3(progress * distance,
                Mathf.Sin(progress * 5f + phase) * 0.28f - progress * 0.9f);
            leaf.transform.localRotation = startRotation * Quaternion.Euler(0f, 0f, progress * 220f + Mathf.Sin(progress * 9f) * 25f);
            SetAlpha(Mathf.Sin(progress * Mathf.PI) * 1.6f);
        }

        // Each gust gives the leaf one chance to fly.
        private void TryToLiftOff()
        {
            if (lastGust == wind.GustNumber || wind.GustAt(transform.position.x) < liftStrength)
                return;

            lastGust = wind.GustNumber;
            if (Random.value > chancePerGust)
                return;

            flying = true;
            progress = 0f;
            flightSeconds = Random.Range(3.2f, 4.8f);
        }

        private void SetAlpha(float alpha)
        {
            Color color = baseColor;
            color.a *= Mathf.Clamp01(alpha);
            leaf.color = color;
        }
    }
}
