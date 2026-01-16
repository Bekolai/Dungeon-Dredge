using UnityEngine;
using System.Collections.Generic;

namespace DungeonDredge.Inventory
{
    [CreateAssetMenu(fileName = "BackpackDatabase", menuName = "DungeonDredge/Backpack Database")]
    public class BackpackDatabase : ScriptableObject
    {
        [SerializeField] private List<BackpackData> backpacks = new List<BackpackData>();

        private Dictionary<string, BackpackData> backpackLookup;

        public IReadOnlyList<BackpackData> AllBackpacks => backpacks;

        private void OnEnable()
        {
            BuildLookup();
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
            return backpacks.Count > 0 ? backpacks[0] : null;
        }

        public BackpackData GetBackpackByLevel(int level)
        {
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
