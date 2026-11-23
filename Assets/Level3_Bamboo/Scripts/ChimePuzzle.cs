using UnityEngine;

namespace Level3Bamboo
{
    // The first log's puzzle: ring the three bamboo chimes in the order carved on the clue stone
    // (MOON -> LEAF -> SUN). Walk up to a chime and press E. Only the closest chime in range rings.
    //
    // nextIndex counts how many correct chimes have been rung in a row. A wrong chime plays a short
    // failure sound and flash and sets nextIndex back to 0 (no damage). After three correct chimes the
    // chest opens once and the log inside is unlocked. Collecting it still needs E near the open
    // chest, which LogCollection handles like every other log.
    //
    // Runs just after LogCollection (-50), so when a log is in reach E belongs to the log, and the
    // press that solves the puzzle cannot also collect the log in the same frame.
    [DefaultExecutionOrder(-40)]
    public class ChimePuzzle : MonoBehaviour
    {
        [Header("Puzzle")]
        public string[] correctOrder = { "Moon", "Leaf", "Sun" };
        public ChimeInteract[] chimes;
        [Tooltip("How close Ryu must be to a chime to ring it.")]
        public float interactionDistance = 1f;
        public int nextIndex;
        public bool solved;

        [Header("References")]
        public BambooPlayerController player;
        public Collider2D playerCollider;
        public LogCollection logCollection;
        public CollectibleLog rewardLog;
        public SpriteRenderer chest;
        public Sprite chestOpenEmpty;
        [Tooltip("The small E key shown above the chime that would ring.")]
        public SpriteRenderer keyPrompt;
        public Vector2 promptOffset = new Vector2(0f, 1.75f);

        [Header("Sounds")]
        public AudioSource feedback;
        public AudioClip wrongSound;
        public AudioClip chestOpenSound;

        // Start is called before the first frame update
        void Start()
        {
            player.Respawned += ResetSequence;
            keyPrompt.enabled = false;
            // The log stays hidden and locked inside the closed chest until the puzzle is solved.
            if (!solved)
            {
                rewardLog.locked = true;
                rewardLog.GetComponent<SpriteRenderer>().enabled = false;
            }
        }

        void OnDestroy()
        {
            if (player != null)
                player.Respawned -= ResetSequence;
        }

        // Update is called once per frame
        void Update()
        {
            ChimeInteract nearest = CanInteract() ? FindClosestChime() : null;

            // Tell LogCollection (and through it the camera's E zoom) that E is in use here.
            logCollection.OtherInteractionInRange = nearest != null;
            keyPrompt.enabled = nearest != null;
            if (nearest == null)
                return;

            keyPrompt.transform.position = nearest.transform.position + (Vector3)promptOffset;
            if (Input.GetKeyDown(KeyCode.E))
                Press(nearest);
        }

        private bool CanInteract()
        {
            // Paused, dead, in a cutscene, already solved, or E is busy with a log: no chime input.
            return !solved && Time.timeScale > 0f && !player.IsDead && !player.ControlsLocked
                && !logCollection.BusyWithLog;
        }

        private ChimeInteract FindClosestChime()
        {
            Vector2 from = playerCollider.bounds.center;
            ChimeInteract closest = null;
            float bestDistance = interactionDistance;
            foreach (ChimeInteract chime in chimes)
            {
                float distance = Vector2.Distance(from, chime.InteractionPoint);
                if (distance <= bestDistance)
                {
                    closest = chime;
                    bestDistance = distance;
                }
            }
            return closest;
        }

        private void Press(ChimeInteract chime)
        {
            chime.Ring();

            if (chime.symbol != correctOrder[nextIndex])
            {
                // Wrong: brief feedback, then start the sequence again.
                nextIndex = 0;
                chime.ShowWrong();
                feedback.PlayOneShot(wrongSound);
                return;
            }

            nextIndex++;
            if (nextIndex == correctOrder.Length)
                OpenChest();
        }

        private void OpenChest()
        {
            solved = true;
            keyPrompt.enabled = false;
            logCollection.OtherInteractionInRange = false;
            chest.sprite = chestOpenEmpty;
            feedback.PlayOneShot(chestOpenSound);

            // Show the log inside the chest and let LogCollection offer it with E.
            rewardLog.GetComponent<SpriteRenderer>().enabled = true;
            rewardLog.locked = false;
        }

        // On respawn an unfinished sequence starts again. A solved puzzle stays solved.
        private void ResetSequence()
        {
            if (!solved)
                nextIndex = 0;
        }
    }
}
