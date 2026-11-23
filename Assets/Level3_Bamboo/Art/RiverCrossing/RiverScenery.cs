using UnityEngine;

namespace Level3Bamboo.River
{
    // Only sky and atmosphere move with the camera. The banks and water stay in world space.
    public class RiverScenery : MonoBehaviour
    {
        public Camera sceneCamera;
        public Transform[] layers;
        public float[] cameraFollow;
        public SpriteRenderer[] leaves;

        private Vector3[] layerOrigins;
        private Vector3[] leafOrigins;
        private float cameraOrigin;

        private void Awake()
        {
            cameraOrigin = sceneCamera.transform.position.x;
            layerOrigins = new Vector3[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                layerOrigins[i] = layers[i].localPosition;

            leafOrigins = new Vector3[leaves.Length];
            for (int i = 0; i < leaves.Length; i++)
                leafOrigins[i] = leaves[i].transform.localPosition;
        }

        private void LateUpdate()
        {
            float travel = sceneCamera.transform.position.x - cameraOrigin;
            for (int i = 0; i < layers.Length; i++)
                layers[i].localPosition = layerOrigins[i] + Vector3.right * (travel * cameraFollow[i]);

            for (int i = 0; i < leaves.Length; i++)
            {
                float phase = Mathf.Repeat(Time.time * 0.12f + i * 0.32f, 1f);
                var leaf = leaves[i];
                leaf.transform.localPosition = leafOrigins[i] + new Vector3(phase * 2.8f, -phase * 2.2f + Mathf.Sin(phase * 10f) * 0.17f);
                leaf.transform.localRotation = Quaternion.Euler(0f, 0f, phase * 80f);
                var tint = leaf.color;
                tint.a = Mathf.Sin(phase * Mathf.PI) * 0.72f;
                leaf.color = tint;
            }
        }
    }
}
