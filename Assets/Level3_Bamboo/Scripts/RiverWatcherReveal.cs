using System.Collections;
using UnityEngine;

namespace Level3Bamboo.River
{
    // A dark, anonymous figure stands behind a tree in the distant forest, mostly hidden by its trunk.
    // The first time Ryu stands safely on the far bank (after the river is crossed), the figure leans out,
    // watches him for a moment, then turns away and slips back behind the tree.
    // While this plays, Ryu's controls are locked and the camera zooms in hard on the figure; afterwards
    // the camera and the controls go back to normal. It only ever plays once (not again after a respawn).
    public class RiverWatcherReveal : MonoBehaviour
    {
        [Header("Scene References")]
        public BambooPlayerController player;
        public RiverCrossing crossing;
        public CameraFollow2D follow;
        public Camera cam;

        [Header("Watcher")]
        public SpriteRenderer watcher;
        [Tooltip("The four poses from the watcher sheet: 1 standing, 2 leaning, 3 leaning further, 4 turning away.")]
        public Sprite[] poses;
        [Tooltip("How far (world units) he leans out from behind the trunk. Negative = to the left.")]
        public float peekDistance = -0.24f;

        [Header("Trigger")]
        [Tooltip("Ryu must be grounded on the far bank, past this X position.")]
        public float triggerX = 37f;

        [Header("Camera")]
        [Tooltip("Camera size while it is zoomed in on the watcher (smaller = closer).")]
        public float zoomSize = 2.1f;
        public float blendSeconds = 1.1f;
        public float watchSeconds = 1.6f;

        private bool played;
        private bool revealing;
        private Vector3 hiddenPosition;

        // Start is called before the first frame update
        void Start()
        {
            hiddenPosition = watcher.transform.position;
            watcher.sprite = poses[0];
        }

        // Update is called once per frame
        void Update()
        {
            if (played || Time.timeScale <= 0f)
                return;

            // Only on the safe far bank: the river is crossed, Ryu is standing on the ground and alive.
            if (crossing.Complete && player.IsGrounded && !player.IsDead && player.transform.position.x >= triggerX)
            {
                played = true;
                StartCoroutine(Reveal());
            }
        }

        private IEnumerator Reveal()
        {
            revealing = true;
            player.LockControls(true);

            // Take the camera over, like the tea conversation in the bamboo trail.
            follow.enabled = false;
            Vector3 startPosition = cam.transform.position;
            float startSize = cam.orthographicSize;
            // Aim at the watcher himself (a little above his feet), not halfway between him and Ryu.
            Vector3 focus = new Vector3(hiddenPosition.x + peekDistance * 0.5f, hiddenPosition.y + 0.9f, startPosition.z);
            focus = KeepInsideLevel(focus, zoomSize);

            // 1. Strong zoom toward the watcher.
            yield return MoveCamera(startPosition, focus, startSize, zoomSize);

            // 2. He leans out from behind the tree and watches Ryu.
            yield return Slide(hiddenPosition, hiddenPosition + Vector3.right * peekDistance, 0.6f, poses[1]);
            watcher.sprite = poses[2];
            yield return Wait(watchSeconds);

            // 3. He turns away and slips back behind the trunk.
            yield return Slide(hiddenPosition + Vector3.right * peekDistance, hiddenPosition, 0.6f, poses[3]);
            yield return Wait(0.4f);
            watcher.sprite = poses[0];

            // 4. Camera back to normal, then give Ryu his controls again.
            yield return MoveCamera(focus, startPosition, zoomSize, startSize);
            follow.enabled = true;
            player.LockControls(false);
            revealing = false;
        }

        // Safety: if the scene is left in the middle of the reveal, never leave the camera or Ryu stuck.
        void OnDisable()
        {
            if (!revealing)
                return;
            revealing = false;
            follow.enabled = true;
            player.LockControls(false);
        }

        // Moves and zooms the camera smoothly. Uses Time.deltaTime, so it freezes while the game is paused.
        private IEnumerator MoveCamera(Vector3 from, Vector3 to, float fromSize, float toSize)
        {
            for (float time = 0f; time < blendSeconds; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, time / blendSeconds);
                cam.transform.position = Vector3.Lerp(from, to, t);
                cam.orthographicSize = Mathf.Lerp(fromSize, toSize, t);
                yield return null;
            }
            cam.transform.position = to;
            cam.orthographicSize = toSize;
        }

        private IEnumerator Slide(Vector3 from, Vector3 to, float seconds, Sprite pose)
        {
            watcher.sprite = pose;
            for (float time = 0f; time < seconds; time += Time.deltaTime)
            {
                watcher.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, time / seconds));
                yield return null;
            }
            watcher.transform.position = to;
        }

        // A wait that also freezes while the game is paused.
        private IEnumerator Wait(float seconds)
        {
            for (float time = 0f; time < seconds; time += Time.deltaTime)
                yield return null;
        }

        // Keeps the zoomed camera inside the same edges CameraFollow2D uses, so no empty space shows.
        private Vector3 KeepInsideLevel(Vector3 center, float size)
        {
            float halfWidth = size * cam.aspect;
            center.x = Mathf.Clamp(center.x, follow.minWorldX + halfWidth, follow.maxWorldX - halfWidth);
            center.y = Mathf.Clamp(center.y, follow.groundBottomY + size, follow.worldTopY - size);
            return center;
        }
    }
}
