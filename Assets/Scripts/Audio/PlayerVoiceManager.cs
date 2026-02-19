using UnityEngine;
using DungeonDredge.Core;
using DungeonDredge.Player;

namespace DungeonDredge.Audio
{
    /// <summary>
    /// Manages player voice effects: breathing, grunting, damage sounds, etc.
    /// Uses PlayerVoices ScriptableObject for sound assets.
    /// </summary>
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(StaminaSystem))]
    public class PlayerVoiceManager : MonoBehaviour
    {
        [Header("Voice Settings")]
        [SerializeField] private PlayerVoices playerVoices;
        [SerializeField] private float spatialBlend = 1f; // Full 3D audio
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 25f;

        [Header("Breathing Settings")]
        [SerializeField] private float breathFadeSpeed = 3f;

        // Components
        private PlayerMovement playerMovement;
        private StaminaSystem staminaSystem;
        private AudioSource voiceSource; // For one-shot sounds (effort, damage, etc.)
        private AudioSource breathingSource; // Dedicated source for breathing

        // State
        private bool isBreathingActive;
        private bool wasMovingLastFrame;
        private float currentBreathVolume;
        private AudioClip currentBreathClip;
        private float lastBreathTime;
        private float breathCooldown = 2f; // Minimum time between breaths
        private float lastEffortSoundTime;
        private float effortSoundCooldown = 1.5f; // Minimum time between effort sounds
        private EncumbranceTier currentBreathingTier; // Track which tier we're breathing for
        private bool isExhaustedBreathing; // Track if we're in exhausted state

        private void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
            staminaSystem = GetComponent<StaminaSystem>();

            // Create voice source for one-shot sounds
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = spatialBlend;
            voiceSource.minDistance = minDistance;
            voiceSource.maxDistance = maxDistance;
            voiceSource.rolloffMode = AudioRolloffMode.Linear;
            voiceSource.outputAudioMixerGroup = null;

            // Create dedicated breathing source
            breathingSource = gameObject.AddComponent<AudioSource>();
            breathingSource.playOnAwake = false;
            breathingSource.loop = false;
            breathingSource.spatialBlend = spatialBlend;
            breathingSource.minDistance = minDistance;
            breathingSource.maxDistance = maxDistance;
            breathingSource.rolloffMode = AudioRolloffMode.Linear;
            breathingSource.outputAudioMixerGroup = null;
        }

        private void Start()
        {
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();
            if (staminaSystem == null)
                staminaSystem = GetComponent<StaminaSystem>();

            // Subscribe to events
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
            if (playerVoices == null || playerMovement == null)
                return;

            HandleBreathing();
            HandleEffortSounds();
        }

        /// <summary>
        /// Handle breathing based on encumbrance and stamina
        /// </summary>
        private void HandleBreathing()
        {
            if (playerVoices == null || breathingSource == null)
                return;

            // Determine current breathing state
            bool shouldBeExhausted = staminaSystem != null && staminaSystem.IsExhausted;
            EncumbranceTier currentTier = playerMovement.CurrentTier;
            bool shouldBeHeavy = currentTier == EncumbranceTier.Snail ||
                                 (currentTier == EncumbranceTier.Heavy && staminaSystem != null &&
                                  staminaSystem.StaminaRatio < 0.7f);

            // Check if breathing state has changed
            bool stateChanged = (shouldBeExhausted != isExhaustedBreathing) ||
                               (shouldBeHeavy && currentBreathingTier != currentTier) ||
                               (!shouldBeExhausted && !shouldBeHeavy && isBreathingActive);

            // Determine target breathing clip and volume
            AudioClip targetClip = null;
            float targetVolume = 0f;
            AudioClip[] clipArray = null;

            // Check exhaustion first (highest priority)
            if (shouldBeExhausted)
            {
                clipArray = playerVoices.exhaustedBreathing;
                targetVolume = playerVoices.voiceVolume;
            }
            // Check encumbrance
            else if (shouldBeHeavy)
            {
                clipArray = playerVoices.heavyBreathing;
                targetVolume = playerVoices.voiceVolume * 0.7f;
            }
            else
            {
                // Normal breathing
                clipArray = playerVoices.idleBreathing;
                targetVolume = playerVoices.voiceVolume * 0.5f;
            }

            // Get a clip if available
            if (clipArray != null && clipArray.Length > 0)
            {
                // Only change clip if state changed, otherwise keep current
                if (stateChanged || currentBreathClip == null)
                {
                    targetClip = clipArray[Random.Range(0, clipArray.Length)];
                }
                else
                {
                    targetClip = currentBreathClip; // Keep same clip
                }
            }

            // Handle breathing playback
            if (targetClip != null)
            {
                // Check if enough time has passed since last breath
                bool canPlayBreath = Time.time - lastBreathTime >= breathCooldown;

                // Play if state changed or if cooldown expired
                if (stateChanged || (canPlayBreath && !breathingSource.isPlaying))
                {
                    // Stop current breathing if state changed
                    if (stateChanged && breathingSource.isPlaying)
                    {
                        breathingSource.Stop();
                    }

                    // Play new breath sound
                    breathingSource.pitch = Random.Range(playerVoices.pitchRange.x, playerVoices.pitchRange.y);
                    breathingSource.PlayOneShot(targetClip, targetVolume);
                    
                    isBreathingActive = true;
                    currentBreathClip = targetClip;
                    currentBreathVolume = targetVolume;
                    lastBreathTime = Time.time;
                    isExhaustedBreathing = shouldBeExhausted;
                    currentBreathingTier = currentTier;

                    // Schedule next breath with random interval
                    breathCooldown = Random.Range(2f, 4f);
                }
            }
            else
            {
                // No breathing needed
                if (breathingSource.isPlaying)
                {
                    breathingSource.Stop();
                }
                isBreathingActive = false;
            }
        }

        /// <summary>
        /// Play grunt/grunt sounds when moving while encumbered
        /// </summary>
        private void HandleEffortSounds()
        {
            if (playerVoices == null || playerVoices.moveEffort == null || playerVoices.moveEffort.Length == 0)
                return;

            // Don't play effort sounds if breathing is currently playing (prevents overlap)
            if (breathingSource != null && breathingSource.isPlaying)
                return;

            bool isMoving = playerMovement.IsMoving;
            bool isHeavy = playerMovement.CurrentTier == EncumbranceTier.Heavy ||
                           playerMovement.CurrentTier == EncumbranceTier.Snail;

            // Check cooldown before playing any effort sounds
            bool canPlayEffort = Time.time - lastEffortSoundTime >= effortSoundCooldown;

            // Play effort when starting to move while heavy
            if (isMoving && isHeavy && !wasMovingLastFrame && canPlayEffort)
            {
                PlayRandomClip(playerVoices.moveEffort, playerVoices.voiceVolume * 0.6f);
                lastEffortSoundTime = Time.time;
            }

            // Play effort on movement while snail-tier (with cooldown)
            if (playerMovement.CurrentTier == EncumbranceTier.Snail && isMoving && canPlayEffort)
            {
                // Random chance to play effort sound (reduced from 5% per frame to occasional)
                if (Random.value < 0.02f) // 2% chance per frame, but only if cooldown allows
                {
                    PlayRandomClip(playerVoices.moveEffort, playerVoices.voiceVolume * 0.5f);
                    lastEffortSoundTime = Time.time;
                }
            }

            wasMovingLastFrame = isMoving;
        }

        /// <summary>
        /// Play damage sound
        /// </summary>
        public void PlayDamageSound(float damageAmount)
        {
            if (playerVoices == null) return;

            AudioClip[] damageClips = damageAmount > 20 ? playerVoices.heavyDamage : playerVoices.lightDamage;
            PlayRandomClip(damageClips, playerVoices.voiceVolume);
        }

        /// <summary>
        /// Play jump sound
        /// </summary>
        public void PlayJumpSound()
        {
            if (playerVoices == null || playerVoices.jump == null || playerVoices.jump.Length == 0)
                return;

            PlayRandomClip(playerVoices.jump, playerVoices.voiceVolume * 0.7f);
        }

        /// <summary>
        /// Play shove sound
        /// </summary>
        public void PlayShoveSound()
        {
            if (playerVoices == null || playerVoices.shove == null || playerVoices.shove.Length == 0)
                return;

            PlayRandomClip(playerVoices.shove, playerVoices.voiceVolume);
        }

        /// <summary>
        /// Play a random clip from array with pitch variation
        /// </summary>
        private void PlayRandomClip(AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0 || voiceSource == null)
                return;

            // Don't overlap if already playing (prevents sound stacking)
            if (voiceSource.isPlaying)
                return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;

            voiceSource.pitch = Random.Range(playerVoices.pitchRange.x, playerVoices.pitchRange.y);
            voiceSource.PlayOneShot(clip, volume);
        }

        private void OnStaminaDepleted()
        {
            // Play gasp/exhausted sound (one-shot, separate from breathing loop)
            if (playerVoices != null && playerVoices.exhaustedBreathing != null &&
                playerVoices.exhaustedBreathing.Length > 0)
            {
                // Use breathing source for this gasp since it's breathing-related
                // But only if breathing source isn't already playing
                if (breathingSource != null && !breathingSource.isPlaying)
                {
                    AudioClip gasp = playerVoices.exhaustedBreathing[Random.Range(0, playerVoices.exhaustedBreathing.Length)];
                    breathingSource.pitch = Random.Range(0.8f, 1.2f);
                    breathingSource.PlayOneShot(gasp, playerVoices.voiceVolume);
                }
            }
        }

        /// <summary>
        /// Play a one-shot voice effect at the player's position
        /// </summary>
        public void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip == null || voiceSource == null) return;

            voiceSource.pitch = Random.Range(playerVoices.pitchRange.x, playerVoices.pitchRange.y);
            voiceSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Set the voice volume multiplier
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            if (playerVoices != null)
            {
                playerVoices.voiceVolume = Mathf.Clamp01(volume);
            }
        }
    }
}