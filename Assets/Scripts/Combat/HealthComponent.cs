using UnityEngine;
using UnityEngine.Events;
using DungeonDredge.Core;
using System;
using DungeonDredge.Audio;

namespace DungeonDredge.Core
{
    public class HealthComponent : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private bool _destroyOnDeath = true;

        [Header("Events")]
        public UnityEvent OnDeath;
        public static Action OnPlayerDeathEvent;
        public UnityEvent<float> OnTakeDamage;
        public UnityEvent<float, float> OnHealthChanged; // Current, Max

        public float CurrentHealth { get; private set; }
        public float MaxHealth => _maxHealth; // Expose MaxHealth
        public bool IsDead { get; private set; }

        public bool IsPlayer => gameObject.CompareTag("Player");
        public PlayerVoiceManager PlayerVoiceManager { get; private set; }

        private void Awake()
        {


            CurrentHealth = _maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
            if (PlayerVoiceManager == null && IsPlayer)
                PlayerVoiceManager = GetComponent<PlayerVoiceManager>();

            PublishHealthEvent();
        }

        public void ModifyMaxHealth(float amount)
        {
            _maxHealth += amount;
            // Optionally heal the added amount or keep percentage?
            // Usually adding max health heals the difference.
            if (amount > 0)
            {
                CurrentHealth += amount;
            }
            // Clamp
            if (CurrentHealth > _maxHealth) CurrentHealth = _maxHealth;

            OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
            PublishHealthEvent();
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            CurrentHealth -= amount;
            
            // Invoke damage event (useful for UI or flashing effects)
            OnTakeDamage?.Invoke(amount);
            OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);

            if (IsPlayer)
            {
                PlayerVoiceManager?.PlayDamageSound(amount);
            }

            PublishHealthEvent();

            if (CurrentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            CurrentHealth = 0f;

            Debug.Log($"{gameObject.name} has died.");
            
            OnDeath?.Invoke();

            if (IsPlayer)
            {
                OnPlayerDeathEvent?.Invoke();
            }

            if (_destroyOnDeath)
            {
                if (ObjectPool.Instance != null)
                {
                    ObjectPool.Instance.Return(gameObject);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        /// <summary>
        /// Resets health and death state. Call this when retrieving from pool.
        /// </summary>
        public void Revive()
        {
            IsDead = false;
            CurrentHealth = _maxHealth;
            OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
            PublishHealthEvent();
        }

        private void PublishHealthEvent()
        {
            if (!IsPlayer) return;

            EventBus.Publish(new HealthChangedEvent
            {
                CurrentHealth = CurrentHealth,
                MaxHealth = _maxHealth,
                Ratio = _maxHealth > 0f ? CurrentHealth / _maxHealth : 0f,
                IsPlayer = true
            });
        }
    }
}
