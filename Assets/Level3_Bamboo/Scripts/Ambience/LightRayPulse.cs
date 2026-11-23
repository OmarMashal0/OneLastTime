using UnityEngine;

namespace Level3Bamboo
{
    // Sunlight shafts through the canopy gently brighten and dim.
    public class LightRayPulse : MonoBehaviour
    {
        public SpriteRenderer rays;
        public float phase;
        [Range(0f, 2f)] public float brightness = 0.8f;

        private Color baseColor;

        // Start is called before the first frame update
        void Start()
        {
            baseColor = rays.color;
        }

        // Update is called once per frame
        void Update()
        {
            float wave = Mathf.Sin(Time.timeSinceLevelLoad * 0.7f + phase);
            Color color = baseColor;
            color.a *= Mathf.Clamp01((0.65f + wave * 0.27f) * brightness);
            rays.color = color;
        }
    }
}
