using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DungeonDredge.Inventory
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "DungeonDredge/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemData> items = new List<ItemData>();

        private Dictionary<string, ItemData> itemLookup;

        public IReadOnlyList<ItemData> AllItems => items;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            itemLookup = new Dictionary<string, ItemData>();
            foreach (var item in items)
            {
                if (item != null && !string.IsNullOrEmpty(item.itemId))
                {
                    if (!itemLookup.ContainsKey(item.itemId))
                    {
                        itemLookup[item.itemId] = item;
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate item ID: {item.itemId}");
                    }
                }
            }
        }

        public ItemData GetItem(string itemId)
        {
            if (itemLookup == null)
                BuildLookup();

            itemLookup.TryGetValue(itemId, out ItemData item);
            return item;
        }

        public List<ItemData> GetItemsByRarity(ItemRarity rarity)
        {
            return items.Where(i => i.rarity == rarity).ToList();
        }

        public List<ItemData> GetItemsByCategory(ItemCategory category)
        {
            return items.Where(i => i.category == category).ToList();
        }

        public List<ItemData> GetItemsForRank(DungeonRank rank)
        {
            return items.Where(i => i.minimumRank <= rank).ToList();
        }

        /// <summary>
        /// Get a random item appropriate for the given dungeon rank
        /// </summary>
        public ItemData GetRandomItem(DungeonRank rank)
        {
            var validItems = GetItemsForRank(rank);
            if (validItems.Count == 0) return null;

            // Weighted random selection
            float totalWeight = validItems.Sum(i => i.spawnWeight);
            float randomValue = Random.Range(0f, totalWeight);

            float currentWeight = 0f;
            foreach (var item in validItems)
            {
                currentWeight += item.spawnWeight;
                if (randomValue <= currentWeight)
                {
                    return item;
                }
            }

            return validItems[validItems.Count - 1];
        }

        /// <summary>
        /// Get multiple random items for loot generation
        /// </summary>
        public List<ItemData> GetRandomItems(DungeonRank rank, int count)
        {
            var result = new List<ItemData>();
            for (int i = 0; i < count; i++)
            {
                var item = GetRandomItem(rank);
                if (item != null)
                {
                    result.Add(item);
                }
            }
            return result;
        }

#if UNITY_EDITOR
        [ContextMenu("Find All Items in Project")]
        private void FindAllItems()
        {
            items.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null)
                {
                    items.Add(item);
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"Found {items.Count} items");
        }
#endif
    }
}
