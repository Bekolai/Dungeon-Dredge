using UnityEngine;
using DungeonDredge.Player;

namespace DungeonDredge.Inventory
{
    public class DroppedBackpack : MonoBehaviour, IInteractable
    {
        [Header("Visual")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material highlightMaterial;

        private InventorySaveData savedInventory;
        private BackpackData backpackData;
        private Material originalMaterial;

        public InventorySaveData SavedInventory => savedInventory;
        public BackpackData BackpackData => backpackData;

        public void Initialize(InventorySaveData inventory, BackpackData backpack)
        {
            savedInventory = inventory;
            backpackData = backpack;
        }

        public void Interact(PlayerController player)
        {
            // Player picks up the backpack
            var playerInventory = player.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.TryPickupBackpack();
            }
        }

        public string GetInteractionPrompt()
        {
            int itemCount = savedInventory?.items?.Count ?? 0;
            return $"Pick up Backpack ({itemCount} items)";
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && highlightMaterial != null && meshRenderer != null)
            {
                originalMaterial = meshRenderer.material;
                meshRenderer.material = highlightMaterial;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && originalMaterial != null && meshRenderer != null)
            {
                meshRenderer.material = originalMaterial;
            }
        }
    }
}
