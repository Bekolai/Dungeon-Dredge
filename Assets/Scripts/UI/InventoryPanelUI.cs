using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DungeonDredge.Core;
using DungeonDredge.Inventory;
using DungeonDredge.Dungeon;

namespace DungeonDredge.UI
{
    /// <summary>
    /// Wrapper controller for the full inventory screen.
    /// Left side: Character stats panel. Center: Inventory grid. Right: Item detail panel.
    /// </summary>
    public class InventoryPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private CharacterPanelUI characterPanel;
        [SerializeField] private InventoryUI inventoryUI;
        [SerializeField] private ItemDetailPanelUI itemDetailPanel;

        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private Image overlayBackground;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI goldTotalText;
        [SerializeField] private TextMeshProUGUI itemCountText;
        [SerializeField] private TextMeshProUGUI rankText;


        [Header("Drop Zone")]
        [SerializeField] private RectTransform gridBoundary;

        [Header("Settings")]
        [SerializeField] private float fadeSpeed = 8f;
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.6f);

        private bool isVisible = false;

        // Public accessor for other UI components
        public PlayerInventory PlayerInventory => playerInventory;

        private void Start()
        {
            // Subscribe to player spawn for runtime reference finding
            DungeonManager.OnPlayerSpawned += OnPlayerSpawned;

            // Try to find player now
            TryBindToPlayer();

            if (overlayBackground != null)
            {
                overlayBackground.color = overlayColor;
            }

            // Ensure detail panel exists and is bound
            EnsureDetailPanel();

            // Start hidden
            SetVisible(false);
        }

        private void OnDestroy()
        {
            DungeonManager.OnPlayerSpawned -= OnPlayerSpawned;
            UnbindFromPlayer();
        }

        private void OnPlayerSpawned(GameObject player)
        {
            TryBindToPlayer(player);
        }

        private void TryBindToPlayer(GameObject player = null)
        {
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            // Unbind old player first
            UnbindFromPlayer();

            playerInventory = player.GetComponent<PlayerInventory>();

            if (playerInventory != null)
            {
                playerInventory.OnInventoryOpened += Show;
                playerInventory.OnInventoryClosed += Hide;

                if (playerInventory.Grid != null)
                {
                    playerInventory.Grid.OnItemAdded += OnInventoryChanged;
                    playerInventory.Grid.OnItemRemoved += OnInventoryChanged;
                }
            }

            // Also bind the child InventoryUI to the same player
            if (inventoryUI != null)
            {
                inventoryUI.BindToPlayer(playerInventory);
            }
        }

        private void UnbindFromPlayer()
        {
            if (playerInventory != null)
            {
                playerInventory.OnInventoryOpened -= Show;
                playerInventory.OnInventoryClosed -= Hide;

                if (playerInventory.Grid != null)
                {
                    playerInventory.Grid.OnItemAdded -= OnInventoryChanged;
                    playerInventory.Grid.OnItemRemoved -= OnInventoryChanged;
                }
            }
        }

        /// <summary>
        /// Ensures the ItemDetailPanelUI exists. Creates one programmatically if not assigned.
        /// </summary>
        private void EnsureDetailPanel()
        {
            if (itemDetailPanel != null)
            {
                // Already assigned in inspector, just bind it
                if (inventoryUI != null)
                    itemDetailPanel.BindToInventoryUI(inventoryUI);
                return;
            }

            // Try to find one in children
            itemDetailPanel = GetComponentInChildren<ItemDetailPanelUI>(true);
            if (itemDetailPanel != null)
            {
                if (inventoryUI != null)
                    itemDetailPanel.BindToInventoryUI(inventoryUI);
                return;
            }

            // Create one programmatically as a child of this panel
            var detailGO = new GameObject("ItemDetailPanel");
            detailGO.transform.SetParent(transform, false);

            var detailRect = detailGO.AddComponent<RectTransform>();
            // Position to the right side of the panel
            detailRect.anchorMin = new Vector2(1f, 0.5f);
            detailRect.anchorMax = new Vector2(1f, 0.5f);
            detailRect.pivot = new Vector2(0f, 0.5f);
            detailRect.anchoredPosition = new Vector2(16f, 0f);
            detailRect.sizeDelta = new Vector2(220f, 400f);

            itemDetailPanel = detailGO.AddComponent<ItemDetailPanelUI>();
            if (inventoryUI != null)
                itemDetailPanel.BindToInventoryUI(inventoryUI);
        }

        public void Show()
        {
            isVisible = true;

            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1f;
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
            }

            // Show the inventory grid (driven by us, not independently)
            if (inventoryUI != null)
                inventoryUI.Show();

            // Refresh data
            characterPanel?.OnShow();
            RefreshHeader();
        }

        public void Hide()
        {
            isVisible = false;

            // Hide the inventory grid
            if (inventoryUI != null)
                inventoryUI.Hide();

            // Hide the detail panel
            if (itemDetailPanel != null)
                itemDetailPanel.HidePanel();

            // Immediately hide everything - no fade
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;

            if (panelRoot != null)
                panelRoot.SetActive(visible);

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = visible ? 1f : 0f;
                panelCanvasGroup.interactable = visible;
                panelCanvasGroup.blocksRaycasts = visible;
            }
        }

        private void OnInventoryChanged(InventoryItem item)
        {
            RefreshHeader();
            characterPanel?.RefreshWeightInfo();
        }

        private void RefreshHeader()
        {
            if (playerInventory?.Grid == null) return;

            if (goldTotalText != null)
            {
                int totalGold = playerInventory.Grid.GetTotalValue();
                goldTotalText.text = $"{totalGold}g";
            }

            if (itemCountText != null)
            {
                int itemCount = playerInventory.Grid.Items.Count;
                itemCountText.text = $"{itemCount} items";
            }

            if (rankText != null)
            {
                rankText.text = $"Rank: {Village.QuestManager.Instance?.CurrentRank}";
            }
        }

        /// <summary>
        /// Check if a screen position is outside the inventory grid boundary.
        /// Used by InventoryItemUI to determine if an item should be dropped to world.
        /// </summary>
        public bool IsOutsideGrid(Vector2 screenPosition)
        {
            if (gridBoundary == null) return false;

            return !RectTransformUtility.RectangleContainsScreenPoint(
                gridBoundary, screenPosition, null);
        }

        /// <summary>
        /// Drop an item from the inventory into the world.
        /// </summary>
        public void DropItemToWorld(InventoryItem item)
        {
            if (playerInventory == null || item == null) return;
            playerInventory.DropItem(item);
        }
    }
}
