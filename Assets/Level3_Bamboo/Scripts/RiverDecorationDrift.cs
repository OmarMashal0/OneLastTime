using UnityEngine;

namespace Level3Bamboo.River
{
    public class RiverDecorationDrift : MonoBehaviour
    {
        public Vector2 range;
        public float speed = 1f;
        public float phase;
        public bool driftAcross;
        private Vector3 origin;
        private SpriteRenderer artwork;

        private void Start()
        {
            origin = transform.localPosition;
            artwork = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            float time = Time.time * speed + phase;
            if (driftAcross)
            {
                float cycle = Mathf.Repeat(time, 1f);
                transform.localPosition = origin + (Vector3)(range * cycle);
                var tint = artwork.color;
                tint.a = Mathf.Sin(cycle * Mathf.PI) * 0.70f;
                artwork.color = tint;
                return;
            }
            transform.localPosition = origin + new Vector3(Mathf.Sin(time) * range.x, Mathf.Sin(time * 1.7f) * range.y);
        }
    }
}
