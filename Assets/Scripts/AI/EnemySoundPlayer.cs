using UnityEngine;

namespace DungeonDredge.AI
{
    /// <summary>
    /// Handles 3D spatial audio for enemies: idle vocalizations, alert roars,
    /// chase growls, attack grunts, and death cries.
    /// Attach to enemy root (next to EnemyAI). AudioSource is auto-created.
    /// Clips come from EnemyData — just drag-and-drop WAV files onto the arrays.
    /// </summary>
    [RequireComponent(typeof(EnemyAI))]
    public class EnemySoundPlayer : MonoBehaviour
    {
        [Header("3D Audio Settings")]
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private float spatialBlend = 1f; // 1 = full 3D

        [Header("Pitch Variation")]
        [SerializeField] private float minPitch = 0.85f;
        [SerializeField] private float maxPitch = 1.15f;

        // Components
        private EnemyAI enemyAI;
        private AudioSource audioSource;
        private EnemyData data;

        // Idle vocalization timer
        private float nextIdleTime;
        private bool wasAggressive;
        private bool wasAlerted;
        private bool isDead;

        private void Awake()
        {
            enemyAI = GetComponent<EnemyAI>();

            // Create a dedicated AudioSource for monster sounds
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = spatialBlend;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.outputAudioMixerGroup = null; // Use default, or assign via AudioManager
        }

        private void Start()
        {
            data = enemyAI.EnemyData;
            if (data != null)
            {
                audioSource.volume = data.enemySounds.soundVolume;
            }
            ScheduleNextIdle();
        }

        private void OnEnable()
        {
            if (enemyAI != null)
            {
                enemyAI.OnDeath += OnEnemyDeath;
            }
        }

        private void OnDisable()
        {
            if (enemyAI != null)
            {
                enemyAI.OnDeath -= OnEnemyDeath;
            }
        }

        private void Update()
        {
            if (isDead || data == null) return;

            // --- State transition sounds ---
            bool aggressive = enemyAI.IsAggressive;
            bool alerted = enemyAI.IsAlerted;

            // Just became aggressive (chase started)
            if (aggressive && !wasAggressive)
            {
                PlayRandomClip(data.enemySounds.chaseSounds);
            }
            // Just became alerted (heard/saw something)
            else if (alerted && !wasAlerted && !aggressive)
            {
                PlayRandomClip(data.enemySounds.alertSounds);
            }

            wasAggressive = aggressive;
            wasAlerted = alerted;

            // --- Idle vocalizations ---
            if (!aggressive && !alerted && Time.time >= nextIdleTime)
            {
                PlayRandomClip(data.enemySounds.idleSounds);
                ScheduleNextIdle();
            }
        }

        /// <summary>
        /// Call from EnemyAI when an attack animation starts.
        /// </summary>
        public void PlayAttackSound()
        {
            if (data != null)
                PlayRandomClip(data.enemySounds.attackSounds);
        }

        private void OnEnemyDeath()
        {
            if (isDead) return;
            isDead = true;
            PlayRandomClip(data?.enemySounds.deathSounds);
        }

        /// <summary>
        /// Reset state when the enemy is respawned from a pool.
        /// </summary>
        public void ResetState()
        {
            isDead = false;
            wasAggressive = false;
            wasAlerted = false;
            data = enemyAI != null ? enemyAI.EnemyData : null;
            if (data != null)
            {
                audioSource.volume = data.enemySounds.soundVolume;
            }
            ScheduleNextIdle();
        }

        private void PlayRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0 || audioSource == null) return;

            // Don't overlap if already playing (prevents sound stacking)
            if (audioSource.isPlaying) return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;

            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip, data != null ? data.enemySounds.soundVolume : 0.8f);
        }

        private void ScheduleNextIdle()
        {
            Vector2 interval = data != null ? data.enemySounds.idleSoundInterval : new Vector2(5f, 15f);
            nextIdleTime = Time.time + Random.Range(interval.x, interval.y);
        }
    }
}
