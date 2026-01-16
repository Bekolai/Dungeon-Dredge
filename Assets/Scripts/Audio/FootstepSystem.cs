using UnityEngine;
using DungeonDredge.Core;
using DungeonDredge.Player;

namespace DungeonDredge.Audio
{
    public class FootstepSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private AudioSource audioSource;

        [Header("Footstep Sounds")]
        [SerializeField] private FootstepSoundSet defaultSounds;
        [SerializeField] private FootstepSoundSet stoneSounds;
        [SerializeField] private FootstepSoundSet metalSounds;
        [SerializeField] private FootstepSoundSet dirtSounds;
        [SerializeField] private FootstepSoundSet waterSounds;

        [Header("Timing")]
        [SerializeField] private float baseStepInterval = 0.5f;
        [SerializeField] private float sprintIntervalMultiplier = 0.6f;
        [SerializeField] private float crouchIntervalMultiplier = 1.5f;

        [Header("Volume by Encumbrance")]
        [SerializeField] private float lightVolume = 0.3f;
        [SerializeField] private float mediumVolume = 0.5f;
        [SerializeField] private float heavyVolume = 0.8f;
        [SerializeField] private float snailVolume = 1.0f;

        [Header("Surface Detection")]
        [SerializeField] private float raycastDistance = 0.3f;
        [SerializeField] private LayerMask groundLayer;

        // State
        private float stepTimer;
        private SurfaceType currentSurface = SurfaceType.Stone;

        private void Awake()
        {
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            if (playerMovement == null || !playerMovement.IsMoving || !playerMovement.IsGrounded)
            {
                stepTimer = 0f;
                return;
            }

            // Calculate step interval
            float interval = baseStepInterval;
            
            if (playerMovement.IsSprinting)
            {
                interval *= sprintIntervalMultiplier;
            }
            else if (playerMovement.IsCrouching)
            {
                interval *= crouchIntervalMultiplier;
            }

            // Speed-based adjustment
            float speedRatio = playerMovement.CurrentSpeed / 5f; // Assuming 5 is base speed
            interval /= Mathf.Max(0.5f, speedRatio);

            stepTimer += Time.deltaTime;

            if (stepTimer >= interval)
            {
                stepTimer = 0f;
                PlayFootstep();
            }
        }

        private void PlayFootstep()
        {
            // Detect surface
            DetectSurface();

            // Get sound set
            FootstepSoundSet soundSet = GetSoundSetForSurface(currentSurface);
            if (soundSet == null || soundSet.footstepClips == null || soundSet.footstepClips.Length == 0)
            {
                soundSet = defaultSounds;
            }

            if (soundSet == null || soundSet.footstepClips == null || soundSet.footstepClips.Length == 0)
                return;

            // Get random clip
            AudioClip clip = soundSet.footstepClips[Random.Range(0, soundSet.footstepClips.Length)];

            // Calculate volume based on encumbrance
            float volume = GetVolumeForTier(playerMovement.CurrentTier);
            
            // Reduce for crouching
            if (playerMovement.IsCrouching)
            {
                volume *= 0.4f;
            }

            // Play
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(clip, volume);
        }

        private void DetectSurface()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                // Check for surface type tag or component
                var surfaceTag = hit.collider.GetComponent<SurfaceTag>();
                if (surfaceTag != null)
                {
                    currentSurface = surfaceTag.SurfaceType;
                }
                else
                {
                    // Fallback to tag-based detection
                    currentSurface = hit.collider.tag switch
                    {
                        "Metal" => SurfaceType.Metal,
                        "Dirt" => SurfaceType.Dirt,
                        "Water" => SurfaceType.Water,
                        "Wood" => SurfaceType.Wood,
                        _ => SurfaceType.Stone
                    };
                }
            }
        }

        private FootstepSoundSet GetSoundSetForSurface(SurfaceType surface)
        {
            return surface switch
            {
                SurfaceType.Stone => stoneSounds,
                SurfaceType.Metal => metalSounds,
                SurfaceType.Dirt => dirtSounds,
                SurfaceType.Water => waterSounds,
                _ => defaultSounds
            };
        }

        private float GetVolumeForTier(EncumbranceTier tier)
        {
            return tier switch
            {
                EncumbranceTier.Light => lightVolume,
                EncumbranceTier.Medium => mediumVolume,
                EncumbranceTier.Heavy => heavyVolume,
                EncumbranceTier.Snail => snailVolume,
                _ => mediumVolume
            };
        }
    }

    [System.Serializable]
    public class FootstepSoundSet
    {
        public string surfaceName;
        public AudioClip[] footstepClips;
        public AudioClip[] landingClips;
        public AudioClip[] scuffClips;
    }

    public enum SurfaceType
    {
        Stone,
        Metal,
        Dirt,
        Water,
        Wood,
        Grass
    }

    public class SurfaceTag : MonoBehaviour
    {
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Stone;
        public SurfaceType SurfaceType => surfaceType;
    }
}
