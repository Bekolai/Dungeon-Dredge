using UnityEngine;
using DungeonDredge.Inventory;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        [Tooltip("Grid cell size - spacing between room centers")]
        public Vector2 roomSize = new Vector2(20f, 20f);
        [Tooltip("Actual room prefab size (smaller than grid cell to leave gaps for corridors)")]
        public float roomActualSize = 16f;
        [Tooltip("Width of corridors (should match gap between rooms)")]
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

        [Header("Mining Settings")]
        [Range(0f, 1f)]
        public float mineableSpawnChance = 0.45f;
        public int minMineablesPerRoom = 0;
        public int maxMineablesPerRoom = 2;
        public GameObject[] mineablePrefabs;

        [Header("Visual Theme")]
        public Material floorMaterial;
        public Material wallMaterial;
        public Material ceilingMaterial;
        public Color ambientColor = Color.gray;
        public float ambientIntensity = 0.5f;

        [Header("Atmosphere")]
        [Tooltip("Override fog density for this rank (0 = use theme default). Higher = less visibility.")]
        [Range(0f, 0.15f)]
        public float atmosphereFogDensity = 0f;
        [Tooltip("Override ambient intensity for this rank (0 = use theme default). Keep VERY low for darkness.")]
        [Range(0f, 0.05f)]
        public float atmosphereAmbientIntensity = 0f;

        [Header("Room Theme (Decorations)")]
        [Tooltip("Theme used for randomized room decorations")]
        public RoomTheme roomTheme;
        [Tooltip("Additional themes that can be mixed in for variety")]
        public RoomTheme[] additionalThemes;

        [Header("Room Prefabs")]
        [Tooltip("Empty rooms - contain nothing")]
        public GameObject[] emptyRoomPrefabs;
        [Tooltip("Portal room - player spawns and extracts here")]
        public GameObject[] portalRoomPrefabs;
        [Tooltip("Loot rooms - contain treasure")]
        public GameObject[] lootRoomPrefabs;
        [Tooltip("Enemy rooms - contain monsters")]
        public GameObject[] enemyRoomPrefabs;
        [Tooltip("Boss room - high value loot, challenging enemies")]
        public GameObject[] bossRoomPrefabs;

        [Header("Corridor Prefabs")]
        [Tooltip("Straight corridors - connect 2 opposite directions (N-S or E-W)")]
        public GameObject[] straightCorridorPrefabs;
        [Tooltip("L-shaped corner corridors - connect 2 adjacent directions")]
        public GameObject[] lCorridorPrefabs;
        [Tooltip("T-junction corridors - connect 3 directions")]
        public GameObject[] tJunctionPrefabs;
        [Tooltip("Crossroad corridors - connect all 4 directions")]
        public GameObject[] crossroadPrefabs;

        public void EnsureRankConfiguration()
        {
            if (roomTheme == null)
            {
                roomTheme = LoadDefaultThemeForRank(rank);
            }

            EnsureDefaultPrefabs();
        }

        public static DungeonSettings GetSettingsForRank(DungeonRank rank)
        {
            // Try to load from Resources first (preferred - has prefabs assigned)
            string resourcePath = $"DungeonSettings/DungeonSettings_{rank}";
            DungeonSettings loaded = Resources.Load<DungeonSettings>(resourcePath);
            if (loaded != null)
            {
                loaded.EnsureRankConfiguration();
                Debug.Log($"[DungeonSettings] Loaded settings for rank {rank} from Resources.");
                return loaded;
            }

#if UNITY_EDITOR
            // Editor fallback: load from regular asset path.
            string assetPath = $"Assets/ScriptableObjects/Dungeons/DungeonSettings_Rank{rank}.asset";
            loaded = AssetDatabase.LoadAssetAtPath<DungeonSettings>(assetPath);
            if (loaded != null)
            {
                loaded.EnsureRankConfiguration();
                Debug.Log($"[DungeonSettings] Loaded settings for rank {rank} from asset path.");
                return loaded;
            }
#endif

            // Fallback: create runtime settings (WARNING: no prefabs!)
            Debug.LogWarning($"[DungeonSettings] No DungeonSettings asset found at 'Resources/{resourcePath}'. " +
                "Using runtime defaults WITHOUT room prefabs. Create DungeonSettings assets for proper generation.");
            
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
                    // Rank F: Dark but has more torches. Still need lantern.
                    settings.atmosphereFogDensity = 0.035f;
                    settings.atmosphereAmbientIntensity = 0.025f;
                    break;

                case DungeonRank.E:
                    settings.gridSize = new Vector2Int(7, 7);
                    settings.minRooms = 12;
                    settings.maxRooms = 18;
                    settings.enemyDensity = 0.5f;
                    settings.maxEnemiesPerRoom = 3;
                    settings.rareLootChance = 0.1f;
                    // Rank E: Very dark, thicker fog eats torch reach
                    settings.atmosphereFogDensity = 0.045f;
                    settings.atmosphereAmbientIntensity = 0.015f;
                    break;

                case DungeonRank.D:
                    settings.gridSize = new Vector2Int(9, 9);
                    settings.minRooms = 18;
                    settings.maxRooms = 25;
                    settings.enemyDensity = 0.7f;
                    settings.maxEnemiesPerRoom = 4;
                    settings.rareLootChance = 0.2f;
                    // Rank D: Near pitch black, heavy fog, lantern essential
                    settings.atmosphereFogDensity = 0.06f;
                    settings.atmosphereAmbientIntensity = 0.01f;
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
                    // High ranks: Pitch black. Fog swallows everything. Lantern or die.
                    settings.atmosphereFogDensity = 0.08f;
                    settings.atmosphereAmbientIntensity = 0.005f;
                    break;
            }

            settings.EnsureRankConfiguration();

            return settings;
        }

        private static RoomTheme LoadDefaultThemeForRank(DungeonRank rank)
        {
            string themeAssetName = rank switch
            {
                DungeonRank.F => "RoomTheme_StoneDungeon",
                DungeonRank.E => "RoomTheme_DarkDungeon",
                DungeonRank.D => "RoomTheme_CaveSystem",
                _ => "RoomTheme_Necropolis"
            };

            RoomTheme theme = Resources.Load<RoomTheme>($"Themes/{themeAssetName}");
            if (theme != null)
                return theme;

#if UNITY_EDITOR
            string assetPath = $"Assets/ScriptableObjects/Themes/{themeAssetName}.asset";
            theme = AssetDatabase.LoadAssetAtPath<RoomTheme>(assetPath);
            if (theme != null)
                return theme;
#endif

            Debug.LogWarning($"[DungeonSettings] Could not auto-assign theme for rank {rank}. Expected {themeAssetName}.");
            return null;
        }

        private void EnsureDefaultPrefabs()
        {
            if (emptyRoomPrefabs == null || emptyRoomPrefabs.Length == 0)
            {
                emptyRoomPrefabs = LoadPrefabCandidates(
                    "DungeonRooms/Stone/Room_Empty_Stone",
                    "DungeonRooms/Room_Empty_Template",
                    "Assets/Prefabs/DungeonRooms/Stone/Room_Empty_Stone.prefab",
                    "Assets/Prefabs/DungeonRooms/Room_Empty_Template.prefab");
            }

            if (portalRoomPrefabs == null || portalRoomPrefabs.Length == 0)
            {
                portalRoomPrefabs = LoadPrefabCandidates(
                    "DungeonRooms/Stone/Room_Portal_Stone",
                    "DungeonRooms/Room_Portal_Template",
                    "Assets/Prefabs/DungeonRooms/Stone/Room_Portal_Stone.prefab",
                    "Assets/Prefabs/DungeonRooms/Room_Portal_Template.prefab");
            }

            if (lootRoomPrefabs == null || lootRoomPrefabs.Length == 0)
            {
                lootRoomPrefabs = LoadPrefabCandidates(
                    "DungeonRooms/Stone/Room_Loot_Stone",
                    "DungeonRooms/Room_Loot_Template",
                    "Assets/Prefabs/DungeonRooms/Stone/Room_Loot_Stone.prefab",
                    "Assets/Prefabs/DungeonRooms/Room_Loot_Template.prefab");
            }

            if (enemyRoomPrefabs == null || enemyRoomPrefabs.Length == 0)
            {
                enemyRoomPrefabs = LoadPrefabCandidates(
                    "DungeonRooms/Stone/Room_Enemy_Stone",
                    "DungeonRooms/Room_Enemy_Template",
                    "Assets/Prefabs/DungeonRooms/Stone/Room_Enemy_Stone.prefab",
                    "Assets/Prefabs/DungeonRooms/Room_Enemy_Template.prefab");
            }

            if (bossRoomPrefabs == null || bossRoomPrefabs.Length == 0)
            {
                bossRoomPrefabs = LoadPrefabCandidates(
                    "DungeonRooms/Stone/Room_Boss_Stone",
                    "DungeonRooms/Room_Boss_Template",
                    "Assets/Prefabs/DungeonRooms/Stone/Room_Boss_Stone.prefab",
                    "Assets/Prefabs/DungeonRooms/Room_Boss_Template.prefab");
            }

            if (straightCorridorPrefabs == null || straightCorridorPrefabs.Length == 0)
            {
                straightCorridorPrefabs = LoadPrefabCandidates(
                    "DungeonCorridors/Corridor_Straight",
                    "DungeonRooms/Corridor_Template",
                    "Assets/Prefabs/DungeonCorridors/Corridor_Straight.prefab",
                    "Assets/Prefabs/DungeonRooms/Corridor_Template.prefab");
            }

            if (lCorridorPrefabs == null || lCorridorPrefabs.Length == 0)
            {
                lCorridorPrefabs = LoadPrefabCandidates(
                    "DungeonCorridors/Corridor_LCorner",
                    null,
                    "Assets/Prefabs/DungeonCorridors/Corridor_LCorner.prefab",
                    null);
            }

            if (tJunctionPrefabs == null || tJunctionPrefabs.Length == 0)
            {
                tJunctionPrefabs = LoadPrefabCandidates(
                    "DungeonCorridors/Corridor_TJunction",
                    null,
                    "Assets/Prefabs/DungeonCorridors/Corridor_TJunction.prefab",
                    null);
            }

            if (crossroadPrefabs == null || crossroadPrefabs.Length == 0)
            {
                crossroadPrefabs = LoadPrefabCandidates(
                    "DungeonCorridors/Corridor_Crossroads",
                    null,
                    "Assets/Prefabs/DungeonCorridors/Corridor_Crossroads.prefab",
                    null);
            }

            if (mineablePrefabs == null || mineablePrefabs.Length == 0)
            {
                mineablePrefabs = LoadPrefabCandidates(
                    "DungeonProps/MineableCrystal",
                    "DungeonProps/MineableNode",
                    "Assets/Prefabs/DungeonProps/MineableCrystal.prefab",
                    "Assets/Prefabs/DungeonProps/MineableNode.prefab");
            }
        }

        private GameObject[] LoadPrefabCandidates(
            string resourcesPathPrimary,
            string resourcesPathSecondary,
            string editorAssetPathPrimary,
            string editorAssetPathSecondary)
        {
            var results = new System.Collections.Generic.List<GameObject>();

            if (!string.IsNullOrEmpty(resourcesPathPrimary))
            {
                GameObject prefab = Resources.Load<GameObject>(resourcesPathPrimary);
                if (prefab != null) results.Add(prefab);
            }
            if (!string.IsNullOrEmpty(resourcesPathSecondary))
            {
                GameObject prefab = Resources.Load<GameObject>(resourcesPathSecondary);
                if (prefab != null) results.Add(prefab);
            }

#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(editorAssetPathPrimary))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(editorAssetPathPrimary);
                if (prefab != null && !results.Contains(prefab)) results.Add(prefab);
            }
            if (!string.IsNullOrEmpty(editorAssetPathSecondary))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(editorAssetPathSecondary);
                if (prefab != null && !results.Contains(prefab)) results.Add(prefab);
            }
#endif

            return results.ToArray();
        }
    }
}
