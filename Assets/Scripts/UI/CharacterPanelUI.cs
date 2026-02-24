using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DungeonDredge.Core;
using DungeonDredge.Player;
using DungeonDredge.Inventory;
using DungeonDredge.Dungeon;

namespace DungeonDredge.UI
{
    public class CharacterPanelUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerInventory playerInventory;

        [Header("Character Portrait")]
        [SerializeField] private Image characterFrame;
        [SerializeField] private Image characterPortrait;

        [Header("Strength Row")]
        [SerializeField] private Image strengthIcon;
        [SerializeField] private TextMeshProUGUI strengthLevelText;
        [SerializeField] private Slider strengthXPBar;
        [SerializeField] private Image strengthXPFill;

        [Header("Endurance Row")]
        [SerializeField] private Image enduranceIcon;
        [SerializeField] private TextMeshProUGUI enduranceLevelText;
        [SerializeField] private Slider enduranceXPBar;
        [SerializeField] private Image enduranceXPFill;

        [Header("Perception Row")]
        [SerializeField] private Image perceptionIcon;
        [SerializeField] private TextMeshProUGUI perceptionLevelText;
        [SerializeField] private Slider perceptionXPBar;
        [SerializeField] private Image perceptionXPFill;

        [Header("Weight Info")]
        [SerializeField] private TextMeshProUGUI weightValueText;
        [SerializeField] private Slider weightBar;
        [SerializeField] private Image weightBarFill;
        [SerializeField] private TextMeshProUGUI encumbranceTierText;
        [SerializeField] private Gradient weightGradient;

        [Header("Backpack Info")]
        [SerializeField] private TextMeshProUGUI backpackSizeText;
        [SerializeField] private TextMeshProUGUI backpackNameText;

        [Header("Stat Colors")]
        [SerializeField] private Color strengthColor = new Color(0.9f, 0.3f, 0.2f);
        [SerializeField] private Color enduranceColor = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color perceptionColor = new Color(0.3f, 0.5f, 1f);

        [Header("Encumbrance Tier Colors")]
        [SerializeField] private Color lightTierColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color mediumTierColor = new Color(0.8f, 0.8f, 0.2f);
        [SerializeField] private Color heavyTierColor = new Color(0.9f, 0.5f, 0.1f);
        [SerializeField] private Color snailTierColor = new Color(0.9f, 0.2f, 0.2f);

        private EncumbranceTier currentTier = EncumbranceTier.Light;

        private void Start()
        {
            // Subscribe to events (work without player ref)
            EventBus.Subscribe<PlayerStatChangedEvent>(OnStatChanged);
            EventBus.Subscribe<EncumbranceChangedEvent>(OnEncumbranceChanged);

            // Subscribe to player spawn for runtime reference finding
            DungeonManager.OnPlayerSpawned += OnPlayerSpawned;

            // Try to find player now
            TryFindPlayerReferences();

            // Setup tooltips for stats
            SetupTooltips();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<PlayerStatChangedEvent>(OnStatChanged);
            EventBus.Unsubscribe<EncumbranceChangedEvent>(OnEncumbranceChanged);
            DungeonManager.OnPlayerSpawned -= OnPlayerSpawned;
        }

        private void OnPlayerSpawned(GameObject player)
        {
            TryFindPlayerReferences(player);
            RefreshAllStats();
            RefreshWeightInfo();
            RefreshBackpackInfo();
        }

        private void TryFindPlayerReferences(GameObject player = null)
        {
            if (player == null)
                player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            if (playerStats == null)
                playerStats = player.GetComponent<PlayerStats>();
            if (playerInventory == null)
                playerInventory = player.GetComponent<PlayerInventory>();
        }

        /// <summary>
        /// Called when the panel becomes visible to refresh all data
        /// </summary>
        public void OnShow()
        {
            RefreshAllStats();
            RefreshWeightInfo();
            RefreshBackpackInfo();
        }

        private void OnStatChanged(PlayerStatChangedEvent evt)
        {
            switch (evt.StatType)
            {
                case StatType.Strength:
                    UpdateStatRow(strengthLevelText, strengthXPBar, strengthXPFill,
                        playerStats.Strength, strengthColor, "STR");
                    // Strength affects weight capacity, refresh weight too
                    RefreshWeightInfo();
                    break;
                case StatType.Endurance:
                    UpdateStatRow(enduranceLevelText, enduranceXPBar, enduranceXPFill,
                        playerStats.Endurance, enduranceColor, "END");
                    break;
                case StatType.Perception:
                    UpdateStatRow(perceptionLevelText, perceptionXPBar, perceptionXPFill,
                        playerStats.Perception, perceptionColor, "PER");
                    break;
            }

            // Update tooltips when stats change to reflect new bonuses
            SetupTooltips();
        }

        private void OnEncumbranceChanged(EncumbranceChangedEvent evt)
        {
            currentTier = evt.Tier;
            RefreshWeightInfo();
        }

        private void RefreshAllStats()
        {
            if (playerStats == null) return;

            UpdateStatRow(strengthLevelText, strengthXPBar, strengthXPFill,
                playerStats.Strength, strengthColor, "STR");
            UpdateStatRow(enduranceLevelText, enduranceXPBar, enduranceXPFill,
                playerStats.Endurance, enduranceColor, "END");
            UpdateStatRow(perceptionLevelText, perceptionXPBar, perceptionXPFill,
                playerStats.Perception, perceptionColor, "PER");

            SetupTooltips();
        }

        private void UpdateStatRow(TextMeshProUGUI levelText, Slider xpBar,
            Image xpFill, Stat stat, Color color, string prefix)
        {
            if (levelText != null)
            {
                levelText.text = $"{prefix} {stat.level}";
            }

            if (xpBar != null)
            {
                xpBar.value = stat.XPProgress;
            }

            if (xpFill != null)
            {
                xpFill.color = color;
            }
        }

        public void RefreshWeightInfo()
        {
            if (playerInventory?.Grid == null) return;

            float currentWeight = playerInventory.Grid.CurrentWeight;
            float maxWeight = playerInventory.Grid.MaxWeight;
            float ratio = maxWeight > 0f ? currentWeight / maxWeight : 0f;

            // Update local tier state using current ratio
            currentTier = EncumbranceUtils.GetTier(ratio);

            if (weightValueText != null)
            {
                weightValueText.text = $"{currentWeight:F1} / {maxWeight:F1} kg";
            }

            if (weightBar != null)
            {
                weightBar.value = Mathf.Clamp01(ratio);
            }

            if (weightBarFill != null && weightGradient != null)
            {
                weightBarFill.color = weightGradient.Evaluate(ratio);
            }

            if (encumbranceTierText != null)
            {
                string tierName = EncumbranceUtils.GetTierName(currentTier);
                Color tierColor;

                switch (currentTier)
                {
                    case EncumbranceTier.Light:
                        tierColor = lightTierColor;
                        break;
                    case EncumbranceTier.Medium:
                        tierColor = mediumTierColor;
                        break;
                    case EncumbranceTier.Heavy:
                        tierColor = heavyTierColor;
                        break;
                    case EncumbranceTier.Snail:
                        tierColor = snailTierColor;
                        break;
                    default:
                        tierColor = lightTierColor;
                        break;
                }

                encumbranceTierText.text = tierName;
                encumbranceTierText.color = tierColor;
            }
        }

        private void RefreshBackpackInfo()
        {
            if (playerInventory == null) return;

            if (backpackSizeText != null && playerInventory.Grid != null)
            {
                backpackSizeText.text = $"{playerInventory.Grid.Width}x{playerInventory.Grid.Height}";
            }

            if (backpackNameText != null && playerInventory.CurrentBackpack != null)
            {
                backpackNameText.text = playerInventory.CurrentBackpack.name;
            }
        }

        private void SetupTooltips()
        {
            if (playerStats == null) return;

            // Strength
            float strengthDamageBonus = (1f + (playerStats.Strength.level - 1) * 0.1f); // 10% per level above 1
            float strengthBonus = (playerStats.Strength.level - 1) * 2f;
            string strDescription = $"Increases your maximum weight capacity and shove damage.\n\n" +
                                   $"<b>Current Bonus:</b> +{strengthBonus:F1} kg, +{strengthDamageBonus:F1} damage\n\n" +
                                   $"<color=#AAAAAA><b>How to Train:</b>\nWalk while carrying items (overloaded).</color>";
            
            AddTooltip(strengthIcon != null ? strengthIcon.gameObject : null, "STRENGTH", strDescription, strengthColor);
            AddTooltip(strengthLevelText != null ? strengthLevelText.gameObject : null, "STRENGTH", strDescription, strengthColor);

            // Endurance
            float staminaBonus = (playerStats.Endurance.level - 1) * 10f;
            float delayReduction = (playerStats.Endurance.level - 1) * 0.1f;
            string endDescription = $"Increases maximum stamina and recovery speed.\n\n" +
                                   $"<b>Current Bonus:</b> +{staminaBonus:F0} Max Stamina, -{delayReduction:F1}s Recovery Delay\n\n" +
                                   $"<color=#AAAAAA><b>How to Train:</b>\n Train your endurance by walking or sprinting. (underloaded).</color>";

            AddTooltip(enduranceIcon != null ? enduranceIcon.gameObject : null, "ENDURANCE", endDescription, enduranceColor);
            AddTooltip(enduranceLevelText != null ? enduranceLevelText.gameObject : null, "ENDURANCE", endDescription, enduranceColor);

            // Perception
            float perceptionBonus = (playerStats.Perception.level - 1) * 2f;
            string perDescription = $"Increases threat detection radius and ability to spot items.\n\n" +
                                   $"<b>Current Bonus:</b> +{perceptionBonus:F1}m Detection Radius\n\n" +
                                   $"<color=#AAAAAA><b>How to Train:</b>\nExtracting with valuables or spotting threats.</color>";

            AddTooltip(perceptionIcon != null ? perceptionIcon.gameObject : null, "PERCEPTION", perDescription, perceptionColor);
            AddTooltip(perceptionLevelText != null ? perceptionLevelText.gameObject : null, "PERCEPTION", perDescription, perceptionColor);
        }

        private void AddTooltip(GameObject target, string title, string description, Color? color = null)
        {
            if (target == null) return;

            StatTooltipTrigger trigger = target.GetComponent<StatTooltipTrigger>();
            if (trigger == null)
            {
                trigger = target.AddComponent<StatTooltipTrigger>();
            }

            trigger.title = title;
            trigger.description = description;
            if (color.HasValue)
            {
                trigger.titleColor = color.Value;
            }
        }
    }
}
