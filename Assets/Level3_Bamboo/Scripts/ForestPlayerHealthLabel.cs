using TMPro;
using UnityEngine;

namespace Level3Bamboo
{
    // Shows "HP x/y" for Ryu and only rewrites the text when his health changes.
    public class ForestPlayerHealthLabel : MonoBehaviour
    {
        public BambooPlayerController player;
        public TMP_Text label;
        private int shown = -1;

        // Update is called once per frame
        void Update()
        {
            if (shown == player.Health)
                return;

            shown = player.Health;
            label.text = $"HP {shown}/{player.maxHealth}";
        }
    }
}
