using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonDredge.Core
{
    public enum GameState
    {
        MainMenu,
        Village,
        Dungeon,
        Paused,
        Dead,
        Extracting
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        public GameState CurrentState => currentState;

        [Header("Scene Names")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string villageScene = "Village";
        [SerializeField] private string dungeonScene = "Dungeon";

        // Track previous state so we can resume to the right one
        private GameState previousGameplayState = GameState.Dungeon;

        // Events
        public System.Action<GameState> OnGameStateChanged;
        public System.Action OnPlayerDied;
        public System.Action OnPlayerExtracted;

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

        public void SetGameState(GameState newState)
        {
            if (currentState == newState) return;

            GameState previousState = currentState;
            currentState = newState;

            HandleStateChange(previousState, newState);
            OnGameStateChanged?.Invoke(newState);
        }

        private void HandleStateChange(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.Paused:
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;

                case GameState.Dungeon:
                case GameState.Village:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;

                case GameState.MainMenu:
                case GameState.Dead:
                case GameState.Extracting:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
            }
        }

        public void LoadMainMenu()
        {
            SetGameState(GameState.MainMenu);
            SceneManager.LoadScene(mainMenuScene);
        }

        public void LoadVillage()
        {
            SetGameState(GameState.Village);
            SceneManager.LoadScene(villageScene);
        }

        public void LoadDungeon()
        {
            SetGameState(GameState.Dungeon);
            SceneManager.LoadScene(dungeonScene);
        }

        public void PauseGame()
        {
            if (currentState == GameState.Dungeon || currentState == GameState.Village)
            {
                previousGameplayState = currentState;
                SetGameState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                // Return to the previous gameplay state (Dungeon or Village)
                SetGameState(previousGameplayState);
            }
        }

        public void PlayerDied()
        {
            SetGameState(GameState.Dead);
            OnPlayerDied?.Invoke();
        }

        public void PlayerExtracted()
        {
            SetGameState(GameState.Extracting);
            OnPlayerExtracted?.Invoke();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
