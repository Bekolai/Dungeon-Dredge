using UnityEngine;
using System.Collections.Generic;
using DungeonDredge.Inventory;
using DungeonDredge.Player;

namespace DungeonDredge.Village
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("Player Reference")]
        [SerializeField] private PlayerInventory playerInventory;

        [Header("Currency")]
        [SerializeField] private int playerGold = 0;

        // Events
        public System.Action<int> OnGoldChanged;
        public System.Action<ItemData, int> OnItemPurchased;
        public System.Action<ItemData, int> OnItemSold;

        public int PlayerGold => playerGold;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetPlayerReference(PlayerInventory inventory)
        {
            playerInventory = inventory;
        }

        #region Currency

        public void AddGold(int amount)
        {
            playerGold += amount;
            OnGoldChanged?.Invoke(playerGold);
        }

        public bool SpendGold(int amount)
        {
            if (playerGold < amount) return false;

            playerGold -= amount;
            OnGoldChanged?.Invoke(playerGold);
            return true;
        }

        public bool HasGold(int amount)
        {
            return playerGold >= amount;
        }

        #endregion

        #region Buying

        public bool CanBuyItem(ShopItem shopItem)
        {
            if (!shopItem.isAvailable) return false;
            if (shopItem.stock == 0) return false;
            if (!HasGold(shopItem.price)) return false;
            
            // Check rank requirement
            if (QuestManager.Instance != null)
            {
                if (shopItem.requiredRank > QuestManager.Instance.CurrentRank)
                    return false;
            }

            return true;
        }

        public bool BuyItem(ShopItem shopItem, ShopData shop)
        {
            if (!CanBuyItem(shopItem)) return false;
            if (playerInventory == null) return false;

            // Try to add to inventory
            if (!playerInventory.TryPickupItem(shopItem.item))
            {
                Debug.Log("Inventory full!");
                return false;
            }

            // Deduct gold
            SpendGold(shopItem.price);

            // Reduce stock
            if (shopItem.stock > 0)
            {
                shopItem.stock--;
            }

            OnItemPurchased?.Invoke(shopItem.item, shopItem.price);
            return true;
        }

        #endregion

        #region Selling

        public int GetSellPrice(ItemData item, ShopData shop)
        {
            return Mathf.RoundToInt(item.goldValue * shop.sellPriceMultiplier);
        }

        public bool SellItem(InventoryItem item, ShopData shop)
        {
            if (playerInventory == null) return false;
            if (item == null) return false;

            int sellPrice = GetSellPrice(item.itemData, shop);

            // Remove from inventory
            if (playerInventory.Grid.RemoveItem(item))
            {
                // Add gold
                AddGold(sellPrice);

                OnItemSold?.Invoke(item.itemData, sellPrice);
                return true;
            }

            return false;
        }

        public void SellAllItems(ShopData shop)
        {
            if (playerInventory == null) return;

            var items = new List<InventoryItem>(playerInventory.Grid.Items);
            int totalGold = 0;

            foreach (var item in items)
            {
                // Skip quest items
                if (item.itemData.category == ItemCategory.QuestItem) continue;

                int sellPrice = GetSellPrice(item.itemData, shop);
                if (playerInventory.Grid.RemoveItem(item))
                {
                    totalGold += sellPrice;
                }
            }

            AddGold(totalGold);
        }

        #endregion

        #region Blacksmith (Upgrades)

        public bool CanUpgradeBackpack()
        {
            if (playerInventory == null) return false;
            if (playerInventory.CurrentBackpack?.nextUpgrade == null) return false;

            int cost = playerInventory.CurrentBackpack.nextUpgrade.upgradeCost;
            return HasGold(cost);
        }

        public bool UpgradeBackpack()
        {
            if (!CanUpgradeBackpack()) return false;

            BackpackData nextBackpack = playerInventory.CurrentBackpack.nextUpgrade;
            
            if (SpendGold(nextBackpack.upgradeCost))
            {
                playerInventory.UpgradeBackpack();
                return true;
            }

            return false;
        }

        #endregion

        #region Save/Load

        public ShopSaveData GetSaveData()
        {
            return new ShopSaveData
            {
                playerGold = playerGold
            };
        }

        public void LoadSaveData(ShopSaveData data)
        {
            playerGold = data.playerGold;
            OnGoldChanged?.Invoke(playerGold);
        }

        #endregion
    }

    [System.Serializable]
    public class ShopSaveData
    {
        public int playerGold;
    }
}
