using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DungeonDredge.Inventory;

namespace DungeonDredge.UI
{
    public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("UI Components")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private RectTransform rectTransform;

        private InventoryUI inventoryUI;
        private InventoryItem item;
        private Vector2 dragOffset;

        public InventoryItem Item => item;

        public void Initialize(InventoryUI ui, InventoryItem inventoryItem)
        {
            inventoryUI = ui;
            item = inventoryItem;

            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();

            UpdateVisual();
            UpdatePosition();
        }

        public void UpdateVisual()
        {
            if (item?.itemData == null) return;

            // Set icon
            if (iconImage != null)
            {
                iconImage.sprite = item.itemData.icon;
            }

            // Set size based on item dimensions
            float width = item.Width * inventoryUI.CellSize + (item.Width - 1) * inventoryUI.CellSpacing;
            float height = item.Height * inventoryUI.CellSize + (item.Height - 1) * inventoryUI.CellSpacing;
            rectTransform.sizeDelta = new Vector2(width, height);

            // Set rarity color
            if (backgroundImage != null)
            {
                Color rarityColor = ItemData.GetRarityColor(item.itemData.rarity);
                rarityColor.a = 0.5f;
                backgroundImage.color = rarityColor;
            }

            // Rotate icon if item is rotated
            if (iconImage != null)
            {
                iconImage.rectTransform.localRotation = item.isRotated ? 
                    Quaternion.Euler(0, 0, -90) : Quaternion.identity;
            }
        }

        public void UpdatePosition()
        {
            if (inventoryUI == null || item == null) return;

            Vector2 uiPos = inventoryUI.GridToUIPosition(item.gridPosition);
            rectTransform.anchoredPosition = uiPos;
        }

        #region Drag Handlers

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Calculate offset from item origin
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
            dragOffset = localPoint;

            inventoryUI.StartDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Move item to follow cursor
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform.parent, eventData.position, 
                eventData.pressEventCamera, out Vector2 localPoint);
            
            rectTransform.anchoredPosition = localPoint - dragOffset;

            inventoryUI.OnDrag(this, eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            inventoryUI.EndDrag(this, eventData.position);
        }

        #endregion

        public void OnPointerClick(PointerEventData eventData)
        {
            // Right click to rotate
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                inventoryUI.RotateItem(this);
            }
        }

        private void Update()
        {
            // R key to rotate while hovering
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                // Check if mouse is over this item
                if (RectTransformUtility.RectangleContainsScreenPoint(
                    rectTransform, Mouse.current.position.ReadValue()))
                {
                    inventoryUI.RotateItem(this);
                }
            }
        }
    }
}
