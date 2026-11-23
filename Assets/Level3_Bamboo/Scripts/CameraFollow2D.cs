using UnityEngine;

namespace Level3Bamboo
{
    // Follows Ryu smoothly, leads a little in the direction he runs, and never shows past the level edges.
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

        [Tooltip("Maximum horizontal framing lead in the direction of motion.")]
        public float lookAhead = 1.35f;
        public float worldTopY = 8f;
        public LogCollection logCollection;

        private Camera cam;
        private Rigidbody2D targetBody;
        private float currentLead;
        private float leadVelocity;
        private Vector3 currentVelocity;
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

            if (targetBody == null)
                targetBody = target.GetComponent<Rigidbody2D>();

            float speed = targetBody == null ? 0f : targetBody.velocity.x;
            float lead = Mathf.Clamp(speed / 5f, -1f, 1f) * lookAhead;
            currentLead = Mathf.SmoothDamp(currentLead, lead, ref leadVelocity, 0.28f);

            Vector3 goal = GetTargetPosition();
            Vector3 next = Vector3.SmoothDamp(transform.position, goal, ref currentVelocity, smoothTime);
            // Clamp the final viewport too, so a zoom or aspect change cannot expose an edge.
            transform.position = ClampViewport(next);
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
            Vector3 goal = target.position + offset;
            goal.x += currentLead;
            goal.z = offset.z;
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

        public void SetZoom(float newZoom)
        {
            targetZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
        }

        public void ResetPositionImmediate()
        {
            if (target != null)
            {
                currentLead = 0f;
                leadVelocity = 0f;
                transform.position = GetTargetPosition();
                currentVelocity = Vector3.zero;
            }
        }
    }
}
