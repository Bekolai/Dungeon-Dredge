using UnityEngine;
using DungeonDredge.Player;
using DungeonDredge.Inventory;
using DungeonDredge.Dungeon;

namespace DungeonDredge.Village
{
    public class DungeonCart : MonoBehaviour, IInteractable
    {
        [Header("References")]
        [SerializeField] private DungeonManager dungeonManager;

        [Header("Visual")]
        [SerializeField] private GameObject interactionIndicator;

        // Events
        public System.Action OnCartInteracted;
        public System.Action<DungeonRank> OnDungeonSelected;

        private void Start()
        {
            if (dungeonManager == null)
            {
                dungeonManager = FindObjectOfType<DungeonManager>();
            }
        }

        public void Interact(PlayerController player)
        {
            OnCartInteracted?.Invoke();
            // Open dungeon selection UI
            Debug.Log("Opening dungeon selection");
        }

        public string GetInteractionPrompt()
        {
            return "Select Dungeon";
        }

        public bool CanEnterDungeon(DungeonRank rank)
        {
            if (QuestManager.Instance == null) return rank == DungeonRank.F;
            return rank <= QuestManager.Instance.CurrentRank;
        }

        public void EnterDungeon(DungeonRank rank)
        {
            if (!CanEnterDungeon(rank))
            {
                Debug.Log($"Rank {rank} is locked!");
                return;
            }

            if (dungeonManager != null)
            {
                dungeonManager.StartDungeon(rank);
                OnDungeonSelected?.Invoke(rank);
            }
        }

        public DungeonRankInfo[] GetAvailableRanks()
        {
            DungeonRank maxRank = QuestManager.Instance?.CurrentRank ?? DungeonRank.F;

            var ranks = new System.Collections.Generic.List<DungeonRankInfo>();

            for (int i = 0; i <= (int)maxRank && i <= (int)DungeonRank.S; i++)
            {
                DungeonRank rank = (DungeonRank)i;
                ranks.Add(new DungeonRankInfo
                {
                    rank = rank,
                    isUnlocked = rank <= maxRank,
                    displayName = GetRankDisplayName(rank),
                    description = GetRankDescription(rank)
                });
            }

            return ranks.ToArray();
        }

        private string GetRankDisplayName(DungeonRank rank)
        {
            return $"Rank {rank} Dungeon";
        }

        private string GetRankDescription(DungeonRank rank)
        {
            return rank switch
            {
                DungeonRank.F => "Cleared dungeon. Pests flee from light.",
                DungeonRank.E => "Unstable. Aggressive monsters require tools.",
                DungeonRank.D => "Dangerous. Stalker AI. High value loot.",
                DungeonRank.C => "Treacherous. Advanced threats.",
                DungeonRank.B => "Deadly. Not for the unprepared.",
                DungeonRank.A => "Extreme danger. Elite rewards.",
                DungeonRank.S => "The Deep. Legends only.",
                _ => "Unknown danger level"
            };
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

    [System.Serializable]
    public class DungeonRankInfo
    {
        public DungeonRank rank;
        public bool isUnlocked;
        public string displayName;
        public string description;
    }
}
