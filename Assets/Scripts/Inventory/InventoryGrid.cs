using UnityEngine;
using System.Collections.Generic;
using DungeonDredge.Core;

namespace DungeonDredge.Inventory
{
    public class InventoryGrid : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 6;
        [SerializeField] private int gridHeight = 4;

        [Header("Weight Settings")]
        [SerializeField] private float maxWeightCapacity = 40f;

        // Grid state
        private InventoryItem[,] grid;
        private List<InventoryItem> items = new List<InventoryItem>();

        // Properties
        public int Width => gridWidth;
        public int Height => gridHeight;
        public float MaxWeight => maxWeightCapacity;
        public float CurrentWeight { get; private set; }
        public float WeightRatio => MaxWeight > 0 ? CurrentWeight / MaxWeight : 0f;
        public IReadOnlyList<InventoryItem> Items => items;

        // Events
        public System.Action<InventoryItem> OnItemAdded;
        public System.Action<InventoryItem> OnItemRemoved;
        public System.Action<float> OnWeightChanged;

        private void Awake()
        {
            InitializeGrid();
        }

        public void InitializeGrid()
        {
            grid = new InventoryItem[gridWidth, gridHeight];
            items.Clear();
            CurrentWeight = 0f;
        }

        /// <summary>
        /// Resize the grid (used for backpack upgrades)
        /// </summary>
        public void ResizeGrid(int newWidth, int newHeight)
        {
            // Create new grid
            var newGrid = new InventoryItem[newWidth, newHeight];

            // Copy existing items that still fit
            var itemsToRemove = new List<InventoryItem>();
            
            foreach (var item in items)
            {
                bool fits = true;
                var positions = item.GetOccupiedPositions();
                
                foreach (var pos in positions)
                {
                    if (pos.x >= newWidth || pos.y >= newHeight)
                    {
                        fits = false;
                        break;
                    }
                }

                if (fits)
                {
                    foreach (var pos in positions)
                    {
                        newGrid[pos.x, pos.y] = item;
                    }
                }
                else
                {
                    itemsToRemove.Add(item);
                }
            }

            // Remove items that don't fit
            foreach (var item in itemsToRemove)
            {
                items.Remove(item);
                CurrentWeight -= item.itemData.weight;
                OnItemRemoved?.Invoke(item);
            }

            grid = newGrid;
            gridWidth = newWidth;
            gridHeight = newHeight;

            OnWeightChanged?.Invoke(CurrentWeight);
        }

        /// <summary>
        /// Try to add an item at a specific position
        /// </summary>
        public bool TryAddItem(InventoryItem item, Vector2Int position)
        {
            if (!CanPlaceItem(item, position))
                return false;

            PlaceItem(item, position);
            return true;
        }

        /// <summary>
        /// Try to add an item at the first available position
        /// </summary>
        public bool TryAddItemAuto(InventoryItem item)
        {
            // Try both rotations
            for (int rotation = 0; rotation < 2; rotation++)
            {
                if (rotation == 1 && item.itemData.canRotate)
                {
                    item.Rotate();
                }

                // Scan grid for valid position
                for (int y = 0; y < gridHeight; y++)
                {
                    for (int x = 0; x < gridWidth; x++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (CanPlaceItem(item, pos))
                        {
                            PlaceItem(item, pos);
                            return true;
                        }
                    }
                }

                // Reset rotation if we rotated and failed
                if (rotation == 1 && item.itemData.canRotate)
                {
                    item.Rotate();
                }
            }

            return false;
        }

        /// <summary>
        /// Check if an item can be placed at a position
        /// </summary>
        public bool CanPlaceItem(InventoryItem item, Vector2Int position)
        {
            bool[,] shape = item.GetCurrentShape();

            for (int x = 0; x < item.Width; x++)
            {
                for (int y = 0; y < item.Height; y++)
                {
                    if (!shape[x, y]) continue;

                    int gridX = position.x + x;
                    int gridY = position.y + y;

                    // Check bounds
                    if (gridX < 0 || gridX >= gridWidth ||
                        gridY < 0 || gridY >= gridHeight)
                    {
                        return false;
                    }

                    // Check if cell is occupied by another item
                    if (grid[gridX, gridY] != null && grid[gridX, gridY] != item)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Place an item at a position (assumes CanPlaceItem was already checked)
        /// </summary>
        private void PlaceItem(InventoryItem item, Vector2Int position)
        {
            item.gridPosition = position;
            bool[,] shape = item.GetCurrentShape();

            for (int x = 0; x < item.Width; x++)
            {
                for (int y = 0; y < item.Height; y++)
                {
                    if (shape[x, y])
                    {
                        grid[position.x + x, position.y + y] = item;
                    }
                }
            }

            if (!items.Contains(item))
            {
                items.Add(item);
                CurrentWeight += item.itemData.weight;
                OnItemAdded?.Invoke(item);
                OnWeightChanged?.Invoke(CurrentWeight);
                
                EventBus.Publish(new ItemPickedUpEvent
                {
                    ItemId = item.itemData.itemId,
                    Weight = item.itemData.weight
                });
            }
        }

        /// <summary>
        /// Remove an item from the grid
        /// </summary>
        public bool RemoveItem(InventoryItem item)
        {
            if (!items.Contains(item))
                return false;

            // Clear grid cells
            var positions = item.GetOccupiedPositions();
            foreach (var pos in positions)
            {
                if (pos.x >= 0 && pos.x < gridWidth &&
                    pos.y >= 0 && pos.y < gridHeight)
                {
                    grid[pos.x, pos.y] = null;
                }
            }

            items.Remove(item);
            CurrentWeight -= item.itemData.weight;
            
            OnItemRemoved?.Invoke(item);
            OnWeightChanged?.Invoke(CurrentWeight);

            EventBus.Publish(new ItemDroppedEvent
            {
                ItemId = item.itemData.itemId,
                Weight = item.itemData.weight
            });

            return true;
        }

        /// <summary>
        /// Move an item to a new position
        /// </summary>
        public bool MoveItem(InventoryItem item, Vector2Int newPosition)
        {
            if (!items.Contains(item))
                return false;

            // Store old position
            Vector2Int oldPosition = item.gridPosition;

            // Temporarily remove from grid
            var oldPositions = item.GetOccupiedPositions();
            foreach (var pos in oldPositions)
            {
                if (pos.x >= 0 && pos.x < gridWidth &&
                    pos.y >= 0 && pos.y < gridHeight)
                {
                    grid[pos.x, pos.y] = null;
                }
            }

            // Check if new position is valid
            item.gridPosition = newPosition;
            if (!CanPlaceItem(item, newPosition))
            {
                // Restore old position
                item.gridPosition = oldPosition;
                foreach (var pos in oldPositions)
                {
                    if (pos.x >= 0 && pos.x < gridWidth &&
                        pos.y >= 0 && pos.y < gridHeight)
                    {
                        grid[pos.x, pos.y] = item;
                    }
                }
                return false;
            }

            // Place at new position
            bool[,] shape = item.GetCurrentShape();
            for (int x = 0; x < item.Width; x++)
            {
                for (int y = 0; y < item.Height; y++)
                {
                    if (shape[x, y])
                    {
                        grid[newPosition.x + x, newPosition.y + y] = item;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Get the item at a specific grid position
        /// </summary>
        public InventoryItem GetItemAt(Vector2Int position)
        {
            if (position.x < 0 || position.x >= gridWidth ||
                position.y < 0 || position.y >= gridHeight)
            {
                return null;
            }

            return grid[position.x, position.y];
        }

        /// <summary>
        /// Check if a cell is empty
        /// </summary>
        public bool IsCellEmpty(Vector2Int position)
        {
            return GetItemAt(position) == null;
        }

        /// <summary>
        /// Get total gold value of all items
        /// </summary>
        public int GetTotalValue()
        {
            int total = 0;
            foreach (var item in items)
            {
                total += item.itemData.goldValue;
            }
            return total;
        }

        /// <summary>
        /// Check if inventory contains any rare+ items
        /// </summary>
        public bool HasRareItems()
        {
            foreach (var item in items)
            {
                if (item.itemData.rarity >= ItemRarity.Rare)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Clear all items from the grid
        /// </summary>
        public void ClearAll()
        {
            var itemsCopy = new List<InventoryItem>(items);
            foreach (var item in itemsCopy)
            {
                RemoveItem(item);
            }
        }

        /// <summary>
        /// Set weight capacity (for player strength stat)
        /// </summary>
        public void SetWeightCapacity(float capacity)
        {
            maxWeightCapacity = capacity;
            OnWeightChanged?.Invoke(CurrentWeight);
        }

        #region Save/Load

        public InventorySaveData GetSaveData()
        {
            var saveData = new InventorySaveData
            {
                gridWidth = gridWidth,
                gridHeight = gridHeight,
                items = new List<ItemSaveData>()
            };

            foreach (var item in items)
            {
                saveData.items.Add(item.ToSaveData());
            }

            return saveData;
        }

        public void LoadSaveData(InventorySaveData saveData, ItemDatabase itemDatabase)
        {
            gridWidth = saveData.gridWidth;
            gridHeight = saveData.gridHeight;
            InitializeGrid();

            foreach (var itemSave in saveData.items)
            {
                ItemData itemData = itemDatabase.GetItem(itemSave.itemId);
                if (itemData != null)
                {
                    InventoryItem item = InventoryItem.FromSaveData(itemSave, itemData);
                    TryAddItem(item, item.gridPosition);
                }
            }
        }

        #endregion
    }

    [System.Serializable]
    public class InventorySaveData
    {
        public int gridWidth;
        public int gridHeight;
        public List<ItemSaveData> items;
    }
}
