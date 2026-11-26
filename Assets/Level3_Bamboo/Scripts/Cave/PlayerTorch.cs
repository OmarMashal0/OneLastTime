using TMPro;
using UnityEngine;

namespace Level3Bamboo
{
    // The torch in Ryu's hand. It is a child of Ryu, so it and its glow flip when he turns.
    // F switches it on and off. On: the flame clip plays and the warm glow shows.
    // Off: the unlit torch shows and the glow is hidden.
    //
    // The project uses the Built-in renderer without 2D lights, so the "light" is a simple trick:
    // the cave has a dim tint sprite over it, and the torch carries a soft round glow sprite drawn with
    // an additive material (it brightens what is behind it). It does not light walls or cast shadows.
    public class PlayerTorch : MonoBehaviour
    {
        [Tooltip("Plays the Lit and Unlit clips.")]
        public Animator animator;
        [Tooltip("The warm glow that sits on the flame (child of this torch).")]
        public GameObject torchLight;
        public TMP_Text hint;
        public float hintSeconds = 2.5f;
        public AudioSource sound;
        public AudioClip onClip;
        public AudioClip offClip;

        public bool Lit { get; private set; }

        private float hintTimer;

        void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            if (Input.GetKeyDown(KeyCode.F))
            {
                SetLit(!Lit);
                ShowHint();
                AudioClip clip = Lit ? onClip : offClip;
                if (sound != null && clip != null)
                    sound.PlayOneShot(clip, 0.5f);
            }

            if (hintTimer > 0f)
            {
                hintTimer -= Time.deltaTime;
                hint.enabled = hintTimer > 0f;
            }
        }

        public void SetLit(bool on)
        {
            Lit = on;
            torchLight.SetActive(on);
            if (gameObject.activeInHierarchy)
                animator.Play(on ? "Lit" : "Unlit");
        }

        public void ShowHint()
        {
            hint.text = "F — Torch";
            hint.enabled = true;
            hintTimer = hintSeconds;
        }

        void OnDisable()
        {
            if (hint != null)
                hint.enabled = false;
        }
    }
}
