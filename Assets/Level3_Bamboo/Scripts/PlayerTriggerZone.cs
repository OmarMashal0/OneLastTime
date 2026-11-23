using UnityEngine;

namespace Level3Bamboo
{
    // Put on a GameObject with a trigger collider (Is Trigger ticked). It remembers whether Ryu
    // (tagged "Player") is standing inside it, so another script can simply check PlayerInside.
    // Used for the Badawy fight start area and the five quest areas.
    public class PlayerTriggerZone : MonoBehaviour
    {
        public bool PlayerInside { get; private set; }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.tag == "Player")
                PlayerInside = true;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.tag == "Player")
                PlayerInside = false;
        }
    }
}
