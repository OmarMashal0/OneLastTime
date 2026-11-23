using UnityEngine;

namespace Level3Bamboo
{
    // Moves a background or foreground layer a fraction of the camera's movement.
    // Positive parallax: the layer lags behind, so it looks far away (sky, distant trees).
    // Negative parallax: the layer moves against the camera, so it looks close (foreground silhouettes).
    // Runs after the camera has followed Ryu in its own LateUpdate.
    [DefaultExecutionOrder(100)]
    public class ParallaxLayer : MonoBehaviour
    {
        public Camera gameplayCamera;
        [Range(-0.6f, 0.4f)] public float parallax = 0.1f;
        [Tooltip("Foreground pieces: measure the camera from this object's own x, so it sits at its placed spot when the camera is centred on it.")]
        public bool measureFromOwnPosition;

        private Vector3 startPosition;
        private float cameraStartX;
        private float ownX;

        // Start is called before the first frame update
        void Start()
        {
            startPosition = transform.localPosition;
            cameraStartX = gameplayCamera.transform.position.x;
            ownX = transform.position.x;
        }

        void LateUpdate()
        {
            float cameraX = gameplayCamera.transform.position.x;
            float from = measureFromOwnPosition ? ownX : cameraStartX;
            transform.localPosition = startPosition + Vector3.right * ((cameraX - from) * parallax);
        }
    }
}
