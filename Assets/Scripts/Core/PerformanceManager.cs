using UnityEngine;
using System.Collections.Generic;

namespace DungeonDredge.Core
{
    public class PerformanceManager : MonoBehaviour
    {
        public static PerformanceManager Instance { get; private set; }

        [Header("Quality Settings")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool enableVSync = true;

        [Header("LOD Settings")]
        [SerializeField] private float lodBias = 1f;
        [SerializeField] private int maximumLODLevel = 0;

        [Header("Culling")]
        [SerializeField] private float cullingDistance = 100f;
        [SerializeField] private bool useOcclusionCulling = true;

        [Header("Debug")]
        [SerializeField] private bool showFPS = false;
        [SerializeField] private bool showMemory = false;

        // FPS tracking
        private float deltaTime = 0f;
        private float fps = 0f;
        private List<float> fpsHistory = new List<float>();
        private const int fpsHistorySize = 60;

        // Memory tracking
        private long lastMemoryUsage = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ApplySettings();
        }

        private void Update()
        {
            // FPS calculation
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            fps = 1.0f / deltaTime;

            // Track FPS history
            fpsHistory.Add(fps);
            if (fpsHistory.Count > fpsHistorySize)
            {
                fpsHistory.RemoveAt(0);
            }
        }

        private void ApplySettings()
        {
            // Frame rate
            Application.targetFrameRate = targetFrameRate;
            QualitySettings.vSyncCount = enableVSync ? 1 : 0;

            // LOD
            QualitySettings.lodBias = lodBias;
            QualitySettings.maximumLODLevel = maximumLODLevel;
        }

        #region Quality Presets

        public void SetQualityLevel(int level)
        {
            QualitySettings.SetQualityLevel(level, true);
            ApplySettings();
        }

        public void SetLowQuality()
        {
            QualitySettings.SetQualityLevel(0, true);
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.softParticles = false;
            QualitySettings.antiAliasing = 0;
            lodBias = 0.5f;
            ApplySettings();
        }

        public void SetMediumQuality()
        {
            QualitySettings.SetQualityLevel(2, true);
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.softParticles = false;
            QualitySettings.antiAliasing = 2;
            lodBias = 1f;
            ApplySettings();
        }

        public void SetHighQuality()
        {
            QualitySettings.SetQualityLevel(4, true);
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.softParticles = true;
            QualitySettings.antiAliasing = 4;
            lodBias = 1.5f;
            ApplySettings();
        }

        #endregion

        #region Stats

        public float GetCurrentFPS()
        {
            return fps;
        }

        public float GetAverageFPS()
        {
            if (fpsHistory.Count == 0) return 0;

            float sum = 0f;
            foreach (float f in fpsHistory)
            {
                sum += f;
            }
            return sum / fpsHistory.Count;
        }

        public float GetMinFPS()
        {
            if (fpsHistory.Count == 0) return 0;

            float min = float.MaxValue;
            foreach (float f in fpsHistory)
            {
                if (f < min) min = f;
            }
            return min;
        }

        public long GetMemoryUsage()
        {
            return System.GC.GetTotalMemory(false);
        }

        public string GetMemoryString()
        {
            long bytes = GetMemoryUsage();
            return $"{bytes / 1048576f:F1} MB";
        }

        #endregion

        #region Memory Management

        public void CollectGarbage()
        {
            System.GC.Collect();
        }

        public void UnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
        }

        public void OptimizeMemory()
        {
            CollectGarbage();
            UnloadUnusedAssets();
        }

        #endregion

        #region Debug GUI

        private void OnGUI()
        {
            if (!showFPS && !showMemory) return;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 20;

            int y = 10;

            if (showFPS)
            {
                string fpsText = $"FPS: {fps:F1} (Avg: {GetAverageFPS():F1}, Min: {GetMinFPS():F1})";
                GUI.Label(new Rect(10, y, 400, 30), fpsText, style);
                y += 25;
            }

            if (showMemory)
            {
                string memText = $"Memory: {GetMemoryString()}";
                GUI.Label(new Rect(10, y, 400, 30), memText, style);
            }
        }

        #endregion

        #region Settings

        public void SetTargetFrameRate(int fps)
        {
            targetFrameRate = fps;
            Application.targetFrameRate = fps;
            PlayerPrefs.SetInt("TargetFPS", fps);
        }

        public void SetVSync(bool enabled)
        {
            enableVSync = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;
            PlayerPrefs.SetInt("VSync", enabled ? 1 : 0);
        }

        public void ToggleDebugFPS(bool show)
        {
            showFPS = show;
        }

        public void ToggleDebugMemory(bool show)
        {
            showMemory = show;
        }

        #endregion
    }
}
