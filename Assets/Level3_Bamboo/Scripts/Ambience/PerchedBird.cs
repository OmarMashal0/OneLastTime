using UnityEngine;

namespace Level3Bamboo
{
    // A bird sits on its perch and every few seconds hops up with its wings raised, then settles again.
    // Give each bird a different Phase (0-1) so they do not all flap at the same moment.
    public class PerchedBird : MonoBehaviour
    {
        public SpriteRenderer body;
        public Sprite wingsUp;
        public float flapEverySeconds = 5f;
        [Range(0f, 1f)] public float phase;

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
            // One loop lasts flapEverySeconds. The flap and hop happen near the end of each loop
            // and take about 0.8 seconds. timeSinceLevelLoad stops while the game is paused.
            float loop = Mathf.Repeat(Time.timeSinceLevelLoad / flapEverySeconds + phase, 1f);
            float hop = Mathf.InverseLerp(0.84f, 1f, loop);
            bool flying = hop > 0f && hop < 1f;

            body.sprite = flying ? wingsUp : perched;
            body.transform.localPosition = startPosition + new Vector3(Mathf.Sin(hop * Mathf.PI * 2f) * 0.36f, Mathf.Sin(hop * Mathf.PI) * 0.4f);
        }
    }
}
