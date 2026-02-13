using System.Collections.Generic;
using DungeonDredge.Core;
using DungeonDredge.Player;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonDredge.Audio
{
    /// <summary>
    /// Shared footstep audio system for both player and enemies.
    /// Supports movement-timer and animation-event triggering.
    /// </summary>
    public class FootstepSystem : MonoBehaviour
    {
        public enum FootstepActorType
        {
            AutoDetect,
            Player,
            Enemy
        }

        [System.Serializable]
        public class SurfaceSoundBinding
        {
            public SurfaceType surfaceType = SurfaceType.Stone;
            public FootstepSoundSetAsset soundSetAsset;
        }

        [Header("Actor")]
        [SerializeField] private FootstepActorType actorType = FootstepActorType.AutoDetect;

        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform leftFoot;
        [SerializeField] private Transform rightFoot;

        [Header("Triggering")]
        [Tooltip("If enabled, footsteps can be played automatically by movement speed over time.")]
        [SerializeField] private bool useMovementTimer = false;
        [Tooltip("Allow external calls (animation events) to trigger footsteps.")]
        [SerializeField] private bool allowExternalTriggers = true;
        [Tooltip("For animation events, require the selected foot to be near ground before playing.")]
        [SerializeField] private bool requireGroundContactForExternalTriggers = true;
        [SerializeField] private float externalTriggerGroundDistance = 0.3f;

        [Header("Surface Sound Sets")]
        [SerializeField] private FootstepSoundSetAsset defaultSoundsAsset;
        [SerializeField] private SurfaceSoundBinding[] surfaceSoundSets;

        [Header("Timing")]
        [SerializeField] private float baseStepInterval = 0.5f;
        [SerializeField] private float sprintIntervalMultiplier = 0.65f;
        [SerializeField] private float crouchIntervalMultiplier = 1.45f;
        [SerializeField] private float referenceMoveSpeed = 5f;
        [SerializeField] private float minSpeedToStep = 0.2f;

        [Header("Volume Settings")]
        [SerializeField] private float enemyBaseVolume = 0.55f;
        [SerializeField] private float lightVolumeBase = 0.3f;
        [SerializeField] private float mediumVolumeBase = 0.5f;
        [SerializeField] private float heavyVolumeBase = 0.8f;
        [SerializeField] private float snailVolumeBase = 1.0f;
        [SerializeField] private float crouchVolumeMultiplier = 0.4f;
        [SerializeField] private float crouchAdditionalVolumeMultiplier = 0.8f;
        [SerializeField] private float sprintVolumeMultiplier = 1.15f;
        [SerializeField] private Vector2 randomVolumeRange = new Vector2(0.9f, 1.1f);

        [Header("Pitch Settings")]
        [SerializeField] private Vector2 randomPitchRange = new Vector2(0.94f, 1.06f);
        [SerializeField] private float crouchPitchMultiplier = 0.96f;

        [Header("3D Audio Settings")]
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

        [Header("Distance Hearing Filter")]
        [Tooltip("Additional distance-based volume filter on top of AudioSource rolloff.")]
        [SerializeField] private bool applyDistanceVolumeFilter = true;
        [SerializeField] private Transform listenerOverride;
        [SerializeField] private float distanceNear = 1.5f;
        [SerializeField] private float distanceFar = 20f;
        [SerializeField] private AnimationCurve distanceVolumeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [Tooltip("Skip playback if final volume after distance filtering is below this value.")]
        [SerializeField, Range(0f, 0.2f)] private float cullVolumeThreshold = 0.02f;

        [Header("Surface Detection")]
        [SerializeField] private float raycastDistance = 0.6f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Object Pooling")]
        [SerializeField] private int audioSourcePoolSize = 8;
        [SerializeField] private GameObject audioSourcePrefab;

        private float stepTimer;
        private bool isLeftFoot = true;
        private int lastClipIndex = -1;
        private SurfaceType currentSurface = SurfaceType.Stone;
        private Vector3 lastFootHitPoint;

        private AudioSource[] audioSourcePool;
        private int currentPoolIndex;

        private FootstepSoundSet defaultSounds;
        private readonly Dictionary<SurfaceType, FootstepSoundSet> cachedSurfaceSets = new Dictionary<SurfaceType, FootstepSoundSet>();
        private AudioListener cachedListener;

        private void Awake()
        {
            AutoAssignReferences();

            if (leftFoot == null) leftFoot = transform;
            if (rightFoot == null) rightFoot = transform;

            InitializeAudioPool();
            CacheSoundSets();
        }

        private void Update()
        {
            if (!useMovementTimer)
                return;

            if (!CanAutoStep())
            {
                stepTimer = 0f;
                return;
            }

            stepTimer += Time.deltaTime;
            if (stepTimer >= CalculateStepInterval())
            {
                stepTimer = 0f;
                PlayFootstepInternal(isLeftFoot, true);
            }
        }

        public void TriggerFootstep()
        {
            if (!allowExternalTriggers)
                return;

            PlayFootstepInternal(isLeftFoot, true);
        }

        public void TriggerLeftFootstep()
        {
            TriggerFootstepForFoot(true);
        }

        public void TriggerRightFootstep()
        {
            TriggerFootstepForFoot(false);
        }

        public void PlayLandingSound()
        {
            Vector3 position = DetectSurfaceAndGetPosition(isLeftFoot);
            FootstepSoundSet soundSet = GetSoundSetForSurface(currentSurface);
            if (soundSet?.landingClips == null || soundSet.landingClips.Length == 0)
                return;

            AudioClip clip = soundSet.landingClips[Random.Range(0, soundSet.landingClips.Length)];
            float volume = Mathf.Clamp01(GetBaseVolume() * soundSet.volumeMultiplier * 1.2f * Random.Range(randomVolumeRange.x, randomVolumeRange.y));
            float pitch = soundSet.basePitch * Random.Range(randomPitchRange.x, randomPitchRange.y);
            PlayAtPosition(clip, position, volume, pitch);
        }

        public void PlayScuffSound()
        {
            Vector3 position = DetectSurfaceAndGetPosition(isLeftFoot);
            FootstepSoundSet soundSet = GetSoundSetForSurface(currentSurface);
            if (soundSet?.scuffClips == null || soundSet.scuffClips.Length == 0)
                return;

            AudioClip clip = soundSet.scuffClips[Random.Range(0, soundSet.scuffClips.Length)];
            float volume = Mathf.Clamp01(GetBaseVolume() * soundSet.volumeMultiplier * 0.85f * Random.Range(randomVolumeRange.x, randomVolumeRange.y));
            float pitch = soundSet.basePitch * Random.Range(randomPitchRange.x, randomPitchRange.y);
            PlayAtPosition(clip, position, volume, pitch);
        }

        private void AutoAssignReferences()
        {
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();

            if (navMeshAgent == null)
                navMeshAgent = GetComponent<NavMeshAgent>();

            if (characterController == null)
                characterController = GetComponent<CharacterController>();

            if (actorType == FootstepActorType.AutoDetect)
            {
                actorType = playerMovement != null ? FootstepActorType.Player : FootstepActorType.Enemy;
            }
        }

        private void CacheSoundSets()
        {
            defaultSounds = defaultSoundsAsset != null ? defaultSoundsAsset.ToSoundSet() : null;
            cachedSurfaceSets.Clear();

            if (surfaceSoundSets == null)
                return;

            for (int i = 0; i < surfaceSoundSets.Length; i++)
            {
                SurfaceSoundBinding binding = surfaceSoundSets[i];
                if (binding == null || binding.soundSetAsset == null)
                    continue;

                cachedSurfaceSets[binding.surfaceType] = binding.soundSetAsset.ToSoundSet();
            }
        }

        private void InitializeAudioPool()
        {
            if (audioSourcePoolSize < 1)
                audioSourcePoolSize = 1;

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

                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = spatialBlend;
                source.minDistance = minDistance;
                source.maxDistance = maxDistance;
                source.rolloffMode = rolloffMode;
                audioSourcePool[i] = source;
            }
        }

        private bool CanAutoStep()
        {
            if (actorType == FootstepActorType.Player && playerMovement != null)
            {
                return playerMovement.IsGrounded && playerMovement.IsMoving && playerMovement.CurrentSpeed > minSpeedToStep;
            }

            float speed = GetCurrentSpeed();
            return speed > minSpeedToStep && IsGroundedForNonPlayer();
        }

        private bool IsGroundedForNonPlayer()
        {
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            return Physics.Raycast(origin, Vector3.down, raycastDistance, GetGroundMask(), QueryTriggerInteraction.Ignore);
        }

        private float CalculateStepInterval()
        {
            float interval = baseStepInterval;
            float speed = GetCurrentSpeed();

            if (actorType == FootstepActorType.Player && playerMovement != null)
            {
                if (playerMovement.IsSprinting)
                    interval *= sprintIntervalMultiplier;
                else if (playerMovement.IsCrouching)
                    interval *= crouchIntervalMultiplier;
            }

            float speedRatio = speed / Mathf.Max(0.1f, referenceMoveSpeed);
            speedRatio = Mathf.Clamp(speedRatio, 0.5f, 2.5f);
            return interval / speedRatio;
        }

        private float GetCurrentSpeed()
        {
            if (actorType == FootstepActorType.Player && playerMovement != null)
                return playerMovement.CurrentSpeed;

            if (navMeshAgent != null && navMeshAgent.enabled)
                return navMeshAgent.velocity.magnitude;

            if (characterController != null)
                return characterController.velocity.magnitude;

            return 0f;
        }

        private void TriggerFootstepForFoot(bool useLeftFoot)
        {
            if (!allowExternalTriggers)
                return;

            if (requireGroundContactForExternalTriggers &&
                !TryDetectSurfaceAndGetPosition(useLeftFoot, externalTriggerGroundDistance, out _))
            {
                return;
            }

            PlayFootstepInternal(useLeftFoot, false);
        }

        private void PlayFootstepInternal(bool useLeftFoot, bool toggleAfterPlay)
        {
            Vector3 footPosition = DetectSurfaceAndGetPosition(useLeftFoot);
            FootstepSoundSet soundSet = GetSoundSetForSurface(currentSurface);
            if (soundSet?.footstepClips == null || soundSet.footstepClips.Length == 0)
                return;

            AudioClip clip = GetRandomClipNoRepeat(soundSet.footstepClips);
            if (clip == null)
                return;

            float finalVolume = Mathf.Clamp01(GetBaseVolume() * soundSet.volumeMultiplier * Random.Range(randomVolumeRange.x, randomVolumeRange.y));
            float finalPitch = soundSet.basePitch * Random.Range(randomPitchRange.x, randomPitchRange.y);

            if (actorType == FootstepActorType.Player && playerMovement != null && playerMovement.IsCrouching)
            {
                finalVolume *= crouchAdditionalVolumeMultiplier;
                finalPitch *= crouchPitchMultiplier;
            }

            PlayAtPosition(clip, footPosition, finalVolume, finalPitch);

            if (toggleAfterPlay)
                isLeftFoot = !isLeftFoot;
        }

        private float GetBaseVolume()
        {
            if (actorType != FootstepActorType.Player || playerMovement == null)
                return enemyBaseVolume;

            float baseVolume = GetVolumeForTier(playerMovement.CurrentTier);
            if (playerMovement.IsCrouching)
                baseVolume *= crouchVolumeMultiplier;
            else if (playerMovement.IsSprinting)
                baseVolume *= sprintVolumeMultiplier;

            return baseVolume;
        }

        private Vector3 DetectSurfaceAndGetPosition(bool useLeftFoot)
        {
            if (TryDetectSurfaceAndGetPosition(useLeftFoot, raycastDistance, out Vector3 hitPoint))
            {
                return hitPoint;
            }

            Transform currentFoot = useLeftFoot ? leftFoot : rightFoot;
            return currentFoot.position;
        }

        private bool TryDetectSurfaceAndGetPosition(bool useLeftFoot, float castDistance, out Vector3 hitPoint)
        {
            Transform currentFoot = useLeftFoot ? leftFoot : rightFoot;
            Vector3 rayOrigin = currentFoot.position + Vector3.up * 0.1f;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, castDistance, GetGroundMask(), QueryTriggerInteraction.Ignore))
            {
                lastFootHitPoint = hit.point;
                hitPoint = hit.point;

                SurfaceTag surfaceTag = hit.collider.GetComponent<SurfaceTag>();
                if (surfaceTag != null)
                {
                    currentSurface = surfaceTag.SurfaceType;
                    return true;
                }

                if (hit.collider.sharedMaterial != null)
                {
                    currentSurface = GetSurfaceFromPhysicMaterial(hit.collider.sharedMaterial);
                    return true;
                }

                currentSurface = GetSurfaceFromTag(hit.collider.tag);
                return true;
            }

            hitPoint = currentFoot.position;
            return false;
        }

        private static SurfaceType GetSurfaceFromTag(string tag)
        {
            return tag switch
            {
                "Metal" => SurfaceType.Metal,
                "Dirt" => SurfaceType.Dirt,
                "Water" => SurfaceType.Water,
                "Wood" => SurfaceType.Wood,
                "Grass" => SurfaceType.Grass,
                "Gravel" => SurfaceType.Gravel,
                "Stone" => SurfaceType.Stone,
                _ => SurfaceType.Stone
            };
        }

        private static SurfaceType GetSurfaceFromPhysicMaterial(PhysicsMaterial material)
        {
            string matName = material.name.ToLowerInvariant();

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
            if (clips == null || clips.Length == 0)
                return null;
            if (clips.Length == 1)
                return clips[0];

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
            if (audioSourcePool == null || audioSourcePool.Length == 0)
                return;

            float filteredVolume = volume * GetDistanceVolumeMultiplier(position);
            if (filteredVolume <= cullVolumeThreshold)
                return;

            AudioSource source = audioSourcePool[currentPoolIndex];
            currentPoolIndex = (currentPoolIndex + 1) % audioSourcePool.Length;

            source.transform.position = position;
            source.clip = clip;
            source.volume = Mathf.Clamp01(filteredVolume);
            source.pitch = pitch;
            source.Play();
        }

        private float GetDistanceVolumeMultiplier(Vector3 sourcePosition)
        {
            if (!applyDistanceVolumeFilter)
                return 1f;

            Transform listenerTransform = GetListenerTransform();
            if (listenerTransform == null)
                return 1f;

            float near = Mathf.Max(0f, distanceNear);
            float far = Mathf.Max(near + 0.01f, distanceFar);
            float distance = Vector3.Distance(sourcePosition, listenerTransform.position);
            float normalized = Mathf.InverseLerp(near, far, distance);
            return Mathf.Clamp01(distanceVolumeCurve.Evaluate(normalized));
        }

        private Transform GetListenerTransform()
        {
            if (listenerOverride != null)
                return listenerOverride;

            if (cachedListener == null || !cachedListener.isActiveAndEnabled)
            {
                cachedListener = FindFirstObjectByType<AudioListener>();
            }

            return cachedListener != null ? cachedListener.transform : null;
        }

        private FootstepSoundSet GetSoundSetForSurface(SurfaceType surfaceType)
        {
            if (cachedSurfaceSets.TryGetValue(surfaceType, out FootstepSoundSet set) &&
                set != null &&
                set.footstepClips != null &&
                set.footstepClips.Length > 0)
            {
                return set;
            }

            return defaultSounds;
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

        private int GetGroundMask()
        {
            return groundLayer.value == 0 ? Physics.DefaultRaycastLayers : groundLayer.value;
        }

        private void OnValidate()
        {
            if (randomVolumeRange.x > randomVolumeRange.y)
                randomVolumeRange = new Vector2(randomVolumeRange.y, randomVolumeRange.x);

            if (randomPitchRange.x > randomPitchRange.y)
                randomPitchRange = new Vector2(randomPitchRange.y, randomPitchRange.x);

            externalTriggerGroundDistance = Mathf.Max(0.05f, externalTriggerGroundDistance);
            distanceNear = Mathf.Max(0f, distanceNear);
            distanceFar = Mathf.Max(distanceNear + 0.01f, distanceFar);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
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

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(lastFootHitPoint, 0.1f);

            Gizmos.color = Color.yellow;
            Vector3 rayOrigin = (isLeftFoot ? leftFoot : rightFoot) != null
                ? (isLeftFoot ? leftFoot.position : rightFoot.position)
                : transform.position;
            rayOrigin += Vector3.up * 0.1f;
            Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * raycastDistance);
        }
#endif
    }

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
        [Tooltip("Per-surface volume multiplier")]
        public float volumeMultiplier = 1f;
        [Tooltip("Per-surface base pitch")]
        public float basePitch = 1f;
    }

/*     public enum SurfaceType
    {
        Stone,
        Metal,
        Dirt,
        Water,
        Wood,
        Grass,
        Gravel
    }

    public class SurfaceTag : MonoBehaviour
    {
        [SerializeField] private SurfaceType surfaceType = SurfaceType.Stone;
        public SurfaceType SurfaceType => surfaceType;
    } */
}
