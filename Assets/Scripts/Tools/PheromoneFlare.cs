using UnityEngine;
using System.Collections;
using DungeonDredge.AI;
using DungeonDredge.Core;

namespace DungeonDredge.Tools
{
    public class PheromoneFlare : ToolBase
    {
        [Header("Throw Settings")]
        [SerializeField] private float throwForce = 15f;
        [SerializeField] private float arcHeight = 2f;
        [SerializeField] private GameObject flarePrefab;

        [Header("Attraction Settings")]
        [SerializeField] private float attractionRadius = 15f;
        [SerializeField] private float attractionDuration = 10f;

        private Camera playerCamera;

        protected override void Awake()
        {
            base.Awake();
            playerCamera = Camera.main;
        }

        protected override void UsePrimary()
        {
            if (flarePrefab == null)
            {
                Debug.LogError("No flare prefab assigned!");
                return;
            }

            // Calculate throw trajectory
            Vector3 throwDirection = playerCamera != null ? 
                playerCamera.transform.forward : transform.forward;
            
            Vector3 spawnPos = transform.position + throwDirection * 0.5f + Vector3.up * 0.5f;

            // Spawn flare
            GameObject flare = Instantiate(flarePrefab, spawnPos, Quaternion.identity);
            
            // Add physics
            Rigidbody rb = flare.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = flare.AddComponent<Rigidbody>();
            }

            // Calculate throw velocity with arc
            Vector3 velocity = throwDirection * throwForce + Vector3.up * arcHeight;
            rb.linearVelocity = velocity;

            // Add flare behavior
            var flareBehavior = flare.GetComponent<FlareEffect>();
            if (flareBehavior == null)
            {
                flareBehavior = flare.AddComponent<FlareEffect>();
            }
            flareBehavior.Initialize(attractionRadius, attractionDuration);

            // Sound and effect
            PlaySound(useSound);
            GenerateNoise(0.5f);
        }
    }

    public class FlareEffect : MonoBehaviour
    {
        private float attractionRadius;
        private float remainingDuration;
        private bool isActive = false;
        private bool hasLanded = false;

        [SerializeField] private ParticleSystem smokeEffect;
        [SerializeField] private Light flareLight;
        [SerializeField] private AudioSource audioSource;

        public void Initialize(float radius, float duration)
        {
            attractionRadius = radius;
            remainingDuration = duration;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!hasLanded)
            {
                hasLanded = true;
                Activate();
            }
        }

        private void Activate()
        {
            isActive = true;

            // Stop physics
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // Start effects
            if (smokeEffect != null)
                smokeEffect.Play();
            
            if (flareLight != null)
                flareLight.enabled = true;

            // Attract nearby enemies
            AttractEnemies();
        }

        private void Update()
        {
            if (!isActive) return;

            remainingDuration -= Time.deltaTime;

            // Periodically re-attract enemies
            if (Time.frameCount % 60 == 0)
            {
                AttractEnemies();
            }

            // Fade light
            if (flareLight != null)
            {
                flareLight.intensity = Mathf.Lerp(0, 2, remainingDuration / 10f);
            }

            if (remainingDuration <= 0)
            {
                Deactivate();
            }
        }

        private void AttractEnemies()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, attractionRadius);

            foreach (var hit in hits)
            {
                EnemyAI enemy = hit.GetComponent<EnemyAI>();
                if (enemy != null)
                {
                    enemy.AttractTo(transform.position);
                }
            }
        }

        private void Deactivate()
        {
            isActive = false;

            if (smokeEffect != null)
                smokeEffect.Stop();
            
            if (flareLight != null)
                flareLight.enabled = false;

            // Destroy after effects finish
            Destroy(gameObject, 2f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, attractionRadius);
        }
    }
}
