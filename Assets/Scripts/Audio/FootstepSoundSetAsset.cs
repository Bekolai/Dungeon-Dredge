using UnityEngine;

namespace DungeonDredge.Audio
{
    /// <summary>
    /// ScriptableObject version of FootstepSoundSet for easy asset management.
    /// Assign this to the FootstepSystem via the inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "FootstepSoundSet", menuName = "DungeonDredge/Audio/Footstep Sound Set")]
    public class FootstepSoundSetAsset : ScriptableObject
    {
        [Header("Surface Info")]
        public string surfaceName;
        public SurfaceType surfaceType;

        [Header("Audio Clips")]
        [Tooltip("Multiple footstep clips for variation - system will randomly select one")]
        public AudioClip[] footstepClips;
        
        [Tooltip("Sound when landing from a jump")]
        public AudioClip[] landingClips;
        
        [Tooltip("Sound when sliding or stopping suddenly")]
        public AudioClip[] scuffClips;

        [Header("Volume Overrides")]
        [Tooltip("Volume multiplier for this surface (1.0 = normal)")]
        [Range(0.5f, 2f)]
        public float volumeMultiplier = 1f;

        [Header("Pitch Overrides")]
        [Tooltip("Base pitch for this surface (1.0 = normal)")]
        [Range(0.5f, 1.5f)]
        public float basePitch = 1f;

        /// <summary>
        /// Convert to runtime FootstepSoundSet struct
        /// </summary>
        public FootstepSoundSet ToSoundSet()
        {
            return new FootstepSoundSet
            {
                surfaceName = surfaceName,
                footstepClips = footstepClips,
                landingClips = landingClips,
                scuffClips = scuffClips,
                volumeMultiplier = volumeMultiplier,
                basePitch = basePitch
            };
        }

        private void OnValidate()
        {
            // Auto-set surface type from name
            if (!string.IsNullOrEmpty(surfaceName))
            {
                surfaceType = surfaceName.ToLower() switch
                {
                    "stone" => SurfaceType.Stone,
                    "metal" => SurfaceType.Metal,
                    "dirt" => SurfaceType.Dirt,
                    "water" => SurfaceType.Water,
                    "grass" => SurfaceType.Grass,
                    "gravel" => SurfaceType.Gravel,
                    "wood" => SurfaceType.Wood,
                    _ => SurfaceType.Stone
                };
            }
        }
    }
}
