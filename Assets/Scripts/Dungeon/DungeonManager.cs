using UnityEngine;
using DungeonDredge.Core;
using DungeonDredge.Inventory;
using DungeonDredge.Player;

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

        // State
        private GameObject currentPlayer;
        private bool dungeonActive = false;

        // Properties
        public DungeonRank CurrentRank => currentRank;
        public bool IsDungeonActive => dungeonActive;
        public DungeonGenerator Generator => generator;

        // Events
        public System.Action<DungeonRank> OnDungeonStarted;
        public System.Action<bool> OnDungeonEnded; // bool = extracted successfully

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
