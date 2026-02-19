using UnityEngine;

namespace DungeonDredge.Audio
{
    /// <summary>
    /// ScriptableObject containing arrays of voice clips for player reactions.
    /// Attach this to a PlayerVoiceManager to play voiced reactions.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlayerVoices", menuName = "DungeonDredge/Player Voices")]
    public class PlayerVoices : ScriptableObject
    {
        [Header("Breathing")]
        [Tooltip("Idle breathing loop (short inhale/exhale sounds)")]
        public AudioClip[] idleBreathing;

        [Tooltip("Heavy breathing when encumbered")]
        public AudioClip[] heavyBreathing;

        [Tooltip("Exhausted breathing when stamina is low")]
        public AudioClip[] exhaustedBreathing;

        [Header("Effort/Grunt")]
        [Tooltip("Grunt sounds when moving while carrying heavy weight")]
        public AudioClip[] moveEffort;

        [Tooltip("Grunt when starting to move from rest")]
        public AudioClip[] startMovement;

        [Header("Damage")]
        [Tooltip("Light damage reaction")]
        public AudioClip[] lightDamage;

        [Tooltip("Heavy damage reaction")]
        public AudioClip[] heavyDamage;

        [Header("Jump")]
        [Tooltip("Jump sound (short grunt or exhale)")]
        public AudioClip[] jump;

        [Header("Shove")]
        [Tooltip("Shove action sound (industrial shoving)")]
        public AudioClip[] shove;

        [Header("Settings")]
        [Tooltip("Volume for all voice sounds (0-1)")]
        [Range(0f, 1f)] public float voiceVolume = 0.8f;

        [Tooltip("Min/Max pitch variation for voices")]
        public Vector2 pitchRange = new Vector2(0.9f, 1.1f);
    }
        }