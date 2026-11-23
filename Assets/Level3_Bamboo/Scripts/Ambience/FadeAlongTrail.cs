using UnityEngine;

namespace Level3Bamboo
{
    // Fades a sprite's transparency as the camera travels between two x positions.
    // Used near the river: the near canopy thins out and a cool river haze appears.
    public class FadeAlongTrail : MonoBehaviour
    {
        public Camera gameplayCamera;
        public SpriteRenderer target;
        public float startX;
        public float endX;
        [Range(0f, 1f)] public float startAlpha = 1f;
        [Range(0f, 1f)] public float endAlpha = 1f;

        private Color baseColor;

        // Start is called before the first frame update
        void Start()
        {
            baseColor = target.color;
        }

        void LateUpdate()
        {
            float x = gameplayCamera.transform.position.x;
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(startX, endX, x));
            Color color = baseColor;
            color.a = Mathf.Lerp(startAlpha, endAlpha, blend);
            target.color = color;
        }
    }
}
