using UnityEngine;

namespace Level3Bamboo
{
    // A lantern checkpoint (based on Lab 5, Exercise 15). The lantern has a trigger collider. When Ryu
    // stands in it, the lantern lights up and becomes his respawn point. The Grove Exit lantern only
    // lights after the Badawy fight is won (Required Encounter).
    public class ForestCheckpoint : MonoBehaviour
    {
        public BambooPlayerController player;
        public Transform respawnPoint;
        public SpriteRenderer lantern;
        public Sprite litSprite;
        public SpriteRenderer glow;
        [Tooltip("If this earlier lantern was skipped, lighting this one does not move the respawn point.")]
        public ForestCheckpoint previous;
        [Tooltip("Optional: this lantern only lights after this fight is won.")]
        public BadawyEncounter requiredEncounter;
        [Header("Sound")]
        public AudioSource sound;
        public AudioClip lightClip;   // the lantern catches fire

        public bool Activated { get; private set; }

        private float flash;

        // Start is called before the first frame update
        void Start()
        {
            glow.enabled = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (!Activated)
                return;

            // The glow flares up for a moment, then fades down to a steady light (no text; a soft ignite sound plays once).
            flash -= Time.deltaTime;
            glow.color = new Color(1f, 0.66f, 0.24f, 0.23f + Mathf.Clamp01(flash) * 0.48f);
        }

        // Stay (not Enter) so the lantern still lights if Ryu is already standing here when the fight ends.
        void OnTriggerStay2D(Collider2D other)
        {
            if (other.tag != "Player" || Activated || player.IsDead)
                return;
            if (requiredEncounter != null && !requiredEncounter.Completed)
                return;

            Activated = true;
            if (previous == null || previous.Activated || requiredEncounter != null)
                player.SetCheckpoint(respawnPoint.position);
            lantern.sprite = litSprite;
            if (sound != null && lightClip != null)
                sound.PlayOneShot(lightClip, 0.6f);
            flash = 1.8f;
            glow.enabled = true;
        }
    }
}
