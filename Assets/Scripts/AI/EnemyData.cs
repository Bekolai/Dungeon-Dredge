using UnityEngine;
using DungeonDredge.Inventory;

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

        [Header("Prefab")]
        public GameObject prefab;

        [Header("Visual")]
        public Sprite icon;
        public Color tintColor = Color.white;

        [Header("Audio")]
        public AudioClip[] idleSounds;
        public AudioClip[] alertSounds;
        public AudioClip[] attackSounds;

        [Header("Drops")]
        public ItemDropChance[] itemDrops;

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
