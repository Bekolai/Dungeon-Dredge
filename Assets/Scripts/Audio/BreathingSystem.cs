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

        [Header("Breathing Sounds")]
        [SerializeField] private AudioClip normalBreathingLoop;
        [SerializeField] private AudioClip heavyBreathingLoop;
        [SerializeField] private AudioClip exhaustedBreathingLoop;
        [SerializeField] private AudioClip[] gasps;

        [Header("Settings")]
        [SerializeField] private float snailBreathingVolume = 0.5f;
        [SerializeField] private float exhaustedBreathingVolume = 0.8f;
        [SerializeField] private float staminaThresholdForHeavy = 0.5f;
        [SerializeField] private float staminaThresholdForExhausted = 0.2f;
        [SerializeField] private float fadeSpeed = 2f;

        // State
        private AudioClip currentLoop;
        private float targetVolume = 0f;
        private bool wasExhausted = false;

        private void Awake()
        {
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();
            if (staminaSystem == null)
                staminaSystem = GetComponent<StaminaSystem>();
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
            UpdateBreathingState();
            UpdateBreathingVolume();
        }

        private void UpdateBreathingState()
        {
            AudioClip targetLoop = null;
            targetVolume = 0f;

            // Check encumbrance tier
            if (playerMovement != null && playerMovement.CurrentTier == EncumbranceTier.Snail)
            {
                targetLoop = heavyBreathingLoop;
                targetVolume = snailBreathingVolume;
            }

            // Check stamina
            if (staminaSystem != null)
            {
                float staminaRatio = staminaSystem.StaminaRatio;

                if (staminaRatio < staminaThresholdForExhausted)
                {
                    targetLoop = exhaustedBreathingLoop;
                    targetVolume = exhaustedBreathingVolume;
                }
                else if (staminaRatio < staminaThresholdForHeavy)
                {
                    targetLoop = heavyBreathingLoop;
                    targetVolume = Mathf.Max(targetVolume, 0.4f);
                }
            }

            // Switch loop if needed
            if (targetLoop != currentLoop)
            {
                currentLoop = targetLoop;
                if (breathingSource != null)
                {
                    if (targetLoop != null)
                    {
                        breathingSource.clip = targetLoop;
                        breathingSource.loop = true;
                        if (!breathingSource.isPlaying)
                            breathingSource.Play();
                    }
                    else
                    {
                        breathingSource.Stop();
                    }
                }
            }
        }

        private void UpdateBreathingVolume()
        {
            if (breathingSource == null) return;

            breathingSource.volume = Mathf.Lerp(
                breathingSource.volume, 
                targetVolume, 
                Time.deltaTime * fadeSpeed);

            // Stop if volume is too low
            if (breathingSource.volume < 0.01f && targetVolume == 0f)
            {
                breathingSource.Stop();
            }
        }

        private void OnStaminaDepleted()
        {
            // Play gasp sound
            if (gasps != null && gasps.Length > 0 && !wasExhausted)
            {
                AudioClip gasp = gasps[Random.Range(0, gasps.Length)];
                AudioManager.Instance?.PlaySoundAt(gasp, transform.position, 0.5f);
            }
            wasExhausted = true;
        }
    }
}
