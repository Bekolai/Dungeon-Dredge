using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DungeonDredge.Inventory;

namespace DungeonDredge.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private RectTransform itemContainer;

        [Header("Prefabs")]
        [SerializeField] private GameObject gridCellPrefab;
        [SerializeField] private GameObject inventoryItemPrefab;

        [Header("Settings")]
        [SerializeField] private float cellSize = 50f;
        [SerializeField] private float cellSpacing = 2f;

        [Header("Colors")]
        [SerializeField] private Color emptyCellColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        [SerializeField] private Color occupiedCellColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color validPlacementColor = new Color(0.2f, 0.6f, 0.2f, 0.8f);
        [SerializeField] private Color invalidPlacementColor = new Color(0.6f, 0.2f, 0.2f, 0.8f);

        // Grid cells
        private Image[,] gridCells;
        private Dictionary<InventoryItem, InventoryItemUI> itemUIs = new Dictionary<InventoryItem, InventoryItemUI>();

        // Drag state
        private InventoryItemUI draggedItem;
        private Vector2Int dragStartPosition;

        // Properties
        public float CellSize => cellSize;
        public float CellSpacing => cellSpacing;

        private void Start()
        {
            if (playerInventory == null)
            {
                playerInventory = FindObjectOfType<PlayerInventory>();
            }

            if (playerInventory != null)
            {
                playerInventory.OnInventoryOpened += Show;
                playerInventory.OnInventoryClosed += Hide;

                if (playerInventory.Grid != null)
                {
                    playerInventory.Grid.OnItemAdded += OnItemAdded;
                    playerInventory.Grid.OnItemRemoved += OnItemRemoved;
                }
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (playerInventory != null)
            {
                playerInventory.OnInventoryOpened -= Show;
                playerInventory.OnInventoryClosed -= Hide;

                if (playerInventory.Grid != null)
                {
                    playerInventory.Grid.OnItemAdded -= OnItemAdded;
                    playerInventory.Grid.OnItemRemoved -= OnItemRemoved;
                }
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RebuildGrid();
            RefreshItems();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void RebuildGrid()
        {
            if (playerInventory?.Grid == null) return;

            // Clear existing cells
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }

            int width = playerInventory.Grid.Width;
            int height = playerInventory.Grid.Height;
            gridCells = new Image[width, height];

            // Set container size
            float totalWidth = width * (cellSize + cellSpacing) - cellSpacing;
            float totalHeight = height * (cellSize + cellSpacing) - cellSpacing;
            gridContainer.sizeDelta = new Vector2(totalWidth, totalHeight);

            // Create cells
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GameObject cell = Instantiate(gridCellPrefab, gridContainer);
                    RectTransform rect = cell.GetComponent<RectTransform>();
                    
                    // Position
                    float posX = x * (cellSize + cellSpacing);
                    float posY = -y * (cellSize + cellSpacing); // Negative because UI Y is inverted
                    rect.anchoredPosition = new Vector2(posX, posY);
                    rect.sizeDelta = new Vector2(cellSize, cellSize);

                    // Store reference
                    Image image = cell.GetComponent<Image>();
                    gridCells[x, y] = image;
                    image.color = emptyCellColor;

                    // Add cell component for interaction
                    var gridCell = cell.AddComponent<InventoryGridCell>();
                    gridCell.Initialize(this, new Vector2Int(x, y));
                }
            }
        }

        private void RefreshItems()
        {
            // Clear existing item UIs
            foreach (var itemUI in itemUIs.Values)
            {
                if (itemUI != null)
                    Destroy(itemUI.gameObject);
            }
            itemUIs.Clear();

            // Create UIs for current items
            if (playerInventory?.Grid == null) return;

            foreach (var item in playerInventory.Grid.Items)
            {
                CreateItemUI(item);
            }

            UpdateGridColors();
        }

        private void CreateItemUI(InventoryItem item)
        {
            if (inventoryItemPrefab == null) return;

            GameObject itemObj = Instantiate(inventoryItemPrefab, itemContainer);
            InventoryItemUI itemUI = itemObj.GetComponent<InventoryItemUI>();
            
            if (itemUI != null)
            {
                itemUI.Initialize(this, item);
                itemUIs[item] = itemUI;
            }
        }

        private void OnItemAdded(InventoryItem item)
        {
            if (!itemUIs.ContainsKey(item))
            {
                CreateItemUI(item);
            }
            UpdateGridColors();
        }

        private void OnItemRemoved(InventoryItem item)
        {
            if (itemUIs.TryGetValue(item, out InventoryItemUI itemUI))
            {
                Destroy(itemUI.gameObject);
                itemUIs.Remove(item);
            }
            UpdateGridColors();
        }

        public void UpdateGridColors()
        {
            if (playerInventory?.Grid == null || gridCells == null) return;

            for (int x = 0; x < playerInventory.Grid.Width; x++)
            {
                for (int y = 0; y < playerInventory.Grid.Height; y++)
                {
                    bool occupied = !playerInventory.Grid.IsCellEmpty(new Vector2Int(x, y));
                    gridCells[x, y].color = occupied ? occupiedCellColor : emptyCellColor;
                }
            }
        }

        #region Drag and Drop

        public void StartDrag(InventoryItemUI itemUI)
        {
            draggedItem = itemUI;
            dragStartPosition = itemUI.Item.gridPosition;
            itemUI.transform.SetAsLastSibling(); // Bring to front
        }

        public void OnDrag(InventoryItemUI itemUI, Vector2 screenPosition)
        {
            if (draggedItem != itemUI) return;

            // Convert screen position to grid position
            Vector2Int gridPos = ScreenToGridPosition(screenPosition);

            // Show preview
            ShowPlacementPreview(itemUI.Item, gridPos);
        }

        public void EndDrag(InventoryItemUI itemUI, Vector2 screenPosition)
        {
            if (draggedItem != itemUI) return;

            Vector2Int gridPos = ScreenToGridPosition(screenPosition);

            // Try to place at new position
            if (playerInventory.Grid.MoveItem(itemUI.Item, gridPos))
            {
                itemUI.UpdatePosition();
            }
            else
            {
                // Return to original position
                itemUI.UpdatePosition();
            }

            ClearPlacementPreview();
            draggedItem = null;
        }

        public void RotateItem(InventoryItemUI itemUI)
        {
            if (!itemUI.Item.itemData.canRotate) return;

            // Temporarily remove from grid
            playerInventory.Grid.RemoveItem(itemUI.Item);

            // Rotate
            itemUI.Item.Rotate();

            // Try to place back
            if (!playerInventory.Grid.TryAddItem(itemUI.Item, itemUI.Item.gridPosition))
            {
                // Can't place rotated, rotate back
                itemUI.Item.Rotate();
                playerInventory.Grid.TryAddItem(itemUI.Item, itemUI.Item.gridPosition);
            }

            itemUI.UpdateVisual();
            UpdateGridColors();
        }

        private Vector2Int ScreenToGridPosition(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridContainer, screenPosition, null, out Vector2 localPoint);

            int x = Mathf.FloorToInt(localPoint.x / (cellSize + cellSpacing));
            int y = Mathf.FloorToInt(-localPoint.y / (cellSize + cellSpacing));

            return new Vector2Int(x, y);
        }

        private void ShowPlacementPreview(InventoryItem item, Vector2Int position)
        {
            UpdateGridColors();

            bool canPlace = playerInventory.Grid.CanPlaceItem(item, position);
            Color previewColor = canPlace ? validPlacementColor : invalidPlacementColor;

            bool[,] shape = item.GetCurrentShape();
            for (int x = 0; x < item.Width; x++)
            {
                for (int y = 0; y < item.Height; y++)
                {
                    if (!shape[x, y]) continue;

                    int gridX = position.x + x;
                    int gridY = position.y + y;

                    if (gridX >= 0 && gridX < playerInventory.Grid.Width &&
                        gridY >= 0 && gridY < playerInventory.Grid.Height)
                    {
                        gridCells[gridX, gridY].color = previewColor;
                    }
                }
            }
        }

        private void ClearPlacementPreview()
        {
            UpdateGridColors();
        }

        #endregion

        public Vector2 GridToUIPosition(Vector2Int gridPosition)
        {
            float x = gridPosition.x * (cellSize + cellSpacing);
            float y = -gridPosition.y * (cellSize + cellSpacing);
            return new Vector2(x, y);
        }
    }
}
