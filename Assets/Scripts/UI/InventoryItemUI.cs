using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using DungeonDredge.Inventory;

namespace DungeonDredge.UI
{
    public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Components")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private RectTransform rectTransform;

        [Header("Border")]
        [SerializeField] private Outline borderOutline;

        [Header("Name Label")]
        [SerializeField] private TextMeshProUGUI nameLabel;

        private InventoryUI inventoryUI;
        private InventoryPanelUI inventoryPanelUI;
        private InventoryItem item;
        private Vector2 dragOffset;
        private bool isDragging = false;
        private bool isSelected = false;

        // Colors
        private static readonly Color normalBorderColor = new Color(0.4f, 0.42f, 0.48f, 0.7f);
        private static readonly Color selectedBorderColor = new Color(1f, 0.85f, 0.3f, 0.95f);

        public InventoryItem Item => item;
        public bool IsDragging => isDragging;

        public void Initialize(InventoryUI ui, InventoryItem inventoryItem)
        {
            inventoryUI = ui;
            item = inventoryItem;
            inventoryPanelUI = GetComponentInParent<InventoryPanelUI>();

            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            // Ensure anchors for proper positioning
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);

            // Setup border outline
            if (borderOutline == null)
            {
                borderOutline = GetComponent<Outline>();
                if (borderOutline == null)
                    borderOutline = gameObject.AddComponent<Outline>();
            }
            borderOutline.effectColor = normalBorderColor;
            borderOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Setup name label if it doesn't exist - create one for larger items
            SetupNameLabel();

            UpdateVisual();
            UpdatePosition();
        }

        private void SetupNameLabel()
        {
            if (item?.itemData == null) return;

            // Only show name label for items tall enough to fit text without overlapping the icon
            bool showLabel = item.Height >= 2;

            if (nameLabel == null && showLabel)
            {
                // Create name label child
                var labelGO = new GameObject("NameLabel");
                labelGO.transform.SetParent(transform, false);

                var labelRect = labelGO.AddComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0, 0);
                labelRect.anchorMax = new Vector2(1, 0);
                labelRect.pivot = new Vector2(0.5f, 0);
                labelRect.anchoredPosition = new Vector2(0, 2f);
                labelRect.sizeDelta = new Vector2(0, 14f);

                nameLabel = labelGO.AddComponent<TextMeshProUGUI>();
                nameLabel.fontSize = 8f;
                nameLabel.alignment = TextAlignmentOptions.Bottom;
                nameLabel.overflowMode = TextOverflowModes.Ellipsis;
                nameLabel.enableWordWrapping = false;
                nameLabel.raycastTarget = false;
            }

            if (nameLabel != null)
            {
                nameLabel.gameObject.SetActive(showLabel);
                if (showLabel)
                {
                    nameLabel.text = item.itemData.itemName;
                    nameLabel.color = new Color(0.9f, 0.9f, 0.9f, 0.85f);
                }
            }
        }

        public void UpdateVisual()
        {
            if (item?.itemData == null) return;

            // Set size based on item dimensions
            float width = item.Width * inventoryUI.CellSize + (item.Width - 1) * inventoryUI.CellSpacing;
            float height = item.Height * inventoryUI.CellSize + (item.Height - 1) * inventoryUI.CellSpacing;
            rectTransform.sizeDelta = new Vector2(width, height);

            // Set icon
            if (iconImage != null)
            {
                if (item.itemData.icon != null)
                {
                    iconImage.sprite = item.itemData.icon;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                    iconImage.enabled = true;
                }
                else
                {
                    // Fallback: show a colored placeholder
                    iconImage.sprite = null;
                    Color fallbackColor = ItemData.GetRarityColor(item.itemData.rarity);
                    fallbackColor.a = 0.6f;
                    iconImage.color = fallbackColor;
                    iconImage.enabled = true;
                }

                // Rotate icon if item is rotated
                iconImage.rectTransform.localRotation = item.isRotated ?
                    Quaternion.Euler(0, 0, -90) : Quaternion.identity;

                // Ensure icon fills the item rect with some padding
                float iconPadding = 4f;
                iconImage.rectTransform.anchorMin = Vector2.zero;
                iconImage.rectTransform.anchorMax = Vector2.one;
                iconImage.rectTransform.offsetMin = new Vector2(iconPadding, iconPadding);
                iconImage.rectTransform.offsetMax = new Vector2(-iconPadding, -iconPadding);
            }

            // Set rarity-tinted background
            if (backgroundImage != null)
            {
                Color rarityColor = ItemData.GetRarityColor(item.itemData.rarity);
                // Create a gradient-like effect: darker at top, rarity-tinted at bottom
                Color bgColor = Color.Lerp(new Color(0.08f, 0.09f, 0.12f, 0.85f), rarityColor, 0.2f);
                bgColor.a = 0.75f;
                backgroundImage.color = bgColor;
            }

            // Update border color based on rarity
            if (borderOutline != null && !isSelected)
            {
                Color rarityColor = ItemData.GetRarityColor(item.itemData.rarity);
                borderOutline.effectColor = Color.Lerp(normalBorderColor, rarityColor, 0.35f);
            }

            // Update name label
            if (nameLabel != null)
            {
                bool showLabel = item.Height >= 2;
                nameLabel.gameObject.SetActive(showLabel);
                if (showLabel)
                {
                    nameLabel.text = item.itemData.itemName;
                }
            }
        }

        public void UpdatePosition()
        {
            if (inventoryUI == null || item == null) return;

            Vector2 uiPos = inventoryUI.GridToUIPosition(item.gridPosition);
            rectTransform.anchoredPosition = uiPos;
        }

        #region Selection

        /// <summary>
        /// Set the selected visual state.
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (borderOutline != null)
            {
                if (selected)
                {
                    borderOutline.effectColor = selectedBorderColor;
                    borderOutline.effectDistance = new Vector2(2f, -2f);
                }
                else
                {
                    Color rarityColor = item?.itemData != null ?
                        ItemData.GetRarityColor(item.itemData.rarity) : Color.white;
                    borderOutline.effectColor = Color.Lerp(normalBorderColor, rarityColor, 0.35f);
                    borderOutline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }
        }

        #endregion

        #region Drag Ghost

        /// <summary>
        /// Set drag ghost visual (semi-transparent during drag).
        /// </summary>
        public void SetDragGhost(bool ghost)
        {
            if (backgroundImage != null)
            {
                Color bgColor = backgroundImage.color;
                bgColor.a = ghost ? 0.3f : 0.75f;
                backgroundImage.color = bgColor;
            }

            if (iconImage != null)
            {
                Color iconColor = iconImage.color;
                iconColor.a = ghost ? 0.5f : 1f;
                iconImage.color = iconColor;
            }

            if (nameLabel != null)
            {
                Color labelColor = nameLabel.color;
                labelColor.a = ghost ? 0.3f : 0.85f;
                nameLabel.color = labelColor;
            }
        }

        #endregion

        #region Drag Handlers

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            isDragging = true;

            // Hide tooltip while dragging
            ItemTooltipUI.Instance?.Hide();

            // Calculate offset from item origin
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            dragOffset = localPoint;

            inventoryUI.StartDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            // Move item to follow cursor
            RectTransform parentRT = (RectTransform)transform.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRT, eventData.position,
                eventData.pressEventCamera, out Vector2 localPoint);

            // localPoint is in parent-local space (origin at parent's pivot = center).
            // anchoredPosition for anchor (0,1) is relative to the parent's top-left corner.
            // Convert: anchoredPos = localPoint - topLeftCornerInLocalSpace
            Rect parentRect = parentRT.rect;
            Vector2 anchoredPos = new Vector2(
                localPoint.x - parentRect.x,                        // left edge = rect.x (negative half-width)
                localPoint.y - (parentRect.y + parentRect.height)   // top edge = rect.y + rect.height
            );

            rectTransform.anchoredPosition = anchoredPos - dragOffset;

            inventoryUI.OnDrag(this, eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            // Delegate all end-drag logic to InventoryUI (it handles both in-grid and outside-grid)
            inventoryUI.EndDrag(this, eventData.position);
        }

        #endregion

        #region Tooltip Handlers

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isDragging) return;
            if (item?.itemData == null) return;

            ItemTooltipUI.Instance?.Show(item.itemData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ItemTooltipUI.Instance?.Hide();
        }

        #endregion

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // Left-click to select
                inventoryUI.SelectItem(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                // Right-click to rotate
                inventoryUI.RotateItem(this);
            }
        }

        private void Update()
        {
            // R key to rotate while hovering (when not dragging - drag rotation is handled by InventoryUI)
            if (!isDragging && !inventoryUI.IsDragging &&
                Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                if (Mouse.current != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(
                        rectTransform, Mouse.current.position.ReadValue()))
                {
                    inventoryUI.RotateItem(this);
                }
            }
        }
    }
}
