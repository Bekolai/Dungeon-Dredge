using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using DungeonDredge.Dungeon;
using DungeonDredge.Inventory;
using DungeonDredge.Audio;
using System.IO;

namespace DungeonDredge.Editor
{
    public class DungeonSetupTool : EditorWindow
    {
        private ItemDatabase itemDatabase;
        private DungeonSettings defaultSettings;
        private GameObject[] roomPrefabs;
        private Vector2 scrollPosition;

        // Paths
        private const string ITEMS_PATH = "Assets/ScriptableObjects/Items";
        private const string AUDIO_PATH = "Assets/ScriptableObjects/Audio";
        private const string QUATERNIUS_PATH = "Assets/Assets/Quaternius/Fantasy Props MegaKit[Standard]/Exports/FBX";
        private const string ARTSYSTACK_ICONS_PATH = "Assets/Assets/Artsystack - Fantasy RPG GUI/ResourcesData/Sprites/flaticon/textured";
        private const string FOOTSTEPS_PATH = "Assets/Assets/cplomedia/Footsteps Pack";

        [MenuItem("DungeonDredge/Setup Tools/Dungeon Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<DungeonSetupTool>("Dungeon Setup");
        }

        [MenuItem("DungeonDredge/Setup Tools/Create All Items (50)")]
        public static void CreateAllItemsMenu()
        {
            var window = GetWindow<DungeonSetupTool>("Dungeon Setup");
            window.CreateAllItems();
        }

        [MenuItem("DungeonDredge/Setup Tools/Create Footstep Sound Sets")]
        public static void CreateFootstepSetsMenu()
        {
            var window = GetWindow<DungeonSetupTool>("Dungeon Setup");
            window.CreateFootstepSoundSets();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            GUILayout.Label("Dungeon Dredge Setup Wizard", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 1. Item Database
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("1. Core Assets", EditorStyles.boldLabel);
            itemDatabase = (ItemDatabase)EditorGUILayout.ObjectField("Item Database", itemDatabase, typeof(ItemDatabase), false);
            
            if (itemDatabase == null)
            {
                if (GUILayout.Button("Find or Create Item Database"))
                {
                    FindOrCreateItemDatabase();
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // 2. Items
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("2. Items (50 Total)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create All Items", GUILayout.Height(30)))
            {
                CreateAllItems();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rank F (15)")) CreateRankFItems();
            if (GUILayout.Button("Rank E (15)")) CreateRankEItems();
            if (GUILayout.Button("Rank D (12)")) CreateRankDItems();
            if (GUILayout.Button("Special (8)")) CreateSpecialItems();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // 3. Audio
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("3. Footstep Audio", EditorStyles.boldLabel);
            if (GUILayout.Button("Create Footstep Sound Sets"))
            {
                CreateFootstepSoundSets();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // 4. Room Themes
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("4. Room Themes (Decoration Pools)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Room Themes define decoration pools. After creation:\n" +
                "1. Open each theme asset\n" +
                "2. Drag FAB prefabs into the decoration arrays\n" +
                "3. Adjust spawn weights and counts", 
                MessageType.Info);
            if (GUILayout.Button("Create Room Theme Templates"))
            {
                CreateRoomThemes();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // 5. Dungeon Settings
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("5. Dungeon Settings", EditorStyles.boldLabel);
            if (GUILayout.Button("Create Dungeon Settings (F, E, D)"))
            {
                CreateDungeonSettings();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // 5. Room Prefabs
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("5. Room Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Room templates are structural frameworks. After creating them:\n" +
                "1. Open each prefab in Prefab Mode\n" +
                "2. Replace placeholders with FAB dungeon meshes\n" +
                "3. Add decorations (torches, props, etc.)", 
                MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Room Templates"))
            {
                CreateRoomPrefabTemplates();
            }
            if (GUILayout.Button("Create Basic Placeholders"))
            {
                CreatePlaceholderRooms();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // 5b. Corridor Prefabs
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("5b. Corridor Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Corridor templates for connecting rooms:\n" +
                "• Straight - connects N-S or E-W\n" +
                "• L-Corner - 90° turns (NE, SE, SW, NW)\n" +
                "• T-Junction - 3-way intersections\n" +
                "• Crossroads - 4-way intersections\n\n" +
                "Corridors are 4x4 units to fit between 16x16 rooms in 20x20 grid cells.", 
                MessageType.Info);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create All Corridors", GUILayout.Height(30)))
            {
                CreateAllCorridorTemplates();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Straight")) CreateStraightCorridorTemplate();
            if (GUILayout.Button("L-Corner")) CreateLCorridorTemplate();
            if (GUILayout.Button("T-Junction")) CreateTJunctionTemplate();
            if (GUILayout.Button("Crossroads")) CreateCrossroadsTemplate();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            // 6. Scene Setup
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("6. Scene Setup", EditorStyles.boldLabel);
            if (GUILayout.Button("Setup Dungeon Manager in Scene"))
            {
                SetupScene();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }

        #region Item Creation

        private void CreateAllItems()
        {
            EnsureItemsFolderExists();
            
            int created = 0;
            created += CreateRankFItems();
            created += CreateRankEItems();
            created += CreateRankDItems();
            created += CreateSpecialItems();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Update item database
            FindOrCreateItemDatabase();

            Debug.Log($"Created {created} items total!");
            EditorUtility.DisplayDialog("Items Created", $"Successfully created {created} items!\n\nItem Database has been updated.", "OK");
        }

        private int CreateRankFItems()
        {
            EnsureItemsFolderExists();
            int count = 0;

            // Rank F - Common dungeon trash
            count += CreateItem("Gold Coin", "gold_coin", 1, 1, 0.1f, 5, ItemRarity.Common, ItemCategory.Valuable, DungeonRank.F, "Coin.fbx", 2.0f) ? 1 : 0;
            count += CreateItem("Coin Pile", "coin_pile", 2, 1, 0.5f, 25, ItemRarity.Common, ItemCategory.Valuable, DungeonRank.F, "Coin_Pile.fbx", 1.5f) ? 1 : 0;
            count += CreateItem("Rusty Key", "rusty_key", 1, 1, 0.2f, 3, ItemRarity.Common, ItemCategory.Scrap, DungeonRank.F, "Key_Metal.fbx", 1.0f) ? 1 : 0;
            count += CreateItem("Old Rope", "old_rope", 2, 1, 0.8f, 2, ItemRarity.Common, ItemCategory.Material, DungeonRank.F, "Rope_1.fbx", 0.8f) ? 1 : 0;
            count += CreateItem("Bone Fragment", "bone_fragment", 1, 1, 0.3f, 1, ItemRarity.Common, ItemCategory.Scrap, DungeonRank.F, null, 1.2f) ? 1 : 0;
            count += CreateItem("Broken Chain", "broken_chain", 2, 1, 1.0f, 4, ItemRarity.Common, ItemCategory.Scrap, DungeonRank.F, "Chain_Coil.fbx", 0.7f) ? 1 : 0;
            count += CreateItem("Empty Bottle", "empty_bottle", 1, 1, 0.2f, 1, ItemRarity.Common, ItemCategory.Scrap, DungeonRank.F, "Bottle_1.fbx", 1.0f) ? 1 : 0;
            count += CreateItem("Worn Book", "worn_book", 1, 2, 0.5f, 8, ItemRarity.Common, ItemCategory.Material, DungeonRank.F, "Book_5.fbx", 0.9f) ? 1 : 0;
            count += CreateItem("Melted Candle", "melted_candle", 1, 1, 0.1f, 1, ItemRarity.Common, ItemCategory.Scrap, DungeonRank.F, "Candle_1.fbx", 1.1f) ? 1 : 0;
            count += CreateItem("Wooden Bucket", "wooden_bucket", 2, 2, 1.5f, 6, ItemRarity.Common, ItemCategory.Scrap, DungeonRank.F, "Bucket_Wooden_1.fbx", 0.5f) ? 1 : 0;
            count += CreateItem("Iron Mug", "iron_mug", 1, 1, 0.4f, 3, ItemRarity.Common, ItemCategory.Scrap, DungeonRank.F, "Mug.fbx", 1.0f) ? 1 : 0;
            count += CreateItem("Cracked Pot", "cracked_pot", 2, 1, 0.8f, 2, ItemRarity.Common, ItemCategory.Scrap, DungeonRank.F, "Pot_1.fbx", 0.6f) ? 1 : 0;
            count += CreateItem("Tattered Scroll", "tattered_scroll", 1, 2, 0.2f, 10, ItemRarity.Common, ItemCategory.Material, DungeonRank.F, "Scroll_1.fbx", 0.9f) ? 1 : 0;
            count += CreateItem("Rusty Nail Pile", "rusty_nails", 1, 1, 0.3f, 1, ItemRarity.Common, ItemCategory.Scrap, DungeonRank.F, null, 1.3f) ? 1 : 0;
            count += CreateItem("Old Pouch", "old_pouch", 1, 2, 0.4f, 5, ItemRarity.Common, ItemCategory.Material, DungeonRank.F, "Pouch_Large.fbx", 0.8f) ? 1 : 0;

            Debug.Log($"Created {count} Rank F items");
            return count;
        }

        private int CreateRankEItems()
        {
            EnsureItemsFolderExists();
            int count = 0;

            // Rank E - Uncommon valuables
            count += CreateItem("Silver Chalice", "silver_chalice", 1, 2, 0.8f, 35, ItemRarity.Uncommon, ItemCategory.Valuable, DungeonRank.E, "Chalice.fbx", 1.2f) ? 1 : 0;
            count += CreateItem("Golden Key", "golden_key", 1, 1, 0.3f, 50, ItemRarity.Uncommon, ItemCategory.Valuable, DungeonRank.E, "Key_Gold.fbx", 1.0f) ? 1 : 0;
            count += CreateItem("Bronze Axe", "bronze_axe", 2, 1, 2.5f, 40, ItemRarity.Uncommon, ItemCategory.Material, DungeonRank.E, "Axe_Bronze.fbx", 0.8f) ? 1 : 0;
            count += CreateItem("Bronze Pickaxe", "bronze_pickaxe", 2, 1, 2.0f, 35, ItemRarity.Uncommon, ItemCategory.Material, DungeonRank.E, "Pickaxe_Bronze.fbx", 0.7f) ? 1 : 0;
            count += CreateItem("Intact Scroll", "intact_scroll", 1, 2, 0.3f, 45, ItemRarity.Uncommon, ItemCategory.Valuable, DungeonRank.E, "Scroll_1.fbx", 0.9f) ? 1 : 0;
            count += CreateItem("Book Stack", "book_stack", 2, 2, 2.0f, 60, ItemRarity.Uncommon, ItemCategory.Valuable, DungeonRank.E, "Book_Stack_1.fbx", 0.6f) ? 1 : 0;
            count += CreateItem("Ornate Candle", "ornate_candle", 1, 2, 0.5f, 25, ItemRarity.Uncommon, ItemCategory.Valuable, DungeonRank.E, "CandleStick.fbx", 0.9f) ? 1 : 0;
            count += CreateItem("Metal Crate", "metal_crate", 2, 2, 3.0f, 30, ItemRarity.Uncommon, ItemCategory.Material, DungeonRank.E, "Crate_Metal.fbx", 0.4f) ? 1 : 0;
            count += CreateItem("Crystal Shard", "crystal_shard", 1, 2, 0.4f, 55, ItemRarity.Uncommon, ItemCategory.Valuable, DungeonRank.E, null, 1.1f) ? 1 : 0;
            count += CreateItem("Monster Fang", "monster_fang", 1, 1, 0.2f, 40, ItemRarity.Uncommon, ItemCategory.Material, DungeonRank.E, null, 1.3f) ? 1 : 0;
            count += CreateItem("Enchanted Rope", "enchanted_rope", 2, 1, 0.6f, 35, ItemRarity.Uncommon, ItemCategory.Material, DungeonRank.E, "Rope_2.fbx", 0.8f) ? 1 : 0;
            count += CreateItem("Ancient Coin", "ancient_coin", 1, 1, 0.2f, 30, ItemRarity.Uncommon, ItemCategory.Valuable, DungeonRank.E, "Coin.fbx", 1.5f) ? 1 : 0;
            count += CreateItem("Iron Lantern", "iron_lantern", 1, 2, 1.2f, 45, ItemRarity.Uncommon, ItemCategory.Valuable, DungeonRank.E, "Lantern_Wall.fbx", 0.7f) ? 1 : 0;
            count += CreateItem("Spell Book", "spell_book", 2, 2, 1.5f, 75, ItemRarity.Uncommon, ItemCategory.Valuable, DungeonRank.E, "Book_7.fbx", 0.5f) ? 1 : 0;
            count += CreateItem("Cage Key", "cage_key", 1, 1, 0.2f, 20, ItemRarity.Uncommon, ItemCategory.Material, DungeonRank.E, "Key_Metal.fbx", 1.0f) ? 1 : 0;

            Debug.Log($"Created {count} Rank E items");
            return count;
        }

        private int CreateRankDItems()
        {
            EnsureItemsFolderExists();
            int count = 0;

            // Rank D - Rare relics
            count += CreateItem("Golden Chalice", "golden_chalice", 1, 2, 1.0f, 150, ItemRarity.Rare, ItemCategory.Valuable, DungeonRank.D, "Chalice.fbx", 0.8f) ? 1 : 0;
            count += CreateItem("Ancient Relic", "ancient_relic", 2, 2, 2.5f, 200, ItemRarity.Rare, ItemCategory.Valuable, DungeonRank.D, null, 0.5f) ? 1 : 0;
            count += CreateItem("Arcane Tome", "arcane_tome", 2, 3, 3.0f, 250, ItemRarity.Rare, ItemCategory.Valuable, DungeonRank.D, "Book_Stack_2.fbx", 0.4f) ? 1 : 0;
            count += CreateItem("Hero's Axe", "heros_axe", 3, 1, 4.0f, 180, ItemRarity.Rare, ItemCategory.Valuable, DungeonRank.D, "Axe_Bronze.fbx", 0.3f) ? 1 : 0;
            count += CreateItem("Treasure Map", "treasure_map", 2, 1, 0.1f, 100, ItemRarity.Rare, ItemCategory.Valuable, DungeonRank.D, "Scroll_1.fbx", 0.6f) ? 1 : 0;
            count += CreateItem("Jeweled Key", "jeweled_key", 1, 1, 0.3f, 120, ItemRarity.Rare, ItemCategory.Valuable, DungeonRank.D, "Key_Gold.fbx", 0.7f) ? 1 : 0;
            count += CreateItem("Crystal Cluster", "crystal_cluster", 2, 2, 1.5f, 175, ItemRarity.Rare, ItemCategory.Valuable, DungeonRank.D, null, 0.5f) ? 1 : 0;
            count += CreateItem("Dragon Scale", "dragon_scale", 2, 1, 0.8f, 200, ItemRarity.Epic, ItemCategory.Material, DungeonRank.D, null, 0.3f) ? 1 : 0;
            count += CreateItem("Enchanted Chain", "enchanted_chain", 3, 1, 2.0f, 160, ItemRarity.Rare, ItemCategory.Material, DungeonRank.D, "Chain_Coil.fbx", 0.4f) ? 1 : 0;
            count += CreateItem("Royal Coin", "royal_coin", 1, 1, 0.3f, 80, ItemRarity.Rare, ItemCategory.Valuable, DungeonRank.D, "Coin.fbx", 0.8f) ? 1 : 0;
            count += CreateItem("Mystic Orb", "mystic_orb", 2, 2, 1.0f, 220, ItemRarity.Epic, ItemCategory.Valuable, DungeonRank.D, null, 0.3f) ? 1 : 0;
            count += CreateItem("Dungeon Map", "dungeon_map", 2, 2, 0.2f, 90, ItemRarity.Rare, ItemCategory.Valuable, DungeonRank.D, "Scroll_1.fbx", 0.6f) ? 1 : 0;

            Debug.Log($"Created {count} Rank D items");
            return count;
        }

        private int CreateSpecialItems()
        {
            EnsureItemsFolderExists();
            int count = 0;

            // Consumables
            count += CreateItem("Health Potion", "health_potion", 1, 1, 0.3f, 15, ItemRarity.Common, ItemCategory.Consumable, DungeonRank.F, "Potion_1.fbx", 1.5f) ? 1 : 0;
            count += CreateItem("Stamina Potion", "stamina_potion", 1, 1, 0.3f, 20, ItemRarity.Common, ItemCategory.Consumable, DungeonRank.F, "Potion_2.fbx", 1.3f) ? 1 : 0;
            count += CreateItem("Pheromone Flask", "pheromone_flask", 1, 2, 0.5f, 30, ItemRarity.Uncommon, ItemCategory.Consumable, DungeonRank.E, "Potion_4.fbx", 0.8f) ? 1 : 0;
            count += CreateItem("Stun Bomb", "stun_bomb", 1, 1, 0.4f, 25, ItemRarity.Uncommon, ItemCategory.Consumable, DungeonRank.E, null, 0.7f) ? 1 : 0;

            // Quest Items
            count += CreateItem("Guild Badge", "guild_badge", 1, 1, 0.1f, 0, ItemRarity.Common, ItemCategory.QuestItem, DungeonRank.F, null, 0f) ? 1 : 0;
            count += CreateItem("Insect Leg", "insect_leg", 1, 1, 0.1f, 2, ItemRarity.Common, ItemCategory.QuestItem, DungeonRank.F, null, 1.5f) ? 1 : 0;

            // L-Shaped items (custom shapes)
            count += CreateLShapedItem("L-Shaped Relic", "l_relic", 1.5f, 100, ItemRarity.Rare, DungeonRank.E) ? 1 : 0;
            count += CreateTShapedItem("T-Shaped Crystal", "t_crystal", 1.2f, 120, ItemRarity.Rare, DungeonRank.E) ? 1 : 0;

            Debug.Log($"Created {count} Special items");
            return count;
        }

        private bool CreateItem(string itemName, string itemId, int width, int height, float weight, int goldValue,
            ItemRarity rarity, ItemCategory category, DungeonRank minRank, string modelFileName, float spawnWeight)
        {
            string assetPath = $"{ITEMS_PATH}/{itemId}.asset";
            
            // Skip if already exists
            if (AssetDatabase.LoadAssetAtPath<ItemData>(assetPath) != null)
            {
                Debug.Log($"Item already exists: {itemId}");
                return false;
            }

            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            item.itemId = itemId;
            item.itemName = itemName;
            item.description = $"A {rarity.ToString().ToLower()} {category.ToString().ToLower()} from dungeon rank {minRank}.";
            item.width = width;
            item.height = height;
            item.weight = weight;
            item.goldValue = goldValue;
            item.rarity = rarity;
            item.category = category;
            item.minimumRank = minRank;
            item.spawnWeight = spawnWeight;
            item.canRotate = true;
            item.rarityColor = ItemData.GetRarityColor(rarity);

            // Try to find icon
            item.icon = FindIconForItem(itemId, itemName);

            // Try to find world prefab
            if (!string.IsNullOrEmpty(modelFileName))
            {
                string modelPath = $"{QUATERNIUS_PATH}/{modelFileName}";
                item.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            }

            AssetDatabase.CreateAsset(item, assetPath);
            return true;
        }

        private bool CreateLShapedItem(string itemName, string itemId, float weight, int goldValue, ItemRarity rarity, DungeonRank minRank)
        {
            string assetPath = $"{ITEMS_PATH}/{itemId}.asset";
            
            if (AssetDatabase.LoadAssetAtPath<ItemData>(assetPath) != null)
                return false;

            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            item.itemId = itemId;
            item.itemName = itemName;
            item.description = "An unusually shaped ancient relic. Difficult to fit in your pack.";
            item.width = 2;
            item.height = 2;
            item.weight = weight;
            item.goldValue = goldValue;
            item.rarity = rarity;
            item.category = ItemCategory.Valuable;
            item.minimumRank = minRank;
            item.spawnWeight = 0.3f;
            item.canRotate = true;
            item.rarityColor = ItemData.GetRarityColor(rarity);

            // L-shape: occupies 3 of 4 cells
            // [X][_]
            // [X][X]
            item.customShape = new bool[] { true, false, true, true };

            item.icon = FindIconForItem(itemId, itemName);

            AssetDatabase.CreateAsset(item, assetPath);
            return true;
        }

        private bool CreateTShapedItem(string itemName, string itemId, float weight, int goldValue, ItemRarity rarity, DungeonRank minRank)
        {
            string assetPath = $"{ITEMS_PATH}/{itemId}.asset";
            
            if (AssetDatabase.LoadAssetAtPath<ItemData>(assetPath) != null)
                return false;

            ItemData item = ScriptableObject.CreateInstance<ItemData>();
            item.itemId = itemId;
            item.itemName = itemName;
            item.description = "A crystalline formation with an unusual T-shape. Valuable but awkward to carry.";
            item.width = 3;
            item.height = 2;
            item.weight = weight;
            item.goldValue = goldValue;
            item.rarity = rarity;
            item.category = ItemCategory.Valuable;
            item.minimumRank = minRank;
            item.spawnWeight = 0.25f;
            item.canRotate = true;
            item.rarityColor = ItemData.GetRarityColor(rarity);

            // T-shape: 
            // [X][X][X]
            // [_][X][_]
            item.customShape = new bool[] { true, true, true, false, true, false };

            item.icon = FindIconForItem(itemId, itemName);

            AssetDatabase.CreateAsset(item, assetPath);
            return true;
        }

        private Sprite FindIconForItem(string itemId, string itemName)
        {
            // Try to find a matching icon based on item name keywords
            string searchTerm = itemName.ToLower();
            string[] iconKeywords = GetIconKeywordsForItem(searchTerm);

            foreach (string keyword in iconKeywords)
            {
                string[] guids = AssetDatabase.FindAssets($"btn_{keyword} t:Sprite", new[] { ARTSYSTACK_ICONS_PATH });
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
            }

            // Fallback - try to find any matching sprite
            string[] fallbackGuids = AssetDatabase.FindAssets($"t:Sprite", new[] { ARTSYSTACK_ICONS_PATH });
            if (fallbackGuids.Length > 0)
            {
                // Return a default icon
                string path = AssetDatabase.GUIDToAssetPath(fallbackGuids[0]);
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return null;
        }

        private string[] GetIconKeywordsForItem(string itemName)
        {
            // Map item names to likely icon keywords
            if (itemName.Contains("coin")) return new[] { "coin", "gold", "money" };
            if (itemName.Contains("key")) return new[] { "key", "lock" };
            if (itemName.Contains("book") || itemName.Contains("tome")) return new[] { "book", "scroll", "magic" };
            if (itemName.Contains("scroll") || itemName.Contains("map")) return new[] { "scroll", "paper", "map" };
            if (itemName.Contains("potion")) return new[] { "potion", "bottle", "flask" };
            if (itemName.Contains("axe")) return new[] { "axe", "weapon" };
            if (itemName.Contains("rope")) return new[] { "rope", "chain" };
            if (itemName.Contains("chain")) return new[] { "chain", "rope" };
            if (itemName.Contains("chalice") || itemName.Contains("mug")) return new[] { "cup", "chalice", "drink" };
            if (itemName.Contains("candle") || itemName.Contains("lantern")) return new[] { "candle", "light", "torch" };
            if (itemName.Contains("crystal") || itemName.Contains("shard")) return new[] { "crystal", "gem", "diamond" };
            if (itemName.Contains("bone") || itemName.Contains("skull")) return new[] { "skull", "bone" };
            if (itemName.Contains("bucket") || itemName.Contains("pot")) return new[] { "bucket", "pot", "container" };
            if (itemName.Contains("crate") || itemName.Contains("chest")) return new[] { "chest", "box", "crate" };
            if (itemName.Contains("pickaxe")) return new[] { "pickaxe", "tool" };
            if (itemName.Contains("orb")) return new[] { "orb", "sphere", "magic" };
            if (itemName.Contains("scale")) return new[] { "scale", "dragon" };
            if (itemName.Contains("fang")) return new[] { "fang", "tooth", "monster" };
            if (itemName.Contains("badge")) return new[] { "badge", "medal", "shield" };
            if (itemName.Contains("leg") || itemName.Contains("insect")) return new[] { "insect", "bug", "leg" };
            if (itemName.Contains("relic")) return new[] { "relic", "artifact", "ancient" };
            if (itemName.Contains("bottle")) return new[] { "bottle", "flask", "potion" };
            if (itemName.Contains("pouch") || itemName.Contains("bag")) return new[] { "bag", "pouch", "sack" };
            if (itemName.Contains("nail")) return new[] { "nail", "spike", "metal" };
            
            return new[] { "item", "misc" };
        }

        private void EnsureItemsFolderExists()
        {
            if (!Directory.Exists(Application.dataPath + "/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            if (!Directory.Exists(Application.dataPath + "/ScriptableObjects/Items"))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Items");
            }
        }

        #endregion

        #region Footstep Audio

        private void CreateFootstepSoundSets()
        {
            EnsureAudioFolderExists();

            int created = 0;

            // Create sound sets for each surface type
            created += CreateFootstepSoundSet("Stone", "Concrete") ? 1 : 0;
            created += CreateFootstepSoundSet("Metal", "Metal") ? 1 : 0;
            created += CreateFootstepSoundSet("Dirt", "EarthGround") ? 1 : 0;
            created += CreateFootstepSoundSet("Water", "Water") ? 1 : 0;
            created += CreateFootstepSoundSet("Grass", "Grass") ? 1 : 0;
            created += CreateFootstepSoundSet("Gravel", "Gravel") ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created {created} footstep sound sets!");
            EditorUtility.DisplayDialog("Footstep Sound Sets", $"Successfully created {created} footstep sound sets!\n\nAssign them to the FootstepSystem component.", "OK");
        }

        private bool CreateFootstepSoundSet(string surfaceName, string folderName)
        {
            string assetPath = $"{AUDIO_PATH}/FootstepSoundSet_{surfaceName}.asset";
            
            // Check if already exists
            if (AssetDatabase.LoadAssetAtPath<FootstepSoundSetAsset>(assetPath) != null)
            {
                Debug.Log($"Sound set already exists: {surfaceName}");
                return false;
            }

            FootstepSoundSetAsset soundSet = ScriptableObject.CreateInstance<FootstepSoundSetAsset>();
            soundSet.surfaceName = surfaceName;

            // Find audio clips
            string clipsFolderPath = $"{FOOTSTEPS_PATH}/{folderName}";
            List<AudioClip> footstepClips = new List<AudioClip>();
            List<AudioClip> landingClips = new List<AudioClip>();

            // Search for footstep clips
            string[] clipGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { clipsFolderPath });
            
            foreach (string guid in clipGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                
                if (clip != null)
                {
                    string clipName = clip.name.ToLower();
                    
                    // Categorize clips
                    if (clipName.Contains("land") || clipName.Contains("jump"))
                    {
                        landingClips.Add(clip);
                    }
                    else
                    {
                        footstepClips.Add(clip);
                    }
                }
            }

            // Limit to reasonable number (first 20 of each type)
            soundSet.footstepClips = footstepClips.GetRange(0, Mathf.Min(20, footstepClips.Count)).ToArray();
            soundSet.landingClips = landingClips.Count > 0 
                ? landingClips.GetRange(0, Mathf.Min(5, landingClips.Count)).ToArray()
                : new AudioClip[0];
            soundSet.scuffClips = new AudioClip[0]; // Can be populated manually

            AssetDatabase.CreateAsset(soundSet, assetPath);
            Debug.Log($"Created sound set '{surfaceName}' with {soundSet.footstepClips.Length} footstep clips and {soundSet.landingClips.Length} landing clips");
            
            return true;
        }

        private void EnsureAudioFolderExists()
        {
            if (!Directory.Exists(Application.dataPath + "/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            if (!Directory.Exists(Application.dataPath + "/ScriptableObjects/Audio"))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Audio");
            }
        }

        #endregion

        #region Room Themes

        [MenuItem("DungeonDredge/Setup Tools/Create Room Themes")]
        public static void CreateRoomThemesMenu()
        {
            var window = GetWindow<DungeonSetupTool>("Dungeon Setup");
            window.CreateRoomThemes();
        }

        private void CreateRoomThemes()
        {
            EnsureThemesFolderExists();

            int created = 0;

            // Create theme templates for each FAB pack style
            created += CreateRoomTheme("StoneDungeon", "Stone Dungeon", 
                SurfaceType.Stone, 
                new Color(0.4f, 0.35f, 0.3f), 0.5f,
                new Color(1f, 0.7f, 0.4f)) ? 1 : 0;

            created += CreateRoomTheme("DarkDungeon", "Dark Dungeon", 
                SurfaceType.Stone, 
                new Color(0.2f, 0.18f, 0.22f), 0.3f,
                new Color(0.8f, 0.5f, 0.3f)) ? 1 : 0;

            created += CreateRoomTheme("CaveSystem", "Mystery Cave", 
                SurfaceType.Dirt, 
                new Color(0.15f, 0.2f, 0.18f), 0.25f,
                new Color(0.4f, 0.8f, 0.6f)) ? 1 : 0;

            created += CreateRoomTheme("Necropolis", "Necropolis Ruins", 
                SurfaceType.Stone, 
                new Color(0.25f, 0.22f, 0.28f), 0.35f,
                new Color(0.6f, 0.4f, 0.8f)) ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Room Themes Created", 
                $"Created {created} room theme templates!\n\n" +
                "Now populate each theme with prefabs from your FAB packs:\n\n" +
                "• StoneDungeon: ModularDungeonCollection + Dungeon_Environment\n" +
                "• DarkDungeon: Dungeon_Environment variants\n" +
                "• CaveSystem: _MysteryCave prefabs\n" +
                "• Necropolis: Cryptic Realms prefabs\n\n" +
                "Drag prefabs into the decoration arrays in each theme.", "OK");
        }

        private bool CreateRoomTheme(string fileName, string themeName, SurfaceType surface,
            Color ambientColor, float ambientIntensity, Color torchColor)
        {
            string path = $"Assets/ScriptableObjects/Themes/RoomTheme_{fileName}.asset";
            
            if (AssetDatabase.LoadAssetAtPath<RoomTheme>(path) != null)
            {
                Debug.Log($"Room theme already exists: {fileName}");
                return false;
            }

            RoomTheme theme = ScriptableObject.CreateInstance<RoomTheme>();
            theme.themeName = themeName;
            theme.defaultSurface = surface;
            theme.ambientColor = ambientColor;
            theme.ambientIntensity = ambientIntensity;
            theme.torchColor = torchColor;

            // Set reasonable defaults for spawn counts
            theme.minLightSources = 2;
            theme.maxLightSources = 4;
            theme.minLargeProps = 1;
            theme.maxLargeProps = 3;
            theme.minSmallProps = 3;
            theme.maxSmallProps = 8;
            theme.minWallDecorations = 1;
            theme.maxWallDecorations = 4;
            theme.minCornerProps = 0;
            theme.maxCornerProps = 2;
            theme.minFloorClutter = 2;
            theme.maxFloorClutter = 6;
            theme.specialPropChance = 0.15f;

            // Initialize empty arrays (user will populate these)
            theme.lightSources = new DecorationEntry[0];
            theme.largeProps = new DecorationEntry[0];
            theme.smallProps = new DecorationEntry[0];
            theme.wallDecorations = new DecorationEntry[0];
            theme.cornerProps = new DecorationEntry[0];
            theme.floorClutter = new DecorationEntry[0];
            theme.specialProps = new DecorationEntry[0];

            AssetDatabase.CreateAsset(theme, path);
            Debug.Log($"Created room theme: {themeName}");
            return true;
        }

        private void EnsureThemesFolderExists()
        {
            if (!Directory.Exists(Application.dataPath + "/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            if (!Directory.Exists(Application.dataPath + "/ScriptableObjects/Themes"))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Themes");
            }
        }

        #endregion

        #region Dungeon Settings

        [MenuItem("DungeonDredge/Setup Tools/Create Dungeon Settings")]
        public static void CreateDungeonSettingsMenu()
        {
            var window = GetWindow<DungeonSetupTool>("Dungeon Setup");
            window.CreateDungeonSettings();
        }

        private void CreateDungeonSettings()
        {
            EnsureDungeonSettingsFolderExists();

            int created = 0;
            created += CreateDungeonSettingsForRank(DungeonRank.F, "Stone Dungeon", 
                new Color(0.4f, 0.4f, 0.45f), 0.4f) ? 1 : 0;
            created += CreateDungeonSettingsForRank(DungeonRank.E, "Dark Dungeon", 
                new Color(0.25f, 0.25f, 0.3f), 0.3f) ? 1 : 0;
            created += CreateDungeonSettingsForRank(DungeonRank.D, "Cave System", 
                new Color(0.15f, 0.18f, 0.2f), 0.25f) ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created {created} DungeonSettings!");
            EditorUtility.DisplayDialog("Dungeon Settings", 
                $"Successfully created {created} DungeonSettings!\n\n" +
                "Assign room prefabs to each setting in the Inspector.", "OK");
        }

        private bool CreateDungeonSettingsForRank(DungeonRank rank, string themeName, 
            Color ambientColor, float ambientIntensity)
        {
            string path = $"Assets/ScriptableObjects/Dungeons/DungeonSettings_Rank{rank}.asset";
            
            if (AssetDatabase.LoadAssetAtPath<DungeonSettings>(path) != null)
            {
                Debug.Log($"DungeonSettings already exists for rank {rank}");
                return false;
            }

            DungeonSettings settings = ScriptableObject.CreateInstance<DungeonSettings>();
            settings.rank = rank;
            settings.ambientColor = ambientColor;
            settings.ambientIntensity = ambientIntensity;

            // Configure based on rank
            switch (rank)
            {
                case DungeonRank.F:
                    settings.gridSize = new Vector2Int(5, 5);
                    settings.roomSize = new Vector2(20f, 20f);
                    settings.minRooms = 8;
                    settings.maxRooms = 12;
                    settings.lootRoomChance = 0.35f;
                    settings.enemyRoomChance = 0.35f;
                    settings.emptyRoomChance = 0.30f;
                    settings.minEnemiesPerRoom = 0;
                    settings.maxEnemiesPerRoom = 2;
                    settings.enemyDensity = 0.3f;
                    settings.minLootPerRoom = 2;
                    settings.maxLootPerRoom = 5;
                    settings.rareLootChance = 0.05f;
                    break;

                case DungeonRank.E:
                    settings.gridSize = new Vector2Int(7, 7);
                    settings.roomSize = new Vector2(20f, 20f);
                    settings.minRooms = 12;
                    settings.maxRooms = 18;
                    settings.lootRoomChance = 0.30f;
                    settings.enemyRoomChance = 0.45f;
                    settings.emptyRoomChance = 0.25f;
                    settings.minEnemiesPerRoom = 1;
                    settings.maxEnemiesPerRoom = 3;
                    settings.enemyDensity = 0.5f;
                    settings.minLootPerRoom = 2;
                    settings.maxLootPerRoom = 6;
                    settings.rareLootChance = 0.10f;
                    break;

                case DungeonRank.D:
                    settings.gridSize = new Vector2Int(9, 9);
                    settings.roomSize = new Vector2(24f, 24f);
                    settings.minRooms = 18;
                    settings.maxRooms = 25;
                    settings.lootRoomChance = 0.25f;
                    settings.enemyRoomChance = 0.50f;
                    settings.emptyRoomChance = 0.25f;
                    settings.minEnemiesPerRoom = 1;
                    settings.maxEnemiesPerRoom = 4;
                    settings.enemyDensity = 0.7f;
                    settings.minLootPerRoom = 3;
                    settings.maxLootPerRoom = 7;
                    settings.rareLootChance = 0.20f;
                    break;
            }

            settings.roomTheme = LoadThemeForRank(rank);
            settings.additionalThemes = new RoomTheme[0];
            AssignDefaultRoomPrefabs(settings);
            AssignDefaultCorridorPrefabs(settings);

            AssetDatabase.CreateAsset(settings, path);
            Debug.Log($"Created DungeonSettings for Rank {rank}: {themeName}");
            return true;
        }

        private RoomTheme LoadThemeForRank(DungeonRank rank)
        {
            string themePath = rank switch
            {
                DungeonRank.F => "Assets/ScriptableObjects/Themes/RoomTheme_StoneDungeon.asset",
                DungeonRank.E => "Assets/ScriptableObjects/Themes/RoomTheme_DarkDungeon.asset",
                DungeonRank.D => "Assets/ScriptableObjects/Themes/RoomTheme_CaveSystem.asset",
                _ => "Assets/ScriptableObjects/Themes/RoomTheme_Necropolis.asset"
            };

            RoomTheme theme = AssetDatabase.LoadAssetAtPath<RoomTheme>(themePath);
            if (theme == null)
            {
                Debug.LogWarning($"[DungeonSetupTool] Could not find theme at {themePath}.");
            }
            return theme;
        }

        private void AssignDefaultRoomPrefabs(DungeonSettings settings)
        {
            settings.emptyRoomPrefabs = LoadPrefabArray(new[]
            {
                "Assets/Prefabs/DungeonRooms/Stone/Room_Empty_Stone.prefab",
                "Assets/Prefabs/DungeonRooms/Room_Empty_Template.prefab"
            });

            settings.portalRoomPrefabs = LoadPrefabArray(new[]
            {
                "Assets/Prefabs/DungeonRooms/Stone/Room_Portal_Stone.prefab",
                "Assets/Prefabs/DungeonRooms/Room_Portal_Template.prefab"
            });

            settings.lootRoomPrefabs = LoadPrefabArray(new[]
            {
                "Assets/Prefabs/DungeonRooms/Stone/Room_Loot_Stone.prefab",
                "Assets/Prefabs/DungeonRooms/Room_Loot_Template.prefab"
            });

            settings.enemyRoomPrefabs = LoadPrefabArray(new[]
            {
                "Assets/Prefabs/DungeonRooms/Stone/Room_Enemy_Stone.prefab",
                "Assets/Prefabs/DungeonRooms/Room_Enemy_Template.prefab"
            });

            settings.bossRoomPrefabs = LoadPrefabArray(new[]
            {
                "Assets/Prefabs/DungeonRooms/Stone/Room_Boss_Stone.prefab",
                "Assets/Prefabs/DungeonRooms/Room_Boss_Template.prefab"
            });
        }

        private void AssignDefaultCorridorPrefabs(DungeonSettings settings)
        {
            settings.straightCorridorPrefabs = LoadPrefabArray(new[]
            {
                "Assets/Prefabs/DungeonCorridors/Corridor_Straight.prefab",
                "Assets/Prefabs/DungeonRooms/Corridor_Template.prefab"
            });

            settings.lCorridorPrefabs = LoadPrefabArray(new[]
            {
                "Assets/Prefabs/DungeonCorridors/Corridor_LCorner.prefab"
            });

            settings.tJunctionPrefabs = LoadPrefabArray(new[]
            {
                "Assets/Prefabs/DungeonCorridors/Corridor_TJunction.prefab"
            });

            settings.crossroadPrefabs = LoadPrefabArray(new[]
            {
                "Assets/Prefabs/DungeonCorridors/Corridor_Crossroads.prefab"
            });
        }

        private GameObject[] LoadPrefabArray(string[] candidatePaths)
        {
            var results = new List<GameObject>();
            foreach (string path in candidatePaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    results.Add(prefab);
                }
            }
            return results.ToArray();
        }

        private void EnsureDungeonSettingsFolderExists()
        {
            if (!Directory.Exists(Application.dataPath + "/ScriptableObjects"))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            if (!Directory.Exists(Application.dataPath + "/ScriptableObjects/Dungeons"))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Dungeons");
            }
        }

        #endregion

        #region Room Prefab Templates

        [MenuItem("DungeonDredge/Setup Tools/Create Room Prefab Templates")]
        public static void CreateRoomTemplatesMenu()
        {
            var window = GetWindow<DungeonSetupTool>("Dungeon Setup");
            window.CreateRoomPrefabTemplates();
        }

        private void CreateRoomPrefabTemplates()
        {
            string folderPath = "Assets/Prefabs/DungeonRooms";
            EnsureRoomFolderExists();

            int created = 0;

            // Create room templates with proper structure for FAB assets
            created += CreateRoomTemplate("Room_Portal_Template", RoomType.Portal, folderPath, 
                new Color(0.3f, 0.8f, 0.9f)) ? 1 : 0;  // Cyan - spawn/extract point
            created += CreateRoomTemplate("Room_Empty_Template", RoomType.Empty, folderPath, 
                new Color(0.5f, 0.5f, 0.5f)) ? 1 : 0;
            created += CreateRoomTemplate("Room_Loot_Template", RoomType.Loot, folderPath, 
                new Color(0.9f, 0.8f, 0.2f)) ? 1 : 0;
            created += CreateRoomTemplate("Room_Enemy_Template", RoomType.Enemy, folderPath, 
                new Color(0.8f, 0.3f, 0.3f)) ? 1 : 0;
            created += CreateRoomTemplate("Room_Boss_Template", RoomType.Boss, folderPath, 
                new Color(0.8f, 0.2f, 0.8f)) ? 1 : 0;  // Purple - boss room
            created += CreateCorridorTemplate("Corridor_Template", folderPath) ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Room Templates Created", 
                $"Created {created} room prefab templates!\n\n" +
                "These are structural templates. Add FAB dungeon meshes to customize them:\n\n" +
                "1. Open each prefab\n" +
                "2. Add wall/floor meshes from FAB assets\n" +
                "3. Add decorations (torches, props)\n" +
                "4. Save the prefab", "OK");
        }

        private bool CreateRoomTemplate(string name, RoomType type, string folderPath, Color gizmoColor)
        {
            string prefabPath = $"{folderPath}/{name}.prefab";
            
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"Room template already exists: {name}");
                return false;
            }

            // Room size: 16x16 units (in 24x24 grid cells, leaving 8-unit gaps for corridors)
            const float ROOM_SIZE = 16f;
            const float HALF_SIZE = ROOM_SIZE / 2f; // 8

            // Create root object
            GameObject root = new GameObject(name);

            // Add Room component
            Room room = root.AddComponent<Room>();

            // Create structure hierarchy
            GameObject structure = new GameObject("Structure");
            structure.transform.SetParent(root.transform);
            
            // Floor placeholder (replace with FAB floor) - 16x16 units
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor_Placeholder";
            floor.transform.SetParent(structure.transform);
            floor.transform.localScale = new Vector3(ROOM_SIZE / 10f, 1f, ROOM_SIZE / 10f); // Plane is 10 units, so 1.6 scale = 16 units
            floor.transform.localPosition = Vector3.zero;
            
            // Add NavMeshSurface if available
            var navMeshType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (navMeshType != null)
            {
                floor.AddComponent(navMeshType);
            }

            // Walls container
            GameObject walls = new GameObject("Walls");
            walls.transform.SetParent(structure.transform);

            // Create wall placeholders at room edges (8 units from center for 16x16 room)
            // Each wall has a doorway gap in the center (4 units wide)
            CreateWallWithDoorway(walls.transform, "Wall_North", new Vector3(0, 2, HALF_SIZE), ROOM_SIZE, true);
            CreateWallWithDoorway(walls.transform, "Wall_South", new Vector3(0, 2, -HALF_SIZE), ROOM_SIZE, true);
            CreateWallWithDoorway(walls.transform, "Wall_East", new Vector3(HALF_SIZE, 2, 0), ROOM_SIZE, false);
            CreateWallWithDoorway(walls.transform, "Wall_West", new Vector3(-HALF_SIZE, 2, 0), ROOM_SIZE, false);

            // Doors container (at room edges, will connect to corridors)
            GameObject doors = new GameObject("Doors");
            doors.transform.SetParent(root.transform);
            CreateDoorAttachPoint(doors.transform, "Door_North", new Vector3(0, 0, HALF_SIZE), Quaternion.identity);
            CreateDoorAttachPoint(doors.transform, "Door_South", new Vector3(0, 0, -HALF_SIZE), Quaternion.Euler(0, 180, 0));
            CreateDoorAttachPoint(doors.transform, "Door_East", new Vector3(HALF_SIZE, 0, 0), Quaternion.Euler(0, 90, 0));
            CreateDoorAttachPoint(doors.transform, "Door_West", new Vector3(-HALF_SIZE, 0, 0), Quaternion.Euler(0, -90, 0));

            // Spawn points container
            GameObject spawnPoints = new GameObject("SpawnPoints");
            spawnPoints.transform.SetParent(root.transform);

            // Create type-specific spawn points
            switch (type)
            {
                case RoomType.Portal:
                    // Portal room: player spawns AND extracts here
                    CreateSpawnPoint(spawnPoints.transform, "PlayerSpawnPoint", Vector3.up, Color.cyan);
                    CreateSpawnPoint(spawnPoints.transform, "PortalPoint", Vector3.zero, Color.cyan);
                    // Add portal/extraction trigger
                    GameObject portalTrigger = new GameObject("PortalTrigger");
                    portalTrigger.transform.SetParent(root.transform);
                    portalTrigger.transform.localPosition = Vector3.zero;
                    BoxCollider portalCollider = portalTrigger.AddComponent<BoxCollider>();
                    portalCollider.isTrigger = true;
                    portalCollider.size = new Vector3(4f, 3f, 4f);
                    portalTrigger.AddComponent<ExtractionPoint>(); // Reuse for portal
                    break;
                case RoomType.Loot:
                    CreateSpawnPoint(spawnPoints.transform, "LootSpawn_1", new Vector3(-5, 0, -5), Color.yellow);
                    CreateSpawnPoint(spawnPoints.transform, "LootSpawn_2", new Vector3(5, 0, -5), Color.yellow);
                    CreateSpawnPoint(spawnPoints.transform, "LootSpawn_3", new Vector3(0, 0, 5), Color.yellow);
                    break;
                case RoomType.Enemy:
                    CreateSpawnPoint(spawnPoints.transform, "EnemySpawn_1", new Vector3(-5, 0, 0), Color.red);
                    CreateSpawnPoint(spawnPoints.transform, "EnemySpawn_2", new Vector3(5, 0, 0), Color.red);
                    CreateSpawnPoint(spawnPoints.transform, "LootSpawn_1", new Vector3(0, 0, -5), Color.yellow);
                    break;
                case RoomType.Boss:
                    // Boss room: high-value loot and tough enemies
                    CreateSpawnPoint(spawnPoints.transform, "BossSpawn", Vector3.zero, Color.magenta);
                    CreateSpawnPoint(spawnPoints.transform, "LootSpawn_1", new Vector3(-5, 0, -5), Color.yellow);
                    CreateSpawnPoint(spawnPoints.transform, "LootSpawn_2", new Vector3(5, 0, -5), Color.yellow);
                    CreateSpawnPoint(spawnPoints.transform, "LootSpawn_3", new Vector3(-5, 0, 5), Color.yellow);
                    CreateSpawnPoint(spawnPoints.transform, "LootSpawn_4", new Vector3(5, 0, 5), Color.yellow);
                    break;
            }

            // Decorations container (for FAB props)
            GameObject decorations = new GameObject("Decorations");
            decorations.transform.SetParent(root.transform);

            // Lighting container
            GameObject lighting = new GameObject("Lighting");
            lighting.transform.SetParent(root.transform);
            
            // Add ambient light
            GameObject ambientLight = new GameObject("AmbientLight");
            ambientLight.transform.SetParent(lighting.transform);
            Light light = ambientLight.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 25f;
            light.intensity = 0.5f;
            light.color = new Color(1f, 0.9f, 0.7f);
            ambientLight.transform.localPosition = new Vector3(0, 4, 0);

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            Debug.Log($"Created room template: {name}");
            return true;
        }

        private void CreateWallPlaceholder(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name + "_Placeholder";
            wall.transform.SetParent(parent);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
            
            // Add a comment component to guide the user
            wall.tag = "Untagged"; // Will be replaced with FAB meshes
        }

        /// <summary>
        /// Create a wall with a doorway gap in the center.
        /// (Keep this small by default; corridor/doorway scale is handled by prefabs/settings.)
        /// </summary>
        private void CreateWallWithDoorway(Transform parent, string name, Vector3 centerPosition, float wallLength, bool isHorizontal)
        {
            const float WALL_HEIGHT = 4f;
            const float WALL_THICKNESS = 0.5f;
            const float DOORWAY_WIDTH = 4f;
            
            float sideWallLength = (wallLength - DOORWAY_WIDTH) / 2f;
            
            GameObject wallContainer = new GameObject(name);
            wallContainer.transform.SetParent(parent);
            wallContainer.transform.localPosition = centerPosition;

            if (isHorizontal)
            {
                // Left section
                GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftWall.name = "Left_Placeholder";
                leftWall.transform.SetParent(wallContainer.transform);
                leftWall.transform.localPosition = new Vector3(-(sideWallLength / 2 + DOORWAY_WIDTH / 2), 0, 0);
                leftWall.transform.localScale = new Vector3(sideWallLength, WALL_HEIGHT, WALL_THICKNESS);

                // Right section
                GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightWall.name = "Right_Placeholder";
                rightWall.transform.SetParent(wallContainer.transform);
                rightWall.transform.localPosition = new Vector3((sideWallLength / 2 + DOORWAY_WIDTH / 2), 0, 0);
                rightWall.transform.localScale = new Vector3(sideWallLength, WALL_HEIGHT, WALL_THICKNESS);
            }
            else
            {
                // Front section
                GameObject frontWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frontWall.name = "Front_Placeholder";
                frontWall.transform.SetParent(wallContainer.transform);
                frontWall.transform.localPosition = new Vector3(0, 0, -(sideWallLength / 2 + DOORWAY_WIDTH / 2));
                frontWall.transform.localScale = new Vector3(WALL_THICKNESS, WALL_HEIGHT, sideWallLength);

                // Back section
                GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                backWall.name = "Back_Placeholder";
                backWall.transform.SetParent(wallContainer.transform);
                backWall.transform.localPosition = new Vector3(0, 0, (sideWallLength / 2 + DOORWAY_WIDTH / 2));
                backWall.transform.localScale = new Vector3(WALL_THICKNESS, WALL_HEIGHT, sideWallLength);
            }
        }

        private void CreateDoorAttachPoint(Transform parent, string name, Vector3 position, Quaternion rotation)
        {
            const float DOORWAY_WIDTH = 4f;
            const float WALL_HEIGHT = 4f;
            const float WALL_THICKNESS = 0.5f;

            GameObject door = new GameObject(name);
            door.transform.SetParent(parent);
            door.transform.localPosition = position;
            door.transform.localRotation = rotation;
            
            // Add Door component
            Door doorComponent = door.AddComponent<Door>();
            
            // Door frame (archway visual - always visible when passage exists)
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "DoorFrame_Placeholder";
            frame.transform.SetParent(door.transform);
            frame.transform.localPosition = new Vector3(0, WALL_HEIGHT - 0.25f, 0);
            frame.transform.localScale = new Vector3(DOORWAY_WIDTH, 0.5f, WALL_THICKNESS); // Top of archway
            
            // Wall filler (shows when blocked - solid wall where doorway would be)
            GameObject wallFiller = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallFiller.name = "WallFiller";
            wallFiller.transform.SetParent(door.transform);
            wallFiller.transform.localPosition = new Vector3(0, WALL_HEIGHT / 2, 0);
            wallFiller.transform.localScale = new Vector3(DOORWAY_WIDTH, WALL_HEIGHT, WALL_THICKNESS);
            wallFiller.SetActive(false); // Starts hidden, shows when blocked
            
            // Set up door component references via SerializedObject
            var so = new SerializedObject(doorComponent);
            so.FindProperty("wallFiller").objectReferenceValue = wallFiller;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void CreateSpawnPoint(Transform parent, string name, Vector3 position, Color gizmoColor)
        {
            GameObject spawn = new GameObject(name);
            spawn.transform.SetParent(parent);
            spawn.transform.localPosition = position;
            
            // Add gizmo drawer
            var drawer = spawn.AddComponent<SpawnPointGizmo>();
            drawer.gizmoColor = gizmoColor;
        }

        private bool CreateCorridorTemplate(string name, string folderPath)
        {
            string prefabPath = $"{folderPath}/{name}.prefab";
            
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                return false;

            GameObject root = new GameObject(name);

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor_Placeholder";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(0.4f, 1f, 0.2f); // 4x2 units
            
            // Walls
            CreateWallPlaceholder(root.transform, "Wall_Left", new Vector3(-2, 1.5f, 0), new Vector3(0.3f, 3f, 2f));
            CreateWallPlaceholder(root.transform, "Wall_Right", new Vector3(2, 1.5f, 0), new Vector3(0.3f, 3f, 2f));

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            Debug.Log($"Created corridor template: {name}");
            return true;
        }

        private void EnsureRoomFolderExists()
        {
            if (!Directory.Exists(Application.dataPath + "/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!Directory.Exists(Application.dataPath + "/Prefabs/DungeonRooms"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "DungeonRooms");
            }
        }

        #endregion

        #region Corridor Templates

        [MenuItem("DungeonDredge/Setup Tools/Create All Corridor Templates")]
        public static void CreateAllCorridorsMenu()
        {
            var window = GetWindow<DungeonSetupTool>("Dungeon Setup");
            window.CreateAllCorridorTemplates();
        }

        private void CreateAllCorridorTemplates()
        {
            EnsureCorridorFolderExists();

            int created = 0;
            created += CreateStraightCorridorTemplate() ? 1 : 0;
            created += CreateLCorridorTemplate() ? 1 : 0;
            created += CreateTJunctionTemplate() ? 1 : 0;
            created += CreateCrossroadsTemplate() ? 1 : 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Corridor Templates Created",
                $"Created {created} corridor prefab templates!\n\n" +
                "These are structural templates. Customize them:\n\n" +
                "1. Open each prefab\n" +
                "2. Replace placeholder geometry with FAB meshes\n" +
                "3. Add floor details, wall decorations\n" +
                "4. Add lighting (torches, etc.)", "OK");
        }

        private bool CreateStraightCorridorTemplate()
        {
            string folderPath = "Assets/Prefabs/DungeonCorridors";
            string prefabPath = $"{folderPath}/Corridor_Straight.prefab";
            
            EnsureCorridorFolderExists();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log("Straight corridor template already exists");
                return false;
            }

            // Straight corridor: 8x8 units, opens N and S (or rotate 90° for E-W)
            const float SIZE = 8f;
            const float HALF = SIZE / 2f;
            const float WALL_HEIGHT = 4f;
            
            GameObject root = new GameObject("Corridor_Straight");

            // Floor (8x8 units)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(SIZE / 10f, 1f, SIZE / 10f);
            floor.transform.localPosition = Vector3.zero;

            // Walls on the sides (E and W closed, N and S open)
            CreateCorridorWall(root.transform, "Wall_East", new Vector3(HALF, WALL_HEIGHT / 2f, 0f), new Vector3(0.3f, WALL_HEIGHT, SIZE));
            CreateCorridorWall(root.transform, "Wall_West", new Vector3(-HALF, WALL_HEIGHT / 2f, 0f), new Vector3(0.3f, WALL_HEIGHT, SIZE));

            // Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(root.transform);
            ceiling.transform.localScale = new Vector3(SIZE / 10f, 1f, SIZE / 10f);
            ceiling.transform.localPosition = new Vector3(0f, WALL_HEIGHT, 0f);
            ceiling.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);

            // Add point light
            CreateCorridorLight(root.transform, Vector3.up * (WALL_HEIGHT - 0.5f));

            ApplyCorridorMaterial(root, new Color(0.4f, 0.4f, 0.45f));
            
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            Debug.Log("Created Straight corridor template (8x8)");
            return true;
        }

        private bool CreateLCorridorTemplate()
        {
            string folderPath = "Assets/Prefabs/DungeonCorridors";
            string prefabPath = $"{folderPath}/Corridor_LCorner.prefab";
            
            EnsureCorridorFolderExists();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log("L-Corner corridor template already exists");
                return false;
            }

            // L-Corner: 8x8 units, opens N and E (rotate for other orientations)
            const float SIZE = 8f;
            const float HALF = SIZE / 2f;
            const float WALL_HEIGHT = 4f;
            
            GameObject root = new GameObject("Corridor_LCorner");

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(SIZE / 10f, 1f, SIZE / 10f);
            floor.transform.localPosition = Vector3.zero;

            // Walls: S and W are closed, N and E are open
            CreateCorridorWall(root.transform, "Wall_South", new Vector3(0f, WALL_HEIGHT / 2f, -HALF), new Vector3(SIZE, WALL_HEIGHT, 0.3f));
            CreateCorridorWall(root.transform, "Wall_West", new Vector3(-HALF, WALL_HEIGHT / 2f, 0f), new Vector3(0.3f, WALL_HEIGHT, SIZE));

            // Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(root.transform);
            ceiling.transform.localScale = new Vector3(SIZE / 10f, 1f, SIZE / 10f);
            ceiling.transform.localPosition = new Vector3(0f, WALL_HEIGHT, 0f);
            ceiling.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);

            // Light
            CreateCorridorLight(root.transform, Vector3.up * (WALL_HEIGHT - 0.5f));

            ApplyCorridorMaterial(root, new Color(0.4f, 0.4f, 0.45f));

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            Debug.Log("Created L-Corner corridor template (8x8)");
            return true;
        }

        private bool CreateTJunctionTemplate()
        {
            string folderPath = "Assets/Prefabs/DungeonCorridors";
            string prefabPath = $"{folderPath}/Corridor_TJunction.prefab";
            
            EnsureCorridorFolderExists();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log("T-Junction corridor template already exists");
                return false;
            }

            // T-Junction: 8x8 units, opens N, E, W (S is closed). Rotate for other orientations.
            const float SIZE = 8f;
            const float HALF = SIZE / 2f;
            const float WALL_HEIGHT = 4f;
            
            GameObject root = new GameObject("Corridor_TJunction");

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(SIZE / 10f, 1f, SIZE / 10f);
            floor.transform.localPosition = Vector3.zero;

            // Only S wall is closed
            CreateCorridorWall(root.transform, "Wall_South", new Vector3(0f, WALL_HEIGHT / 2f, -HALF), new Vector3(SIZE, WALL_HEIGHT, 0.3f));

            // Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(root.transform);
            ceiling.transform.localScale = new Vector3(SIZE / 10f, 1f, SIZE / 10f);
            ceiling.transform.localPosition = new Vector3(0f, WALL_HEIGHT, 0f);
            ceiling.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);

            // Light
            CreateCorridorLight(root.transform, Vector3.up * (WALL_HEIGHT - 0.5f));

            ApplyCorridorMaterial(root, new Color(0.4f, 0.4f, 0.45f));

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            Debug.Log("Created T-Junction corridor template (8x8)");
            return true;
        }

        private bool CreateCrossroadsTemplate()
        {
            string folderPath = "Assets/Prefabs/DungeonCorridors";
            string prefabPath = $"{folderPath}/Corridor_Crossroads.prefab";
            
            EnsureCorridorFolderExists();

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log("Crossroads corridor template already exists");
                return false;
            }

            // Crossroads: 8x8 units, all 4 directions open
            const float SIZE = 8f;
            const float HALF = SIZE / 2f;
            const float WALL_HEIGHT = 4f;
            const float PILLAR_OFFSET = HALF - 0.5f; // Pillars at corners
            
            GameObject root = new GameObject("Corridor_Crossroads");

            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.localScale = new Vector3(SIZE / 10f, 1f, SIZE / 10f);
            floor.transform.localPosition = Vector3.zero;

            // No walls - all directions open
            // Corner pillars for structural integrity
            CreateCorridorPillar(root.transform, "Pillar_NE", new Vector3(PILLAR_OFFSET, WALL_HEIGHT / 2f, PILLAR_OFFSET));
            CreateCorridorPillar(root.transform, "Pillar_NW", new Vector3(-PILLAR_OFFSET, WALL_HEIGHT / 2f, PILLAR_OFFSET));
            CreateCorridorPillar(root.transform, "Pillar_SE", new Vector3(PILLAR_OFFSET, WALL_HEIGHT / 2f, -PILLAR_OFFSET));
            CreateCorridorPillar(root.transform, "Pillar_SW", new Vector3(-PILLAR_OFFSET, WALL_HEIGHT / 2f, -PILLAR_OFFSET));

            // Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(root.transform);
            ceiling.transform.localScale = new Vector3(SIZE / 10f, 1f, SIZE / 10f);
            ceiling.transform.localPosition = new Vector3(0f, WALL_HEIGHT, 0f);
            ceiling.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);

            // Light (brighter for crossroads)
            CreateCorridorLight(root.transform, Vector3.up * (WALL_HEIGHT - 0.5f), 1.5f);

            ApplyCorridorMaterial(root, new Color(0.4f, 0.4f, 0.45f));

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            Debug.Log("Created Crossroads corridor template (8x8)");
            return true;
        }

        private void CreateCorridorWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;
        }

        private void CreateCorridorPillar(Transform parent, string name, Vector3 position)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = name;
            pillar.transform.SetParent(parent);
            pillar.transform.localPosition = position;
            pillar.transform.localScale = new Vector3(0.4f, 3f, 0.4f);
        }

        private void CreateCorridorLight(Transform parent, Vector3 position, float intensity = 0.8f)
        {
            GameObject lightObj = new GameObject("Light");
            lightObj.transform.SetParent(parent);
            lightObj.transform.localPosition = position;

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 8f;
            light.intensity = intensity;
            light.color = new Color(1f, 0.85f, 0.6f); // Warm torch color
        }

        private void ApplyCorridorMaterial(GameObject root, Color color)
        {
            string folderPath = "Assets/Prefabs/DungeonCorridors";
            string matPath = $"{folderPath}/Corridor_Material.mat";
            
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null) mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                AssetDatabase.CreateAsset(mat, matPath);
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = mat;
            }
        }

        private void EnsureCorridorFolderExists()
        {
            if (!Directory.Exists(Application.dataPath + "/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!Directory.Exists(Application.dataPath + "/Prefabs/DungeonCorridors"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "DungeonCorridors");
            }
        }

        #endregion

        private void FindOrCreateItemDatabase()
        {
            // Try to find existing
            string[] guids = AssetDatabase.FindAssets("t:ItemDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                itemDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);
                Debug.Log($"Found existing ItemDatabase at {path}");
            }
            else
            {
                // Create new
                itemDatabase = CreateInstance<ItemDatabase>();
                string path = "Assets/Resources/ItemDatabase.asset";
                
                // Ensure Resources folder exists
                if (!Directory.Exists(Application.dataPath + "/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                AssetDatabase.CreateAsset(itemDatabase, path);
                Debug.Log($"Created new ItemDatabase at {path}");
            }

            // Populate
            if (itemDatabase != null)
            {
                // Use reflection or SerializedObject to access the private list if needed, 
                // but for now let's try to assume the context menu "Find All Items" works or we call it via reflection.
                // Or just manually add found items.
                
                var items = new List<ItemData>();
                string[] itemGuids = AssetDatabase.FindAssets("t:ItemData");
                foreach (var guid in itemGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                    if (item != null) items.Add(item);
                }

                SerializedObject so = new SerializedObject(itemDatabase);
                SerializedProperty itemsProp = so.FindProperty("items");
                itemsProp.ClearArray();
                
                for (int i = 0; i < items.Count; i++)
                {
                    itemsProp.InsertArrayElementAtIndex(i);
                    itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                }
                so.ApplyModifiedProperties();
                
                Debug.Log($"Populated ItemDatabase with {items.Count} items.");
            }
        }

        private void CreatePlaceholderRooms()
        {
            string folderPath = "Assets/Prefabs/DungeonRooms";
            if (!Directory.Exists(Application.dataPath + "/Prefabs/DungeonRooms"))
            {
                if (!Directory.Exists(Application.dataPath + "/Prefabs"))
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                AssetDatabase.CreateFolder("Assets/Prefabs", "DungeonRooms");
            }

            // Create types - 16x16 rooms with 8-unit corridor gaps
            CreateRoomPrefab("Room_Portal", RoomType.Portal, Color.cyan, folderPath);
            CreateRoomPrefab("Room_Empty", RoomType.Empty, Color.gray, folderPath);
            CreateRoomPrefab("Room_Loot", RoomType.Loot, Color.yellow, folderPath);
            CreateRoomPrefab("Room_Enemy", RoomType.Enemy, Color.red, folderPath);
            CreateRoomPrefab("Room_Boss", RoomType.Boss, Color.magenta, folderPath);

            Debug.Log("Created placeholder room prefabs (16x16 with doorways).");
            EditorUtility.DisplayDialog("Rooms Created",
                "Created 16x16 room prefabs with 8-unit doorway openings.\n\n" +
                "These will connect via 8x8 corridor segments.\n" +
                "Grid spacing is 24 units, so there's an 8-unit gap for corridors.",
                "OK");
        }

        private void CreateRoomPrefab(string name, RoomType type, Color color, string path)
        {
            // Room is 16x16 units (in a 24x24 grid cell, leaving 8-unit gaps)
            const float ROOM_SIZE = 16f;
            const float HALF_SIZE = ROOM_SIZE / 2f;
            
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = name;
            go.transform.localScale = new Vector3(ROOM_SIZE / 10f, 1f, ROOM_SIZE / 10f); // 16x16 units

            // Add walls with doorway gaps
            CreateWallWithDoorway(go.transform, "Wall_North", new Vector3(0, 2, HALF_SIZE), ROOM_SIZE, true);
            CreateWallWithDoorway(go.transform, "Wall_South", new Vector3(0, 2, -HALF_SIZE), ROOM_SIZE, true);
            CreateWallWithDoorway(go.transform, "Wall_East", new Vector3(HALF_SIZE, 2, 0), ROOM_SIZE, false);
            CreateWallWithDoorway(go.transform, "Wall_West", new Vector3(-HALF_SIZE, 2, 0), ROOM_SIZE, false);

            // Add doors container
            GameObject doors = new GameObject("Doors");
            doors.transform.SetParent(go.transform);
            CreateDoorAttachPoint(doors.transform, "Door_North", new Vector3(0, 0, HALF_SIZE), Quaternion.identity);
            CreateDoorAttachPoint(doors.transform, "Door_South", new Vector3(0, 0, -HALF_SIZE), Quaternion.Euler(0, 180, 0));
            CreateDoorAttachPoint(doors.transform, "Door_East", new Vector3(HALF_SIZE, 0, 0), Quaternion.Euler(0, 90, 0));
            CreateDoorAttachPoint(doors.transform, "Door_West", new Vector3(-HALF_SIZE, 0, 0), Quaternion.Euler(0, -90, 0));

            // Add Room component
            Room room = go.AddComponent<Room>();

            // Add material color
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
                    mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.sharedMaterial = mat;
                
                string matPath = $"{path}/{name}_Mat.mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null)
                    AssetDatabase.CreateAsset(mat, matPath);
            }

            // Apply material to walls too
            foreach (var childRenderer in go.GetComponentsInChildren<Renderer>())
            {
                if (childRenderer != renderer)
                {
                    childRenderer.sharedMaterial = renderer.sharedMaterial;
                }
            }

            string prefabPath = $"{path}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            DestroyImmediate(go);
        }


        private void SetupScene()
        {
            // Find or create DungeonManager
            var dm = FindObjectOfType<DungeonManager>();
            if (dm == null)
            {
                GameObject go = new GameObject("DungeonManager");
                dm = go.AddComponent<DungeonManager>();
            }

            // Find generator
            var gen = dm.GetComponent<DungeonGenerator>();
            if (gen == null) gen = dm.gameObject.AddComponent<DungeonGenerator>();

            // Assign ItemDatabase using SerializedObject
            if (itemDatabase != null)
            {
                SerializedObject so = new SerializedObject(dm);
                SerializedProperty dbProp = so.FindProperty("itemDatabase");
                dbProp.objectReferenceValue = itemDatabase;
                so.ApplyModifiedProperties();
                
                SerializedObject soGen = new SerializedObject(gen);
                SerializedProperty dbPropGen = soGen.FindProperty("itemDatabase");
                dbPropGen.objectReferenceValue = itemDatabase;
                
                // Assign room prefabs
                string roomPath = "Assets/Prefabs/DungeonRooms";
                GameObject portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{roomPath}/Room_Portal.prefab");
                GameObject lootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{roomPath}/Room_Loot.prefab");
                GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{roomPath}/Room_Enemy.prefab");
                GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{roomPath}/Room_Boss.prefab");

                soGen.FindProperty("defaultRoomPrefab").objectReferenceValue = portalPrefab; // Fallback
                soGen.FindProperty("portalRoomPrefab").objectReferenceValue = portalPrefab;
                soGen.FindProperty("lootRoomPrefab").objectReferenceValue = lootPrefab;
                soGen.FindProperty("enemyRoomPrefab").objectReferenceValue = enemyPrefab;
                soGen.FindProperty("bossRoomPrefab").objectReferenceValue = bossPrefab;

                // Assign corridor prefabs
                string corridorPath = "Assets/Prefabs/DungeonCorridors";
                GameObject straightCorridor = AssetDatabase.LoadAssetAtPath<GameObject>($"{corridorPath}/Corridor_Straight.prefab");
                GameObject lCorridor = AssetDatabase.LoadAssetAtPath<GameObject>($"{corridorPath}/Corridor_LCorner.prefab");
                GameObject tJunction = AssetDatabase.LoadAssetAtPath<GameObject>($"{corridorPath}/Corridor_TJunction.prefab");
                GameObject crossroads = AssetDatabase.LoadAssetAtPath<GameObject>($"{corridorPath}/Corridor_Crossroads.prefab");

                soGen.FindProperty("straightCorridorPrefab").objectReferenceValue = straightCorridor;
                soGen.FindProperty("lCorridorPrefab").objectReferenceValue = lCorridor;
                soGen.FindProperty("tJunctionPrefab").objectReferenceValue = tJunction;
                soGen.FindProperty("crossroadPrefab").objectReferenceValue = crossroads;

                soGen.ApplyModifiedProperties();
            }
            
            Debug.Log("Dungeon Manager setup complete.");
        }
    }
}
