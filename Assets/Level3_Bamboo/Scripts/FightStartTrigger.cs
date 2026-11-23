using UnityEngine;

namespace Level3Bamboo
{
    // The trigger at the entrance of Badawy's clearing (same idea as Lab 9's NPCDialogue trigger).
    // When Ryu walks in, it tells the fight to start.
    public class FightStartTrigger : MonoBehaviour
    {
        public BadawyEncounter fight;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.tag == "Player")
                fight.StartFight();
        }
    }
}
