using System.Collections;
using UnityEngine;
using OneLastTime.AI;
using OneLastTime.Player;
using OneLastTime.UI;

namespace OneLastTime.Systems
{
    public class CheckpointManager : MonoBehaviour
    {
        public static CheckpointManager Instance { get; private set; }

        [Header("Fade Settings")]
        [SerializeField] private float fadeDuration = 0.35f;

        private Checkpoint currentCheckpoint;
        private GameObject playerObj;
        private PlayerMovement playerMovement;
        private PlayerHealth playerHealth;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            FindPlayer();
        }

        private void FindPlayer()
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerObj = playerHealth.gameObject;
                playerMovement = playerHealth.GetComponent<PlayerMovement>();
                return;
            }

            playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement != null)
            {
                playerObj = playerMovement.gameObject;
                playerHealth = playerMovement.GetComponent<PlayerHealth>() ?? playerMovement.gameObject.AddComponent<PlayerHealth>();
            }
        }

        public void SetCheckpoint(Checkpoint checkpoint)
        {
            currentCheckpoint = checkpoint;
        }

        public void RespawnPlayer()
        {
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            FindPlayer();

            // 1. Fade to black
            var fader = ScreenFader.Instance;
            if (fader != null)
            {
                yield return fader.FadeToBlack(fadeDuration);
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
            }

            // 2. Destroy lingering bullets
            var bullets = FindObjectsOfType<GuardBullet>();
            foreach (var b in bullets)
            {
                if (b != null) Destroy(b.gameObject);
            }

            // 3. Reset all guards
            var guards = FindObjectsOfType<MuseumGuard>();
            foreach (var g in guards)
            {
                if (g != null) g.ResetGuard();
            }

            // 4. Reposition player at checkpoint
            Vector3 spawnPos = Vector3.zero;
            if (playerObj != null)
            {
                if (currentCheckpoint != null)
                {
                    spawnPos = currentCheckpoint.SpawnPosition;
                }
                else
                {
                    var cp = FindObjectOfType<Checkpoint>();
                    if (cp != null)
                    {
                        currentCheckpoint = cp;
                        spawnPos = cp.SpawnPosition;
                    }
                    else
                    {
                        var t6 = GameObject.Find("Tileset (2) _6");
                        float targetX = (t6 != null) ? t6.transform.position.x : -3.84f;
                        spawnPos = new Vector3(targetX, -23.15f, 0f);
                    }
                }

                Transform rootPlayer = playerObj.transform.root != null ? playerObj.transform.root : playerObj.transform;
                if (rootPlayer != null && (rootPlayer.name.IndexOf("ryu", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rootPlayer.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    rootPlayer.position = spawnPos;
                }
                playerObj.transform.position = spawnPos;

                var rb2d = playerObj.GetComponent<Rigidbody2D>();
                if (rb2d != null)
                {
                    rb2d.velocity = Vector2.zero;
                    rb2d.position = spawnPos;
                }
            }

            // 5. Restore player health, animation & movement
            if (playerHealth != null)
            {
                playerHealth.RestorePlayer();
            }

            // 6. Snap Camera
            var cam = Camera.main;
            if (cam != null)
            {
                var camFollow = cam.GetComponent<CameraFollow>();
                if (camFollow != null)
                {
                    if (spawnPos.y < -35f)
                    {
                        camFollow.SetLimits(-6.0f, 120.0f, -42.5f, -42.0f);
                    }
                    else if (spawnPos.y < -15f)
                    {
                        camFollow.SetLimits(-1.0f, 151.85f, -20.65f, -20.45f);
                    }
                    else
                    {
                        camFollow.SetLimits(0.0f, 151.85f, 0.0f, 3.0f);
                    }
                    camFollow.SnapToTarget();
                }
            }

            // 7. Reposition Raccoon companion if present (safe lookup, no undefined tag queries)
            var raccoon = GameObject.Find("Companion_Raccoon") ?? GameObject.Find("Raccoon");
            if (raccoon == null)
            {
                var allGos = FindObjectsOfType<GameObject>();
                foreach (var go in allGos)
                {
                    if (go.name.IndexOf("raccoon", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                        go.name.IndexOf("companion", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        raccoon = go;
                        break;
                    }
                }
            }
            if (raccoon != null && playerObj != null)
            {
                raccoon.transform.position = playerObj.transform.position + new Vector3(-1f, 0f, 0f);
            }

            // 8. Fade back in
            if (fader != null)
            {
                yield return fader.FadeFromBlack(fadeDuration);
            }
        }
    }
}

