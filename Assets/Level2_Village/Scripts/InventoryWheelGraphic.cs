using UnityEngine;
using UnityEngine.UI;

namespace NoticeboardRunes
{
    public sealed class InventoryWheelGraphic : MaskableGraphic
    {
        private int count;
        public void SetItemCount(int value)
        {
            count = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            int slices = Mathf.Max(1, count);
            float arc = 2f * Mathf.PI / slices;
            float radius = rectTransform.rect.width * 0.5f;
            Color32 outline = new Color32(43, 29, 28, 245);
            Color32 brass = new Color32(196, 151, 86, 255);
            Band(mesh, radius - 6f, radius, 0f, 2f * Mathf.PI, outline);
            Band(mesh, radius - 4f, radius - 2f, 0f, 2f * Mathf.PI, brass);

            for (int i = 0; i < slices; i++)
            {
                float gap = slices == 1 ? 0f : Mathf.Min(0.009f, arc * 0.05f);
                float start = (i - 0.5f) * arc + gap;
                float end = (i + 0.5f) * arc - gap;
                Color32 fill = i % 2 == 0 ? new Color32(55, 46, 43, 201) : new Color32(46, 41, 43, 201);
                Band(mesh, slices == 1 ? 0f : 14f, radius - 6f, start, end, fill);
            }
            if (slices > 1)
                Band(mesh, 10f, 12f, 0f, 2f * Mathf.PI, brass);
        }

        private static void Band(VertexHelper mesh, float inner, float outer, float start, float end, Color32 tint)
        {
            int steps = Mathf.Max(2, Mathf.CeilToInt((end - start) * 20f));
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(start, end, (float)i / steps);
                Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                int index = mesh.currentVertCount;
                mesh.AddVert(direction * inner, tint, Vector2.zero);
                mesh.AddVert(direction * outer, tint, Vector2.zero);
                if (i == 0) continue;
                mesh.AddTriangle(index - 2, index - 1, index + 1);
                mesh.AddTriangle(index - 2, index + 1, index);
            }
        }
    }
}
