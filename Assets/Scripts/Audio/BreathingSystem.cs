using UnityEngine;
using DungeonDredge.Core;
using DungeonDredge.Player;

namespace DungeonDredge.Audio
{
    public class BreathingSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private StaminaSystem staminaSystem;
        [SerializeField] private AudioSource breathingSource;

        [Header("Voice Settings")]
        [SerializeField] private PlayerVoices playerVoices;

        [Header("Settings")]
        [SerializeField] private float exhaustedRecoveryThreshold = 0.2f;

        // State
        private bool isBreathingActive;
        private float currentBreathVolume;
        private AudioClip currentBreathClip;
        private float lastBreathTime;
        private float breathCooldown = 2f;
        private EncumbranceTier currentBreathingTier;
        private bool isRecoveringFromExhaustion = false;
        private bool isBreathingExhausted = false;

        private void Awake()
        {
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();
            if (staminaSystem == null)
                staminaSystem = GetComponent<StaminaSystem>();
            if (breathingSource == null)
                breathingSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (staminaSystem != null)
            {
                staminaSystem.OnStaminaDepleted += OnStaminaDepleted;
            }
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
            HandleBreathing();
        }

        private void HandleBreathing()
        {
            if (playerVoices == null || breathingSource == null || playerMovement == null)
                return;

            // Update exhaustion state with hysteresis
            if (staminaSystem != null)
            {
                if (staminaSystem.IsExhausted)
                {
                    isRecoveringFromExhaustion = true;
                }
                else if (staminaSystem.StaminaRatio >= exhaustedRecoveryThreshold)
                {
                    isRecoveringFromExhaustion = false;
                }
            }

            bool shouldBeExhausted = isRecoveringFromExhaustion;
            EncumbranceTier currentTier = playerMovement.CurrentTier;
            bool shouldBeHeavy = currentTier == EncumbranceTier.Snail ||
                                 (currentTier == EncumbranceTier.Heavy && staminaSystem != null &&
                                  staminaSystem.StaminaRatio < 0.7f);

            // Check if breathing state has changed
            bool stateChanged = (shouldBeExhausted != isBreathingExhausted) ||
                               (shouldBeHeavy && currentBreathingTier != currentTier) ||
                               (!shouldBeExhausted && !shouldBeHeavy && isBreathingActive);

            // Determine target breathing clip and volume
            AudioClip targetClip = null;
            float targetVolume = 0f;
            AudioClip[] clipArray = null;

            if (shouldBeExhausted)
            {
                clipArray = playerVoices.exhaustedBreathing;
                targetVolume = playerVoices.voiceVolume;
            }
            else if (shouldBeHeavy)
            {
                clipArray = playerVoices.heavyBreathing;
                targetVolume = playerVoices.voiceVolume * 0.7f;
            }
            else
            {
                clipArray = playerVoices.idleBreathing;
                targetVolume = playerVoices.voiceVolume * 0.5f;
            }

            if (clipArray != null && clipArray.Length > 0)
            {
                if (stateChanged || currentBreathClip == null)
                {
                    targetClip = clipArray[Random.Range(0, clipArray.Length)];
                }
                else
                {
                    targetClip = currentBreathClip;
                }
            }

            if (targetClip != null)
            {
                bool canPlayBreath = Time.time - lastBreathTime >= breathCooldown;

                if (stateChanged || (canPlayBreath && !breathingSource.isPlaying))
                {
                    if (stateChanged && breathingSource.isPlaying)
                    {
                        breathingSource.Stop();
                    }

                    breathingSource.pitch = Random.Range(playerVoices.pitchRange.x, playerVoices.pitchRange.y);
                    breathingSource.PlayOneShot(targetClip, targetVolume);
                    
                    isBreathingActive = true;
                    currentBreathClip = targetClip;
                    currentBreathVolume = targetVolume;
                    lastBreathTime = Time.time;
                    isBreathingExhausted = shouldBeExhausted;
                    currentBreathingTier = currentTier;

                    breathCooldown = Random.Range(2f, 4f);
                }
            }
            else
            {
                if (breathingSource.isPlaying)
                {
                    breathingSource.Stop();
                }
                isBreathingActive = false;
            }
        }

        private void OnStaminaDepleted()
        {
            if (playerVoices != null && playerVoices.exhaustedBreathing != null && playerVoices.exhaustedBreathing.Length > 0)
            {
                if (breathingSource != null && !breathingSource.isPlaying)
                {
                    AudioClip gasp = playerVoices.exhaustedBreathing[Random.Range(0, playerVoices.exhaustedBreathing.Length)];
                    breathingSource.pitch = Random.Range(0.8f, 1.2f);
                    breathingSource.PlayOneShot(gasp, playerVoices.voiceVolume);
                }
            }
        }
    }
}
