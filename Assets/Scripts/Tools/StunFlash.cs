using UnityEngine;
using System.Collections;
using DungeonDredge.AI;

namespace DungeonDredge.Tools
{
    public class StunFlash : ToolBase
    {
        [Header("Flash Settings")]
        [SerializeField] private float flashRadius = 10f;
        [SerializeField] private float stunDuration = 3f;
        [SerializeField] private float flashAngle = 180f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Visual")]
        [SerializeField] private Light flashLight;
        [SerializeField] private float flashIntensity = 10f;
        [SerializeField] private float flashDuration = 0.2f;
        [SerializeField] private ParticleSystem flashParticles;

        [Header("Screen Effect")]
        [SerializeField] private bool affectPlayerScreen = true;
        [SerializeField] private CanvasGroup flashOverlay;

        protected override void UsePrimary()
        {
            // Find enemies in range and angle
            Collider[] hits = Physics.OverlapSphere(transform.position, flashRadius, enemyLayer);

            int stunCount = 0;

            foreach (var hit in hits)
            {
                // Check if enemy is facing the flash (or within angle)
                Vector3 directionToEnemy = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToEnemy);

                if (angle <= flashAngle / 2f)
                {
                    // Check line of sight
                    if (!Physics.Linecast(transform.position, hit.transform.position, ~enemyLayer))
                    {
                        EnemyAI enemy = hit.GetComponent<EnemyAI>();
                        if (enemy != null)
                        {
                            // Calculate stun duration based on distance
                            float distance = Vector3.Distance(transform.position, hit.transform.position);
                            float effectiveStun = stunDuration * (1f - distance / flashRadius * 0.5f);
                            
                            enemy.Stun(effectiveStun);
                            stunCount++;
                        }
                    }
                }
            }

            // Visual effects
            StartCoroutine(FlashEffect());

            // Audio
            PlaySound(useSound);

            // Generate noise
            GenerateNoise(0.6f);

            Debug.Log($"Stunned {stunCount} enemies!");
        }

        private IEnumerator FlashEffect()
        {
            // Light flash
            if (flashLight != null)
            {
                flashLight.enabled = true;
                flashLight.intensity = flashIntensity;
            }

            // Particles
            if (flashParticles != null)
            {
                flashParticles.Play();
            }

            // Screen flash
            if (affectPlayerScreen && flashOverlay != null)
            {
                flashOverlay.alpha = 1f;
            }

            // Fade out
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashDuration;

                if (flashLight != null)
                {
                    flashLight.intensity = Mathf.Lerp(flashIntensity, 0f, t);
                }

                if (affectPlayerScreen && flashOverlay != null)
                {
                    flashOverlay.alpha = Mathf.Lerp(1f, 0f, t);
                }

                yield return null;
            }

            // Cleanup
            if (flashLight != null)
            {
                flashLight.enabled = false;
            }

            if (flashOverlay != null)
            {
                flashOverlay.alpha = 0f;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Flash range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, flashRadius);

            // Flash angle
            Vector3 leftBound = Quaternion.Euler(0, -flashAngle / 2, 0) * transform.forward * flashRadius;
            Vector3 rightBound = Quaternion.Euler(0, flashAngle / 2, 0) * transform.forward * flashRadius;
            
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, transform.position + leftBound);
            Gizmos.DrawLine(transform.position, transform.position + rightBound);
        }
    }
}
