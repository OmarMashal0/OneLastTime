using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The title break-up effect used by LevelTitleFade. It copies the title's letter shapes into a mesh made of
// many small squares, then moves and fades each square on its own so the title crumbles away.
public class TitleFleck : MonoBehaviour
{
    private struct Fragment
    {
        public float release;
        public Vector2 velocity;
    }

    private static readonly Color32 Plum = new Color32(66, 38, 65, 255);
    private readonly List<Fragment> fragments = new List<Fragment>();
    private Mesh mesh;
    private CanvasRenderer canvasRenderer;
    private Vector3[] originalPositions;
    private Vector3[] positions;
    private Color32[] originalColors;
    private Color32[] colors;
    private float duration;
    private float elapsed;

    public void Initialize(TextMeshProUGUI text, int fragmentCount, float driftSpeed, float duration)
    {
        this.duration = duration;
        TMP_TextInfo textInfo = text.textInfo;
        float totalArea = 0f;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (character.isVisible)
            {
                Vector2 size = character.topRight - character.bottomLeft;
                totalArea += size.x * size.y;
            }
        }

        float tileSize = Mathf.Max(1f, Mathf.Sqrt(totalArea / Mathf.Max(1, fragmentCount)));
        var vertices = new List<Vector3>();
        var atlasUvs = new List<Vector2>();
        var sdfUvs = new List<Vector2>();
        var vertexColors = new List<Color32>();
        var triangles = new List<List<int>>();
        for (int i = 0; i < textInfo.materialCount; i++)
            triangles.Add(new List<int>());

        var random = new System.Random(31);
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (!character.isVisible)
                continue;

            int materialIndex = character.materialReferenceIndex;
            TMP_MeshInfo glyphMesh = textInfo.meshInfo[materialIndex];
            int first = character.vertexIndex;
            int columns = Mathf.Max(1, Mathf.RoundToInt((character.topRight.x - character.bottomLeft.x) / tileSize));
            int rows = Mathf.Max(1, Mathf.RoundToInt((character.topRight.y - character.bottomLeft.y) / tileSize));

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int tileStart = vertices.Count;
                    for (int corner = 0; corner < 4; corner++)
                    {
                        float x = (column + (corner >= 2 ? 1f : 0f)) / columns;
                        float y = (row + (corner == 1 || corner == 2 ? 1f : 0f)) / rows;
                        vertices.Add(Vector3.Lerp(
                            Vector3.Lerp(glyphMesh.vertices[first], glyphMesh.vertices[first + 3], x),
                            Vector3.Lerp(glyphMesh.vertices[first + 1], glyphMesh.vertices[first + 2], x), y));
                        atlasUvs.Add(Vector2.Lerp(
                            Vector2.Lerp(glyphMesh.uvs0[first], glyphMesh.uvs0[first + 3], x),
                            Vector2.Lerp(glyphMesh.uvs0[first + 1], glyphMesh.uvs0[first + 2], x), y));

                        // TMP stores SDF scale in the second UV channel.
                        sdfUvs.Add(glyphMesh.uvs2[first]);
                        vertexColors.Add(glyphMesh.colors32[first]);
                    }

                    List<int> indices = triangles[materialIndex];
                    indices.Add(tileStart);
                    indices.Add(tileStart + 1);
                    indices.Add(tileStart + 2);
                    indices.Add(tileStart + 2);
                    indices.Add(tileStart + 3);
                    indices.Add(tileStart);

                    // Stagger the breakup without touching Unity's gameplay random state.
                    float bias = (row + 0.5f) / rows;
                    fragments.Add(new Fragment
                    {
                        release = ((float)random.NextDouble() * 0.58f + bias * 0.16f) * duration,
                        velocity = new Vector2((float)random.NextDouble() * 1.6f - 0.6f,
                            0.45f + (float)random.NextDouble() * 0.65f) * driftSpeed
                    });
                }
            }
        }

        originalPositions = vertices.ToArray();
        originalColors = vertexColors.ToArray();
        positions = (Vector3[])originalPositions.Clone();
        colors = (Color32[])originalColors.Clone();

        // Keep the original atlas, SDF shader and vertex alpha. The atlas masks off
        // empty parts of each tile, so only fragments of the actual strokes show.
        mesh = new Mesh { name = "Level title fragments" };
        mesh.MarkDynamic();
        mesh.vertices = positions;
        mesh.colors32 = colors;
        mesh.SetUVs(0, atlasUvs);
        mesh.SetUVs(1, sdfUvs);
        mesh.subMeshCount = triangles.Count;
        for (int i = 0; i < triangles.Count; i++)
            mesh.SetTriangles(triangles[i], i);

        canvasRenderer = GetComponent<CanvasRenderer>();
        canvasRenderer.materialCount = triangles.Count;
        for (int i = 0; i < triangles.Count; i++)
            canvasRenderer.SetMaterial(textInfo.meshInfo[i].material, i);
        if (triangles.Count == 1)
            canvasRenderer.SetTexture(textInfo.meshInfo[0].material.mainTexture);
        canvasRenderer.SetColor(Color.white);
        canvasRenderer.SetMesh(mesh);
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (mesh == null || duration <= 0f)
            return;

        elapsed += Time.deltaTime;
        for (int i = 0; i < fragments.Count; i++)
        {
            Fragment fragment = fragments[i];
            float age = elapsed - fragment.release;
            if (age <= 0f)
                continue;

            float progress = Mathf.Clamp01(age / (duration * 0.26f));
            Vector2 drift = fragment.velocity * age;
            Vector3 offset = new Vector3(Mathf.Round(drift.x), Mathf.Round(drift.y));
            int first = i * 4;
            Color32 color = Color32.Lerp(originalColors[first], Plum, Mathf.Clamp01(progress * 2f));
            color.a = (byte)(originalColors[first].a * (1f - Mathf.InverseLerp(0.35f, 1f, progress)));

            for (int corner = 0; corner < 4; corner++)
            {
                positions[first + corner] = originalPositions[first + corner] + offset;
                colors[first + corner] = color;
            }
        }

        mesh.vertices = positions;
        mesh.colors32 = colors;
        mesh.RecalculateBounds();
        canvasRenderer.SetMesh(mesh);
    }

    private void OnDestroy()
    {
        if (mesh != null)
            Destroy(mesh);
    }
}
