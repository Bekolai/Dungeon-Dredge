using UnityEngine;
using UnityEngine.AI;

namespace DungeonDredge.AI
{
    #region Idle State

    public class IdleState : AIState
    {
        private float idleTime;
        private float idleDuration;

        public override void Enter()
        {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
                enemy.Agent.isStopped = true;
            idleTime = 0f;
            idleDuration = Random.Range(2f, 5f);
        
        }

        public override void Update()
        {
            idleTime += Time.deltaTime;
            
            if (idleTime >= idleDuration)
            {
                stateMachine.SetState<PatrolState>();
            }
        }

        public override void Exit()
        {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
                enemy.Agent.isStopped = false;
        }

        public override void OnNoiseHeard(Vector3 position, float intensity)
        {
            enemy.SetInvestigationTarget(position);
            stateMachine.SetState<InvestigateState>();
        }

        public override void OnPlayerSpotted(Transform player)
        {
            enemy.SetTarget(player);
            
            if (enemy.BehaviorType == EnemyBehaviorType.Flee)
            {
                stateMachine.SetState<FleeState>();
            }
            else if (enemy.BehaviorType == EnemyBehaviorType.Stalker)
            {
                stateMachine.SetState<StalkState>();
            }
            else
            {
                stateMachine.SetState<ChaseState>();
            }
        }
    }

    #endregion

    #region Patrol State

    public class PatrolState : AIState
    {
        private int currentWaypointIndex;
        private float waitTime;
        private bool waiting;

        public override void Enter()
        {
            waiting = false;
            MoveToNextWaypoint();
        }

        public override void Update()
        {
            if (waiting)
            {
                waitTime -= Time.deltaTime;
                if (waitTime <= 0)
                {
                    waiting = false;
                    MoveToNextWaypoint();
                }
                return;
            }

            // Check if reached waypoint
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh && 
                !enemy.Agent.pathPending && enemy.Agent.remainingDistance <= enemy.PatrolPointReachThreshold)
            {
                waiting = true;
                waitTime = Random.Range(1f, 3f);
            }
        }

        public override void Exit() { }

        private void MoveToNextWaypoint()
        {
            if (enemy.PatrolPoints == null || enemy.PatrolPoints.Length < 2)
            {
                if (enemy.TryGetRandomPatrolPoint(out Vector3 randomPatrolPoint))
                if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
                {
                    enemy.Agent.SetDestination(randomPatrolPoint);
                }
                else
                {
                    waiting = true;
                    waitTime = 0.5f;
                }
                return;
            }

            currentWaypointIndex = (currentWaypointIndex + 1) % enemy.PatrolPoints.Length;
            Transform nextPoint = enemy.PatrolPoints[currentWaypointIndex];
            if (nextPoint == null || enemy.Agent == null || !enemy.Agent.isActiveAndEnabled || !enemy.Agent.isOnNavMesh || !enemy.Agent.SetDestination(nextPoint.position))
            {
                waiting = true;
                waitTime = 0.5f;
            }
        }

        public override void OnNoiseHeard(Vector3 position, float intensity)
        {
            enemy.SetInvestigationTarget(position);
            stateMachine.SetState<InvestigateState>();
        }

        public override void OnPlayerSpotted(Transform player)
        {
            enemy.SetTarget(player);
            
            if (enemy.BehaviorType == EnemyBehaviorType.Flee)
            {
                stateMachine.SetState<FleeState>();
            }
            else if (enemy.BehaviorType == EnemyBehaviorType.Stalker)
            {
                stateMachine.SetState<StalkState>();
            }
            else
            {
                stateMachine.SetState<ChaseState>();
            }
        }
    }

    #endregion

    #region Investigate State

    public class InvestigateState : AIState
    {
        private float investigateTime;
        private const float MaxInvestigateTime = 10f;
        private bool reachedTarget;

        public override void Enter()
        {
            investigateTime = 0f;
            reachedTarget = false;
            
            if (enemy.InvestigationTarget != Vector3.zero && enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.SetDestination(enemy.InvestigationTarget);
            }

            // Increase alertness
            enemy.SetAlerted(true);
        }

        public override void Update()
        {
            investigateTime += Time.deltaTime;

            // Check if reached investigation point
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh && 
                !enemy.Agent.pathPending && enemy.Agent.remainingDistance < 1f)
            {
                if (!reachedTarget)
                {
                    reachedTarget = true;
                    // Look around
                    if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
                    {
                        enemy.Agent.isStopped = true;
                    }
                }
            }

            // Timeout - return to patrol
            if (investigateTime >= MaxInvestigateTime)
            {
                stateMachine.SetState<PatrolState>();
            }
        }

        public override void Exit()
        {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.isStopped = false;
            }
            enemy.SetAlerted(false);
        }

        public override void OnNoiseHeard(Vector3 position, float intensity)
        {
            enemy.SetInvestigationTarget(position);
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.SetDestination(position);
            }
            investigateTime = 0f;
            reachedTarget = false;
        }

        public override void OnPlayerSpotted(Transform player)
        {
            enemy.SetTarget(player);
            
            if (enemy.BehaviorType == EnemyBehaviorType.Flee)
            {
                stateMachine.SetState<FleeState>();
            }
            else if (enemy.BehaviorType == EnemyBehaviorType.Stalker)
            {
                stateMachine.SetState<StalkState>();
            }
            else
            {
                stateMachine.SetState<ChaseState>();
            }
        }
    }

    #endregion

    #region Stalk State

    public class StalkState : AIState
    {
        private float lostPlayerTime;
        private float strafeTimer;
        private float nextStrikeTime;
        private int strafeDirection = 1;

        private const float LosePlayerDelay = 6f;
        private const float DesiredMinDistance = 5f;
        private const float DesiredMaxDistance = 9f;
        private const float StrafeRadius = 3f;

        public override void Enter()
        {
            lostPlayerTime = 0f;
            strafeTimer = Random.Range(1f, 2f);
            nextStrikeTime = Time.time + Random.Range(1.5f, 3f);
            strafeDirection = Random.value < 0.5f ? -1 : 1;

            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.speed = enemy.WalkSpeed;
            }
            enemy.SetAlerted(true);
            enemy.SetAggressive(false);
        }

        public override void Update()
        {
            if (enemy.Target == null)
            {
                stateMachine.SetState<InvestigateState>();
                return;
            }

            if (!enemy.CanSeePlayer())
            {
                lostPlayerTime += Time.deltaTime;
                if (lostPlayerTime >= LosePlayerDelay)
                {
                    enemy.SetInvestigationTarget(enemy.Target.position);
                    stateMachine.SetState<InvestigateState>();
                }
                return;
            }

            lostPlayerTime = 0f;

            float distance = Vector3.Distance(enemy.transform.position, enemy.Target.position);
            if (distance <= enemy.AttackRange * 1.05f && Time.time >= nextStrikeTime)
            {
                stateMachine.SetState<AttackState>();
                return;
            }

            if (distance > DesiredMaxDistance)
            {
                if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
                {
                    enemy.Agent.SetDestination(enemy.Target.position);
                }
                return;
            }

            if (distance < DesiredMinDistance)
            {
                Vector3 away = (enemy.transform.position - enemy.Target.position).normalized;
                Vector3 retreatPoint = enemy.transform.position + away * 4f;
                if (NavMesh.SamplePosition(retreatPoint, out NavMeshHit retreatHit, 5f, NavMesh.AllAreas))
                {
                    if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
                    {
                        enemy.Agent.SetDestination(retreatHit.position);
                    }
                }
                return;
            }

            strafeTimer -= Time.deltaTime;
            if (strafeTimer <= 0f)
            {
                strafeTimer = Random.Range(0.75f, 1.5f);
                strafeDirection *= -1;
            }

            Vector3 toTarget = (enemy.Target.position - enemy.transform.position).normalized;
            Vector3 strafe = Vector3.Cross(Vector3.up, toTarget).normalized * strafeDirection;
            Vector3 orbitPoint = enemy.Target.position + toTarget * -DesiredMinDistance + strafe * StrafeRadius;
            if (NavMesh.SamplePosition(orbitPoint, out NavMeshHit orbitHit, 4f, NavMesh.AllAreas))
            {
                if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
                {
                    enemy.Agent.SetDestination(orbitHit.position);
                }
            }
        }

        public override void Exit()
        {
            enemy.SetAlerted(false);
        }

        public override void OnPlayerSpotted(Transform player)
        {
            enemy.SetTarget(player);
        }
    }

    #endregion

    #region Chase State

    public class ChaseState : AIState
    {
        private float lostPlayerTime;
        private const float LosePlayerDelay = 5f;
        private Vector3 lastKnownPosition;
        private float strafeDirectionTimer;
        private int strafeDirection = 1;

        public override void Enter()
        {
            lostPlayerTime = 0f;
            strafeDirectionTimer = Random.Range(0.6f, 1.1f);
            strafeDirection = Random.value < 0.5f ? -1 : 1;
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.speed = enemy.ChaseSpeed;
            }
            enemy.SetAggressive(true);

            if (enemy.Target != null)
            {
                lastKnownPosition = enemy.Target.position;
            }
        }

        public override void Update()
        {
            if (enemy.Target == null)
            {
                // Lost target, go to last known position
                if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
                {
                    enemy.Agent.SetDestination(lastKnownPosition);
                }
                
                if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh && enemy.Agent.remainingDistance < 1f)
                {
                    stateMachine.SetState<InvestigateState>();
                }
                return;
            }

            // Check if player is still visible
            if (enemy.CanSeePlayer())
            {
                lostPlayerTime = 0f;
                lastKnownPosition = enemy.Target.position;
                if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
                {
                    enemy.Agent.SetDestination(enemy.Target.position);
                }

                // Check attack range
                float distance = Vector3.Distance(enemy.transform.position, enemy.Target.position);
                if (distance <= enemy.AttackRange)
                {
                    if (enemy.CanAttackNow)
                    {
                        stateMachine.SetState<AttackState>();
                    }
                    else
                    {
                        // Keep movement pressure while waiting for cooldown.
                        StrafeAroundTarget();
                    }
                }
            }
            else
            {
                // Lost sight of player
                lostPlayerTime += Time.deltaTime;
                
                if (lostPlayerTime >= LosePlayerDelay)
                {
                    enemy.SetInvestigationTarget(lastKnownPosition);
                    stateMachine.SetState<InvestigateState>();
                }
            }
        }

        public override void Exit()
        {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.speed = enemy.WalkSpeed;
            }
            enemy.SetAggressive(false);
        }

        public override void OnPlayerLost()
        {
            enemy.SetInvestigationTarget(lastKnownPosition);
            stateMachine.SetState<InvestigateState>();
        }

        private void StrafeAroundTarget()
        {
            if (enemy.Target == null) return;

            strafeDirectionTimer -= Time.deltaTime;
            if (strafeDirectionTimer <= 0f)
            {
                strafeDirection *= -1;
                strafeDirectionTimer = Random.Range(0.6f, 1.2f);
            }

            Vector3 toTarget = (enemy.Target.position - enemy.transform.position).normalized;
            Vector3 lateral = Vector3.Cross(Vector3.up, toTarget).normalized * strafeDirection;
            Vector3 strafePoint = enemy.Target.position - toTarget * (enemy.AttackRange * 1.1f) + lateral * 2f;

            if (NavMesh.SamplePosition(strafePoint, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
                {
                    enemy.Agent.SetDestination(hit.position);
                }
            }
        }
    }

    #endregion

    #region Attack State

    public class AttackState : AIState
    {
        private float attackCooldown;
        private bool hasCommittedAttack;

        public override void Enter()
        {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.isStopped = true;
                enemy.Agent.ResetPath();
            }
            attackCooldown = 0f;   
            hasCommittedAttack = false;
        }

        public override void Update()
        {
            if (enemy.Target == null)
            {
                stateMachine.SetState<InvestigateState>();
                return;
            }

            if (enemy.Agent != null && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.isStopped = true;
            }

            // Face target
            Vector3 direction = (enemy.Target.position - enemy.transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                enemy.transform.rotation = Quaternion.Slerp(
                    enemy.transform.rotation,
                    Quaternion.LookRotation(direction),
                    Time.deltaTime * 5f);
            }

            // Check if still in range
            float distance = Vector3.Distance(enemy.transform.position, enemy.Target.position);
            if (distance > enemy.AttackRange * 1.2f)
            {
                if (enemy.BehaviorType == EnemyBehaviorType.Stalker)
                {
                    stateMachine.SetState<StalkState>();
                }
                else
                {
                    stateMachine.SetState<ChaseState>();
                }
                return;
            }

            // Attack animation is event-driven; only start a new swing when idle.
            if (enemy.IsAttackInProgress)
                return;

            // One hit per engage: once a strike was committed, disengage immediately.
            if (hasCommittedAttack)
            {
                if (enemy.BehaviorType == EnemyBehaviorType.Stalker)
                {
                    stateMachine.SetState<StalkState>();
                }
                else
                {
                    stateMachine.SetState<ChaseState>();
                }
                return;
            }

            if (enemy.BehaviorType == EnemyBehaviorType.Stalker && attackCooldown > 0f)
            {
                stateMachine.SetState<StalkState>();
                return;
            }

            attackCooldown -= Time.deltaTime;
            if (attackCooldown <= 0f && enemy.CanAttackNow)
            {
                bool started = enemy.TryStartAttack();
                if (started)
                {
                    hasCommittedAttack = true;
                    // Stalkers should disengage a bit longer between attempts.
                    float cooldownScale = enemy.BehaviorType == EnemyBehaviorType.Stalker ? 1.25f : 1f;
                    enemy.MarkAttackUsed(cooldownScale);
                    attackCooldown = enemy.AttackCooldown;
                }
            }
        }

        public override void Exit()
        {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.isStopped = false;
            }
        }
    }

    #endregion

    #region Flee State

    public class FleeState : AIState
    {
        private float fleeTime;
        private float lostSightTime;
        private float stuckTime;
        private const float MinFleeTime = 5f;
        private const float LostSightReturnDelay = 2.5f;
        private const float MaxFleeTime = 12f;
        private const float StuckVelocityThreshold = 0.05f;
        private const float StuckRepathDelay = 0.9f;

        public override void Enter()
        {
            fleeTime = 0f;
            lostSightTime = 0f;
            stuckTime = 0f;
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.speed = enemy.ChaseSpeed;
            }
            FleeFromPlayer();
        }

        public override void Update()
        {
            fleeTime += Time.deltaTime;
            bool canSeePlayer = enemy.CanSeePlayer();

            if (canSeePlayer)
            {
                lostSightTime = 0f;
            }
            else
            {
                lostSightTime += Time.deltaTime;
            }

            // Repath while fleeing if destination was reached or frequently while player is visible.
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh && 
                (enemy.Agent.remainingDistance < 1f || (canSeePlayer && fleeTime % 2f < Time.deltaTime)))
            {
                FleeFromPlayer();
            }

            // If the agent is barely moving while still trying to flee, force a new route.
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh &&
                enemy.Agent.hasPath &&
                !enemy.Agent.pathPending &&
                enemy.Agent.remainingDistance > 1f &&
                enemy.Agent.velocity.sqrMagnitude <= StuckVelocityThreshold * StuckVelocityThreshold)
            {
                stuckTime += Time.deltaTime;
                if (stuckTime >= StuckRepathDelay)
                {
                    stuckTime = 0f;
                    FleeFromPlayer();
                }
            }
            else
            {
                stuckTime = 0f;
            }

            // Return to patrol after enough safe time, or force-return after max flee duration.
            if ((fleeTime >= MinFleeTime && lostSightTime >= LostSightReturnDelay) || fleeTime >= MaxFleeTime)
            {
                enemy.SetTarget(null);
                stateMachine.SetState<PatrolState>();
            }
        }

        public override void Exit()
        {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.speed = enemy.WalkSpeed;
            }
        }

        private void FleeFromPlayer()
        {
            Vector3 fleeOrigin = enemy.transform.position;
            Vector3 primaryDirection = Vector3.zero;

            if (enemy.Target != null)
            {
                primaryDirection = (enemy.transform.position - enemy.Target.position).normalized;
            }
            else if (enemy.TryGetRandomPatrolPoint(out Vector3 patrolFallback))
            {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.SetDestination(patrolFallback);
            }
                return;
            }

            // Try several candidate points so enemies don't keep choosing blocked walls.
            for (int i = 0; i < 8; i++)
            {
                Vector3 lateral = Quaternion.Euler(0f, Random.Range(-55f, 55f), 0f) * primaryDirection;
                Vector3 candidate = fleeOrigin + lateral * Random.Range(10f, 16f);

                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 8f, NavMesh.AllAreas))
                {
                    if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
                    {
                        if (enemy.Agent.SetDestination(hit.position))
                        {
                            return;
                        }
                    }
                }
            }

            // Last fallback: roam point near patrol anchor.
            if (enemy.TryGetRandomPatrolPoint(out Vector3 fallbackPoint))
            {
                if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
                {
                    enemy.Agent.SetDestination(fallbackPoint);
                }
            }
        }

        public override void OnPlayerSpotted(Transform player)
        {
            enemy.SetTarget(player);
            FleeFromPlayer();
        }

        public override void OnPlayerLost()
        {
            // Keep current flee target; return-to-patrol is handled by lostSightTime.
        }
    }

    #endregion

    #region Stunned State

    public class StunnedState : AIState
    {
        private float stunDuration;
        private float stunTime;

        public void SetStunDuration(float duration)
        {
            stunDuration = duration;
        }

        public override void Enter()
        {
            stunTime = 0f;
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.isStopped = true;
            }
            enemy.SetStunned(true);
             enemy.PlayStunnedAnimation(); // NEW
        }

        public override void Update()
        {
            stunTime += Time.deltaTime;
            
            if (stunTime >= stunDuration)
            {
                if (enemy.BehaviorType == EnemyBehaviorType.Flee)
                {
                    stateMachine.SetState<FleeState>();
                }
                else if (enemy.Target != null && enemy.CanSeePlayer())
                {
                    if (enemy.BehaviorType == EnemyBehaviorType.Stalker)
                    {
                        stateMachine.SetState<StalkState>();
                    }
                    else
                    {
                        stateMachine.SetState<ChaseState>();
                    }
                }
                else
                {
                    stateMachine.SetState<PatrolState>();
                }
            }
        }

        public override void Exit()
        {
            if (enemy.Agent != null && enemy.Agent.isActiveAndEnabled && enemy.Agent.isOnNavMesh)
            {
                enemy.Agent.isStopped = false;
            }
            enemy.SetStunned(false);
        }
    }

    #endregion
}
