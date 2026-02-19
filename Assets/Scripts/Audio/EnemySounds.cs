using UnityEngine;
[CreateAssetMenu(fileName = "NewEnemySounds", menuName = "DungeonDredge/Enemy Sounds")]

public class EnemySounds : ScriptableObject
{      
       [Header("Audio")]
        public AudioClip[] idleSounds;
        public AudioClip[] alertSounds;
        public AudioClip[] chaseSounds;
        public AudioClip[] attackSounds;
        public AudioClip[] deathSounds;
        [Range(0f, 1f)] public float soundVolume = 0.8f;
        [Tooltip("Min/max seconds between idle vocalizations")]
        public Vector2 idleSoundInterval = new Vector2(5f, 15f);

}
