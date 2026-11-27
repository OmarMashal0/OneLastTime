using System.Collections;
using TMPro;
using UnityEngine;

namespace Level3Bamboo
{
    // The tea rest stop in the middle of the bamboo trail (same idea as Lab 9's NPCDialogue trigger).
    //
    // 1. Ryu walks into this object's BoxCollider2D trigger (first time only).
    // 2. He slows to a stop on the ground and his controls are locked.
    // 3. The camera stops following him and glides in on the two travellers; black bars and a dark tint
    //    fade in, the music gets quieter and a low drone fades in.
    // 4. The four lines play one at a time: a cloud bubble above the speaker, text typed letter by letter,
    //    the speaker's Animator plays a talking state while the listener stays in Idle.
    // 5. Everything glides back, the camera follows Ryu again and he can walk on.
    //
    // If Ryu dies, or this object is disabled or unloaded, RestoreNow() puts everything back at once.
    // The pause button sets Time.timeScale to 0, which freezes this coroutine until the game resumes.
    public class TeaRestConversation : MonoBehaviour
    {
        [System.Serializable]
        public class Line
        {
            public bool womanSpeaks;
            [TextArea] public string text;
            [Tooltip("Animator state the speaker plays during this line.")]
            public string talkState = "Talk";
        }

        [Header("People")]
        public BambooPlayerController player;
        public Animator woman;
        public Animator traveller;

        [Header("Dialogue")]
        public Line[] lines;
        public GameObject womanCloud;
        public TMP_Text womanText;
        public GameObject travellerCloud;
        public TMP_Text travellerText;
        public float lettersPerSecond = 24f;
        [Tooltip("Reading time after a line is fully shown: this many seconds plus extra per letter.")]
        public float readSeconds = 1.4f;
        public float readSecondsPerLetter = 0.03f;

        [Header("Camera")]
        public Camera cam;
        public CameraFollow2D follow;
        [Tooltip("Where the camera looks during the conversation (between the two travellers).")]
        public Transform cameraFocus;
        public float conversationZoom = 2.2f;
        public float blendSeconds = 1.4f;

        [Header("Cinematic look")]
        [Tooltip("Canvas group holding the dark tint and the two black bars. Alpha 0 = hidden.")]
        public CanvasGroup cinematicLook;
        [Tooltip("These draw above the dark tint during the conversation (travellers, lanterns, glows).")]
        public SpriteRenderer[] keepBright;
        public int brightOrderBoost = 60;
        public TeaLanternGlow[] lanterns;

        [Header("Audio")]
        public AudioSource music;
        [Range(0f, 1f)] public float musicVolumeDuringTalk = 0.35f;
        public AudioSource drone;
        [Range(0f, 1f)] public float droneVolume = 0.35f;

        [Header("Dialogue sounds (quiet)")]
        public AudioSource dialogueSound;
        public AudioClip openClip;      // the box opens
        public AudioClip lineClip;      // a new line starts
        public AudioClip tickClip;      // a soft typing tick
        [Min(1)] public int lettersPerTick = 4;   // one tick every few letters, not every letter

        private bool played;
        private bool running;

        // True while the conversation has the camera (the level title waits for it).
        public bool Running => running;
        private Vector3 savedCameraPosition;
        private float savedCameraSize;
        private float savedMusicVolume;
        private int[] savedOrders;

        // Start is called before the first frame update
        void Start()
        {
            HideClouds();
            cinematicLook.alpha = 0f;
            woman.Play("Idle");
            traveller.Play("Idle", 0, 0.45f);
        }

        // Update is called once per frame
        void Update()
        {
            if (!running)
                return;

            // The drone pauses with the game; the music is never paused by the pause button.
            if (Time.timeScale <= 0f) drone.Pause(); else drone.UnPause();

            if (player.IsDead)
                RestoreNow();
        }

        private void OnDisable()
        {
            RestoreNow();
        }

        // Called by the Skip button: end the talk now with the normal finish
        // (camera, zoom, lighting, music, drone and Ryu's controls all go back).
        public void Skip()
        {
            RestoreNow();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (played || other.tag != "Player" || player.IsDead)
                return;

            played = true;
            StartCoroutine(PlayConversation());
        }

        private IEnumerator PlayConversation()
        {
            running = true;

            // Ryu slows down and lands; wait (at most 2 seconds) until he stands still on the ground.
            player.LockControls(true);
            float waited = 0f;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            while (waited < 2f && !(player.IsGrounded && Mathf.Abs(body.velocity.x) < 0.05f))
            {
                waited += Time.deltaTime;
                yield return null;
            }

            // Remember the gameplay framing and audio, then take over the camera.
            savedCameraPosition = cam.transform.position;
            savedCameraSize = cam.orthographicSize;
            savedMusicVolume = music.volume;
            follow.enabled = false;

            savedOrders = new int[keepBright.Length];
            for (int i = 0; i < keepBright.Length; i++)
            {
                savedOrders[i] = keepBright[i].sortingOrder;
                keepBright[i].sortingOrder += brightOrderBoost;
            }
            foreach (TeaLanternGlow lantern in lanterns)
                lantern.conversationGlow = true;
            drone.volume = 0f;
            drone.Play();

            Vector3 focus = new Vector3(cameraFocus.position.x, cameraFocus.position.y, savedCameraPosition.z);
            PlayDialogueSound(openClip, 0.5f);
            yield return Blend(savedCameraPosition, focus, savedCameraSize, conversationZoom, 0f, 1f);

            foreach (Line line in lines)
                yield return Say(line);

            yield return Blend(focus, savedCameraPosition, conversationZoom, savedCameraSize, 1f, 0f);
            RestoreNow();
        }

        // Moves and zooms the camera and fades the cinematic look and audio together.
        private IEnumerator Blend(Vector3 fromPosition, Vector3 toPosition, float fromSize, float toSize,
            float fromLook, float toLook)
        {
            float time = 0f;
            while (time < blendSeconds)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, time / blendSeconds);
                cam.transform.position = Vector3.Lerp(fromPosition, toPosition, t);
                cam.orthographicSize = Mathf.Lerp(fromSize, toSize, t);

                float look = Mathf.Lerp(fromLook, toLook, t);
                cinematicLook.alpha = look;
                music.volume = Mathf.Lerp(savedMusicVolume, savedMusicVolume * musicVolumeDuringTalk, look);
                drone.volume = droneVolume * look;
                yield return null;
            }
        }

        private IEnumerator Say(Line line)
        {
            Animator speaker = line.womanSpeaks ? woman : traveller;
            Animator listener = line.womanSpeaks ? traveller : woman;
            GameObject cloud = line.womanSpeaks ? womanCloud : travellerCloud;
            TMP_Text text = line.womanSpeaks ? womanText : travellerText;

            speaker.Play(line.talkState);
            listener.Play("Idle");

            // Type the line letter by letter (Lab 9 style). Time.deltaTime is 0 while paused.
            text.text = line.text;
            text.maxVisibleCharacters = 0;
            cloud.SetActive(true);
            PlayDialogueSound(lineClip, 0.45f);
            float shown = 0f;
            int nextTick = lettersPerTick;
            while (shown < line.text.Length)
            {
                shown += Time.deltaTime * lettersPerSecond;
                text.maxVisibleCharacters = (int)shown;
                if (shown >= nextTick)
                {
                    PlayDialogueSound(tickClip, 0.12f);
                    nextTick += lettersPerTick;
                }
                yield return null;
            }
            text.maxVisibleCharacters = line.text.Length;

            yield return new WaitForSeconds(readSeconds + readSecondsPerLetter * line.text.Length);
            cloud.SetActive(false);
            speaker.Play("Idle");
            yield return new WaitForSeconds(0.4f);
        }

        // Plays a dialogue sound once, quietly (all three clips use this).
        private void PlayDialogueSound(AudioClip clip, float volume)
        {
            if (dialogueSound != null && clip != null)
                dialogueSound.PlayOneShot(clip, volume);
        }

        // Puts the camera, look, audio and Ryu back to normal. Safe to call more than once.
        private void RestoreNow()
        {
            if (!running)
                return;

            running = false;
            StopAllCoroutines();
            if (womanCloud != null && travellerCloud != null)
                HideClouds();
            if (woman != null && woman.isActiveAndEnabled) woman.Play("Idle");
            if (traveller != null && traveller.isActiveAndEnabled) traveller.Play("Idle");

            // The "!= null" checks matter when the scene is being unloaded and objects are already gone.
            if (savedOrders != null)
            {
                for (int i = 0; i < keepBright.Length; i++)
                    if (keepBright[i] != null)
                        keepBright[i].sortingOrder = savedOrders[i];
                savedOrders = null;

                if (cam != null)
                {
                    cam.transform.position = savedCameraPosition;
                    cam.orthographicSize = savedCameraSize;
                }
                if (music != null)
                    music.volume = savedMusicVolume;
            }
            foreach (TeaLanternGlow lantern in lanterns)
                if (lantern != null)
                    lantern.conversationGlow = false;
            if (cinematicLook != null) cinematicLook.alpha = 0f;
            if (drone != null) drone.Stop();
            if (follow != null) follow.enabled = true;
            if (player != null) player.LockControls(false);
        }

        private void HideClouds()
        {
            womanCloud.SetActive(false);
            travellerCloud.SetActive(false);
        }
    }
}
