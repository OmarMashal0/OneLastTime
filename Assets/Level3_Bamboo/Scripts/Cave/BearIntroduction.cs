using System.Collections;
using UnityEngine;

namespace Level3Bamboo
{
    // Plays after every fall into the cave, once Ryu stands on the cave floor:
    // locks his controls, points the EXISTING camera (CameraFollow2D) at the sleeping family and
    // zooms in, shows a short warning, then points the camera back at Ryu and gives the controls back.
    // The camera script does all the moving and smoothing; this script only changes its target and zoom.
    // Every step waits with Time.deltaTime, so pausing the game also pauses the introduction.
    // Listening tells the bear when running starts to count: after the introduction, and only once
    // Ryu has let go of left/right (or after a short grace time), so a key held from before is not punished.
    public class BearIntroduction : MonoBehaviour
    {
        public BambooPlayerController player;
        public CaveZone caveZone;
        public CameraFollow2D follow;
        [Tooltip("An empty object in the middle of the mother and her two cubs.")]
        public Transform family;
        [Tooltip("The caption 'Walk quietly. Hold Shift to move slowly. Running will wake her.'")]
        public GameObject caption;
        public float zoomSize = 3.6f;
        [Tooltip("Time the camera gets to glide to the family, and back to Ryu.")]
        public float moveSeconds = 1.3f;
        public float holdSeconds = 2.4f;
        [Tooltip("Short pause after landing so the camera has caught up with Ryu first.")]
        public float settleSeconds = 0.8f;
        [Tooltip("After the introduction, running counts once left/right is released, or after this many seconds.")]
        public float graceSeconds = 1f;

        public bool Played { get; private set; }
        public bool Finished { get; private set; }
        public bool Listening { get; private set; }

        private bool playing;
        private float normalZoom;
        private float graceTimer;

        void Start()
        {
            caption.SetActive(false);
        }

        void Update()
        {
            if (!Played && caveZone.PlayerInside && player.IsGrounded && !player.IsDead && Time.timeScale > 0f)
            {
                Played = true;
                StartCoroutine(Introduce());
            }

            if (Finished && !Listening && Time.timeScale > 0f)
            {
                graceTimer += Time.deltaTime;
                if (Input.GetAxisRaw("Horizontal") == 0f || graceTimer >= graceSeconds)
                    Listening = true;
            }
        }

        private IEnumerator Introduce()
        {
            playing = true;
            player.LockControls(true);
            yield return Wait(settleSeconds);

            // 1. The camera glides to the family and zooms in.
            normalZoom = follow.TargetZoom;
            follow.Target = family;
            follow.SetZoom(zoomSize);
            yield return Wait(moveSeconds);

            // 2. Hold so the player notices them.
            caption.SetActive(true);
            yield return Wait(holdSeconds);
            caption.SetActive(false);

            // 3. Back to Ryu, then give the controls back.
            GiveCameraBack();
            yield return Wait(moveSeconds);
            player.LockControls(false);
            playing = false;
            Finished = true;
            graceTimer = 0f;
        }

        // Ready to play again on the next fall. Called by CaveZone when the cave resets.
        public void ResetIntro()
        {
            StopAllCoroutines();
            if (playing)
            {
                // Stopped half way: camera back on Ryu and controls back.
                GiveCameraBack();
                player.LockControls(false);
            }
            caption.SetActive(false);
            playing = false;
            Played = false;
            Finished = false;
            Listening = false;
        }

        private void GiveCameraBack()
        {
            follow.Target = player.transform;
            if (normalZoom > 0f)
                follow.SetZoom(normalZoom);
        }

        // A wait that also freezes while the game is paused.
        private IEnumerator Wait(float seconds)
        {
            for (float time = 0f; time < seconds; time += Time.deltaTime)
                yield return null;
        }
    }
}
