using System;
using UnityEngine;

namespace Level3Bamboo
{
    // The forest's wind. Every few seconds a gust starts at the left of the screen and sweeps to the
    // right. Plants, leaves, the decoration animations and the rain ask GustAt(x) how strong the wind
    // is at their position. Wind zones make some stretches calmer or windier.
    public class ForestWind : MonoBehaviour
    {
        [Serializable]
        public class WindZone
        {
            public float startX;
            public float endX;
            [Min(0.01f)] public float blend = 3f;
            [Min(0f)] public float strength = 1f;
        }

        public Camera gameplayCamera;
        [Range(0f, 2f)] public float windStrength = 1f;

        [Header("Gusts")]
        [Min(1f)] public float minGustInterval = 8f;
        [Min(1f)] public float maxGustInterval = 20f;
        public Vector2 gustSeconds = new Vector2(3.5f, 6f);
        public Vector2 gustPower = new Vector2(0.75f, 1.15f);
        [Tooltip("How fast a gust crosses the screen from left to right, in units per second.")]
        [Min(1f)] public float gustFrontSpeed = 14f;
        public WindZone[] windZones;

        [Header("Leaf particles released by each gust")]
        public ParticleSystem[] leafGustEmitters;
        public Vector2Int leavesPerGust = new Vector2Int(2, 5);

        // Goes up by one every time a new gust starts (gust leaves use it to fly once per gust).
        public int GustNumber { get; private set; }

        private float clock;
        private float nextGust;
        private float gustStart = -100f;
        private float gustLength = 1f;
        private float gustStrength;
        private float previousStart = -100f;
        private float previousLength = 1f;
        private float previousStrength;
        private float cameraX;
        private float halfWidth = 8f;
        private float[] emitAt;
        private int[] emitCount;

        // Start is called before the first frame update
        void Start()
        {
            // The first gust arrives soon after the level opens, then the rhythm turns irregular.
            nextGust = UnityEngine.Random.Range(3f, 6f);
            emitAt = new float[leafGustEmitters.Length];
            emitCount = new int[leafGustEmitters.Length];
            for (int i = 0; i < emitAt.Length; i++)
                emitAt[i] = -1f;
        }

        // Update is called once per frame
        void Update()
        {
            clock += Time.deltaTime;
            cameraX = gameplayCamera.transform.position.x;
            halfWidth = gameplayCamera.orthographicSize * gameplayCamera.aspect;

            if (clock >= nextGust)
                StartGust();
            ReleaseLeaves();
        }

        // Wind strength (about 0 to 1.3) at a world x. The lag lets neighbouring plants react a moment apart.
        public float GustAt(float x, float lag = 0f)
        {
            float t = clock - lag - FrontDelay(x);
            float gust = Mathf.Max(Envelope(t, gustStart, gustLength, gustStrength),
                Envelope(t, previousStart, previousLength, previousStrength));
            return gust * WindAt(x) * windStrength;
        }

        private void StartGust()
        {
            previousStart = gustStart;
            previousLength = gustLength;
            previousStrength = gustStrength;
            gustStart = clock;
            gustLength = UnityEngine.Random.Range(gustSeconds.x, gustSeconds.y);
            gustStrength = UnityEngine.Random.Range(gustPower.x, gustPower.y);
            GustNumber++;
            nextGust = clock + UnityEngine.Random.Range(minGustInterval, Mathf.Max(minGustInterval, maxGustInterval));

            // Leaf emitters near the screen let go of a few leaves when the gust reaches them.
            for (int i = 0; i < leafGustEmitters.Length; i++)
            {
                float x = leafGustEmitters[i].transform.position.x;
                if (Mathf.Abs(x - cameraX) > halfWidth + 10f)
                    continue;
                emitAt[i] = clock + gustLength * 0.3f + FrontDelay(x) + UnityEngine.Random.Range(0f, 0.5f);
                float amount = UnityEngine.Random.Range(leavesPerGust.x, leavesPerGust.y + 1) * WindAt(x) * gustStrength;
                emitCount[i] = Mathf.Clamp(Mathf.RoundToInt(amount), 1, 8);
            }
        }

        private void ReleaseLeaves()
        {
            for (int i = 0; i < emitAt.Length; i++)
            {
                if (emitAt[i] < 0f || clock < emitAt[i])
                    continue;
                emitAt[i] = -1f;
                if (leafGustEmitters[i].gameObject.activeInHierarchy)
                    leafGustEmitters[i].Emit(emitCount[i]);
            }
        }

        // The gust reaches the left edge of the screen first and the right edge a moment later.
        private float FrontDelay(float x)
        {
            return Mathf.Clamp((x - (cameraX - halfWidth - 2f)) / gustFrontSpeed, 0f, 2.5f);
        }

        // 1 everywhere, blended toward each zone's strength inside that zone.
        private float WindAt(float x)
        {
            float wind = 1f;
            foreach (WindZone zone in windZones)
            {
                float inside = Mathf.Min(Mathf.InverseLerp(zone.startX - zone.blend, zone.startX + zone.blend, x),
                    Mathf.InverseLerp(zone.endX + zone.blend, zone.endX - zone.blend, x));
                wind = Mathf.Lerp(wind, zone.strength, Mathf.SmoothStep(0f, 1f, inside));
            }
            return wind;
        }

        // Shape of one gust: builds up over 40% of its length, then settles more slowly.
        private static float Envelope(float t, float start, float length, float power)
        {
            float progress = (t - start) / length;
            if (progress <= 0f || progress >= 1f)
                return 0f;
            float rise = Mathf.SmoothStep(0f, 1f, progress / 0.4f);
            float fall = Mathf.SmoothStep(0f, 1f, (1f - progress) / 0.45f);
            return power * Mathf.Min(rise, fall);
        }
    }
}
