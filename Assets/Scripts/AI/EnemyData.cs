using UnityEngine;
using DungeonDredge.Inventory;
using DungeonDredge.Enemies;
namespace DungeonDredge.AI
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "DungeonDredge/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Basic Info")]
        public string enemyId;
        public string enemyName;
        [TextArea(2, 4)]
        public string description;

        [Header("Rank")]
        public DungeonRank minimumRank = DungeonRank.F;
        public EnemyBehaviorType behaviorType = EnemyBehaviorType.Flee;

        [Header("Stats")]
        public float health = 100f;
        public float walkSpeed = 3f;
        public float chaseSpeed = 6f;

        [Header("Detection")]
        public float sightRange = 15f;
        public float sightAngle = 120f;
        public float hearingThreshold = 0.5f;

        [Header("Combat")]
        public float attackRange = 2f;
        public float attackCooldown = 1.5f;
        public float attackDamage = 10f;

        [Header("Prefab & Visual")]
        public GameObject prefab;
        public Sprite icon;
        public Color tintColor = Color.white;
        [Tooltip("Scale multiplier applied to the prefab")]
        public float modelScale = 1f;

        [Header("Animation")]
        [Tooltip("Override animator controller (optional)")]
        public RuntimeAnimatorController animatorOverride;
        [Tooltip("Advanced animation configuration (for creatures with many animations)")]
        public EnemyAnimationData animationData;

        [Header("Audio")]
        public AudioClip[] idleSounds;
        public AudioClip[] alertSounds;
        public AudioClip[] attackSounds;

        [Header("Drops")]
        public ItemDropChance[] itemDrops;

        [Header("Scaling Curves")]
        [Tooltip("How health scales over dungeon rank (X = rank index 0-6, Y = multiplier)")]
        public AnimationCurve healthScaling = AnimationCurve.Linear(0f, 1f, 6f, 3f);
        [Tooltip("How damage scales over dungeon rank")]
        public AnimationCurve damageScaling = AnimationCurve.Linear(0f, 1f, 6f, 2.5f);

        /// <summary>
        /// Get scaled health based on dungeon rank.
        /// </summary>
        public float GetScaledHealth(DungeonRank dungeonRank)
        {
            return health * healthScaling.Evaluate((int)dungeonRank);
        }

        /// <summary>
        /// Get scaled damage based on dungeon rank.
        /// </summary>
        public float GetScaledDamage(DungeonRank dungeonRank)
        {
            return attackDamage * damageScaling.Evaluate((int)dungeonRank);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(enemyId))
            {
                enemyId = name.ToLower().Replace(" ", "_");
            }
        }
    }

    [System.Serializable]
    public class ItemDropChance
    {
        public ItemData item;
        [Range(0f, 1f)]
        public float dropChance = 0.5f;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }
}
