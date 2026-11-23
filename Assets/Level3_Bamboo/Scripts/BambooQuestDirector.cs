using System;
using UnityEngine;

namespace Level3Bamboo
{
    // Chooses what the quest scroll says. Each log site has a quest area (a trigger). When Ryu walks into
    // one whose log is still waiting, the scroll shows that site's instructions. It also reacts to the
    // Badawy fight, the Swamp Thing cage and collected logs.
    public class BambooQuestDirector : MonoBehaviour
    {
        [Serializable]
        public class Section
        {
            [Tooltip("The quest area trigger (child of QuestDirector).")]
            public PlayerTriggerZone area;
            public CollectibleLog log;
            public Sprite icon;
            public string title;
            [TextArea] public string instruction;
            public bool requiresUnlock;
            [NonSerialized] public bool visited;
            [NonSerialized] public bool inside;
        }

        public BambooPlayerController player;
        public LogCollection collection;
        public SwampCageEncounter swamp;
        public BadawyEncounter badawy;
        public BambooQuestScroll scroll;
        public Section[] sections;
        public Sprite logIcon;
        public Sprite swordIcon;

        private Section active;
        private int displayedStage = -1;
        private bool trackingBoss;
        private bool bossBriefShown;

        // Start is called before the first frame update
        void Start()
        {
            player.Respawned += RestoreObjective;
            collection.LogCollected += HandleCollection;
            swamp.ObjectiveChanged += HandleCage;
            badawy.ObjectiveChanged += HandleBoss;
        }

        void OnDestroy()
        {
            if (player != null) player.Respawned -= RestoreObjective;
            if (collection != null) collection.LogCollected -= HandleCollection;
            if (swamp != null) swamp.ObjectiveChanged -= HandleCage;
            if (badawy != null) badawy.ObjectiveChanged -= HandleBoss;
        }

        // Update is called once per frame
        void Update()
        {
            if (player.IsDead || badawy.Active)
                return;

            foreach (Section section in sections)
            {
                bool inside = section.area.PlayerInside;
                if (inside && !section.inside && HasReward(section) && section != active)
                {
                    active = section;
                    bool full = section.log == swamp.log && !section.visited && Stage(section) == 0;
                    section.visited = true;
                    ShowSection(full);
                }
                section.inside = inside;
            }

            if (active != null && Stage(active) != displayedStage)
                ShowSection(false);
        }

        private bool HasReward(Section section)
        {
            return section.log.gameObject.activeSelf
                || section.log == swamp.log && swamp.deathReward.gameObject.activeSelf;
        }

        private int Stage(Section section)
        {
            if (section.log == swamp.log)
                return swamp.Defeated ? 2 : swamp.Broken ? 1 : 0;
            return section.requiresUnlock && !section.log.locked ? 2 : 0;
        }

        private void ShowSection(bool full)
        {
            displayedStage = Stage(active);
            string title = active.title;
            string instruction = active.instruction;
            Sprite icon = active.icon;
            bool swampSection = active.log == swamp.log;
            if (displayedStage == 1)
            {
                title = "DEFEAT SWAMP THING";
                instruction = "Defeat him. He will turn into another collectible log.";
                icon = swordIcon;
            }
            else if (displayedStage == 2)
            {
                title = swampSection ? "COLLECT THE LOGS" : "COLLECT THE LOG";
                instruction = swampSection ? "Press E near each available log to collect it." : "Approach the log and press E.";
                icon = logIcon;
            }

            if (full)
                scroll.Present(title, instruction, icon, swampSection);
            else
                scroll.RestoreReminder(title, instruction, icon, swampSection, swampSection);
        }

        private void HandleBoss()
        {
            if (badawy.Active)
            {
                trackingBoss = true;
                active = null;
                if (!badawy.badawy.Alive)
                    scroll.RestoreReminder("Defeat the remaining Gloom Eyes.", "", swordIcon);
                else if (!bossBriefShown)
                {
                    bossBriefShown = true;
                    scroll.Present("CLEAR THE TRAIL", "Defeat Badawy and his Gloom Eyes to open the way forward.", swordIcon);
                }
                else
                    scroll.RestoreReminder("CLEAR THE TRAIL", "Defeat Badawy and his Gloom Eyes.", swordIcon);
            }
            else if (trackingBoss)
            {
                trackingBoss = false;
                if (badawy.Completed)
                    scroll.Hide();
                else
                    scroll.RestoreReminder("CLEAR THE TRAIL", "Return to the clearing to retry.", swordIcon);
            }
        }

        private void HandleCage()
        {
            if (active != null && active.log == swamp.log && Stage(active) != displayedStage)
                ShowSection(false);
        }

        private void HandleCollection(CollectibleLog log, int count, int total)
        {
            if (count == total)
            {
                active = null;
                scroll.Complete(logIcon);
            }
            else if (active != null && active.log == swamp.log && HasReward(active))
                ShowSection(false);
            else
            {
                active = null;
                displayedStage = -1;
                scroll.Hide();
            }
        }

        private void RestoreObjective()
        {
            if (badawy.Active)
                HandleBoss();
            else if (active != null && HasReward(active))
                ShowSection(false);
            else if (bossBriefShown && !badawy.Completed)
                scroll.RestoreReminder("CLEAR THE TRAIL", "Return to the clearing to retry.", swordIcon);
            else
                scroll.Hide();
        }
    }
}
