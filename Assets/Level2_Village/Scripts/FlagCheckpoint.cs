using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// The trigger collider on this object is the checkpoint area.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class FlagCheckpoint : MonoBehaviour
{
    // =====================================================================
    //  CHECKPOINT MEMORY (same as your original script)
    // =====================================================================

    // Remembers where the player should respawn, one entry per scene.
    private static readonly Dictionary<string, Vector2> checkpoints = new Dictionary<string, Vector2>();

    // Runs when the game starts and clears every saved checkpoint.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => checkpoints.Clear();

    // =====================================================================
    //  SPARK SETTINGS (shown in the Inspector so you can tweak them)
    // =====================================================================

    [Header("Checkpoint sparks")]

    [Tooltip("Optional. Leave empty and the script builds a glowing material for you.")]
    [SerializeField] private Material sparkMaterial;

    [Tooltip("How many sparks fly out each time the checkpoint is touched.")]
    [SerializeField] private int sparkCount = 24;

    [Tooltip("How long the burst lasts, in seconds. Sparks come out one at a time across this window.")]
    [SerializeField] private float burstTime = 0.3f;

    [Tooltip("Sparks start anywhere this far left or right of the pole's base.")]
    [SerializeField] private float spreadWidth = 0.55f;

    [Tooltip("Smallest and largest spark size, in world units.")]
    [SerializeField] private float minSize = 0.1f;
    [SerializeField] private float maxSize = 0.3f;

    [Tooltip("Shortest and longest spark life, in seconds.")]
    [SerializeField] private float minLife = 0.6f;
    [SerializeField] private float maxLife = 1.2f;

    [Tooltip("Slowest and fastest upward launch speed.")]
    [SerializeField] private float minRise = 1.2f;
    [SerializeField] private float maxRise = 2.4f;

    [Tooltip("Pulls sparks downward over time. Higher means they fall back sooner.")]
    [SerializeField] private float gravityPull = 0.6f;

    [Tooltip("Air resistance. Higher means sparks slow down faster.")]
    [SerializeField] private float airDrag = 1f;

    // =====================================================================
    //  RUNTIME STATE
    // =====================================================================

    private ParticleSystem lights;   // the particle system, built the first time it's needed
    private Material ownedMaterial;  // a material this script made (destroyed in OnDestroy)
    private Texture2D ownedTexture;  // a glow texture this script made (destroyed in OnDestroy)

    // =====================================================================
    //  RESPAWN LOOKUP (same as your original script)
    // =====================================================================

    // Other scripts call this to find where the player should respawn.
    public static Vector2 GetSpawn(string scenePath, Vector2 initialSpawn)
    {
        return checkpoints.TryGetValue(scenePath, out Vector2 checkpoint) ? checkpoint : initialSpawn;
    }

    // =====================================================================
    //  TRIGGER: the player touches the flag
    // =====================================================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D player = other.attachedRigidbody;
        if (player == null || !player.CompareTag("Player")) return;

        // Place the player's feet just above the base of the pole.
        float footOffset = player.position.y - other.bounds.min.y;
        Bounds flag = GetComponent<BoxCollider2D>().bounds;
        checkpoints[gameObject.scene.path] = new Vector2(flag.center.x, flag.min.y + footOffset + 0.1f);

        // Celebrate with sparks at the base of the pole.
        EmitLights(new Vector2(flag.center.x, flag.min.y));
    }

    // =====================================================================
    //  EMITTING SPARKS (runs every time the checkpoint is touched)
    // =====================================================================

    // 'origin' is the bottom of the pole, in world space.
    private void EmitLights(Vector2 origin)
    {
        // The first time we need sparks, build the particle system.
        if (lights == null)
        {
            CreateLightSystem();
        }

        // Move the system to the base of the pole and make sure it's running.
        lights.transform.position = origin;
        lights.Play();

        // Release the sparks over a short window instead of all in one frame.
        StartCoroutine(EmitBurst(origin));
    }

    // Releases the sparks one at a time, spread across 'burstTime' seconds.
    private IEnumerator EmitBurst(Vector2 origin)
    {
        // The pause between sparks, so the whole burst takes about 'burstTime' seconds.
        // Mathf.Max keeps us from dividing by zero if sparkCount is set to 0.
        float gap = burstTime / Mathf.Max(1, sparkCount);

        for (int i = 0; i < sparkCount; i++)
        {
            SpawnSpark(origin);
            yield return new WaitForSeconds(gap); // wait here, then continue the loop
        }
    }

    // Spawns one spark with its own random position, speed, size and lifetime.
    private void SpawnSpark(Vector2 origin)
    {
        // Two random numbers multiplied together favor small results.
        // Result: mostly tiny sparks with the occasional bigger one, which looks more natural.
        float sizeRoll = Random.value * Random.value;

        var spark = new ParticleSystem.EmitParams
        {
            // Start at a random spot across the width of the flag.
            position = origin + Vector2.right * Random.Range(-spreadWidth, spreadWidth),

            // Shoot mostly upward, with a small sideways nudge so no two sparks match.
            velocity = new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(minRise, maxRise)),

            // Each spark gets its own size and lifetime.
            // Color is NOT set here on purpose: the color-over-lifetime gradient handles it.
            startSize = Mathf.Lerp(minSize, maxSize, sizeRoll),
            startLifetime = Random.Range(minLife, maxLife)
        };

        lights.Emit(spark, 1); // spawn exactly one spark with these settings
    }

    // =====================================================================
    //  BUILDING THE PARTICLE SYSTEM (runs once)
    // =====================================================================

    private void CreateLightSystem()
    {
        // 1. A child object to hold the particle system. It moves with the flag and is easy to find.
        var holder = new GameObject("Checkpoint lights");
        holder.transform.SetParent(transform, true);

        // 2. Add the particle system and stop it immediately, so it doesn't start on its own.
        lights = holder.AddComponent<ParticleSystem>();
        lights.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // 3. MAIN settings: rules that apply to every spark.
        var main = lights.main;
        main.playOnAwake = false;                                   // don't start by itself
        main.loop = false;                                          // one burst, not a constant fountain
        main.simulationSpace = ParticleSystemSimulationSpace.World; // sparks stay where they were born
        main.startColor = Color.white;                              // base color; the gradient tints it over time
        main.maxParticles = 500;                                    // room for overlapping bursts

        // 4. EMISSION and SHAPE are off, because SpawnSpark() creates every spark by hand.
        var emission = lights.emission;
        emission.enabled = false;

        var shape = lights.shape;
        shape.enabled = false;

        // 5. COLOR over lifetime: white-hot -> gold -> orange -> gone.
        var colorLife = lights.colorOverLifetime;
        colorLife.enabled = true;
        colorLife.color = MakeSparkGradient();

        // 6. SIZE over lifetime: sparks shrink as they cool.
        // This curve is multiplied by each spark's starting size. 1 = full size, 0 = gone.
        var sizeLife = lights.sizeOverLifetime;
        sizeLife.enabled = true;
        sizeLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),     // born at full size
            new Keyframe(0.5f, 0.6f), // halfway through life: smaller
            new Keyframe(1f, 0f)));   // end of life: gone

        // 7. FORCE over lifetime: gravity. Sparks arc up and then fall back down.
        var force = lights.forceOverLifetime;
        force.enabled = true;
        force.space = ParticleSystemSimulationSpace.World;      // "down" always means world-down
        force.y = new ParticleSystem.MinMaxCurve(-gravityPull); // negative pulls downward

        // 8. LIMIT VELOCITY over lifetime, used for its DRAG (air resistance).
        // Sparks lose speed over time, so they slow down and hang in the air like real embers.
        var airResist = lights.limitVelocityOverLifetime;
        airResist.enabled = true;
        airResist.limit = 100f;   // a speed cap so high it never triggers. We only want the drag.
        airResist.drag = airDrag; // how strongly the air slows each spark

        // 9. NOISE: a small random wobble so sparks flicker instead of moving in rigid lines.
        var noise = lights.noise;
        noise.enabled = true;
        noise.strength = 0.12f; // how far they wobble (keep it small)
        noise.frequency = 1.5f; // how quickly the wobble changes

        // 10. RENDERER: how each spark is drawn.
        var flag = GetComponent<SpriteRenderer>();
        var rend = lights.GetComponent<ParticleSystemRenderer>();

        // Use the material from the Inspector if you assigned one. Otherwise build a glowing one.
        rend.sharedMaterial = sparkMaterial != null ? sparkMaterial : BuildSparkMaterial();

        // Draw the sparks on the flag's layer, a step in front of it so they're visible.
        if (flag != null)
        {
            rend.sortingLayerID = flag.sortingLayerID;
            rend.sortingOrder = flag.sortingOrder + 2;
        }
    }

    // The color each spark goes through during its life, from birth (0) to death (1).
    private static Gradient MakeSparkGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 0.9f), 0f),     // birth: white-hot core
                new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.3f), // early life: gold
                new GradientColorKey(new Color(1f, 0.35f, 0.05f), 1f)  // end of life: cooling orange
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),    // invisible at birth
                new GradientAlphaKey(1f, 0.1f),  // quickly brighten (no hard pop-in)
                new GradientAlphaKey(1f, 0.55f), // stay bright for a while
                new GradientAlphaKey(0f, 1f)     // fade out completely at the end
            });
        return gradient;
    }

    // =====================================================================
    //  MATERIAL AND TEXTURE (made in code, so no image files are needed)
    // =====================================================================

    // Builds a soft, additive glow material for the sparks.
    // Works with both the Built-in render pipeline and URP.
    private Material BuildSparkMaterial()
    {
        // Ask the project which render pipeline it uses. URP needs its own shader and settings.
        bool usingUrp = GraphicsSettings.currentRenderPipeline != null;

        Shader shader = usingUrp
            ? Shader.Find("Universal Render Pipeline/Particles/Unlit")
            : Shader.Find("Legacy Shaders/Particles/Additive");

        if (shader == null)
        {
            // Unity can leave out shaders that nothing references, so this can happen in builds.
            // Fix: drag a material that uses this shader into the "Spark Material" slot.
            Debug.LogWarning("FlagCheckpoint: particle shader not found. " +
                             "Assign a Spark Material in the Inspector.", this);
            return null;
        }

        ownedMaterial = new Material(shader) { name = "Checkpoint spark (generated)" };
        ownedTexture = MakeGlowTexture();
        ownedMaterial.mainTexture = ownedTexture;

        if (usingUrp)
        {
            // URP's particle shader is set up with properties. These match its Inspector dropdowns.
            ownedMaterial.SetTexture("_BaseMap", ownedTexture);        // URP's name for the main texture
            ownedMaterial.SetFloat("_Surface", 1f);                    // 1 = Transparent
            ownedMaterial.SetFloat("_Blend", 2f);                      // 2 = Additive
            ownedMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            ownedMaterial.SetFloat("_DstBlend", (float)BlendMode.One); // add the light together
            ownedMaterial.SetFloat("_ZWrite", 0f);                     // sparks don't block what's behind them
            ownedMaterial.SetFloat("_Cull", (float)CullMode.Off);      // visible from both sides
            ownedMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            ownedMaterial.renderQueue = (int)RenderQueue.Transparent;  // draw after solid objects
        }

        return ownedMaterial;
    }

    // Draws a soft, round glow in code: bright in the middle, fading to transparent at the edges.
    private static Texture2D MakeGlowTexture(int size = 64)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp, // don't repeat the image past its edges
            filterMode = FilterMode.Bilinear  // smooth the pixels when the spark is scaled
        };

        var pixels = new Color32[size * size];
        float mid = (size - 1) * 0.5f;        // the center of the image, in pixels

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Distance from the center: 0 in the middle, 1 at the edge (corners go past 1).
                float dx = (x - mid) / mid;
                float dy = (y - mid) / mid;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Brightness: 1 at the center, 0 at the edge.
                float alpha = Mathf.Clamp01(1f - dist);
                alpha *= alpha; // squaring makes a bright core with a soft halo, like real light

                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels); // copy the pixels into the texture
        tex.Apply();             // upload them so they can be drawn
        return tex;
    }

    // =====================================================================
    //  CLEANUP
    // =====================================================================

    private void OnDestroy()
    {
        // Free the material and texture this script made, so they don't stay in memory.
        // A material you assigned in the Inspector isn't ours, so we leave it alone.
        if (ownedMaterial != null) Destroy(ownedMaterial);
        if (ownedTexture != null) Destroy(ownedTexture);
    }
}