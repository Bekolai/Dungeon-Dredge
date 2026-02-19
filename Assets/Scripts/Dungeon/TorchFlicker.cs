using UnityEngine;

namespace DungeonDredge.Dungeon
{
    /// <summary>
    /// Adds a realistic flickering effect to a light source (torch, brazier, candle, etc.).
    /// Attach to any GameObject with a Light component, or it will find one in children.
    /// Also handles an optional particle system for fire/smoke visuals.
    /// </summary>
    public class TorchFlicker : MonoBehaviour
    {
        [Header("Light Flicker")]
        [SerializeField] private Light targetLight;
        [SerializeField] private float baseIntensity = 1.2f;
        [SerializeField] private float flickerIntensity = 0.3f;
        [SerializeField] private float flickerSpeed = 8f;
        [Tooltip("Secondary noise layer for more organic feel")]
        [SerializeField] private float secondaryFlickerSpeed = 23f;
        [SerializeField] private float secondaryFlickerIntensity = 0.1f;

        [Header("Range Flicker")]
        [SerializeField] private float baseRange = 10f;
        [SerializeField] private float rangeFlicker = 0.5f;

        [Header("Color Variation")]
        [SerializeField] private bool enableColorShift = true;
        [SerializeField] private Color warmColor = new Color(1f, 0.7f, 0.4f);
        [SerializeField] private Color coolColor = new Color(1f, 0.55f, 0.3f);
        [SerializeField] private float colorShiftSpeed = 3f;

        [Header("Wind Gusts")]
        [Tooltip("Occasional stronger flickers simulating wind")]
        [SerializeField] private bool enableWindGusts = true;
        [Range(0f, 1f)]
        [SerializeField] private float gustChance = 0.02f;
        [SerializeField] private float gustIntensityDrop = 0.5f;
        [SerializeField] private float gustDuration = 0.3f;

        // Internal state
        private float noiseOffset;
        private float gustTimer;
        private bool isGusting;

        private void Awake()
        {
            if (targetLight == null)
                targetLight = GetComponent<Light>();
            if (targetLight == null)
                targetLight = GetComponentInChildren<Light>();

            // Each torch gets a unique noise offset so they don't flicker in sync
            noiseOffset = Random.Range(0f, 1000f);
        }

        private void Start()
        {
            if (targetLight != null)
            {
                baseIntensity = targetLight.intensity;
                baseRange = targetLight.range;
            }
        }

        private void Update()
        {
            if (targetLight == null || !targetLight.enabled) return;

            float time = Time.time + noiseOffset;

            // Primary flicker using Perlin noise for smooth organic movement
            float primaryNoise = Mathf.PerlinNoise(time * flickerSpeed, 0f) * 2f - 1f;
            float secondaryNoise = Mathf.PerlinNoise(0f, time * secondaryFlickerSpeed) * 2f - 1f;

            float intensityOffset = primaryNoise * flickerIntensity + secondaryNoise * secondaryFlickerIntensity;

            // Wind gust effect
            if (enableWindGusts)
            {
                if (isGusting)
                {
                    gustTimer -= Time.deltaTime;
                    float gustProgress = 1f - (gustTimer / gustDuration);
                    // Sharp dip then recovery
                    float gustCurve = Mathf.Sin(gustProgress * Mathf.PI);
                    intensityOffset -= gustCurve * gustIntensityDrop;

                    if (gustTimer <= 0f)
                        isGusting = false;
                }
                else if (Random.value < gustChance * Time.deltaTime * 60f)
                {
                    isGusting = true;
                    gustTimer = gustDuration;
                }
            }

            // Apply intensity
            targetLight.intensity = Mathf.Max(0.1f, baseIntensity + intensityOffset);

            // Range flicker
            float rangeNoise = Mathf.PerlinNoise(time * flickerSpeed * 0.5f, 100f) * 2f - 1f;
            targetLight.range = baseRange + rangeNoise * rangeFlicker;

            // Color shift
            if (enableColorShift)
            {
                float colorT = (Mathf.PerlinNoise(time * colorShiftSpeed, 200f) + 1f) * 0.5f;
                targetLight.color = Color.Lerp(warmColor, coolColor, colorT);
            }
        }

        /// <summary>
        /// Configure this torch flicker with theme-appropriate settings
        /// </summary>
        public void Configure(Color torchColor, float intensity, float range)
        {
            baseIntensity = intensity;
            baseRange = range;
            warmColor = torchColor;
            // Cool color is a slightly darker/redder version
            coolColor = new Color(
                torchColor.r * 0.85f,
                torchColor.g * 0.7f,
                torchColor.b * 0.6f
            );

            if (targetLight != null)
            {
                targetLight.color = torchColor;
                targetLight.intensity = intensity;
                targetLight.range = range;
            }
        }
    }
}
