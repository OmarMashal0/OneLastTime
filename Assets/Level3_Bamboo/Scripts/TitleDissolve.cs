using UnityEngine;
using UnityEngine.UI;

// The "breaking apart" effect on the Level 3 title letters (used by LevelTitleFade).
//
// Why it is code: a CanvasGroup can only fade the whole title evenly. To make the letters crumble
// into small squares that vanish one by one, the Image's flat rectangle (4 corners) is rebuilt as a
// 48-column grid of small squares, and squares are left out as progress goes from 0 to 1.
// The original sprite's transparency still shapes the letters, so each square shows its piece of them.
//
// How it runs: LevelTitleFade calls SetProgress(0..1) every frame of the fade-out. Unity UI then calls
// ModifyMesh (BaseMeshEffect) whenever the Image is redrawn, and this script decides which squares to draw.
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
        // Nothing to do before the fade-out starts (or if the Image is not a simple rectangle).
        if (!IsActive() || progress <= 0f || mesh.currentVertCount != 4)
            return;

        // Read the bottom-left and top-right corners of the Image, then throw the rectangle away.

        var bottom = new UIVertex();
        var top = new UIVertex();
        mesh.PopulateUIVertex(ref bottom, 0);
        mesh.PopulateUIVertex(ref top, 2);
        mesh.Clear();

        // Make the squares roughly square: same count across, rows depend on the Image's height.
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

            // Draw this square: 4 corners and 2 triangles.
            int first = mesh.currentVertCount;
            AddCorner(mesh, bottom, top, x / (float)columns, y / (float)rows);
            AddCorner(mesh, bottom, top, x / (float)columns, (y + 1f) / rows);
            AddCorner(mesh, bottom, top, (x + 1f) / columns, (y + 1f) / rows);
            AddCorner(mesh, bottom, top, (x + 1f) / columns, y / (float)rows);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }

    // One corner of a square, at a fraction (x, y) of the way across the Image: position and texture spot.
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
