using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using DungeonDredge.AI;
using DungeonDredge.Audio;

namespace DungeonDredge.Enemies
{
    /// <summary>
    /// Advanced enemy animator that handles:
    /// - Multiple attack variations
    /// - Root motion vs NavMeshAgent movement
    /// - Animation layers to prevent sliding
    /// - Directional hit reactions
    /// - Variable animation sets per creature type
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EnemyAnimatorAdvanced : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private EnemyAnimationData _animationData;

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        // Cached Components
        private Animator _animator;
        private NavMeshAgent _agent;
        private EnemyAI _enemyAI;
        private FootstepSystem _footstepSystem;

        // Cached Parameter Hashes
        private int _speedHash;
        private int _horizontalHash;
        private int _verticalHash;
        private int _isMovingHash;
        private int _attackIndexHash;
        private int _isDeadHash;

        // State
        private bool _isInitialized;
        private bool _isDead;
        private bool _isAttacking;
        private bool _isInHitReaction;
        private AttackAnimationConfig _currentAttack;
        private Coroutine _attackCoroutine;
        private Coroutine _hitReactionCoroutine;

        // Root Motion
        private bool _applyRootMotion;
        private Vector3 _rootMotionDelta;

        // Callbacks
        public event System.Action OnAttackHitFrame;
        public event System.Action OnAttackComplete;
        public event System.Action OnDeathComplete;

        #region Properties

        public bool IsAttacking => _isAttacking;
        public bool IsInHitReaction => _isInHitReaction;
        public bool IsDead => _isDead;
        public EnemyAnimationData AnimationData => _animationData;
        public AttackAnimationConfig CurrentAttack => _currentAttack;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            if (_agent == null) _agent = GetComponentInParent<NavMeshAgent>();
            
            _enemyAI = GetComponent<EnemyAI>();
            if (_enemyAI == null) _enemyAI = GetComponentInParent<EnemyAI>();
            _footstepSystem = GetComponent<FootstepSystem>();
            if (_footstepSystem == null) _footstepSystem = GetComponentInParent<FootstepSystem>();

            CacheParameterHashes();
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!_isInitialized || _isDead) return;

            UpdateLocomotion();
        }

        private void OnAnimatorMove()
        {
            // Handle root motion
            if (_applyRootMotion && _animationData != null && _animationData.UseRootMotion)
            {
                _rootMotionDelta = _animator.deltaPosition;
                
                // Apply root motion to position
                transform.position += _rootMotionDelta;
                
                // Apply root rotation if desired
                // transform.rotation *= _animator.deltaRotation;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _attackCoroutine = null;
            _hitReactionCoroutine = null;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize with animation data.
        /// </summary>
        public void Initialize(EnemyAnimationData data = null)
        {
            if (data != null)
            {
                _animationData = data;
            }

            if (_animationData == null)
            {
                Debug.LogWarning($"EnemyAnimatorAdvanced on {name}: No animation data assigned!");
                _isInitialized = false;
                return;
            }

            // Apply animator controller
            if (_animationData.AnimatorController != null && _animator != null)
            {
                _animator.runtimeAnimatorController = _animationData.AnimatorController;
            }

            // Configure root motion
            _applyRootMotion = _animationData.UseRootMotion && !_animationData.PreferInPlaceAnimations;
            if (_animator != null)
            {
                _animator.applyRootMotion = _applyRootMotion;
            }

            // Sync with NavMeshAgent
            if (_agent != null)
            {
                _agent.updatePosition = !_applyRootMotion;
                _agent.updateRotation = true;
            }

            CacheParameterHashes();
            _isInitialized = true;
            _isDead = false;
            _isAttacking = false;
            _isInHitReaction = false;
        }

        private void CacheParameterHashes()
        {
            if (_animationData == null) return;

            var loco = _animationData.Locomotion;
            _speedHash = Animator.StringToHash(loco.speedParameter);
            _horizontalHash = Animator.StringToHash(loco.horizontalParameter);
            _verticalHash = Animator.StringToHash(loco.verticalParameter);
            _isMovingHash = Animator.StringToHash(loco.isMovingParameter);
            _attackIndexHash = Animator.StringToHash("AttackIndex");
            _isDeadHash = Animator.StringToHash("IsDead");
        }

        /// <summary>
        /// Reset animator state (for pooling).
        /// </summary>
        public void ResetAnimator()
        {
            StopAllCoroutines();
            _attackCoroutine = null;
            _hitReactionCoroutine = null;

            _isDead = false;
            _isAttacking = false;
            _isInHitReaction = false;
            _currentAttack = null;

            if (_animator != null)
            {
                _animator.Rebind();
                _animator.Update(0f);
                
                // Reset common parameters
                SetBool(_isDeadHash, false);
                SetBool(_isMovingHash, false);
                SetFloat(_speedHash, 0f);
            }

            _isInitialized = _animationData != null;
        }

        #endregion

        #region Locomotion

        private void UpdateLocomotion()
        {
            if (_animator == null || _animationData == null) return;
            if (_isAttacking || _isInHitReaction) return;

            var loco = _animationData.Locomotion;
            float speed = 0f;
            bool isMoving = false;

            if (_agent != null && _agent.enabled)
            {
                Vector3 velocity = _agent.velocity;
                speed = velocity.magnitude;
                isMoving = speed > loco.idleThreshold;

                // Normalize speed
                float normalizedSpeed = Mathf.Clamp01(speed / loco.maxSpeed);
                SetFloat(_speedHash, normalizedSpeed, 0.1f);

                // Directional movement
                if (loco.hasDirectionalMovement)
                {
                    Vector3 localVelocity = transform.InverseTransformDirection(velocity);
                    float horizontal = Mathf.Clamp(localVelocity.x / loco.maxSpeed, -1f, 1f);
                    float vertical = Mathf.Clamp(localVelocity.z / loco.maxSpeed, -1f, 1f);

                    SetFloat(_horizontalHash, horizontal, 0.1f);
                    SetFloat(_verticalHash, vertical, 0.1f);
                }
            }

            SetBool(_isMovingHash, isMoving);
        }

        /// <summary>
        /// Manually set movement state (for non-NavMeshAgent movement).
        /// </summary>
        public void SetMovementState(float normalizedSpeed, bool isMoving)
        {
            if (_animator == null) return;

            SetFloat(_speedHash, normalizedSpeed, 0.1f);
            SetBool(_isMovingHash, isMoving);
        }

        /// <summary>
        /// Set directional movement (for strafe support).
        /// </summary>
        public void SetDirectionalMovement(float horizontal, float vertical)
        {
            if (_animator == null || _animationData == null) return;
            if (!_animationData.Locomotion.hasDirectionalMovement) return;

            SetFloat(_horizontalHash, horizontal, 0.1f);
            SetFloat(_verticalHash, vertical, 0.1f);
        }

        #endregion

        #region Attack

        /// <summary>
        /// Play a random attack animation from available attacks.
        /// </summary>
        public void PlayRandomAttack()
        {
            if (_animationData == null || _animationData.AttackCount == 0) return;

            var attack = _animationData.GetRandomAttack();
            PlayAttack(attack);
        }

        /// <summary>
        /// Play a specific attack by index.
        /// </summary>
        public void PlayAttack(int index)
        {
            if (_animationData == null) return;

            var attack = _animationData.GetAttack(index);
            if (attack != null)
            {
                PlayAttack(attack);
            }
        }

        /// <summary>
        /// Play a specific attack configuration.
        /// </summary>
        public void PlayAttack(AttackAnimationConfig attack)
        {
           
            if (_animator == null || attack == null) return;
            if (_isDead) return;

            // Stop any existing attack
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
            }

            _currentAttack = attack;
            _isAttacking = true;

            // Stop movement during attack (prevents sliding and moving attacks).
            if (_agent != null)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }

            // Configure root motion for this attack
            _applyRootMotion = attack.usesRootMotion;
            if (_animator != null)
            {
                _animator.applyRootMotion = _applyRootMotion;
            }

            // Set attack index if using index system
            if (HasParameter(_attackIndexHash))
            {
                _animator.SetInteger(_attackIndexHash, attack.attackIndex);
            }

            // Trigger attack
            _animator.SetTrigger(attack.triggerName);

            if (_debugMode)
            {
                Debug.Log($"Playing attack: {attack.attackName} (trigger: {attack.triggerName})");
            }
             
            // Start attack timing coroutine
            _attackCoroutine = StartCoroutine(AttackTimingCoroutine(attack));
        }

        private IEnumerator AttackTimingCoroutine(AttackAnimationConfig attack)
        {
            // Wait for hit frame
            float hitTime = attack.duration * attack.hitTimeNormalized;
            yield return new WaitForSeconds(hitTime);

            // Fire hit event
           // OnAttackHitFrame?.Invoke();

            // Wait for attack to complete
            float remainingTime = attack.duration * (1f - attack.hitTimeNormalized);
            yield return new WaitForSeconds(remainingTime);

            // Attack complete
            EndAttack();
        }

        private void EndAttack()
        {
            if (!_isAttacking) return;

            _isAttacking = false;
            _currentAttack = null;
            _attackCoroutine = null;

            // Restore movement
            if (_agent != null)
            {
                _agent.isStopped = false;
            }

            // Restore root motion setting
            _applyRootMotion = _animationData != null && _animationData.UseRootMotion && !_animationData.PreferInPlaceAnimations;
            if (_animator != null)
            {
                _animator.applyRootMotion = _applyRootMotion;
            }

            OnAttackComplete?.Invoke();
        }

        /// <summary>
        /// Force end attack early (e.g., on death or stun).
        /// </summary>
        public void CancelAttack()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }

            _isAttacking = false;
            _currentAttack = null;
            _attackCoroutine = null;

            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
            }
        }

        #endregion

        #region Hit Reaction

        /// <summary>
        /// Play hit reaction animation.
        /// </summary>
        public void PlayHitReaction(Vector3 hitDirection = default)
        {
            if (_animator == null || _animationData == null) return;
            if (_isDead || _isAttacking) return; // Don't interrupt attacks with hit reactions

            string trigger = _animationData.GetHitReactionTrigger(hitDirection, transform);
            
            if (_hitReactionCoroutine != null)
            {
                StopCoroutine(_hitReactionCoroutine);
            }

            _isInHitReaction = true;
            _animator.SetTrigger(trigger);

            if (_debugMode)
            {
                Debug.Log($"Playing hit reaction: {trigger}");
            }

            _hitReactionCoroutine = StartCoroutine(HitReactionCoroutine());
        }

        private IEnumerator HitReactionCoroutine()
        {
            // Hit reactions are usually short
            yield return new WaitForSeconds(0.3f);
            _isInHitReaction = false;
        }

        #endregion

        #region Death

        /// <summary>
        /// Play death animation.
        /// </summary>
        public void PlayDeath()
        {
            if (_animator == null || _animationData == null) return;

            // Cancel any ongoing animations
            CancelAttack();
            if (_hitReactionCoroutine != null)
            {
                StopCoroutine(_hitReactionCoroutine);
            }

            _isDead = true;
            _isInHitReaction = false;

            // Stop movement
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.enabled = false;
            }
            else if (_agent != null)
            {
                _agent.enabled = false;
            }

            // Set dead state
            SetBool(_isDeadHash, true);

            // Play random death animation
            string deathTrigger = _animationData.GetRandomDeathTrigger();
            _animator.SetTrigger(deathTrigger);

            if (_debugMode)
            {
                Debug.Log($"Playing death: {deathTrigger}");
            }

            // Notify after death animation
            StartCoroutine(DeathCompleteCoroutine());
        }

        private IEnumerator DeathCompleteCoroutine()
        {
            yield return new WaitForSeconds(_animationData.DeathDuration);
            OnDeathComplete?.Invoke();
        }

        #endregion

        #region Special Animations

        /// <summary>
        /// Play shout/roar animation (for aggro, boss phase changes, etc.)
        /// </summary>
        public void PlayShout()
        {
            if (_animator == null || _animationData == null) return;
            if (!_animationData.HasSpecialAnimations) return;

            string trigger = _animationData.GetShoutTrigger();
            if (!string.IsNullOrEmpty(trigger))
            {
                _animator.SetTrigger(trigger);
            }
        }

        /// <summary>
        /// Play jump animation.
        /// </summary>
        public void PlayJump()
        {
            if (_animator == null || _animationData == null) return;
            if (!_animationData.HasSpecialAnimations) return;

            string trigger = _animationData.GetJumpTrigger();
            if (!string.IsNullOrEmpty(trigger))
            {
                _animator.SetTrigger(trigger);
            }
        }

        /// <summary>
        /// Play custom trigger animation.
        /// </summary>
        public void PlayCustomTrigger(string triggerName)
        {
            if (_animator == null || string.IsNullOrEmpty(triggerName)) return;
            _animator.SetTrigger(triggerName);
        }

        #endregion

        #region Utility

        private void SetFloat(int hash, float value, float dampTime = 0f)
        {
            if (_animator == null || !HasParameter(hash)) return;
            
            if (dampTime > 0f)
            {
                _animator.SetFloat(hash, value, dampTime, Time.deltaTime);
            }
            else
            {
                _animator.SetFloat(hash, value);
            }
        }

        private void SetBool(int hash, bool value)
        {
            if (_animator == null || !HasParameter(hash)) return;
            _animator.SetBool(hash, value);
        }

        private bool HasParameter(int hash)
        {
            if (_animator == null) return false;

            foreach (var param in _animator.parameters)
            {
                if (param.nameHash == hash) return true;
            }
            return false;
        }

        /// <summary>
        /// Get current attack damage multiplier.
        /// </summary>
        public float GetCurrentDamageMultiplier()
        {
            return _currentAttack?.damageMultiplier ?? 1f;
        }

        /// <summary>
        /// Get current attack range multiplier.
        /// </summary>
        public float GetCurrentRangeMultiplier()
        {
            return _currentAttack?.rangeMultiplier ?? 1f;
        }

        #endregion

        #region Animation Events (called from animation clips)

        /// <summary>
        /// Animation event: Attack hit frame.
        /// </summary>
        public void AnimEvent_AttackHit()
        {
            OnAttackHitFrame?.Invoke();
        }

        /// <summary>
        /// Animation event: Attack complete.
        /// </summary>
        public void AnimEvent_AttackEnd()
        {
            EndAttack();
        }

        /// <summary>
        /// Animation event: Death complete.
        /// </summary>
        public void AnimEvent_DeathComplete()
        {
            OnDeathComplete?.Invoke();
        }

        /// <summary>
        /// Animation event: Footstep (for sound effects).
        /// </summary>
        public void AnimEvent_Footstep()
        {
            _footstepSystem?.TriggerFootstep();
        }

        #endregion
    }
}

