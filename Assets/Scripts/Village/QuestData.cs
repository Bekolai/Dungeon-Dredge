using UnityEngine;
using DungeonDredge.Inventory;

namespace DungeonDredge.Village
{
    public enum QuestType
    {
        Collect,
        Extract,
        Explore,
        Promotion
    }

    public enum QuestStatus
    {
        Locked,
        Available,
        Active,
        Completed,
        TurnedIn
    }

    [CreateAssetMenu(fileName = "NewQuest", menuName = "DungeonDredge/Quest Data")]
    public class QuestData : ScriptableObject
    {
        [Header("Basic Info")]
        public string questId;
        public string questName;
        [TextArea(2, 4)]
        public string description;
        public QuestType questType = QuestType.Collect;

        [Header("Requirements")]
        public QuestObjective[] objectives;
        public DungeonRank requiredRank = DungeonRank.F;
        public string[] prerequisiteQuestIds;

        [Header("Rewards")]
        public int goldReward = 100;
        public int experienceReward = 50;
        public DungeonRank unlocksRank = DungeonRank.F;
        public ItemData[] itemRewards;

        [Header("UI")]
        public Sprite questIcon;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(questId))
            {
                questId = name.ToLower().Replace(" ", "_");
            }
        }
    }

    [System.Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public ObjectiveType objectiveType;
        
        [Header("Collect Objective")]
        public ItemData targetItem;
        public int requiredAmount = 1;

        [Header("Explore Objective")]
        public DungeonRank targetDungeon;
        public bool mustExtract = true;

        public enum ObjectiveType
        {
            CollectItem,
            ExtractFromDungeon,
            ExploreRooms,
            DefeatEnemies // Even though non-lethal, could be "stun X enemies"
        }
    }
}
