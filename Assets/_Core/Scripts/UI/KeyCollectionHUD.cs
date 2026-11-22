using UnityEngine;
using UnityEngine.UI;

namespace OneLastTime.UI
{
    public class KeyCollectionHUD : MonoBehaviour
    {
        public static KeyCollectionHUD Instance { get; private set; }

        [Header("Badge Sprites")]
        [SerializeField] private Sprite[] badgeSprites;

        [Header("UI Image")]
        [SerializeField] private Image badgeImage;

        [Header("Keys State")]
        [SerializeField] private int currentKeys = 0;

        public int CurrentKeys => currentKeys;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (badgeImage == null)
            {
                badgeImage = GetComponentInChildren<Image>(true);
            }

            // keep hidden until player inspects the sword case
            if (badgeImage != null && !badgeImage.gameObject.activeSelf)
            {
                badgeImage.gameObject.SetActive(false);
            }
        }

        public void ShowBadge(int keys = 0)
        {
            currentKeys = Mathf.Clamp(keys, 0, 2);
            UpdateSprite();

            if (badgeImage != null)
            {
                badgeImage.gameObject.SetActive(true);
            }
            gameObject.SetActive(true);
        }

        public void SetKeys(int keys)
        {
            currentKeys = Mathf.Clamp(keys, 0, 2);
            UpdateSprite();
        }

        void UpdateSprite()
        {
            if (badgeImage != null && badgeSprites != null && badgeSprites.Length > currentKeys)
            {
                if (badgeSprites[currentKeys] != null)
                {
                    badgeImage.sprite = badgeSprites[currentKeys];
                }
            }
        }
    }
}
