using UnityEngine;

namespace Level3Bamboo
{
    // A soft lantern glow that flickers, and burns brighter once its checkpoint lantern has been lit.
    public class CheckpointGlow : MonoBehaviour
    {
        public SpriteRenderer glow;
        public ForestCheckpoint checkpoint;
        public float phase;

        private Color baseColor;
        private Vector3 baseScale;

        // Start is called before the first frame update
        void Start()
        {
            baseColor = glow.color;
            baseScale = glow.transform.localScale;
        }

        // Update is called once per frame
        void Update()
        {
            float flicker = Mathf.PerlinNoise(Time.timeSinceLevelLoad * 2.3f, phase * 7.1f);
            float brightness = checkpoint != null && checkpoint.Activated ? 1f : 0.55f;
            Color color = baseColor;
            color.a *= Mathf.Clamp01(brightness * Mathf.Lerp(0.72f, 1f, flicker));
            glow.color = color;
            glow.transform.localScale = baseScale * (0.97f + flicker * 0.06f);
        }
    }
}
