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
            enemy.Agent.isStopped = false;
        }

        public override void OnNoiseHeard(Vector3 position, float intensity)
        {
            if (intensity > enemy.HearingThreshold)
            {
                enemy.SetInvestigationTarget(position);
                stateMachine.SetState<InvestigateState>();
            }
        }

        public override void OnPlayerSpotted(Transform player)
        {
            enemy.SetTarget(player);
            
            if (enemy.BehaviorType == EnemyBehaviorType.Flee)
            {
                stateMachine.SetState<FleeState>();
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
            if (!enemy.Agent.pathPending && enemy.Agent.remainingDistance < 0.5f)
            {
                waiting = true;
                waitTime = Random.Range(1f, 3f);
            }
        }

        public override void Exit() { }

        private void MoveToNextWaypoint()
        {
            if (enemy.PatrolPoints == null || enemy.PatrolPoints.Length == 0)
            {
                // Random patrol
                Vector3 randomPoint = enemy.transform.position + Random.insideUnitSphere * 10f;
                randomPoint.y = enemy.transform.position.y;

                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    enemy.Agent.SetDestination(hit.position);
                }
                return;
            }

            currentWaypointIndex = (currentWaypointIndex + 1) % enemy.PatrolPoints.Length;
            enemy.Agent.SetDestination(enemy.PatrolPoints[currentWaypointIndex].position);
        }

        public override void OnNoiseHeard(Vector3 position, float intensity)
        {
            if (intensity > enemy.HearingThreshold)
            {
                enemy.SetInvestigationTarget(position);
                stateMachine.SetState<InvestigateState>();
            }
        }

        public override void OnPlayerSpotted(Transform player)
        {
            enemy.SetTarget(player);
            
            if (enemy.BehaviorType == EnemyBehaviorType.Flee)
            {
                stateMachine.SetState<FleeState>();
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
            
            if (enemy.InvestigationTarget != Vector3.zero)
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
            if (!enemy.Agent.pathPending && enemy.Agent.remainingDistance < 1f)
            {
                if (!reachedTarget)
                {
                    reachedTarget = true;
                    // Look around
                    enemy.Agent.isStopped = true;
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
            enemy.Agent.isStopped = false;
            enemy.SetAlerted(false);
        }

        public override void OnNoiseHeard(Vector3 position, float intensity)
        {
            if (intensity > enemy.HearingThreshold * 0.5f)
            {
                enemy.SetInvestigationTarget(position);
                enemy.Agent.SetDestination(position);
                investigateTime = 0f;
                reachedTarget = false;
            }
        }

        public override void OnPlayerSpotted(Transform player)
        {
            enemy.SetTarget(player);
            
            if (enemy.BehaviorType == EnemyBehaviorType.Flee)
            {
                stateMachine.SetState<FleeState>();
            }
            else
            {
                stateMachine.SetState<ChaseState>();
            }
        }
    }

    #endregion

    #region Chase State

    public class ChaseState : AIState
    {
        private float lostPlayerTime;
        private const float LosePlayerDelay = 5f;
        private Vector3 lastKnownPosition;

        public override void Enter()
        {
            lostPlayerTime = 0f;
            enemy.Agent.speed = enemy.ChaseSpeed;
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
                enemy.Agent.SetDestination(lastKnownPosition);
                
                if (enemy.Agent.remainingDistance < 1f)
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
                enemy.Agent.SetDestination(enemy.Target.position);

                // Check attack range
                float distance = Vector3.Distance(enemy.transform.position, enemy.Target.position);
                if (distance <= enemy.AttackRange)
                {
                    stateMachine.SetState<AttackState>();
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
            enemy.Agent.speed = enemy.WalkSpeed;
            enemy.SetAggressive(false);
        }

        public override void OnPlayerLost()
        {
            enemy.SetInvestigationTarget(lastKnownPosition);
            stateMachine.SetState<InvestigateState>();
        }
    }

    #endregion

    #region Attack State

    public class AttackState : AIState
    {
        private float attackCooldown;

        public override void Enter()
        {
            enemy.Agent.isStopped = true;
            attackCooldown = 0f;
        }

        public override void Update()
        {
            if (enemy.Target == null)
            {
                stateMachine.SetState<InvestigateState>();
                return;
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
                stateMachine.SetState<ChaseState>();
                return;
            }

            // Attack
            attackCooldown -= Time.deltaTime;
            if (attackCooldown <= 0f)
            {
                enemy.Attack();
                attackCooldown = enemy.AttackCooldown;
            }
        }

        public override void Exit()
        {
            enemy.Agent.isStopped = false;
        }
    }

    #endregion

    #region Flee State

    public class FleeState : AIState
    {
        private float fleeTime;
        private const float MinFleeTime = 5f;

        public override void Enter()
        {
            fleeTime = 0f;
            enemy.Agent.speed = enemy.ChaseSpeed;
            FleeFromPlayer();
        }

        public override void Update()
        {
            fleeTime += Time.deltaTime;

            // Update flee direction
            if (enemy.Agent.remainingDistance < 1f || fleeTime % 2f < Time.deltaTime)
            {
                FleeFromPlayer();
            }

            // Check if safe
            if (fleeTime >= MinFleeTime && !enemy.CanSeePlayer())
            {
                float distance = enemy.Target != null ? 
                    Vector3.Distance(enemy.transform.position, enemy.Target.position) : 
                    float.MaxValue;

                if (distance > enemy.SightRange * 2f)
                {
                    stateMachine.SetState<PatrolState>();
                }
            }
        }

        public override void Exit()
        {
            enemy.Agent.speed = enemy.WalkSpeed;
        }

        private void FleeFromPlayer()
        {
            if (enemy.Target == null) return;

            Vector3 fleeDirection = (enemy.transform.position - enemy.Target.position).normalized;
            Vector3 fleePoint = enemy.transform.position + fleeDirection * 15f;

            if (NavMesh.SamplePosition(fleePoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                enemy.Agent.SetDestination(hit.position);
            }
        }

        public override void OnPlayerSpotted(Transform player)
        {
            // Keep fleeing!
            FleeFromPlayer();
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
            enemy.Agent.isStopped = true;
            enemy.SetStunned(true);
        }

        public override void Update()
        {
            stunTime += Time.deltaTime;
            
            if (stunTime >= stunDuration)
            {
                stateMachine.SetState<IdleState>();
            }
        }

        public override void Exit()
        {
            enemy.Agent.isStopped = false;
            enemy.SetStunned(false);
        }
    }

    #endregion
}
