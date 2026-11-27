using UnityEngine;
using UnityEngine.EventSystems;

namespace Level3Bamboo
{
    // The "Skip" button for Level 3's dialogue sequences (Lab 8 style UI Button + Lab 9 dialogue).
    //
    // Update: the button is shown only while the tea talk (the NPC conversation) is playing.
    // Swamp Thing's lines and the hallucination have no Skip button; they finish by themselves.
    // Skip(): called by the Button's On Click () list in the Inspector. Each dialogue's own Skip() uses
    // that dialogue's normal finish, so the camera, zoom, lighting, music and Ryu's controls are restored
    // there and no story progress is lost. A dialogue that is not playing simply ignores the call.
    public class DialogueSkipButton : MonoBehaviour
    {
        [Tooltip("The Skip button object. Hidden outside dialogue.")]
        public GameObject button;
        public TeaRestConversation teaConversation;
        [Header("Click sound")]
        public AudioSource clickSource;
        public AudioClip clickSound;

        void Update()
        {
            bool dialoguePlaying = teaConversation.Running;
            if (button.activeSelf != dialoguePlaying)
                button.SetActive(dialoguePlaying);
        }

        public void Skip()
        {
            if (clickSource != null && clickSound != null)
                clickSource.PlayOneShot(clickSound, 0.5f);

            teaConversation.Skip();

            // Forget the clicked button, so pressing Space or Enter afterwards cannot "click" it again.
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
