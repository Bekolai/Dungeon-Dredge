using UnityEngine;
using DungeonDredge.Audio;

namespace DungeonDredge.Dungeon
{
    /// <summary>
    /// Defines the visual theme for a dungeon, including decoration pools
    /// that get randomly selected during room generation.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomTheme", menuName = "DungeonDredge/Room Theme")]
    public class RoomTheme : ScriptableObject
    {
        [Header("Theme Info")]
        public string themeName = "Stone Dungeon";
        public SurfaceType defaultSurface = SurfaceType.Stone;

        [Header("Lighting")]
        [Tooltip("Ambient color hint - will be hard-clamped to near-black by DungeonAtmosphere")]
        public Color ambientColor = new Color(0.02f, 0.015f, 0.025f);
        [Range(0f, 2f)]
        [Tooltip("Ambient intensity hint - will be hard-clamped to stay dark")]
        public float ambientIntensity = 0.02f;
        public Color torchColor = new Color(1f, 0.7f, 0.4f);
        [Range(0.1f, 3f)]
        [Tooltip("Intensity of spawned torch/lantern lights - keep low for tight light pools")]
        public float torchIntensity = 0.8f;
        [Range(2f, 20f)]
        [Tooltip("Range of spawned torch lights - smaller = tighter pools of light")]
        public float torchRange = 5f;

        [Header("Fog")]
        [Tooltip("Enable distance fog for this theme")]
        public bool enableFog = true;
        [Range(0f, 0.15f)]
        [Tooltip("Fog density - higher means less visibility at distance")]
        public float fogDensity = 0.045f;

        [Header("Light Source Decorations")]
        [Tooltip("Torches, braziers, candles, etc.")]
        public DecorationEntry[] lightSources;
        [Range(0, 6)]
        [Tooltip("Fewer torches = more dark areas to navigate")]
        public int minLightSources = 1;
        [Range(0, 6)]
        public int maxLightSources = 2;

        [Header("Large Props")]
        [Tooltip("Tables, crates, barrels, statues, etc.")]
        public DecorationEntry[] largeProps;
        [Range(0, 10)]
        public int minLargeProps = 1;
        [Range(0, 10)]
        public int maxLargeProps = 4;

        [Header("Small Props")]
        [Tooltip("Bottles, books, bones, debris, etc.")]
        public DecorationEntry[] smallProps;
        [Range(0, 20)]
        public int minSmallProps = 3;
        [Range(0, 20)]
        public int maxSmallProps = 10;

        [Header("Wall Decorations")]
        [Tooltip("Chains, banners, shelves, wall torches, etc.")]
        public DecorationEntry[] wallDecorations;
        [Range(0, 8)]
        public int minWallDecorations = 0;
        [Range(0, 8)]
        public int maxWallDecorations = 4;

        [Header("Corner/Pillar Props")]
        [Tooltip("Pillars, corner debris, cobwebs, etc.")]
        public DecorationEntry[] cornerProps;
        [Range(0, 4)]
        public int minCornerProps = 0;
        [Range(0, 4)]
        public int maxCornerProps = 2;

        [Header("Floor Clutter")]
        [Tooltip("Rocks, debris, puddles, blood stains, etc.")]
        public DecorationEntry[] floorClutter;
        [Range(0, 15)]
        public int minFloorClutter = 2;
        [Range(0, 15)]
        public int maxFloorClutter = 8;

        [Header("Special Decorations")]
        [Tooltip("Rare special props (cages, torture devices, altars, etc.)")]
        public DecorationEntry[] specialProps;
        [Range(0f, 1f)]
        public float specialPropChance = 0.15f;

        /// <summary>
        /// Get a random decoration from a pool based on spawn weights
        /// </summary>
        public GameObject GetRandomFromPool(DecorationEntry[] pool)
        {
            if (pool == null || pool.Length == 0) return null;

            float totalWeight = 0f;
            foreach (var entry in pool)
            {
                totalWeight += entry.spawnWeight;
            }

            float random = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var entry in pool)
            {
                cumulative += entry.spawnWeight;
                if (random <= cumulative && entry.prefab != null)
                {
                    return entry.prefab;
                }
            }

            // Fallback to first valid entry
            foreach (var entry in pool)
            {
                if (entry.prefab != null) return entry.prefab;
            }

            return null;
        }

        /// <summary>
        /// Get multiple random decorations from a pool (no duplicates unless necessary)
        /// </summary>
        public GameObject[] GetMultipleFromPool(DecorationEntry[] pool, int count)
        {
            if (pool == null || pool.Length == 0) return new GameObject[0];

            var result = new System.Collections.Generic.List<GameObject>();
            var availableIndices = new System.Collections.Generic.List<int>();
            
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i].prefab != null)
                    availableIndices.Add(i);
            }

            for (int i = 0; i < count; i++)
            {
                if (availableIndices.Count == 0)
                {
                    // Reset pool if we need more than available
                    for (int j = 0; j < pool.Length; j++)
                    {
                        if (pool[j].prefab != null)
                            availableIndices.Add(j);
                    }
                }

                if (availableIndices.Count > 0)
                {
                    // Weighted random selection
                    float totalWeight = 0f;
                    foreach (int idx in availableIndices)
                    {
                        totalWeight += pool[idx].spawnWeight;
                    }

                    float random = Random.Range(0f, totalWeight);
                    float cumulative = 0f;
                    int selectedIndex = availableIndices[0];

                    foreach (int idx in availableIndices)
                    {
                        cumulative += pool[idx].spawnWeight;
                        if (random <= cumulative)
                        {
                            selectedIndex = idx;
                            break;
                        }
                    }

                    result.Add(pool[selectedIndex].prefab);
                    availableIndices.Remove(selectedIndex);
                }
            }

            return result.ToArray();
        }
    }

    [System.Serializable]
    public class DecorationEntry
    {
        public GameObject prefab;
        [Range(0.1f, 10f)]
        public float spawnWeight = 1f;
        [Tooltip("Random rotation around Y axis")]
        public bool randomYRotation = true;
        [Tooltip("Random scale variation (1 = no variation)")]
        [Range(0.8f, 1.2f)]
        public float scaleVariation = 1f;
        [Tooltip("Offset from spawn point")]
        public Vector3 positionOffset;
    }
}
