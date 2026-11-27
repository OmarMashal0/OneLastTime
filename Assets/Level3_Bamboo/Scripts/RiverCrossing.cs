using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level3Bamboo.River
{
    // The river puzzle. Ryu arrives with the logs from the bamboo trail (RiverProgress). At the log cradle he
    // presses E: if he has all 6, the logs drop into the river. Falling below the water line kills him; on
    // respawn the logs are rebuilt. Reaching the far bank saves the far checkpoint and shows "River crossed".
    public class RiverCrossing : MonoBehaviour
    {
        public BambooPlayerController player;
        public Collider2D PlayerCollider;
        public FloatingRiverLog[] logs;
        public Transform placement;
        public Transform nearCheckpoint;
        public Transform farCheckpoint;
        public AudioSource music;
        public AudioSource sound;
        public AudioSource ambience;
        private bool audioPaused;
        public AudioClip placeClip;      // wooden thump when the logs land in place
        public AudioClip[] crackClips;   // a log starts to crack (clips take turns)
        public AudioClip breakClip;
        public AudioClip[] splashClips;  // water splashes (clips take turns)
        public AudioClip notReadyClip;   // E at the cradle without all six logs
        private int nextClip;
        private float splashQuietUntil;  // stops two splashes playing on top of each other
        public GameObject ripplePrefab;
        public Transform effectsRoot;
        public GameObject prompt;
        public Image promptKey;
        public Sprite keyIdle;
        public Sprite keyHighlighted;
        public Sprite keyPressed;
        public TMP_Text feedback;
        public TMP_Text counter;
        [Header("Quest (same scroll as the bamboo trail)")]
        public BambooQuestScroll quest;
        public Sprite questIcon;
        public float interactionRange = 1.55f;
        public float deploymentSeconds = 0.85f;
        public float waterHeight = -0.56f;
        public float leftLip = 8.37f;
        public float rightLip = 26.19f;

        public int Collected { get; private set; }
        public bool ActiveAttempt => deployed && !complete && !player.IsDead;
        public bool Complete => complete;
        private bool deploying;
        private bool deployed;
        private bool complete;
        private bool nearSaved;
        private float messageUntil;
        private Coroutine deployment;
        private int questStage = -1;   // 0 = place the logs, 1 = cross, 2 = crossed (-1 = nothing shown yet)

        private void Start()
        {
            Collected = RiverProgress.Receive(player);
            music.mute = RiverProgress.MusicMuted;
            counter.text = $"Logs: {Collected}/{RiverProgress.RequiredLogs}";
            player.Respawned += ResetAfterFailure;
            foreach (FloatingRiverLog log in logs)
                log.Initialize(this);
        }

        private void OnDestroy()
        {
            player.Respawned -= ResetAfterFailure;
        }

        private void Update()
        {
            bool paused = Time.timeScale <= 0f;
            if (audioPaused != paused)
            {
                audioPaused = paused;
                if (paused)
                {
                    sound.Pause();
                    ambience.Pause();
                }
                else
                {
                    sound.UnPause();
                    ambience.UnPause();
                }
            }
            if (paused)
                return;
            UpdateQuest();
            ambience.volume = 0.22f * Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(-3f, leftLip, player.transform.position.x));

            float x = player.transform.position.x;
            var feet = PlayerCollider.bounds.min;
            if (!player.IsDead && x > leftLip && x < rightLip && feet.y < waterHeight)
            {
                Splash(new Vector3(x, waterHeight, 0f));
                player.TriggerDeath();
            }

            if (!nearSaved && !player.IsDead && player.IsGrounded && x >= nearCheckpoint.position.x && x < leftLip)
            {
                nearSaved = true;
                player.SetCheckpoint(nearCheckpoint.position);
            }

            if (deployed && !complete && !player.IsDead && player.IsGrounded && x >= farCheckpoint.position.x)
            {
                complete = true;
                player.SetCheckpoint(farCheckpoint.position);
            }

            if (feedback.gameObject.activeSelf && Time.time >= messageUntil)
                feedback.gameObject.SetActive(false);

            bool inRange = !player.IsDead && Vector2.Distance(player.transform.position, placement.position) <= interactionRange;
            prompt.SetActive(inRange && !deploying && !deployed || deploying && prompt.activeSelf);
            if (!inRange || deploying || deployed)
                return;

            promptKey.sprite = Mathf.Sin(Time.time * 5f) > 0.15f ? keyHighlighted : keyIdle;
            if (Input.GetKeyDown(KeyCode.E))
                TryPlace();
        }

        // Reads the real crossing state, so after a respawn or re-entry the text is still right.
        // The scroll only changes when the stage changes (not every frame).
        private void UpdateQuest()
        {
            int stage = 0;
            if (deployed)
                stage = 1;
            if (complete)
                stage = 2;
            if (quest == null || stage == questStage)
                return;

            questStage = stage;
            if (stage == 0)
                quest.Present("PLACE THE LOGS", "Place the six logs to cross the river.", questIcon);
            else if (stage == 1)
                quest.Present("CROSS THE RIVER", "Cross the river before the logs break.", questIcon);
            else
                quest.ShowDone("RIVER CROSSED", "You reached the far bank safely.", questIcon);
        }

        private void TryPlace()
        {
            if (Collected != RiverProgress.RequiredLogs)
            {
                ShowMessage("Collect all 6 logs first.", 2.4f);
                if (notReadyClip != null)
                    sound.PlayOneShot(notReadyClip, 0.35f);
                return;
            }

            deploying = true;
            promptKey.sprite = keyPressed;
            deployment = StartCoroutine(Deploy());
        }

        private IEnumerator Deploy()
        {
            yield return new WaitForSeconds(0.12f);
            prompt.SetActive(false);
            var positions = new Vector3[logs.Length];
            for (int i = 0; i < logs.Length; i++)
            {
                positions[i] = logs[i].art.localPosition;
                logs[i].gameObject.SetActive(true);
                logs[i].art.localPosition = positions[i] + Vector3.up * 0.85f;
            }
            float time = 0f;
            while (time < deploymentSeconds)
            {
                time += Time.deltaTime;
                for (int i = 0; i < logs.Length; i++)
                {
                    float fall = Mathf.Clamp01((time - i * 0.05f) / Mathf.Max(0.1f, deploymentSeconds - 0.25f));
                    logs[i].art.localPosition = positions[i] + Vector3.up * (0.85f * (1f - fall * fall));
                }
                yield return null;
            }
            for (int i = 0; i < logs.Length; i++)
            {
                logs[i].ResetLog(true);
                Splash(logs[i].transform.position, false);
            }
            // One wooden thump for the placement (not six splashes at once).
            if (placeClip != null)
                sound.PlayOneShot(placeClip, 0.7f);
            deployed = true;
            deploying = false;
            deployment = null;
        }

        private void ResetAfterFailure()
        {
            if (complete)
                return;
            if (deployment != null)
            {
                StopCoroutine(deployment);
                deployment = null;
                deployed = true;
                deploying = false;
            }
            foreach (Transform effect in effectsRoot)
                Destroy(effect.gameObject);
            sound.Stop();
            foreach (FloatingRiverLog log in logs)
                log.ResetLog(deployed);
        }

        public void PlayCrack()
        {
            AudioClip crack = Next(crackClips);
            if (crack != null)
                sound.PlayOneShot(crack, 0.55f);
        }

        public void PlayBreak(Vector3 position)
        {
            if (breakClip != null)
                sound.PlayOneShot(breakClip, 0.75f);
            Splash(position);
        }

        private void Splash(Vector3 position, bool audible = true)
        {
            position.y = waterHeight;
            if (ripplePrefab != null)
                Destroy(Instantiate(ripplePrefab, position, Quaternion.identity, effectsRoot), 0.75f);
            // A log breaking and Ryu falling in happen almost together: play only one splash.
            if (audible && Time.time >= splashQuietUntil)
            {
                AudioClip splash = Next(splashClips);
                if (splash != null)
                    sound.PlayOneShot(splash, 0.5f);
                splashQuietUntil = Time.time + 0.4f;
            }
        }

        // Picks the clips of an array in turn, so repeated cracks and splashes do not sound identical.
        private AudioClip Next(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;
            nextClip++;
            return clips[nextClip % clips.Length];
        }

        private void ShowMessage(string text, float seconds)
        {
            feedback.text = text;
            feedback.gameObject.SetActive(true);
            messageUntil = Time.time + seconds;
        }
    }
}

