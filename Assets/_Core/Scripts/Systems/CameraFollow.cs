using System.Collections;
using UnityEngine;

namespace OneLastTime.Systems
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        public Transform Target { get => target; set => target = value; }

        [Header("Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);
        [Range(0.01f, 1f)]
        [SerializeField] private float smoothTime = 0.2f;

        [Header("Vertical Movement")]
        [SerializeField] private bool followY = true;
        [SerializeField] private float fixedY = 0f;

        [Header("Camera Boundaries")]
        [SerializeField] private bool useLimits = true;
        [SerializeField] private float minY = 0f;
        [SerializeField] private float maxY = 3f;
        [SerializeField] private float minX = 0f;
        [SerializeField] private float maxX = 151.85f;

        public float MinX => minX;
        public float MaxX => maxX;
        public float MinY => minY;
        public float MaxY => maxY;

        [Header("Zoom")]
        [SerializeField] private float baseOrthoSize = 5.0f;
        [SerializeField] private bool keepBottomAlignedOnZoom = true;

        [Header("Trench Zone")]
        [SerializeField] private bool trenchZoneEnabled = false;
        [SerializeField] private float trenchStartX = 53.0f;
        [SerializeField] private float trenchEndX = 85.71f;
        [SerializeField] private float trenchTriggerY = -44.0f;
        [SerializeField] private float trenchMinY = -50.5f;
        [SerializeField] private float trenchYOffset = -1.0f;

        public bool TrenchZoneEnabled { get => trenchZoneEnabled; set => trenchZoneEnabled = value; }
        public void SetTrenchZoneEnabled(bool enabled) => trenchZoneEnabled = enabled;

        private Vector3 currentVelocity = Vector3.zero;
        private Camera cam;
        private Vector3 shakeOffset = Vector3.zero;
        private Transform savedTarget;
        private bool isFocusing = false;
        private Vector3 focusOffset = Vector3.zero;
        private bool hasCustomFocusOffset = false;

        public void FocusOnTarget(Transform focusTarget)
        {
            if (focusTarget == null) return;
            if (!isFocusing)
            {
                savedTarget = target;
                isFocusing = true;
            }
            target = focusTarget;
            hasCustomFocusOffset = false;
        }

        public void FocusOnTarget(Transform focusTarget, Vector3 customOffset)
        {
            if (focusTarget == null) return;
            if (!isFocusing)
            {
                savedTarget = target;
                isFocusing = true;
            }
            target = focusTarget;
            focusOffset = customOffset;
            hasCustomFocusOffset = true;
        }

        public void ClearFocus()
        {
            if (isFocusing && savedTarget != null)
            {
                target = savedTarget;
                savedTarget = null;
            }
            isFocusing = false;
            focusOffset = Vector3.zero;
            hasCustomFocusOffset = false;
        }

        void Awake()
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

        void Start()
        {
            // find player if target is unassigned
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

            // set initial boundaries depending on which floor player starts on
            if (target != null)
            {
                if (target.position.y < -35f)
                {
                    // lower floor
                    SetLimits(-6.0f, 120.0f, -42.5f, -42.0f);
                    SnapToTarget();
                }
                else if (target.position.y < -15f)
                {
                    // first floor
                    float calculatedMaxX = 141.0f;
                    var rightWall = GameObject.Find("Museum_Right_Wall");
                    if (rightWall != null)
                    {
                        float halfWidth = (cam != null && cam.orthographic) ? cam.orthographicSize * cam.aspect : 8.89f;
                        calculatedMaxX = rightWall.transform.position.x - halfWidth;
                    }

                    float targetMaxX = Mathf.Max(maxX, calculatedMaxX);
                    SetLimits(-1.0f, targetMaxX, -20.65f, -20.45f);
                    SnapToTarget();
                }
                else
                {
                    // outside village
                    SetLimits(0.0f, maxX, 0.0f, 3.0f);
                    SnapToTarget();
                }
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            float targetX;
            float targetY;

            if (isFocusing && hasCustomFocusOffset)
            {
                targetX = target.position.x + focusOffset.x;
                targetY = target.position.y + focusOffset.y;

                if (useLimits)
                {
                    targetX = Mathf.Clamp(targetX, minX, maxX);
                }
            }
            else
            {
                targetX = target.position.x + offset.x;
                targetY = followY ? (target.position.y + offset.y) : fixedY;

                // keep bottom of screen aligned when zooming
                float zoomYCompensation = 0f;
                if (keepBottomAlignedOnZoom && cam != null)
                {
                    zoomYCompensation = -(baseOrthoSize - cam.orthographicSize);
                }

                targetY += zoomYCompensation;
                float effectiveMinY = minY + zoomYCompensation;
                float effectiveMaxY = maxY + zoomYCompensation;

                // lower floor trench camera adjustment between trap door and exit jump (only active after interact)
                if (trenchZoneEnabled && target.position.y < trenchTriggerY && target.position.x >= trenchStartX && target.position.x <= trenchEndX)
                {
                    effectiveMinY = trenchMinY + zoomYCompensation;
                    targetY += trenchYOffset;
                }

                // clamp position within boundaries
                if (useLimits)
                {
                    targetX = Mathf.Clamp(targetX, minX, maxX);
                    if (followY)
                    {
                        targetY = Mathf.Clamp(targetY, effectiveMinY, effectiveMaxY);
                    }
                }
            }

            Vector3 targetPosition = new Vector3(targetX + shakeOffset.x, targetY + shakeOffset.y, offset.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
        }

        void OnDrawGizmosSelected()
        {
            if (!useLimits) return;

            // visualize camera boundaries in editor
            Gizmos.color = Color.yellow;
            float width = maxX - minX;
            float height = maxY - minY;
            Vector3 center = new Vector3(minX + width / 2f, minY + height / 2f, 0f);
            Gizmos.DrawWireCube(center, new Vector3(width, height, 0.1f));
        }
    }
}
