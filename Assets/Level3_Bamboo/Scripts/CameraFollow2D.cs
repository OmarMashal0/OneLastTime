using UnityEngine;

namespace Level3Bamboo
{
    // Follows Ryu smoothly with a small dead zone, looks ahead in the direction he faces,
    // ignores normal jump bounces, and never shows past the level edges.
    // Mouse wheel or +/- (also Q/E and I/O) zoom in and out. E is skipped while it is needed to collect a log.
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Follow Target")]
        [Tooltip("Ryu's Player_Ryu object (assigned in the Inspector).")]
        public Transform target;
        [Tooltip("Camera position relative to the target. Y around 3.35 frames the player on the ground perfectly matching the level design.")]
        public Vector3 offset = new Vector3(0f, 3.38f, -10f);
        public float smoothTime = 0.15f;

        [Header("Zoom Settings")]
        public float defaultZoom = 5f;
        public float minZoom = 2.5f;
        public float maxZoom = 5.8f;
        public float zoomSpeed = 2.5f;
        public float zoomSmoothTime = 0.12f;

        [Header("Ground & World Clamping")]
        [Tooltip("Prevents the bottom of the camera from dipping below the grassy ground tiles into empty space.")]
        public bool clampGroundBottom = true;
        public float groundBottomY = -7.60f;
        public bool clampHorizontalBounds = true;
        public float minWorldX = -12.0f;
        public float maxWorldX = 158.0f;

        [Tooltip("How far the camera looks ahead in the direction Ryu is facing.")]
        public float lookAhead = 1.35f;

        [Header("Dead Zone & Jumps")]
        [Tooltip("Ryu can move this far left or right before the camera starts to follow.")]
        public float deadZoneWidth = 0.6f;
        [Tooltip("Seconds the camera takes to catch up vertically (bigger = calmer).")]
        public float verticalSmoothTime = 0.32f;
        [Tooltip("The camera ignores jumps up to this height above the last ground Ryu stood on.")]
        public float jumpAllowance = 2.4f;

        [Header("Forest Height")]
        [Tooltip("On: above the forest floor the camera keeps the normal forest height (ledges do not lift it).")]
        public bool holdForestHeight = false;
        [Tooltip("Ryu's height when he stands on the forest floor.")]
        public float forestStandY = -5.27f;
        [Tooltip("Space kept above Ryu's feet at the top of the screen; the camera only rises if he would go higher.")]
        public float topMargin = 1.8f;
        public float worldTopY = 8f;
        public LogCollection logCollection;

        // Camera shake: Shake(seconds) adds a small random offset for a short time.
        [Header("Camera Shake")]
        public float shakeStrength = 0.12f;   // how far the camera jumps, in world units
        private float shakeTimer;             // seconds of shaking left
        private Vector3 followPosition;       // where the camera would be without shaking

        private Camera cam;
        private float currentLead;
        private float leadVelocity;
        private Vector3 currentVelocity;
        private BambooPlayerController player;
        private float focusX;        // centre of the dead zone
        private float groundY;       // ground height the camera frames (only changes outside the dead zone)
        private float xVelocity;
        private float yVelocity;
        private float targetZoom;
        private float zoomVelocity;

        public Transform Target
        {
            get => target;
            set => target = value;
        }

        public float TargetZoom
        {
            get => targetZoom;
            set => targetZoom = Mathf.Clamp(value, minZoom, maxZoom);
        }

        // Target, TargetZoom and SetZoom are also used by the project's shared editor test tool.

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null && cam.orthographic)
            {
                targetZoom = cam.orthographicSize > 0.01f ? cam.orthographicSize : defaultZoom;
            }
            else
            {
                targetZoom = defaultZoom;
            }
        }

        // When the camera is switched back on (after the tea talk or the hallucination),
        // follow from where it is now.
        private void OnEnable()
        {
            followPosition = transform.position;
        }

        // Start is called before the first frame update
        private void Start()
        {
            ResetPositionImmediate();
        }

        // Update is called once per frame
        private void Update()
        {
            if (Time.timeScale > 0f)
                HandleZoomInput();
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0f)
                return;

            if (player == null)
                player = target.GetComponent<BambooPlayerController>();

            // 1. Dead zone: small steps left and right do not move the camera.
            float x = target.position.x;
            if (x > focusX + deadZoneWidth)
                focusX = x - deadZoneWidth;
            else if (x < focusX - deadZoneWidth)
                focusX = x + deadZoneWidth;

            // 2. Look ahead in the direction Ryu faces (his transform is flipped to face left).
            float facing = target.lossyScale.x < 0f ? -1f : 1f;
            currentLead = Mathf.SmoothDamp(currentLead, facing * lookAhead, ref leadVelocity, 0.45f);

            // 3. Vertical.
            //    Forest (on or above the forest floor): always frame the forest floor, so ledges and
            //    jumps never lift the camera (GetTargetPosition only raises it if Ryu would leave the top).
            //    Cave and escape shaft (below the floor): follow the ground Ryu stands on; while in the air
            //    only a very high jump or a fall moves the camera.
            float y = target.position.y;
            if (holdForestHeight && y > forestStandY - 0.5f)
                groundY = forestStandY;
            else if (player == null || player.IsGrounded)
                groundY = y;
            else if (y < groundY - 0.5f)
                groundY = y;
            else if (y > groundY + jumpAllowance)
                groundY = y - jumpAllowance;

            Vector3 goal = GetTargetPosition();
            // Follow from the calm position (x and y smoothed separately), then add the shake on top.
            Vector3 next = followPosition;
            next.x = Mathf.SmoothDamp(followPosition.x, goal.x, ref xVelocity, smoothTime);
            next.y = Mathf.SmoothDamp(followPosition.y, goal.y, ref yVelocity, verticalSmoothTime);
            next.z = goal.z;
            followPosition = ClampViewport(next);
            // Clamp again so neither a zoom nor the shake can show past the level edges.
            transform.position = ClampViewport(followPosition + ShakeOffset());
        }

        private void HandleZoomInput()
        {
            if (cam == null || !cam.orthographic)
            {
                return;
            }

            // 1. Mouse Scroll Wheel (standard zoom control)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                targetZoom -= scroll * zoomSpeed * 6f;
            }

            // 2. Keyboard zoom shortcuts: Keypad +/-, Equals/Minus, Q/E, or I/O (In/Out)
            bool zoomInWithE = Input.GetKey(KeyCode.E) && (logCollection == null || !logCollection.ReservesInteractionKey);
            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.Plus) || zoomInWithE || Input.GetKey(KeyCode.I))
            {
                targetZoom -= zoomSpeed * Time.deltaTime * 2.5f;
            }
            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.O))
            {
                targetZoom += zoomSpeed * Time.deltaTime * 2.5f;
            }

            float fitX = clampHorizontalBounds ? (maxWorldX - minWorldX) / (2f * cam.aspect) : maxZoom;
            float fitY = clampGroundBottom ? (worldTopY - groundBottomY) * 0.5f : maxZoom;
            float zoomLimit = Mathf.Max(0.01f, Mathf.Min(maxZoom, Mathf.Min(fitX, fitY)));
            targetZoom = Mathf.Clamp(targetZoom, Mathf.Min(minZoom, zoomLimit), maxZoom);
            cam.orthographicSize = Mathf.Min(zoomLimit,
                Mathf.SmoothDamp(cam.orthographicSize, Mathf.Min(targetZoom, zoomLimit), ref zoomVelocity, zoomSmoothTime));
        }

        private Vector3 GetTargetPosition()
        {
            Vector3 goal = new Vector3(focusX + currentLead, groundY, 0f) + offset;
            goal.z = offset.z;
            // Rise only as much as needed to keep Ryu below the top edge (he is never centred).
            if (holdForestHeight && cam != null)
                goal.y = Mathf.Max(goal.y, target.position.y + topMargin - cam.orthographicSize);
            return ClampViewport(goal);
        }

        private Vector3 ClampViewport(Vector3 center)
        {
            if (cam == null || !cam.orthographic)
                return center;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            if (clampGroundBottom)
                center.y = ClampCenter(center.y, groundBottomY, worldTopY, halfHeight);
            if (clampHorizontalBounds)
                center.x = ClampCenter(center.x, minWorldX, maxWorldX, halfWidth);

            return center;
        }

        private static float ClampCenter(float center, float min, float max, float extent)
        {
            return max - min >= extent * 2f
                ? Mathf.Clamp(center, min + extent, max - extent)
                : (min + max) * 0.5f;
        }

        // Call this to shake the camera, e.g. Shake(0.25f) for a quarter of a second.
        public void Shake(float seconds)
        {
            shakeTimer = seconds;
        }

        // A small random offset while the timer runs, zero afterwards.
        private Vector3 ShakeOffset()
        {
            if (shakeTimer <= 0f)
                return Vector3.zero;

            shakeTimer -= Time.deltaTime;
            Vector2 random = Random.insideUnitCircle * shakeStrength;
            return new Vector3(random.x, random.y, 0f);
        }

        public void SetZoom(float newZoom)
        {
            targetZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
        }

        public void ResetPositionImmediate()
        {
            if (target != null)
            {
                focusX = target.position.x;
                groundY = target.position.y;
                currentLead = (target.lossyScale.x < 0f ? -1f : 1f) * lookAhead;
                leadVelocity = 0f;
                xVelocity = 0f;
                yVelocity = 0f;
                transform.position = GetTargetPosition();
                followPosition = transform.position;
                currentVelocity = Vector3.zero;
            }
        }
    }
}
