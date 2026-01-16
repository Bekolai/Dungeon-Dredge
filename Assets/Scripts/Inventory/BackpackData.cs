using UnityEngine;

namespace DungeonDredge.Inventory
{
    [CreateAssetMenu(fileName = "NewBackpack", menuName = "DungeonDredge/Backpack Data")]
    public class BackpackData : ScriptableObject
    {
        [Header("Basic Info")]
        public string backpackId;
        public string backpackName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Grid Size")]
        public int gridWidth = 6;
        public int gridHeight = 4;

        [Header("Upgrade Info")]
        public int upgradeCost = 0;
        public BackpackData nextUpgrade;
        public int upgradeLevel = 1;

        [Header("Visual")]
        public GameObject worldPrefab;

        public int TotalCells => gridWidth * gridHeight;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(backpackId))
            {
                backpackId = name.ToLower().Replace(" ", "_");
            }
            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);
        }
    }
}
