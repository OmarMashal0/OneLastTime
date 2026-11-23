using UnityEngine;

namespace Level3Bamboo
{
    // A soft amber glow behind a tea-stop lantern that flickers gently.
    // TeaRestConversation turns conversationGlow on during the talk, so the glow warms up a little,
    // and off afterwards, so it fades back to its normal strength.
    public class TeaLanternGlow : MonoBehaviour
    {
        public SpriteRenderer glow;
        [Range(0f, 1f)] public float normalAlpha = 0.45f;
        [Range(0f, 1f)] public float conversationAlpha = 0.75f;
        public float flickerSpeed = 2.3f;
        public bool conversationGlow;

        private float alpha;
        private float seed;
        private Vector3 baseScale;

        // Start is called before the first frame update
        void Start()
        {
            alpha = normalAlpha;
            seed = Random.value * 10f;
            baseScale = glow.transform.localScale;
        }

        // Update is called once per frame
        void Update()
        {
            float goal = conversationGlow ? conversationAlpha : normalAlpha;
            alpha = Mathf.MoveTowards(alpha, goal, Time.deltaTime * 0.4f);

            float flicker = Mathf.PerlinNoise(Time.time * flickerSpeed, seed);
            Color color = glow.color;
            color.a = alpha * Mathf.Lerp(0.8f, 1f, flicker);
            glow.color = color;
            glow.transform.localScale = baseScale * (0.97f + flicker * 0.06f);
        }
    }
}
