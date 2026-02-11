using UnityEngine;
using System.Collections.Generic;

namespace DungeonDredge.Dungeon
{
    public enum RoomType
    {
        Empty,      // Basic room with nothing special
        Portal,     // Spawn AND extraction point (entrance/exit)
        Loot,       // Contains treasure
        Enemy,      // Contains enemies
        Boss        // Boss room with high-value loot (replaces extraction)
    }

    public enum DoorDirection
    {
        North,
        East,
        South,
        West
    }

    [CreateAssetMenu(fileName = "NewRoom", menuName = "DungeonDredge/Room Data")]
    public class RoomData : ScriptableObject
    {
        [Header("Basic Info")]
        public string roomId;
        public string roomName;
        public RoomType roomType = RoomType.Empty;

        [Header("Size")]
        public Vector2Int size = Vector2Int.one; // In grid units

        [Header("Doors")]
        public bool hasNorthDoor = true;
        public bool hasEastDoor = true;
        public bool hasSouthDoor = true;
        public bool hasWestDoor = true;

        [Header("Spawn Points")]
        public Transform[] lootSpawnPoints;
        public Transform[] enemySpawnPoints;
        public Transform playerSpawnPoint;

        [Header("Prefab")]
        public GameObject roomPrefab;

        public bool HasDoor(DoorDirection direction)
        {
            return direction switch
            {
                DoorDirection.North => hasNorthDoor,
                DoorDirection.East => hasEastDoor,
                DoorDirection.South => hasSouthDoor,
                DoorDirection.West => hasWestDoor,
                _ => false
            };
        }

        public static DoorDirection GetOppositeDirection(DoorDirection direction)
        {
            return direction switch
            {
                DoorDirection.North => DoorDirection.South,
                DoorDirection.East => DoorDirection.West,
                DoorDirection.South => DoorDirection.North,
                DoorDirection.West => DoorDirection.East,
                _ => direction
            };
        }

        public static Vector2Int GetDirectionOffset(DoorDirection direction)
        {
            return direction switch
            {
                DoorDirection.North => new Vector2Int(0, 1),
                DoorDirection.East => new Vector2Int(1, 0),
                DoorDirection.South => new Vector2Int(0, -1),
                DoorDirection.West => new Vector2Int(-1, 0),
                _ => Vector2Int.zero
            };
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(roomId))
            {
                roomId = name.ToLower().Replace(" ", "_");
            }
        }
    }
}
