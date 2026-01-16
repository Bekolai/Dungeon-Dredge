using UnityEngine;
using System.Collections.Generic;

namespace DungeonDredge.AI
{
    public abstract class AIState
    {
        protected AIStateMachine stateMachine;
        protected EnemyAI enemy;

        public virtual void Initialize(AIStateMachine sm, EnemyAI ai)
        {
            stateMachine = sm;
            enemy = ai;
        }

        public abstract void Enter();
        public abstract void Update();
        public abstract void Exit();

        public virtual void OnNoiseHeard(Vector3 position, float intensity) { }
        public virtual void OnPlayerSpotted(Transform player) { }
        public virtual void OnPlayerLost() { }
    }

    public class AIStateMachine
    {
        private Dictionary<System.Type, AIState> states = new Dictionary<System.Type, AIState>();
        private AIState currentState;
        private EnemyAI enemy;

        public AIState CurrentState => currentState;
        public System.Type CurrentStateType => currentState?.GetType();

        public AIStateMachine(EnemyAI enemy)
        {
            this.enemy = enemy;
        }

        public void AddState(AIState state)
        {
            state.Initialize(this, enemy);
            states[state.GetType()] = state;
        }

        public void SetState<T>() where T : AIState
        {
            System.Type type = typeof(T);
            if (states.TryGetValue(type, out AIState newState))
            {
                currentState?.Exit();
                currentState = newState;
                currentState.Enter();
            }
            else
            {
                Debug.LogWarning($"State {type.Name} not found!");
            }
        }

        public void SetState(System.Type type)
        {
            if (states.TryGetValue(type, out AIState newState))
            {
                currentState?.Exit();
                currentState = newState;
                currentState.Enter();
            }
        }

        public void Update()
        {
            currentState?.Update();
        }

        public T GetState<T>() where T : AIState
        {
            if (states.TryGetValue(typeof(T), out AIState state))
            {
                return state as T;
            }
            return null;
        }

        public void OnNoiseHeard(Vector3 position, float intensity)
        {
            currentState?.OnNoiseHeard(position, intensity);
        }

        public void OnPlayerSpotted(Transform player)
        {
            currentState?.OnPlayerSpotted(player);
        }

        public void OnPlayerLost()
        {
            currentState?.OnPlayerLost();
        }
    }
}
