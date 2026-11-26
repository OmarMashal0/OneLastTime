using UnityEngine;

namespace Level3Bamboo
{
    // One bamboo chime of the first log's puzzle. ChimePuzzle decides which chime Ryu is closest to
    // and calls Ring(); the chime itself only plays its animation, its sound and the "wrong" flash.
    public class ChimeInteract : MonoBehaviour
    {
        [Tooltip("Moon, Leaf or Sun. ChimePuzzle compares this with its correct order.")]
        public string symbol;
        public Animator animator;
        [Tooltip("Plays this chime's own note.")]
        public AudioSource sound;
        public SpriteRenderer art;
        [Tooltip("Where Ryu has to stand near, measured from the chime's feet.")]
        public Vector2 interactionOffset = new Vector2(0f, 0.6f);

        private float wrongFlashTime;

        public Vector2 InteractionPoint => (Vector2)transform.position + interactionOffset;

        // Update is called once per frame
        void Update()
        {
            // A short reddish flash after a wrong chime. Time.deltaTime is 0 while paused, so it waits.
            wrongFlashTime = Mathf.Max(0f, wrongFlashTime - Time.deltaTime);
            float flash = Mathf.Clamp01(wrongFlashTime / 0.45f);
            art.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.5f), flash);
        }

        public void Ring()
        {
            animator.Play("Ring", 0, 0f);
            sound.Play();
        }

        public void ShowWrong()
        {
            wrongFlashTime = 0.45f;
        }
    }
}
