using UnityEngine;
using UnityEngine.AI;
using DungeonDredge.Core;
using DungeonDredge.Inventory;
using DungeonDredge.Enemies;
using DungeonDredge.Player;

namespace DungeonDredge.AI
{
    public enum EnemyBehaviorType
    {
        Flee,       // Rank F pests - flee from player
        Aggressive, // Rank E - chase and attack
        Stalker     // Rank D+ - advanced pursuit
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour, INoiseListener, IEnemyState
    {
        [Header("Enemy Data (Primary Configuration)")]
        [SerializeField] private EnemyData enemyData;
        [Tooltip("Override the dungeon rank for scaling (uses enemyData.minimumRank if not set)")]
        [SerializeField] private DungeonRank dungeonRankOverride = DungeonRank.F;
        [SerializeField] private bool useDungeonRankOverride = false;

        [Header("Enemy Info (Auto-filled from EnemyData)")]
        [SerializeField] private string enemyName = "Enemy";
        [SerializeField] private DungeonRank rank = DungeonRank.F;
        [SerializeField] private EnemyBehaviorType behaviorType = EnemyBehaviorType.Flee;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float chaseSpeed = 6f;

        [Header("Detection")]
        [SerializeField] private float sightRange = 15f;
        [SerializeField] private float sightAngle = 120f;
        [SerializeField] private float closeProximityDetection = 2.5f;
        [SerializeField] private float rearAwarenessRange = 4f;
        [SerializeField] private float hearingRange = 16f;
        [SerializeField] private float hearingThreshold = 0.5f;
        [SerializeField] private float crouchStillSightRangeMultiplier = 0.7f;
        [SerializeField] private float crouchMovingSightRangeMultiplier = 0.85f;
        [SerializeField] private float sprintSightRangeMultiplier = 1.2f;
        [SerializeField] private float minimumCrouchVisibleRange = 4f;
        [SerializeField] private float crouchRearAwarenessMultiplier = 0.6f;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask obstructionLayer;

        [Header("Combat")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float attackDamage = 10f;

        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float randomPatrolRadius = 9f;
        [SerializeField] private float patrolPointReachThreshold = 0.75f;

        [Header("Visual")]
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color alertedColor = Color.yellow;
        [SerializeField] private Color aggressiveColor = Color.red;

        // Animation components
        private Animator animator;
        private EnemyAnimator enemyAnimator;
        private EnemyAnimatorAdvanced advancedAnimator;
        private HealthComponent healthComponent;

        // Components
        private NavMeshAgent agent;
        private AIStateMachine stateMachine;

        // State
        private Transform target;
        private Vector3 investigationTarget;
        private bool isAlerted;
        private bool isAggressive;
        private bool isStunned;
        private bool attackInProgress;
        private bool attackDamageApplied;
        private float nextAttackAllowedTime;

        // Detection
        private float lastDetectionCheck;
        private const float DetectionCheckInterval = 0.2f;
        private Transform cachedPlayerTransform;
        private float lastPlayerLookupTime;
        private const float PlayerLookupInterval = 1f;
        private Vector3 patrolAnchor;

        // Properties
        public NavMeshAgent Agent => agent;
        public Transform Target => target;
        public Vector3 InvestigationTarget => investigationTarget;
        public Transform[] PatrolPoints => patrolPoints;
        public float PatrolPointReachThreshold => patrolPointReachThreshold;
        public EnemyBehaviorType BehaviorType => behaviorType;
        public float WalkSpeed => walkSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float SightRange => sightRange;
        public float HearingThreshold => hearingThreshold;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public bool CanAttackNow => Time.time >= nextAttackAllowedTime;
        public bool IsAggressive => isAggressive;
        public bool IsAlerted => isAlerted;
        public bool IsStunned => isStunned;
        public bool IsAttackInProgress => attackInProgress;
        public DungeonRank Rank => rank;
        public string EnemyName => enemyName;
        public EnemyData EnemyData => enemyData;

        // Events
        public System.Action OnDeath;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = walkSpeed;
            patrolAnchor = transform.position;

            InitializeAnimationComponents();
            InitializeStateMachine();
        }

        private void Start()
        {
            // Auto-initialize from EnemyData if assigned
            if (enemyData != null)
            {
                DungeonRank effectiveRank = useDungeonRankOverride ? dungeonRankOverride : enemyData.minimumRank;
                Initialize(enemyData, effectiveRank);
            }

            // Register with stealth manager
            StealthManager.Instance?.RegisterEnemy(gameObject);
        }

        private void OnDestroy()
        {
            UnhookAnimationEvents();
            StealthManager.Instance?.UnregisterEnemy(gameObject);
        }

        private void Update()
        {
            if (isStunned) return;

            // Periodic detection check
            if (Time.time - lastDetectionCheck >= DetectionCheckInterval)
            {
                lastDetectionCheck = Time.time;
                CheckForPlayer();
            }

            stateMachine.Update();
        }

        /// <summary>
        /// Initialize from EnemyData ScriptableObject.
        /// Called automatically on Start if enemyData is assigned, or manually when spawning from pool.
        /// </summary>
        public void Initialize(EnemyData data, DungeonRank dungeonRank)
        {
            if (data == null)
            {
                Debug.LogWarning($"[EnemyAI] {gameObject.name}: No EnemyData provided for initialization!");
                return;
            }

            enemyData = data;

            // Apply basic info
            enemyName = data.enemyName;
            rank = data.minimumRank;
            behaviorType = data.behaviorType;

            // Apply stats (scaled by dungeon rank)
            walkSpeed = data.walkSpeed;
            chaseSpeed = data.chaseSpeed;
            attackRange = data.attackRange;
            attackCooldown = data.attackCooldown;
            attackDamage = data.GetScaledDamage(dungeonRank);

            // Apply detection
            sightRange = data.sightRange;
            sightAngle = data.sightAngle;
            hearingThreshold = data.hearingThreshold;

            // Apply NavMeshAgent speed
            if (agent != null)
            {
                agent.speed = walkSpeed;
            }

            // Apply animator controller - prioritize animationData, then animatorOverride
            if (animator != null)
            {
                if (data.animationData != null && data.animationData.AnimatorController != null)
                {
                    animator.runtimeAnimatorController = data.animationData.AnimatorController;
                }
                else if (data.animatorOverride != null)
                {
                    animator.runtimeAnimatorController = data.animatorOverride;
                }
            }

            // Initialize advanced animator if animation data is available
            if (advancedAnimator != null && data.animationData != null)
            {
                advancedAnimator.Initialize(data.animationData);
                Debug.Log($"[EnemyAI] {enemyName}: Initialized EnemyAnimatorAdvanced with {data.animationData.name}");
            }
            else if (enemyAnimator != null)
            {
                // Simple animator - just needs the controller which we already set
                Debug.Log($"[EnemyAI] {enemyName}: Using simple EnemyAnimator");
            }

            // Apply model scale
            if (data.modelScale != 1f)
            {
                transform.localScale = Vector3.one * data.modelScale;
            }

            // Initialize health component if available
            if (healthComponent != null)
            {
                float scaledHealth = data.GetScaledHealth(dungeonRank);
                healthComponent.ModifyMaxHealth(scaledHealth - healthComponent.MaxHealth);
                healthComponent.Revive();
            }

            Debug.Log($"[EnemyAI] {enemyName} initialized: Rank={dungeonRank}, Behavior={behaviorType}, HP={data.GetScaledHealth(dungeonRank)}, Damage={attackDamage}");
        }
        private void InitializeStateMachine()
        {
            stateMachine = new AIStateMachine(this);

            stateMachine.AddState(new IdleState());
            stateMachine.AddState(new PatrolState());
            stateMachine.AddState(new InvestigateState());
            stateMachine.AddState(new StalkState());
            stateMachine.AddState(new ChaseState());
            stateMachine.AddState(new AttackState());
            stateMachine.AddState(new FleeState());
            stateMachine.AddState(new StunnedState());

            // Start in patrol or idle
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                stateMachine.SetState<PatrolState>();
            }
            else
            {
                stateMachine.SetState<IdleState>();
            }
        }

        #region Detection

        private void CheckForPlayer()
        {
            bool spottedPlayer = false;

            // Primary path: layer-based detection (fast and explicit when configured).
            if (playerLayer.value != 0)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, sightRange, playerLayer);
                foreach (var hit in hits)
                {
                    if (!hit.CompareTag("Player"))
                        continue;

                    if (CanSeeTarget(hit.transform) || CanSenseNearbyTarget(hit.transform))
                    {
                        stateMachine.OnPlayerSpotted(hit.transform);
                        spottedPlayer = true;
                        break;
                    }
                }
            }

            // Fallback path: if layer mask is unset/misconfigured, still detect by tag.
            if (!spottedPlayer)
            {
                Transform player = GetPlayerTransformCached();
                if (player != null && (CanSeeTarget(player) || CanSenseNearbyTarget(player)))
                {
                    stateMachine.OnPlayerSpotted(player);
                    spottedPlayer = true;
                }
            }

            if (!spottedPlayer && target != null)
            {
                stateMachine.OnPlayerLost();
            }
        }

        private Transform GetPlayerTransformCached()
        {
            if (cachedPlayerTransform != null)
                return cachedPlayerTransform;

            if (Time.time - lastPlayerLookupTime < PlayerLookupInterval)
                return null;

            lastPlayerLookupTime = Time.time;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            cachedPlayerTransform = player != null ? player.transform : null;
            return cachedPlayerTransform;
        }

        public bool CanSeePlayer()
        {
            if (target == null) return false;
            return CanSeeTarget(target) || CanSenseNearbyTarget(target);
        }

        private bool CanSeeTarget(Transform targetTransform)
        {
            Vector3 directionToTarget = (targetTransform.position - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
            float effectiveSightRange = GetEffectiveSightRange(targetTransform);

            // Check range
            if (distanceToTarget > effectiveSightRange)
                return false;

            // Check angle. Allow very close targets to be noticed even outside the cone.
            if (distanceToTarget > closeProximityDetection)
            {
                float angle = Vector3.Angle(transform.forward, directionToTarget);
                if (angle > sightAngle / 2f)
                    return false;
            }

            // Check line of sight
            Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
            if (obstructionLayer.value != 0 &&
                Physics.Raycast(eyePosition, directionToTarget, distanceToTarget, obstructionLayer))
                return false;

            return true;
        }

        private bool CanSenseNearbyTarget(Transform targetTransform)
        {
            float effectiveRearAwarenessRange = GetEffectiveRearAwarenessRange(targetTransform);
            float distanceToTarget = Vector3.Distance(transform.position, targetTransform.position);
            if (distanceToTarget > effectiveRearAwarenessRange)
                return false;

            Vector3 directionToTarget = (targetTransform.position - transform.position).normalized;
            Vector3 earPosition = transform.position + Vector3.up * 1.4f;

            if (obstructionLayer.value != 0 &&
                Physics.Raycast(earPosition, directionToTarget, distanceToTarget, obstructionLayer))
            {
                return false;
            }

            return true;
        }

        private float GetEffectiveSightRange(Transform targetTransform)
        {
            float effectiveRange = sightRange;

            if (TryGetPlayerMovement(targetTransform, out PlayerMovement playerMovement))
            {
                if (playerMovement.IsSprinting)
                {
                    effectiveRange *= sprintSightRangeMultiplier;
                }
                else if (playerMovement.IsCrouching)
                {
                    float crouchMultiplier = playerMovement.IsMoving
                        ? crouchMovingSightRangeMultiplier
                        : crouchStillSightRangeMultiplier;
                    effectiveRange = Mathf.Max(minimumCrouchVisibleRange, sightRange * crouchMultiplier);
                }
            }

            return effectiveRange;
        }

        private float GetEffectiveRearAwarenessRange(Transform targetTransform)
        {
            float effectiveRange = rearAwarenessRange;

            if (TryGetPlayerMovement(targetTransform, out PlayerMovement playerMovement) &&
                playerMovement.IsCrouching &&
                !playerMovement.IsSprinting)
            {
                effectiveRange *= crouchRearAwarenessMultiplier;
            }

            return effectiveRange;
        }

        private static bool TryGetPlayerMovement(Transform targetTransform, out PlayerMovement playerMovement)
        {
            playerMovement = targetTransform.GetComponent<PlayerMovement>();
            if (playerMovement == null)
            {
                playerMovement = targetTransform.GetComponentInParent<PlayerMovement>();
            }

            return playerMovement != null;
        }

        #endregion

        #region Noise

        public void OnNoiseHeard(Vector3 noisePosition, float intensity)
        {
            if (isStunned) return;

            // Distance-based hearing falloff. This keeps loud, nearby footsteps meaningful
            // while still allowing far noises to fade out naturally.
            float distance = Vector3.Distance(transform.position, noisePosition);
            if (distance > hearingRange)
                return;

            float distanceFactor = 1f - Mathf.Clamp01(distance / Mathf.Max(0.1f, hearingRange));
            float effectiveIntensity = intensity * distanceFactor;

            if (effectiveIntensity > hearingThreshold)
            {
                stateMachine.OnNoiseHeard(noisePosition, effectiveIntensity);
            }
        }

        #endregion

        #region Combat

        public bool TryStartAttack()
        {
            if (target == null || isStunned || attackInProgress || !CanAttackNow)
                return false;

            attackInProgress = true;
            attackDamageApplied = false;

            // Force stop movement while attacking.
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            if (advancedAnimator != null)
            {
                advancedAnimator.PlayRandomAttack();
                return true;
            }

            if (enemyAnimator != null)
            {
                enemyAnimator.PlayAttack();
                return true;
            }

            // Fallback if no animator exists.
            ApplyAttackDamage();
            EndAttackAnimation();
            return true;
        }

        public void MarkAttackUsed(float cooldownScale = 1f)
        {
            float randomizedCooldown = attackCooldown * Random.Range(0.9f, 1.15f);
            nextAttackAllowedTime = Time.time + randomizedCooldown * Mathf.Max(0.1f, cooldownScale);
        }

        // Backward compatibility with existing call sites.
        public void Attack()
        {
            TryStartAttack();
        }

        public void OnAnimationAttackHit()
        {
            if (!attackInProgress) return;
            ApplyAttackDamage();
        }

        public void OnAnimationAttackEnd()
        {
            EndAttackAnimation();
        }

        public void TakeDamage(float damage)
        {
            // Enemies don't take damage in this game design
            // But they can be stunned/pushed
        }

        public void Stun(float duration)
        {
            var stunnedState = stateMachine.GetState<StunnedState>();
            if (stunnedState != null)
            {
                stunnedState.SetStunDuration(duration);
                stateMachine.SetState<StunnedState>();
            }
        }

        public void ApplyPush(Vector3 force)
        {
            // Apply push using NavMesh warp
            Vector3 pushTarget = transform.position + force;
            
            if (NavMesh.SamplePosition(pushTarget, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }

            // Brief stun from push
            Stun(1.5f);
        }

        #endregion

        #region State Setters

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetInvestigationTarget(Vector3 position)
        {
            investigationTarget = position;
        }

        public bool TryGetRandomPatrolPoint(out Vector3 patrolPoint)
        {
            patrolPoint = patrolAnchor;
            float radius = Mathf.Max(2f, randomPatrolRadius);

            for (int i = 0; i < 10; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * radius;
                randomOffset.y = 0f;
                Vector3 candidate = patrolAnchor + randomOffset;

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius * 0.6f, NavMesh.AllAreas))
                {
                    patrolPoint = hit.position;
                    return true;
                }
            }

            if (NavMesh.SamplePosition(patrolAnchor, out NavMeshHit fallbackHit, radius, NavMesh.AllAreas))
            {
                patrolPoint = fallbackHit.position;
                return true;
            }

            return false;
        }

        public void SetAlerted(bool alerted)
        {
            isAlerted = alerted;
            UpdateVisual();

            if (alerted)
            {
                EventBus.Publish(new EnemyAlertedEvent
                {
                    Enemy = gameObject,
                    LastKnownPlayerPosition = investigationTarget
                });
            }
        }

        public void SetAggressive(bool aggressive)
        {
            isAggressive = aggressive;
            UpdateVisual();
        }

        public void SetStunned(bool stunned)
        {
            isStunned = stunned;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (mainRenderer == null) return;

            Color targetColor = normalColor;
            
            if (isStunned)
            {
                targetColor = Color.blue;
            }
            else if (isAggressive)
            {
                targetColor = aggressiveColor;
            }
            else if (isAlerted)
            {
                targetColor = alertedColor;
            }

            mainRenderer.material.color = targetColor;
        }

        #endregion

        #region Pheromone Attraction

        public void AttractTo(Vector3 position)
        {
            SetInvestigationTarget(position);
            stateMachine.SetState<InvestigateState>();
        }

        #endregion

        #region Animation Methods

        /// <summary>
        /// Play attack animation using the appropriate animator.
        /// </summary>
        public void PlayAttackAnimation()
        {
            if (advancedAnimator != null)
            {
                advancedAnimator.PlayRandomAttack();
            }
            else if (enemyAnimator != null)
            {
                enemyAnimator.PlayAttack();
            }
        }

        /// <summary>
        /// Play stunned/hit reaction animation.
        /// </summary>
        public void PlayStunnedAnimation()
        {
            if (advancedAnimator != null)
            {
                advancedAnimator.PlayHitReaction();
            }
            else if (enemyAnimator != null)
            {
                enemyAnimator.PlayHitReaction();
            }
        }

        /// <summary>
        /// Play death animation.
        /// </summary>
        public void PlayDeathAnimation()
        {
            if (advancedAnimator != null)
            {
                advancedAnimator.PlayDeath();
            }
            else if (enemyAnimator != null)
            {
                enemyAnimator.PlayDeath();
            }
        }

        /// <summary>
        /// Initialize animation components. Call after setting up the enemy.
        /// </summary>
        private void InitializeAnimationComponents()
        {
            animator = GetComponent<Animator>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            enemyAnimator = GetComponent<EnemyAnimator>();
            if (enemyAnimator == null) enemyAnimator = GetComponentInChildren<EnemyAnimator>();

            advancedAnimator = GetComponent<EnemyAnimatorAdvanced>();
            if (advancedAnimator == null) advancedAnimator = GetComponentInChildren<EnemyAnimatorAdvanced>();

            healthComponent = GetComponent<HealthComponent>();
            HookAnimationEvents();
        }

        private void HookAnimationEvents()
        {
            if (enemyAnimator != null)
            {
                enemyAnimator.OnAttackHit -= OnAnimationAttackHit;
                enemyAnimator.OnAttackHit += OnAnimationAttackHit;
                enemyAnimator.OnAttackEnd -= OnAnimationAttackEnd;
                enemyAnimator.OnAttackEnd += OnAnimationAttackEnd;
                enemyAnimator.OnDeathComplete -= HandleDeathAnimationComplete;
                enemyAnimator.OnDeathComplete += HandleDeathAnimationComplete;
            }

            if (advancedAnimator != null)
            {
                advancedAnimator.OnAttackHitFrame -= OnAnimationAttackHit;
                advancedAnimator.OnAttackHitFrame += OnAnimationAttackHit;
                advancedAnimator.OnAttackComplete -= OnAnimationAttackEnd;
                advancedAnimator.OnAttackComplete += OnAnimationAttackEnd;
                advancedAnimator.OnDeathComplete -= HandleDeathAnimationComplete;
                advancedAnimator.OnDeathComplete += HandleDeathAnimationComplete;
            }
        }

        private void UnhookAnimationEvents()
        {
            if (enemyAnimator != null)
            {
                enemyAnimator.OnAttackHit -= OnAnimationAttackHit;
                enemyAnimator.OnAttackEnd -= OnAnimationAttackEnd;
                enemyAnimator.OnDeathComplete -= HandleDeathAnimationComplete;
            }

            if (advancedAnimator != null)
            {
                advancedAnimator.OnAttackHitFrame -= OnAnimationAttackHit;
                advancedAnimator.OnAttackComplete -= OnAnimationAttackEnd;
                advancedAnimator.OnDeathComplete -= HandleDeathAnimationComplete;
            }
        }

        private void ApplyAttackDamage()
        {
            if (attackDamageApplied || target == null)
                return;

            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > attackRange * 1.35f)
                return;

            float damageMultiplier = advancedAnimator != null ? advancedAnimator.GetCurrentDamageMultiplier() : 1f;
            float finalDamage = attackDamage * damageMultiplier;

            HealthComponent targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth == null)
            {
                targetHealth = target.GetComponentInParent<HealthComponent>();
            }

            if (targetHealth != null)
            {
                targetHealth.TakeDamage(finalDamage);
                attackDamageApplied = true;
            }
        }

        private void EndAttackAnimation()
        {
            attackInProgress = false;
            attackDamageApplied = false;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }
        }

        private void HandleDeathAnimationComplete()
        {
            OnDeath?.Invoke();
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Sight range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, sightRange);

            // Sight angle
            Vector3 leftBound = Quaternion.Euler(0, -sightAngle / 2, 0) * transform.forward * sightRange;
            Vector3 rightBound = Quaternion.Euler(0, sightAngle / 2, 0) * transform.forward * sightRange;
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + leftBound);
            Gizmos.DrawLine(transform.position, transform.position + rightBound);

            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Patrol points
            if (patrolPoints != null)
            {
                Gizmos.color = Color.cyan;
                foreach (var point in patrolPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawSphere(point.position, 0.5f);
                    }
                }
            }
        }

        private void OnValidate()
        {
            if (playerLayer.value == 0)
            {
                Debug.LogWarning($"[EnemyAI] {name}: Player Layer is not configured. Detection will fall back to tag lookup.");
            }
            if (obstructionLayer.value == 0)
            {
                Debug.LogWarning($"[EnemyAI] {name}: Obstruction Layer is not configured. Line-of-sight checks will ignore walls.");
            }
        }

        #endregion
    }
}
