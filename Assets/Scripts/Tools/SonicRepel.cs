using UnityEngine;
using DungeonDredge.AI;
using DungeonDredge.Core;

namespace DungeonDredge.Tools
{
    public class SonicRepel : ToolBase
    {
        [Header("Repel Settings")]
        [SerializeField] private float repelRadius = 8f;
        [SerializeField] private float repelForce = 12f;
        [SerializeField] private float stunDuration = 2f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Throwable Grenade (Secondary)")]
        [SerializeField] private GameObject grenadePrefab;
        [SerializeField] private float throwForce = 14f;
        [SerializeField] private float arcHeight = 3f;

        [Header("Visual")]
        [SerializeField] private ParticleSystem shockwaveEffect;
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private float effectDuration = 0.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip sonicBoomSound;

        private Camera playerCamera;

        protected override void Awake()
        {
            base.Awake();
            playerCamera = Camera.main;
        }

        /// <summary>
        /// Primary (left click): instant area-of-effect repel around the player.
        /// </summary>
        protected override void UsePrimary()
        {
            DetonateAt(transform.position);
        }

        /// <summary>
        /// Secondary (right click): throw a sonic grenade that detonates on impact.
        /// </summary>
        protected override void UseSecondary()
        {
            if (!CanUse()) return;

            if (grenadePrefab == null)
            {
                // Fallback: no prefab assigned, use instant repel
                UsePrimary();
                return;
            }

            Vector3 throwDir = playerCamera != null
                ? playerCamera.transform.forward
                : transform.forward;

            Vector3 spawnPos = transform.position + throwDir * 0.5f + Vector3.up * 0.3f;

            GameObject grenade = Instantiate(grenadePrefab, spawnPos, Quaternion.identity);

            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            if (rb == null) rb = grenade.AddComponent<Rigidbody>();

            rb.linearVelocity = throwDir * throwForce + Vector3.up * arcHeight;

            // Attach grenade behavior
            var behavior = grenade.GetComponent<RepelGrenade>();
            if (behavior == null) behavior = grenade.AddComponent<RepelGrenade>();
            behavior.Initialize(repelRadius, repelForce, stunDuration, enemyLayer, sonicBoomSound, explosionEffectPrefab);

            // Consume charge and cooldown
            ConsumeCharge();
            currentCooldown = cooldownTime;
            OnUsed?.Invoke();

            PlaySound(useSound);
            GenerateNoise(0.5f);
        }

        private void DetonateAt(Vector3 position)
        {
            Collider[] hits = Physics.OverlapSphere(position, repelRadius, enemyLayer);

            foreach (var hit in hits)
            {
                EnemyAI enemy = hit.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    Vector3 direction = (hit.transform.position - position).normalized;
                    float distance = Vector3.Distance(position, hit.transform.position);
                    float forceMultiplier = 1f - (distance / repelRadius);

                    enemy.ApplyPush(direction * repelForce * forceMultiplier);
                    enemy.Stun(stunDuration * forceMultiplier);
                }

                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(repelForce * 100f, position, repelRadius);
                }
            }

            if (shockwaveEffect != null)
                shockwaveEffect.Play();

            PlayEffect();

            if (sonicBoomSound != null)
                PlaySound(sonicBoomSound);
            else
                PlaySound(useSound);

            GenerateNoise(1.5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawSphere(transform.position, repelRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, repelRadius);
        }
    }

    /// <summary>
    /// Thrown sonic grenade that detonates on impact, repelling all nearby enemies.
    /// </summary>
    public class RepelGrenade : MonoBehaviour
    {
        private float repelRadius;
        private float repelForce;
        private float stunDuration;
        private LayerMask enemyLayer;
        private AudioClip detonationSound;
        private GameObject explosionEffectPrefab;
        private bool hasDetonated;

        public void Initialize(float radius, float force, float stun, LayerMask layer, AudioClip sound, GameObject effectPrefab)
        {
            repelRadius = radius;
            repelForce = force;
            stunDuration = stun;
            enemyLayer = layer;
            detonationSound = sound;
            explosionEffectPrefab = effectPrefab;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasDetonated) return;
            hasDetonated = true;
            Detonate();
        }

        private void Detonate()
        {
            // Stop physics
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // Repel all enemies in radius
            Collider[] hits = Physics.OverlapSphere(transform.position, repelRadius, enemyLayer);
            foreach (var hit in hits)
            {
                EnemyAI enemy = hit.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    Vector3 direction = (hit.transform.position - transform.position).normalized;
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    float forceMultiplier = 1f - (distance / repelRadius);

                    enemy.ApplyPush(direction * repelForce * forceMultiplier);
                    enemy.Stun(stunDuration * forceMultiplier);
                }

                Rigidbody hitRb = hit.GetComponent<Rigidbody>();
                if (hitRb != null)
                {
                    hitRb.AddExplosionForce(repelForce * 100f, transform.position, repelRadius);
                }
            }

            // Sound
            if (detonationSound != null)
            {
                AudioSource.PlayClipAtPoint(detonationSound, transform.position);
            }

            // Noise
            EventBus.Publish(new NoiseEvent
            {
                Position = transform.position,
                Intensity = 1.5f,
                Source = gameObject
            });

            // Effect
            if (explosionEffectPrefab != null)
            {
                Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Destroy after brief delay
            Destroy(gameObject, 0.5f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 1, 0.2f);
            Gizmos.DrawWireSphere(transform.position, repelRadius);
        }
    }
}

