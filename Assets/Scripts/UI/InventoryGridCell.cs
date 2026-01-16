using UnityEngine;
using UnityEngine.EventSystems;

namespace DungeonDredge.UI
{
    public class InventoryGridCell : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private InventoryUI inventoryUI;
        private Vector2Int gridPosition;
        private bool isHovered;

        public Vector2Int GridPosition => gridPosition;
        public bool IsHovered => isHovered;

        public void Initialize(InventoryUI ui, Vector2Int position)
        {
            inventoryUI = ui;
            gridPosition = position;
        }

        public void OnDrop(PointerEventData eventData)
        {
            // Handle drop - the InventoryUI handles actual placement logic
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
        }
    }
}
