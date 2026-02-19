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

        // State
        private float currentEyeLevel = 0f;
        private float currentNoiseLevel = 0f;
        private float currentThreatLevel = 0f;
        private float pulseTimer = 0f;
        private bool isStaminaVisible = false;
        private float temporaryPromptTimer = 0f;

        private void Start()
        {
            // Subscribe to EventBus events (these work without player ref)
            EventBus.Subscribe<HealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<StaminaChangedEvent>(OnStaminaChanged);
            EventBus.Subscribe<EncumbranceChangedEvent>(OnEncumbranceChanged);
            EventBus.Subscribe<InventoryFeedbackEvent>(OnInventoryFeedback);

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
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<HealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<StaminaChangedEvent>(OnStaminaChanged);
            EventBus.Unsubscribe<EncumbranceChangedEvent>(OnEncumbranceChanged);
            EventBus.Unsubscribe<InventoryFeedbackEvent>(OnInventoryFeedback);

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
            UpdateTemporaryPromptTimer();
            if (temporaryPromptTimer <= 0f)
            {
                UpdateInteractionCheck();
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

            ShowInteractionPrompt(evt.Message);
            temporaryPromptTimer = Mathf.Max(0.2f, evt.Duration);
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
