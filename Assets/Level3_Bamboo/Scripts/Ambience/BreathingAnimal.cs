using UnityEngine;

namespace Level3Bamboo
{
    // A resting rabbit or deer breathes: its sprite gently stretches up and down.
    public class BreathingAnimal : MonoBehaviour
    {
        public SpriteRenderer body;
        public float phase;

        private Vector3 startScale;

        // Start is called before the first frame update
        void Start()
        {
            startScale = body.transform.localScale;
        }

        // Update is called once per frame
        void Update()
        {
            float wave = Mathf.Sin(Time.timeSinceLevelLoad * 0.7f + phase);
            body.transform.localScale = new Vector3(startScale.x, startScale.y * (1f + wave * 0.023f), startScale.z);
        }
    }
}
