using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DungeonDredge.UI
{
    public class InventoryGridCell : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler,
        IPointerClickHandler
    {
        private InventoryUI inventoryUI;
        private Vector2Int gridPosition;
        private bool isHovered;

        // Visual references
        private Image cellImage;
        private Outline cellOutline;
        private Color baseColor;

        public Vector2Int GridPosition => gridPosition;
        public bool IsHovered => isHovered;
        public Image CellImage => cellImage;
        public Outline CellOutline => cellOutline;

        [Header("Hover Settings")]
        private static readonly Color hoverTint = new Color(0.3f, 0.35f, 0.45f, 0.95f);

        public void Initialize(InventoryUI ui, Vector2Int position)
        {
            inventoryUI = ui;
            gridPosition = position;

            cellImage = GetComponent<Image>();
            cellOutline = GetComponent<Outline>();

            // Add outline if missing
            if (cellOutline == null)
            {
                cellOutline = gameObject.AddComponent<Outline>();
                cellOutline.effectColor = new Color(0.3f, 0.35f, 0.42f, 0.6f);
                cellOutline.effectDistance = new Vector2(1f, -1f);
            }
        }

        /// <summary>
        /// Set the base color of this cell (used by InventoryUI for empty/occupied/preview).
        /// </summary>
        public void SetColor(Color color)
        {
            baseColor = color;
            if (cellImage != null)
                cellImage.color = isHovered ? Color.Lerp(color, hoverTint, 0.4f) : color;
        }

        /// <summary>
        /// Set the outline/border color of this cell.
        /// </summary>
        public void SetBorderColor(Color color)
        {
            if (cellOutline != null)
                cellOutline.effectColor = color;
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Handle drop - the InventoryUI handles actual placement logic
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            if (cellImage != null)
                cellImage.color = Color.Lerp(baseColor, hoverTint, 0.4f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (cellImage != null)
                cellImage.color = baseColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                // Clicking an empty cell deselects the current item
                inventoryUI?.OnEmptyGridClick();
            }
        }
    }
}
