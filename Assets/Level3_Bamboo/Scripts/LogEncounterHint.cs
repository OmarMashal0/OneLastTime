using UnityEngine;
using TMPro;

namespace Level3Bamboo
{
    // Shows an instruction text when Ryu walks near a log that is still locked.
    public class LogEncounterHint : MonoBehaviour
    {
        public Transform player;
        public CollectibleLog log;
        public TMP_Text instruction;
        public bool onlyWhileLocked = true;
        public float showDistance = 5f;

        // Start is called before the first frame update
        void Start()
        {
            instruction.enabled = false;
        }

        // Update is called once per frame
        void Update()
        {
            instruction.enabled = log.gameObject.activeSelf && (!onlyWhileLocked || log.locked)
                && Mathf.Abs(player.position.x - transform.position.x) <= showDistance && Time.timeScale > 0f;
        }
    }
}
