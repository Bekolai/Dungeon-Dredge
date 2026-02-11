using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using DungeonDredge.Inventory;
using DungeonDredge.Core;
using DungeonDredge.AI;
using Unity.AI.Navigation;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DungeonDredge.Dungeon
{
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private DungeonSettings settings;
        [SerializeField] private ItemDatabase itemDatabase;

        [Header("Room Prefabs")]
        [SerializeField] private GameObject defaultRoomPrefab;
        [SerializeField] private GameObject portalRoomPrefab;  // Spawn AND extraction
        [SerializeField] private GameObject lootRoomPrefab;
        [SerializeField] private GameObject enemyRoomPrefab;
        [SerializeField] private GameObject bossRoomPrefab;    // High-value loot room

        [Header("Corridor Prefabs (Fallback)")]
        [SerializeField] private GameObject straightCorridorPrefab;
        [SerializeField] private GameObject lCorridorPrefab;
        [SerializeField] private GameObject tJunctionPrefab;
        [SerializeField] private GameObject crossroadPrefab;

        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject[] enemyPrefabs;

        [Header("Generation")]
        [SerializeField] private Transform dungeonParent;
        [SerializeField]  private NavMeshSurface navMeshSurface;
        [SerializeField] private int seed = -1; // -1 for random seed

        // Generated dungeon
        private Room[,] roomGrid;
        private List<Room> rooms = new List<Room>();
        private List<GameObject> corridors = new List<GameObject>();
        private Room portalRoom;  // Spawn AND extraction point
        private Room bossRoom;    // Boss room with high-value loot

        // Properties
        public Room PortalRoom => portalRoom;   // Player spawns and extracts here
        public Room BossRoom => bossRoom;       // Boss/treasure room
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
                Debug.LogError("[DungeonGenerator] No dungeon settings assigned!");
                return;
            }

            settings.EnsureRankConfiguration();
            EnsureEnemyPrefabsForRank();

            // Validate that we have at least a default room prefab
            if (!HasAnyRoomPrefab())
            {
                Debug.LogError("[DungeonGenerator] No room prefabs assigned! Cannot generate dungeon. " +
                    "Please assign at least a defaultRoomPrefab in the inspector, or create DungeonSettings assets with room prefabs.");
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

            // Bake NavMesh
            BakeNavMesh();

            // Spawn content (after NavMesh is ready for AI agents)
            PopulateRooms();

            OnDungeonGenerated?.Invoke();
        }

        #region Layout Generation

        private void GenerateLayout()
        {
            // Start with portal room (spawn/extraction point) near center-bottom
            Vector2Int portalPos = new Vector2Int(settings.gridSize.x / 2, 0);
            portalRoom = CreateRoom(RoomType.Portal, portalPos);

            // Track visited positions to prevent infinite loops when room creation fails
            HashSet<Vector2Int> visitedPositions = new HashSet<Vector2Int>();
            visitedPositions.Add(portalPos);

            // Generate rooms using random walk
            List<Vector2Int> availablePositions = new List<Vector2Int>();
            AddAdjacentPositions(portalPos, availablePositions, visitedPositions);

            int roomCount = Random.Range(settings.minRooms, settings.maxRooms + 1);

            // Safety limit to prevent infinite loops
            int maxIterations = settings.gridSize.x * settings.gridSize.y * 2;
            int iterations = 0;

            while (rooms.Count < roomCount && availablePositions.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                // Pick random available position
                int index = Random.Range(0, availablePositions.Count);
                Vector2Int pos = availablePositions[index];
                availablePositions.RemoveAt(index);

                // Mark as visited before attempting creation
                visitedPositions.Add(pos);

                // Determine room type
                RoomType type = DetermineRoomType();

                // Create room
                Room room = CreateRoom(type, pos);
                
                // Add new adjacent positions (only unvisited ones)
                AddAdjacentPositions(pos, availablePositions, visitedPositions);
            }

            if (iterations >= maxIterations)
            {
                Debug.LogWarning($"[DungeonGenerator] Room generation hit max iterations ({maxIterations}). Generated {rooms.Count} rooms.");
            }

            // Place boss room at furthest point from portal
            PlaceBossRoom();
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

        private void AddAdjacentPositions(Vector2Int pos, List<Vector2Int> available, HashSet<Vector2Int> visited = null)
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

                // Check if already visited (prevents infinite loop when room creation fails)
                if (visited != null && visited.Contains(newPos))
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

        private void PlaceBossRoom()
        {
            // Find room furthest from portal - this becomes the boss room
            Room furthestRoom = null;
            float maxDistance = 0f;

            foreach (var room in rooms)
            {
                if (room == portalRoom) continue;
                if (room.Type == RoomType.Portal) continue;

                float distance = Vector2Int.Distance(room.GridPosition, portalRoom.GridPosition);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    furthestRoom = room;
                }
            }

            if (furthestRoom != null)
            {
                // Convert to boss room
                rooms.Remove(furthestRoom);
                Vector2Int pos = furthestRoom.GridPosition;
                Destroy(furthestRoom.gameObject);
                roomGrid[pos.x, pos.y] = null;

                bossRoom = CreateRoom(RoomType.Boss, pos);
            }
            else
            {
                // Create at edge
                Vector2Int pos = new Vector2Int(settings.gridSize.x / 2, settings.gridSize.y - 1);
                bossRoom = CreateRoom(RoomType.Boss, pos);
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
                    case RoomType.Empty:
                        if (settings.emptyRoomPrefabs?.Length > 0)
                            return settings.emptyRoomPrefabs[Random.Range(0, settings.emptyRoomPrefabs.Length)];
                        break;    
                    case RoomType.Portal:
                        if (settings.portalRoomPrefabs?.Length > 0)
                            return settings.portalRoomPrefabs[Random.Range(0, settings.portalRoomPrefabs.Length)];
                        break;
                    case RoomType.Loot:
                        if (settings.lootRoomPrefabs?.Length > 0)
                            return settings.lootRoomPrefabs[Random.Range(0, settings.lootRoomPrefabs.Length)];
                        break;
                    case RoomType.Enemy:
                        if (settings.enemyRoomPrefabs?.Length > 0)
                            return settings.enemyRoomPrefabs[Random.Range(0, settings.enemyRoomPrefabs.Length)];
                        break;
                    case RoomType.Boss:
                        if (settings.bossRoomPrefabs?.Length > 0)
                            return settings.bossRoomPrefabs[Random.Range(0, settings.bossRoomPrefabs.Length)];
                        break;
                }
            }

            // Fall back to assigned prefabs
            return type switch
            {
                RoomType.Portal => portalRoomPrefab ?? defaultRoomPrefab,
                RoomType.Loot => lootRoomPrefab ?? defaultRoomPrefab,
                RoomType.Enemy => enemyRoomPrefab ?? defaultRoomPrefab,
                RoomType.Boss => bossRoomPrefab ?? defaultRoomPrefab,
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
            // Step 1: Connect adjacent rooms (set door states, no corridors yet)
            ConnectAdjacentRooms();

            // Step 2: Ensure all rooms are connected
            EnsureConnectivity();

            // Step 3: Place corridor prefabs based on connection analysis
            PlaceCorridorPrefabs();
        }

        /// <summary>
        /// Connect all adjacent rooms by enabling their doors
        /// </summary>
        private void ConnectAdjacentRooms()
        {
            // FIRST: Disable ALL doors on all rooms (prevent doors to void)
            foreach (var room in rooms)
            {
                room.DisableAllDoors();
            }

            // THEN: Only enable doors where connections actually exist
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
        }

        /// <summary>
        /// Connect two rooms by enabling their doors (no corridor prefab placement)
        /// </summary>
        private void ConnectRooms(Room roomA, Room roomB, DoorDirection direction)
        {
            // Enable doors
            roomA.SetDoor(direction, true);
            roomB.SetDoor(RoomData.GetOppositeDirection(direction), true);

            // Link rooms
            roomA.ConnectTo(roomB, direction);
        }

        /// <summary>
        /// Place ONLY straight corridor prefabs between connected adjacent rooms.
        /// (This is the stable/working approach: midpoint between room transforms.)
        /// </summary>
        private void PlaceCorridorPrefabs()
        {
            float gapSize = settings.roomSize.x - settings.roomActualSize;
            if (gapSize < 1f)
            {
                Debug.LogWarning($"[DungeonGenerator] Gap between rooms is {gapSize} - corridors may overlap rooms. " +
                                 $"Recommended: roomActualSize < roomSize.");
            }

            HashSet<string> placedConnections = new HashSet<string>();

            foreach (var room in rooms)
            {
                Vector2Int roomPos = room.GridPosition;

                // East connection
                if (room.HasConnection(DoorDirection.East))
                {
                    Room other = roomGrid[roomPos.x + 1, roomPos.y];
                    if (other != null)
                    {
                        string key = GetConnectionKey(room.GridPosition, other.GridPosition);
                        if (placedConnections.Add(key))
                        {
                            PlaceStraightCorridorBetweenRooms(room, other, isHorizontal: true);
                        }
                    }
                }

                // North connection
                if (room.HasConnection(DoorDirection.North))
                {
                    Room other = roomGrid[roomPos.x, roomPos.y + 1];
                    if (other != null)
                    {
                        string key = GetConnectionKey(room.GridPosition, other.GridPosition);
                        if (placedConnections.Add(key))
                        {
                            PlaceStraightCorridorBetweenRooms(room, other, isHorizontal: false);
                        }
                    }
                }
            }

            Debug.Log($"[DungeonGenerator] Placed {corridors.Count} straight corridors.");
        }

        private static string GetConnectionKey(Vector2Int a, Vector2Int b)
        {
            // order-independent
            if (a.x < b.x || (a.x == b.x && a.y <= b.y))
                return $"{a.x},{a.y}-{b.x},{b.y}";
            return $"{b.x},{b.y}-{a.x},{a.y}";
        }

        private void PlaceStraightCorridorBetweenRooms(Room roomA, Room roomB, bool isHorizontal)
        {
            GameObject prefab = GetCorridorPrefab(CorridorType.Straight);
            if (prefab == null) return;

            Vector3 corridorPos = (roomA.transform.position + roomB.transform.position) / 2f;
            float rotY = isHorizontal ? 90f : 0f;

            GameObject obj = Instantiate(prefab, corridorPos, Quaternion.Euler(0, rotY, 0), dungeonParent);
            obj.name = $"Corridor_Straight_{roomA.GridPosition}_{roomB.GridPosition}";
            corridors.Add(obj);
        }

        /// <summary>
        /// Get the appropriate corridor prefab for the given type
        /// </summary>
        private GameObject GetCorridorPrefab(CorridorType type)
        {
            // Try settings prefabs first
            if (settings != null)
            {
                switch (type)
                {
                    case CorridorType.Straight:
                        if (settings.straightCorridorPrefabs?.Length > 0)
                            return settings.straightCorridorPrefabs[Random.Range(0, settings.straightCorridorPrefabs.Length)];
                        break;
                    case CorridorType.Corner:
                        if (settings.lCorridorPrefabs?.Length > 0)
                            return settings.lCorridorPrefabs[Random.Range(0, settings.lCorridorPrefabs.Length)];
                        break;
                    case CorridorType.TJunction:
                        if (settings.tJunctionPrefabs?.Length > 0)
                            return settings.tJunctionPrefabs[Random.Range(0, settings.tJunctionPrefabs.Length)];
                        break;
                    case CorridorType.Crossroads:
                        if (settings.crossroadPrefabs?.Length > 0)
                            return settings.crossroadPrefabs[Random.Range(0, settings.crossroadPrefabs.Length)];
                        break;
                }
            }

            // Fall back to generator's own prefabs
            return type switch
            {
                CorridorType.Straight => straightCorridorPrefab,
                CorridorType.Corner => lCorridorPrefab,
                CorridorType.TJunction => tJunctionPrefab,
                CorridorType.Crossroads => crossroadPrefab,
                _ => straightCorridorPrefab
            };
        }

        private void EnsureConnectivity()
        {
            if (portalRoom == null) return;

            // Use flood fill to check connectivity
            HashSet<Room> visited = new HashSet<Room>();
            Queue<Room> queue = new Queue<Room>();

            queue.Enqueue(portalRoom);
            visited.Add(portalRoom);

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
                
                if (currentRoom != null && nextRoom != null && !currentRoom.HasConnection(dir))
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
                // Decorate room with theme
                DecorateRoom(room);

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

                    case RoomType.Boss:
                        // Boss rooms should always feel meaningful.
                        room.SpawnEnemies(enemyPrefabs, settings);
                        room.SpawnLoot(itemDatabase, settings);
                        if (Random.value < 0.75f)
                        {
                            room.SpawnLoot(itemDatabase, settings);
                        }
                        break;
                }
            }
        }

        private void DecorateRoom(Room room)
        {
            if (settings == null) return;

            // Get theme - either main theme or randomly pick from additional themes
            RoomTheme theme = settings.roomTheme;

            if (settings.additionalThemes != null && settings.additionalThemes.Length > 0)
            {
                // 30% chance to use an additional theme for variety
                if (Random.value < 0.3f)
                {
                    theme = settings.additionalThemes[Random.Range(0, settings.additionalThemes.Length)];
                }
            }

            if (theme != null)
            {
                room.DecorateRoom(theme);
            }
        }

        #endregion

        #region NavMesh

        private void BakeNavMesh()
        {
            navMeshSurface.BuildNavMesh();
            foreach (var room in rooms)
            {
                room.BakeNavMesh();
            }
        }

        #endregion

        #region Cleanup

        public void ClearDungeon()
        {
            // Clear rooms
            foreach (var room in rooms)
            {
                if (room != null)
                {
                    room.ClearRoom();
                    Destroy(room.gameObject);
                }
            }
            rooms.Clear();

            // Clear corridors
            foreach (var corridor in corridors)
            {
                if (corridor != null)
                {
                    Destroy(corridor);
                }
            }
            corridors.Clear();

            roomGrid = null;
            portalRoom = null;
            bossRoom = null;

            // Clear any remaining children in dungeon parent
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
            if (portalRoom?.PlayerSpawnPoint != null)
            {
                return portalRoom.PlayerSpawnPoint.position;
            }
            return portalRoom?.transform.position ?? Vector3.zero;
        }

        /// <summary>
        /// Check if we have any room prefab assigned (either on generator or in settings)
        /// </summary>
        private bool HasAnyRoomPrefab()
        {
            // Check generator's own prefabs
            if (defaultRoomPrefab != null) return true;
            if (portalRoomPrefab != null) return true;
            if (lootRoomPrefab != null) return true;
            if (enemyRoomPrefab != null) return true;
            if (bossRoomPrefab != null) return true;

            // Check settings prefabs
            if (settings != null)
            {
                if (settings.emptyRoomPrefabs?.Length > 0) return true;
                if (settings.portalRoomPrefabs?.Length > 0) return true;
                if (settings.lootRoomPrefabs?.Length > 0) return true;
                if (settings.enemyRoomPrefabs?.Length > 0) return true;
                if (settings.bossRoomPrefabs?.Length > 0) return true;
            }

            return false;
        }

        private void EnsureEnemyPrefabsForRank()
        {
            if (enemyPrefabs != null && enemyPrefabs.Length > 0)
                return;

#if UNITY_EDITOR
            var prefabs = new List<GameObject>();
            string[] guids = AssetDatabase.FindAssets("t:EnemyData", new[] { "Assets/ScriptableObjects/Enemies/Data" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (data?.prefab == null) continue;
                if (settings != null && data.minimumRank > settings.rank) continue;
                if (!prefabs.Contains(data.prefab))
                    prefabs.Add(data.prefab);
            }

            // Fallback: if rank-filtered list is empty, use any configured enemy prefab.
            if (prefabs.Count == 0)
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                    if (data?.prefab == null) continue;
                    if (!prefabs.Contains(data.prefab))
                        prefabs.Add(data.prefab);
                }
            }

            if (prefabs.Count > 0)
            {
                enemyPrefabs = prefabs.ToArray();
                Debug.Log($"[DungeonGenerator] Auto-loaded {enemyPrefabs.Length} enemy prefabs for rank {settings?.rank}.");
            }
            else
            {
                Debug.LogWarning("[DungeonGenerator] Enemy prefab list is empty and no EnemyData prefabs were found.");
            }
#else
            Debug.LogWarning("[DungeonGenerator] Enemy prefab list is empty. Assign enemyPrefabs on DungeonGenerator.");
#endif
        }

        #endregion
    }
}
