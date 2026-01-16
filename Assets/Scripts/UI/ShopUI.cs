using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DungeonDredge.Village;
using DungeonDredge.Inventory;

namespace DungeonDredge.UI
{
    public class ShopUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private GameObject buyPanel;
        [SerializeField] private GameObject sellPanel;

        [Header("Shop Info")]
        [SerializeField] private TextMeshProUGUI shopNameText;
        [SerializeField] private TextMeshProUGUI shopDescriptionText;
        [SerializeField] private TextMeshProUGUI playerGoldText;

        [Header("Buy List")]
        [SerializeField] private Transform buyItemContainer;
        [SerializeField] private GameObject buyItemPrefab;

        [Header("Sell List")]
        [SerializeField] private Transform sellItemContainer;
        [SerializeField] private GameObject sellItemPrefab;

        [Header("Item Details")]
        [SerializeField] private Image selectedItemIcon;
        [SerializeField] private TextMeshProUGUI selectedItemName;
        [SerializeField] private TextMeshProUGUI selectedItemDescription;
        [SerializeField] private TextMeshProUGUI selectedItemPrice;
        [SerializeField] private Button actionButton;
        [SerializeField] private TextMeshProUGUI actionButtonText;

        [Header("Tabs")]
        [SerializeField] private Button buyTabButton;
        [SerializeField] private Button sellTabButton;
        [SerializeField] private Button closeButton;

        private ShopData currentShop;
        private ShopItem selectedBuyItem;
        private InventoryItem selectedSellItem;
        private bool isBuyMode = true;

        private List<GameObject> spawnedItems = new List<GameObject>();

        private void Start()
        {
            if (buyTabButton != null)
                buyTabButton.onClick.AddListener(() => SetBuyMode(true));
            if (sellTabButton != null)
                sellTabButton.onClick.AddListener(() => SetBuyMode(false));
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (actionButton != null)
                actionButton.onClick.AddListener(OnActionButtonClicked);

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnGoldChanged += UpdateGoldDisplay;
            }

            Close();
        }

        private void OnDestroy()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
            }
        }

        public void Open(ShopData shop)
        {
            currentShop = shop;
            shopPanel.SetActive(true);

            if (shopNameText != null)
                shopNameText.text = shop.shopName;
            if (shopDescriptionText != null)
                shopDescriptionText.text = shop.description;

            UpdateGoldDisplay(ShopManager.Instance?.PlayerGold ?? 0);
            SetBuyMode(true);

            // Pause/unlock cursor
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            shopPanel.SetActive(false);
            currentShop = null;
            ClearSelection();

            // Resume
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void SetBuyMode(bool buy)
        {
            isBuyMode = buy;
            
            if (buyPanel != null) buyPanel.SetActive(buy);
            if (sellPanel != null) sellPanel.SetActive(!buy);

            ClearSelection();

            if (buy)
                PopulateBuyList();
            else
                PopulateSellList();
        }

        private void PopulateBuyList()
        {
            ClearSpawnedItems();
            if (currentShop == null || buyItemPrefab == null) return;

            foreach (var shopItem in currentShop.items)
            {
                if (!shopItem.isAvailable) continue;
                if (shopItem.stock == 0) continue;

                GameObject itemObj = Instantiate(buyItemPrefab, buyItemContainer);
                spawnedItems.Add(itemObj);

                var itemUI = itemObj.GetComponent<ShopItemUI>();
                if (itemUI != null)
                {
                    itemUI.Setup(shopItem, OnBuyItemSelected);
                }
            }
        }

        private void PopulateSellList()
        {
            ClearSpawnedItems();
            if (sellItemPrefab == null) return;

            var playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory?.Grid == null) return;

            foreach (var item in playerInventory.Grid.Items)
            {
                // Skip quest items
                if (item.itemData.category == ItemCategory.QuestItem) continue;

                GameObject itemObj = Instantiate(sellItemPrefab, sellItemContainer);
                spawnedItems.Add(itemObj);

                var itemUI = itemObj.GetComponent<SellItemUI>();
                if (itemUI != null)
                {
                    int sellPrice = ShopManager.Instance?.GetSellPrice(item.itemData, currentShop) ?? 0;
                    itemUI.Setup(item, sellPrice, OnSellItemSelected);
                }
            }
        }

        private void OnBuyItemSelected(ShopItem item)
        {
            selectedBuyItem = item;
            selectedSellItem = null;
            UpdateItemDetails();
        }

        private void OnSellItemSelected(InventoryItem item)
        {
            selectedSellItem = item;
            selectedBuyItem = null;
            UpdateItemDetails();
        }

        private void UpdateItemDetails()
        {
            if (isBuyMode && selectedBuyItem != null)
            {
                if (selectedItemIcon != null)
                    selectedItemIcon.sprite = selectedBuyItem.item.icon;
                if (selectedItemName != null)
                    selectedItemName.text = selectedBuyItem.item.itemName;
                if (selectedItemDescription != null)
                    selectedItemDescription.text = selectedBuyItem.item.description;
                if (selectedItemPrice != null)
                    selectedItemPrice.text = $"Price: {selectedBuyItem.price} Gold";
                if (actionButtonText != null)
                    actionButtonText.text = "Buy";
                if (actionButton != null)
                    actionButton.interactable = ShopManager.Instance?.CanBuyItem(selectedBuyItem) ?? false;
            }
            else if (!isBuyMode && selectedSellItem != null)
            {
                int sellPrice = ShopManager.Instance?.GetSellPrice(selectedSellItem.itemData, currentShop) ?? 0;

                if (selectedItemIcon != null)
                    selectedItemIcon.sprite = selectedSellItem.itemData.icon;
                if (selectedItemName != null)
                    selectedItemName.text = selectedSellItem.itemData.itemName;
                if (selectedItemDescription != null)
                    selectedItemDescription.text = selectedSellItem.itemData.description;
                if (selectedItemPrice != null)
                    selectedItemPrice.text = $"Value: {sellPrice} Gold";
                if (actionButtonText != null)
                    actionButtonText.text = "Sell";
                if (actionButton != null)
                    actionButton.interactable = true;
            }
            else
            {
                ClearSelection();
            }
        }

        private void ClearSelection()
        {
            selectedBuyItem = null;
            selectedSellItem = null;

            if (selectedItemIcon != null)
                selectedItemIcon.sprite = null;
            if (selectedItemName != null)
                selectedItemName.text = "";
            if (selectedItemDescription != null)
                selectedItemDescription.text = "Select an item";
            if (selectedItemPrice != null)
                selectedItemPrice.text = "";
            if (actionButton != null)
                actionButton.interactable = false;
        }

        private void OnActionButtonClicked()
        {
            if (isBuyMode && selectedBuyItem != null)
            {
                if (ShopManager.Instance?.BuyItem(selectedBuyItem, currentShop) == true)
                {
                    PopulateBuyList();
                    ClearSelection();
                }
            }
            else if (!isBuyMode && selectedSellItem != null)
            {
                if (ShopManager.Instance?.SellItem(selectedSellItem, currentShop) == true)
                {
                    PopulateSellList();
                    ClearSelection();
                }
            }
        }

        private void UpdateGoldDisplay(int gold)
        {
            if (playerGoldText != null)
            {
                playerGoldText.text = $"Gold: {gold}";
            }
        }

        private void ClearSpawnedItems()
        {
            foreach (var item in spawnedItems)
            {
                if (item != null)
                    Destroy(item);
            }
            spawnedItems.Clear();
        }
    }

    // Helper components for shop items
    public class ShopItemUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button selectButton;

        private ShopItem shopItem;
        private System.Action<ShopItem> onSelected;

        public void Setup(ShopItem item, System.Action<ShopItem> callback)
        {
            shopItem = item;
            onSelected = callback;

            if (iconImage != null)
                iconImage.sprite = item.item.icon;
            if (nameText != null)
                nameText.text = item.item.itemName;
            if (priceText != null)
                priceText.text = $"{item.price}g";
            if (selectButton != null)
                selectButton.onClick.AddListener(() => onSelected?.Invoke(shopItem));
        }
    }

    public class SellItemUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private Button selectButton;

        private InventoryItem inventoryItem;
        private System.Action<InventoryItem> onSelected;

        public void Setup(InventoryItem item, int sellPrice, System.Action<InventoryItem> callback)
        {
            inventoryItem = item;
            onSelected = callback;

            if (iconImage != null)
                iconImage.sprite = item.itemData.icon;
            if (nameText != null)
                nameText.text = item.itemData.itemName;
            if (priceText != null)
                priceText.text = $"{sellPrice}g";
            if (selectButton != null)
                selectButton.onClick.AddListener(() => onSelected?.Invoke(inventoryItem));
        }
    }
}
