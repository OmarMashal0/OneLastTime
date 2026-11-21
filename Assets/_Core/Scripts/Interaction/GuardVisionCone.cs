using UnityEngine;
using OneLastTime.Interaction;
using OneLastTime.Player;

namespace OneLastTime.Enemies
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GuardVisionCone : MonoBehaviour
    {
        [Header("Vision Settings")]
        [SerializeField] private float viewDistance = 7.0f;
        [SerializeField] private float viewAngle = 50.0f;
        [Range(10, 60)]
        [SerializeField] private int rayCount = 30;

        [Header("Obstacle Detection")]
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Visual Styling")]
        [SerializeField] private Color normalColor = new Color(1.0f, 0.95f, 0.35f, 0.35f);
        [SerializeField] private Color alertColor = new Color(1.0f, 0.15f, 0.15f, 0.60f);
        [SerializeField] private string sortingLayerName = "ground";
        [SerializeField] private int sortingOrder = 5;

        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Collider2D guardCol;
        private bool isAlert;

        public float ViewDistance { get => viewDistance; set => viewDistance = value; }
        public float ViewAngle { get => viewAngle; set => viewAngle = value; }

        void Awake()
        {
            Init();
        }

        void OnEnable()
        {
            Init();
        }

        void Init()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

            if (mesh == null)
            {
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    mesh = meshFilter.sharedMesh;
                }
                else
                {
                    mesh = new Mesh();
                    mesh.name = "VisionConeMesh";
                    mesh.MarkDynamic();
                    if (meshFilter != null) meshFilter.sharedMesh = mesh;
                }
            }
            else if (meshFilter != null && meshFilter.sharedMesh == null)
            {
                meshFilter.sharedMesh = mesh;
            }

            if (meshRenderer.sharedMaterial == null)
            {
                var mat = Resources.Load<Material>("guard_vision_cone");
                if (mat == null)
                {
                    var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
                    mat = new Material(shader);
                }
                meshRenderer.sharedMaterial = mat;
            }

            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;

            guardCol = GetComponentInParent<Collider2D>();

            if (obstacleLayer.value == 0)
            {
                obstacleLayer = LayerMask.GetMask("ground");
            }
        }

        void LateUpdate()
        {
            RenderCone();
        }

        public void SetAlert(bool alert)
        {
            isAlert = alert;
        }

        public void RenderCone()
        {
            if (mesh == null) Init();
            if (mesh == null) return;

            Vector3 origin = transform.position;
            float scaleX = transform.lossyScale.x;
            if (Mathf.Abs(scaleX) < 0.001f) scaleX = 0.22f;
            float facingSign = Mathf.Sign(scaleX);
            float baseAngle = (facingSign >= 0f) ? 0f : 180f;

            float halfAngle = viewAngle * 0.5f;
            float angleStep = viewAngle / rayCount;

            Vector3[] vertices = new Vector3[rayCount + 2];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[rayCount * 6];
            Color[] colors = new Color[vertices.Length];

            Color activeColor = isAlert ? alertColor : normalColor;

            vertices[0] = Vector3.zero;
            uvs[0] = Vector2.zero;
            colors[0] = activeColor;

            float startAngle = baseAngle - halfAngle;

            for (int i = 0; i <= rayCount; i++)
            {
                float angle = startAngle + (angleStep * i);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                // raycast to detect walls blocking vision
                RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, viewDistance, obstacleLayer);
                Vector3 endpoint = origin + (Vector3)dir * viewDistance;

                foreach (var hit in hits)
                {
                    if (hit.collider == guardCol) continue;
                    if (hit.collider.isTrigger) continue;
                    if (hit.distance < 0.2f) continue;
                    if (hit.collider.CompareTag("Player") || hit.collider.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;

                    endpoint = hit.point;
                    break;
                }

                vertices[i + 1] = transform.InverseTransformPoint(endpoint);
                uvs[i + 1] = Vector2.zero;

                Color edgeColor = activeColor;
                edgeColor.a *= 0.50f;
                colors[i + 1] = edgeColor;

                if (i < rayCount)
                {
                    int triIdx = i * 6;
                    // front face
                    triangles[triIdx] = 0;
                    triangles[triIdx + 1] = i + 1;
                    triangles[triIdx + 2] = i + 2;
                    // back face
                    triangles[triIdx + 3] = 0;
                    triangles[triIdx + 4] = i + 2;
                    triangles[triIdx + 5] = i + 1;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateBounds();

            if (meshFilter != null && meshFilter.sharedMesh != mesh)
            {
                meshFilter.sharedMesh = mesh;
            }
        }

        public bool CanSeePlayer(Transform playerTarget, out Vector3 targetTorsoPoint)
        {
            targetTorsoPoint = Vector3.zero;
            if (playerTarget == null) return false;

            // check if player is hidden behind cover
            var activeCovers = FindObjectsOfType<StealthCover>();
            foreach (var cover in activeCovers)
            {
                if (cover != null && cover.IsPlayerHidden)
                {
                    return false;
                }
            }

            var col = playerTarget.GetComponent<Collider2D>();
            Vector3 bodyCenter = (col != null) ? (Vector3)col.bounds.center : playerTarget.position;
            targetTorsoPoint = bodyCenter;

            Vector2 origin = transform.position;

            Vector3[] testPoints = new Vector3[]
            {
                bodyCenter,
                bodyCenter + Vector3.up * 0.4f,
                bodyCenter - Vector3.up * 0.4f
            };

            float facingDir = Mathf.Sign(transform.lossyScale.x);
            Vector2 forward = (facingDir >= 0f) ? Vector2.right : Vector2.left;
            float halfAngle = viewAngle * 0.5f;

            foreach (var pt in testPoints)
            {
                Vector2 toPt = (Vector2)pt - origin;
                float dist = toPt.magnitude;
                if (dist > viewDistance) continue;

                float angle = Vector2.Angle(forward, toPt.normalized);
                if (angle > halfAngle) continue;

                // check if line of sight is clear
                RaycastHit2D[] hits = Physics2D.RaycastAll(origin, toPt.normalized, dist, obstacleLayer);
                bool blocked = false;
                foreach (var hit in hits)
                {
                    if (hit.collider == guardCol) continue;
                    if (hit.collider != null && (hit.collider.gameObject == playerTarget.gameObject || hit.transform.IsChildOf(playerTarget)))
                    {
                        break;
                    }
                    if (hit.collider.isTrigger) continue;
                    if (hit.distance < 0.2f) continue;

                    if (hit.distance < dist - 0.25f)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    targetTorsoPoint = pt;
                    return true;
                }
            }

            return false;
        }

        public bool CanShootPlayer(Transform playerTarget, out Vector3 targetTorsoPoint)
        {
            targetTorsoPoint = Vector3.zero;
            if (playerTarget == null) return false;

            // player is hidden behind cover
            var activeCovers = FindObjectsOfType<StealthCover>();
            foreach (var cover in activeCovers)
            {
                if (cover != null && cover.IsPlayerHidden)
                {
                    return false;
                }
            }

            // player is already dead
            var health = playerTarget.GetComponent<PlayerHealth>() ?? playerTarget.GetComponentInParent<PlayerHealth>();
            if (health != null && health.IsDead) return false;

            var col = playerTarget.GetComponent<Collider2D>();
            targetTorsoPoint = (col != null) ? (Vector3)col.bounds.center : playerTarget.position;
            Vector2 origin = transform.position;
            Vector2 toPlayer = (Vector2)targetTorsoPoint - origin;
            float dist = toPlayer.magnitude;

            if (dist > viewDistance * 2f) return false;

            // check if wall blocks the shot
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, toPlayer.normalized, dist, obstacleLayer);
            foreach (var hit in hits)
            {
                if (hit.collider == guardCol) continue;
                if (hit.collider != null && (hit.collider.gameObject == playerTarget.gameObject || hit.transform.IsChildOf(playerTarget)))
                {
                    break;
                }
                if (hit.collider.isTrigger) continue;
                if (hit.distance < 0.2f) continue;

                if (hit.distance < dist - 0.25f)
                {
                    return false;
                }
            }

            return true;
        }

        void OnDestroy()
        {
            if (Application.isPlaying && mesh != null) Destroy(mesh);
        }
    }
}
