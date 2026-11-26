using UnityEngine;
using UnityEngine.UI;

// Split the existing sprite into little squares. The original sprite alpha still shapes the letters.
public class TitleDissolve : BaseMeshEffect
{
    private float progress;

    public void SetProgress(float value)
    {
        if (progress == value)
            return;
        progress = value;
        graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper mesh)
    {
        if (!IsActive() || progress <= 0f || mesh.currentVertCount != 4)
            return;

        var bottom = new UIVertex();
        var top = new UIVertex();
        mesh.PopulateUIVertex(ref bottom, 0);
        mesh.PopulateUIVertex(ref top, 2);
        mesh.Clear();

        const int columns = 48;
        int rows = Mathf.Max(1, Mathf.RoundToInt(columns * (top.position.y - bottom.position.y)
            / Mathf.Max(1f, top.position.x - bottom.position.x)));
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < columns; x++)
        {
            // A fixed staggered order keeps the same squares gone on the next frame.
            float threshold = ((x * 127 + y * 197 + x * y * 17) % 257) / 257f;
            if (threshold < progress)
                continue;

            int first = mesh.currentVertCount;
            AddCorner(mesh, bottom, top, x / (float)columns, y / (float)rows);
            AddCorner(mesh, bottom, top, x / (float)columns, (y + 1f) / rows);
            AddCorner(mesh, bottom, top, (x + 1f) / columns, (y + 1f) / rows);
            AddCorner(mesh, bottom, top, (x + 1f) / columns, y / (float)rows);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }

    private static void AddCorner(VertexHelper mesh, UIVertex bottom, UIVertex top, float x, float y)
    {
        var corner = bottom;
        corner.position.x = Mathf.Lerp(bottom.position.x, top.position.x, x);
        corner.position.y = Mathf.Lerp(bottom.position.y, top.position.y, y);
        corner.uv0.x = Mathf.Lerp(bottom.uv0.x, top.uv0.x, x);
        corner.uv0.y = Mathf.Lerp(bottom.uv0.y, top.uv0.y, y);
        mesh.AddVert(corner);
    }
}
