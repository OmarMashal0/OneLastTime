using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Level3Bamboo
{
    // The quest scroll under the log counter. It always stays on screen as a small tab showing the current
    // objective's title. Clicking the tab (or its icon) opens it to show the instruction and the log progress;
    // clicking again closes it back to the tab. Opening it never restarts the quest: it only shows the text
    // the quest director last sent.
    public class BambooQuestScroll : MonoBehaviour
    {
        public RectTransform card;
        public RectTransform paperWindow;
        public RectTransform bottomRod;
        public CanvasGroup visibility;
        public TMP_Text titleLabel;
        public TMP_Text instructionLabel;
        public TMP_FontAsset swampFont;
        public Material swampHeaderMaterial;
        public Material swampBodyMaterial;
        public Image checkbox;
        public Image objectiveIcon;
        public Sprite emptyBox;
        public Sprite checkedBox;
        public Image[] leaves;
        public AudioSource audioSource;
        public AudioClip writingTick;
        public AudioClip paperClose;
        public CanvasGroup entranceTitle;
        public CanvasGroup encounterTitle;
        public float lettersPerSecond = 35f;
        public float instructionHold = 4f;
        public float completionHold = 3.8f;
        public float openHeight = 212f;
        public float reminderHeight = 48f;

        [Header("Click to open / close")]
        [Tooltip("Invisible button over the visible part of the scroll (the tab and its icon).")]
        public Button tabButton;
        [Tooltip("Used for the progress line when the scroll is opened by a click.")]
        public LogCollection logs;
        [Tooltip("Shown in the tab when no log site is active.")]
        public string defaultTitle = "FIND THE SIX LOGS";
        [TextArea] public string defaultInstruction = "Follow the trail. Each challenge hides one log.";
        public Sprite defaultIcon;
        public float swampTitleSize = 42f;
        public float swampBodySize = 38f;

        private Coroutine sequence;
        private Coroutine leafBurst;
        private bool opened;
        private bool expanded;   // true while the instruction is showing
        private string currentInstruction;
        private TMP_FontAsset titleFont, instructionFont;
        private Material titleMaterial, instructionMaterial;
        private float titleSize, instructionSize;
        private RectTransform root;
        private Vector2 homePosition;
        private readonly Vector3[] titleCorners = new Vector3[4];

        // Start is called before the first frame update
        void Start()
        {
            root = (RectTransform)transform;
            homePosition = root.anchoredPosition;
            titleFont = titleLabel.font;
            instructionFont = instructionLabel.font;
            titleMaterial = titleLabel.fontSharedMaterial;
            instructionMaterial = instructionLabel.fontSharedMaterial;
            titleSize = titleLabel.fontSize;
            instructionSize = instructionLabel.fontSize;
            tabButton.onClick.AddListener(ToggleOpen);
            Hide();
        }

        void OnDestroy()
        {
            if (tabButton != null)
                tabButton.onClick.RemoveListener(ToggleOpen);
        }

        // Called by the tab button. Paused game = no clicks (the scroll is frozen with everything else).
        public void ToggleOpen()
        {
            if (Time.timeScale <= 0f)
                return;

            bool wasOpen = expanded || sequence != null;
            CancelSequence();
            if (wasOpen)
                Collapse();
            else
                Expand();
        }

        private void Expand()
        {
            // Same objective as before, plus the current log progress.
            instructionLabel.text = currentInstruction + ProgressLine();
            instructionLabel.maxVisibleCharacters = int.MaxValue;
            float height = OpenHeightFor(instructionLabel.text);
            sequence = StartCoroutine(AnimatePose(paperWindow.rect.height, height, card.localScale.x, 1f, 1f, 0.2f));
            expanded = true;
        }

        private void Collapse()
        {
            instructionLabel.maxVisibleCharacters = 0;
            sequence = StartCoroutine(AnimatePose(paperWindow.rect.height, reminderHeight, card.localScale.x, 0.83f, 1f, 0.2f));
            expanded = false;
        }

        private string ProgressLine()
        {
            if (logs == null)
                return "";
            return "\n<size=88%>Logs collected: " + logs.CollectedCount + "/" + logs.logs.Length + "</size>";
        }

        private float OpenHeightFor(string text)
        {
            RectTransform textBounds = instructionLabel.rectTransform;
            float contentHeight = instructionLabel.GetPreferredValues(text, textBounds.rect.width, 0f).y;
            return Mathf.Clamp(-textBounds.anchoredPosition.y + contentHeight + 18f, reminderHeight + 40f, openHeight);
        }

        void LateUpdate()
        {
            float targetY = PositionBelow(entranceTitle, homePosition.y);
            targetY = PositionBelow(encounterTitle, targetY);
            float y = targetY < homePosition.y ? targetY
                : Mathf.MoveTowards(root.anchoredPosition.y, targetY, Time.deltaTime * 680f);
            root.anchoredPosition = new Vector2(homePosition.x, y);
        }

        private float PositionBelow(CanvasGroup overlay, float targetY)
        {
            if (overlay == null || overlay.alpha <= 0.002f)
                return targetY;
            ((RectTransform)overlay.transform).GetWorldCorners(titleCorners);
            var parent = (RectTransform)root.parent;
            float bottom = parent.InverseTransformPoint(titleCorners[0]).y - parent.rect.yMax;
            return Mathf.Min(targetY, bottom - 14f);
        }

        public void Present(string title, string instruction, Sprite icon, bool swampStyle = false)
        {
            CancelSequence();
            SetContent(title, instruction, icon, false, swampStyle);
            sequence = StartCoroutine(TypeInstruction());
        }

        public void RestoreReminder(string title, string instruction, Sprite icon, bool showInstruction = false, bool swampStyle = false)
        {
            CancelSequence();
            CancelLeaves();
            SetContent(title, instruction, icon, false, swampStyle);
            instructionLabel.maxVisibleCharacters = showInstruction ? int.MaxValue : 0;
            float height = reminderHeight;
            if (showInstruction)
                height = OpenHeightFor(instruction);
            SetPose(height, showInstruction ? 1f : 0.83f, 1f);
            opened = true;
            expanded = showInstruction;
        }

        public void Complete(Sprite icon)
        {
            CancelSequence();
            SetContent("LOGS SECURED", "All six logs collected. Head to the river.", icon, true);
            instructionLabel.maxVisibleCharacters = int.MaxValue;
            sequence = StartCoroutine(Finish());
        }

        // A finished objective: the small tab with a ticked box. Clicking it still opens the text.
        public void ShowDone(string title, string instruction, Sprite icon)
        {
            CancelSequence();
            CancelLeaves();
            SetContent(title, instruction, icon, true);
            ShowTab();
        }

        // No active objective: keep the small tab with the general goal so it can still be clicked.
        public void Hide()
        {
            CancelSequence();
            CancelLeaves();
            SetContent(defaultTitle, defaultInstruction, defaultIcon, false);
            ShowTab();
        }

        private void ShowTab()
        {
            instructionLabel.maxVisibleCharacters = 0;
            SetPose(reminderHeight, 0.83f, 1f);
            opened = true;
            expanded = false;
        }

        private void SetContent(string title, string instruction, Sprite icon, bool done, bool swampStyle = false)
        {
            bool useSwampFont = swampStyle && swampFont != null;
            titleLabel.font = useSwampFont ? swampFont : titleFont;
            titleLabel.fontSharedMaterial = useSwampFont ? swampHeaderMaterial : titleMaterial;
            titleLabel.fontSize = useSwampFont ? swampTitleSize : titleSize;
            instructionLabel.font = useSwampFont ? swampFont : instructionFont;
            instructionLabel.fontSharedMaterial = useSwampFont ? swampBodyMaterial : instructionMaterial;
            instructionLabel.fontSize = useSwampFont ? swampBodySize : instructionSize;
            currentInstruction = instruction;
            titleLabel.text = title;
            instructionLabel.text = instruction;
            checkbox.sprite = done ? checkedBox : emptyBox;
            objectiveIcon.sprite = icon;
            objectiveIcon.enabled = icon != null;
        }

        private IEnumerator TypeInstruction()
        {
            instructionLabel.maxVisibleCharacters = 0;
            instructionLabel.ForceMeshUpdate();
            TMP_TextInfo info = instructionLabel.textInfo;
            var characters = new char[info.characterCount];
            for (int i = 0; i < characters.Length; i++)
                characters[i] = info.characterInfo[i].character;

            float start = Mathf.Max(reminderHeight, paperWindow.rect.height);
            SetPose(start, opened ? card.localScale.x : 0.92f, 1f);
            yield return AnimatePose(start, openHeight, card.localScale.x, 1f, 1f, 0.23f);
            opened = true;
            expanded = true;

            // TMP lays out the entire sentence before revealing individual letters.
            float clock = 0f;
            float nextLetter = 0f;
            int visible = 0;
            int sinceTick = 0;
            int tickAfter = 2;
            while (visible < characters.Length)
            {
                clock += Time.deltaTime;
                bool tickNow = false;
                while (visible < characters.Length && clock >= nextLetter)
                {
                    char c = characters[visible++];
                    instructionLabel.maxVisibleCharacters = visible;
                    nextLetter += 1f / Mathf.Max(1f, lettersPerSecond);
                    if (char.IsLetterOrDigit(c) && ++sinceTick >= tickAfter)
                    {
                        tickNow = true;
                        sinceTick = 0;
                        tickAfter = 5 - tickAfter;
                    }
                    if (c == '.' || c == '!' || c == '?')
                        nextLetter += 0.17f;
                    else if (c == ',' || c == ':' || c == ';')
                        nextLetter += 0.09f;
                }
                if (tickNow && writingTick != null)
                    audioSource.PlayOneShot(writingTick, 0.32f);
                yield return null;
            }

            yield return new WaitForSeconds(instructionHold);
            yield return AnimatePose(openHeight, reminderHeight, 1f, 0.83f, 1f, 0.27f);
            instructionLabel.maxVisibleCharacters = 0;
            expanded = false;
            sequence = null;
        }

        private IEnumerator Finish()
        {
            float start = Mathf.Max(reminderHeight, paperWindow.rect.height);
            SetPose(start, opened ? card.localScale.x : 0.92f, 1f);
            yield return AnimatePose(start, openHeight, card.localScale.x, 1f, 1f, 0.19f);
            opened = true;
            yield return new WaitForSeconds(completionHold);
            if (paperClose != null)
                audioSource.PlayOneShot(paperClose, 0.62f);
            CancelLeaves();
            leafBurst = StartCoroutine(ScatterLeaves());
            yield return AnimatePose(openHeight, 0f, 1f, 0.62f, 0f, 0.30f);

            // Afterwards the tab comes back with the next goal.
            SetContent("HEAD TO THE RIVER", "All six logs are collected. Follow the trail to the river.", objectiveIcon.sprite, true);
            ShowTab();
            sequence = null;
        }

        private IEnumerator AnimatePose(float fromHeight, float height, float fromScale, float scale, float alpha, float duration)
        {
            float oldAlpha = visibility.alpha;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, time / duration);
                SetPose(Mathf.Lerp(fromHeight, height, t), Mathf.Lerp(fromScale, scale, t), Mathf.Lerp(oldAlpha, alpha, t));
                yield return null;
            }
            SetPose(height, scale, alpha);
        }

        private void SetPose(float height, float scale, float alpha)
        {
            paperWindow.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            bottomRod.anchoredPosition = new Vector2(0f, -height);
            card.localScale = Vector3.one * scale;
            visibility.alpha = alpha;
        }

        private IEnumerator ScatterLeaves()
        {
            for (float time = 0f; time < 0.72f; time += Time.deltaTime)
            {
                float progress = time / 0.72f;
                for (int i = 0; i < leaves.Length; i++)
                {
                    Image leaf = leaves[i];
                    leaf.enabled = true;
                    float drift = 65f + i * 15f;
                    leaf.rectTransform.anchoredPosition = new Vector2(110f + i * 43f + progress * drift,
                        -40f - progress * (43f + i * 10f) + Mathf.Sin(progress * Mathf.PI) * 23f);
                    leaf.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i * 31f + progress * 115f);
                    leaf.color = new Color(1f, 1f, 1f, 1f - progress);
                }
                yield return null;
            }
            foreach (Image leaf in leaves)
                leaf.enabled = false;
            leafBurst = null;
        }

        private void CancelSequence()
        {
            if (sequence != null)
                StopCoroutine(sequence);
            sequence = null;
        }

        private void CancelLeaves()
        {
            if (leafBurst != null)
                StopCoroutine(leafBurst);
            leafBurst = null;
            foreach (Image leaf in leaves)
                leaf.enabled = false;
        }
    }
}
