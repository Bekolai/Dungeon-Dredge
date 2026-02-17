using UnityEngine;
using UnityEngine.InputSystem;
using DungeonDredge.Core;
using DungeonDredge.Player;

namespace DungeonDredge.Inventory
{
    public class PlayerInventory : MonoBehaviour
    {
        private const float MaxAllowedWeightRatioForPickup = 1.4f;

        [Header("References")]
        [SerializeField] private BackpackData startingBackpack;
        [SerializeField] private BackpackDatabase backpackDatabase;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private Transform dropPoint;

        [Header("Backpack")]
        [SerializeField] private InventoryGrid inventoryGrid;
        [SerializeField] private GameObject droppedBackpackPrefab;

        [Header("Input")]
        [SerializeField] private InputActionReference dropBackpackAction;
        [SerializeField] private InputActionReference openInventoryAction;
        [Tooltip("Fallback key for inventory toggle if no InputActionReference is assigned")]
        [SerializeField] private Key inventoryFallbackKey = Key.Tab;

        // State
        private BackpackData currentBackpack;
        private bool inventoryOpen = false;
        private GameObject droppedBackpack;

        // References
        private PlayerMovement playerMovement;
        private PlayerStats playerStats;
        private PlayerController playerController;

        // Properties
        public InventoryGrid Grid => inventoryGrid;
        public BackpackData CurrentBackpack => currentBackpack;
        public bool IsInventoryOpen => inventoryOpen;
        public bool HasDroppedBackpack => droppedBackpack != null;

        // Events
        public System.Action OnInventoryOpened;
        public System.Action OnInventoryClosed;
        public System.Action OnBackpackDropped;
        public System.Action OnBackpackPickedUp;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            playerMovement = GetComponent<PlayerMovement>();
            playerStats = GetComponent<PlayerStats>();

            if (inventoryGrid == null)
            {
                inventoryGrid = GetComponentInChildren<InventoryGrid>();
            }

            if (dropPoint == null)
            {
                dropPoint = transform;
            }
        }

        private void Start()
        {
            // Initialize with starting backpack
            BackpackData initialBackpack = startingBackpack;
            if (initialBackpack == null && backpackDatabase != null)
            {
                initialBackpack = backpackDatabase.GetStartingBackpack();
            }

            if (initialBackpack != null)
            {
                EquipBackpack(initialBackpack);
            }

            // Subscribe to weight changes
            if (inventoryGrid != null)
            {
                inventoryGrid.OnWeightChanged += OnWeightChanged;
            }
        }

        private void OnEnable()
        {
            if (dropBackpackAction != null)
            {
                dropBackpackAction.action.Enable();
                dropBackpackAction.action.performed += OnDropBackpack;
            }

            if (openInventoryAction != null)
            {
                openInventoryAction.action.Enable();
                openInventoryAction.action.performed += OnToggleInventory;
            }
        }

        private void OnDisable()
        {
            if (dropBackpackAction != null)
            {
                dropBackpackAction.action.performed -= OnDropBackpack;
            }

            if (openInventoryAction != null)
            {
                openInventoryAction.action.performed -= OnToggleInventory;
            }

            if (inventoryGrid != null)
            {
                inventoryGrid.OnWeightChanged -= OnWeightChanged;
            }
        }

        private void Update()
        {
            // Fallback keyboard toggle if no InputActionReference assigned
            if (openInventoryAction == null || openInventoryAction.action == null)
            {
                if (Keyboard.current != null && Keyboard.current[inventoryFallbackKey].wasPressedThisFrame)
                {
                    if (inventoryOpen)
                        CloseInventory();
                    else
                        OpenInventory();
                }
            }

            // Always allow Escape to close inventory
            if (inventoryOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseInventory();
            }
        }

        public void EquipBackpack(BackpackData backpack)
        {
            currentBackpack = backpack;
            
            if (inventoryGrid != null)
            {
                inventoryGrid.ResizeGrid(backpack.gridWidth, backpack.gridHeight);
            }
        }

        public void UpgradeBackpack()
        {
            if (currentBackpack?.nextUpgrade != null)
            {
                EquipBackpack(currentBackpack.nextUpgrade);
            }
        }

        private void OnWeightChanged(float weight)
        {
            // Update player movement with weight ratio
            if (playerMovement != null && playerStats != null)
            {
                float capacity = playerStats.WeightCapacity;
                inventoryGrid.SetWeightCapacity(capacity);
                playerMovement.SetWeightRatio(weight / capacity);
            }
        }

        #region Backpack Drop/Pickup

        private void OnDropBackpack(InputAction.CallbackContext context)
        {
            if (HasDroppedBackpack)
            {
                TryPickupBackpack();
            }
            else
            {
                DropBackpack();
            }
        }

        public void DropBackpack()
        {
            if (currentBackpack == null || inventoryGrid == null) return;
            if (HasDroppedBackpack) return; // Can only drop one

            DungeonDredge.Audio.AudioManager.Instance?.PlayItemDrop();

            // Create dropped backpack in world
            Vector3 dropPosition = dropPoint.position + dropPoint.forward * 0.5f;
            
            if (droppedBackpackPrefab != null)
            {
                droppedBackpack = Instantiate(droppedBackpackPrefab, dropPosition, Quaternion.identity);
                
                // Transfer inventory data to dropped backpack
                var droppedInventory = droppedBackpack.GetComponent<DroppedBackpack>();
                if (droppedInventory != null)
                {
                    droppedInventory.Initialize(inventoryGrid.GetSaveData(), currentBackpack);
                }
            }

            // Clear player inventory but keep backpack equipped
            inventoryGrid.ClearAll();

            // Generate noise
            EventBus.Publish(new NoiseEvent
            {
                Position = dropPosition,
                Intensity = 1.0f,
                Source = gameObject
            });

            // Update weight (now at 0)
            OnWeightChanged(0);

            OnBackpackDropped?.Invoke();
        }

        public void TryPickupBackpack()
        {
            if (!HasDroppedBackpack) return;

            // Check distance
            float distance = Vector3.Distance(transform.position, droppedBackpack.transform.position);
            if (distance > 3f)
            {
                Debug.Log("Too far from backpack");
                return;
            }

            DungeonDredge.Audio.AudioManager.Instance?.PlayItemPickup();

            // Get dropped backpack data
            var droppedInventory = droppedBackpack.GetComponent<DroppedBackpack>();
            if (droppedInventory != null)
            {
                // Restore inventory
                inventoryGrid.LoadSaveData(droppedInventory.SavedInventory, itemDatabase);
            }

            // Destroy dropped backpack
            Destroy(droppedBackpack);
            droppedBackpack = null;

            OnBackpackPickedUp?.Invoke();
        }

        #endregion

        #region Inventory UI

        private void OnToggleInventory(InputAction.CallbackContext context)
        {
            if (inventoryOpen)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }

        public void OpenInventory()
        {
            if (inventoryOpen) return;

            inventoryOpen = true;
            
            // Play UI Sound
            DungeonDredge.Audio.AudioManager.Instance?.PlayMenuOpen();

            // Unlock cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Disable player movement and look
            if (playerController != null)
                playerController.SetInputEnabled(false);

            OnInventoryOpened?.Invoke();
        }

        public void CloseInventory()
        {
            if (!inventoryOpen) return;

            inventoryOpen = false;

            // Play UI Sound
            DungeonDredge.Audio.AudioManager.Instance?.PlayMenuClose();

            // Lock cursor back for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Re-enable player movement and look
            if (playerController != null)
                playerController.SetInputEnabled(true);

            OnInventoryClosed?.Invoke();
        }

        #endregion

        #region Item Operations

        public bool TryPickupItem(ItemData itemData)
        {
            if (inventoryGrid == null)
            {
                // Last-resort attempt to find the grid
                inventoryGrid = GetComponentInChildren<InventoryGrid>();
                if (inventoryGrid == null)
                {
                    Debug.LogWarning("[PlayerInventory] Cannot pick up item - no InventoryGrid found!");
                    return false;
                }
            }
            if (itemData == null) return false;

            if (!CanPickupByWeight(itemData))
            {
                float capacity = GetCurrentWeightCapacity();
                float projectedWeight = inventoryGrid.CurrentWeight + itemData.weight;
                EventBus.Publish(new InventoryFeedbackEvent
                {
                    Message = $"Too overloaded to pick up {itemData.itemName} ({projectedWeight:F1}/{capacity:F1}kg)",
                    Duration = 1.5f,
                    IsWarning = true
                });
                return false;
            }

            InventoryItem item = new InventoryItem(itemData);
            bool added = inventoryGrid.TryAddItemAuto(item);
            if (added)
            {
                DungeonDredge.Audio.AudioManager.Instance?.PlayItemPickup();
            }
            else
            {
                EventBus.Publish(new InventoryFeedbackEvent
                {
                    Message = "No room in backpack",
                    Duration = 1.2f,
                    IsWarning = true
                });
            }
            return added;
        }

        public bool TryPickupItem(InventoryItem item)
        {
            if (inventoryGrid == null || item == null) return false;
            bool added = inventoryGrid.TryAddItemAuto(item);
            if (added)
            {
                DungeonDredge.Audio.AudioManager.Instance?.PlayItemPickup();
            }
            return added;
        }

        public void DropItem(InventoryItem item)
        {
            if (inventoryGrid == null || item == null) return;

            if (inventoryGrid.RemoveItem(item))
            {
                DungeonDredge.Audio.AudioManager.Instance?.PlayItemDrop();

                // Spawn item in world
                if (item.itemData.worldPrefab != null)
                {
                    Vector3 dropPos = dropPoint.position + dropPoint.forward;
                    GameObject dropped = Instantiate(item.itemData.worldPrefab, dropPos, Quaternion.identity);
                    var worldItem = dropped.GetComponent<WorldItem>() ?? dropped.GetComponentInChildren<WorldItem>();
                    worldItem?.SetItemData(item.itemData);
                }
            }
        }

        #endregion

        #region Save/Load

        public PlayerInventorySaveData GetSaveData()
        {
            return new PlayerInventorySaveData
            {
                backpackId = currentBackpack?.backpackId,
                inventory = inventoryGrid?.GetSaveData()
            };
        }

        public void LoadSaveData(PlayerInventorySaveData saveData, BackpackDatabase backpackDb)
        {
            if (saveData == null) return;

            // Load backpack
            if (!string.IsNullOrEmpty(saveData.backpackId) && backpackDb != null)
            {
                BackpackData backpack = backpackDb.GetBackpack(saveData.backpackId);
                if (backpack != null)
                {
                    EquipBackpack(backpack);
                }
            }

            // Load inventory
            if (saveData.inventory != null && inventoryGrid != null && itemDatabase != null)
            {
                inventoryGrid.LoadSaveData(saveData.inventory, itemDatabase);
            }
        }

        #endregion

        private bool CanPickupByWeight(ItemData itemData)
        {
            float capacity = Mathf.Max(0.1f, GetCurrentWeightCapacity());
            float projectedRatio = (inventoryGrid.CurrentWeight + itemData.weight) / capacity;
            return projectedRatio <= MaxAllowedWeightRatioForPickup;
        }

        private float GetCurrentWeightCapacity()
        {
            if (playerStats != null)
                return Mathf.Max(0.1f, playerStats.WeightCapacity);
            return Mathf.Max(0.1f, inventoryGrid.MaxWeight);
        }
    }

    [System.Serializable]
    public class PlayerInventorySaveData
    {
        public string backpackId;
        public InventorySaveData inventory;
    }
}
