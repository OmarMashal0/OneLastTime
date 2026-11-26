using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level3Bamboo.River
{
    // The river puzzle. Ryu arrives with the logs from the bamboo trail (RiverProgress). At the log cradle he
    // presses E: if he has all 6, the logs drop into the river. Falling below the water line kills him; on
    // respawn the logs are rebuilt. Reaching the far bank saves the far checkpoint and shows "River crossed".
    [DefaultExecutionOrder(-40)]
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
        public AudioClip crackClip;
        public AudioClip breakClip;
        public AudioClip splashClip;
        public GameObject ripplePrefab;
        public Transform effectsRoot;
        public GameObject prompt;
        public Image promptKey;
        public Sprite keyIdle;
        public Sprite keyHighlighted;
        public Sprite keyPressed;
        public TMP_Text feedback;
        public TMP_Text counter;
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
                ShowMessage("River crossed", 3f);
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

        private void TryPlace()
        {
            if (Collected != RiverProgress.RequiredLogs)
            {
                ShowMessage("Collect all 6 logs first.", 2.4f);
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
                Splash(logs[i].transform.position, i == 0);
            }
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
            if (crackClip != null)
                sound.PlayOneShot(crackClip, 0.55f);
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
            if (audible && splashClip != null)
                sound.PlayOneShot(splashClip, 0.5f);
        }

        private void ShowMessage(string text, float seconds)
        {
            feedback.text = text;
            feedback.gameObject.SetActive(true);
            messageUntil = Time.time + seconds;
        }
    }
}

