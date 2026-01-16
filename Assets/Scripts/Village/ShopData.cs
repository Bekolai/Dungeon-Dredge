using UnityEngine;
using DungeonDredge.Inventory;

namespace DungeonDredge.Village
{
    public enum ShopType
    {
        GeneralStore,
        Blacksmith,
        Guild
    }

    [CreateAssetMenu(fileName = "NewShop", menuName = "DungeonDredge/Shop Data")]
    public class ShopData : ScriptableObject
    {
        [Header("Shop Info")]
        public string shopId;
        public string shopName;
        [TextArea(2, 4)]
        public string description;
        public ShopType shopType;

        [Header("Stock")]
        public ShopItem[] items;

        [Header("Sell Rates")]
        [Range(0.1f, 1f)]
        public float sellPriceMultiplier = 0.5f;

        [Header("Visual")]
        public Sprite shopIcon;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(shopId))
            {
                shopId = name.ToLower().Replace(" ", "_");
            }
        }
    }

    [System.Serializable]
    public class ShopItem
    {
        public ItemData item;
        public int price;
        public int stock = -1; // -1 for unlimited
        public bool isAvailable = true;
        public DungeonRank requiredRank = DungeonRank.F;
    }
}
