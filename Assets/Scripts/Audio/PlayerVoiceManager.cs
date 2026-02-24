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

        // State
        private bool wasMovingLastFrame;
        private float lastEffortSoundTime;
        private float effortSoundCooldown = 1.5f; // Minimum time between effort sounds

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

        }

        private void Start()
        {
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();
            if (staminaSystem == null)
                staminaSystem = GetComponent<StaminaSystem>();

        }

        private void Update()
        {
            if (playerVoices == null || playerMovement == null)
                return;

            HandleEffortSounds();
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