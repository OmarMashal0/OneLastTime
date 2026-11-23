using UnityEngine;

namespace Level3Bamboo
{
    // A bird sits on its perch and now and then hops up with its wings raised, then settles again.
    public class PerchedBird : MonoBehaviour
    {
        public SpriteRenderer body;
        public Sprite wingsUp;
        public float phase;

        private Vector3 startPosition;
        private Sprite perched;

        // Start is called before the first frame update
        void Start()
        {
            startPosition = body.transform.localPosition;
            perched = body.sprite;
        }

        // Update is called once per frame
        void Update()
        {
            // The hop takes the last 16% of a slow loop (about every 14 seconds).
            float loop = Mathf.Repeat(Time.timeSinceLevelLoad * 0.07f + phase, 1f);
            float hop = Mathf.InverseLerp(0.8f, 0.96f, loop);
            bool flying = hop > 0f && hop < 1f;

            body.sprite = flying ? wingsUp : perched;
            body.transform.localPosition = startPosition + new Vector3(Mathf.Sin(hop * Mathf.PI * 2f) * 0.36f, Mathf.Sin(hop * Mathf.PI) * 0.4f);
        }
    }
}
