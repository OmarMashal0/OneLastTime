using UnityEngine;

namespace Level3Bamboo
{
    // A butterfly that flutters in a small loop around where it was placed.
    public class Butterfly : MonoBehaviour
    {
        public SpriteRenderer body;
        public float phase;
        public float range = 0.48f;
        public float speed = 1f;

        private Vector3 startPosition;
        private Vector3 startScale;

        void Start()
        {
            startPosition = body.transform.localPosition;
            startScale = body.transform.localScale;
        }

        void Update()
        {
            float time = Time.timeSinceLevelLoad * speed + phase;

            // Move left and right
            float x = Mathf.Sin(time * 0.7f) * range;

            // Move up and down
            float y = Mathf.Sin(time * 1.95f) * 0.24f;

            body.transform.localPosition = startPosition + new Vector3(x, y, 0f);

            // Open and close wings
            float wings = 0.36f + 0.64f * Mathf.Abs(Mathf.Sin(time * 9f));

            body.transform.localScale = new Vector3(
                startScale.x * wings,
                startScale.y,
                startScale.z
            );
        }
    }
}