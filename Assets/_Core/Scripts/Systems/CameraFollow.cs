using System.Collections;
using UnityEngine;

namespace OneLastTime.Systems
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target to Follow")]
        [Tooltip("The GameObject for the camera to follow (e.g. Player_Ryu)")]
        [SerializeField] private Transform target;
        public Transform Target { get => target; set => target = value; }

        [Header("Camera Settings")]
        [Tooltip("Offset relative to the player. Keep Z at -10 for 2D.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);

        [Tooltip("How smoothly the camera catches up to the target")]
        [Range(0.01f, 1f)]
        [SerializeField] private float smoothTime = 0.2f;

        [Header("Vertical Movement")]
        [Tooltip("Set to false to completely stop the camera from bobbing up/down when jumping")]
        [SerializeField] private bool followY = true;
        [Tooltip("Fixed Y position used when Follow Y is false")]
        [SerializeField] private float fixedY = 0f;

        [Header("Camera Boundaries (Clamps)")]
        [Tooltip("Enable to prevent the camera from moving beyond your level edges")]
        [SerializeField] private bool useLimits = true;
        [SerializeField] private float minY = -1f;
        [SerializeField] private float maxY = 3.5f;
        [SerializeField] private float minX = -2f;
        [SerializeField] private float maxX = 151.85f;

        public float MinX => minX;
        public float MaxX => maxX;
        public float MinY => minY;
        public float MaxY => maxY;

        [Header("Zoom & Alignment")]
        [Tooltip("Default unzoomed orthographic size used as baseline for bottom-alignment")]
        [SerializeField] private float baseOrthoSize = 5.0f;
        [Tooltip("When zooming, keep the bottom edge of the camera view aligned at the same world min Y")]
        [SerializeField] private bool keepBottomAlignedOnZoom = true;

        private Vector3 currentVelocity = Vector3.zero;
        private Camera cam;
        private Vector3 shakeOffset = Vector3.zero;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        public IEnumerator ZoomTo(float targetOrthoSize, float duration)
        {
            if (cam == null) yield break;
            float startSize = cam.orthographicSize;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cam.orthographicSize = Mathf.Lerp(startSize, targetOrthoSize, elapsed / duration);
                yield return null;
            }
            cam.orthographicSize = targetOrthoSize;
        }

        public IEnumerator Shake(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                shakeOffset = new Vector3(x, y, 0f);
                yield return null;
            }
            shakeOffset = Vector3.zero;
        }

        public void SetLimits(float newMinX, float newMaxX, float newMinY, float newMaxY)
        {
            minX = newMinX;
            maxX = newMaxX;
            minY = newMinY;
            maxY = newMaxY;
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            float targetX = target.position.x + offset.x;
            float targetY = followY ? (target.position.y + offset.y) : fixedY;
            if (useLimits)
            {
                targetX = Mathf.Clamp(targetX, minX, maxX);
                targetY = Mathf.Clamp(targetY, minY, maxY);
            }
            transform.position = new Vector3(targetX, targetY, offset.z);
            currentVelocity = Vector3.zero;
        }

        private void Start()
        {
            if (target == null)
            {
                var pm = FindObjectOfType<PlayerMovement>();
                if (pm != null) target = pm.transform;
                else
                {
                    var player = GameObject.FindWithTag("Player");
                    if (player == null) player = GameObject.Find("player_ryu") ?? GameObject.Find("Player_Ryu");
                    if (player != null) target = player.transform;
                }
            }

            if (target != null)
            {
                if (target.position.y < -35f)
                {
                    // Target is in the lower floor / basement!
                    SetLimits(-8.0f, 120.0f, -42.5f, -41.0f);
                    SnapToTarget();
                }
                else if (target.position.y < -15f)
                {
                    // Target is in the museum upper corridor! Calculate museum boundaries dynamically
                    float calculatedMaxX = 141.0f;
                    var rightWall = GameObject.Find("Museum_Right_Wall");
                    if (rightWall != null)
                    {
                        float halfWidth = (cam != null && cam.orthographic) ? cam.orthographicSize * cam.aspect : 8.89f;
                        calculatedMaxX = rightWall.transform.position.x - halfWidth;
                    }

                    // Respect whichever is larger: the calculated wall bound or any inspector/custom maxX
                    float targetMaxX = Mathf.Max(maxX, calculatedMaxX);
                    SetLimits(-2.5f, targetMaxX, -20.65f, -20.45f);
                    SnapToTarget();
                }
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float targetX = target.position.x + offset.x;
            float targetY = followY ? (target.position.y + offset.y) : fixedY;

            // When zoomed, compensate camera Y downwards so the bottom of the viewport
            // stays aligned with the same world min Y as without zoom.
            float zoomYCompensation = 0f;
            if (keepBottomAlignedOnZoom && cam != null)
            {
                zoomYCompensation = -(baseOrthoSize - cam.orthographicSize);
            }

            targetY += zoomYCompensation;
            float effectiveMinY = minY + zoomYCompensation;
            float effectiveMaxY = maxY + zoomYCompensation;

            // Clamp within level boundaries
            if (useLimits)
            {
                targetX = Mathf.Clamp(targetX, minX, maxX);
                if (followY)
                {
                    targetY = Mathf.Clamp(targetY, effectiveMinY, effectiveMaxY);
                }
            }

            Vector3 targetPosition = new Vector3(targetX + shakeOffset.x, targetY + shakeOffset.y, offset.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        }

        private void OnDrawGizmosSelected()
        {
            if (!useLimits) return;

            // Draw visual boundary box in Scene view when Main Camera is selected
            Gizmos.color = Color.yellow;
            float width = maxX - minX;
            float height = maxY - minY;
            Vector3 center = new Vector3(minX + width / 2f, minY + height / 2f, 0f);
            Gizmos.DrawWireCube(center, new Vector3(width, height, 0.1f));
        }
    }
}
