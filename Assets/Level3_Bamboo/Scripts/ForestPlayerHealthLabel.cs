using TMPro;
using UnityEngine;

namespace Level3Bamboo
{
    public class ForestPlayerHealthLabel : MonoBehaviour
    {
        public BambooPlayerController player;
        public TMP_Text label;
        private int shown = -1;

        // Start is called before the first frame update
        void Start()
        {
        }

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
