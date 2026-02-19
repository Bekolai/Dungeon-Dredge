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
        private AudioSource voiceSource;

        // State
        private bool isBreathingLoopPlaying;
        private bool wasMovingLastFrame;
        private float currentBreathVolume;
        private AudioClip currentBreathClip;

        private void Awake()
        {
            playerMovement = GetComponent<PlayerMovement>();
            staminaSystem = GetComponent<StaminaSystem>();

            // Create voice source
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = spatialBlend;
            voiceSource.minDistance = minDistance;
            voiceSource.maxDistance = maxDistance;
            voiceSource.rolloffMode = AudioRolloffMode.Linear;
            voiceSource.outputAudioMixerGroup = null;
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
            if (playerVoices == null || voiceSource == null)
                return;

            // Determine target breathing clip and volume
            AudioClip targetClip = null;
            float targetVolume = 0f;

            // Check exhaustion first
            if (staminaSystem != null && staminaSystem.IsExhausted)
            {
                if (playerVoices.exhaustedBreathing != null && playerVoices.exhaustedBreathing.Length > 0)
                {
                    targetClip = playerVoices.exhaustedBreathing[Random.Range(0, playerVoices.exhaustedBreathing.Length)];
                    targetVolume = playerVoices.voiceVolume;
                }
            }
            // Check encumbrance
            else if (playerMovement.CurrentTier == EncumbranceTier.Snail ||
                     (playerMovement.CurrentTier == EncumbranceTier.Heavy && staminaSystem != null &&
                      staminaSystem.StaminaRatio < 0.7f))
            {
                if (playerVoices.heavyBreathing != null && playerVoices.heavyBreathing.Length > 0)
                {
                    targetClip = playerVoices.heavyBreathing[Random.Range(0, playerVoices.heavyBreathing.Length)];
                    targetVolume = playerVoices.voiceVolume * 0.7f;
                }
            }
            else
            {
                // Normal breathing
                if (playerVoices.idleBreathing != null && playerVoices.idleBreathing.Length > 0)
                {
                    targetClip = playerVoices.idleBreathing[Random.Range(0, playerVoices.idleBreathing.Length)];
                    targetVolume = playerVoices.voiceVolume * 0.5f;
                }
            }

            // Handle loop playing
            if (targetClip != null)
            {
                if (!isBreathingLoopPlaying || targetClip != currentBreathClip)
                {
                    // Stop current if different
                    if (isBreathingLoopPlaying && voiceSource.isPlaying)
                    {
                        voiceSource.Stop();
                    }

                    voiceSource.pitch = Random.Range(playerVoices.pitchRange.x, playerVoices.pitchRange.y);
                    voiceSource.PlayOneShot(targetClip, targetVolume);
                    isBreathingLoopPlaying = true;
                    currentBreathClip = targetClip;

                    // Schedule next breath
                    float breathInterval = Random.Range(2f, 4f);
                    Invoke(nameof(TriggerNextBreath), breathInterval);
                }
            }
            else
            {
                isBreathingLoopPlaying = false;
            }
        }

        private void TriggerNextBreath()
        {
            if (playerVoices != null && voiceSource != null)
            {
                // Trigger a new breath sound
                HandleBreathing();
            }
        }

        /// <summary>
        /// Play grunt/grunt sounds when moving while encumbered
        /// </summary>
        private void HandleEffortSounds()
        {
            if (playerVoices == null || playerVoices.moveEffort == null || playerVoices.moveEffort.Length == 0)
                return;

            bool isMoving = playerMovement.IsMoving;
            bool isHeavy = playerMovement.CurrentTier == EncumbranceTier.Heavy ||
                           playerMovement.CurrentTier == EncumbranceTier.Snail;

            // Play effort when starting to move while heavy
            if (isMoving && isHeavy && !wasMovingLastFrame)
            {
                PlayRandomClip(playerVoices.moveEffort, playerVoices.voiceVolume * 0.6f);
            }

            // Play effort on movement while snail-tier
            if (playerMovement.CurrentTier == EncumbranceTier.Snail && isMoving)
            {
                // Random chance to play effort sound
                if (Random.value < 0.05f) // 5% chance per frame
                {
                    PlayRandomClip(playerVoices.moveEffort, playerVoices.voiceVolume * 0.5f);
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
            // Play gasp/exhausted sound
            if (playerVoices != null && playerVoices.exhaustedBreathing != null &&
                playerVoices.exhaustedBreathing.Length > 0)
            {
                AudioClip gasp = playerVoices.exhaustedBreathing[Random.Range(0, playerVoices.exhaustedBreathing.Length)];
                voiceSource.pitch = Random.Range(0.8f, 1.2f);
                voiceSource.PlayOneShot(gasp, playerVoices.voiceVolume);
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