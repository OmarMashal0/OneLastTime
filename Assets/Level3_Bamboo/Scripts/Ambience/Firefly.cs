using UnityEngine;

namespace Level3Bamboo
{
    // A firefly hovers around its spot and slowly blinks on and off.
    public class Firefly : MonoBehaviour
    {
        public SpriteRenderer body;
        public float phase;
        [Range(0f, 2f)] public float brightness = 0.8f;

        private Vector3 startPosition;
        private Color baseColor;

        // Start is called before the first frame update
        void Start()
        {
            startPosition = body.transform.localPosition;
            baseColor = body.color;
        }

        // Update is called once per frame
        void Update()
        {
            float time = Time.timeSinceLevelLoad;
            float wave = Mathf.Sin(time * 0.7f + phase);
            body.transform.localPosition = startPosition + new Vector3(wave * 0.42f, Mathf.Sin(time * 1.1f + phase * 4f) * 0.23f);

            float blink = Mathf.Repeat(time * 0.24f + phase, 1f);
            Color color = baseColor;
            color.a *= Mathf.Clamp01(Mathf.Sin(blink * Mathf.PI) * brightness);
            body.color = color;
        }
    }
}
