using System;
using UnityEngine;

namespace Level3Bamboo
{
    // A few pale moths circle near the checkpoint lantern and scatter when Ryu comes close.
    // If he stays near the lantern for a while they grow used to him and settle back.
    public class LanternMoths : MonoBehaviour
    {
        [Serializable]
        public class Moth
        {
            public SpriteRenderer renderer;
            public Vector2 radius = new Vector2(0.32f, 0.2f);
            public float speed = 1f;
            public float phase;
        }

        public BambooPlayerController player;
        public Moth[] moths;
        public Sprite[] flap;
        [Min(1f)] public float flapFps = 13f;
        public float scatterRadius = 1.7f;
        public float scatterDistance = 0.85f;
        public float scatterHold = 1.1f;
        public float returnSeconds = 1.8f;
        [Tooltip("If Ryu lingers nearby (e.g. at the checkpoint), the moths settle back toward the lantern after this long.")]
        public Vector2 settleWhileNear = new Vector2(2.5f, 7f);

        // Per-moth values that change while playing (kept in plain arrays, one entry per moth).
        private Vector3[] home;
        private Vector2[] offset;
        private Vector2[] offsetVelocity;
        private Vector2[] away;
        private float[] scattered;
        private float[] clock;
        private float[] exposure;

        private void OnEnable()
        {
            player.Respawned += CalmDown;
        }

        private void OnDisable()
        {
            player.Respawned -= CalmDown;
        }

        // Start is called before the first frame update
        void Start()
        {
            int count = moths.Length;
            home = new Vector3[count];
            offset = new Vector2[count];
            offsetVelocity = new Vector2[count];
            away = new Vector2[count];
            scattered = new float[count];
            clock = new float[count];
            exposure = new float[count];
            for (int i = 0; i < count; i++)
            {
                home[i] = moths[i].renderer.transform.localPosition;
                clock[i] = moths[i].phase * 10f;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            float dt = Time.deltaTime;
            for (int i = 0; i < moths.Length; i++)
                UpdateMoth(i, dt);
        }

        private void UpdateMoth(int i, float dt)
        {
            Moth moth = moths[i];
            clock[i] += dt * moth.speed;
            float t = clock[i];

            // Each moth follows its own figure-of-eight around its resting spot.
            Vector2 loop = new Vector2(Mathf.Sin(t * 0.9f) * moth.radius.x,
                Mathf.Sin(t * 1.8f + moth.phase) * moth.radius.y + Mathf.Sin(t * 0.37f) * 0.05f);

            Vector3 world = transform.TransformPoint(home[i]);
            Vector2 fromRyu = world - (player.transform.position + Vector3.up * 0.7f);
            if (!player.IsDead && fromRyu.sqrMagnitude < scatterRadius * scatterRadius)
            {
                if (scattered[i] <= 0f)
                    away[i] = (fromRyu.sqrMagnitude > 0.0001f ? fromRyu.normalized : Vector2.up) * scatterDistance + Vector2.up * 0.3f;
                scattered[i] = scatterHold;
                exposure[i] += dt;
            }
            else
            {
                scattered[i] -= dt;
                exposure[i] = Mathf.Max(0f, exposure[i] - dt * 0.5f);
            }

            // Startled at first, then they grow used to Ryu standing by the lantern.
            float calm = Mathf.Lerp(1f, 0.3f, Mathf.InverseLerp(settleWhileNear.x, settleWhileNear.y, exposure[i]));
            Vector2 goal = scattered[i] > 0f ? away[i] * calm : Vector2.zero;
            bool startled = scattered[i] > 0f && calm > 0.95f;
            offset[i] = Vector2.SmoothDamp(offset[i], goal, ref offsetVelocity[i], startled ? 0.22f : returnSeconds, Mathf.Infinity, dt);

            moth.renderer.transform.localPosition = home[i] + (Vector3)(loop + offset[i]);
            moth.renderer.sprite = flap[Mathf.FloorToInt(t * flapFps + moth.phase * flap.Length) % flap.Length];
        }

        private void CalmDown()
        {
            if (scattered == null)
                return;
            for (int i = 0; i < moths.Length; i++)
            {
                scattered[i] = 0f;
                exposure[i] = 0f;
            }
        }
    }
}
