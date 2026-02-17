using UnityEngine;
using DungeonDredge.Core;

namespace DungeonDredge.Player
{
    [System.Serializable]
    public class Stat
    {
        public int level = 1;
        public float currentXP = 0f;
        public int maxLevel = 50;

        public float XPToNextLevel => level * 100f;
        public float XPProgress => currentXP / XPToNextLevel;
        public bool IsMaxLevel => level >= maxLevel;

        public bool AddXP(float amount)
        {
            if (IsMaxLevel) return false;

            currentXP += amount;
            
            if (currentXP >= XPToNextLevel)
            {
                currentXP -= XPToNextLevel;
                level++;
                return true; // Level up occurred
            }
            return false;
        }

        public void SetLevel(int newLevel)
        {
            level = Mathf.Clamp(newLevel, 1, maxLevel);
            currentXP = 0f;
        }
    }

    public class PlayerStats : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private Stat strength = new Stat();
        [SerializeField] private Stat endurance = new Stat();
        [SerializeField] private Stat perception = new Stat();

        [Header("Strength Settings")]
        [SerializeField] private float baseWeightCapacity = 40f;
        [SerializeField] private float weightPerStrengthLevel = 2f;
        [SerializeField] private float overloadedXPPerMeter = 1f;

        [Header("Endurance Settings")]
        [SerializeField] private float staminaDepletionXP = 50f;
        [SerializeField] private float sprintXPPerSecond = 5f;

        [Header("Perception Settings")]
        [SerializeField] private float baseThreatRadius = 10f;
        [SerializeField] private float radiusPerPerceptionLevel = 2f;
        [SerializeField] private float rareItemXP = 25f;
        [SerializeField] private float extractionXPMultiplier = 1f;

        // Tracking for XP triggers
        private float distanceWhileOverloaded = 0f;
        private float sprintTime = 0f;
        private Vector3 lastPosition;
        private bool wasOverloaded = false;

        // References
        private PlayerMovement playerMovement;
        private StaminaSystem staminaSystem;

        // Properties
        public Stat Strength => strength;
        public Stat Endurance => endurance;
        public Stat Perception => perception;

        public float WeightCapacity => baseWeightCapacity + (strength.level - 1) * weightPerStrengthLevel;
        public float ThreatDetectionRadius => baseThreatRadius + (perception.level - 1) * radiusPerPerceptionLevel;

        // Events
        public System.Action<StatType, int> OnLevelUp;

        private void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
            staminaSystem = GetComponent<StaminaSystem>();
            lastPosition = transform.position;
        }

        private void Start()
        {
            // Subscribe to stamina events
            if (staminaSystem != null)
            {
                staminaSystem.OnStaminaDepleted += OnStaminaDepleted;
            }

            OnLevelUp += (type, level) => DungeonDredge.Audio.AudioManager.Instance?.PlayLevelUp();

            // Apply initial stats
            ApplyStatBonuses();
        }

        private void OnDestroy()
        {
            if (staminaSystem != null)
            {
                staminaSystem.OnStaminaDepleted -= OnStaminaDepleted;
            }
        }

        private void Update()
        {
            TrackStrengthXP();
            TrackEnduranceXP();
        }

        private void TrackStrengthXP()
        {
            if (playerMovement == null) return;

            // Check if overloaded (> 40% weight ratio)
            bool isOverloaded = playerMovement.CurrentTier != EncumbranceTier.Light;

            if (isOverloaded && playerMovement.IsMoving)
            {
                float distance = Vector3.Distance(transform.position, lastPosition);
                distanceWhileOverloaded += distance;

                // Award XP every 10 meters
                if (distanceWhileOverloaded >= 10f)
                {
                    float xpToAward = (distanceWhileOverloaded / 10f) * overloadedXPPerMeter;
                    AwardStrengthXP(xpToAward);
                    distanceWhileOverloaded = distanceWhileOverloaded % 10f;
                }

                wasOverloaded = true;
            }
            else if (!isOverloaded && wasOverloaded)
            {
                // Reset tracking when no longer overloaded
                wasOverloaded = false;
            }

            lastPosition = transform.position;
        }

        private void TrackEnduranceXP()
        {
            if (playerMovement == null) return;

            // Track sprint time for XP
            if (playerMovement.IsSprinting)
            {
                sprintTime += Time.deltaTime;
                
                // Award XP every second of sprinting
                if (sprintTime >= 1f)
                {
                    AwardEnduranceXP(sprintXPPerSecond);
                    sprintTime = 0f;
                }
            }
        }

        private void OnStaminaDepleted()
        {
            // Award endurance XP when stamina fully depletes
            AwardEnduranceXP(staminaDepletionXP);
        }

        public void AwardStrengthXP(float amount)
        {
            if (strength.AddXP(amount))
            {
                OnStatLevelUp(StatType.Strength, strength.level);
            }
            PublishStatChange(StatType.Strength);
        }

        public void AwardEnduranceXP(float amount)
        {
            if (endurance.AddXP(amount))
            {
                OnStatLevelUp(StatType.Endurance, endurance.level);
            }
            PublishStatChange(StatType.Endurance);
        }

        public void AwardPerceptionXP(float amount)
        {
            if (perception.AddXP(amount))
            {
                OnStatLevelUp(StatType.Perception, perception.level);
            }
            PublishStatChange(StatType.Perception);
        }

        /// <summary>
        /// Award perception XP when extracting with valuable items
        /// </summary>
        public void OnExtraction(int totalGoldValue, bool hasRareItems)
        {
            float xp = totalGoldValue * extractionXPMultiplier;
            if (hasRareItems)
            {
                xp += rareItemXP;
            }
            AwardPerceptionXP(xp);
        }

        /// <summary>
        /// Award perception XP when spotting hidden enemies
        /// </summary>
        public void OnEnemySpotted(bool wasHidden)
        {
            if (wasHidden)
            {
                AwardPerceptionXP(rareItemXP);
            }
        }

        private void OnStatLevelUp(StatType statType, int newLevel)
        {
            Debug.Log($"{statType} leveled up to {newLevel}!");
            OnLevelUp?.Invoke(statType, newLevel);
            ApplyStatBonuses();
        }

        private void ApplyStatBonuses()
        {
            // Update endurance bonus to stamina system
            if (staminaSystem != null)
            {
                staminaSystem.SetEnduranceLevel(endurance.level);
            }
        }

        private void PublishStatChange(StatType statType)
        {
            Stat stat = statType switch
            {
                StatType.Strength => strength,
                StatType.Endurance => endurance,
                StatType.Perception => perception,
                _ => null
            };

            if (stat != null)
            {
                EventBus.Publish(new PlayerStatChangedEvent
                {
                    StatType = statType,
                    NewLevel = stat.level,
                    CurrentXP = stat.currentXP
                });
            }
        }

        #region Save/Load

        public StatsSaveData GetSaveData()
        {
            return new StatsSaveData
            {
                strengthLevel = strength.level,
                strengthXP = strength.currentXP,
                enduranceLevel = endurance.level,
                enduranceXP = endurance.currentXP,
                perceptionLevel = perception.level,
                perceptionXP = perception.currentXP
            };
        }

        public void LoadSaveData(StatsSaveData data)
        {
            strength.level = data.strengthLevel;
            strength.currentXP = data.strengthXP;
            endurance.level = data.enduranceLevel;
            endurance.currentXP = data.enduranceXP;
            perception.level = data.perceptionLevel;
            perception.currentXP = data.perceptionXP;

            ApplyStatBonuses();
        }

        #endregion
    }

    [System.Serializable]
    public class StatsSaveData
    {
        public int strengthLevel = 1;
        public float strengthXP = 0f;
        public int enduranceLevel = 1;
        public float enduranceXP = 0f;
        public int perceptionLevel = 1;
        public float perceptionXP = 0f;
    }
}
