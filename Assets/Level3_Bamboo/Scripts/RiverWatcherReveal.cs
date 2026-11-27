using System.Collections;
using UnityEngine;

namespace Level3Bamboo.River
{
    // A hooded stranger stands behind the tree trunk in the distant forest (the trunk is drawn in front of her).
    // The first time in a level run that Ryu stands safely on the far bank (river crossed, grounded, alive),
    // one coroutine plays a short cinematic:
    //   1. Ryu's movement and attacks are locked.
    //   2. The camera pans and zooms in on the trunk.
    //   3. Everything else fades to near-black (a black sprite drawn in front of the scenery). The stranger,
    //      the trunk and a soft light behind her are drawn in front of the black, so her silhouette stays visible.
    //   4. The light behind her brightens briefly: it only rims the edge of the hood (her face stays in shadow).
    //   5. She peeks out (frames 2-4) and withdraws (frames 5-6).
    //   6. Lighting, camera follow, zoom, music and Ryu's controls are restored.
    // Pause freezes it (everything uses Time.deltaTime). Death or leaving the scene restores everything at once.
    public class RiverWatcherReveal : MonoBehaviour
    {
        [Header("Scene References")]
        public BambooPlayerController player;
        public RiverCrossing crossing;
        public CameraFollow2D follow;
        public Camera cam;
        public AudioSource music;

        [Header("Stranger")]
        public SpriteRenderer watcher;
        [Tooltip("The six frames of the hooded strip: 1 standing, 2 leaning, 3-4 peeking, 5 withdrawing, 6 standing.")]
        public Sprite[] poses;
        [Tooltip("How far (world units) she leans out from behind the trunk. Negative = to the left.")]
        public float peekDistance = -0.24f;
        [Tooltip("The tree trunk already in the scene, drawn in front of her.")]
        public SpriteRenderer trunk;
        [Tooltip("Soft light behind her. Brightens briefly to light the edge of the hood.")]
        public SpriteRenderer hoodLight;
        [Tooltip("A plain black sprite that darkens everything around her.")]
        public SpriteRenderer darkness;
        [Range(0f, 1f)] public float darkAlpha = 0.9f;
        [Tooltip("While the cinematic plays, the darkness and the stranger are drawn at this sorting order and above.")]
        public int cinematicOrder = 200;

        [Header("Trigger")]
        [Tooltip("Ryu must be grounded on the far bank, past this X position.")]
        public float triggerX = 37f;

        [Header("Camera")]
        [Tooltip("Camera size while it is zoomed in on her (smaller = closer).")]
        public float zoomSize = 1.6f;
        public float blendSeconds = 1.2f;
        public float watchSeconds = 1.4f;

        // Once per level run: a static flag that RiverProgress clears when the bamboo trail loads again.
        public static bool SeenThisRun;

        private bool revealing;
        private Vector3 hiddenPosition;
        private Vector3 savedCameraPosition;
        private float savedCameraSize;
        private float savedMusicVolume;
        private int watcherOrder, trunkOrder, lightOrder;
        private Color trunkColor, lightColor;

        void Start()
        {
            hiddenPosition = watcher.transform.position;
            watcher.sprite = poses[0];
            watcherOrder = watcher.sortingOrder;
            trunkOrder = trunk.sortingOrder;
            lightOrder = hoodLight.sortingOrder;
            trunkColor = trunk.color;
            lightColor = hoodLight.color;
            darkness.enabled = false;
        }

        void Update()
        {
            if (revealing)
            {
                if (player.IsDead)
                    RestoreNow();   // never leave the camera or the controls locked
                return;
            }
            if (SeenThisRun || Time.timeScale <= 0f)
                return;

            // Only on the safe far bank: the river is crossed (no timed jump is running), grounded and alive.
            if (crossing.Complete && player.IsGrounded && !player.IsDead && player.transform.position.x >= triggerX)
            {
                SeenThisRun = true;
                StartCoroutine(Reveal());
            }
        }

        private IEnumerator Reveal()
        {
            // 1. Stop Ryu and take the camera (like the tea conversation in the bamboo trail).
            revealing = true;
            player.LockControls(true);
            follow.enabled = false;
            savedCameraPosition = cam.transform.position;
            savedCameraSize = cam.orthographicSize;
            savedMusicVolume = music.volume;

            // Draw the darkness in front of the scenery, and her (with the light and the trunk) in front of it.
            darkness.sortingOrder = cinematicOrder;
            hoodLight.sortingOrder = cinematicOrder + 1;
            watcher.sortingOrder = cinematicOrder + 2;
            trunk.sortingOrder = cinematicOrder + 3;
            darkness.transform.position = new Vector3(hiddenPosition.x, hiddenPosition.y, 0f);
            darkness.enabled = true;

            Vector3 focus = new Vector3(hiddenPosition.x + peekDistance * 0.5f, hiddenPosition.y + 0.75f, savedCameraPosition.z);
            focus = KeepInsideLevel(focus, zoomSize);

            // 2-3. Pan and zoom in while the surroundings fade to near-black.
            for (float time = 0f; time < blendSeconds; time += Time.deltaTime)
            {
                Blend(Mathf.SmoothStep(0f, 1f, time / blendSeconds), focus);
                yield return null;
            }
            Blend(1f, focus);

            // 4-5. She peeks out; the light behind her rims the hood for a moment.
            yield return Slide(hiddenPosition, hiddenPosition + Vector3.right * peekDistance, 0.5f, poses[1]);
            watcher.sprite = poses[2];
            yield return Wait(0.35f);
            watcher.sprite = poses[3];
            yield return HoodLight(watchSeconds);

            // 5. She withdraws behind the trunk.
            watcher.sprite = poses[4];
            yield return Wait(0.3f);
            yield return Slide(hiddenPosition + Vector3.right * peekDistance, hiddenPosition, 0.5f, poses[5]);
            yield return Wait(0.3f);
            watcher.sprite = poses[0];

            // 6. Lighting and camera back, then everything else.
            for (float time = 0f; time < blendSeconds; time += Time.deltaTime)
            {
                Blend(1f - Mathf.SmoothStep(0f, 1f, time / blendSeconds), focus);
                yield return null;
            }
            RestoreNow();
        }

        // amount 0 = normal view, 1 = zoomed in and dark.
        private void Blend(float amount, Vector3 focus)
        {
            cam.transform.position = Vector3.Lerp(savedCameraPosition, focus, amount);
            cam.orthographicSize = Mathf.Lerp(savedCameraSize, zoomSize, amount);
            darkness.color = new Color(0f, 0f, 0f, darkAlpha * amount);
            trunk.color = Color.Lerp(trunkColor, new Color(0.22f, 0.22f, 0.26f, trunkColor.a), amount);
            music.volume = Mathf.Lerp(savedMusicVolume, savedMusicVolume * 0.4f, amount);
        }

        // The light behind her brightens, holds, and fades back (only her outline catches it).
        private IEnumerator HoodLight(float seconds)
        {
            for (float time = 0f; time < seconds; time += Time.deltaTime)
            {
                float glow = Mathf.Sin(Mathf.PI * time / seconds);
                hoodLight.color = Color.Lerp(lightColor, new Color(1f, 0.92f, 0.75f, 1f), glow);
                yield return null;
            }
            hoodLight.color = lightColor;
        }

        // Puts everything back. Called at the end, on death, and if the scene is left mid-way.
        private void RestoreNow()
        {
            if (!revealing)
                return;
            revealing = false;
            StopAllCoroutines();

            cam.transform.position = savedCameraPosition;
            cam.orthographicSize = savedCameraSize;
            music.volume = savedMusicVolume;
            darkness.enabled = false;
            watcher.sortingOrder = watcherOrder;
            trunk.sortingOrder = trunkOrder;
            hoodLight.sortingOrder = lightOrder;
            trunk.color = trunkColor;
            hoodLight.color = lightColor;
            watcher.transform.position = hiddenPosition;
            watcher.sprite = poses[0];

            follow.enabled = true;
            player.LockControls(false);
        }

        void OnDisable()
        {
            RestoreNow();
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
