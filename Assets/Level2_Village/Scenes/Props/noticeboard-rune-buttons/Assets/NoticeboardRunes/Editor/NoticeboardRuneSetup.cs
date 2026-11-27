using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NoticeboardRunes.Editor
{
    internal static class NoticeboardRuneSetup
    {
        private const string MenuPath = "Tools/Noticeboard/Add 26 Rune Buttons";
        private const string CanvasName = "Noticeboard Rune Buttons";
        private const string UndoName = "Add Noticeboard Rune Buttons";
        private static readonly Vector2 ArtworkSize = new Vector2(1280f, 720f);

        private struct Rune
        {
            public readonly string Name;
            public readonly Vector2 Center;
            public readonly float Width;

            public Rune(string name, float x, float y, float width)
            {
                Name = name;
                Center = new Vector2(x, y);
                Width = width;
            }
        }

        private static readonly Rune[] Layout =
        {
            new Rune("Crescent", 129f, 168f, 96f),
            new Rune("Mountain", 235f, 168f, 96f),
            new Rune("Compass", 341f, 168f, 96f),
            new Rune("Spiral", 447f, 168f, 96f),
            new Rune("Eye", 553f, 168f, 96f),
            new Rune("Feather", 659f, 168f, 96f),
            new Rune("Sun", 765f, 168f, 96f),
            new Rune("Key", 129f, 296f, 96f),
            new Rune("Tide", 235f, 296f, 96f),
            new Rune("Lantern", 341f, 296f, 96f),
            new Rune("Frost", 447f, 296f, 96f),
            new Rune("Hourglass", 553f, 296f, 96f),
            new Rune("Lightning", 659f, 296f, 96f),
            new Rune("Branch", 765f, 296f, 96f),
            new Rune("Flame", 129f, 424f, 96f),
            new Rune("Kite", 235f, 424f, 96f),
            new Rune("Gate", 341f, 424f, 96f),
            new Rune("Lotus", 447f, 424f, 96f),
            new Rune("Trident", 553f, 424f, 96f),
            new Rune("Cloud", 659f, 424f, 96f),
            new Rune("Hook", 765f, 424f, 96f),
            new Rune("Fish", 235f, 552f, 96f),
            new Rune("Crown", 341f, 552f, 96f),
            new Rune("Gem", 447f, 552f, 96f),
            new Rune("Maze", 553f, 552f, 96f),
            new Rune("Comet", 659f, 552f, 96f),
        };

        [MenuItem(MenuPath, true)]
        private static bool CanAddButtons()
        {
            GameObject selected = Selection.activeGameObject;
            return !EditorApplication.isPlayingOrWillChangePlaymode && selected != null &&
                selected.scene.IsValid() && selected.GetComponent<SpriteRenderer>() != null;
        }

        [MenuItem(MenuPath)]
        private static void AddButtons()
        {
            var board = Selection.activeGameObject.GetComponent<SpriteRenderer>();
            if (board.sprite == null)
            {
                EditorUtility.DisplayDialog("Noticeboard", "Assign a board sprite or an animation frame to the Sprite Renderer first.", "OK");
                return;
            }

            Transform existing = board.transform.Find(CanvasName);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            string menuScript = AssetDatabase.FindAssets("NoticeboardRuneSetup t:MonoScript")[0];
            string editorPath = System.IO.Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(menuScript));
            string directory = System.IO.Path.GetDirectoryName(editorPath).Replace('\\', '/') + "/Sprites/";
            var bases = new Sprite[Layout.Length];
            var glows = new Sprite[Layout.Length];
            for (int i = 0; i < Layout.Length; i++)
            {
                string number = (i + 1).ToString("00");
                bases[i] = AssetDatabase.LoadAssetAtPath<Sprite>(directory + "button_" + number + ".png");
                glows[i] = AssetDatabase.LoadAssetAtPath<Sprite>(directory + "glow_" + number + ".png");
                if (bases[i] == null || glows[i] == null)
                {
                    EditorUtility.DisplayDialog("Noticeboard", "The rune sprites could not be found. Keep the Editor and Sprites folders together under NoticeboardRunes.", "OK");
                    return;
                }
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            var root = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(root, UndoName);
            Undo.SetTransformParent(root.transform, board.transform, UndoName);
            root.layer = board.gameObject.layer;

            var rect = (RectTransform)root.transform;
            Bounds artwork = board.sprite.bounds;
            rect.sizeDelta = ArtworkSize;
            rect.localPosition = artwork.center + Vector3.back * 0.01f;
            rect.localRotation = Quaternion.identity;
            rect.localScale = new Vector3(artwork.size.x / ArtworkSize.x, artwork.size.y / ArtworkSize.y, 1f);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingLayerID = board.sortingLayerID;
            canvas.sortingOrder = Mathf.Min(short.MaxValue, board.sortingOrder + 1);
            canvas.worldCamera = FindBoardCamera(board);

            for (int i = 0; i < Layout.Length; i++)
                CreateButton(rect, Layout[i], i, bases[i], glows[i]);

            EnsureEventSystem(board.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = root;
        }

        private static void CreateButton(RectTransform parent, Rune rune, int index, Sprite faceSprite, Sprite glowSprite)
        {
            var item = new GameObject(string.Format("Rune {0:00} - {1}", index + 1, rune.Name),
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(NoticeboardRuneGlow));
            Undo.RegisterCreatedObjectUndo(item, UndoName);
            item.layer = parent.gameObject.layer;
            var rect = (RectTransform)item.transform;
            Undo.SetTransformParent(rect, parent, UndoName);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.localPosition = new Vector3(rune.Center.x - ArtworkSize.x / 2f, ArtworkSize.y / 2f - rune.Center.y, 0f);
            rect.sizeDelta = new Vector2(rune.Width, rune.Width * 0.75f);

            var face = item.GetComponent<Image>();
            face.sprite = faceSprite;
            var button = item.GetComponent<Button>();
            button.targetGraphic = face;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.93f, 0.78f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.65f, 0.43f, 0.28f);
            button.colors = colors;

            var overlay = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(overlay, UndoName);
            overlay.layer = item.layer;
            var overlayRect = (RectTransform)overlay.transform;
            Undo.SetTransformParent(overlayRect, rect, UndoName);
            overlayRect.localScale = Vector3.one;
            overlayRect.localRotation = Quaternion.identity;
            overlayRect.localPosition = Vector3.zero;
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var glow = overlay.GetComponent<Image>();
            glow.sprite = glowSprite;
            glow.raycastTarget = false;
            glow.color = new Color(1f, 1f, 1f, 0.45f);
            item.GetComponent<NoticeboardRuneGlow>().Configure(glow, index * 0.173f);
        }

        private static Camera FindBoardCamera(SpriteRenderer board)
        {
            int layerMask = 1 << board.gameObject.layer;
            if (Camera.main != null && (Camera.main.cullingMask & layerMask) != 0)
                return Camera.main;

            foreach (Camera camera in Camera.allCameras)
            {
                if (camera.gameObject.scene == board.gameObject.scene && (camera.cullingMask & layerMask) != 0)
                    return camera;
            }
            return null;
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;

            var events = new GameObject("EventSystem", typeof(EventSystem));
            SceneManager.MoveGameObjectToScene(events, scene);
            Undo.RegisterCreatedObjectUndo(events, UndoName);
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var inputModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModule != null)
            {
                Component module = Undo.AddComponent(events, inputModule);
                inputModule.GetMethod("AssignDefaultActions")?.Invoke(module, null);
            }
#else
            Undo.AddComponent<StandaloneInputModule>(events);
#endif
        }
    }
}
