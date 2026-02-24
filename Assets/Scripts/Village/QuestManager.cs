using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DungeonDredge.Core;
using DungeonDredge.Inventory;

namespace DungeonDredge.Village
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Quests")]
        [SerializeField] private QuestData[] allQuests;

        // Quest states
        private Dictionary<string, QuestProgress> questProgress = new Dictionary<string, QuestProgress>();
        private List<string> completedQuests = new List<string>();
        private List<string> activeQuests = new List<string>();

        // Current rank
        private DungeonRank currentRank = DungeonRank.F;

        // Properties
        public DungeonRank CurrentRank  => currentRank;
        public IReadOnlyList<string> ActiveQuests => activeQuests;
        public IReadOnlyList<string> CompletedQuests => completedQuests;

        // Events
        public System.Action<QuestData> OnQuestAccepted;
        public System.Action<QuestData> OnQuestCompleted;
        public System.Action<QuestData, QuestObjective> OnObjectiveProgress;
        public System.Action<DungeonRank> OnRankUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Subscribe<ExtractionCompletedEvent>(OnExtraction);
            EventBus.Subscribe<EnemyStunnedEvent>(OnEnemyStunned);
            EventBus.Subscribe<NodeMinedEvent>(OnNodeMined);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Unsubscribe<ExtractionCompletedEvent>(OnExtraction);
            EventBus.Unsubscribe<EnemyStunnedEvent>(OnEnemyStunned);
            EventBus.Unsubscribe<NodeMinedEvent>(OnNodeMined);
        }

        #region Quest Management

        public QuestData GetQuest(string questId)
        {
            return allQuests.FirstOrDefault(q => q.questId == questId);
        }

        public QuestStatus GetQuestStatus(string questId)
        {
            if (completedQuests.Contains(questId))
            {
                var progress = GetProgress(questId);
                return progress.turnedIn ? QuestStatus.TurnedIn : QuestStatus.Completed;
            }
            
            if (activeQuests.Contains(questId))
                return QuestStatus.Active;

            QuestData quest = GetQuest(questId);
            if (quest == null) return QuestStatus.Locked;

            // Check prerequisites
            if (quest.prerequisiteQuestIds != null)
            {
                foreach (var prereq in quest.prerequisiteQuestIds)
                {
                    if (!completedQuests.Contains(prereq))
                        return QuestStatus.Locked;
                }
            }

            // Check rank requirement
            if (quest.requiredRank > currentRank)
                return QuestStatus.Locked;

            return QuestStatus.Available;
        }

        public List<QuestData> GetAvailableQuests()
        {
            return allQuests.Where(q => GetQuestStatus(q.questId) == QuestStatus.Available).ToList();
        }

        public List<QuestData> GetActiveQuests()
        {
            return activeQuests.Select(id => GetQuest(id)).Where(q => q != null).ToList();
        }

        public bool AcceptQuest(string questId)
        {
            if (GetQuestStatus(questId) != QuestStatus.Available)
                return false;

            QuestData quest = GetQuest(questId);
            if (quest == null) return false;

            activeQuests.Add(questId);

            // Initialize progress
            var progress = new QuestProgress
            {
                questId = questId,
                objectiveProgress = new Dictionary<string, int>()
            };

            foreach (var objective in quest.objectives)
            {
                progress.objectiveProgress[objective.objectiveId] = 0;
            }

            questProgress[questId] = progress;

            OnQuestAccepted?.Invoke(quest);
            return true;
        }

        public bool TurnInQuest(string questId)
        {
            if (GetQuestStatus(questId) != QuestStatus.Completed)
                return false;

            QuestData quest = GetQuest(questId);
            if (quest == null) return false;

            var progress = GetProgress(questId);
            progress.turnedIn = true;

            // Grant rewards
            // Gold would be added to player's currency
            // Items would be added to inventory
            // Unlock rank if applicable
            if (quest.unlocksRank > currentRank)
            {
                currentRank = quest.unlocksRank;
                OnRankUnlocked?.Invoke(currentRank);
            }

            OnQuestCompleted?.Invoke(quest);
            return true;
        }

    [ContextMenu("Debug Set Rank")]
    public void DebugSetRank()
    {
        currentRank = DungeonRank.S;
    }

        public void AbandonQuest(string questId)
        {
            activeQuests.Remove(questId);
            questProgress.Remove(questId);
        }

        #endregion

        #region Progress Tracking

        private QuestProgress GetProgress(string questId)
        {
            if (!questProgress.ContainsKey(questId))
            {
                questProgress[questId] = new QuestProgress
                {
                    questId = questId,
                    objectiveProgress = new Dictionary<string, int>()
                };
            }
            return questProgress[questId];
        }

        public int GetObjectiveProgress(string questId, string objectiveId)
        {
            var progress = GetProgress(questId);
            return progress.objectiveProgress.TryGetValue(objectiveId, out int value) ? value : 0;
        }

        public void UpdateObjectiveProgress(string questId, string objectiveId, int amount)
        {
            if (!activeQuests.Contains(questId)) return;

            QuestData quest = GetQuest(questId);
            if (quest == null) return;

            var progress = GetProgress(questId);
            
            if (!progress.objectiveProgress.ContainsKey(objectiveId))
                progress.objectiveProgress[objectiveId] = 0;

            progress.objectiveProgress[objectiveId] += amount;

            // Find objective
            var objective = quest.objectives.FirstOrDefault(o => o.objectiveId == objectiveId);
            if (objective != null)
            {
                OnObjectiveProgress?.Invoke(quest, objective);

                // Check if quest is complete
                CheckQuestCompletion(questId);
            }
        }

        private void CheckQuestCompletion(string questId)
        {
            QuestData quest = GetQuest(questId);
            if (quest == null) return;

            var progress = GetProgress(questId);
            bool allComplete = true;

            foreach (var objective in quest.objectives)
            {
                int current = progress.objectiveProgress.TryGetValue(objective.objectiveId, out int val) ? val : 0;
                
                if (current < objective.requiredAmount)
                {
                    allComplete = false;
                    break;
                }
            }

            if (allComplete && !completedQuests.Contains(questId))
            {
                completedQuests.Add(questId);
                activeQuests.Remove(questId);
            }
        }

        #endregion

        #region Event Handlers

        private void OnItemPickedUp(ItemPickedUpEvent evt)
        {
            // Check active quests for collect objectives
            foreach (var questId in activeQuests.ToList())
            {
                QuestData quest = GetQuest(questId);
                if (quest == null) continue;

                foreach (var objective in quest.objectives)
                {
                    if (objective.objectiveType == QuestObjective.ObjectiveType.CollectItem)
                    {
                        if (objective.targetItem != null && objective.targetItem.itemId == evt.ItemId)
                        {
                            UpdateObjectiveProgress(questId, objective.objectiveId, 1);
                        }
                    }
                }
            }
        }

        private void OnExtraction(ExtractionCompletedEvent evt)
        {
            // Check active quests for extraction objectives
            foreach (var questId in activeQuests.ToList())
            {
                QuestData quest = GetQuest(questId);
                if (quest == null) continue;

                foreach (var objective in quest.objectives)
                {
                    if (objective.objectiveType == QuestObjective.ObjectiveType.ExtractFromDungeon)
                    {
                        UpdateObjectiveProgress(questId, objective.objectiveId, 1);
                    }
                }
            }
        }

        private void OnNodeMined(NodeMinedEvent evt)
        {
            foreach (var questId in activeQuests.ToList())
            {
                QuestData quest = GetQuest(questId);
                if (quest == null) continue;

                foreach (var objective in quest.objectives)
                {
                    if (objective.objectiveType == QuestObjective.ObjectiveType.MineNode)
                    {
                        if (objective.targetItem != null && objective.targetItem.itemId == evt.ItemId)
                        {
                            UpdateObjectiveProgress(questId, objective.objectiveId, 1);
                        }
                    }
                }
            }
        }

        private void OnEnemyStunned(EnemyStunnedEvent evt)
        {
            foreach (var questId in activeQuests.ToList())
            {
                QuestData quest = GetQuest(questId);
                if (quest == null) continue;

                foreach (var objective in quest.objectives)
                {
                    if (objective.objectiveType == QuestObjective.ObjectiveType.StunEnemy)
                    {
                        if (string.IsNullOrEmpty(objective.targetEnemyName) || 
                            objective.targetEnemyName.Equals(evt.EnemyName, System.StringComparison.OrdinalIgnoreCase))
                        {
                            UpdateObjectiveProgress(questId, objective.objectiveId, 1);
                        }
                    }
                }
            }
        }

        #endregion

        #region Save/Load

        public QuestSaveData GetSaveData()
        {
            return new QuestSaveData
            {
                currentRank = (int)currentRank,
                activeQuests = new List<string>(activeQuests),
                completedQuests = new List<string>(completedQuests),
                questProgress = new List<QuestProgress>(questProgress.Values)
            };
        }

        public void LoadSaveData(QuestSaveData data)
        {
            currentRank = (DungeonRank)data.currentRank;
            activeQuests = new List<string>(data.activeQuests);
            completedQuests = new List<string>(data.completedQuests);
            
            questProgress.Clear();
            foreach (var progress in data.questProgress)
            {
                questProgress[progress.questId] = progress;
            }
        }

        #endregion
    }

    [System.Serializable]
    public class QuestProgress
    {
        public string questId;
        public Dictionary<string, int> objectiveProgress;
        public bool turnedIn;
    }

    [System.Serializable]
    public class QuestSaveData
    {
        public int currentRank;
        public List<string> activeQuests;
        public List<string> completedQuests;
        public List<QuestProgress> questProgress;
    }
}
