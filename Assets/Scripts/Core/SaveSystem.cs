using UnityEngine;
using System.IO;
using DungeonDredge.Player;
using DungeonDredge.Inventory;
using DungeonDredge.Village;

namespace DungeonDredge.Core
{
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private string saveFileName = "dungeon_dredge_save.json";
        [SerializeField] private bool useSteamCloud = false;

        private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

        // Events
        public System.Action OnSaveCompleted;
        public System.Action OnLoadCompleted;
        public System.Action<string> OnSaveError;
        public System.Action<string> OnLoadError;

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

        #region Save

        public void Save()
        {
            try
            {
                GameSaveData saveData = GatherSaveData();
                string json = JsonUtility.ToJson(saveData, true);

                if (useSteamCloud && SteamIntegration.Instance?.IsInitialized == true)
                {
                    // Save to Steam Cloud
                    SteamIntegration.Instance.SaveToCloud(saveFileName, json);
                }
                else
                {
                    // Save locally
                    File.WriteAllText(SavePath, json);
                }

                Debug.Log($"Game saved to {SavePath}");
                OnSaveCompleted?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Save failed: {e.Message}");
                OnSaveError?.Invoke(e.Message);
            }
        }

        private GameSaveData GatherSaveData()
        {
            GameSaveData data = new GameSaveData();

            // Player stats
            var playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats != null)
            {
                data.playerStats = playerStats.GetSaveData();
            }

            // Player inventory
            var playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null)
            {
                data.inventory = playerInventory.GetSaveData();
            }

            // Shop/currency
            if (ShopManager.Instance != null)
            {
                data.shop = ShopManager.Instance.GetSaveData();
            }

            // Quests
            if (QuestManager.Instance != null)
            {
                data.quests = QuestManager.Instance.GetSaveData();
            }

            // Metadata
            data.saveVersion = Application.version;
            data.saveTimestamp = System.DateTime.Now.ToString("o");
            data.totalPlayTime = Time.realtimeSinceStartup; // Would need proper tracking

            return data;
        }

        #endregion

        #region Load

        public void Load()
        {
            try
            {
                string json = null;

                if (useSteamCloud && SteamIntegration.Instance?.IsInitialized == true)
                {
                    // Load from Steam Cloud
                    json = SteamIntegration.Instance.LoadFromCloud(saveFileName);
                }
                else
                {
                    // Load locally
                    if (File.Exists(SavePath))
                    {
                        json = File.ReadAllText(SavePath);
                    }
                }

                if (string.IsNullOrEmpty(json))
                {
                    Debug.Log("No save file found");
                    OnLoadError?.Invoke("No save file found");
                    return;
                }

                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
                ApplySaveData(saveData);

                Debug.Log("Game loaded successfully");
                OnLoadCompleted?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Load failed: {e.Message}");
                OnLoadError?.Invoke(e.Message);
            }
        }

        private void ApplySaveData(GameSaveData data)
        {
            // Player stats
            var playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats != null && data.playerStats != null)
            {
                playerStats.LoadSaveData(data.playerStats);
            }

            // Player inventory
            var playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null && data.inventory != null)
            {
                // Need references to databases
                var backpackDb = Resources.Load<BackpackDatabase>("BackpackDatabase");
                playerInventory.LoadSaveData(data.inventory, backpackDb);
            }

            // Shop/currency
            if (ShopManager.Instance != null && data.shop != null)
            {
                ShopManager.Instance.LoadSaveData(data.shop);
            }

            // Quests
            if (QuestManager.Instance != null && data.quests != null)
            {
                QuestManager.Instance.LoadSaveData(data.quests);
            }
        }

        #endregion

        #region Utility

        public bool HasSave()
        {
            if (useSteamCloud && SteamIntegration.Instance?.IsInitialized == true)
            {
                return SteamIntegration.Instance.HasCloudSave(saveFileName);
            }
            return File.Exists(SavePath);
        }

        public void DeleteSave()
        {
            if (useSteamCloud && SteamIntegration.Instance?.IsInitialized == true)
            {
                SteamIntegration.Instance.DeleteCloudSave(saveFileName);
            }
            else if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
        }

        public GameSaveData GetSaveInfo()
        {
            if (!HasSave()) return null;

            try
            {
                string json;
                if (useSteamCloud && SteamIntegration.Instance?.IsInitialized == true)
                {
                    json = SteamIntegration.Instance.LoadFromCloud(saveFileName);
                }
                else
                {
                    json = File.ReadAllText(SavePath);
                }

                return JsonUtility.FromJson<GameSaveData>(json);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Auto-Save

        public void EnableAutoSave(float intervalSeconds = 300f)
        {
            InvokeRepeating(nameof(AutoSave), intervalSeconds, intervalSeconds);
        }

        public void DisableAutoSave()
        {
            CancelInvoke(nameof(AutoSave));
        }

        private void AutoSave()
        {
            // Only auto-save in safe states
            if (GameManager.Instance?.CurrentState == GameState.Village)
            {
                Save();
            }
        }

        #endregion
    }

    [System.Serializable]
    public class GameSaveData
    {
        // Metadata
        public string saveVersion;
        public string saveTimestamp;
        public float totalPlayTime;

        // Game data
        public StatsSaveData playerStats;
        public PlayerInventorySaveData inventory;
        public ShopSaveData shop;
        public QuestSaveData quests;
    }
}
