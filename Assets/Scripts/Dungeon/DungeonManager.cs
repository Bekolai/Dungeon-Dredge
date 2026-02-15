using UnityEngine;
using DungeonDredge.Core;
using DungeonDredge.Inventory;
using DungeonDredge.Player;
using DungeonDredge.Tools;

namespace DungeonDredge.Dungeon
{
    public class DungeonManager : MonoBehaviour
    {
        public static DungeonManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private DungeonGenerator generator;
        [SerializeField] private ItemDatabase itemDatabase;

        [Header("Current Dungeon")]
        [SerializeField] private DungeonRank currentRank = DungeonRank.F;

        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private BackpackData startingBackpack;
        [SerializeField] private BackpackDatabase backpackDatabase;

        // State
        private GameObject currentPlayer;
        private bool dungeonActive = false;

        // Properties
        public DungeonRank CurrentRank => currentRank;
        public bool IsDungeonActive => dungeonActive;
        public DungeonGenerator Generator => generator;
        public GameObject CurrentPlayer => currentPlayer;

        // Events
        public System.Action<DungeonRank> OnDungeonStarted;
        public System.Action<bool> OnDungeonEnded; // bool = extracted successfully

        /// <summary>
        /// Fired after the player is spawned/found and all components are ensured.
        /// UI scripts should subscribe to this to find player references.
        /// </summary>
        public static event System.Action<GameObject> OnPlayerSpawned;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (generator == null)
            {
                generator = GetComponent<DungeonGenerator>();
            }
        }

        private void Start()
        {
            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDied += OnPlayerDied;
                GameManager.Instance.OnPlayerExtracted += OnPlayerExtracted;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDied -= OnPlayerDied;
                GameManager.Instance.OnPlayerExtracted -= OnPlayerExtracted;
            }
        }

        public void StartDungeon(DungeonRank rank)
        {
            if (dungeonActive)
            {
                Debug.LogWarning("Dungeon already active!");
                return;
            }

            currentRank = rank;
            dungeonActive = true;

            // Generate dungeon
            generator.GenerateDungeon(rank);

            // Spawn player
            SpawnPlayer();

            // Set game state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetGameState(GameState.Dungeon);
            }

            OnDungeonStarted?.Invoke(rank);
        }

        public void EndDungeon(bool extracted)
        {
            if (!dungeonActive) return;

            dungeonActive = false;

            // Calculate rewards if extracted
            if (extracted && currentPlayer != null)
            {
                var inventory = currentPlayer.GetComponent<PlayerInventory>();
                var stats = currentPlayer.GetComponent<PlayerStats>();

                if (inventory != null && stats != null)
                {
                    int totalGold = inventory.Grid.GetTotalValue();
                    bool hasRare = inventory.Grid.HasRareItems();
                    
                    // Award perception XP
                    stats.OnExtraction(totalGold, hasRare);

                    // Publish event
                    EventBus.Publish(new ExtractionCompletedEvent
                    {
                        TotalGold = totalGold,
                        ItemCount = inventory.Grid.Items.Count
                    });
                }
            }
            else
            {
                // Player died - clear inventory
                if (currentPlayer != null)
                {
                    var inventory = currentPlayer.GetComponent<PlayerInventory>();
                    inventory?.Grid.ClearAll();
                }
            }

            // Clean up
            generator.ClearDungeon();

            // Save progress
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.Save();
            }

            // Return to village
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadVillage();
            }

            OnDungeonEnded?.Invoke(extracted);
        }

        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("No player prefab assigned!");
                return;
            }

            Vector3 spawnPos = generator.GetSpawnPosition();

            // Check if player already exists
            currentPlayer = GameObject.FindGameObjectWithTag("Player");
            
            if (currentPlayer == null)
            {
                currentPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Move existing player
                CharacterController cc = currentPlayer.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = false;
                    currentPlayer.transform.position = spawnPos;
                    cc.enabled = true;
                }
                else
                {
                    currentPlayer.transform.position = spawnPos;
                }
            }

            // Ensure the player has all required components
            EnsurePlayerComponents();

            // Notify UI and other systems that the player is ready
            OnPlayerSpawned?.Invoke(currentPlayer);
        }

        /// <summary>
        /// Ensures the player has PlayerInventory, PlayerStats, and ToolManager.
        /// Adds them at runtime if missing from the prefab.
        /// </summary>
        private void EnsurePlayerComponents()
        {
            if (currentPlayer == null) return;

            // PlayerStats
            var stats = currentPlayer.GetComponent<PlayerStats>();
            if (stats == null)
            {
                stats = currentPlayer.AddComponent<PlayerStats>();
                Debug.Log("[DungeonManager] Added PlayerStats to player at runtime.");
            }

            // PlayerInventory (needs InventoryGrid as a child)
            var inventory = currentPlayer.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                // Create InventoryGrid child object first (so PlayerInventory.Awake finds it)
                var gridGO = new GameObject("InventoryGrid");
                gridGO.transform.SetParent(currentPlayer.transform, false);
                var grid = gridGO.AddComponent<InventoryGrid>();

                inventory = currentPlayer.AddComponent<PlayerInventory>();
                Debug.Log("[DungeonManager] Added PlayerInventory to player at runtime.");
            }

            // Ensure the grid reference is set (PlayerInventory.Awake should find it,
            // but if it was already on the prefab without the grid wired, fix it here)
            if (inventory.Grid == null)
            {
                var grid = currentPlayer.GetComponentInChildren<InventoryGrid>();
                if (grid == null)
                {
                    var gridGO = new GameObject("InventoryGrid");
                    gridGO.transform.SetParent(currentPlayer.transform, false);
                    grid = gridGO.AddComponent<InventoryGrid>();
                }
                // Use reflection to set the private serialized field
                var field = typeof(PlayerInventory).GetField("inventoryGrid",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(inventory, grid);
                Debug.Log("[DungeonManager] Wired InventoryGrid reference on PlayerInventory.");
            }

            // ToolManager
            var toolManager = currentPlayer.GetComponentInChildren<ToolManager>();
            if (toolManager == null)
            {
                var toolGO = new GameObject("ToolManager");
                toolGO.transform.SetParent(currentPlayer.transform, false);
                toolManager = toolGO.AddComponent<ToolManager>();
                Debug.Log("[DungeonManager] Added ToolManager to player at runtime.");
            }
        }

        private void OnPlayerDied()
        {
            EndDungeon(false);
        }

        private void OnPlayerExtracted()
        {
            EndDungeon(true);
        }

        #region Quick Actions

        /// <summary>
        /// Quick start a dungeon for testing
        /// </summary>
        [ContextMenu("Start F-Rank Dungeon")]
        public void QuickStartFRank()
        {
            StartDungeon(DungeonRank.F);
        }

        [ContextMenu("Start E-Rank Dungeon")]
        public void QuickStartERank()
        {
            StartDungeon(DungeonRank.E);
        }

        [ContextMenu("Start D-Rank Dungeon")]
        public void QuickStartDRank()
        {
            StartDungeon(DungeonRank.D);
        }

        #endregion
    }
}
