using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DungeonDredge.Core;
using DungeonDredge.Player;
using DungeonDredge.Inventory;
using DungeonDredge.Tools;

namespace DungeonDredge.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private StaminaSystem staminaSystem;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private ToolManager toolManager;

        [Header("Stealth Eye")]
        [SerializeField] private Image stealthEyeImage;
        [SerializeField] private Sprite[] eyeSprites; // Closed to open states
        [SerializeField] private float eyeUpdateSpeed = 5f;

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
        private float currentThreatLevel = 0f;
        private float pulseTimer = 0f;
        private bool isStaminaVisible = false;

        private void Start()
        {
            // Subscribe to events
            EventBus.Subscribe<StaminaChangedEvent>(OnStaminaChanged);
            EventBus.Subscribe<EncumbranceChangedEvent>(OnEncumbranceChanged);

            if (StealthManager.Instance != null)
            {
                StealthManager.Instance.OnNoiseChanged += OnNoiseChanged;
                StealthManager.Instance.OnThreatDetected += OnThreatDetected;
            }

            if (toolManager != null)
            {
                toolManager.OnSlotChanged += OnToolSlotChanged;
            }

            // Initialize UI
            UpdateWeightUI(0f);
            UpdateStaminaUI(1f, 100f, 100f);
            UpdateToolSlots();
            HideTargetInfo();
            HideInteractionPrompt();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<StaminaChangedEvent>(OnStaminaChanged);
            EventBus.Unsubscribe<EncumbranceChangedEvent>(OnEncumbranceChanged);

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

        private void Update()
        {
            UpdateStealthEye();
            UpdateThreatPulse();
            UpdateStaminaVisibility();
            UpdateInteractionCheck();
        }

        #region Stealth Eye

        private void UpdateStealthEye()
        {
            if (stealthEyeImage == null || eyeSprites == null || eyeSprites.Length == 0) return;

            // Smoothly interpolate eye level
            float targetLevel = StealthManager.Instance?.NoiseRatio ?? 0f;
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
                weightText.text = $"{current:F1}/{max:F1}kg ({percent}%)";
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

            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    ShowInteractionPrompt(interactable.GetInteractionPrompt());
                    SetCrosshairInteract(true);
                    
                    // Check for enemy target info
                    var enemy = hit.collider.GetComponent<AI.EnemyAI>();
                    if (enemy != null)
                    {
                        ShowTargetInfo(enemy.EnemyName, enemy.Rank.ToString(), 1f);
                    }
                    else
                    {
                        HideTargetInfo();
                    }
                    return;
                }
            }

            HideInteractionPrompt();
            HideTargetInfo();
            SetCrosshairInteract(false);
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
