using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DungeonDredge.Player;

namespace DungeonDredge.Core
{
    public class PostProcessingController : MonoBehaviour
    {
        [Header("Volume")]
        [SerializeField] private Volume postProcessVolume;

        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private StaminaSystem staminaSystem;

        [Header("Vignette Settings")]
        [SerializeField] private float baseThreatVignetteIntensity = 0.4f;
        [SerializeField] private float damageVignetteIntensity = 0.6f;
        [SerializeField] private float vignetteFadeSpeed = 2f;

        [Header("Chromatic Aberration")]
        [SerializeField] private float snailChromaticIntensity = 0.3f;
        [SerializeField] private float damageChromaticIntensity = 0.5f;

        [Header("Motion Blur")]
        [SerializeField] private float sprintMotionBlurIntensity = 0.3f;

        // Volume overrides
        private Vignette vignette;
        private ChromaticAberration chromaticAberration;
        private MotionBlur motionBlur;
        private ColorAdjustments colorAdjustments;

        // State
        private float targetVignetteIntensity = 0f;
        private float targetChromaticIntensity = 0f;
        private float damageFlashTimer = 0f;

        private void Start()
        {
            if (postProcessVolume == null)
            {
                postProcessVolume = FindObjectOfType<Volume>();
            }

            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                postProcessVolume.profile.TryGet(out vignette);
                postProcessVolume.profile.TryGet(out chromaticAberration);
                postProcessVolume.profile.TryGet(out motionBlur);
                postProcessVolume.profile.TryGet(out colorAdjustments);
            }

            // Find player components
            if (playerMovement == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerMovement = player.GetComponent<PlayerMovement>();
                    staminaSystem = player.GetComponent<StaminaSystem>();
                }
            }

            // Subscribe to events
            if (StealthManager.Instance != null)
            {
                StealthManager.Instance.OnThreatDetected += OnThreatDetected;
            }
        }

        private void OnDestroy()
        {
            if (StealthManager.Instance != null)
            {
                StealthManager.Instance.OnThreatDetected -= OnThreatDetected;
            }
        }

        private void Update()
        {
            UpdateVignette();
            UpdateChromaticAberration();
            UpdateMotionBlur();
            UpdateDamageFlash();
        }

        private void OnThreatDetected(float distance, GameObject threat)
        {
            float maxDistance = 30f;
            float threatLevel = 1f - Mathf.Clamp01(distance / maxDistance);
            targetVignetteIntensity = baseThreatVignetteIntensity * threatLevel;
        }

        private void UpdateVignette()
        {
            if (vignette == null) return;

            // Fade threat vignette
            if (StealthManager.Instance?.NearestThreat == null)
            {
                targetVignetteIntensity = Mathf.Lerp(targetVignetteIntensity, 0f, Time.deltaTime * vignetteFadeSpeed);
            }

            // Apply damage flash on top
            float finalIntensity = targetVignetteIntensity;
            if (damageFlashTimer > 0)
            {
                finalIntensity = Mathf.Max(finalIntensity, damageVignetteIntensity * damageFlashTimer);
            }

            vignette.intensity.value = finalIntensity;

            // Color - red for damage, black for threat
            if (damageFlashTimer > 0)
            {
                vignette.color.value = Color.Lerp(Color.black, Color.red, damageFlashTimer);
            }
            else
            {
                vignette.color.value = Color.red;
            }
        }

        private void UpdateChromaticAberration()
        {
            if (chromaticAberration == null) return;

            float targetIntensity = 0f;

            // Snail tier effect
            if (playerMovement != null && playerMovement.CurrentTier == EncumbranceTier.Snail)
            {
                targetIntensity = snailChromaticIntensity;
            }

            // Damage effect
            if (damageFlashTimer > 0)
            {
                targetIntensity = Mathf.Max(targetIntensity, damageChromaticIntensity * damageFlashTimer);
            }

            chromaticAberration.intensity.value = Mathf.Lerp(
                chromaticAberration.intensity.value, 
                targetIntensity, 
                Time.deltaTime * 5f);
        }

        private void UpdateMotionBlur()
        {
            if (motionBlur == null) return;

            float targetIntensity = 0f;

            // Sprint motion blur
            if (playerMovement != null && playerMovement.IsSprinting)
            {
                targetIntensity = sprintMotionBlurIntensity;
            }

            motionBlur.intensity.value = Mathf.Lerp(
                motionBlur.intensity.value, 
                targetIntensity, 
                Time.deltaTime * 3f);
        }

        private void UpdateDamageFlash()
        {
            if (damageFlashTimer > 0)
            {
                damageFlashTimer -= Time.deltaTime * 2f;
                damageFlashTimer = Mathf.Max(0f, damageFlashTimer);
            }
        }

        public void TriggerDamageFlash()
        {
            damageFlashTimer = 1f;
        }

        public void TriggerHealFlash()
        {
            // Could implement green flash for healing
        }

        public void SetSaturation(float saturation)
        {
            if (colorAdjustments != null)
            {
                colorAdjustments.saturation.value = saturation;
            }
        }

        public void FadeToBlack(float duration)
        {
            StartCoroutine(FadeCoroutine(true, duration));
        }

        public void FadeFromBlack(float duration)
        {
            StartCoroutine(FadeCoroutine(false, duration));
        }

        private System.Collections.IEnumerator FadeCoroutine(bool fadeOut, float duration)
        {
            if (colorAdjustments == null) yield break;

            float startExposure = colorAdjustments.postExposure.value;
            float endExposure = fadeOut ? -10f : 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                colorAdjustments.postExposure.value = Mathf.Lerp(startExposure, endExposure, t);
                yield return null;
            }

            colorAdjustments.postExposure.value = endExposure;
        }
    }
}
