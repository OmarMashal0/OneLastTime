using UnityEngine;

namespace Level3Bamboo
{
    // The bamboo trail's weather: light snow at the entrance, and a dark rainy night around Swamp Thing.
    //
    // The zones are marked by empty child objects (select them in the Hierarchy to see where they are):
    //   Snow Full Until -> Snow Gone            entrance snow fades out
    //   Night Starts -> Night Full              daylight fades into a rainy night
    //   Night Ends -> Night Cleared             the night fades back to daylight
    // Ryu's x position is compared with the markers every frame, and the "night" value (0 = day, 1 = rainy
    // night) moves toward the target a little each frame, so it never flickers at a boundary.
    // After a respawn the weather snaps straight to the right value for the checkpoint.
    public class BambooWeatherController : MonoBehaviour
    {
        [Header("Scene links")]
        public BambooPlayerController player;
        public Camera gameplayCamera;
        public ForestWind wind;

        [Header("Zone markers (only their x position is used)")]
        public Transform snowFullUntil;
        public Transform snowGone;
        public Transform nightStarts;
        public Transform nightFull;
        public Transform nightEnds;
        public Transform nightCleared;
        [Tooltip("Seconds to reach the new weather when Ryu changes zone.")]
        public float blendSeconds = 1.6f;

        [Header("Darkening (veils that sit between background layers, never over gameplay or the HUD)")]
        public SpriteRenderer farVeil;
        public Color farVeilColor = new Color(0.05f, 0.07f, 0.17f, 1f);
        [Range(0f, 1f)] public float farVeilAlpha = 0.62f;
        public SpriteRenderer midVeil;
        public Color midVeilColor = new Color(0.06f, 0.08f, 0.18f, 1f);
        [Range(0f, 1f)] public float midVeilAlpha = 0.38f;
        [Tooltip("Sprites tinted toward the night colour (the ground strip).")]
        public SpriteRenderer[] tinted = new SpriteRenderer[0];
        public Color tintedNightColor = new Color(0.58f, 0.62f, 0.78f, 1f);

        [Header("Particles")]
        public ParticleSystem rain;
        [Tooltip("Rain begins once the night value passes this point, then builds to steady.")]
        [Range(0f, 1f)] public float rainBeginsAt = 0.25f;
        public float lightRainRate = 35f;
        public float steadyRainRate = 150f;
        public float rainWindX = -3.2f;
        public ParticleSystem splash;
        public float splashesPerSecond = 9f;
        public ParticleSystem mist;
        public float mistRate = 0.7f;
        public ParticleSystem snow;
        public float snowRate = 14f;

        [Header("Sound (optional: no rain clip exists in the project yet)")]
        public AudioSource rainAudio;
        public float rainAudioVolume = 0.25f;

        public float Night => night;
        public float Snow => snowAmount;

        private float night;
        private float snowAmount;
        private float splashTimer;
        private Color[] tintedStartColors;

        private void OnEnable()
        {
            player.Respawned += SnapToCurrentZone;
        }

        private void OnDisable()
        {
            player.Respawned -= SnapToCurrentZone;
        }

        // Start is called before the first frame update
        void Start()
        {
            tintedStartColors = new Color[tinted.Length];
            for (int i = 0; i < tinted.Length; i++)
                tintedStartColors[i] = tinted[i].color;
            SnapToCurrentZone();
        }

        // Update is called once per frame
        void Update()
        {
            bool paused = Time.timeScale <= 0f;
            if (rainAudio != null)
            {
                if (paused) rainAudio.Pause(); else rainAudio.UnPause();
            }
            if (paused)
                return;

            // Move a little toward the zone's weather every frame.
            float step = Time.deltaTime / blendSeconds;
            night = Mathf.MoveTowards(night, NightTarget(), step);
            snowAmount = Mathf.MoveTowards(snowAmount, SnowTarget(), step);
        }

        // Particles and veils follow the camera, so they are placed after the camera has moved.
        void LateUpdate()
        {
            if (Time.timeScale <= 0f)
                return;
            ApplyWeather();
            EmitSplashes();
        }

        public void SnapToCurrentZone()
        {
            night = NightTarget();
            snowAmount = SnowTarget();
            rain.Clear();
            splash.Clear();
            ApplyWeather();
        }

        private float NightTarget()
        {
            float x = player.transform.position.x;
            float fallingDark = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(nightStarts.position.x, nightFull.position.x, x));
            float clearing = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(nightEnds.position.x, nightCleared.position.x, x));
            return Mathf.Min(fallingDark, 1f - clearing);
        }

        private float SnowTarget()
        {
            float x = player.transform.position.x;
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(snowFullUntil.position.x, snowGone.position.x, x));
        }

        private float RainAmount()
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(rainBeginsAt, 1f, night));
        }

        private void ApplyWeather()
        {
            Vector3 cam = gameplayCamera.transform.position;
            float halfHeight = gameplayCamera.orthographicSize;
            float halfWidth = halfHeight * gameplayCamera.aspect;
            float rainAmount = RainAmount();

            // Darken the far forest and the middle bamboo, and cool the ground colour.
            FitVeil(farVeil, farVeilColor, farVeilAlpha * night, cam, halfWidth, halfHeight);
            FitVeil(midVeil, midVeilColor, midVeilAlpha * night, cam, halfWidth, halfHeight);
            for (int i = 0; i < tinted.Length; i++)
                tinted[i].color = Color.Lerp(tintedStartColors[i], tintedStartColors[i] * tintedNightColor, night);

            // Rain falls from just above the screen, slanted by the wind.
            rain.transform.position = new Vector3(cam.x + halfWidth * 0.25f, cam.y + halfHeight + 1.2f, 0f);
            var rainShape = rain.shape;
            rainShape.scale = new Vector3(halfWidth * 2.6f, 0.2f, 1f);
            var rainEmission = rain.emission;
            rainEmission.rateOverTime = rainAmount <= 0.001f ? 0f : Mathf.Lerp(lightRainRate, steadyRainRate, rainAmount);
            var rainVelocity = rain.velocityOverLifetime;
            float windX = rainWindX * (1f + wind.GustAt(cam.x) * 0.6f);
            // All three velocity axes use "random between two constants", so x must too.
            rainVelocity.x = new ParticleSystem.MinMaxCurve(windX * 0.92f, windX * 1.08f);
            if (rainAmount > 0.001f && !rain.isPlaying)
                rain.Play();

            // Low mist near the ground behind gameplay.
            mist.transform.position = new Vector3(cam.x, cam.y - halfHeight * 0.62f, 0f);
            var mistShape = mist.shape;
            mistShape.scale = new Vector3(halfWidth * 2.2f, 0.3f, 1f);
            var mistEmission = mist.emission;
            mistEmission.rateOverTime = mistRate * night;

            // Entrance snow.
            snow.transform.position = new Vector3(cam.x + halfWidth * 0.1f, cam.y + halfHeight + 0.6f, 0f);
            var snowShape = snow.shape;
            snowShape.scale = new Vector3(halfWidth * 2.4f, 0.2f, 1f);
            var snowEmission = snow.emission;
            snowEmission.rateOverTime = snowRate * snowAmount;

            if (rainAudio != null)
            {
                rainAudio.volume = rainAudioVolume * rainAmount;
                if (rainAmount > 0.01f && !rainAudio.isPlaying)
                    rainAudio.Play();
                else if (rainAmount <= 0.01f && rainAudio.isPlaying)
                    rainAudio.Stop();
            }
        }

        // Stretches a veil sprite over the camera view and sets its transparency.
        private void FitVeil(SpriteRenderer veil, Color color, float alpha, Vector3 cam, float halfWidth, float halfHeight)
        {
            veil.enabled = alpha > 0.002f;
            if (!veil.enabled)
                return;
            veil.transform.position = new Vector3(cam.x, cam.y, 0f);
            Vector2 size = veil.sprite.bounds.size;
            veil.transform.localScale = new Vector3(halfWidth * 2.3f / size.x, halfHeight * 2.3f / size.y, 1f);
            color.a = alpha;
            veil.color = color;
        }

        // A few small splashes per second where raindrops hit the ground or a platform in view.
        private void EmitSplashes()
        {
            splashTimer += splashesPerSecond * RainAmount() * Time.deltaTime;
            while (splashTimer >= 1f)
            {
                splashTimer -= 1f;
                EmitOneSplash();
            }
        }

        private void EmitOneSplash()
        {
            Vector3 cam = gameplayCamera.transform.position;
            float halfHeight = gameplayCamera.orthographicSize;
            float halfWidth = halfHeight * gameplayCamera.aspect;
            Vector2 from = new Vector2(cam.x + Random.Range(-halfWidth * 0.95f, halfWidth * 0.95f), cam.y + halfHeight);

            // Find the first solid surface below. Skip triggers and moving bodies; never splash on Ryu or an enemy.
            RaycastHit2D[] hits = Physics2D.RaycastAll(from, Vector2.down, halfHeight * 2.2f);
            foreach (RaycastHit2D hit in hits)
            {
                Collider2D surface = hit.collider;
                if (surface.isTrigger || surface.attachedRigidbody != null && surface.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic)
                    continue;
                if (surface.GetComponentInParent<BambooPlayerController>() != null || surface.GetComponentInParent<ForestEnemy>() != null
                    || surface.GetComponentInParent<SwampThing>() != null)
                    return;

                var splashPoint = new ParticleSystem.EmitParams { position = hit.point, applyShapeToPosition = false };
                splash.Emit(splashPoint, 1);
                return;
            }
        }
    }
}
