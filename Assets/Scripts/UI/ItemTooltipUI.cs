using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using DungeonDredge.Inventory;

namespace DungeonDredge.UI
{
    /// <summary>
    /// Floating tooltip that displays item details when hovering over inventory items.
    /// </summary>
    public class ItemTooltipUI : MonoBehaviour
    {
        public static ItemTooltipUI Instance { get; private set; }

        [Header("Root")]
        [SerializeField] private RectTransform tooltipRect;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Canvas parentCanvas;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private TextMeshProUGUI itemWeightText;
        [SerializeField] private TextMeshProUGUI itemValueText;
        [SerializeField] private TextMeshProUGUI itemCategoryText;
        [SerializeField] private Image rarityBar;

        [Header("Shape Preview")]
        [SerializeField] private RectTransform shapeContainer;
        [SerializeField] private GameObject shapeCellPrefab;
        [SerializeField] private float shapeCellSize = 12f;
        [SerializeField] private float shapeCellSpacing = 1f;

        [Header("Settings")]
        [SerializeField] private Vector2 offset = new Vector2(16f, -16f);
        [SerializeField] private float screenEdgePadding = 10f;
        [SerializeField] private float fadeSpeed = 15f;

        private bool isShowing = false;
        private float targetAlpha = 0f;

        /// <summary>
        /// When true, Show() calls are ignored. Used when the detail panel is visible
        /// to avoid tooltip clutter.
        /// </summary>
        public bool Suppressed { get; set; } = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (tooltipRect == null)
                tooltipRect = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (parentCanvas == null)
                parentCanvas = GetComponentInParent<Canvas>();

            Hide();
        }

        private void Update()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
            }

            if (isShowing)
            {
                FollowMouse();
            }
        }

        /// <summary>
        /// Show the tooltip for a given item.
        /// </summary>
        public void Show(ItemData itemData)
        {
            if (itemData == null) return;
            if (Suppressed) return;

            isShowing = true;
            targetAlpha = 1f;
            gameObject.SetActive(true);

            // Set content
            if (itemNameText != null)
            {
                itemNameText.text = itemData.itemName;
                itemNameText.color = ItemData.GetRarityColor(itemData.rarity);
            }

            if (itemDescriptionText != null)
            {
                itemDescriptionText.gameObject.SetActive(true);
                itemDescriptionText.text = itemData.description;
            }

            if (itemWeightText != null)
            {
                itemWeightText.gameObject.SetActive(true);
                itemWeightText.text = $"{itemData.weight:F1} kg";
            }

            if (itemValueText != null)
            {
                itemValueText.gameObject.SetActive(true);
                itemValueText.text = $"{itemData.goldValue}g";
            }

            if (itemCategoryText != null)
            {
                itemCategoryText.gameObject.SetActive(true);
                itemCategoryText.text = itemData.category.ToString();
            }

            if (rarityBar != null)
            {
                rarityBar.gameObject.SetActive(true);
                Color rarityColor = ItemData.GetRarityColor(itemData.rarity);
                rarityBar.color = rarityColor;
            }

            // Build shape preview
            if (shapeContainer != null)
            {
                shapeContainer.gameObject.SetActive(true);
                BuildShapePreview(itemData);
            }

            // Force layout rebuild so we get correct size
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

            // Position immediately
            FollowMouse();
        }

        /// <summary>
        /// Show a generic tooltip with title and description.
        /// </summary>
        public void ShowGeneric(string title, string description, Color? titleColor = null)
        {
            if (Suppressed) return;

            isShowing = true;
            targetAlpha = 1f;
            gameObject.SetActive(true);

            if (itemNameText != null)
            {
                itemNameText.text = title;
                itemNameText.color = titleColor ?? Color.white;
            }

            if (itemDescriptionText != null)
            {
                itemDescriptionText.gameObject.SetActive(true);
                itemDescriptionText.text = description;
            }

            // Hide item-specific elements
            if (itemWeightText != null) itemWeightText.gameObject.SetActive(false);
            if (itemValueText != null) itemValueText.gameObject.SetActive(false);
            if (itemCategoryText != null) itemCategoryText.gameObject.SetActive(false);
            if (rarityBar != null) rarityBar.gameObject.SetActive(false);
            if (shapeContainer != null) shapeContainer.gameObject.SetActive(false);

            // Force layout rebuild
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

            // Position immediately
            FollowMouse();
        }

        /// <summary>
        /// Hide the tooltip.
        /// </summary>
        public void Hide()
        {
            isShowing = false;
            targetAlpha = 0f;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        private void FollowMouse()
        {
            if (tooltipRect == null || parentCanvas == null) return;
            if (Mouse.current == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 tooltipPos = mousePos + offset;

            // Clamp to screen bounds
            Vector2 tooltipSize = tooltipRect.sizeDelta;
            float canvasScale = parentCanvas.scaleFactor;

            float rightEdge = tooltipPos.x + tooltipSize.x * canvasScale;
            float bottomEdge = tooltipPos.y - tooltipSize.y * canvasScale;

            if (rightEdge > Screen.width - screenEdgePadding)
            {
                tooltipPos.x = mousePos.x - tooltipSize.x * canvasScale - offset.x;
            }

            if (bottomEdge < screenEdgePadding)
            {
                tooltipPos.y = mousePos.y + tooltipSize.y * canvasScale - offset.y;
            }

            if (tooltipPos.x < screenEdgePadding)
            {
                tooltipPos.x = screenEdgePadding;
            }

            tooltipRect.position = tooltipPos;
        }

        private void BuildShapePreview(ItemData itemData)
        {
            if (shapeContainer == null) return;

            // Clear existing
            foreach (Transform child in shapeContainer)
            {
                Destroy(child.gameObject);
            }

            if (shapeCellPrefab == null) return;

            bool[,] shape = itemData.GetShape();
            int w = itemData.width;
            int h = itemData.height;

            // Set container size
            float totalW = w * (shapeCellSize + shapeCellSpacing) - shapeCellSpacing;
            float totalH = h * (shapeCellSize + shapeCellSpacing) - shapeCellSpacing;
            shapeContainer.sizeDelta = new Vector2(totalW, totalH);

            Color rarityColor = ItemData.GetRarityColor(itemData.rarity);
            rarityColor.a = 0.7f;

            Color emptyColor = new Color(0.15f, 0.15f, 0.15f, 0.4f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    GameObject cell = Instantiate(shapeCellPrefab, shapeContainer);
                    RectTransform cellRect = cell.GetComponent<RectTransform>();

                    if (cellRect != null)
                    {
                        cellRect.anchoredPosition = new Vector2(
                            x * (shapeCellSize + shapeCellSpacing),
                            -y * (shapeCellSize + shapeCellSpacing));
                        cellRect.sizeDelta = new Vector2(shapeCellSize, shapeCellSize);
                    }

                    Image cellImage = cell.GetComponent<Image>();
                    if (cellImage != null)
                    {
                        cellImage.color = shape[x, y] ? rarityColor : emptyColor;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
