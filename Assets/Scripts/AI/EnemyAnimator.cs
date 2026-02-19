using UnityEngine;
using DungeonDredge.AI;
using DungeonDredge.Audio;

namespace DungeonDredge.Enemies
{
    /// <summary>
    /// Handles enemy animations through the Animator component.
    /// Works with a base Animator Controller that can be overridden per creature type.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EnemyAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;

        [Header("Settings")]
        [Tooltip("Smooth damp time for movement blend transitions")]
        [SerializeField] private float _movementDampTime = 0.1f;
        
        [Tooltip("Attack trigger cooldown to prevent spam")]
        [SerializeField] private float _attackTriggerCooldown = 0.1f;

        // Animator Parameter Hashes (cached for performance)
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int HitTriggerHash = Animator.StringToHash("Hit");
        private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
        private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
        private static readonly int SpecialAttackHash = Animator.StringToHash("SpecialAttack");
        private static readonly int SpecialAttackIndexHash = Animator.StringToHash("SpecialAttackIndex");

        // State
        private float _lastAttackTriggerTime;
        private bool _isDead;
        private UnityEngine.AI.NavMeshAgent _agent;
        private FootstepSystem _footstepSystem;

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
            
            _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (_agent == null)
            {
                _agent = GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
            }

            _footstepSystem = GetComponent<FootstepSystem>();
            if (_footstepSystem == null)
            {
                _footstepSystem = GetComponentInParent<FootstepSystem>();
            }
        }

        private void Update()
        {
            UpdateMovementAnimation();
        }

        #region Movement

        /// <summary>
        /// Update movement animation based on NavMeshAgent velocity.
        /// </summary>
        private void UpdateMovementAnimation()
        {
            if (_animator == null || _isDead) return;

            float speed = 0f;
            bool isMoving = false;

            if (_agent != null && _agent.enabled)
            {
                speed = _agent.velocity.magnitude;
                isMoving = speed > 0.1f;
            }

            // Normalize speed (assuming max speed around 8)
            float normalizedSpeed = Mathf.Clamp01(speed / 8f);

            _animator.SetFloat(SpeedHash, normalizedSpeed, _movementDampTime, Time.deltaTime);
            _animator.SetBool(IsMovingHash, isMoving);
        }

        /// <summary>
        /// Manually set moving state (for non-NavMeshAgent movement).
        /// </summary>
        public void SetMoving(bool isMoving)
        {
            if (_animator == null || _isDead) return;
            _animator.SetBool(IsMovingHash, isMoving);
        }

        /// <summary>
        /// Set movement speed directly.
        /// </summary>
        public void SetSpeed(float normalizedSpeed)
        {
            if (_animator == null || _isDead) return;
            _animator.SetFloat(SpeedHash, normalizedSpeed, _movementDampTime, Time.deltaTime);
        }

        #endregion

        #region Combat

        /// <summary>
        /// Play attack animation.
        /// </summary>
        public void PlayAttack()
        {
            if (_animator == null || _isDead) return;

            // Prevent attack trigger spam
            if (Time.time < _lastAttackTriggerTime + _attackTriggerCooldown) return;
            _lastAttackTriggerTime = Time.time;

            _animator.ResetTrigger(AttackTriggerHash);
            _animator.SetTrigger(AttackTriggerHash);
        }

        /// <summary>
        /// Play attack with specific index (for combo or variant attacks).
        /// </summary>
        public void PlayAttack(int attackIndex)
        {
            if (_animator == null || _isDead) return;

            if (Time.time < _lastAttackTriggerTime + _attackTriggerCooldown) return;
            _lastAttackTriggerTime = Time.time;

            _animator.SetInteger(SpecialAttackIndexHash, attackIndex);
            _animator.ResetTrigger(AttackTriggerHash);
            _animator.SetTrigger(AttackTriggerHash);
        }

        /// <summary>
        /// Set aiming state (for ranged enemies).
        /// </summary>
        public void SetAiming(bool isAiming)
        {
            if (_animator == null || _isDead) return;
            _animator.SetBool(IsAimingHash, isAiming);
        }

        /// <summary>
        /// Play hit reaction animation.
        /// </summary>
        public void PlayHitReaction()
        {
            if (_animator == null || _isDead) return;

            _animator.ResetTrigger(HitTriggerHash);
            _animator.SetTrigger(HitTriggerHash);
        }

        #endregion

        #region Special Animations

        /// <summary>
        /// Play a special animation by name (for boss abilities).
        /// </summary>
        public void PlaySpecialAnimation(string animationName)
        {
            if (_animator == null || _isDead) return;

            // Map special animation names to indices
            int index = GetSpecialAnimationIndex(animationName);
            
            _animator.SetInteger(SpecialAttackIndexHash, index);
            _animator.ResetTrigger(SpecialAttackHash);
            _animator.SetTrigger(SpecialAttackHash);
        }

        /// <summary>
        /// End special animation state.
        /// </summary>
        public void EndSpecialAnimation()
        {
            if (_animator == null) return;
            
            // Reset special attack index
            _animator.SetInteger(SpecialAttackIndexHash, 0);
        }

        /// <summary>
        /// Get animation index from name for special attacks.
        /// Override in subclass for custom mapping.
        /// </summary>
        protected virtual int GetSpecialAnimationIndex(string animationName)
        {
            return animationName switch
            {
                "Charge" => 1,
                "Spin" => 2,
                "GroundSlam" => 3,
                "RangedBarrage" => 4,
                "Summon" => 5,
                "PhaseTransition" => 6,
                _ => 0
            };
        }

        #endregion

        #region Death

        /// <summary>
        /// Play death animation.
        /// </summary>
        public void PlayDeath()
        {
            if (_animator == null) return;

            _isDead = true;
            _animator.SetBool(IsDeadHash, true);
            _animator.ResetTrigger(DeathTriggerHash);
            _animator.SetTrigger(DeathTriggerHash);
        }

        #endregion

        #region Reset

        /// <summary>
        /// Reset animator state (call when retrieving from pool).
        /// </summary>
        public void ResetAnimator()
        {
            _isDead = false;
            _lastAttackTriggerTime = 0f;

            if (_animator == null) return;

            // Reset all triggers
            _animator.ResetTrigger(AttackTriggerHash);
            _animator.ResetTrigger(HitTriggerHash);
            _animator.ResetTrigger(DeathTriggerHash);
            _animator.ResetTrigger(SpecialAttackHash);

            // Reset bools
            _animator.SetBool(IsDeadHash, false);
            _animator.SetBool(IsMovingHash, false);
            _animator.SetBool(IsAimingHash, false);

            // Reset floats
            _animator.SetFloat(SpeedHash, 0f);
            _animator.SetInteger(SpecialAttackIndexHash, 0);

            // Force state reset by rebinding
            _animator.Rebind();
            _animator.Update(0f);
        }

        /// <summary>
        /// Set the animator controller (for override controllers).
        /// </summary>
        public void SetAnimatorController(RuntimeAnimatorController controller)
        {
            if (_animator != null && controller != null)
            {
                _animator.runtimeAnimatorController = controller;
            }
        }

        #endregion

        #region Animation Events

        // Event for external listeners (like EnemyAI)
        public event System.Action OnAttackHit;
        public event System.Action OnAttackEnd;
        public event System.Action OnDeathComplete;

        /// <summary>
        /// Called by Animation Event at attack impact frame.
        /// Forwards to enemy controller via event.
        /// </summary>
        public void AnimEvent_AttackHit()
        {
            OnAttackHit?.Invoke();
        }

        /// <summary>
        /// Called by Animation Event when attack animation ends.
        /// </summary>
        public void AnimEvent_AttackEnd()
        {
            OnAttackEnd?.Invoke();
        }

        /// <summary>
        /// Called by Animation Event for footstep sounds.
        /// </summary>
        public void AnimEvent_Footstep()
        {
            _footstepSystem?.TriggerFootstep();
        }

        /// <summary>
        /// Called by Animation Event when death animation completes.
        /// </summary>
        public void AnimEvent_DeathComplete()
        {
            OnDeathComplete?.Invoke();
        }

        #endregion
    }
}

