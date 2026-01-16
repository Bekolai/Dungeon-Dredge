using UnityEngine;
using System.Collections.Generic;
using DungeonDredge.Core;

namespace DungeonDredge.Core
{
    public class StealthManager : MonoBehaviour
    {
        public static StealthManager Instance { get; private set; }

        [Header("Noise Settings")]
        [SerializeField] private float noiseDecayRate = 2f;
        [SerializeField] private float maxNoiseLevel = 2f;
        [SerializeField] private float noiseCheckInterval = 0.1f;

        [Header("Threat Detection")]
        [SerializeField] private float baseThreatRadius = 20f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private LayerMask wallLayer;

        // State
        private float currentNoiseLevel;
        private float nearestThreatDistance = float.MaxValue;
        private GameObject nearestThreat;
        private List<NoiseEvent> recentNoises = new List<NoiseEvent>();
        private float lastNoiseCheck;

        // Tracked enemies
        private List<GameObject> activeEnemies = new List<GameObject>();

        // Properties
        public float CurrentNoiseLevel => currentNoiseLevel;
        public float NearestThreatDistance => nearestThreatDistance;
        public GameObject NearestThreat => nearestThreat;
        public float NoiseRatio => currentNoiseLevel / maxNoiseLevel;

        // Events
        public System.Action<float> OnNoiseChanged;
        public System.Action<float, GameObject> OnThreatDetected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<NoiseEvent>(OnNoiseEvent);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NoiseEvent>(OnNoiseEvent);
        }

        private void Update()
        {
            DecayNoise();
            
            // Periodically check for threats
            if (Time.time - lastNoiseCheck >= noiseCheckInterval)
            {
                lastNoiseCheck = Time.time;
                UpdateNearestThreat();
            }
        }

        private void OnNoiseEvent(NoiseEvent evt)
        {
            RegisterNoise(evt.Position, evt.Intensity);
        }

        public void RegisterNoise(Vector3 position, float intensity)
        {
            currentNoiseLevel = Mathf.Min(maxNoiseLevel, currentNoiseLevel + intensity);
            
            recentNoises.Add(new NoiseEvent 
            { 
                Position = position, 
                Intensity = intensity 
            });

            // Keep only recent noises
            while (recentNoises.Count > 10)
            {
                recentNoises.RemoveAt(0);
            }

            OnNoiseChanged?.Invoke(currentNoiseLevel);

            // Alert nearby enemies
            AlertNearbyEnemies(position, intensity);
        }

        private void DecayNoise()
        {
            if (currentNoiseLevel > 0)
            {
                currentNoiseLevel -= noiseDecayRate * Time.deltaTime;
                currentNoiseLevel = Mathf.Max(0f, currentNoiseLevel);
                OnNoiseChanged?.Invoke(currentNoiseLevel);
            }
        }

        private void AlertNearbyEnemies(Vector3 noisePosition, float intensity)
        {
            float alertRadius = intensity * 10f; // 10 units per intensity point

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null) continue;

                float distance = Vector3.Distance(enemy.transform.position, noisePosition);
                if (distance <= alertRadius)
                {
                    // Check if there's a wall between noise source and enemy
                    if (!IsBlockedByWall(noisePosition, enemy.transform.position))
                    {
                        // Send alert to enemy AI
                        var enemyAI = enemy.GetComponent<INoiseListener>();
                        enemyAI?.OnNoiseHeard(noisePosition, intensity);
                    }
                }
            }
        }

        private void UpdateNearestThreat()
        {
            Transform player = GetPlayerTransform();
            if (player == null) return;

            nearestThreatDistance = float.MaxValue;
            nearestThreat = null;

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null) continue;

                float distance = Vector3.Distance(player.position, enemy.transform.position);
                
                // Check if enemy is aggressive/chasing
                var enemyAI = enemy.GetComponent<IEnemyState>();
                if (enemyAI != null && enemyAI.IsAggressive)
                {
                    if (distance < nearestThreatDistance)
                    {
                        nearestThreatDistance = distance;
                        nearestThreat = enemy;
                    }
                }
            }

            if (nearestThreat != null)
            {
                OnThreatDetected?.Invoke(nearestThreatDistance, nearestThreat);
            }
        }

        private bool IsBlockedByWall(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            
            return Physics.Raycast(from, direction.normalized, distance, wallLayer);
        }

        private Transform GetPlayerTransform()
        {
            // Find player by tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player?.transform;
        }

        #region Enemy Registration

        public void RegisterEnemy(GameObject enemy)
        {
            if (!activeEnemies.Contains(enemy))
            {
                activeEnemies.Add(enemy);
            }
        }

        public void UnregisterEnemy(GameObject enemy)
        {
            activeEnemies.Remove(enemy);
        }

        public void ClearAllEnemies()
        {
            activeEnemies.Clear();
        }

        #endregion

        #region Utility

        /// <summary>
        /// Check if player can be detected from a given position
        /// </summary>
        public bool CanDetectPlayer(Vector3 fromPosition, float detectionRange)
        {
            Transform player = GetPlayerTransform();
            if (player == null) return false;

            float distance = Vector3.Distance(fromPosition, player.position);
            if (distance > detectionRange) return false;

            return !IsBlockedByWall(fromPosition, player.position);
        }

        /// <summary>
        /// Get the player's current visibility level (affected by light, movement, etc.)
        /// </summary>
        public float GetPlayerVisibility()
        {
            // Base visibility
            float visibility = 0.5f;

            // Increase with noise
            visibility += currentNoiseLevel * 0.25f;

            // Could add light detection here
            
            return Mathf.Clamp01(visibility);
        }

        #endregion
    }

    // Interfaces for enemy AI
    public interface INoiseListener
    {
        void OnNoiseHeard(Vector3 noisePosition, float intensity);
    }

    public interface IEnemyState
    {
        bool IsAggressive { get; }
        bool IsAlerted { get; }
    }
}
