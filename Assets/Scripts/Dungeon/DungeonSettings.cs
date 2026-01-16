using UnityEngine;
using DungeonDredge.Inventory;

namespace DungeonDredge.Dungeon
{
    [CreateAssetMenu(fileName = "DungeonSettings", menuName = "DungeonDredge/Dungeon Settings")]
    public class DungeonSettings : ScriptableObject
    {
        [Header("Rank Configuration")]
        public DungeonRank rank = DungeonRank.F;

        [Header("Grid Settings")]
        [Tooltip("Size of the dungeon grid in rooms")]
        public Vector2Int gridSize = new Vector2Int(5, 5);
        [Tooltip("Physical size of each room in units")]
        public Vector2 roomSize = new Vector2(20f, 20f);
        [Tooltip("Width of corridors")]
        public float corridorWidth = 4f;

        [Header("Room Counts")]
        public int minRooms = 8;
        public int maxRooms = 12;
        [Range(0f, 1f)]
        public float lootRoomChance = 0.3f;
        [Range(0f, 1f)]
        public float enemyRoomChance = 0.4f;
        [Range(0f, 1f)]
        public float emptyRoomChance = 0.3f;

        [Header("Enemy Settings")]
        public int minEnemiesPerRoom = 0;
        public int maxEnemiesPerRoom = 3;
        [Range(0f, 1f)]
        public float enemyDensity = 0.5f;

        [Header("Loot Settings")]
        public int minLootPerRoom = 1;
        public int maxLootPerRoom = 4;
        [Range(0f, 1f)]
        public float rareLootChance = 0.1f;

        [Header("Visual Theme")]
        public Material floorMaterial;
        public Material wallMaterial;
        public Material ceilingMaterial;
        public Color ambientColor = Color.gray;
        public float ambientIntensity = 0.5f;

        [Header("Room Prefabs")]
        public GameObject[] spawnRoomPrefabs;
        public GameObject[] lootRoomPrefabs;
        public GameObject[] enemyRoomPrefabs;
        public GameObject[] extractionRoomPrefabs;
        public GameObject[] corridorPrefabs;

        public static DungeonSettings GetSettingsForRank(DungeonRank rank)
        {
            // Return default settings based on rank
            // In practice, these would be loaded from assets
            DungeonSettings settings = CreateInstance<DungeonSettings>();
            settings.rank = rank;

            switch (rank)
            {
                case DungeonRank.F:
                    settings.gridSize = new Vector2Int(5, 5);
                    settings.minRooms = 8;
                    settings.maxRooms = 12;
                    settings.enemyDensity = 0.3f;
                    settings.maxEnemiesPerRoom = 2;
                    settings.rareLootChance = 0.05f;
                    break;

                case DungeonRank.E:
                    settings.gridSize = new Vector2Int(7, 7);
                    settings.minRooms = 12;
                    settings.maxRooms = 18;
                    settings.enemyDensity = 0.5f;
                    settings.maxEnemiesPerRoom = 3;
                    settings.rareLootChance = 0.1f;
                    break;

                case DungeonRank.D:
                    settings.gridSize = new Vector2Int(9, 9);
                    settings.minRooms = 18;
                    settings.maxRooms = 25;
                    settings.enemyDensity = 0.7f;
                    settings.maxEnemiesPerRoom = 4;
                    settings.rareLootChance = 0.2f;
                    break;

                case DungeonRank.C:
                case DungeonRank.B:
                case DungeonRank.A:
                case DungeonRank.S:
                    settings.gridSize = new Vector2Int(11, 11);
                    settings.minRooms = 25;
                    settings.maxRooms = 35;
                    settings.enemyDensity = 0.8f;
                    settings.maxEnemiesPerRoom = 5;
                    settings.rareLootChance = 0.3f;
                    break;
            }

            return settings;
        }
    }
}
