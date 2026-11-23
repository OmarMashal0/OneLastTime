using System.Collections;
using TMPro;
using UnityEngine;

namespace Level3Bamboo
{
    public class SwampDialogue : MonoBehaviour
    {
        public GameObject panel;
        public TMP_Text speakerName;
        public TMP_Text dialogueText;
        public SwampThing swampThing;
        public BambooPlayerController player;
        [Min(1f)] public float lettersPerSecond = 30f;
        [Min(0f)] public float displaySeconds = 1.6f;

        private bool entrancePlayed;
        private bool burrowPlayed;
        private bool defeatPlayed;
        private bool burrowWaiting;
        private Coroutine typing;

        // True while a line is on screen (the hallucination waits for the last line to finish).
        public bool IsShowing => typing != null;

        // Start is called before the first frame update
        void Start()
        {
            speakerName.text = "Swamp Thing";
            Hide();
        }

        // Update is called once per frame
        void Update()
        {
            if (player.IsDead && typing != null)
                Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (entrancePlayed || player.IsDead || other.GetComponentInParent<BambooPlayerController>() != player
                || swampThing.cage.Defeated || swampThing.CurrentState == SwampThing.State.Dead)
                return;

            entrancePlayed = true;
            Say("So… you’ve finally come.");
        }

        public void FirstBurrow()
        {
            if (burrowPlayed || defeatPlayed || player.IsDead)
                return;

            burrowPlayed = true;
            if (typing != null)
                burrowWaiting = true;
            else
                Say("The roots run beneath you.");
        }

        public void Defeated()
        {
            if (defeatPlayed)
                return;

            defeatPlayed = true;
            Hide();
            if (!player.IsDead)
                Say("The forest… remembers.");
        }

        public void ResetAttempt()
        {
            Hide();
            entrancePlayed = false;
            burrowPlayed = false;
            defeatPlayed = false;
        }

        private void Say(string sentence)
        {
            typing = StartCoroutine(TypeLine(sentence));
        }

        private IEnumerator TypeLine(string sentence)
        {
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
            typing = null;
            panel.SetActive(false);
            if (burrowWaiting)
            {
                burrowWaiting = false;
                Say("The roots run beneath you.");
            }
        }

        private void Hide()
        {
            if (typing != null)
                StopCoroutine(typing);
            typing = null;
            burrowWaiting = false;
            if (panel != null)
                panel.SetActive(false);
        }
    }
}
