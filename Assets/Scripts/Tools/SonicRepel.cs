using UnityEngine;
using DungeonDredge.AI;

namespace DungeonDredge.Tools
{
    public class SonicRepel : ToolBase
    {
        [Header("Repel Settings")]
        [SerializeField] private float repelRadius = 8f;
        [SerializeField] private float repelForce = 12f;
        [SerializeField] private float stunDuration = 2f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Visual")]
        [SerializeField] private ParticleSystem shockwaveEffect;
        [SerializeField] private float effectDuration = 0.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip sonicBoomSound;

        protected override void UsePrimary()
        {
            // Find all enemies in radius
            Collider[] hits = Physics.OverlapSphere(transform.position, repelRadius, enemyLayer);

            foreach (var hit in hits)
            {
                EnemyAI enemy = hit.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    // Calculate push direction (away from player)
                    Vector3 direction = (hit.transform.position - transform.position).normalized;
                    
                    // Apply stronger force the closer they are
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    float forceMultiplier = 1f - (distance / repelRadius);
                    
                    // Push and stun
                    enemy.ApplyPush(direction * repelForce * forceMultiplier);
                    enemy.Stun(stunDuration * forceMultiplier);
                }

                // Also push rigidbodies
                Rigidbody rb = hit.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 direction = (hit.transform.position - transform.position).normalized;
                    rb.AddExplosionForce(repelForce * 100f, transform.position, repelRadius);
                }
            }

            // Visual effect
            if (shockwaveEffect != null)
            {
                shockwaveEffect.Play();
            }
            PlayEffect();

            // Audio
            if (sonicBoomSound != null)
            {
                PlaySound(sonicBoomSound);
            }
            else
            {
                PlaySound(useSound);
            }

            // Generate significant noise
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
}
