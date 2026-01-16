using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DungeonDredge.Village;

namespace DungeonDredge.UI
{
    public class QuestUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject questPanel;
        [SerializeField] private GameObject questListPanel;
        [SerializeField] private GameObject questDetailPanel;

        [Header("Quest List")]
        [SerializeField] private Transform availableQuestsContainer;
        [SerializeField] private Transform activeQuestsContainer;
        [SerializeField] private GameObject questListItemPrefab;

        [Header("Quest Details")]
        [SerializeField] private TextMeshProUGUI questTitleText;
        [SerializeField] private TextMeshProUGUI questDescriptionText;
        [SerializeField] private Transform objectivesContainer;
        [SerializeField] private GameObject objectivePrefab;
        [SerializeField] private TextMeshProUGUI rewardsText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button abandonButton;
        [SerializeField] private Button turnInButton;

        [Header("Tabs")]
        [SerializeField] private Button availableTabButton;
        [SerializeField] private Button activeTabButton;
        [SerializeField] private Button closeButton;

        private QuestData selectedQuest;
        private bool showingAvailable = true;
        private List<GameObject> spawnedItems = new List<GameObject>();

        private void Start()
        {
            if (availableTabButton != null)
                availableTabButton.onClick.AddListener(() => ShowTab(true));
            if (activeTabButton != null)
                activeTabButton.onClick.AddListener(() => ShowTab(false));
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (acceptButton != null)
                acceptButton.onClick.AddListener(OnAcceptQuest);
            if (abandonButton != null)
                abandonButton.onClick.AddListener(OnAbandonQuest);
            if (turnInButton != null)
                turnInButton.onClick.AddListener(OnTurnInQuest);

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestAccepted += OnQuestStateChanged;
                QuestManager.Instance.OnQuestCompleted += OnQuestStateChanged;
            }

            Close();
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestAccepted -= OnQuestStateChanged;
                QuestManager.Instance.OnQuestCompleted -= OnQuestStateChanged;
            }
        }

        public void Open()
        {
            questPanel.SetActive(true);
            ShowTab(true);
            ClearDetails();

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            questPanel.SetActive(false);
            selectedQuest = null;

            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ShowTab(bool available)
        {
            showingAvailable = available;
            PopulateQuestList();
            ClearDetails();
        }

        private void PopulateQuestList()
        {
            ClearSpawnedItems();

            if (QuestManager.Instance == null) return;

            Transform container = showingAvailable ? availableQuestsContainer : activeQuestsContainer;
            if (container == null) return;

            List<QuestData> quests = showingAvailable ? 
                QuestManager.Instance.GetAvailableQuests() : 
                QuestManager.Instance.GetActiveQuests();

            foreach (var quest in quests)
            {
                if (questListItemPrefab == null) continue;

                GameObject item = Instantiate(questListItemPrefab, container);
                spawnedItems.Add(item);

                var itemUI = item.GetComponent<QuestListItemUI>();
                if (itemUI != null)
                {
                    QuestStatus status = QuestManager.Instance.GetQuestStatus(quest.questId);
                    itemUI.Setup(quest, status, OnQuestSelected);
                }
            }
        }

        private void OnQuestSelected(QuestData quest)
        {
            selectedQuest = quest;
            ShowQuestDetails(quest);
        }

        private void ShowQuestDetails(QuestData quest)
        {
            if (questDetailPanel != null)
                questDetailPanel.SetActive(true);

            if (questTitleText != null)
                questTitleText.text = quest.questName;
            if (questDescriptionText != null)
                questDescriptionText.text = quest.description;

            // Populate objectives
            ClearObjectives();
            if (objectivesContainer != null && objectivePrefab != null)
            {
                foreach (var objective in quest.objectives)
                {
                    GameObject objItem = Instantiate(objectivePrefab, objectivesContainer);
                    spawnedItems.Add(objItem);

                    var objUI = objItem.GetComponent<QuestObjectiveUI>();
                    if (objUI != null)
                    {
                        int progress = QuestManager.Instance?.GetObjectiveProgress(quest.questId, objective.objectiveId) ?? 0;
                        objUI.Setup(objective, progress);
                    }
                }
            }

            // Rewards
            if (rewardsText != null)
            {
                string rewards = $"Rewards:\n- {quest.goldReward} Gold";
                if (quest.unlocksRank > QuestManager.Instance?.CurrentRank)
                {
                    rewards += $"\n- Unlocks Rank {quest.unlocksRank}";
                }
                rewardsText.text = rewards;
            }

            // Update buttons
            QuestStatus status = QuestManager.Instance?.GetQuestStatus(quest.questId) ?? QuestStatus.Locked;

            if (acceptButton != null)
                acceptButton.gameObject.SetActive(status == QuestStatus.Available);
            if (abandonButton != null)
                abandonButton.gameObject.SetActive(status == QuestStatus.Active);
            if (turnInButton != null)
                turnInButton.gameObject.SetActive(status == QuestStatus.Completed);
        }

        private void ClearDetails()
        {
            if (questDetailPanel != null)
                questDetailPanel.SetActive(false);

            selectedQuest = null;
            ClearObjectives();
        }

        private void ClearObjectives()
        {
            if (objectivesContainer == null) return;

            foreach (Transform child in objectivesContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void OnAcceptQuest()
        {
            if (selectedQuest == null) return;

            QuestManager.Instance?.AcceptQuest(selectedQuest.questId);
            PopulateQuestList();
            ClearDetails();
        }

        private void OnAbandonQuest()
        {
            if (selectedQuest == null) return;

            QuestManager.Instance?.AbandonQuest(selectedQuest.questId);
            PopulateQuestList();
            ClearDetails();
        }

        private void OnTurnInQuest()
        {
            if (selectedQuest == null) return;

            QuestManager.Instance?.TurnInQuest(selectedQuest.questId);
            PopulateQuestList();
            ClearDetails();
        }

        private void OnQuestStateChanged(QuestData quest)
        {
            PopulateQuestList();
            if (selectedQuest != null && selectedQuest.questId == quest.questId)
            {
                ShowQuestDetails(quest);
            }
        }

        private void ClearSpawnedItems()
        {
            foreach (var item in spawnedItems)
            {
                if (item != null)
                    Destroy(item);
            }
            spawnedItems.Clear();
        }
    }

    public class QuestListItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button selectButton;
        [SerializeField] private Image backgroundImage;

        public void Setup(QuestData quest, QuestStatus status, System.Action<QuestData> onSelected)
        {
            if (nameText != null)
                nameText.text = quest.questName;
            
            if (statusText != null)
            {
                statusText.text = status switch
                {
                    QuestStatus.Available => "Available",
                    QuestStatus.Active => "In Progress",
                    QuestStatus.Completed => "Complete!",
                    QuestStatus.TurnedIn => "Done",
                    _ => ""
                };

                statusText.color = status switch
                {
                    QuestStatus.Completed => Color.green,
                    QuestStatus.Active => Color.yellow,
                    _ => Color.white
                };
            }

            if (selectButton != null)
                selectButton.onClick.AddListener(() => onSelected?.Invoke(quest));
        }
    }

    public class QuestObjectiveUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Toggle completedToggle;

        public void Setup(QuestObjective objective, int currentProgress)
        {
            if (descriptionText != null)
                descriptionText.text = objective.description;

            bool isComplete = currentProgress >= objective.requiredAmount;

            if (progressText != null)
                progressText.text = $"{currentProgress}/{objective.requiredAmount}";

            if (completedToggle != null)
            {
                completedToggle.isOn = isComplete;
                completedToggle.interactable = false;
            }

            if (descriptionText != null)
                descriptionText.fontStyle = isComplete ? FontStyles.Strikethrough : FontStyles.Normal;
        }
    }
}
