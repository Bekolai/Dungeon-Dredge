using UnityEngine;

namespace DungeonDredge.Dungeon
{
    /// <summary>
    /// Types of corridor segments based on how many directions they connect
    /// </summary>
    public enum CorridorType
    {
        None,
        Straight,    // Connects 2 opposite directions (N-S or E-W)
        Corner,      // L-shaped, connects 2 adjacent directions
        TJunction,   // Connects 3 directions
        Crossroads   // Connects all 4 directions
    }

    /// <summary>
    /// Represents a corridor segment with its type and connection directions
    /// </summary>
    public struct CorridorData
    {
        public CorridorType Type;
        public bool North;
        public bool East;
        public bool South;
        public bool West;
        public Vector2Int Position;

        public int ConnectionCount => (North ? 1 : 0) + (East ? 1 : 0) + (South ? 1 : 0) + (West ? 1 : 0);

        public CorridorData(Vector2Int position, bool north, bool east, bool south, bool west)
        {
            Position = position;
            North = north;
            East = east;
            South = south;
            West = west;
            Type = DetermineType(north, east, south, west);
        }

        /// <summary>
        /// Determine corridor type based on which directions are connected
        /// </summary>
        private static CorridorType DetermineType(bool north, bool east, bool south, bool west)
        {
            int count = (north ? 1 : 0) + (east ? 1 : 0) + (south ? 1 : 0) + (west ? 1 : 0);

            if (count == 4) return CorridorType.Crossroads;
            if (count == 3) return CorridorType.TJunction;
            if (count == 2)
            {
                // Opposite directions = Straight, Adjacent directions = Corner
                if ((north && south) || (east && west)) return CorridorType.Straight;
                return CorridorType.Corner;
            }
            // Single connection or none - treat as straight (dead-end)
            return CorridorType.Straight;
        }

        /// <summary>
        /// Get the Y rotation in degrees for the corridor prefab based on its connections
        /// </summary>
        public float GetRotationY()
        {
            switch (Type)
            {
                case CorridorType.Straight:
                    // N-S = 0 deg, E-W = 90 deg
                    return (East || West) ? 90f : 0f;

                case CorridorType.Corner:
                    // Corner rotations based on which two adjacent directions connect
                    if (North && East) return 0f;
                    if (East && South) return 90f;
                    if (South && West) return 180f;
                    if (West && North) return 270f;
                    return 0f;

                case CorridorType.TJunction:
                    // T-junction: rotate based on which direction is NOT connected
                    if (!North) return 0f;   // Opens to E, S, W
                    if (!East) return 90f;   // Opens to N, S, W
                    if (!South) return 180f; // Opens to N, E, W
                    if (!West) return 270f;  // Opens to N, E, S
                    return 0f;

                case CorridorType.Crossroads:
                    // All directions - no rotation needed
                    return 0f;

                default:
                    return 0f;
            }
        }
    }

    /// <summary>
    /// Helper methods for corridor calculations
    /// </summary>
    public static class CorridorHelper
    {
        /// <summary>
        /// Get the position between two adjacent room grid positions where a corridor should be placed
        /// </summary>
        public static Vector3 GetCorridorWorldPosition(Vector2Int roomPosA, Vector2Int roomPosB, Vector2 gridCellSize)
        {
            Vector3 posA = new Vector3(roomPosA.x * gridCellSize.x, 0f, roomPosA.y * gridCellSize.y);
            Vector3 posB = new Vector3(roomPosB.x * gridCellSize.x, 0f, roomPosB.y * gridCellSize.y);
            return (posA + posB) / 2f;
        }

        /// <summary>
        /// Get the direction from room A to room B
        /// </summary>
        public static DoorDirection GetDirection(Vector2Int from, Vector2Int to)
        {
            Vector2Int diff = to - from;
            
            if (diff.y > 0) return DoorDirection.North;
            if (diff.y < 0) return DoorDirection.South;
            if (diff.x > 0) return DoorDirection.East;
            if (diff.x < 0) return DoorDirection.West;
            
            return DoorDirection.North; // Same position - shouldn't happen
        }
    }
}
