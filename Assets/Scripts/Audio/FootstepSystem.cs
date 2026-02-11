using UnityEngine;
using DungeonDredge.Core;
using DungeonDredge.Player;

namespace DungeonDredge.Audio
{
    /// <summary>
    /// Realistic footstep system that spawns audio at foot position with
    /// random volume variation and no consecutive clip repetition.
    /// </summary>
    public class FootstepSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightFoot;

        [Header("Footstep Sounds (ScriptableObject Assets)")]
        [SerializeField] private FootstepSoundSetAsset defaultSoundsAsset;
        [SerializeField] private FootstepSoundSetAsset stoneSoundsAsset;
        [SerializeField] private FootstepSoundSetAsset metalSoundsAsset;
        [SerializeField] private FootstepSoundSetAsset dirtSoundsAsset;
        [SerializeField] private FootstepSoundSetAsset waterSoundsAsset;
        [SerializeField] private FootstepSoundSetAsset grassSoundsAsset;
        [SerializeField] private FootstepSoundSetAsset gravelSoundsAsset;

        // Runtime cached sound sets
        private FootstepSoundSet defaultSounds;
        private FootstepSoundSet stoneSounds;
        private FootstepSoundSet metalSounds;
        private FootstepSoundSet dirtSounds;
        private FootstepSoundSet waterSounds;
        private FootstepSoundSet grassSounds;
        private FootstepSoundSet gravelSounds;

        [Header("Timing")]
        [SerializeField] private float baseStepInterval = 0.5f;
        [SerializeField] private float sprintIntervalMultiplier = 0.6f;
        [SerializeField] private float crouchIntervalMultiplier = 1.5f;

        [Header("Volume Settings")]
        [SerializeField] private float lightVolumeBase = 0.3f;
        [SerializeField] private float mediumVolumeBase = 0.5f;
        [SerializeField] private float heavyVolumeBase = 0.8f;
        [SerializeField] private float snailVolumeBase = 1.0f;
        [Tooltip("Random volume variation (±percentage)")]
        [SerializeField, Range(0f, 0.3f)] private float volumeVariation = 0.15f;

        [Header("Pitch Settings")]
        [SerializeField] private float basePitch = 1.0f;
        [Tooltip("Random pitch variation (±amount)")]
        [SerializeField, Range(0f, 0.2f)] private float pitchVariation = 0.1f;

        [Header("3D Audio Settings")]
        [SerializeField] private float spatialBlend = 1.0f; // 1 = full 3D
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

        [Header("Surface Detection")]
        [SerializeField] private float raycastDistance = 0.5f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Object Pooling")]
        [SerializeField] private int audioSourcePoolSize = 8;
        [SerializeField] private GameObject audioSourcePrefab;

        // State
        private float stepTimer;
        private SurfaceType currentSurface = SurfaceType.Stone;
        private bool isLeftFoot = true;
        private int lastClipIndex = -1;
        private AudioSource[] audioSourcePool;
        private int currentPoolIndex;

        // Cached hit point for foot placement
        private Vector3 lastFootHitPoint;

        private void Awake()
        {
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();

            // Create audio source pool for 3D positioned sounds
            InitializeAudioPool();

            // If foot transforms not assigned, use player position
            if (leftFoot == null) leftFoot = transform;
            if (rightFoot == null) rightFoot = transform;

            // Cache sound sets from ScriptableObject assets
            CacheSoundSets();
        }

        private void CacheSoundSets()
        {
            if (defaultSoundsAsset != null) defaultSounds = defaultSoundsAsset.ToSoundSet();
            if (stoneSoundsAsset != null) stoneSounds = stoneSoundsAsset.ToSoundSet();
            if (metalSoundsAsset != null) metalSounds = metalSoundsAsset.ToSoundSet();
            if (dirtSoundsAsset != null) dirtSounds = dirtSoundsAsset.ToSoundSet();
            if (waterSoundsAsset != null) waterSounds = waterSoundsAsset.ToSoundSet();
            if (grassSoundsAsset != null) grassSounds = grassSoundsAsset.ToSoundSet();
            if (gravelSoundsAsset != null) gravelSounds = gravelSoundsAsset.ToSoundSet();
        }

        private void InitializeAudioPool()
        {
            audioSourcePool = new AudioSource[audioSourcePoolSize];
            
            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                GameObject audioObj;
                
                if (audioSourcePrefab != null)
                {
                    audioObj = Instantiate(audioSourcePrefab, transform);
                }
                else
                {
                    audioObj = new GameObject($"FootstepAudio_{i}");
                    audioObj.transform.SetParent(transform);
                }

                AudioSource source = audioObj.GetComponent<AudioSource>();
                if (source == null)
                    source = audioObj.AddComponent<AudioSource>();

                // Configure for 3D spatial audio
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = spatialBlend;
                source.minDistance = minDistance;
                source.maxDistance = maxDistance;
                source.rolloffMode = rolloffMode;
                source.outputAudioMixerGroup = null; // Can be assigned in inspector

                audioSourcePool[i] = source;
            }
        }

        private void Update()
        {
            if (playerMovement == null || !playerMovement.IsMoving || !playerMovement.IsGrounded)
            {
                stepTimer = 0f;
                return;
            }

            // Calculate step interval based on movement state
            float interval = CalculateStepInterval();

            stepTimer += Time.deltaTime;

            if (stepTimer >= interval)
            {
                stepTimer = 0f;
                PlayFootstep();
            }
        }

        private float CalculateStepInterval()
        {
            float interval = baseStepInterval;
            
            if (playerMovement.IsSprinting)
            {
                interval *= sprintIntervalMultiplier;
            }
            else if (playerMovement.IsCrouching)
            {
                interval *= crouchIntervalMultiplier;
            }

            // Speed-based adjustment - faster movement = shorter intervals
            float speedRatio = playerMovement.CurrentSpeed / 5f;
            interval /= Mathf.Max(0.5f, speedRatio);

            return interval;
        }

        private void PlayFootstep()
        {
            // Detect surface at foot position
            Vector3 footPosition = DetectSurfaceAndGetPosition();

            // Get sound set for current surface
            FootstepSoundSet soundSet = GetSoundSetForSurface(currentSurface);
            if (soundSet == null || soundSet.footstepClips == null || soundSet.footstepClips.Length == 0)
            {
                soundSet = defaultSounds;
            }

            if (soundSet == null || soundSet.footstepClips == null || soundSet.footstepClips.Length == 0)
                return;

            // Get random clip (avoiding repetition)
            AudioClip clip = GetRandomClipNoRepeat(soundSet.footstepClips);
            if (clip == null) return;

            // Calculate volume with random variation
            float baseVolume = GetVolumeForTier(playerMovement.CurrentTier);
            
            // Reduce for crouching
            if (playerMovement.IsCrouching)
            {
                baseVolume *= 0.4f;
            }

            // Apply random variation
            float volumeVariationAmount = baseVolume * volumeVariation;
            float finalVolume = baseVolume + Random.Range(-volumeVariationAmount, volumeVariationAmount);
            finalVolume = Mathf.Clamp01(finalVolume);

            // Calculate random pitch
            float finalPitch = basePitch + Random.Range(-pitchVariation, pitchVariation);

            // Play at foot position
            PlayAtPosition(clip, footPosition, finalVolume, finalPitch);

            // Alternate feet
            isLeftFoot = !isLeftFoot;
        }

        private Vector3 DetectSurfaceAndGetPosition()
        {
            Transform currentFoot = isLeftFoot ? leftFoot : rightFoot;
            Vector3 rayOrigin = currentFoot.position + Vector3.up * 0.1f;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                lastFootHitPoint = hit.point;

                // Check for surface type component first
                var surfaceTag = hit.collider.GetComponent<SurfaceTag>();
                if (surfaceTag != null)
                {
                    currentSurface = surfaceTag.SurfaceType;
                }
                else
                {
                    // Check for physical material
                    if (hit.collider.sharedMaterial != null)
                    {
                        currentSurface = GetSurfaceFromPhysicMaterial(hit.collider.sharedMaterial);
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
                            "Grass" => SurfaceType.Grass,
                            "Gravel" => SurfaceType.Gravel,
                            _ => SurfaceType.Stone
                        };
                    }
                }

                return hit.point;
            }

            // Fallback to foot position
            return currentFoot.position;
        }

        private SurfaceType GetSurfaceFromPhysicMaterial(PhysicsMaterial material)
        {
            // Match by material name (case-insensitive)
            string matName = material.name.ToLower();

            if (matName.Contains("metal") || matName.Contains("iron") || matName.Contains("steel"))
                return SurfaceType.Metal;
            if (matName.Contains("dirt") || matName.Contains("earth") || matName.Contains("mud"))
                return SurfaceType.Dirt;
            if (matName.Contains("water") || matName.Contains("puddle"))
                return SurfaceType.Water;
            if (matName.Contains("wood") || matName.Contains("plank"))
                return SurfaceType.Wood;
            if (matName.Contains("grass") || matName.Contains("foliage"))
                return SurfaceType.Grass;
            if (matName.Contains("gravel") || matName.Contains("rubble"))
                return SurfaceType.Gravel;
            if (matName.Contains("stone") || matName.Contains("concrete") || matName.Contains("rock"))
                return SurfaceType.Stone;

            return SurfaceType.Stone;
        }

        private AudioClip GetRandomClipNoRepeat(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];

            // Avoid playing the same clip twice in a row
            int newIndex;
            int attempts = 0;
            do
            {
                newIndex = Random.Range(0, clips.Length);
                attempts++;
            } while (newIndex == lastClipIndex && attempts < 10);

            lastClipIndex = newIndex;
            return clips[newIndex];
        }

        private void PlayAtPosition(AudioClip clip, Vector3 position, float volume, float pitch)
        {
            // Get next available audio source from pool
            AudioSource source = audioSourcePool[currentPoolIndex];
            currentPoolIndex = (currentPoolIndex + 1) % audioSourcePoolSize;

            // Position at foot hit point
            source.transform.position = position;

            // Configure and play
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
        }

        private FootstepSoundSet GetSoundSetForSurface(SurfaceType surface)
        {
            FootstepSoundSet result = surface switch
            {
                SurfaceType.Stone => stoneSounds,
                SurfaceType.Metal => metalSounds,
                SurfaceType.Dirt => dirtSounds,
                SurfaceType.Water => waterSounds,
                SurfaceType.Grass => grassSounds,
                SurfaceType.Gravel => gravelSounds,
                SurfaceType.Wood => stoneSounds, // Fallback - add woodSounds if needed
                _ => defaultSounds
            };

            // Fallback to default if the specific set is null
            return result ?? defaultSounds;
        }

        private float GetVolumeForTier(EncumbranceTier tier)
        {
            return tier switch
            {
                EncumbranceTier.Light => lightVolumeBase,
                EncumbranceTier.Medium => mediumVolumeBase,
                EncumbranceTier.Heavy => heavyVolumeBase,
                EncumbranceTier.Snail => snailVolumeBase,
                _ => mediumVolumeBase
            };
        }

        /// <summary>
        /// Play a landing sound (e.g., after jumping)
        /// </summary>
        public void PlayLandingSound()
        {
            Vector3 landPosition = DetectSurfaceAndGetPosition();
            FootstepSoundSet soundSet = GetSoundSetForSurface(currentSurface) ?? defaultSounds;

            if (soundSet?.landingClips != null && soundSet.landingClips.Length > 0)
            {
                AudioClip clip = soundSet.landingClips[Random.Range(0, soundSet.landingClips.Length)];
                float volume = GetVolumeForTier(playerMovement.CurrentTier) * 1.2f; // Landing is louder
                float pitch = basePitch + Random.Range(-pitchVariation, pitchVariation);
                PlayAtPosition(clip, landPosition, Mathf.Clamp01(volume), pitch);
            }
        }

        /// <summary>
        /// Play a scuff/slide sound (e.g., when stopping suddenly)
        /// </summary>
        public void PlayScuffSound()
        {
            Vector3 scuffPosition = DetectSurfaceAndGetPosition();
            FootstepSoundSet soundSet = GetSoundSetForSurface(currentSurface) ?? defaultSounds;

            if (soundSet?.scuffClips != null && soundSet.scuffClips.Length > 0)
            {
                AudioClip clip = soundSet.scuffClips[Random.Range(0, soundSet.scuffClips.Length)];
                float volume = GetVolumeForTier(playerMovement.CurrentTier) * 0.8f;
                float pitch = basePitch + Random.Range(-pitchVariation, pitchVariation);
                PlayAtPosition(clip, scuffPosition, volume, pitch);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw foot positions
            if (leftFoot != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(leftFoot.position, 0.05f);
            }
            if (rightFoot != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(rightFoot.position, 0.05f);
            }

            // Draw last hit point
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lastFootHitPoint, 0.1f);

            // Draw raycast
            Gizmos.color = Color.yellow;
            Vector3 rayOrigin = (isLeftFoot ? leftFoot : rightFoot)?.position ?? transform.position;
            rayOrigin += Vector3.up * 0.1f;
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * raycastDistance);
        }
#endif
    }

    /// <summary>
    /// Sound set for a specific surface type
    /// </summary>
    [System.Serializable]
    public class FootstepSoundSet
    {
        public string surfaceName;
        [Tooltip("Multiple footstep clips for variation")]
        public AudioClip[] footstepClips;
        [Tooltip("Sound when landing from a jump")]
        public AudioClip[] landingClips;
        [Tooltip("Sound when sliding or stopping suddenly")]
        public AudioClip[] scuffClips;
    }

    /// <summary>
    /// Types of surfaces that affect footstep sounds
    /// </summary>
    public enum SurfaceType
    {
        Stone,
        Metal,
        Dirt,
        Water,
        Wood,
        Grass,
        Gravel
    }

    /// <summary>
    /// Component to tag surfaces with their type for footstep detection
    /// </summary>
    public class SurfaceTag : MonoBehaviour
    {
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Stone;
        public SurfaceType SurfaceType => surfaceType;
    }
}
