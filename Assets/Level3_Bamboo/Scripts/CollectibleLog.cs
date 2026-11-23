using UnityEngine;

namespace Level3Bamboo
{
    public class CollectibleLog : MonoBehaviour
    {
        public string logId;
        public bool locked;
        public Vector2 interactionOffset = new Vector2(0f, 0.18f);

        public Vector2 InteractionPoint => (Vector2)transform.position + interactionOffset;

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void MarkCollected()
        {
            // The scene owns this object, so checkpoint respawns leave it collected.
            gameObject.SetActive(false);
        }
    }
}
