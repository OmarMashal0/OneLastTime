using UnityEngine;

namespace Level3Bamboo
{
    public class SwampCageEncounter : MonoBehaviour
    {
        public SwampThing enemy;
        public CollectibleLog log;
        public Collider2D chargeTarget;
        public SpriteRenderer cageArt;
        public Sprite intactRack;
        public Sprite brokenCage;
        public CollectibleLog deathReward;
        public Transform rewardFallback;
        public AudioSource breakAudio;
        public AudioClip breakClip;
        public ParticleSystem splinters;
        private bool audioPaused;

        public bool Broken { get; private set; }
        public bool Defeated { get; private set; }
        public event System.Action ObjectiveChanged;

        // Start is called before the first frame update
        void Start()
        {
            // Checkpoint retries do not rerun Start, so only a fresh scene resets the rack.
            cageArt.sprite = intactRack;
            chargeTarget.enabled = true;
            log.locked = true;
            log.GetComponent<SpriteRenderer>().enabled = false;
            deathReward.locked = true;
            deathReward.gameObject.SetActive(false);
        }

        // Update is called once per frame
        void Update()
        {
            bool paused = Time.timeScale <= 0f;
            if (breakAudio == null || paused == audioPaused)
                return;
            audioPaused = paused;
            if (paused)
                breakAudio.Pause();
            else
                breakAudio.UnPause();
        }

        public void TryBreakFromArm(SwampThing attacker)
        {
            if (Broken || attacker != enemy || attacker.CurrentState != SwampThing.State.ArmStrike
                || !attacker.arm.enabled || !chargeTarget.enabled)
                return;

            if (attacker.arm.Distance(chargeTarget).isOverlapped)
                BreakCage();
        }

        private bool BreakCage()
        {
            if (Broken)
                return false;

            Broken = true;
            chargeTarget.enabled = false;
            cageArt.sprite = brokenCage;
            // The intact artwork shows the trapped log. The opened cage shows the real pickup.
            log.GetComponent<SpriteRenderer>().enabled = true;
            if (breakAudio != null && breakClip != null)
                breakAudio.PlayOneShot(breakClip);
            if (splinters != null)
                splinters.Emit(9);
            UnlockWhenFinished();
            ObjectiveChanged?.Invoke();
            return true;
        }

        public void ResolveDefeat(Vector3? groundedPosition)
        {
            if (Defeated || enemy.Health != 0 || enemy.CurrentState != SwampThing.State.Dead)
                return;

            Defeated = true;
            deathReward.transform.position = groundedPosition ?? rewardFallback.position;
            deathReward.locked = false;
            deathReward.gameObject.SetActive(true);
            if (!BreakCage())
            {
                UnlockWhenFinished();
                ObjectiveChanged?.Invoke();
            }
        }

        private void UnlockWhenFinished()
        {
            if (!Broken || !Defeated || !log.gameObject.activeSelf)
                return;

            log.locked = false;
        }
    }
}
