using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level3Bamboo
{
    public class Level3VitalsHud : MonoBehaviour
    {
        public BambooPlayerController player;
        public Image healthFill;
        public Image sanityFill;
        public TMP_Text sanityLabel;

        private int shownSanity = -1;
        private int shownMaxSanity = -1;

        private void LateUpdate()
        {
            healthFill.fillAmount = player.Health / (float)Mathf.Max(1, player.maxHealth);
            sanityFill.fillAmount = player.Sanity / (float)Mathf.Max(1, player.maxSanity);
            if (shownSanity == player.Sanity && shownMaxSanity == player.maxSanity)
                return;

            shownSanity = player.Sanity;
            shownMaxSanity = player.maxSanity;
            sanityLabel.text = $"Sanity {shownSanity}/{shownMaxSanity}";
        }
    }
}
