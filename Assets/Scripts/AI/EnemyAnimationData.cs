using UnityEngine;
using System.Collections.Generic;

namespace DungeonDredge.AI
{
    /// <summary>
    /// ScriptableObject that defines animation configuration for an enemy type.
    /// Handles varying animation counts per creature (some have 2 attacks, some have 10).
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Animation Data", menuName = "DungeonDredge/Enemy Animation Data")]
    public class EnemyAnimationData : ScriptableObject
    {
        [Header("Animator Setup")]
        [Tooltip("The animator controller for this creature (from asset pack or custom)")]
        [SerializeField] private RuntimeAnimatorController _animatorController;
        
        [Tooltip("Use root motion for movement (if animations have root motion)")]
        [SerializeField] private bool _useRootMotion = false;
        
        [Tooltip("If true, will use in-place animations and move via NavMeshAgent")]
        [SerializeField] private bool _preferInPlaceAnimations = true;

        [Header("Locomotion")]
        [SerializeField] private LocomotionConfig _locomotion = new LocomotionConfig();

        [Header("Attack Animations")]
        [Tooltip("List of available attack animation names/triggers")]
        [SerializeField] private List<AttackAnimationConfig> _attacks = new List<AttackAnimationConfig>();

        [Header("Hit Reactions")]
        [SerializeField] private List<string> _hitReactionTriggers = new List<string> { "Hit" };
        [SerializeField] private bool _hasDirectionalHits = false;
        [SerializeField] private string _hitFrontTrigger = "Get_Hit";
        [SerializeField] private string _hitBackTrigger = "Get_Hit_Back";
        [SerializeField] private string _hitLeftTrigger = "Get_Hit_Left";
        [SerializeField] private string _hitRightTrigger = "Get_Hit_Right";

        [Header("Death Animations")]
        [SerializeField] private List<string> _deathTriggers = new List<string> { "Death" };
        
        [Header("Special Animations")]
        [SerializeField] private string _shoutTrigger = "Shout";
        [SerializeField] private string _jumpTrigger = "Jump";
        [SerializeField] private string _emergenceTrigger = "Emergence";
        [SerializeField] private bool _hasSpecialAnimations = false;

        [Header("Timing")]
        [Tooltip("Time in seconds before attack damage is dealt (sync with animation)")]
        [SerializeField] private float _attackHitTime = 0.3f;
        
        [Tooltip("Total attack animation duration")]
        [SerializeField] private float _attackDuration = 1f;
        
        [Tooltip("Time before death animation completes")]
        [SerializeField] private float _deathDuration = 2f;

        // Public Properties
        public RuntimeAnimatorController AnimatorController => _animatorController;
        public bool UseRootMotion => _useRootMotion;
        public bool PreferInPlaceAnimations => _preferInPlaceAnimations;
        public LocomotionConfig Locomotion => _locomotion;
        public IReadOnlyList<AttackAnimationConfig> Attacks => _attacks;
        public int AttackCount => _attacks.Count;
        public IReadOnlyList<string> HitReactionTriggers => _hitReactionTriggers;
        public bool HasDirectionalHits => _hasDirectionalHits;
        public IReadOnlyList<string> DeathTriggers => _deathTriggers;
        public bool HasSpecialAnimations => _hasSpecialAnimations;
        public float AttackHitTime => _attackHitTime;
        public float AttackDuration => _attackDuration;
        public float DeathDuration => _deathDuration;

        /// <summary>
        /// Get a random attack configuration.
        /// </summary>
        public AttackAnimationConfig GetRandomAttack()
        {
            if (_attacks.Count == 0) return null;
            return _attacks[Random.Range(0, _attacks.Count)];
        }

        /// <summary>
        /// Get attack by index.
        /// </summary>
        public AttackAnimationConfig GetAttack(int index)
        {
            if (index < 0 || index >= _attacks.Count) return null;
            return _attacks[index];
        }

        /// <summary>
        /// Get a random death trigger.
        /// </summary>
        public string GetRandomDeathTrigger()
        {
            if (_deathTriggers.Count == 0) return "Dead";
            return _deathTriggers[Random.Range(0, _deathTriggers.Count)];
        }

        /// <summary>
        /// Get appropriate hit reaction based on direction.
        /// </summary>
        public string GetHitReactionTrigger(Vector3 hitDirection, Transform enemyTransform)
        {
            if (!_hasDirectionalHits || _hitReactionTriggers.Count == 0)
            {
                return _hitReactionTriggers.Count > 0 
                    ? _hitReactionTriggers[Random.Range(0, _hitReactionTriggers.Count)] 
                    : "Get_Hit";
            }

            // Calculate hit direction relative to enemy
            Vector3 localDir = enemyTransform.InverseTransformDirection(hitDirection);
            
            // Determine primary direction
            if (Mathf.Abs(localDir.z) > Mathf.Abs(localDir.x))
            {
                return localDir.z > 0 ? _hitFrontTrigger : _hitBackTrigger;
            }
            else
            {
                return localDir.x > 0 ? _hitRightTrigger : _hitLeftTrigger;
            }
        }

        /// <summary>
        /// Get shout trigger if available.
        /// </summary>
        public string GetShoutTrigger()
        {
            return _hasSpecialAnimations ? _shoutTrigger : null;
        }

        /// <summary>
        /// Get jump trigger if available.
        /// </summary>
        public string GetJumpTrigger()
        {
            return _hasSpecialAnimations ? _jumpTrigger : null;
        }
    }

    /// <summary>
    /// Configuration for locomotion animations.
    /// </summary>
    [System.Serializable]
    public class LocomotionConfig
    {
        [Header("Parameters")]
        [Tooltip("Animator float parameter for movement speed")]
        public string speedParameter = "Speed";
        
        [Tooltip("Animator float parameter for horizontal movement (strafe)")]
        public string horizontalParameter = "Horizontal";
        
        [Tooltip("Animator float parameter for vertical movement (forward/back)")]
        public string verticalParameter = "Vertical";
        
        [Tooltip("Animator bool for is moving")]
        public string isMovingParameter = "IsMoving";

        [Header("Speed Thresholds")]
        [Tooltip("Speed below which idle plays")]
        public float idleThreshold = 0.1f;
        
        [Tooltip("Speed above which run plays (vs walk)")]
        public float runThreshold = 0.6f;
        
        [Tooltip("Maximum speed for normalization")]
        public float maxSpeed = 8f;

        [Header("Directional Movement")]
        [Tooltip("Does this creature have strafe/directional walk animations?")]
        public bool hasDirectionalMovement = false;
        
        [Tooltip("Use blend tree for smooth directional transitions")]
        public bool useBlendTree = true;
    }

    /// <summary>
    /// Configuration for a single attack animation.
    /// </summary>
    [System.Serializable]
    public class AttackAnimationConfig
    {
        [Tooltip("Name/identifier for this attack")]
        public string attackName = "Attack_1";
        
        [Tooltip("Animator trigger name")]
        public string triggerName = "Attack_1";
        
        [Tooltip("Animator integer value if using attack index system")]
        public int attackIndex = 0;

        [Header("Timing")]
        [Tooltip("Time (normalized 0-1) when damage should be dealt")]
        [Range(0f, 1f)]
        public float hitTimeNormalized = 0.4f;
        
        [Tooltip("Total duration of this attack animation")]
        public float duration = 1f;

        [Header("Properties")]
        [Tooltip("Damage multiplier for this attack")]
        public float damageMultiplier = 1f;
        
        [Tooltip("Range multiplier for this attack")]
        public float rangeMultiplier = 1f;
        
        [Tooltip("Does this attack use root motion?")]
        public bool usesRootMotion = false;
        
        [Tooltip("Is this a special/heavy attack?")]
        public bool isHeavyAttack = false;
        
        [Tooltip("Can this attack be used while moving?")]
        public bool canUseWhileMoving = false;
    }
}

