using UnityEngine;

namespace DungeonDredge.Core
{
    /// <summary>
    /// Bootstrapper that initializes all required game systems in the correct order.
    /// Place this in a "Bootstrap" scene that loads first.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("System Prefabs")]
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject audioManagerPrefab;
        [SerializeField] private GameObject stealthManagerPrefab;
        [SerializeField] private GameObject questManagerPrefab;
        [SerializeField] private GameObject shopManagerPrefab;
        [SerializeField] private GameObject saveSystemPrefab;
        [SerializeField] private GameObject steamIntegrationPrefab;
        [SerializeField] private GameObject objectPoolPrefab;
        [SerializeField] private GameObject performanceManagerPrefab;

        [Header("Startup")]
        [SerializeField] private string firstSceneToLoad = "MainMenu";
        [SerializeField] private bool loadSceneAfterInit = true;

        private void Awake()
        {
            // Ensure this bootstrapper is unique
            if (FindObjectsOfType<GameBootstrapper>().Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            InitializeSystems();
        }

        private void InitializeSystems()
        {
            Debug.Log("Bootstrapping game systems...");

            // Initialize in order of dependency

            // 1. Performance Manager (first for optimization)
            InstantiateSystem(performanceManagerPrefab, "PerformanceManager");

            // 2. Object Pool (needed by many systems)
            InstantiateSystem(objectPoolPrefab, "ObjectPool");

            // 3. Game Manager (core)
            InstantiateSystem(gameManagerPrefab, "GameManager");

            // 4. Steam Integration (before save system)
            InstantiateSystem(steamIntegrationPrefab, "SteamIntegration");

            // 5. Save System
            InstantiateSystem(saveSystemPrefab, "SaveSystem");

            // 6. Audio Manager
            InstantiateSystem(audioManagerPrefab, "AudioManager");

            // 7. Stealth Manager
            InstantiateSystem(stealthManagerPrefab, "StealthManager");

            // 8. Quest Manager
            InstantiateSystem(questManagerPrefab, "QuestManager");

            // 9. Shop Manager
            InstantiateSystem(shopManagerPrefab, "ShopManager");

            Debug.Log("All game systems initialized.");

            // Load first scene
            if (loadSceneAfterInit)
            {
                LoadFirstScene();
            }
        }

        private void InstantiateSystem(GameObject prefab, string systemName)
        {
            if (prefab != null)
            {
                Instantiate(prefab);
                Debug.Log($"  - {systemName} initialized");
            }
            else
            {
                Debug.LogWarning($"  - {systemName} prefab not assigned!");
            }
        }

        private void LoadFirstScene()
        {
            if (!string.IsNullOrEmpty(firstSceneToLoad))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(firstSceneToLoad);
            }
        }

        #region Editor Helper

#if UNITY_EDITOR
        [ContextMenu("Create Default System Prefabs")]
        private void CreateDefaultPrefabs()
        {
            Debug.Log("Creating system prefabs...");

            // Create GameManager
            if (gameManagerPrefab == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
                Debug.Log("Created GameManager prefab");
            }

            // Add similar for other systems...
            Debug.Log("Remember to save these as prefabs!");
        }
#endif

        #endregion
    }
}
