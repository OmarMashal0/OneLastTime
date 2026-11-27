using UnityEngine;

namespace Level3Bamboo
{
    // A log Ryu can collect with E. LogCollection finds the nearest one and calls MarkCollected().
    // locked = true while a puzzle still hides it (the chime box, the Swamp Thing cage).
    public class CollectibleLog : MonoBehaviour
    {
        public string logId;
        public bool locked;
        public Vector2 interactionOffset = new Vector2(0f, 0.18f);

        public Vector2 InteractionPoint => (Vector2)transform.position + interactionOffset;

        public void MarkCollected()
        {
            // The scene owns this object, so checkpoint respawns leave it collected.
            gameObject.SetActive(false);
        }
    }
}
