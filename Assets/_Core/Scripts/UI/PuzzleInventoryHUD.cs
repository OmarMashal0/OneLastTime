using UnityEngine;
using UnityEngine.UI;

namespace OneLastTime.UI
{
    public enum PuzzlePieceType
    {
        None,
        JapaneseWave,
        CherryBlossom
    }

    public class PuzzleInventoryHUD : MonoBehaviour
    {
        private static PuzzleInventoryHUD instance;
        public static PuzzleInventoryHUD Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<PuzzleInventoryHUD>(true);
                    if (instance == null)
                    {
                        var all = Resources.FindObjectsOfTypeAll<PuzzleInventoryHUD>();
                        if (all != null && all.Length > 0)
                        {
                            instance = all[0];
                        }
                    }
                }
                return instance;
            }
            private set => instance = value;
        }

        // static state ensures inventory is preserved reliably regardless of ui lifecycle
        public static PuzzlePieceType HeldPiece { get; private set; } = PuzzlePieceType.None;
        public static bool HasPiece => HeldPiece != PuzzlePieceType.None;
        public static bool IsDiscovered { get; private set; } = false;

        [Header("Sprites")]
        [SerializeField] private Sprite emptySlotSprite;
        [SerializeField] private Sprite fullSlotSprite;

        [Header("UI Image")]
        [SerializeField] private Image slotImage;

        public PuzzlePieceType CurrentPiece => HeldPiece;

        void Awake()
        {
            Instance = this;

            if (slotImage == null)
            {
                slotImage = GetComponentInChildren<Image>(true);
            }

            // keep visual slot image hidden until discovered
            if (slotImage != null)
            {
                slotImage.enabled = IsDiscovered;
            }

            UpdateSprite();
        }

        public void Discover()
        {
            IsDiscovered = true;

            if (slotImage == null)
            {
                slotImage = GetComponentInChildren<Image>(true);
            }

            UpdateSprite();

            if (slotImage != null)
            {
                slotImage.enabled = true;
            }
        }

        public void HideHUD()
        {
            HeldPiece = PuzzlePieceType.None;
            IsDiscovered = false;

            if (slotImage == null)
            {
                slotImage = GetComponentInChildren<Image>(true);
            }

            if (slotImage != null)
            {
                slotImage.enabled = false;
            }
        }

        public void SetHoldingPiece(bool holding, PuzzlePieceType pieceType = PuzzlePieceType.None)
        {
            HeldPiece = holding ? pieceType : PuzzlePieceType.None;

            if (holding)
            {
                IsDiscovered = true;
            }

            if (slotImage == null)
            {
                slotImage = GetComponentInChildren<Image>(true);
            }

            UpdateSprite();

            if (slotImage != null)
            {
                slotImage.enabled = IsDiscovered;
            }
        }

        public void ShowHUD(bool show)
        {
            if (slotImage == null)
            {
                slotImage = GetComponentInChildren<Image>(true);
            }

            if (slotImage != null)
            {
                slotImage.enabled = show;
            }
        }

        public void UpdateSprite()
        {
            if (slotImage == null) return;

            if (HasPiece)
            {
                if (fullSlotSprite != null)
                {
                    slotImage.sprite = fullSlotSprite;
                }
            }
            else
            {
                if (emptySlotSprite != null)
                {
                    slotImage.sprite = emptySlotSprite;
                }
            }
        }
    }
}
