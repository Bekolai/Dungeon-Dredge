using UnityEngine;

namespace DungeonDredge.Inventory
{
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum ItemCategory
    {
        Scrap,
        Material,
        Valuable,
        Tool,
        Consumable,
        QuestItem
    }

    [CreateAssetMenu(fileName = "NewItem", menuName = "DungeonDredge/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemId;
        public string itemName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Grid Shape")]
        [Tooltip("Width of the item in grid cells")]
        public int width = 1;
        [Tooltip("Height of the item in grid cells")]
        public int height = 1;
        [Tooltip("Custom shape (if not rectangular). Leave empty for rectangular items.")]
        public bool[] customShape;
        public bool canRotate = true;

        [Header("Properties")]
        public float weight = 1f;
        public int goldValue = 10;
        public ItemRarity rarity = ItemRarity.Common;
        public ItemCategory category = ItemCategory.Scrap;

        [Header("Dungeon Rank")]
        [Tooltip("Minimum dungeon rank where this item can spawn")]
        public DungeonRank minimumRank = DungeonRank.F;
        [Tooltip("Spawn weight (higher = more common)")]
        public float spawnWeight = 1f;

        [Header("Visuals")]
        public GameObject worldPrefab;
        public Color rarityColor = Color.white;

        /// <summary>
        /// Get the shape as a 2D array (true = occupied cell)
        /// </summary>
        public bool[,] GetShape()
        {
            bool[,] shape = new bool[width, height];

            if (customShape != null && customShape.Length == width * height)
            {
                // Use custom shape
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        shape[x, y] = customShape[y * width + x];
                    }
                }
            }
            else
            {
                // Fill all cells (rectangular)
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        shape[x, y] = true;
                    }
                }
            }

            return shape;
        }

        /// <summary>
        /// Get rotated shape (90 degrees clockwise)
        /// </summary>
        public bool[,] GetRotatedShape()
        {
            bool[,] original = GetShape();
            bool[,] rotated = new bool[height, width];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    rotated[height - 1 - y, x] = original[x, y];
                }
            }

            return rotated;
        }

        /// <summary>
        /// Get the number of cells this item occupies
        /// </summary>
        public int GetCellCount()
        {
            bool[,] shape = GetShape();
            int count = 0;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (shape[x, y]) count++;
                }
            }

            return count;
        }

        public static Color GetRarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => new Color(0.8f, 0.8f, 0.8f),
                ItemRarity.Uncommon => new Color(0.3f, 0.8f, 0.3f),
                ItemRarity.Rare => new Color(0.3f, 0.5f, 1f),
                ItemRarity.Epic => new Color(0.7f, 0.3f, 0.9f),
                ItemRarity.Legendary => new Color(1f, 0.8f, 0.2f),
                _ => Color.white
            };
        }

        private void OnValidate()
        {
            // Auto-generate ID if empty
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = name.ToLower().Replace(" ", "_");
            }

            // Set rarity color
            rarityColor = GetRarityColor(rarity);

            // Validate dimensions
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
        }
    }

    public enum DungeonRank
    {
        F = 0,
        E = 1,
        D = 2,
        C = 3,
        B = 4,
        A = 5,
        S = 6
    }
}
