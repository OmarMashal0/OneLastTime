
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level3Bamboo
{
    public class SwampHallucination : MonoBehaviour
    {
        // References from Unity Inspector
        [Header("Story")]
        public BambooPlayerController player;
        public SwampCageEncounter swampSite;
        public SwampDialogue dialogue;
        public LogCollection logs;

        [Header("Voice and music")]
        public AudioSource voice;
        public AudioClip voiceClip;
        public AudioSource music;
        [Range(0f, 1f)] public float musicVolumeDuringVoice = 0.25f;
        [Tooltip("Optional low drone under the voice.")]
        public AudioSource drone;
        [Range(0f, 1f)] public float droneVolume = 0.35f;

        [Header("Camera")]
        public Camera cam;
        public CameraFollow2D follow;
        public float zoomSize = 3.1f;
        public Vector2 focusOffset = new Vector2(0f, 0.9f);
        [Tooltip("Strength of the first shake (world units).")]
        public float shakeStrength = 0.16f;
        public float shakeSeconds = 1.3f;
        [Tooltip("Small tremor that stays while the voice plays.")]
        public float tremorStrength = 0.025f;
        [Tooltip("Slow camera tilt in degrees.")]
        public float tiltDegrees = 1.5f;

        [Header("Screen")]
        public Image darkOverlay;
        [Range(0f, 1f)] public float overlayAlpha = 0.42f;
        [Range(0f, 0.3f)] public float pulseAmount = 0.08f;

        [Header("Subtitle")]
        [Tooltip("English subtitle shown while the Japanese voice plays.")]
        public TMP_Text subtitle;
        public string subtitleText = "Dad… where are you?";

        [Header("Timing (seconds)")]
        public float fadeInSeconds = 1.2f;
        public float holdAfterVoice = 0.2f;
        public float fadeOutSeconds = 1.4f;

        [Header("Sanity")]
        public int sanityAfterEvent = 80;

        // Internal variables
        private bool played;
        private bool running;
        private bool controlsLocked;
        private bool cameraTaken;
        private bool audioPaused;

        private Vector3 savedCameraPosition;
        private Quaternion savedCameraRotation;
        private float savedCameraSize;
        private float savedMusicVolume;
        private Vector3 focusPosition;

        private void Start()
        {
            SetOverlay(0f);
            SetSubtitle(0f);
        }

        private void OnEnable()
        {
            logs.LogCollected += OnLogCollected;
        }

        private void OnDisable()
        {
            logs.LogCollected -= OnLogCollected;
            RestoreNow();
        }

        private void Update()
        {
            if (!running) return;

            // Pause or resume audio with the game
            bool paused = Time.timeScale <= 0f;

            if (paused != audioPaused)
            {
                audioPaused = paused;

                if (paused)
                {
                    voice.Pause();
                    if (drone != null) drone.Pause();
                }
                else
                {
                    voice.UnPause();
                    if (drone != null) drone.UnPause();
                }
            }

            if (player.IsDead)
                RestoreNow();
        }

        // Start after Swamp Thing is defeated
        // and both logs are collected
        private void OnLogCollected(
            CollectibleLog log, int count, int total)
        {
            bool finishedSwamp = swampSite.Defeated
                && !swampSite.log.gameObject.activeSelf
                && !swampSite.deathReward.gameObject.activeSelf;

            if (played || !finishedSwamp) return;

            played = true;
            StartCoroutine(PlaySequence());
        }

        // Main hallucination sequence
        private IEnumerator PlaySequence()
        {
            // 1. Wait until Ryu is ready
            while (logs.BusyWithLog ||
                   dialogue.IsShowing ||
                   player.IsDead ||
                   !player.IsGrounded)
            {
                yield return null;
            }

            running = true;
            player.LockControls(true);
            controlsLocked = true;

            // Wait for Ryu to stop moving
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            float wait = 0f;

            while (wait < 1f && Mathf.Abs(body.velocity.x) > 0.05f)
            {
                wait += Time.deltaTime;
                yield return null;
            }

            // 2. Prepare camera and sounds
            SaveCameraAndMusic();
            StartSounds();

            // 3. Fade in hallucination
            float elapsed = 0f;
            bool sanityChanged = false;

            while (elapsed < fadeInSeconds)
            {
                elapsed += Time.deltaTime;

                float amount = Mathf.SmoothStep(
                    0f, 1f, elapsed / fadeInSeconds);

                SetCamera(amount, elapsed, true);
                SetOverlay(OverlayAt(elapsed) * amount);
                SetAudio(amount);
                SetSubtitle(Mathf.Clamp01(elapsed / 0.4f));

                // Change sanity once
                if (!sanityChanged && amount >= 0.5f)
                {
                    player.SetSanity(sanityAfterEvent);
                    sanityChanged = true;
                }

                yield return null;
            }

            if (!sanityChanged)
                player.SetSanity(sanityAfterEvent);

            // 4. Wait until the voice finishes
            float remaining =
                Mathf.Max(0f, voiceClip.length - elapsed)
                + holdAfterVoice;

            while (remaining > 0f)
            {
                remaining -= Time.deltaTime;
                elapsed += Time.deltaTime;

                cam.transform.position =
                    focusPosition + Shake(elapsed);

                cam.transform.rotation =
                    savedCameraRotation *
                    Quaternion.Euler(0f, 0f, Tilt(elapsed));

                SetOverlay(OverlayAt(elapsed));
                SetSubtitle(1f);

                yield return null;
            }

            // 5. Fade out hallucination
            float fade = 0f;

            while (fade < fadeOutSeconds)
            {
                fade += Time.deltaTime;
                elapsed += Time.deltaTime;

                float amount = Mathf.SmoothStep(
                    0f, 1f, fade / fadeOutSeconds);

                SetCamera(amount, elapsed, false);
                SetOverlay(OverlayAt(elapsed) * (1f - amount));
                SetAudio(1f - amount);

                SetSubtitle(
                    1f - Mathf.Clamp01(fade / 0.5f));

                yield return null;
            }

            // 6. Return everything to normal
            RestoreNow();
        }

        // Save the normal camera and music settings
        private void SaveCameraAndMusic()
        {
            savedCameraPosition = cam.transform.position;
            savedCameraRotation = cam.transform.rotation;
            savedCameraSize = cam.orthographicSize;
            savedMusicVolume = music.volume;

            follow.enabled = false;
            cameraTaken = true;

            focusPosition = new Vector3(
                player.transform.position.x + focusOffset.x,
                player.transform.position.y + focusOffset.y,
                savedCameraPosition.z);

            focusPosition = KeepInsideLevel(focusPosition);
        }

        // Play the voice and background drone
        private void StartSounds()
        {
            if (subtitle != null)
                subtitle.text = subtitleText;

            voice.clip = voiceClip;
            voice.loop = false;
            voice.Play();

            if (drone != null)
            {
                drone.volume = 0f;
                drone.Play();
            }
        }

        // Move and zoom the camera smoothly
        private void SetCamera(
            float amount, float time, bool fadingIn)
        {
            if (fadingIn)
            {
                cam.transform.position =
                    Vector3.Lerp(
                        savedCameraPosition,
                        focusPosition,
                        amount)
                    + Shake(time) * amount;

                cam.transform.rotation =
                    savedCameraRotation *
                    Quaternion.Euler(
                        0f, 0f, Tilt(time) * amount);

                cam.orthographicSize =
                    Mathf.Lerp(
                        savedCameraSize,
                        zoomSize,
                        amount);
            }
            else
            {
                cam.transform.position =
                    Vector3.Lerp(
                        focusPosition,
                        savedCameraPosition,
                        amount);

                cam.transform.rotation =
                    savedCameraRotation *
                    Quaternion.Euler(
                        0f, 0f,
                        Tilt(time) * (1f - amount));

                cam.orthographicSize =
                    Mathf.Lerp(
                        zoomSize,
                        savedCameraSize,
                        amount);
            }
        }

        // Prevent the camera from showing outside the level
        private Vector3 KeepInsideLevel(Vector3 center)
        {
            float halfHeight = zoomSize;
            float halfWidth = zoomSize * cam.aspect;

            float margin =
                halfWidth * Mathf.Sin(
                    tiltDegrees * Mathf.Deg2Rad)
                + shakeStrength;

            center.y = Mathf.Clamp(
                center.y,
                follow.groundBottomY + halfHeight + margin,
                follow.worldTopY - halfHeight - margin);

            center.x = Mathf.Clamp(
                center.x,
                follow.minWorldX + halfWidth + margin,
                follow.maxWorldX - halfWidth - margin);

            return center;
        }

        // Camera shaking effect
        private Vector3 Shake(float time)
        {
            float strength = tremorStrength;

            if (time < shakeSeconds)
            {
                strength = Mathf.Lerp(
                    shakeStrength,
                    tremorStrength,
                    time / shakeSeconds);
            }

            float x =
                (Mathf.PerlinNoise(time * 16f, 0.3f) - 0.5f) * 2f;

            float y =
                (Mathf.PerlinNoise(0.7f, time * 16f) - 0.5f) * 2f;

            return new Vector3(x, y, 0f) * strength;
        }

        // Slowly tilt the camera
        private float Tilt(float time)
        {
            return Mathf.Sin(time * 1.3f) * tiltDegrees;
        }

        // Make the darkness pulse like a heartbeat
        private float OverlayAt(float time)
        {
            float beat = Mathf.Pow(
                Mathf.Abs(Mathf.Sin(time * 2.6f)), 6f);

            return overlayAlpha + pulseAmount * beat;
        }

        // Fade music down and drone up
        private void SetAudio(float amount)
        {
            music.volume = Mathf.Lerp(
                savedMusicVolume,
                savedMusicVolume * musicVolumeDuringVoice,
                amount);

            if (drone != null)
                drone.volume = droneVolume * amount;
        }

        // Change subtitle visibility
        private void SetSubtitle(float alpha)
        {
            if (subtitle != null)
                subtitle.alpha = alpha;
        }

        // Change dark overlay visibility
        private void SetOverlay(float alpha)
        {
            if (darkOverlay == null) return;

            Color color = darkOverlay.color;
            color.a = alpha;
            darkOverlay.color = color;
        }

        // Restore the game after hallucination
        private void RestoreNow()
        {
            if (!running) return;

            running = false;
            StopAllCoroutines();

            if (voice != null) voice.Stop();
            if (drone != null) drone.Stop();

            if (cameraTaken)
            {
                if (cam != null)
                {
                    cam.transform.position = savedCameraPosition;
                    cam.transform.rotation = savedCameraRotation;
                    cam.orthographicSize = savedCameraSize;
                }

                if (music != null)
                    music.volume = savedMusicVolume;

                if (follow != null)
                    follow.enabled = true;

                cameraTaken = false;
            }

            SetOverlay(0f);
            SetSubtitle(0f);

            if (controlsLocked && player != null)
                player.LockControls(false);

            controlsLocked = false;
        }
    }
}
