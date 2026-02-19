using UnityEngine;
using UnityEngine.Rendering;

namespace DungeonDredge.Dungeon
{
    /// <summary>
    /// Manages the overall atmosphere of the dungeon.
    /// Forces true darkness: kills ambient, directional light, skybox, reflections.
    /// Only local light sources (torches, player lantern) should illuminate the scene.
    /// Think Escape from Tarkov underground areas - if you don't have a light, you can't see.
    /// </summary>
    public class DungeonAtmosphere : MonoBehaviour
    {
        [Header("Ambient Lighting")]
        [Tooltip("Near-black ambient for true dungeon darkness. Keep this VERY low.")]
        [SerializeField] private Color ambientColor = new Color(0.02f, 0.015f, 0.025f);
        [Range(0f, 0.1f)]
        [Tooltip("How much ambient light exists. 0 = pitch black, 0.02 = barely visible silhouettes")]
        [SerializeField] private float ambientIntensity = 0.02f;

        [Header("Fog")]
        [SerializeField] private bool enableFog = true;
        [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;
        [Tooltip("Fog color should be near-black to swallow light at distance")]
        [SerializeField] private Color fogColor = new Color(0.005f, 0.005f, 0.008f);
        [Range(0f, 0.15f)]
        [Tooltip("Fog density. 0.04-0.06 gives good Tarkov-style visibility falloff")]
        [SerializeField] private float fogDensity = 0.045f;
        [SerializeField] private float fogStartDistance = 2f;
        [SerializeField] private float fogEndDistance = 25f;

        [Header("Directional Light")]
        [Tooltip("Kill ALL directional lights in the scene. No sun underground.")]
        [SerializeField] private bool killDirectionalLights = true;

        [Header("Environment")]
        [SerializeField] private bool disableSkybox = true;
        [SerializeField] private bool disableReflections = true;
        [Tooltip("Background color when skybox is removed")]
        [SerializeField] private Color backgroundColor = Color.black;

        [Header("Hard Limits")]
        [Tooltip("Maximum allowed ambient intensity - prevents old theme assets from being too bright")]
        [Range(0f, 0.1f)]
        [SerializeField] private float maxAmbientIntensity = 0.04f;

        // Saved original scene settings for restoration
        private bool hasSavedSettings;
        private Color originalAmbientLight;
        private float originalAmbientIntensity;
        private AmbientMode originalAmbientMode;
        private bool originalFogEnabled;
        private FogMode originalFogMode;
        private Color originalFogColor;
        private float originalFogDensity;
        private float originalFogStart;
        private float originalFogEnd;
        private Material originalSkybox;
        private float originalReflectionIntensity;
        private DefaultReflectionMode originalReflectionMode;
        private CameraClearFlags originalClearFlags;
        private Color originalBackgroundColor;

        // Tracked directional lights
        private struct SavedLight
        {
            public Light light;
            public float originalIntensity;
            public Color originalColor;
            public bool originalEnabled;
        }
        private System.Collections.Generic.List<SavedLight> savedDirectionalLights 
            = new System.Collections.Generic.List<SavedLight>();

        /// <summary>
        /// Apply dungeon atmosphere using settings from a RoomTheme.
        /// Theme values are used as hints but hard-clamped to keep things dark.
        /// </summary>
        public void ApplyFromTheme(RoomTheme theme, DungeonSettings settings = null)
        {
            if (theme != null)
            {
                // Use theme's ambient color but keep it very dark
                ambientColor = new Color(
                    Mathf.Min(theme.ambientColor.r * 0.15f, 0.04f),
                    Mathf.Min(theme.ambientColor.g * 0.15f, 0.03f),
                    Mathf.Min(theme.ambientColor.b * 0.15f, 0.05f)
                );

                // Hard clamp intensity - no theme should make the dungeon bright
                ambientIntensity = Mathf.Min(theme.ambientIntensity * 0.04f, maxAmbientIntensity);

                // Fog color derived from ambient but even darker
                fogColor = new Color(
                    ambientColor.r * 0.3f,
                    ambientColor.g * 0.3f,
                    ambientColor.b * 0.3f
                );

                if (theme.fogDensity > 0f)
                    fogDensity = Mathf.Max(theme.fogDensity, 0.03f); // Minimum fog density
                // Note: enableFog defaults to false on old assets where the field didn't exist.
                // Always enable fog in dungeons - it's essential for the dark atmosphere.
                enableFog = true;
            }

            if (settings != null)
            {
                if (settings.atmosphereFogDensity > 0f)
                    fogDensity = settings.atmosphereFogDensity;
                if (settings.atmosphereAmbientIntensity > 0f)
                    ambientIntensity = Mathf.Min(settings.atmosphereAmbientIntensity, maxAmbientIntensity);
            }

            ApplyAtmosphere();
        }

        /// <summary>
        /// Apply the atmosphere settings to the scene. Makes it DARK.
        /// </summary>
        public void ApplyAtmosphere()
        {
            SaveOriginalSettings();

            // === AMBIENT LIGHTING - Near zero ===
            RenderSettings.ambientMode = AmbientMode.Flat;
            // Final ambient is the color scaled by a tiny intensity
            Color finalAmbient = ambientColor * Mathf.Min(ambientIntensity, maxAmbientIntensity);
            RenderSettings.ambientLight = finalAmbient;
            RenderSettings.ambientIntensity = Mathf.Min(ambientIntensity, maxAmbientIntensity);
            // Kill any equator/ground ambient that might leak in
            RenderSettings.ambientSkyColor = finalAmbient;
            RenderSettings.ambientEquatorColor = finalAmbient;
            RenderSettings.ambientGroundColor = finalAmbient;

            // === FOG - Black fog eats visibility at distance ===
            RenderSettings.fog = enableFog;
            if (enableFog)
            {
                RenderSettings.fogMode = fogMode;
                RenderSettings.fogColor = fogColor;

                if (fogMode == FogMode.ExponentialSquared || fogMode == FogMode.Exponential)
                {
                    RenderSettings.fogDensity = fogDensity;
                }
                else
                {
                    RenderSettings.fogStartDistance = fogStartDistance;
                    RenderSettings.fogEndDistance = fogEndDistance;
                }
            }

            // === SKYBOX - Remove completely ===
            if (disableSkybox)
            {
                RenderSettings.skybox = null;
                RenderSettings.subtractiveShadowColor = Color.black;
            }

            // === REFLECTIONS - Kill environment reflections ===
            if (disableReflections)
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.reflectionIntensity = 0f;
            }

            // === CAMERA - Solid black background ===
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = backgroundColor;
            }

            // === DIRECTIONAL LIGHTS - Kill them ALL ===
            if (killDirectionalLights)
            {
                KillAllDirectionalLights();
            }

            Debug.Log($"[DungeonAtmosphere] TRUE DARKNESS applied - " +
                      $"Ambient: ({finalAmbient.r:F3},{finalAmbient.g:F3},{finalAmbient.b:F3}), " +
                      $"Fog: {(enableFog ? fogDensity.ToString("F3") : "OFF")}, " +
                      $"Reflections: {(disableReflections ? "OFF" : "ON")}, " +
                      $"DirLights: {(killDirectionalLights ? "KILLED" : "KEPT")}");
        }

        /// <summary>
        /// Restore original scene settings (call when leaving dungeon)
        /// </summary>
        public void RestoreOriginalSettings()
        {
            if (!hasSavedSettings) return;

            RenderSettings.ambientMode = originalAmbientMode;
            RenderSettings.ambientLight = originalAmbientLight;
            RenderSettings.ambientIntensity = originalAmbientIntensity;

            RenderSettings.fog = originalFogEnabled;
            RenderSettings.fogMode = originalFogMode;
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogDensity = originalFogDensity;
            RenderSettings.fogStartDistance = originalFogStart;
            RenderSettings.fogEndDistance = originalFogEnd;

            RenderSettings.skybox = originalSkybox;

            if (disableReflections)
            {
                RenderSettings.defaultReflectionMode = originalReflectionMode;
                RenderSettings.reflectionIntensity = originalReflectionIntensity;
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.clearFlags = originalClearFlags;
                mainCam.backgroundColor = originalBackgroundColor;
            }

            // Restore all directional lights
            foreach (var saved in savedDirectionalLights)
            {
                if (saved.light != null)
                {
                    saved.light.enabled = saved.originalEnabled;
                    saved.light.intensity = saved.originalIntensity;
                    saved.light.color = saved.originalColor;
                }
            }
            savedDirectionalLights.Clear();

            hasSavedSettings = false;
            Debug.Log("[DungeonAtmosphere] Restored original scene settings.");
        }

        private void SaveOriginalSettings()
        {
            if (hasSavedSettings) return;

            originalAmbientMode = RenderSettings.ambientMode;
            originalAmbientLight = RenderSettings.ambientLight;
            originalAmbientIntensity = RenderSettings.ambientIntensity;

            originalFogEnabled = RenderSettings.fog;
            originalFogMode = RenderSettings.fogMode;
            originalFogColor = RenderSettings.fogColor;
            originalFogDensity = RenderSettings.fogDensity;
            originalFogStart = RenderSettings.fogStartDistance;
            originalFogEnd = RenderSettings.fogEndDistance;

            originalSkybox = RenderSettings.skybox;
            originalReflectionIntensity = RenderSettings.reflectionIntensity;
            originalReflectionMode = RenderSettings.defaultReflectionMode;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                originalClearFlags = mainCam.clearFlags;
                originalBackgroundColor = mainCam.backgroundColor;
            }

            hasSavedSettings = true;
        }

        /// <summary>
        /// Find and disable ALL directional lights in the scene.
        /// No sun, no moon, no global illumination. Only local lights.
        /// </summary>
        private void KillAllDirectionalLights()
        {
            savedDirectionalLights.Clear();
            Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);

            foreach (var light in allLights)
            {
                if (light.type == LightType.Directional)
                {
                    savedDirectionalLights.Add(new SavedLight
                    {
                        light = light,
                        originalIntensity = light.intensity,
                        originalColor = light.color,
                        originalEnabled = light.enabled
                    });

                    // Completely disable - not dim, OFF
                    light.enabled = false;
                }
            }

            if (savedDirectionalLights.Count > 0)
            {
                Debug.Log($"[DungeonAtmosphere] Disabled {savedDirectionalLights.Count} directional light(s).");
            }
        }

        private void OnDestroy()
        {
            RestoreOriginalSettings();
        }
    }
}
