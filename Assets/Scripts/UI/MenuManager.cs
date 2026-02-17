using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DungeonDredge.Core;
using DungeonDredge.Inventory;
using DungeonDredge.Village;

namespace DungeonDredge.UI
{
    public class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance { get; private set; }

        [Header("Menu Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject deathPanel;
        [SerializeField] private GameObject extractionPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Main Menu")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Pause Menu")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseSettingsButton;
        [SerializeField] private Button saveQuitButton;

        [Header("Death Screen")]
        [SerializeField] private TextMeshProUGUI deathMessageText;
        [SerializeField] private Button returnToVillageButton;

        [Header("Extraction Screen")]
        [SerializeField] private TextMeshProUGUI goldEarnedText;
        [SerializeField] private TextMeshProUGUI itemsCollectedText;
        [SerializeField] private TextMeshProUGUI xpGainedText;
        [SerializeField] private Button extractionContinueButton;

        [Header("Settings")]
        [SerializeField] private Slider mouseSensitivitySlider;
        [SerializeField] private Toggle invertYToggle;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Button settingsBackButton;

        [Header("Loading")]
        [SerializeField] private Slider loadingProgressBar;
        [SerializeField] private TextMeshProUGUI loadingText;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetupButtonListeners();
            HideAllPanels();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                GameManager.Instance.OnPlayerDied += ShowDeathScreen;
                GameManager.Instance.OnPlayerExtracted += ShowExtractionScreen;
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestCompleted += (quest) => DungeonDredge.Audio.AudioManager.Instance?.PlayQuestComplete();
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                GameManager.Instance.OnPlayerDied -= ShowDeathScreen;
                GameManager.Instance.OnPlayerExtracted -= ShowExtractionScreen;
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestCompleted -= (quest) => DungeonDredge.Audio.AudioManager.Instance?.PlayQuestComplete();
            }
        }

        private void Update()
        {
            // Handle escape key for pause
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        private void SetupButtonListeners()
        {
            // Main Menu
            if (newGameButton != null)
                newGameButton.onClick.AddListener(() => { PlayButtonClick(); OnNewGame(); });
            if (continueButton != null)
                continueButton.onClick.AddListener(() => { PlayButtonClick(); OnContinue(); });
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => { PlayButtonClick(); ShowSettings(); });
            if (quitButton != null)
                quitButton.onClick.AddListener(() => { PlayButtonClick(); OnQuit(); });

            // Pause Menu
            if (resumeButton != null)
                resumeButton.onClick.AddListener(() => { PlayButtonClick(); OnResume(); });
            if (pauseSettingsButton != null)
                pauseSettingsButton.onClick.AddListener(() => { PlayButtonClick(); ShowSettings(); });
            if (saveQuitButton != null)
                saveQuitButton.onClick.AddListener(() => { PlayButtonClick(); OnSaveAndQuit(); });

            // Death Screen
            if (returnToVillageButton != null)
                returnToVillageButton.onClick.AddListener(() => { PlayButtonClick(); OnReturnToVillage(); });

            // Extraction Screen
            if (extractionContinueButton != null)
                extractionContinueButton.onClick.AddListener(() => { PlayButtonClick(); OnReturnToVillage(); });

            // Settings
            if (settingsBackButton != null)
                settingsBackButton.onClick.AddListener(() => { PlayButtonClick(); HideSettings(); });

            // Settings sliders
            if (mouseSensitivitySlider != null)
                mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
            if (invertYToggle != null)
                invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        private void PlayButtonClick()
        {
            DungeonDredge.Audio.AudioManager.Instance?.PlayButtonClick();
        }

        #region Panel Management

        private void HideAllPanels()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (deathPanel != null) deathPanel.SetActive(false);
            if (extractionPanel != null) extractionPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }

        public void ShowMainMenu()
        {
            HideAllPanels();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            
            // Check if save exists for continue button
            if (continueButton != null)
            {
                // continueButton.interactable = SaveSystem.HasSave();
                continueButton.interactable = true; // Placeholder
            }
        }

        public void ShowPauseMenu()
        {
            HideAllPanels();
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        }

        public void ShowSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void HideSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void ShowDeathScreen()
        {
            HideAllPanels();
            if (deathPanel != null) deathPanel.SetActive(true);
            
            if (deathMessageText != null)
            {
                deathMessageText.text = "You died. All items lost.";
            }
        }

        public void ShowExtractionScreen()
        {
            HideAllPanels();
            if (extractionPanel != null) extractionPanel.SetActive(true);
        }

        public void SetExtractionResults(int gold, int items, int xp)
        {
            if (goldEarnedText != null)
                goldEarnedText.text = $"Gold: {gold}";
            if (itemsCollectedText != null)
                itemsCollectedText.text = $"Items: {items}";
            if (xpGainedText != null)
                xpGainedText.text = $"XP: {xp}";
        }

        public void ShowLoading(string message = "Loading...")
        {
            HideAllPanels();
            if (loadingPanel != null) loadingPanel.SetActive(true);
            if (loadingText != null) loadingText.text = message;
            if (loadingProgressBar != null) loadingProgressBar.value = 0f;
        }

        public void UpdateLoadingProgress(float progress)
        {
            if (loadingProgressBar != null)
                loadingProgressBar.value = progress;
        }

        public void HideLoading()
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }

        #endregion

        #region Button Handlers

        private void OnNewGame()
        {
            HideAllPanels();
            GameManager.Instance?.LoadVillage();
        }

        private void OnContinue()
        {
            HideAllPanels();
            // Load save and go to village
            GameManager.Instance?.LoadVillage();
        }

        private void OnResume()
        {
            HideAllPanels();
            GameManager.Instance?.ResumeGame();
        }

        private void OnSaveAndQuit()
        {
            // Save game
            // SaveSystem.Save();
            GameManager.Instance?.LoadMainMenu();
        }

        private void OnReturnToVillage()
        {
            HideAllPanels();
            GameManager.Instance?.LoadVillage();
        }

        private void OnQuit()
        {
            GameManager.Instance?.QuitGame();
        }

        private void TogglePause()
        {
            if (GameManager.Instance == null) return;

            // If inventory is open, let PlayerInventory handle the Escape key (close inventory first)
            var playerInv = FindAnyObjectByType<PlayerInventory>();
            if (playerInv != null && playerInv.IsInventoryOpen)
            {
                // PlayerInventory.Update() will handle closing via Escape
                return;
            }

            if (GameManager.Instance.CurrentState == GameState.Paused)
            {
                DungeonDredge.Audio.AudioManager.Instance?.PlayMenuClose();
                OnResume();
            }
            else if (GameManager.Instance.CurrentState == GameState.Dungeon || 
                     GameManager.Instance.CurrentState == GameState.Village)
            {
                DungeonDredge.Audio.AudioManager.Instance?.PlayMenuOpen();
                GameManager.Instance.PauseGame();
                ShowPauseMenu();
            }
        }

        #endregion

        #region Settings Handlers

        private void OnMouseSensitivityChanged(float value)
        {
            var player = FindAnyObjectByType<Player.PlayerController>();
            player?.SetMouseSensitivity(value);
            PlayerPrefs.SetFloat("MouseSensitivity", value);
        }

        private void OnInvertYChanged(bool value)
        {
            var player = FindAnyObjectByType<Player.PlayerController>();
            player?.SetInvertY(value);
            PlayerPrefs.SetInt("InvertY", value ? 1 : 0);
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("MasterVolume", value);
        }

        #endregion

        #region Game State Handling

        private void OnGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    ShowMainMenu();
                    break;
                case GameState.Paused:
                    ShowPauseMenu();
                    break;
                case GameState.Dead:
                    ShowDeathScreen();
                    break;
                case GameState.Extracting:
                    ShowExtractionScreen();
                    break;
                case GameState.Village:
                case GameState.Dungeon:
                    HideAllPanels();
                    break;
            }
        }

        #endregion
    }
}
