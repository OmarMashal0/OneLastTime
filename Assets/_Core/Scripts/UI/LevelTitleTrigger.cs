using UnityEngine;

namespace OneLastTime.UI
{
    [RequireComponent(typeof(Collider2D))]
    public class LevelTitleTrigger : MonoBehaviour
    {
        [Header("Title Settings")]
        [TextArea(2, 3)]
        [SerializeField] private string titleText = "Level 1 | The Return of The Greatest Samurai";
        [SerializeField] private bool triggerOnce = true;

        private bool hasTriggered;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered && triggerOnce) return;

            if (other.CompareTag("Player") || other.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                hasTriggered = true;
                if (LevelTitleCard.Instance != null)
                {
                    LevelTitleCard.Instance.PlayTitle(titleText);
                }
                else
                {
                    var card = FindObjectOfType<LevelTitleCard>();
                    if (card != null)
                    {
                        card.PlayTitle(titleText);
                    }
                }
            }
        }
    }
}
