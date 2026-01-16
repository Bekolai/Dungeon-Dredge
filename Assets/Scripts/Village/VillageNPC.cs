using UnityEngine;
using DungeonDredge.Player;

namespace DungeonDredge.Village
{
    public enum NPCType
    {
        Shopkeeper,
        Blacksmith,
        GuildMaster,
        CartDriver
    }

    public class VillageNPC : MonoBehaviour, IInteractable
    {
        [Header("NPC Info")]
        [SerializeField] private string npcName;
        [SerializeField] private NPCType npcType;
        [TextArea(2, 4)]
        [SerializeField] private string greeting;

        [Header("Shop Data")]
        [SerializeField] private ShopData shopData;

        [Header("Visual")]
        [SerializeField] private GameObject interactionIndicator;

        // Events
        public System.Action<VillageNPC> OnInteracted;

        public string NPCName => npcName;
        public NPCType Type => npcType;
        public ShopData Shop => shopData;
        public string Greeting => greeting;

        public void Interact(PlayerController player)
        {
            OnInteracted?.Invoke(this);

            // Open appropriate UI based on type
            switch (npcType)
            {
                case NPCType.Shopkeeper:
                    OpenShopUI();
                    break;
                case NPCType.Blacksmith:
                    OpenBlacksmithUI();
                    break;
                case NPCType.GuildMaster:
                    OpenGuildUI();
                    break;
                case NPCType.CartDriver:
                    OpenDungeonSelectUI();
                    break;
            }
        }

        public string GetInteractionPrompt()
        {
            return $"Talk to {npcName}";
        }

        private void OpenShopUI()
        {
            // UI system will handle this
            Debug.Log($"Opening shop: {shopData?.shopName}");
        }

        private void OpenBlacksmithUI()
        {
            Debug.Log("Opening blacksmith upgrades");
        }

        private void OpenGuildUI()
        {
            Debug.Log("Opening guild quests");
        }

        private void OpenDungeonSelectUI()
        {
            Debug.Log("Opening dungeon select");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && interactionIndicator != null)
            {
                interactionIndicator.SetActive(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && interactionIndicator != null)
            {
                interactionIndicator.SetActive(false);
            }
        }
    }
}
