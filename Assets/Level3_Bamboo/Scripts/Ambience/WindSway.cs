using UnityEngine;

namespace Level3Bamboo
{
    // Rotates a fern, flower or grass tuft in the wind, and bends it away from Ryu while he walks through it.
    // Put it on the object that should rotate (for grass that is the tuft, so the blades bend from their base).
    public class WindSway : MonoBehaviour
    {
        public enum PlantType { Fern, Grass }

        public ForestWind wind;
        public BambooPlayerController player;
        public PlantType plantType = PlantType.Fern;
        [Tooltip("Offset so neighbouring plants do not sway in step.")]
        public float phase;
        [Tooltip("How strongly this plant reacts to wind and to Ryu.")]
        public float strength = 1f;
        public float bendRadius = 0.95f;
        public float maxBend = 16f;

        private Quaternion startRotation;
        private float lag;
        private float bend;
        private float bendVelocity;

        // Start is called before the first frame update
        void Start()
        {
            startRotation = transform.localRotation;
            // Neighbours respond a fraction of a second apart instead of in lockstep.
            lag = Mathf.Repeat(Mathf.Sin(phase * 12.9898f + 3f * 78.233f) * 43758.5453f, 1f) * 0.55f;
        }

        // Update is called once per frame
        void Update()
        {
            if (Time.timeScale <= 0f)
                return;

            float time = Time.timeSinceLevelLoad;
            float gust = wind.GustAt(transform.position.x, lag);
            float sway;
            float bendScale;
            if (plantType == PlantType.Grass)
            {
                float breeze = Mathf.Sin(time * 1.3f + phase) * 1.6f;
                sway = -(breeze + 1.5f + 9f * gust) * strength;
                bendScale = 1f;
            }
            else
            {
                float wave = Mathf.Sin(time * 0.7f + phase);
                sway = -(1.1f + wave) * (0.45f + 1.8f * gust) * strength;
                bendScale = 0.6f;
            }

            transform.localRotation = startRotation * Quaternion.Euler(0f, 0f, sway + BendAwayFromRyu(bendScale));
        }

        private float BendAwayFromRyu(float scale)
        {
            float target = 0f;
            if (!player.IsDead)
            {
                Vector3 feet = player.transform.position;
                float dx = transform.position.x - feet.x;
                float dy = feet.y - transform.position.y;
                if (Mathf.Abs(dx) < bendRadius && dy > -0.8f && dy < 1.3f)
                    target = -Mathf.Sign(dx) * (1f - Mathf.Abs(dx) / bendRadius) * maxBend * scale * strength;
            }

            // Bend quickly when Ryu arrives, straighten slowly after he leaves.
            float smoothing = Mathf.Abs(target) > Mathf.Abs(bend) ? 0.07f : 0.4f;
            bend = Mathf.SmoothDamp(bend, target, ref bendVelocity, smoothing);
            return bend;
        }
    }
}
