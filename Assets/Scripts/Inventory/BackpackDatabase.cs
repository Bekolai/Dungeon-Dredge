using UnityEngine;
using System.Collections.Generic;

namespace DungeonDredge.Inventory
{
    [CreateAssetMenu(fileName = "BackpackDatabase", menuName = "DungeonDredge/Backpack Database")]
    public class BackpackDatabase : ScriptableObject
    {
        [SerializeField] private List<BackpackData> backpacks = new List<BackpackData>();

        private Dictionary<string, BackpackData> backpackLookup;
        private bool generatedRuntimeDefaults;

        public IReadOnlyList<BackpackData> AllBackpacks => backpacks;

        private void OnEnable()
        {
            EnsureBackpackProgression();
            BuildLookup();
        }

        private void EnsureBackpackProgression()
        {
            backpacks.RemoveAll(b => b == null);
            backpacks.Sort((a, b) => a.upgradeLevel.CompareTo(b.upgradeLevel));

            if (backpacks.Count == 0)
            {
                GenerateRuntimeDefaults();
            }

            // Auto-link upgrade chain by level order when it is missing or inconsistent.
            for (int i = 0; i < backpacks.Count; i++)
            {
                BackpackData current = backpacks[i];
                BackpackData expectedNext = i < backpacks.Count - 1 ? backpacks[i + 1] : null;
                if (current.nextUpgrade != expectedNext)
                {
                    current.nextUpgrade = expectedNext;
                }
            }
        }

        private void GenerateRuntimeDefaults()
        {
            if (generatedRuntimeDefaults)
                return;

            generatedRuntimeDefaults = true;
            backpacks = new List<BackpackData>
            {
                CreateRuntimeBackpack("bp_rank_f", "Rank F Pack", 6, 4, 1, 0),
                CreateRuntimeBackpack("bp_rank_e", "Rank E Pack", 7, 5, 2, 800),
                CreateRuntimeBackpack("bp_rank_d", "Rank D Pack", 8, 6, 3, 1600)
            };
        }

        private static BackpackData CreateRuntimeBackpack(string id, string displayName, int width, int height, int level, int cost)
        {
            BackpackData data = CreateInstance<BackpackData>();
            data.backpackId = id;
            data.backpackName = displayName;
            data.gridWidth = width;
            data.gridHeight = height;
            data.upgradeLevel = level;
            data.upgradeCost = cost;
            data.description = $"Runtime default backpack for {displayName}.";
            return data;
        }

        private void BuildLookup()
        {
            backpackLookup = new Dictionary<string, BackpackData>();
            foreach (var backpack in backpacks)
            {
                if (backpack != null && !string.IsNullOrEmpty(backpack.backpackId))
                {
                    backpackLookup[backpack.backpackId] = backpack;
                }
            }
        }

        public BackpackData GetBackpack(string backpackId)
        {
            if (backpackLookup == null)
                BuildLookup();

            backpackLookup.TryGetValue(backpackId, out BackpackData backpack);
            return backpack;
        }

        public BackpackData GetStartingBackpack()
        {
            EnsureBackpackProgression();
            return backpacks.Count > 0 ? backpacks[0] : null;
        }

        public BackpackData GetBackpackByLevel(int level)
        {
            EnsureBackpackProgression();
            foreach (var backpack in backpacks)
            {
                if (backpack.upgradeLevel == level)
                    return backpack;
            }
            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Find All Backpacks in Project")]
        private void FindAllBackpacks()
        {
            backpacks.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:BackpackData");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                BackpackData backpack = UnityEditor.AssetDatabase.LoadAssetAtPath<BackpackData>(path);
                if (backpack != null)
                {
                    backpacks.Add(backpack);
                }
            }
            // Sort by upgrade level
            backpacks.Sort((a, b) => a.upgradeLevel.CompareTo(b.upgradeLevel));
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"Found {backpacks.Count} backpacks");
        }
#endif
    }
}
