using UnityEngine;

namespace Level3Bamboo
{
    // A trigger that covers the whole cave (and the hole above it). It also owns the cave "reset".
    // While Ryu is inside: Shift walks a little slower, the camera may follow him below the forest
    // floor, and the cave instruction shows. His respawn point is the Retry Point ABOVE the cave,
    // just before the cracked ground.
    // The cave resets (bear asleep at home, torch back on its bracket, cracked ground whole again,
    // introduction ready) when Ryu dies, and when he climbs out of the escape shaft.
    // Collected logs are never touched, so a log can't be collected twice.
    // (The team's PlayerMovement already walks slowly with Left Shift everywhere; the cave only lowers that speed.)
    public class CaveZone : MonoBehaviour
    {
        public BambooPlayerController player;
        public PlayerMovement movement;
        public CameraFollow2D cameraFollow;
        [Tooltip("The cave's log. The instruction hides once it has been collected.")]
        public CollectibleLog log;
        public GameObject instruction;
        [Tooltip("Ryu's feet are placed here when he dies in the cave (above ground, before the cracked ground).")]
        public Transform retryPoint;
        [Tooltip("Ryu's feet are placed here if he dies after climbing out.")]
        public Transform surfaceReturn;
        [Tooltip("Forest foreground plants that would float inside the cave view. Hidden while Ryu is underground.")]
        public GameObject[] surfaceForeground;

        [Header("Reset for another try")]
        public SleepingBear bear;
        public TorchPickup torch;
        public CrackedGround crackedGround;
        public BearIntroduction introduction;
        public WallClimbShaft escapeShaft;

        public float caveSneakSpeed = 2.2f;
        [Tooltip("The lowest point the camera may show while Ryu is in the cave.")]
        public float caveCameraBottom = -14.2f;
        [Tooltip("How fast (units per second) the camera's floor limit moves down or back up.")]
        public float cameraLimitSpeed = 9f;

        public bool PlayerInside { get; private set; }

        private float normalSneakSpeed;
        private float normalCameraBottom;

        void Start()
        {
            normalSneakSpeed = movement.sneakSpeed;
            normalCameraBottom = cameraFollow.groundBottomY;
            instruction.SetActive(false);
            player.Respawned += OnRespawned;
        }

        void OnDestroy()
        {
            if (player != null)
                player.Respawned -= OnRespawned;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.tag != "Player" || PlayerInside || player.IsDead)
                return;
            PlayerInside = true;
            player.SetCheckpoint(retryPoint.position);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.tag != "Player" || !PlayerInside || player.IsDead)
                return;
            PlayerInside = false;
            // Climbed out of the shaft: later deaths respawn on the far side, and the cave is reset.
            if (escapeShaft.PlayerInside)
                player.SetCheckpoint(surfaceReturn.position);
            ResetCave();
        }

        // Ryu respawned (he is now above the cave): reset the cave, put the forest settings back at once,
        // and snap the camera onto Ryu.
        private void OnRespawned()
        {
            PlayerInside = false;
            ResetCave();
            movement.sneakSpeed = normalSneakSpeed;
            cameraFollow.groundBottomY = normalCameraBottom;
            cameraFollow.Target = player.transform;
            cameraFollow.ResetPositionImmediate();
        }

        // The one reset for another try. Collected logs are not touched.
        public void ResetCave()
        {
            introduction.ResetIntro();
            bear.ResetBear();
            torch.ResetTorch();
            crackedGround.ResetGround();
        }

        void Update()
        {
            movement.sneakSpeed = PlayerInside ? caveSneakSpeed : normalSneakSpeed;

            float bottom = PlayerInside ? caveCameraBottom : normalCameraBottom;
            cameraFollow.groundBottomY = Mathf.MoveTowards(cameraFollow.groundBottomY, bottom, cameraLimitSpeed * Time.deltaTime);

            instruction.SetActive(PlayerInside && log.gameObject.activeSelf && Time.timeScale > 0f);
            foreach (GameObject plant in surfaceForeground)
                plant.SetActive(!PlayerInside);
        }
    }
}
