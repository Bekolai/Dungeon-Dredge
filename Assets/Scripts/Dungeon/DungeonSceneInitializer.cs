using UnityEngine;
using DungeonDredge.Core;
using DungeonDredge.Inventory;

namespace DungeonDredge.Dungeon
{
    /// <summary>
    /// Helper script to auto-start the dungeon in an empty scene for testing.
    /// Add this to an object in the scene.
    /// </summary>
    public class DungeonSceneInitializer : MonoBehaviour
    {
        [SerializeField] private DungeonRank startRank = DungeonRank.F;
        [SerializeField] private float startDelay = 0.5f;

        private void Start()
        {
            Invoke(nameof(StartDungeon), startDelay);
        }

        private void StartDungeon()
        {
            if (DungeonManager.Instance != null && !DungeonManager.Instance.IsDungeonActive)
            {
                Debug.Log($"[DungeonSceneInitializer] Starting {startRank}-Rank Dungeon...");
                DungeonManager.Instance.StartDungeon(startRank);
            }
            else
            {
                Debug.LogWarning("[DungeonSceneInitializer] DungeonManager not found or already active.");
            }
        }
    }
}
