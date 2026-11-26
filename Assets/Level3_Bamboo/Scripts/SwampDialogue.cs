using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Level3Bamboo
{
    // Swamp Thing's three short lines, shown one at a time in the dialogue box with the typewriter effect:
    // an entrance line, one attack line (once per attempt) and a defeat line. Lines wait in a short list
    // so they never overlap. The entrance line plays before he wakes (SwampThing waits for
    // EntranceFinished), so Ryu is never attacked while reading it. The defeat line clears everything else and goes first, and
    // IsShowing stays true until the list is empty, so the hallucination afterwards waits for it.
    public class SwampDialogue : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text speakerName;
        public TMP_Text dialogueText;
        public SwampThing swampThing;
        public BambooPlayerController player;
        [Min(1f)] public float lettersPerSecond = 30f;
        [Min(0f)] public float displaySeconds = 1.6f;

        private readonly List<string> waiting = new List<string>();
        private int entranceLinesLeft;
        private bool entrancePlayed, attackSaid, defeatPlayed;
        private Coroutine typing;

        // True while a line is on screen or waiting (the hallucination waits for this to be false).
        public bool IsShowing => typing != null || waiting.Count > 0;

        // True once the entrance line has been read (or if it never started).
        public bool EntranceFinished => entrancePlayed && entranceLinesLeft == 0;

        void Start()
        {
            speakerName.text = "Swamp Thing";
            Hide();
        }

        void Update()
        {
            if (player.IsDead && IsShowing)
                Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        // Walking into the trigger in front of the swamp starts the entrance lines.
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<BambooPlayerController>() == player)
                PlayEntrance();
        }

        public void PlayEntrance()
        {
            if (entrancePlayed || player.IsDead || swampThing.cage.Defeated || swampThing.CurrentState == SwampThing.State.Dead)
                return;
            entrancePlayed = true;
            entranceLinesLeft = 1;
            Add("So… you’ve finally come.");
        }

        // Said once per attempt, the first time he attacks.
        public void AttackLine() { SayOnce(ref attackSaid, "You cannot outrun the roots."); }

        // Defeat goes first: everything else is dropped.
        public void Defeated()
        {
            if (defeatPlayed)
                return;
            defeatPlayed = true;
            Hide();
            if (!player.IsDead)
                Add("The roots… remember.");
        }

        public void ResetAttempt()
        {
            Hide();
            entrancePlayed = attackSaid = defeatPlayed = false;
            entranceLinesLeft = 0;
        }

        private void SayOnce(ref bool said, string sentence)
        {
            if (said || defeatPlayed || player.IsDead)
                return;
            said = true;
            Add(sentence);
        }

        private void Add(string sentence)
        {
            waiting.Add(sentence);
            if (typing == null)
                typing = StartCoroutine(TypeLines());
        }

        private IEnumerator TypeLines()
        {
            while (waiting.Count > 0)
            {
                string sentence = waiting[0];
                waiting.RemoveAt(0);
                dialogueText.text = sentence;
                dialogueText.maxVisibleCharacters = 0;
                panel.SetActive(true);
                dialogueText.ForceMeshUpdate();

                int length = dialogueText.textInfo.characterCount;
                float visible = 0f;
                while (dialogueText.maxVisibleCharacters < length)
                {
                    yield return null;
                    visible += Time.deltaTime * lettersPerSecond;
                    dialogueText.maxVisibleCharacters = Mathf.Min(length, (int)visible);
                }
                yield return new WaitForSeconds(displaySeconds);
                if (entranceLinesLeft > 0)
                    entranceLinesLeft--;
            }
            typing = null;
            panel.SetActive(false);
        }

        private void Hide()
        {
            if (typing != null)
                StopCoroutine(typing);
            typing = null;
            waiting.Clear();
            if (entranceLinesLeft > 0)
                entranceLinesLeft = 0;   // an interrupted entrance never blocks the fight
            if (panel != null)
                panel.SetActive(false);
        }
    }
}
