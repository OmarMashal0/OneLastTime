using UnityEngine;

namespace Level3Bamboo
{
    // Makes a warm glow sprite breathe like a real flame: its size and brightness wobble a little.
    // Put it on the glow object that sits exactly on the flame. Uses Time.time, so it stops while paused.
    public class TorchFlicker : MonoBehaviour
    {
        public SpriteRenderer glow;
        [Tooltip("How much bigger or smaller the glow gets (0.08 = 8%).")]
        public float sizeWobble = 0.08f;
        [Tooltip("How much brighter or darker the glow gets.")]
        public float brightnessWobble = 0.25f;
        public float speed = 6f;

        private Vector3 baseScale;
        private float baseAlpha;
        private float seed;

        void Awake()
        {
            baseScale = transform.localScale;
            baseAlpha = glow.color.a;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed);   // 0..1, smooth
            transform.localScale = baseScale * (1f + (n - 0.5f) * 2f * sizeWobble);
            Color c = glow.color;
            c.a = baseAlpha * (1f + (n - 0.5f) * 2f * brightnessWobble);
            glow.color = c;
        }
    }
}
