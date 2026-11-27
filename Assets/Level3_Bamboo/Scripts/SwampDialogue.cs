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

        [Header("Dialogue sounds (quiet)")]
        public AudioSource dialogueSound;
        public AudioClip openClip;      // the box opens
        public AudioClip lineClip;      // a new line starts
        public AudioClip tickClip;      // a soft typing tick
        [Min(1)] public int lettersPerTick = 4;   // one tick every few letters, not every letter

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

        // A new attempt after a respawn. The entrance lines are NOT reset: they play once per level run,
        // so on a retry he wakes straight away. Only the attack line can be said again.
        public void ResetAttempt()
        {
            Hide();
            attackSaid = false;
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
            PlayDialogueSound(openClip, 0.5f);   // the box opens once for a group of lines
            bool firstLine = true;
            while (waiting.Count > 0)
            {
                string sentence = waiting[0];
                waiting.RemoveAt(0);
                dialogueText.text = sentence;
                dialogueText.maxVisibleCharacters = 0;
                panel.SetActive(true);
                dialogueText.ForceMeshUpdate();

                if (!firstLine)
                    PlayDialogueSound(lineClip, 0.45f);
                firstLine = false;

                int length = dialogueText.textInfo.characterCount;
                float visible = 0f;
                int nextTick = lettersPerTick;
                while (dialogueText.maxVisibleCharacters < length)
                {
                    yield return null;
                    visible += Time.deltaTime * lettersPerSecond;
                    dialogueText.maxVisibleCharacters = Mathf.Min(length, (int)visible);
                    if (visible >= nextTick)
                    {
                        PlayDialogueSound(tickClip, 0.12f);
                        nextTick += lettersPerTick;
                    }
                }
                yield return new WaitForSeconds(displaySeconds);
                if (entranceLinesLeft > 0)
                    entranceLinesLeft--;
            }
            typing = null;
            panel.SetActive(false);
        }

        // Plays a dialogue sound once, quietly (all three clips use this).
        private void PlayDialogueSound(AudioClip clip, float volume)
        {
            if (dialogueSound != null && clip != null)
                dialogueSound.PlayOneShot(clip, volume);
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
