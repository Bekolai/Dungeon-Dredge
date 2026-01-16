using UnityEngine;

namespace DungeonDredge.Inventory
{
    /// <summary>
    /// Runtime instance of an item in the inventory
    /// </summary>
    [System.Serializable]
    public class InventoryItem
    {
        public ItemData itemData;
        public Vector2Int gridPosition;
        public bool isRotated;
        public string uniqueId;

        public InventoryItem(ItemData data)
        {
            itemData = data;
            gridPosition = Vector2Int.zero;
            isRotated = false;
            uniqueId = System.Guid.NewGuid().ToString();
        }

        public InventoryItem(ItemData data, Vector2Int position, bool rotated = false)
        {
            itemData = data;
            gridPosition = position;
            isRotated = rotated;
            uniqueId = System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Get the current width (accounting for rotation)
        /// </summary>
        public int Width => isRotated ? itemData.height : itemData.width;

        /// <summary>
        /// Get the current height (accounting for rotation)
        /// </summary>
        public int Height => isRotated ? itemData.width : itemData.height;

        /// <summary>
        /// Get the shape accounting for rotation
        /// </summary>
        public bool[,] GetCurrentShape()
        {
            return isRotated ? itemData.GetRotatedShape() : itemData.GetShape();
        }

        /// <summary>
        /// Toggle rotation state
        /// </summary>
        public void Rotate()
        {
            if (itemData.canRotate)
            {
                isRotated = !isRotated;
            }
        }

        /// <summary>
        /// Get all grid positions this item occupies
        /// </summary>
        public Vector2Int[] GetOccupiedPositions()
        {
            bool[,] shape = GetCurrentShape();
            var positions = new System.Collections.Generic.List<Vector2Int>();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (shape[x, y])
                    {
                        positions.Add(gridPosition + new Vector2Int(x, y));
                    }
                }
            }

            return positions.ToArray();
        }

        /// <summary>
        /// Check if this item occupies a specific grid position
        /// </summary>
        public bool OccupiesPosition(Vector2Int position)
        {
            bool[,] shape = GetCurrentShape();
            Vector2Int localPos = position - gridPosition;

            if (localPos.x < 0 || localPos.x >= Width ||
                localPos.y < 0 || localPos.y >= Height)
            {
                return false;
            }

            return shape[localPos.x, localPos.y];
        }

        #region Save/Load

        public ItemSaveData ToSaveData()
        {
            return new ItemSaveData
            {
                itemId = itemData.itemId,
                gridX = gridPosition.x,
                gridY = gridPosition.y,
                isRotated = isRotated,
                uniqueId = uniqueId
            };
        }

        public static InventoryItem FromSaveData(ItemSaveData saveData, ItemData itemData)
        {
            return new InventoryItem(itemData, new Vector2Int(saveData.gridX, saveData.gridY), saveData.isRotated)
            {
                uniqueId = saveData.uniqueId
            };
        }

        #endregion
    }

    [System.Serializable]
    public class ItemSaveData
    {
        public string itemId;
        public int gridX;
        public int gridY;
        public bool isRotated;
        public string uniqueId;
    }
}
