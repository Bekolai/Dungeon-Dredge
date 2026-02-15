using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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

        [Header("Grid Colors")]
        [SerializeField] private Color emptyCellColor = new Color(0.12f, 0.14f, 0.18f, 0.95f);
        [SerializeField] private Color occupiedCellColor = new Color(0.18f, 0.20f, 0.26f, 0.95f);
        [SerializeField] private Color validPlacementColor = new Color(0.15f, 0.45f, 0.15f, 0.9f);
        [SerializeField] private Color invalidPlacementColor = new Color(0.55f, 0.12f, 0.12f, 0.9f);
        [SerializeField] private Color swapPlacementColor = new Color(0.55f, 0.45f, 0.1f, 0.9f);
        [SerializeField] private Color cellBorderColor = new Color(0.3f, 0.35f, 0.42f, 0.6f);
        [SerializeField] private Color gridBackgroundColor = new Color(0.06f, 0.07f, 0.10f, 0.95f);

        [Header("Selection Colors")]
        [SerializeField] private Color selectedBorderColor = new Color(1f, 0.85f, 0.3f, 0.9f);

        // Grid cells
        private InventoryGridCell[,] gridCellComponents;
        private Dictionary<InventoryItem, InventoryItemUI> itemUIs = new Dictionary<InventoryItem, InventoryItemUI>();

        // Drag state
        private InventoryItemUI draggedItem;
        private Vector2Int dragStartPosition;
        private bool dragStartRotated;
        private bool isDragging = false;
        private Vector2Int lastDragGridPos;

        // Selection state
        private InventoryItemUI selectedItemUI;
        private InventoryItem selectedItem;

        // Properties
        public float CellSize => cellSize;
        public float CellSpacing => cellSpacing;
        public bool IsDragging => isDragging;
        public InventoryItem SelectedItem => selectedItem;

        // Events for detail panel
        public System.Action<InventoryItem> OnItemSelected;
        public System.Action OnItemDeselected;

        private void Start()
        {
            EnsureContainers();
            Hide();
        }

        /// <summary>
        /// Ensure grid and item containers exist. Creates them if not assigned.
        /// </summary>
        private void EnsureContainers()
        {
            if (gridContainer == null)
            {
                var gridGO = new GameObject("GridContainer");
                gridContainer = gridGO.AddComponent<RectTransform>();
                gridContainer.SetParent(transform, false);
                gridContainer.anchorMin = new Vector2(0.5f, 0.5f);
                gridContainer.anchorMax = new Vector2(0.5f, 0.5f);
                gridContainer.pivot = new Vector2(0.5f, 0.5f);
            }

            // Ensure grid container has a background image
            Image gridBg = gridContainer.GetComponent<Image>();
            if (gridBg == null)
            {
                gridBg = gridContainer.gameObject.AddComponent<Image>();
            }
            gridBg.color = gridBackgroundColor;
            gridBg.raycastTarget = true;

            if (itemContainer == null)
            {
                var itemGO = new GameObject("ItemContainer");
                itemContainer = itemGO.AddComponent<RectTransform>();
                itemContainer.SetParent(transform, false);
                itemContainer.anchorMin = new Vector2(0.5f, 0.5f);
                itemContainer.anchorMax = new Vector2(0.5f, 0.5f);
                itemContainer.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        /// <summary>
        /// Called by InventoryPanelUI when the player reference changes.
        /// </summary>
        public void BindToPlayer(PlayerInventory inventory)
        {
            UnbindFromPlayer();
            playerInventory = inventory;

            if (playerInventory?.Grid != null)
            {
                playerInventory.Grid.OnItemAdded += OnItemAdded;
                playerInventory.Grid.OnItemRemoved += OnItemRemoved;
            }
        }

        private void OnDestroy()
        {
            UnbindFromPlayer();
        }

        private void UnbindFromPlayer()
        {
            if (playerInventory?.Grid != null)
            {
                playerInventory.Grid.OnItemAdded -= OnItemAdded;
                playerInventory.Grid.OnItemRemoved -= OnItemRemoved;
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
            ClearSelection();
            CancelDrag();
            gameObject.SetActive(false);
        }

        #region Grid Building

        private void RebuildGrid()
        {
            if (playerInventory?.Grid == null) return;
            if (gridContainer == null) return;

            // Clear existing cells
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }

            int width = playerInventory.Grid.Width;
            int height = playerInventory.Grid.Height;
            if (width <= 0 || height <= 0) return;

            gridCellComponents = new InventoryGridCell[width, height];

            // Set container size with padding for the background
            float padding = 6f;
            float totalWidth = width * (cellSize + cellSpacing) - cellSpacing;
            float totalHeight = height * (cellSize + cellSpacing) - cellSpacing;
            gridContainer.sizeDelta = new Vector2(totalWidth + padding * 2f, totalHeight + padding * 2f);

            // Create cells
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GameObject cell = CreateGridCell(x, y);
                    cell.transform.SetParent(gridContainer, false);

                    RectTransform rect = cell.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 1);

                    // Position (offset by padding)
                    float posX = padding + x * (cellSize + cellSpacing);
                    float posY = -(padding + y * (cellSize + cellSpacing));
                    rect.anchoredPosition = new Vector2(posX, posY);
                    rect.sizeDelta = new Vector2(cellSize, cellSize);

                    // Initialize cell component
                    var gridCell = cell.GetComponent<InventoryGridCell>();
                    if (gridCell == null)
                        gridCell = cell.AddComponent<InventoryGridCell>();
                    gridCell.Initialize(this, new Vector2Int(x, y));
                    gridCell.SetColor(emptyCellColor);
                    gridCell.SetBorderColor(cellBorderColor);

                    gridCellComponents[x, y] = gridCell;
                }
            }

            // Resize the item container to match grid container (same size, including padding)
            // Items use the same coordinate space as grid cells
            if (itemContainer != null)
            {
                itemContainer.sizeDelta = new Vector2(totalWidth + padding * 2f, totalHeight + padding * 2f);
                itemContainer.anchoredPosition = gridContainer.anchoredPosition;
            }
        }

        /// <summary>
        /// Creates a single grid cell, using the prefab if available or building one programmatically.
        /// </summary>
        private GameObject CreateGridCell(int x, int y)
        {
            if (gridCellPrefab != null)
            {
                GameObject cell = Instantiate(gridCellPrefab);
                cell.SetActive(true);
                cell.name = $"Cell_{x}_{y}";

                // Ensure it has an Image
                if (cell.GetComponent<Image>() == null)
                    cell.AddComponent<Image>();

                return cell;
            }

            // Fallback: create cell programmatically
            GameObject fallbackCell = new GameObject($"Cell_{x}_{y}");
            RectTransform rt = fallbackCell.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(cellSize, cellSize);

            Image img = fallbackCell.AddComponent<Image>();
            img.color = emptyCellColor;
            img.raycastTarget = true;

            return fallbackCell;
        }

        #endregion

        #region Item Management

        private void RefreshItems()
        {
            // Clear existing item UIs
            foreach (var itemUI in itemUIs.Values)
            {
                if (itemUI != null)
                    Destroy(itemUI.gameObject);
            }
            itemUIs.Clear();

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
            itemObj.SetActive(true); // Prefab is saved as inactive; activate the clone
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
            // If the removed item was selected, clear selection
            if (selectedItem == item)
            {
                ClearSelection();
            }

            if (itemUIs.TryGetValue(item, out InventoryItemUI itemUI))
            {
                Destroy(itemUI.gameObject);
                itemUIs.Remove(item);
            }
            UpdateGridColors();
        }

        #endregion

        #region Grid Coloring

        public void UpdateGridColors()
        {
            if (playerInventory?.Grid == null || gridCellComponents == null) return;

            for (int x = 0; x < playerInventory.Grid.Width; x++)
            {
                for (int y = 0; y < playerInventory.Grid.Height; y++)
                {
                    var cell = gridCellComponents[x, y];
                    if (cell == null) continue;

                    InventoryItem itemAtCell = playerInventory.Grid.GetItemAt(new Vector2Int(x, y));

                    if (itemAtCell != null)
                    {
                        // Occupied cell - subtle rarity tint over occupied color
                        Color rarityColor = ItemData.GetRarityColor(itemAtCell.itemData.rarity);
                        Color tinted = Color.Lerp(occupiedCellColor, rarityColor, 0.15f);
                        tinted.a = occupiedCellColor.a;
                        cell.SetColor(tinted);
                        cell.SetBorderColor(Color.Lerp(cellBorderColor, rarityColor, 0.2f));
                    }
                    else
                    {
                        // Empty cell
                        cell.SetColor(emptyCellColor);
                        cell.SetBorderColor(cellBorderColor);
                    }
                }
            }
        }

        #endregion

        #region Selection

        /// <summary>
        /// Select an item (called by InventoryItemUI on left-click).
        /// </summary>
        public void SelectItem(InventoryItemUI itemUI)
        {
            if (itemUI == null || itemUI.Item == null) return;

            // Deselect previous
            if (selectedItemUI != null && selectedItemUI != itemUI)
            {
                selectedItemUI.SetSelected(false);
            }

            // Toggle selection if clicking the same item
            if (selectedItemUI == itemUI)
            {
                ClearSelection();
                return;
            }

            selectedItemUI = itemUI;
            selectedItem = itemUI.Item;
            selectedItemUI.SetSelected(true);

            OnItemSelected?.Invoke(selectedItem);
        }

        /// <summary>
        /// Clear the current selection.
        /// </summary>
        public void ClearSelection()
        {
            if (selectedItemUI != null)
            {
                selectedItemUI.SetSelected(false);
            }
            selectedItemUI = null;
            selectedItem = null;
            OnItemDeselected?.Invoke();
        }

        /// <summary>
        /// Deselect when clicking on empty grid space.
        /// Called by InventoryGridCell or background click.
        /// </summary>
        public void OnEmptyGridClick()
        {
            if (!isDragging)
            {
                ClearSelection();
            }
        }

        #endregion

        #region Drag and Drop

        public void StartDrag(InventoryItemUI itemUI)
        {
            isDragging = true;
            draggedItem = itemUI;
            dragStartPosition = itemUI.Item.gridPosition;
            dragStartRotated = itemUI.Item.isRotated;
            lastDragGridPos = dragStartPosition;

            // Suspend item from grid (clear cells but keep in items list, no events fired)
            playerInventory.Grid.SuspendItemFromGrid(itemUI.Item);

            // Ghost effect on the dragged item
            itemUI.SetDragGhost(true);

            // Bring to front
            itemUI.transform.SetAsLastSibling();

            // Clear selection while dragging
            if (selectedItemUI != null && selectedItemUI != itemUI)
            {
                ClearSelection();
            }
        }

        public void OnDrag(InventoryItemUI itemUI, Vector2 screenPosition)
        {
            if (draggedItem != itemUI) return;

            // Convert screen position to grid position
            Vector2Int gridPos = ScreenToGridPosition(screenPosition);
            lastDragGridPos = gridPos;

            // Show preview
            ShowPlacementPreview(itemUI.Item, gridPos);
        }

        public void EndDrag(InventoryItemUI itemUI, Vector2 screenPosition)
        {
            if (draggedItem != itemUI) return;

            // Check if dropped outside the grid boundary -> drop to world
            var panelUI = GetComponentInParent<InventoryPanelUI>();
            if (panelUI != null && panelUI.IsOutsideGrid(screenPosition))
            {
                // Item is suspended (cells cleared, still in items list).
                // DropItemToWorld -> PlayerInventory.DropItem -> RemoveItem will handle
                // removing from items list, firing events, and spawning the world object.
                panelUI.DropItemToWorld(itemUI.Item);

                // Cleanup drag state
                itemUI.SetDragGhost(false);
                ClearPlacementPreview();
                isDragging = false;
                draggedItem = null;
                return;
            }

            Vector2Int gridPos = ScreenToGridPosition(screenPosition);

            // Try to place at new position
            bool placed = playerInventory.Grid.TryAddItem(itemUI.Item, gridPos);

            if (!placed)
            {
                // Try Tarkov-style swap: if exactly one item is blocking, swap positions
                InventoryItem swappedOut;
                if (playerInventory.Grid.TrySwapItems(itemUI.Item, gridPos, dragStartPosition, out swappedOut))
                {
                    placed = true;

                    // Update the swapped item's UI
                    if (swappedOut != null && itemUIs.TryGetValue(swappedOut, out InventoryItemUI swappedUI))
                    {
                        swappedUI.UpdateVisual();
                        swappedUI.UpdatePosition();
                    }
                }
            }

            if (!placed)
            {
                // Try to place back at original position (may need to un-rotate if we rotated during drag)
                bool wasRotated = itemUI.Item.isRotated;
                placed = playerInventory.Grid.TryAddItem(itemUI.Item, dragStartPosition);

                if (!placed && wasRotated != dragStartRotated)
                {
                    // Rotation changed during drag and original spot doesn't work; try reverting rotation
                    itemUI.Item.isRotated = dragStartRotated;
                    placed = playerInventory.Grid.TryAddItem(itemUI.Item, dragStartPosition);
                }

                if (!placed)
                {
                    // Last resort: auto-place anywhere
                    itemUI.Item.isRotated = dragStartRotated;
                    placed = playerInventory.Grid.TryAddItemAuto(itemUI.Item);
                }
            }

            // Restore visual
            itemUI.SetDragGhost(false);
            itemUI.UpdateVisual();
            itemUI.UpdatePosition();

            ClearPlacementPreview();
            isDragging = false;
            draggedItem = null;
        }

        /// <summary>
        /// Cancel the current drag (e.g. when inventory is closed mid-drag).
        /// </summary>
        private void CancelDrag()
        {
            if (!isDragging || draggedItem == null) return;

            var item = draggedItem.Item;
            draggedItem.SetDragGhost(false);

            // Restore item cells to grid (item is still in items list, just suspended)
            item.isRotated = dragStartRotated;
            item.gridPosition = dragStartPosition;

            // Use TryAddItem which calls PlaceItem - since item is already in items list,
            // PlaceItem won't fire OnItemAdded again
            if (!playerInventory.Grid.TryAddItem(item, dragStartPosition))
            {
                playerInventory.Grid.TryAddItemAuto(item);
            }

            draggedItem.UpdateVisual();
            draggedItem.UpdatePosition();

            ClearPlacementPreview();
            isDragging = false;
            draggedItem = null;
        }

        public void RotateItem(InventoryItemUI itemUI)
        {
            if (!itemUI.Item.itemData.canRotate) return;

            if (isDragging && draggedItem == itemUI)
            {
                // Rotate during drag - item is already removed from grid
                itemUI.Item.Rotate();
                itemUI.UpdateVisual();

                // Refresh preview at current position
                ShowPlacementPreview(itemUI.Item, lastDragGridPos);
            }
            else
            {
                // Rotate in-place (not dragging) - suspend cells only, don't fire events
                playerInventory.Grid.SuspendItemFromGrid(itemUI.Item);

                itemUI.Item.Rotate();

                if (!playerInventory.Grid.TryAddItem(itemUI.Item, itemUI.Item.gridPosition))
                {
                    // Can't place rotated, rotate back
                    itemUI.Item.Rotate();
                    playerInventory.Grid.TryAddItem(itemUI.Item, itemUI.Item.gridPosition);
                }

                itemUI.UpdateVisual();
                itemUI.UpdatePosition();
                UpdateGridColors();
            }
        }

        private void Update()
        {
            // R key to rotate during drag
            if (isDragging && draggedItem != null)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    RotateItem(draggedItem);
                }
            }

            // Delete/X key to discard selected item
            if (!isDragging && selectedItem != null && Keyboard.current != null)
            {
                if (Keyboard.current.deleteKey.wasPressedThisFrame ||
                    Keyboard.current.xKey.wasPressedThisFrame)
                {
                    DiscardSelectedItem();
                }
            }
        }

        /// <summary>
        /// Discard the currently selected item to the world.
        /// </summary>
        public void DiscardSelectedItem()
        {
            if (selectedItem == null) return;

            var itemToDiscard = selectedItem;
            ClearSelection();

            var panelUI = GetComponentInParent<InventoryPanelUI>();
            if (panelUI != null)
            {
                panelUI.DropItemToWorld(itemToDiscard);
            }
        }

        private Vector2Int ScreenToGridPosition(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                gridContainer, screenPosition, null, out Vector2 localPoint);

            // gridContainer pivot is (0.5, 0.5) so local (0,0) is the center.
            // Convert to top-left origin, then subtract padding to get cell space.
            float padding = 6f;
            Rect rect = gridContainer.rect;
            float adjustedX = localPoint.x - rect.x - padding;   // rect.x is negative half-width
            float adjustedY = (rect.y + rect.height) - localPoint.y - padding; // top edge minus y

            int x = Mathf.FloorToInt(adjustedX / (cellSize + cellSpacing));
            int y = Mathf.FloorToInt(adjustedY / (cellSize + cellSpacing));

            return new Vector2Int(x, y);
        }

        private void ShowPlacementPreview(InventoryItem item, Vector2Int position)
        {
            // Reset all cells to base colors first
            UpdateGridColors();

            bool canPlace = playerInventory.Grid.CanPlaceItem(item, position);
            Color previewColor;

            if (canPlace)
            {
                previewColor = validPlacementColor;
            }
            else if (isDragging)
            {
                // Check if a swap would be possible (amber/yellow indicator)
                InventoryItem swapTarget;
                bool canSwap = playerInventory.Grid.CanSwapItems(item, position, dragStartPosition, out swapTarget);
                previewColor = canSwap ? swapPlacementColor : invalidPlacementColor;
            }
            else
            {
                previewColor = invalidPlacementColor;
            }

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
                        gridCellComponents[gridX, gridY].SetColor(previewColor);
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
            float padding = 6f;
            float x = padding + gridPosition.x * (cellSize + cellSpacing);
            float y = -(padding + gridPosition.y * (cellSize + cellSpacing));
            return new Vector2(x, y);
        }

        /// <summary>
        /// Get the InventoryItemUI for a given InventoryItem.
        /// </summary>
        public InventoryItemUI GetItemUI(InventoryItem item)
        {
            if (item != null && itemUIs.TryGetValue(item, out InventoryItemUI ui))
                return ui;
            return null;
        }
    }
}
