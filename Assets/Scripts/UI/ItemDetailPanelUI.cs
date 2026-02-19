using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DungeonDredge.Inventory;

namespace DungeonDredge.UI
{
    /// <summary>
    /// Side panel that shows detailed information about the currently selected inventory item.
    /// Includes icon preview, stats, shape preview, and action buttons (Rotate, Drop).
    /// Built programmatically at runtime so no prefab/scene wiring is needed.
    /// </summary>
    public class ItemDetailPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryUI inventoryUI;

        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Item Info")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private TextMeshProUGUI itemWeightText;
        [SerializeField] private TextMeshProUGUI itemValueText;
        [SerializeField] private TextMeshProUGUI itemCategoryText;
        [SerializeField] private TextMeshProUGUI itemRarityText;
        [SerializeField] private Image rarityBar;

        [Header("Shape Preview")]
        [SerializeField] private RectTransform shapeContainer;
        [SerializeField] private float shapeCellSize = 16f;
        [SerializeField] private float shapeCellSpacing = 2f;

        [Header("Action Buttons")]
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button dropButton;

        [Header("Settings")]
        [SerializeField] private float panelWidth = 220f;

        // State
        private InventoryItem currentItem;
        private bool isBuilt = false;

        private void Start()
        {
            // Subscribe to selection events from InventoryUI
            if (inventoryUI != null)
            {
                inventoryUI.OnItemSelected += OnItemSelected;
                inventoryUI.OnItemDeselected += OnItemDeselected;
            }

            // Build UI if not wired in scene
            if (!isBuilt)
            {
                BuildUI();
            }

            HidePanel();
        }

        private void OnDestroy()
        {
            if (inventoryUI != null)
            {
                inventoryUI.OnItemSelected -= OnItemSelected;
                inventoryUI.OnItemDeselected -= OnItemDeselected;
            }
        }

        /// <summary>
        /// Bind to an InventoryUI at runtime (called by InventoryPanelUI).
        /// </summary>
        public void BindToInventoryUI(InventoryUI ui)
        {
            // Unbind old
            if (inventoryUI != null)
            {
                inventoryUI.OnItemSelected -= OnItemSelected;
                inventoryUI.OnItemDeselected -= OnItemDeselected;
            }

            inventoryUI = ui;

            if (inventoryUI != null)
            {
                inventoryUI.OnItemSelected += OnItemSelected;
                inventoryUI.OnItemDeselected += OnItemDeselected;
            }
        }

        private void OnItemSelected(InventoryItem item)
        {
            currentItem = item;
            ShowPanel(item);
        }

        private void OnItemDeselected()
        {
            currentItem = null;
            HidePanel();
        }

        /// <summary>
        /// Build the detail panel UI programmatically.
        /// </summary>
        private void BuildUI()
        {
            if (isBuilt) return;
            isBuilt = true;

            // Panel root
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            // Canvas group for fading
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Background
            Image bgImage = GetComponent<Image>();
            if (bgImage == null)
                bgImage = gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.09f, 0.12f, 0.95f);
            bgImage.raycastTarget = true;

            // Set size
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(panelWidth, 0); // Height auto
            }

            // Add outline border
            Outline outline = GetComponent<Outline>();
            if (outline == null)
                outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.35f, 0.42f, 0.6f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Create content layout
            var layout = GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            // ContentSizeFitter for auto height
            var fitter = GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            // --- Build child elements ---

            // Item Icon
            if (itemIcon == null)
            {
                var iconGO = CreateUIElement("ItemIcon", transform);
                var iconLayout = iconGO.AddComponent<LayoutElement>();
                iconLayout.preferredHeight = 80f;
                iconLayout.preferredWidth = panelWidth - 24f;

                itemIcon = iconGO.AddComponent<Image>();
                itemIcon.preserveAspect = true;
                itemIcon.color = Color.white;
                itemIcon.raycastTarget = false;
            }

            // Rarity bar
            if (rarityBar == null)
            {
                var barGO = CreateUIElement("RarityBar", transform);
                var barLayout = barGO.AddComponent<LayoutElement>();
                barLayout.preferredHeight = 3f;

                rarityBar = barGO.AddComponent<Image>();
                rarityBar.color = Color.white;
                rarityBar.raycastTarget = false;
            }

            // Item Name
            if (itemNameText == null)
            {
                itemNameText = CreateTextElement("ItemName", transform, 16f, TextAlignmentOptions.Center);
                itemNameText.fontStyle = FontStyles.Bold;
            }

            // Item Description
            if (itemDescriptionText == null)
            {
                itemDescriptionText = CreateTextElement("ItemDescription", transform, 11f, TextAlignmentOptions.TopLeft);
                itemDescriptionText.color = new Color(0.7f, 0.7f, 0.7f, 0.9f);
                itemDescriptionText.enableWordWrapping = true;
            }

            // Separator
            CreateSeparator(transform);

            // Stats container
            var statsGO = CreateUIElement("Stats", transform);
            var statsLayout = statsGO.AddComponent<VerticalLayoutGroup>();
            statsLayout.spacing = 4f;
            statsLayout.childForceExpandWidth = true;
            statsLayout.childForceExpandHeight = false;
            statsLayout.childControlWidth = true;
            statsLayout.childControlHeight = true;
            var statsFitter = statsGO.AddComponent<ContentSizeFitter>();
            statsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Weight
            if (itemWeightText == null)
                itemWeightText = CreateTextElement("Weight", statsGO.transform, 11f, TextAlignmentOptions.TopLeft);

            // Value
            if (itemValueText == null)
                itemValueText = CreateTextElement("Value", statsGO.transform, 11f, TextAlignmentOptions.TopLeft);

            // Category
            if (itemCategoryText == null)
                itemCategoryText = CreateTextElement("Category", statsGO.transform, 11f, TextAlignmentOptions.TopLeft);

            // Rarity
            if (itemRarityText == null)
                itemRarityText = CreateTextElement("Rarity", statsGO.transform, 11f, TextAlignmentOptions.TopLeft);

            // Separator
            CreateSeparator(transform);

            // Shape preview label
            CreateTextElement("ShapeLabel", transform, 10f, TextAlignmentOptions.TopLeft).text = "SHAPE";

            // Shape container
            if (shapeContainer == null)
            {
                var shapeGO = CreateUIElement("ShapeContainer", transform);
                shapeContainer = shapeGO.GetComponent<RectTransform>();
                var shapeLayout = shapeGO.AddComponent<LayoutElement>();
                shapeLayout.preferredHeight = 60f;
            }

            // Separator
            CreateSeparator(transform);

            // Action buttons container
            var buttonsGO = CreateUIElement("Buttons", transform);
            var buttonsLayout = buttonsGO.AddComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 8f;
            buttonsLayout.childForceExpandWidth = true;
            buttonsLayout.childForceExpandHeight = false;
            buttonsLayout.childControlWidth = true;
            buttonsLayout.childControlHeight = true;
            var buttonsFitter = buttonsGO.AddComponent<ContentSizeFitter>();
            buttonsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Rotate button
            if (rotateButton == null)
            {
                rotateButton = CreateButton("RotateBtn", buttonsGO.transform, "Rotate [R]",
                    new Color(0.2f, 0.35f, 0.5f, 0.9f));
                rotateButton.onClick.AddListener(OnRotateClicked);
            }

            // Drop button
            if (dropButton == null)
            {
                dropButton = CreateButton("DropBtn", buttonsGO.transform, "Drop [X]",
                    new Color(0.5f, 0.2f, 0.2f, 0.9f));
                dropButton.onClick.AddListener(OnDropClicked);
            }
        }

        #region Show/Hide

        private void ShowPanel(InventoryItem item)
        {
            if (item?.itemData == null)
            {
                HidePanel();
                return;
            }

            if (!isBuilt) BuildUI();

            panelRoot.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // Suppress tooltip while detail panel is showing
            if (ItemTooltipUI.Instance != null)
            {
                ItemTooltipUI.Instance.Suppressed = true;
                ItemTooltipUI.Instance.Hide();
            }

            RefreshContent(item);
        }

        public void HidePanel()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (panelRoot != null)
                panelRoot.SetActive(false);

            // Re-enable tooltip
            if (ItemTooltipUI.Instance != null)
            {
                ItemTooltipUI.Instance.Suppressed = false;
            }
        }

        #endregion

        #region Content

        private void RefreshContent(InventoryItem item)
        {
            ItemData data = item.itemData;

            // Icon
            if (itemIcon != null)
            {
                if (data.icon != null)
                {
                    itemIcon.sprite = data.icon;
                    itemIcon.color = Color.white;
                }
                else
                {
                    itemIcon.sprite = null;
                    Color fallback = ItemData.GetRarityColor(data.rarity);
                    fallback.a = 0.5f;
                    itemIcon.color = fallback;
                }
            }

            // Rarity bar
            if (rarityBar != null)
            {
                rarityBar.color = ItemData.GetRarityColor(data.rarity);
            }

            // Name
            if (itemNameText != null)
            {
                itemNameText.text = data.itemName;
                itemNameText.color = ItemData.GetRarityColor(data.rarity);
            }

            // Description
            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = string.IsNullOrEmpty(data.description) ? "No description." : data.description;
            }

            // Weight
            if (itemWeightText != null)
            {
                itemWeightText.text = $"<color=#AAA>Weight:</color> {data.weight:F1} kg";
            }

            // Value
            if (itemValueText != null)
            {
                itemValueText.text = $"<color=#AAA>Value:</color> {data.goldValue}g";
            }

            // Category
            if (itemCategoryText != null)
            {
                itemCategoryText.text = $"<color=#AAA>Type:</color> {data.category}";
            }

            // Rarity
            if (itemRarityText != null)
            {
                Color rc = ItemData.GetRarityColor(data.rarity);
                string hex = ColorUtility.ToHtmlStringRGB(rc);
                itemRarityText.text = $"<color=#AAA>Rarity:</color> <color=#{hex}>{data.rarity}</color>";
            }

            // Rotate button - only visible if item can rotate
            if (rotateButton != null)
            {
                rotateButton.gameObject.SetActive(data.canRotate);
            }

            // Shape preview
            BuildShapePreview(data, item.isRotated);
        }

        private void BuildShapePreview(ItemData data, bool isRotated)
        {
            if (shapeContainer == null) return;

            // Clear existing
            foreach (Transform child in shapeContainer)
            {
                Destroy(child.gameObject);
            }

            bool[,] shape = isRotated ? data.GetRotatedShape() : data.GetShape();
            int w = isRotated ? data.height : data.width;
            int h = isRotated ? data.width : data.height;

            // Set container size
            float totalW = w * (shapeCellSize + shapeCellSpacing) - shapeCellSpacing;
            float totalH = h * (shapeCellSize + shapeCellSpacing) - shapeCellSpacing;

            var shapeLayoutElem = shapeContainer.GetComponent<LayoutElement>();
            if (shapeLayoutElem != null)
            {
                shapeLayoutElem.preferredHeight = totalH + 4f;
            }

            Color rarityColor = ItemData.GetRarityColor(data.rarity);
            rarityColor.a = 0.8f;
            Color emptyColor = new Color(0.15f, 0.15f, 0.18f, 0.5f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var cellGO = new GameObject($"Shape_{x}_{y}");
                    cellGO.transform.SetParent(shapeContainer, false);

                    var cellRect = cellGO.AddComponent<RectTransform>();
                    cellRect.anchorMin = new Vector2(0, 1);
                    cellRect.anchorMax = new Vector2(0, 1);
                    cellRect.pivot = new Vector2(0, 1);
                    cellRect.anchoredPosition = new Vector2(
                        x * (shapeCellSize + shapeCellSpacing),
                        -y * (shapeCellSize + shapeCellSpacing));
                    cellRect.sizeDelta = new Vector2(shapeCellSize, shapeCellSize);

                    var cellImage = cellGO.AddComponent<Image>();
                    cellImage.color = shape[x, y] ? rarityColor : emptyColor;
                    cellImage.raycastTarget = false;

                    // Add subtle outline to each shape cell
                    var cellOutline = cellGO.AddComponent<Outline>();
                    cellOutline.effectColor = new Color(0.4f, 0.4f, 0.45f, 0.4f);
                    cellOutline.effectDistance = new Vector2(1f, -1f);
                }
            }
        }

        #endregion

        #region Button Callbacks

        private void OnRotateClicked()
        {
            if (inventoryUI == null || currentItem == null) return;

            var itemUI = inventoryUI.GetItemUI(currentItem);
            if (itemUI != null)
            {
                inventoryUI.RotateItem(itemUI);
                // Refresh the detail panel to show updated shape
                RefreshContent(currentItem);
            }
        }

        private void OnDropClicked()
        {
            if (inventoryUI == null || currentItem == null) return;
            inventoryUI.DiscardSelectedItem();
        }

        #endregion

        #region UI Builder Helpers

        private GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.AddComponent<RectTransform>();
            go.transform.SetParent(parent, false);
            return go;
        }

        private TextMeshProUGUI CreateTextElement(string name, Transform parent, float fontSize,
            TextAlignmentOptions alignment)
        {
            var go = CreateUIElement(name, parent);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = fontSize + 6f;

            return text;
        }

        private void CreateSeparator(Transform parent)
        {
            var go = CreateUIElement("Separator", parent);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.3f, 0.32f, 0.38f, 0.5f);
            img.raycastTarget = false;

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 1f;
        }

        private Button CreateButton(string name, Transform parent, string label, Color bgColor)
        {
            var go = CreateUIElement(name, parent);

            var btnImage = go.AddComponent<Image>();
            btnImage.color = bgColor;
            btnImage.raycastTarget = true;

            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(bgColor, Color.black, 0.15f);
            button.colors = colors;

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 30f;
            layout.flexibleWidth = 1f;

            // Button outline
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.5f, 0.5f, 0.55f, 0.4f);
            outline.effectDistance = new Vector2(1f, -1f);

            // Label
            var labelGO = CreateUIElement("Label", go.transform);
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 11f;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            labelText.raycastTarget = false;

            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        #endregion
    }
}
