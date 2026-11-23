using UnityEngine;

namespace Level3Bamboo
{
    // Steps one SpriteRenderer through its own frames on scaled time, so pause freezes it.
    // Used for the entrance house: the hanging lantern and each doorway curtain run their own copy
    // with a different phase, so nothing sways in lockstep.
    public class EntranceSpriteLoop : MonoBehaviour
    {
        public SpriteRenderer target;
        public Sprite[] frames = new Sprite[0];
        [Tooltip("Frames per second.")]
        public float fps = 5f;
        [Tooltip("Plays 0..n..1 instead of wrapping, for back-and-forth sway.")]
        public bool pingPong = true;
        [Tooltip("Start offset in seconds, so neighbouring pieces are out of step.")]
        public float phase;
        [Tooltip("Each pass holds the last pose a little longer by up to this many seconds, for a less mechanical rhythm.")]
        public float restVariation = 0.15f;

        [Header("Warm flicker (optional)")]
        public bool flicker;
        public Color baseColor = Color.white;
        [Range(0f, 0.3f)] public float flickerAmount = 0.06f;
        public SpriteRenderer glow;
        public float glowAlpha = 0.22f;
        [Range(0f, 0.5f)] public float glowFlicker = 0.25f;

        private float clock;
        private float hold;
        private int lastIndex = -1;
        private float seed;

        private void Awake()
        {
            if (target == null)
                target = GetComponent<SpriteRenderer>();
            clock = phase;
            seed = Random.value * 50f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || target == null || frames == null || frames.Length == 0)
                return;

            if (hold > 0f)
                hold -= dt;
            else
                clock += dt;

            int count = frames.Length;
            int cycle = pingPong && count > 2 ? count * 2 - 2 : count;
            int step = Mathf.FloorToInt(clock * Mathf.Max(0.1f, fps)) % cycle;
            int index = step < count ? step : cycle - step;
            if (index != lastIndex)
            {
                // Rest briefly at either end of the sway.
                if ((index == 0 || index == count - 1) && lastIndex >= 0 && restVariation > 0f)
                    hold = Random.Range(0f, restVariation);
                lastIndex = index;
                target.sprite = frames[index];
            }

            if (flicker)
            {
                float n = Mathf.PerlinNoise(seed, clock * 2.3f) - 0.5f;
                float k = 1f + n * 2f * flickerAmount;
                target.color = new Color(Mathf.Clamp01(baseColor.r * k), Mathf.Clamp01(baseColor.g * k), Mathf.Clamp01(baseColor.b * (k * 0.97f)), baseColor.a);
                if (glow != null)
                {
                    Color g = glow.color;
                    g.a = glowAlpha * (1f + n * 2f * glowFlicker);
                    glow.color = g;
                }
            }
        }
    }
}
