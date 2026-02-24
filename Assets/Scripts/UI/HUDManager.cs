using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DungeonDredge.Core;
using DungeonDredge.Player;
using DungeonDredge.Inventory;
using DungeonDredge.Tools;
using DungeonDredge.Dungeon;

namespace DungeonDredge.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private StaminaSystem staminaSystem;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private ToolManager toolManager;
        private PlayerController playerController;

        [Header("HP Bar")]
        [SerializeField] private Slider hpSlider;
        [SerializeField] private Image hpFill;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Color healthyColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color criticalColor = new Color(0.4f, 0f, 0f);

        [Header("Stealth Eye")]
        [SerializeField] private Image stealthEyeImage;
        [SerializeField] private Sprite[] eyeSprites; // Closed to open states
        [SerializeField] private float eyeUpdateSpeed = 5f;

        [Header("Stealth Noise Bar")]
        [SerializeField] private Slider noiseSlider;
        [SerializeField] private Image noiseFill;
        [SerializeField] private CanvasGroup noiseBarGroup;
        [SerializeField] private Color quietNoiseColor = new Color(0.3f, 0.7f, 0.3f);
        [SerializeField] private Color loudNoiseColor = new Color(0.9f, 0.2f, 0.2f);

        [Header("Threat Pulse")]
        [SerializeField] private Image threatVignette;
        [SerializeField] private float minPulseSpeed = 0.5f;
        [SerializeField] private float maxPulseSpeed = 4f;
        [SerializeField] private float threatFadeSpeed = 2f;
        [SerializeField] private bool enableHeartbeatAudio = true;
        [SerializeField] private AudioSource heartbeatAudioSource;
        [SerializeField] private AudioClip heartbeatClip;

        [Header("Weight Bar")]
        [SerializeField] private Slider weightSlider;
        [SerializeField] private Image weightFill;
        [SerializeField] private TextMeshProUGUI weightText;
        [SerializeField] private Gradient weightGradient;

        [Header("Stamina Bar")]
        [SerializeField] private Slider staminaSlider;
        [SerializeField] private Image staminaFill;
        [SerializeField] private CanvasGroup staminaGroup;
        [SerializeField] private Color normalStaminaColor = Color.green;
        [SerializeField] private Color exhaustedStaminaColor = Color.red;

        [Header("Dungeon Timer")]
        [SerializeField] private CanvasGroup timerGroup;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Color normalTimerColor = Color.white;
        [SerializeField] private Color urgentTimerColor = Color.red;
        [SerializeField] private float urgentTimeThreshold = 60f; // 1 min left

        [Header("Tool Hotbar")]
        [SerializeField] private ToolSlotUI[] toolSlots;

        [Header("Crosshair")]
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Color normalCrosshairColor = Color.white;
        [SerializeField] private Color interactCrosshairColor = Color.yellow;

        [Header("Interaction Prompt")]
        [SerializeField] private CanvasGroup interactionPromptGroup;
        [SerializeField] private TextMeshProUGUI interactionText;
        [SerializeField] private Image interactionKeyIcon;

        [Header("Target Info")]
        [SerializeField] private CanvasGroup targetInfoGroup;
        [SerializeField] private TextMeshProUGUI targetNameText;
        [SerializeField] private TextMeshProUGUI targetRankText;
        [SerializeField] private Slider targetHealthSlider;

        [Header("Notifications (bottom-left)")]
        [SerializeField] private RectTransform notificationContainer;
        [SerializeField] private GameObject notificationEntryPrefab; // Optional prefab, or we'll create dynamically
        [SerializeField] private float notificationDefaultDuration = 2.5f;
        [SerializeField] private float notificationFadeInTime = 0.2f;
        [SerializeField] private float notificationFadeOutTime = 0.3f;
        [SerializeField] private float notificationSpacing = 5f; // Vertical spacing between notifications
        [SerializeField] private int notificationMaxCount = 5; // Max notifications visible at once
        [SerializeField] private int notificationPoolSize = 10; // Initial pool size

        // State
        private float currentEyeLevel = 0f;
        private float currentNoiseLevel = 0f;
        private float currentThreatLevel = 0f;
        private float pulseTimer = 0f;
        private bool isStaminaVisible = false;
        private float temporaryPromptTimer = 0f;

        private Dictionary<StatType, int> currentStatLevels = new Dictionary<StatType, int>();

        // Notifications: list of active notification entries
        private class NotificationEntry
        {
            public GameObject gameObject;
            public TextMeshProUGUI text;
            public CanvasGroup canvasGroup;
            public RectTransform rectTransform;
            public float timer;
            public float fadeInRemaining;
        }
        private readonly List<NotificationEntry> activeNotifications = new();
        
        // Object pool for notification entries
        private readonly Queue<GameObject> notificationPool = new();

        private void Start()
        {
            // Subscribe to EventBus events (these work without player ref)
            EventBus.Subscribe<HealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<StaminaChangedEvent>(OnStaminaChanged);
            EventBus.Subscribe<EncumbranceChangedEvent>(OnEncumbranceChanged);
            EventBus.Subscribe<InventoryFeedbackEvent>(OnInventoryFeedback);
            EventBus.Subscribe<PlayerStatChangedEvent>(OnPlayerStatChanged);

            if (StealthManager.Instance != null)
            {
                StealthManager.Instance.OnNoiseChanged += OnNoiseChanged;
                StealthManager.Instance.OnThreatDetected += OnThreatDetected;
            }

            // Subscribe to player spawn event for runtime reference finding
            DungeonManager.OnPlayerSpawned += OnPlayerSpawned;

            // Try to find player now (may already exist)
            TryFindPlayerReferences();

            // Initialize UI
            UpdateHPUI(1f, 100f, 100f);
            UpdateWeightUI(0f);
            UpdateStaminaUI(1f, 100f, 100f);
            UpdateNoiseUI(0f);
            UpdateToolSlots();
            HideTargetInfo();
            HideInteractionPrompt();
            InitializeNotificationPool();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<HealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<StaminaChangedEvent>(OnStaminaChanged);
            EventBus.Unsubscribe<EncumbranceChangedEvent>(OnEncumbranceChanged);
            EventBus.Unsubscribe<InventoryFeedbackEvent>(OnInventoryFeedback);
            EventBus.Unsubscribe<PlayerStatChangedEvent>(OnPlayerStatChanged);

            DungeonManager.OnPlayerSpawned -= OnPlayerSpawned;

            if (StealthManager.Instance != null)
            {
                StealthManager.Instance.OnNoiseChanged -= OnNoiseChanged;
                StealthManager.Instance.OnThreatDetected -= OnThreatDetected;
            }

            if (toolManager != null)
            {
                toolManager.OnSlotChanged -= OnToolSlotChanged;
            }
        }

        /// <summary>
        /// Called when DungeonManager spawns the player at runtime.
        /// </summary>
        private void OnPlayerSpawned(GameObject player)
        {
            TryFindPlayerReferences(player);
        }

        /// <summary>
        /// Find player component references at runtime.
        /// </summary>
        private void TryFindPlayerReferences(GameObject player = null)
        {
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }
            if (player == null) return;

            if (playerController == null)
                playerController = player.GetComponent<PlayerController>();
            if (playerMovement == null)
                playerMovement = player.GetComponent<PlayerMovement>();
            if (staminaSystem == null)
                staminaSystem = player.GetComponent<StaminaSystem>();
            if (playerInventory == null)
                playerInventory = player.GetComponent<PlayerInventory>();
            if (toolManager == null)
                toolManager = player.GetComponentInChildren<ToolManager>();

            // Re-subscribe to tool manager events
            if (toolManager != null)
            {
                toolManager.OnSlotChanged -= OnToolSlotChanged; // prevent double
                toolManager.OnSlotChanged += OnToolSlotChanged;
                UpdateToolSlots();
            }
        }

        private void Update()
        {
            UpdateStealthEye();
            UpdateStealthNoiseBar();
            UpdateThreatPulse();
            UpdateStaminaVisibility();
            UpdateNotifications();
            UpdateTimer();
            UpdateTemporaryPromptTimer();
            if (temporaryPromptTimer <= 0f)
            {
                UpdateInteractionCheck();
            }
        }

        private void UpdateTimer()
        {
            if (DungeonManager.Instance != null && DungeonManager.Instance.IsDungeonActive)
            {
                if (timerGroup != null) timerGroup.alpha = 1f;

                if (timerText != null)
                {
                    float timeRemaining = DungeonManager.Instance.TimeRemaining;
                    int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                    int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                    
                    timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                    
                    if (timeRemaining <= urgentTimeThreshold)
                    {
                        // Pulse the urgent color
                        float pulse = Mathf.PingPong(Time.time * 2f, 1f);
                        timerText.color = Color.Lerp(normalTimerColor, urgentTimerColor, pulse);
                    }
                    else
                    {
                        timerText.color = normalTimerColor;
                    }
                }
            }
            else
            {
                if (timerGroup != null) timerGroup.alpha = 0f;
            }
        }

        #region HP Bar

        private void OnHealthChanged(HealthChangedEvent evt)
        {
            if (!evt.IsPlayer) return;
            UpdateHPUI(evt.Ratio, evt.CurrentHealth, evt.MaxHealth);
        }

        private void UpdateHPUI(float ratio, float current, float max)
        {
            if (hpSlider != null)
            {
                hpSlider.value = ratio;
            }

            if (hpFill != null)
            {
                hpFill.color = Color.Lerp(criticalColor, healthyColor, ratio);
            }

            if (hpText != null)
            {
                hpText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            }
        }

        #endregion

        #region Stealth Eye

        private void UpdateStealthEye()
        {
            if (stealthEyeImage == null || eyeSprites == null || eyeSprites.Length == 0) return;

            // Use the combined visibility score (crouching, lantern, movement, noise)
            // instead of raw noise ratio so the eye actually reacts to player actions
            float targetLevel = StealthManager.Instance?.PlayerVisibility ?? 0f;
            currentEyeLevel = Mathf.Lerp(currentEyeLevel, targetLevel, Time.deltaTime * eyeUpdateSpeed);

            // Select sprite based on level
            int spriteIndex = Mathf.Clamp(
                Mathf.FloorToInt(currentEyeLevel * (eyeSprites.Length - 1)),
                0, eyeSprites.Length - 1);

            stealthEyeImage.sprite = eyeSprites[spriteIndex];
        }

        private void OnNoiseChanged(float noiseLevel)
        {
            // Eye will update in Update()
            // Also update the noise bar target
            currentNoiseLevel = noiseLevel;
        }

        #endregion

        #region Stealth Noise Bar

        private void UpdateStealthNoiseBar()
        {
            float targetNoise = StealthManager.Instance?.NoiseRatio ?? 0f;
            float smoothNoise = noiseSlider != null ? 
                Mathf.Lerp(noiseSlider.value, targetNoise, Time.deltaTime * eyeUpdateSpeed) : 0f;

            UpdateNoiseUI(smoothNoise);

            // Auto-hide the noise BAR only (not the eye icon) when quiet
            // noiseBarGroup should be assigned to the noise slider's own CanvasGroup,
            // not the parent that also contains the eye icon
            if (noiseBarGroup != null)
            {
                float targetAlpha = targetNoise > 0.01f ? 1f : 0f;
                noiseBarGroup.alpha = Mathf.Lerp(noiseBarGroup.alpha, targetAlpha, Time.deltaTime * 3f);
            }

            // Eye is always visible - ensure it stays enabled
            if (stealthEyeImage != null && !stealthEyeImage.gameObject.activeSelf)
            {
                stealthEyeImage.gameObject.SetActive(true);
            }
        }

        private void UpdateNoiseUI(float noiseRatio)
        {
            if (noiseSlider != null)
            {
                noiseSlider.value = noiseRatio;
            }

            if (noiseFill != null)
            {
                noiseFill.color = Color.Lerp(quietNoiseColor, loudNoiseColor, noiseRatio);
            }
        }

        #endregion

        #region Threat Pulse

        private void OnThreatDetected(float distance, GameObject threat)
        {
            if (threat == null) return;

            // Convert distance to threat level (closer = higher)
            float maxDistance = 30f;
            currentThreatLevel = 1f - Mathf.Clamp01(distance / maxDistance);
        }

        private void UpdateThreatPulse()
        {
            if (threatVignette == null) return;

            // Fade threat level over time if no threat
            if (StealthManager.Instance?.NearestThreat == null)
            {
                currentThreatLevel = Mathf.Lerp(currentThreatLevel, 0f, Time.deltaTime * threatFadeSpeed);
            }

            if (currentThreatLevel <= 0.01f)
            {
                threatVignette.gameObject.SetActive(false);
                if (heartbeatAudioSource != null && heartbeatAudioSource.isPlaying)
                {
                    heartbeatAudioSource.Stop();
                }
                return;
            }

            threatVignette.gameObject.SetActive(true);

            // Calculate pulse
            float pulseSpeed = Mathf.Lerp(minPulseSpeed, maxPulseSpeed, currentThreatLevel);
            pulseTimer += Time.deltaTime * pulseSpeed;

            float pulse = (Mathf.Sin(pulseTimer * Mathf.PI * 2f) + 1f) / 2f;
            float alpha = currentThreatLevel * pulse * 0.5f;

            Color vignetteColor = threatVignette.color;
            vignetteColor.a = alpha;
            threatVignette.color = vignetteColor;

            // Heartbeat audio mapping
            if (enableHeartbeatAudio && heartbeatAudioSource != null && heartbeatClip != null)
            {
                if (!heartbeatAudioSource.isPlaying)
                {
                    heartbeatAudioSource.clip = heartbeatClip;
                    heartbeatAudioSource.loop = true;
                    heartbeatAudioSource.Play();
                }
                heartbeatAudioSource.volume = currentThreatLevel * 0.8f;
                // Map the pitch to the pulse speed so it beats faster
                heartbeatAudioSource.pitch = Mathf.Lerp(0.9f, 1.4f, currentThreatLevel);
            }
            else if (heartbeatAudioSource != null && heartbeatAudioSource.isPlaying)
            {
                heartbeatAudioSource.Stop(); // Stop if disabled during gameplay
            }
        }

        #endregion

        #region Weight

        private void OnEncumbranceChanged(EncumbranceChangedEvent evt)
        {
            UpdateWeightUI(evt.WeightRatio);
        }

        private void UpdateWeightUI(float weightRatio)
        {
            if (weightSlider != null)
            {
                weightSlider.value = Mathf.Clamp01(weightRatio);
            }

            if (weightFill != null && weightGradient != null)
            {
                weightFill.color = weightGradient.Evaluate(weightRatio);
            }

            if (weightText != null && playerInventory?.Grid != null)
            {
                float current = playerInventory.Grid.CurrentWeight;
                float max = playerInventory.Grid.MaxWeight;
                int percent = Mathf.RoundToInt(weightRatio * 100f);
                //weightText.text = $"{current:F1}/{max:F1}kg ({percent}%)";
                weightText.text = $"{current:F1}/{max:F1}kg";
            }
        }

        #endregion

        #region Stamina

        private void OnStaminaChanged(StaminaChangedEvent evt)
        {

            UpdateStaminaUI(evt.Ratio, evt.CurrentStamina, evt.MaxStamina);
        }

        private void UpdateStaminaUI(float ratio, float current, float max)
        {
            if (staminaSlider != null)
            {
                staminaSlider.value = ratio;
            }

            if (staminaFill != null)
            {
                staminaFill.color = ratio < 0.2f ? exhaustedStaminaColor : normalStaminaColor;
            }

            // Show stamina bar when not full
            isStaminaVisible = ratio < 0.99f;
        }

        private void UpdateStaminaVisibility()
        {
            if (staminaGroup == null) return;

            float targetAlpha = isStaminaVisible ? 1f : 0f;
            staminaGroup.alpha = Mathf.Lerp(staminaGroup.alpha, targetAlpha, Time.deltaTime * 5f);
        }

        #endregion

        #region Tool Hotbar

        private void OnToolSlotChanged(int slot)
        {
            UpdateToolSlots();
        }

        private void UpdateToolSlots()
        {
            if (toolSlots == null || toolManager == null) return;

            for (int i = 0; i < toolSlots.Length; i++)
            {
                if (toolSlots[i] == null) continue;

                ToolBase tool = i < toolManager.AllTools.Length ? toolManager.AllTools[i] : null;
                bool isSelected = i == toolManager.CurrentSlot;

                toolSlots[i].UpdateSlot(tool, isSelected);
            }
        }

        #endregion

        #region Interaction

        private void UpdateInteractionCheck()
        {
            if (Camera.main == null) return;

            // Use the same range and layer mask as the PlayerController so prompt matches actual interaction
            float range = playerController != null ? playerController.InteractionRange : 3f;
            int mask = playerController != null ? playerController.EffectiveInteractionMask : Physics.DefaultRaycastLayers;

            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, range, mask, QueryTriggerInteraction.Collide))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable == null)
                {
                    interactable = hit.collider.GetComponentInParent<IInteractable>();
                }
                if (interactable != null)
                {
                    string prompt = interactable.GetInteractionPrompt();

                    // Skip empty prompts (e.g. archway doors that are always open)
                    if (string.IsNullOrEmpty(prompt))
                    {
                        HideInteractionPrompt();
                        SetCrosshairInteract(false);
                        HideTargetInfo();
                        return;
                    }

                    ShowInteractionPrompt(prompt);
                    SetCrosshairInteract(true);
                    
                    // Check for enemy target info
                    var enemy = hit.collider.GetComponent<AI.EnemyAI>();
                    if (enemy == null)
                    {
                        enemy = hit.collider.GetComponentInParent<AI.EnemyAI>();
                    }
                    if (enemy != null)
                    {
                        float healthRatio = 1f;
                        var health = enemy.GetComponent<HealthComponent>() ?? enemy.GetComponentInParent<HealthComponent>();
                        if (health != null && health.MaxHealth > 0f)
                        {
                            healthRatio = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
                        }
                        ShowTargetInfo(enemy.EnemyName, enemy.Rank.ToString(), healthRatio);
                    }
                    else
                    {
                        HideTargetInfo();
                    }
                    return;
                }
            }

            HideInteractionPrompt();
            SetCrosshairInteract(false);

            // Extended target scan: detect enemies at longer range even if not interactable
            UpdateTargetScan(ray);
        }

        [Header("Target Scan")]
        [SerializeField] private float targetScanRange = 15f;
        [SerializeField] private LayerMask targetScanMask;

        private void UpdateTargetScan(Ray ray)
        {
            if (Physics.Raycast(ray, out RaycastHit scanHit, targetScanRange, targetScanMask != 0 ? (int)targetScanMask : Physics.DefaultRaycastLayers))
            {
                var enemy = scanHit.collider.GetComponent<AI.EnemyAI>();
                if (enemy == null) enemy = scanHit.collider.GetComponentInParent<AI.EnemyAI>();

                if (enemy != null)
                {
                    float healthRatio = 1f;
                    var health = enemy.GetComponent<HealthComponent>() ?? enemy.GetComponentInParent<HealthComponent>();
                    if (health != null && health.MaxHealth > 0f)
                    {
                        healthRatio = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
                    }
                    ShowTargetInfo(enemy.EnemyName, enemy.Rank.ToString(), healthRatio);
                    return;
                }
            }

            HideTargetInfo();
        }

        public void ShowInteractionPrompt(string text)
        {
            if (interactionPromptGroup != null)
            {
                interactionPromptGroup.alpha = 1f;
            }
            if (interactionText != null)
            {
                interactionText.text = text;
            }
        }

        private void OnInventoryFeedback(InventoryFeedbackEvent evt)
        {
            if (string.IsNullOrWhiteSpace(evt.Message))
                return;

            float duration = evt.Duration > 0f ? evt.Duration : notificationDefaultDuration;
            ShowNotification(evt.Message, duration);
        }

        private void OnPlayerStatChanged(PlayerStatChangedEvent evt)
        {
            if (!currentStatLevels.ContainsKey(evt.StatType))
            {
                currentStatLevels[evt.StatType] = evt.NewLevel;
                return; // Don't notify on initial load/spawn
            }

            if (evt.NewLevel > currentStatLevels[evt.StatType])
            {
                currentStatLevels[evt.StatType] = evt.NewLevel;
                string statName = evt.StatType.ToString();
                ShowNotification($"{statName} level up {evt.NewLevel}!", notificationDefaultDuration);
            }
        }

        private void UpdateTemporaryPromptTimer()
        {
            if (temporaryPromptTimer <= 0f)
                return;

            temporaryPromptTimer -= Time.deltaTime;
            if (temporaryPromptTimer <= 0f)
            {
                HideInteractionPrompt();
            }
        }

        public void HideInteractionPrompt()
        {
            if (interactionPromptGroup != null)
            {
                interactionPromptGroup.alpha = 0f;
            }
        }

        private void SetCrosshairInteract(bool interact)
        {
            if (crosshairImage != null)
            {
                crosshairImage.color = interact ? interactCrosshairColor : normalCrosshairColor;
            }
        }

        #endregion

        #region Notifications

        [ContextMenu("Debug Notifications")]
        public void DebugNotifications()
        {
            ShowNotification("Looted Golden Idol!", 2f);
            ShowNotification("Endurance level up 2!", 2.5f);
            ShowNotification("No room in backpack", 1.5f);
        }

        /// <summary>
        /// Initialize the notification pool with pre-created entries.
        /// </summary>
        private void InitializeNotificationPool()
        {
            if (notificationContainer == null) return;

            for (int i = 0; i < notificationPoolSize; i++)
            {
                GameObject pooledObj = CreateNotificationEntry();
                ReturnToPool(pooledObj);
            }
        }

        /// <summary>
        /// Get a notification entry from the pool, or create a new one if pool is empty.
        /// </summary>
        private GameObject GetFromPool()
        {
            GameObject entryObj;
            
            if (notificationPool.Count > 0)
            {
                entryObj = notificationPool.Dequeue();
                entryObj.SetActive(true);
            }
            else
            {
                // Pool is empty, create a new one
                entryObj = CreateNotificationEntry();
            }

            return entryObj;
        }

        /// <summary>
        /// Return a notification entry to the pool for reuse.
        /// </summary>
        private void ReturnToPool(GameObject entryObj)
        {
            if (entryObj == null) return;

            entryObj.SetActive(false);
            entryObj.transform.SetParent(notificationContainer, false);
            
            // Reset components
            CanvasGroup canvasGroup = entryObj.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            TextMeshProUGUI text = entryObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = "";
            }

            notificationPool.Enqueue(entryObj);
        }

        /// <summary>
        /// Show a temporary message in the bottom-left notification area.
        /// New notifications appear at the bottom, pushing older ones up.
        /// </summary>
        public void ShowNotification(string message, float duration = -1f)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (notificationContainer == null) return;
            if (duration <= 0f) duration = notificationDefaultDuration;

            // Remove oldest if we're at max count
            if (activeNotifications.Count >= notificationMaxCount)
            {
                NotificationEntry oldest = activeNotifications[0];
                activeNotifications.RemoveAt(0);
                RemoveNotification(oldest);
            }

            // Get notification entry from pool
            GameObject entryObj = GetFromPool();

            NotificationEntry entry = new NotificationEntry
            {
                gameObject = entryObj,
                text = entryObj.GetComponentInChildren<TextMeshProUGUI>(),
                canvasGroup = entryObj.GetComponent<CanvasGroup>(),
                rectTransform = entryObj.GetComponent<RectTransform>(),
                timer = duration,
                fadeInRemaining = notificationFadeInTime
            };

            // Ensure CanvasGroup exists
            if (entry.canvasGroup == null)
            {
                entry.canvasGroup = entryObj.AddComponent<CanvasGroup>();
            }

            // Set text
            if (entry.text != null)
            {
                entry.text.text = message;
            }

            // Initialize position and alpha
            if (entry.rectTransform != null)
            {
                entry.rectTransform.anchoredPosition = Vector2.zero;
                entry.rectTransform.anchorMin = new Vector2(0f, 0f);
                entry.rectTransform.anchorMax = new Vector2(1f, 0f);
                entry.rectTransform.pivot = new Vector2(0f, 0f);
            }
            entry.canvasGroup.alpha = 0f;

            // Add to list (newest at end, oldest at start)
            activeNotifications.Add(entry);

            // Update positions
            UpdateNotificationPositions();
        }

        private GameObject CreateNotificationEntry()
        {
            GameObject entryObj;
            
            if (notificationEntryPrefab != null)
            {
                entryObj = Instantiate(notificationEntryPrefab, notificationContainer);
            }
            else
            {
                // Create a simple notification entry GameObject if no prefab is provided
                entryObj = new GameObject("NotificationEntry");
                entryObj.transform.SetParent(notificationContainer, false);

                RectTransform rect = entryObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.sizeDelta = new Vector2(0f, 30f); // Height will be auto-sized by text

                CanvasGroup canvasGroup = entryObj.AddComponent<CanvasGroup>();

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(entryObj.transform, false);
                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = "";
                text.fontSize = 14f;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Left;
                text.overflowMode = TextOverflowModes.Truncate;

                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                textRect.offsetMin = new Vector2(5f, 0f);
                textRect.offsetMax = new Vector2(-5f, 0f);
            }

            return entryObj;
        }

        private void UpdateNotifications()
        {
            if (notificationContainer == null) return;

            // Update all active notifications
            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                NotificationEntry entry = activeNotifications[i];
                if (entry == null || entry.gameObject == null)
                {
                    activeNotifications.RemoveAt(i);
                    continue;
                }

                entry.timer -= Time.deltaTime;

                // Fade in at start
                if (entry.fadeInRemaining > 0f)
                {
                    entry.fadeInRemaining -= Time.deltaTime;
                    float t = 1f - Mathf.Clamp01(entry.fadeInRemaining / notificationFadeInTime);
                    entry.canvasGroup.alpha = t;
                }
                // Fade out near end
                else if (entry.timer <= notificationFadeOutTime)
                {
                    entry.canvasGroup.alpha = Mathf.Clamp01(entry.timer / notificationFadeOutTime);
                }
                else
                {
                    entry.canvasGroup.alpha = 1f;
                }

                // Remove expired notifications
                if (entry.timer <= 0f)
                {
                    RemoveNotification(entry);
                    activeNotifications.RemoveAt(i);
                }
            }

            // Update positions after removals
            UpdateNotificationPositions();
        }

        private void UpdateNotificationPositions()
        {
            float yOffset = 0f;
            for (int i = 0; i < activeNotifications.Count; i++)
            {
                NotificationEntry entry = activeNotifications[i];
                if (entry?.rectTransform == null) continue;

                // Position from bottom up (newest at bottom, oldest at top)
                entry.rectTransform.anchoredPosition = new Vector2(0f, yOffset);
                
                // Calculate height (text height + spacing)
                float height = entry.text != null ? entry.text.preferredHeight : 30f;
                entry.rectTransform.sizeDelta = new Vector2(0f, height);
                
                yOffset += height + notificationSpacing;
            }
        }

        private void RemoveNotification(NotificationEntry entry)
        {
            if (entry?.gameObject != null)
            {
                ReturnToPool(entry.gameObject);
            }
        }

        #endregion

        #region Target Info

        public void ShowTargetInfo(string name, string rank, float healthPercent)
        {
            if (targetInfoGroup != null)
            {
                targetInfoGroup.alpha = 1f;
            }
            if (targetNameText != null)
            {
                targetNameText.text = name;
            }
            if (targetRankText != null)
            {
                targetRankText.text = $"Rank {rank}";
            }
            if (targetHealthSlider != null)
            {
                targetHealthSlider.value = healthPercent;
            }
        }

        public void HideTargetInfo()
        {
            if (targetInfoGroup != null)
            {
                targetInfoGroup.alpha = 0f;
            }
        }

        #endregion
    }
}
