using UnityEngine;

namespace Level3Bamboo
{
    // Plays a decoration's sway Animator clip (canopy, vines, roots, ruins) at its own speed and phase.
    // Gusts from ForestWind briefly speed the clip up. Optional: drifts and fades a mist sprite.
    public class ForestDecorationMotion : MonoBehaviour
    {
        public ForestWind wind;
        public Animator animator;
        [Min(0f)] public float animationSpeed = 1f;
        [Range(0f, 1f)] public float phase;
        public SpriteRenderer mistRenderer;
        [Range(0f, 1f)] public float opacity = 0.17f;
        public float driftDistance = 2f;
        [Min(1f)] public float driftSeconds = 17f;
        [Tooltip("How much the shared forest gust quickens this animation.")]
        [Min(0f)] public float gustResponse = 0.6f;

        private Vector3 mistOrigin;
        private Color mistTint;

        // Start is called before the first frame update
        void Start()
        {
            if (animator != null)
            {
                animator.speed = animationSpeed;
                animator.Play(0, 0, phase);
            }

            if (mistRenderer != null)
            {
                mistOrigin = mistRenderer.transform.localPosition;
                mistTint = mistRenderer.color;
                UpdateMist();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (animator != null)
            {
                // The prefab asset has no wind; each scene copy gets one assigned in the Inspector.
                float gust = wind != null ? wind.GustAt(transform.position.x, phase * 0.6f) : 0f;
                animator.speed = animationSpeed * (1f + gustResponse * gust);
            }

            if (mistRenderer != null)
                UpdateMist();
        }

        private void UpdateMist()
        {
            float progress = Mathf.Repeat(Time.timeSinceLevelLoad / Mathf.Max(1f, driftSeconds) + phase, 1f);
            mistRenderer.transform.localPosition = mistOrigin + Vector3.right * ((progress - 0.5f) * driftDistance);

            // The wisp crosses its reset point at zero opacity.
            Color color = mistTint;
            color.a = opacity * Mathf.Sin(progress * Mathf.PI);
            mistRenderer.color = color;
        }
    }
}
