using UnityEngine;

namespace Level3Bamboo
{
    // A mist patch slowly drifts to the right, fading in and out so it can loop without a visible jump.
    public class DriftingMist : MonoBehaviour
    {
        public SpriteRenderer mist;
        [Tooltip("How far the patch drifts before it loops.")]
        public float driftDistance = 5f;
        [Tooltip("Speed multiplier (1 = normal).")]
        public float speed = 1f;
        [Tooltip("Where in its loop this patch starts (0 to 1).")]
        public float phase;
        [Range(0f, 2f)] public float brightness = 0.8f;

        private Vector3 startPosition;
        private Color baseColor;

        // Start is called before the first frame update
        void Start()
        {
            startPosition = mist.transform.localPosition;
            baseColor = mist.color;
        }

        // Update is called once per frame
        void Update()
        {
            float loop = Mathf.Repeat(Time.timeSinceLevelLoad * 0.035f * speed + phase, 1f);
            mist.transform.localPosition = startPosition + Vector3.right * ((loop - 0.5f) * driftDistance);

            // Invisible at the start and end of the loop, strongest in the middle.
            Color color = baseColor;
            color.a *= Mathf.Clamp01(Mathf.Sin(loop * Mathf.PI) * brightness);
            mist.color = color;
        }
    }
}
