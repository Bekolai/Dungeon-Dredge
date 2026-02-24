using UnityEngine;
using DungeonDredge.Core;

namespace DungeonDredge.Player
{
    public class StaminaSystem : MonoBehaviour
    {
        [Header("Stamina Settings")]
        [SerializeField] private float baseMaxStamina = 100f;
        [SerializeField] private float staminaPerEnduranceLevel = 10f;

        [Header("Drain Rates")]
        [SerializeField] private float sprintDrainRate = 15f;
        [SerializeField] private float snailWalkDrainRate = 5f;
        [SerializeField] private float jumpStaminaCost = 10f;
        [SerializeField] private float shoveStaminaCost = 15f;

        [Header("Recovery Settings")]
        [SerializeField] private float baseRecoveryRate = 20f;
        [SerializeField] private float recoveryDelay = 1f;
        [SerializeField] private float recoveryDelayPerEnduranceLevel = 0.1f;

        [Header("Thresholds")]
        [SerializeField] private float minimumSprintStamina = 10f;


        // State
        private float currentStamina;
        private float maxStamina;
        private float timeSinceLastDrain;
        private int enduranceLevel = 1;
        private bool isRecovering = true;

        // Reference to movement for encumbrance info
        private PlayerMovement playerMovement;

        // Properties
        public float CurrentStamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float StaminaRatio => currentStamina / maxStamina;
        public bool CanSprint => currentStamina > minimumSprintStamina;
        public bool CanJump => currentStamina >= jumpStaminaCost && 
                              (playerMovement == null || playerMovement.CurrentTier != EncumbranceTier.Snail);
        public bool IsExhausted => currentStamina <= 0f;
        public bool CanShove => currentStamina >= shoveStaminaCost;

        // Events
        public System.Action OnStaminaDepleted;
        public System.Action OnStaminaRecovered;

        private void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
            CalculateMaxStamina();
            currentStamina = maxStamina;
        }

        private void Update()
        {
            // Check if we should drain stamina
            bool shouldDrain = ShouldDrainStamina();

            if (shouldDrain)
            {
                DrainStamina();
                timeSinceLastDrain = 0f;
                isRecovering = false;
            }
            else
            {
                timeSinceLastDrain += Time.deltaTime;
                
                // Start recovery after delay
                float currentRecoveryDelay = recoveryDelay - (enduranceLevel - 1) * recoveryDelayPerEnduranceLevel;
                currentRecoveryDelay = Mathf.Max(0.2f, currentRecoveryDelay);

                if (timeSinceLastDrain >= currentRecoveryDelay)
                {
                    RecoverStamina();
                }
            }

            // Publish stamina update
            PublishStaminaUpdate();
        }


        private bool ShouldDrainStamina()
        {
            if (playerMovement == null) return false;

            // Drain when sprinting
            if (playerMovement.IsSprinting && playerMovement.IsMoving)
                return true;

            // Drain when walking in Snail tier
            if (playerMovement.CurrentTier == EncumbranceTier.Snail && playerMovement.IsMoving)
                return true;

            return false;
        }

        private void DrainStamina()
        {
            if (playerMovement == null) return;

            float drainAmount = 0f;

            if (playerMovement.IsSprinting)
            {
                drainAmount = sprintDrainRate * Time.deltaTime;
            }
            else if (playerMovement.CurrentTier == EncumbranceTier.Snail)
            {
                drainAmount = snailWalkDrainRate * Time.deltaTime;
            }

            float previousStamina = currentStamina;
            currentStamina -= drainAmount;
            currentStamina = Mathf.Max(0f, currentStamina);

            if (previousStamina > 0f && currentStamina <= 0f)
            {
                OnStaminaDepleted?.Invoke();
            }
        }

        private void RecoverStamina()
        {
            if (currentStamina >= maxStamina) return;

            float recoveryAmount = baseRecoveryRate * Time.deltaTime;

            // Reduce recovery rate based on encumbrance
            if (playerMovement != null)
            {
                switch (playerMovement.CurrentTier)
                {
                    case EncumbranceTier.Heavy:
                        recoveryAmount *= 0.7f;
                        break;
                    case EncumbranceTier.Snail:
                        recoveryAmount *= 0.4f;
                        break;
                }
            }

            float previousStamina = currentStamina;
            currentStamina += recoveryAmount;
            currentStamina = Mathf.Min(maxStamina, currentStamina);

            // Check if we've recovered from exhaustion
            if (previousStamina <= 0f && currentStamina > minimumSprintStamina)
            {
                if (!isRecovering)
                {
                    isRecovering = true;
                    OnStaminaRecovered?.Invoke();
                }
            }
        }

        public void ConsumeStamina(float amount)
        {
            float previousStamina = currentStamina;
            currentStamina -= amount;
            currentStamina = Mathf.Max(0f, currentStamina);
            timeSinceLastDrain = 0f;

            if (previousStamina > 0f && currentStamina <= 0f)
            {
                OnStaminaDepleted?.Invoke();
            }
        }

        public void UseJumpStamina()
        {
            ConsumeStamina(jumpStaminaCost);
        }

        public bool TryUseJumpStamina()
        {
            if (!CanJump)
                return false;

            UseJumpStamina();
            return true;
        }

        public bool TryUseShoveStamina()
        {
            if (!CanShove)
                return false;

            ConsumeStamina(shoveStaminaCost);
            return true;
        }

        public void SetEnduranceLevel(int level)
        {
            enduranceLevel = Mathf.Max(1, level);
            float previousMax = maxStamina;
            CalculateMaxStamina();

            // Scale current stamina proportionally
            if (previousMax > 0)
            {
                float ratio = currentStamina / previousMax;
                currentStamina = maxStamina * ratio;
            }
        }

        private void CalculateMaxStamina()
        {
            maxStamina = baseMaxStamina + (enduranceLevel - 1) * staminaPerEnduranceLevel;
        }

        private void PublishStaminaUpdate()
        {
            EventBus.Publish(new StaminaChangedEvent
            {
                CurrentStamina = currentStamina,
                MaxStamina = maxStamina,
                Ratio = StaminaRatio
            });
        }

        /// <summary>
        /// Fully restore stamina (e.g., when extracting or using consumable)
        /// </summary>
        public void RestoreStamina()
        {
            currentStamina = maxStamina;
            isRecovering = true;
        }

        /// <summary>
        /// Restore a portion of stamina
        /// </summary>
        public void RestoreStamina(float amount)
        {
            currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
        }
    }
}
