using UnityEngine;
using UnityEngine.InputSystem;
using DungeonDredge.Core;

namespace DungeonDredge.Tools
{
    public abstract class ToolBase : MonoBehaviour
    {
        [Header("Tool Info")]
        [SerializeField] protected string toolName;
        [SerializeField] protected string description;
        [SerializeField] protected Sprite icon;

        [Header("Charges")]
        [SerializeField] protected int maxCharges = 3;
        [SerializeField] protected int currentCharges;

        [Header("Cooldown")]
        [SerializeField] protected float cooldownTime = 1f;
        protected float currentCooldown = 0f;

        [Header("Audio")]
        [SerializeField] protected AudioSource audioSource;
        [SerializeField] protected AudioClip useSound;
        [SerializeField] protected AudioClip emptySound;

        [Header("Visual")]
        [SerializeField] protected ParticleSystem useEffect;

        // Properties
        public string ToolName => toolName;
        public string Description => description;
        public Sprite Icon => icon;
        public int MaxCharges => maxCharges;
        public int CurrentCharges => currentCharges;
        public bool IsOnCooldown => currentCooldown > 0f;
        public float CooldownProgress => cooldownTime > 0 ? 1f - (currentCooldown / cooldownTime) : 1f;

        // Events
        public System.Action OnUsed;
        public System.Action OnChargesChanged;
        public System.Action OnEmpty;

        protected virtual void Awake()
        {
            currentCharges = maxCharges;
        }

        protected virtual void Update()
        {
            if (currentCooldown > 0f)
            {
                currentCooldown -= Time.deltaTime;
            }
        }

        public virtual bool CanUse()
        {
            return currentCharges > 0 && !IsOnCooldown;
        }

        public void TryUsePrimary()
        {
            if (CanUse())
            {
                UsePrimary();
                ConsumeCharge();
                currentCooldown = cooldownTime;
                OnUsed?.Invoke();
            }
            else if (currentCharges <= 0)
            {
                PlaySound(emptySound);
                OnEmpty?.Invoke();
            }
        }

        public void TryUseSecondary()
        {
            if (CanUse())
            {
                UseSecondary();
            }
        }

        protected abstract void UsePrimary();
        
        protected virtual void UseSecondary() { }

        protected void ConsumeCharge()
        {
            currentCharges = Mathf.Max(0, currentCharges - 1);
            OnChargesChanged?.Invoke();
        }

        public void AddCharges(int amount)
        {
            currentCharges = Mathf.Min(maxCharges, currentCharges + amount);
            OnChargesChanged?.Invoke();
        }

        public void Refill()
        {
            currentCharges = maxCharges;
            OnChargesChanged?.Invoke();
        }

        protected void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        protected void PlayEffect()
        {
            if (useEffect != null)
            {
                useEffect.Play();
            }
        }

        protected void GenerateNoise(float intensity)
        {
            EventBus.Publish(new NoiseEvent
            {
                Position = transform.position,
                Intensity = intensity,
                Source = gameObject
            });
        }
    }
}
