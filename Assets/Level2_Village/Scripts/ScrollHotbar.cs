using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NoticeboardRunes
{
    public sealed class ScrollHotbar : MonoBehaviour
    {
        public const string ScrollId = "hidden-leaf-scroll";
        private sealed class Item
        {
            public string id;
            public string name;
            public Sprite icon;
        }

        private static readonly List<Item> items = new List<Item>();
        public static bool HasDecodedScroll => items.Exists(item => item.id == ScrollId);

        [SerializeField] private Font labelFont;
        private Canvas canvas;
        private InventoryWheelGraphic wheel;
        private int renderedCount = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void BeginPlaySession() => items.Clear();

        public static void Collect(string id, string displayName, Sprite icon)
        {
            if (!items.Exists(item => item.id == id))
                items.Add(new Item { id = id, name = displayName, icon = icon });
        }

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            wheel = CreateGraphic<InventoryWheelGraphic>("Item wheel", transform, new Vector2(304f, 304f));
            canvas.enabled = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q) && !SceneFader.Instance.IsTransitioning)
                canvas.enabled = !canvas.enabled;

            if (canvas.enabled && renderedCount != items.Count)
                Refresh();
        }

        private void Refresh()
        {
            foreach (Transform child in wheel.transform)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }

            renderedCount = items.Count;
            wheel.SetItemCount(renderedCount);
            if (renderedCount == 0)
            {
                AddLabel("Empty", Vector2.zero, 100f);
                return;
            }

            float radius = renderedCount == 1 ? 0f : 90f;
            float spacing = renderedCount > 1 ? 2f * radius * Mathf.Sin(Mathf.PI / renderedCount) : 120f;
            float size = Mathf.Min(renderedCount == 1 ? 64f : 46f, spacing * 0.72f);
            for (int i = 0; i < renderedCount; i++)
            {
                float angle = i * Mathf.PI * 2f / renderedCount;
                Vector2 center = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
                Image icon = CreateGraphic<Image>(items[i].name, wheel.transform, Vector2.one * size);
                icon.rectTransform.anchoredPosition = center + Vector2.up * 7f;
                icon.sprite = items[i].icon;
                icon.preserveAspect = true;
                if (renderedCount <= 6)
                    AddLabel(items[i].name, center + Vector2.down * (size / 2f + 7f), Mathf.Min(spacing, 120f));
            }
        }

        private void AddLabel(string value, Vector2 position, float width)
        {
            Text label = CreateGraphic<Text>(value, wheel.transform, new Vector2(width, 22f));
            label.rectTransform.anchoredPosition = position;
            label.font = labelFont;
            label.fontSize = 10;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 7;
            label.resizeTextMaxSize = 10;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color32(255, 231, 183, 255);
            label.text = value;
        }

        private static T CreateGraphic<T>(string name, Transform parent, Vector2 size) where T : Graphic
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            T graphic = obj.AddComponent<T>();
            graphic.rectTransform.sizeDelta = size;
            graphic.raycastTarget = false;
            return graphic;
        }
    }
}
