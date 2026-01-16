using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using DungeonDredge.Inventory;
using DungeonDredge.Core;

namespace DungeonDredge.Dungeon
{
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private DungeonSettings settings;
        [SerializeField] private ItemDatabase itemDatabase;

        [Header("Room Prefabs")]
        [SerializeField] private GameObject defaultRoomPrefab;
        [SerializeField] private GameObject spawnRoomPrefab;
        [SerializeField] private GameObject lootRoomPrefab;
        [SerializeField] private GameObject enemyRoomPrefab;
        [SerializeField] private GameObject extractionRoomPrefab;
        [SerializeField] private GameObject corridorPrefab;

        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject[] enemyPrefabs;

        [Header("Generation")]
        [SerializeField] private Transform dungeonParent;
        [SerializeField] private int seed = -1; // -1 for random seed

        // Generated dungeon
        private Room[,] roomGrid;
        private List<Room> rooms = new List<Room>();
        private Room spawnRoom;
        private Room extractionRoom;

        // Properties
        public Room SpawnRoom => spawnRoom;
        public Room ExtractionRoom => extractionRoom;
        public IReadOnlyList<Room> Rooms => rooms;

        // Events
        public System.Action OnDungeonGenerated;
        public System.Action OnDungeonCleared;

        public void GenerateDungeon(DungeonRank rank)
        {
            // Get settings for rank if not assigned
            if (settings == null || settings.rank != rank)
            {
                settings = DungeonSettings.GetSettingsForRank(rank);
            }

            GenerateDungeon();
        }

        public void GenerateDungeon()
        {
            if (settings == null)
            {
                Debug.LogError("No dungeon settings assigned!");
                return;
            }

            // Set random seed
            if (seed >= 0)
            {
                Random.InitState(seed);
            }
            else
            {
                Random.InitState(System.DateTime.Now.Millisecond);
            }

            // Clear existing dungeon
            ClearDungeon();

            // Create parent if needed
            if (dungeonParent == null)
            {
                GameObject parent = new GameObject("Dungeon");
                dungeonParent = parent.transform;
            }

            // Initialize grid
            roomGrid = new Room[settings.gridSize.x, settings.gridSize.y];

            // Generate room layout
            GenerateLayout();

            // Create corridors
            GenerateCorridors();

            // Spawn content
            PopulateRooms();

            // Bake NavMesh
            BakeNavMesh();

            OnDungeonGenerated?.Invoke();
        }

        #region Layout Generation

        private void GenerateLayout()
        {
            // Start with spawn room near center
            Vector2Int spawnPos = new Vector2Int(settings.gridSize.x / 2, 0);
            spawnRoom = CreateRoom(RoomType.Spawn, spawnPos);

            // Generate rooms using random walk
            List<Vector2Int> availablePositions = new List<Vector2Int>();
            AddAdjacentPositions(spawnPos, availablePositions);

            int roomCount = Random.Range(settings.minRooms, settings.maxRooms + 1);

            while (rooms.Count < roomCount && availablePositions.Count > 0)
            {
                // Pick random available position
                int index = Random.Range(0, availablePositions.Count);
                Vector2Int pos = availablePositions[index];
                availablePositions.RemoveAt(index);

                // Determine room type
                RoomType type = DetermineRoomType();

                // Create room
                Room room = CreateRoom(type, pos);
                
                // Add new adjacent positions
                AddAdjacentPositions(pos, availablePositions);
            }

            // Place extraction room at furthest point from spawn
            PlaceExtractionRoom();
        }

        private RoomType DetermineRoomType()
        {
            float roll = Random.value;

            if (roll < settings.lootRoomChance)
                return RoomType.Loot;
            
            if (roll < settings.lootRoomChance + settings.enemyRoomChance)
                return RoomType.Enemy;
            
            return RoomType.Empty;
        }

        private void AddAdjacentPositions(Vector2Int pos, List<Vector2Int> available)
        {
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),  // North
                new Vector2Int(1, 0),  // East
                new Vector2Int(0, -1), // South
                new Vector2Int(-1, 0)  // West
            };

            foreach (var dir in directions)
            {
                Vector2Int newPos = pos + dir;
                
                // Check bounds
                if (newPos.x < 0 || newPos.x >= settings.gridSize.x ||
                    newPos.y < 0 || newPos.y >= settings.gridSize.y)
                    continue;

                // Check if already has room
                if (roomGrid[newPos.x, newPos.y] != null)
                    continue;

                // Check if already in list
                if (available.Contains(newPos))
                    continue;

                available.Add(newPos);
            }
        }

        private void PlaceExtractionRoom()
        {
            // Find room furthest from spawn
            Room furthestRoom = null;
            float maxDistance = 0f;

            foreach (var room in rooms)
            {
                if (room == spawnRoom) continue;
                if (room.Type == RoomType.Spawn) continue;

                float distance = Vector2Int.Distance(room.GridPosition, spawnRoom.GridPosition);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    furthestRoom = room;
                }
            }

            if (furthestRoom != null)
            {
                // Convert to extraction room
                rooms.Remove(furthestRoom);
                Vector2Int pos = furthestRoom.GridPosition;
                Destroy(furthestRoom.gameObject);
                roomGrid[pos.x, pos.y] = null;

                extractionRoom = CreateRoom(RoomType.Extraction, pos);
            }
            else
            {
                // Create at edge
                Vector2Int pos = new Vector2Int(settings.gridSize.x / 2, settings.gridSize.y - 1);
                extractionRoom = CreateRoom(RoomType.Extraction, pos);
            }
        }

        #endregion

        #region Room Creation

        private Room CreateRoom(RoomType type, Vector2Int position)
        {
            // Get appropriate prefab
            GameObject prefab = GetRoomPrefab(type);
            if (prefab == null)
            {
                Debug.LogError($"No prefab for room type: {type}");
                return null;
            }

            // Calculate world position
            Vector3 worldPos = GridToWorldPosition(position);

            // Instantiate
            GameObject roomObj = Instantiate(prefab, worldPos, Quaternion.identity, dungeonParent);
            roomObj.name = $"Room_{position.x}_{position.y}_{type}";

            // Get or add Room component
            Room room = roomObj.GetComponent<Room>();
            if (room == null)
            {
                room = roomObj.AddComponent<Room>();
            }

            room.Initialize(type, position);

            // Store in grid
            roomGrid[position.x, position.y] = room;
            rooms.Add(room);

            return room;
        }

        private GameObject GetRoomPrefab(RoomType type)
        {
            // Try settings prefabs first
            if (settings != null)
            {
                switch (type)
                {
                    case RoomType.Spawn:
                        if (settings.spawnRoomPrefabs?.Length > 0)
                            return settings.spawnRoomPrefabs[Random.Range(0, settings.spawnRoomPrefabs.Length)];
                        break;
                    case RoomType.Loot:
                        if (settings.lootRoomPrefabs?.Length > 0)
                            return settings.lootRoomPrefabs[Random.Range(0, settings.lootRoomPrefabs.Length)];
                        break;
                    case RoomType.Enemy:
                        if (settings.enemyRoomPrefabs?.Length > 0)
                            return settings.enemyRoomPrefabs[Random.Range(0, settings.enemyRoomPrefabs.Length)];
                        break;
                    case RoomType.Extraction:
                        if (settings.extractionRoomPrefabs?.Length > 0)
                            return settings.extractionRoomPrefabs[Random.Range(0, settings.extractionRoomPrefabs.Length)];
                        break;
                }
            }

            // Fall back to assigned prefabs
            return type switch
            {
                RoomType.Spawn => spawnRoomPrefab ?? defaultRoomPrefab,
                RoomType.Loot => lootRoomPrefab ?? defaultRoomPrefab,
                RoomType.Enemy => enemyRoomPrefab ?? defaultRoomPrefab,
                RoomType.Extraction => extractionRoomPrefab ?? defaultRoomPrefab,
                _ => defaultRoomPrefab
            };
        }

        private Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            return new Vector3(
                gridPos.x * settings.roomSize.x,
                0f,
                gridPos.y * settings.roomSize.y
            );
        }

        #endregion

        #region Corridor Generation

        private void GenerateCorridors()
        {
            // Connect adjacent rooms
            for (int x = 0; x < settings.gridSize.x; x++)
            {
                for (int y = 0; y < settings.gridSize.y; y++)
                {
                    Room room = roomGrid[x, y];
                    if (room == null) continue;

                    // Check east neighbor
                    if (x < settings.gridSize.x - 1)
                    {
                        Room eastRoom = roomGrid[x + 1, y];
                        if (eastRoom != null)
                        {
                            ConnectRooms(room, eastRoom, DoorDirection.East);
                        }
                    }

                    // Check north neighbor
                    if (y < settings.gridSize.y - 1)
                    {
                        Room northRoom = roomGrid[x, y + 1];
                        if (northRoom != null)
                        {
                            ConnectRooms(room, northRoom, DoorDirection.North);
                        }
                    }
                }
            }

            // Ensure all rooms are connected (create spanning tree)
            EnsureConnectivity();
        }

        private void ConnectRooms(Room roomA, Room roomB, DoorDirection direction)
        {
            // Enable doors
            roomA.SetDoor(direction, true);
            roomB.SetDoor(RoomData.GetOppositeDirection(direction), true);

            // Link rooms
            roomA.ConnectTo(roomB, direction);

            // Create corridor if needed
            if (corridorPrefab != null)
            {
                Vector3 midpoint = (roomA.transform.position + roomB.transform.position) / 2f;
                Quaternion rotation = (direction == DoorDirection.East || direction == DoorDirection.West)
                    ? Quaternion.Euler(0, 90, 0)
                    : Quaternion.identity;

                Instantiate(corridorPrefab, midpoint, rotation, dungeonParent);
            }
        }

        private void EnsureConnectivity()
        {
            // Use flood fill to check connectivity
            HashSet<Room> visited = new HashSet<Room>();
            Queue<Room> queue = new Queue<Room>();

            queue.Enqueue(spawnRoom);
            visited.Add(spawnRoom);

            while (queue.Count > 0)
            {
                Room current = queue.Dequeue();
                Vector2Int pos = current.GridPosition;

                // Check all neighbors
                CheckAndConnect(current, pos + new Vector2Int(0, 1), DoorDirection.North, visited, queue);
                CheckAndConnect(current, pos + new Vector2Int(1, 0), DoorDirection.East, visited, queue);
                CheckAndConnect(current, pos + new Vector2Int(0, -1), DoorDirection.South, visited, queue);
                CheckAndConnect(current, pos + new Vector2Int(-1, 0), DoorDirection.West, visited, queue);
            }

            // Connect any unvisited rooms
            foreach (var room in rooms)
            {
                if (!visited.Contains(room))
                {
                    // Find nearest visited room and connect
                    Room nearest = FindNearestRoom(room, visited);
                    if (nearest != null)
                    {
                        // Create path to connect
                        CreatePathBetweenRooms(room, nearest);
                    }
                }
            }
        }

        private void CheckAndConnect(Room current, Vector2Int neighborPos, DoorDirection direction, 
            HashSet<Room> visited, Queue<Room> queue)
        {
            if (neighborPos.x < 0 || neighborPos.x >= settings.gridSize.x ||
                neighborPos.y < 0 || neighborPos.y >= settings.gridSize.y)
                return;

            Room neighbor = roomGrid[neighborPos.x, neighborPos.y];
            if (neighbor == null || visited.Contains(neighbor))
                return;

            // Connect if not already
            if (!current.HasConnection(direction))
            {
                ConnectRooms(current, neighbor, direction);
            }

            visited.Add(neighbor);
            queue.Enqueue(neighbor);
        }

        private Room FindNearestRoom(Room room, HashSet<Room> candidates)
        {
            Room nearest = null;
            float minDistance = float.MaxValue;

            foreach (var candidate in candidates)
            {
                float dist = Vector2Int.Distance(room.GridPosition, candidate.GridPosition);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = candidate;
                }
            }

            return nearest;
        }

        private void CreatePathBetweenRooms(Room from, Room to)
        {
            // Simple direct path - in a full implementation, use A* pathfinding
            Vector2Int current = from.GridPosition;
            Vector2Int target = to.GridPosition;

            while (current != target)
            {
                Vector2Int next = current;
                DoorDirection dir;

                // Move towards target
                if (current.x < target.x)
                {
                    next.x++;
                    dir = DoorDirection.East;
                }
                else if (current.x > target.x)
                {
                    next.x--;
                    dir = DoorDirection.West;
                }
                else if (current.y < target.y)
                {
                    next.y++;
                    dir = DoorDirection.North;
                }
                else
                {
                    next.y--;
                    dir = DoorDirection.South;
                }

                // Create room if doesn't exist
                if (roomGrid[next.x, next.y] == null)
                {
                    CreateRoom(RoomType.Empty, next);
                }

                // Connect
                Room currentRoom = roomGrid[current.x, current.y];
                Room nextRoom = roomGrid[next.x, next.y];
                
                if (!currentRoom.HasConnection(dir))
                {
                    ConnectRooms(currentRoom, nextRoom, dir);
                }

                current = next;
            }
        }

        #endregion

        #region Population

        private void PopulateRooms()
        {
            foreach (var room in rooms)
            {
                switch (room.Type)
                {
                    case RoomType.Loot:
                        room.SpawnLoot(itemDatabase, settings);
                        break;

                    case RoomType.Enemy:
                        room.SpawnEnemies(enemyPrefabs, settings);
                        room.SpawnLoot(itemDatabase, settings);
                        break;

                    case RoomType.Empty:
                        // Small chance of loot in empty rooms
                        if (Random.value < 0.2f)
                        {
                            room.SpawnLoot(itemDatabase, settings);
                        }
                        break;
                }
            }
        }

        #endregion

        #region NavMesh

        private void BakeNavMesh()
        {
            foreach (var room in rooms)
            {
                room.BakeNavMesh();
            }
        }

        #endregion

        #region Cleanup

        public void ClearDungeon()
        {
            foreach (var room in rooms)
            {
                if (room != null)
                {
                    room.ClearRoom();
                    Destroy(room.gameObject);
                }
            }

            rooms.Clear();
            roomGrid = null;
            spawnRoom = null;
            extractionRoom = null;

            // Clear corridor children
            if (dungeonParent != null)
            {
                foreach (Transform child in dungeonParent)
                {
                    Destroy(child.gameObject);
                }
            }

            // Notify StealthManager to clear enemies
            StealthManager.Instance?.ClearAllEnemies();

            OnDungeonCleared?.Invoke();
        }

        #endregion

        #region Utility

        public Room GetRoomAtPosition(Vector2Int position)
        {
            if (position.x < 0 || position.x >= settings.gridSize.x ||
                position.y < 0 || position.y >= settings.gridSize.y)
                return null;

            return roomGrid[position.x, position.y];
        }

        public Vector3 GetSpawnPosition()
        {
            if (spawnRoom?.PlayerSpawnPoint != null)
            {
                return spawnRoom.PlayerSpawnPoint.position;
            }
            return spawnRoom?.transform.position ?? Vector3.zero;
        }

        #endregion
    }
}
