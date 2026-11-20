using UnityEngine;

namespace OneLastTime.Systems
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target to Follow")]
        [Tooltip("The GameObject for the camera to follow (e.g. Player_Ryu)")]
        [SerializeField] private Transform target;

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
        [SerializeField] private float maxX = 85f;

        private Vector3 currentVelocity = Vector3.zero;

        private void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player == null) player = GameObject.Find("Player_Ryu");
                if (player != null) target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float targetX = target.position.x + offset.x;
            float targetY = followY ? (target.position.y + offset.y) : fixedY;

            // Clamp within level boundaries
            if (useLimits)
            {
                targetX = Mathf.Clamp(targetX, minX, maxX);
                if (followY)
                {
                    targetY = Mathf.Clamp(targetY, minY, maxY);
                }
            }

            Vector3 targetPosition = new Vector3(targetX, targetY, offset.z);
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
