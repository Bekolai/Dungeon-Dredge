using UnityEngine;
using DungeonDredge.AI;

namespace DungeonDredge.Tools
{
    public class IndustrialShove : ToolBase
    {
        [Header("Shove Settings")]
        [SerializeField] private float shoveForce = 10f;
        [SerializeField] private float shoveRange = 3f;
        [SerializeField] private float shoveAngle = 60f;
        [SerializeField] private float stunDuration = 1.5f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Visual")]
        [SerializeField] private Animator armAnimator;
        [SerializeField] private string shoveAnimationTrigger = "Shove";

        protected override void UsePrimary()
        {
            // Play animation
            if (armAnimator != null)
            {
                armAnimator.SetTrigger(shoveAnimationTrigger);
            }

            // Find enemies in front
            Collider[] hits = Physics.OverlapSphere(transform.position, shoveRange, enemyLayer);
            
            bool hitSomething = false;

            foreach (var hit in hits)
            {
                // Check angle
                Vector3 directionToEnemy = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToEnemy);

                if (angle <= shoveAngle / 2f)
                {
                    // Apply shove
                    EnemyAI enemy = hit.GetComponent<EnemyAI>();
                    if (enemy != null)
                    {
                        Vector3 pushDirection = directionToEnemy + Vector3.up * 0.3f;
                        enemy.ApplyPush(pushDirection.normalized * shoveForce);
                        enemy.Stun(stunDuration);
                        hitSomething = true;
                    }

                    // Also push rigidbodies
                    Rigidbody rb = hit.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(directionToEnemy * shoveForce * 100f, ForceMode.Impulse);
                        hitSomething = true;
                    }
                }
            }

            // Effects
            PlaySound(useSound);
            PlayEffect();

            // Generate noise
            GenerateNoise(0.8f);

            if (hitSomething)
            {
                Debug.Log("Shove hit!");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Shove range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, shoveRange);

            // Shove angle
            Vector3 leftBound = Quaternion.Euler(0, -shoveAngle / 2, 0) * transform.forward * shoveRange;
            Vector3 rightBound = Quaternion.Euler(0, shoveAngle / 2, 0) * transform.forward * shoveRange;
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + leftBound);
            Gizmos.DrawLine(transform.position, transform.position + rightBound);
        }
    }
}
