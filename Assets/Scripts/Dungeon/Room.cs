using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;
using System.Linq;
using DungeonDredge.Inventory;
using DungeonDredge.AI;

namespace DungeonDredge.Dungeon
{
    public class Room : MonoBehaviour
    {
        [Header("Room Info")]
        [SerializeField] private RoomType roomType = RoomType.Empty;
        [SerializeField] private Vector2Int gridPosition;

        [Header("Doors")]
        [SerializeField] private Door northDoor;
        [SerializeField] private Door eastDoor;
        [SerializeField] private Door southDoor;
        [SerializeField] private Door westDoor;

        [Header("Spawn Points")]
        [SerializeField] private Transform[] lootSpawnPoints;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private Transform playerSpawnPoint;

        [Header("NavMesh")]
        [SerializeField] private NavMeshSurface navMeshSurface;

        [Header("Decorations")]
        [SerializeField] private RoomDecorator decorator;

        [Header("Connections")]
        private Dictionary<DoorDirection, Room> connectedRooms = new Dictionary<DoorDirection, Room>();

        // Spawned objects
        private List<GameObject> spawnedEnemies = new List<GameObject>();
        private List<GameObject> spawnedLoot = new List<GameObject>();

        // State
        private bool isCleared = false;
        private bool isVisited = false;
        private bool isDecorated = false;

        // Properties
        public RoomType Type => roomType;
        public Vector2Int GridPosition => gridPosition;
        public bool IsCleared => isCleared;
        public bool IsVisited => isVisited;
        public Transform PlayerSpawnPoint => playerSpawnPoint;

        // Events
        public System.Action<Room> OnRoomEntered;
        public System.Action<Room> OnRoomCleared;

        private void Awake()
        {
            if (decorator == null)
                decorator = GetComponent<RoomDecorator>();

            EnsureSpawnPointReferences();
        }

        public void Initialize(RoomType type, Vector2Int position)
        {
            roomType = type;
            gridPosition = position;
            EnsureSpawnPointReferences();
        }

        /// <summary>
        /// Decorate the room using the specified theme
        /// </summary>
        public void DecorateRoom(RoomTheme theme)
        {
            if (isDecorated || theme == null) return;

            if (decorator == null)
                decorator = GetComponent<RoomDecorator>();
            if (decorator == null)
                decorator = gameObject.AddComponent<RoomDecorator>();
            if (decorator == null) return;

            decorator.EnsureRuntimeReferences();

            decorator.Decorate(theme, roomType);
            isDecorated = true;
        }

        /// <summary>
        /// Enable a door (set to archway/passage)
        /// </summary>
        public void SetDoor(DoorDirection direction, bool active)
        {
            Door door = GetDoor(direction);
            if (door != null)
            {
                if (active)
                {
                    door.Unblock(); // Open passage
                }
                else
                {
                    door.Block(); // Solid wall
                }
            }
        }

        /// <summary>
        /// Block all doors with solid walls (call before enabling only connected ones)
        /// </summary>
        public void DisableAllDoors()
        {
            if (northDoor != null) northDoor.Block();
            if (eastDoor != null) eastDoor.Block();
            if (southDoor != null) southDoor.Block();
            if (westDoor != null) westDoor.Block();
        }

        /// <summary>
        /// Set a specific door mode
        /// </summary>
        public void SetDoorMode(DoorDirection direction, DoorMode mode)
        {
            Door door = GetDoor(direction);
            if (door != null)
            {
                door.SetMode(mode);
            }
        }

        public Door GetDoor(DoorDirection direction)
        {
            return direction switch
            {
                DoorDirection.North => northDoor,
                DoorDirection.East => eastDoor,
                DoorDirection.South => southDoor,
                DoorDirection.West => westDoor,
                _ => null
            };
        }

        public void ConnectTo(Room other, DoorDirection direction)
        {
            connectedRooms[direction] = other;
            DoorDirection opposite = RoomData.GetOppositeDirection(direction);
            other.connectedRooms[opposite] = this;
        }

        public Room GetConnectedRoom(DoorDirection direction)
        {
            connectedRooms.TryGetValue(direction, out Room room);
            return room;
        }

        public bool HasConnection(DoorDirection direction)
        {
            return connectedRooms.ContainsKey(direction);
        }

        /// <summary>
        /// Get all directions this room has connections to
        /// </summary>
        public IEnumerable<DoorDirection> GetConnectionDirections()
        {
            return connectedRooms.Keys;
        }

        /// <summary>
        /// Get the total number of connections this room has
        /// </summary>
        public int ConnectionCount => connectedRooms.Count;

        #region Spawning

        public void SpawnLoot(ItemDatabase itemDatabase, DungeonSettings settings)
        {
            if (lootSpawnPoints == null || lootSpawnPoints.Length == 0) return;

            int lootCount = Random.Range(settings.minLootPerRoom, settings.maxLootPerRoom + 1);

            for (int i = 0; i < lootCount && i < lootSpawnPoints.Length; i++)
            {
                Transform spawnPoint = lootSpawnPoints[i];
                
                // Get random item for this rank
                ItemData itemData = itemDatabase.GetRandomItem(settings.rank);
                if (itemData?.worldPrefab != null)
                {
                    GameObject loot = Instantiate(itemData.worldPrefab, spawnPoint.position, spawnPoint.rotation);
                    
                    // Set item data
                    var worldItem = loot.GetComponent<WorldItem>();
                    if (worldItem != null)
                    {
                        worldItem.SetItemData(itemData);
                    }

                    spawnedLoot.Add(loot);
                }
            }
        }

        public void SpawnEnemies(GameObject[] enemyPrefabs, DungeonSettings settings)
        {
            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0) return;
            if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

            var eligiblePrefabs = GetEligibleEnemyPrefabs(enemyPrefabs, settings);
            if (eligiblePrefabs.Count == 0)
            {
                Debug.LogWarning($"[Room] No eligible enemy prefabs for rank {settings.rank} in room {name}.");
                return;
            }

            // Calculate enemy count based on density
            int maxEnemies = Mathf.Min(settings.maxEnemiesPerRoom, enemySpawnPoints.Length);
            int enemyCount = Mathf.RoundToInt(maxEnemies * settings.enemyDensity);
            enemyCount = Mathf.Max(settings.minEnemiesPerRoom, enemyCount);

            for (int i = 0; i < enemyCount; i++)
            {
                Transform spawnPoint = enemySpawnPoints[i % enemySpawnPoints.Length];
                GameObject prefab = eligiblePrefabs[Random.Range(0, eligiblePrefabs.Count)];
                Vector3 spawnPosition = spawnPoint.position;

                // Keep enemy spawns anchored to walkable navmesh to avoid invalid agent placement.
                if (NavMesh.SamplePosition(spawnPoint.position, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
                {
                    spawnPosition = navHit.position;
                }
                else
                {
                    Debug.LogWarning($"[Room] No NavMesh near enemy spawn point '{spawnPoint.name}' in room '{name}'.");
                    continue;
                }

                GameObject enemy = Instantiate(prefab, spawnPosition, spawnPoint.rotation);
                spawnedEnemies.Add(enemy);

                EnemyAI enemyAI = enemy.GetComponent<EnemyAI>() ?? enemy.GetComponentInChildren<EnemyAI>();
                if (enemyAI != null && enemyAI.EnemyData != null)
                {
                    enemyAI.Initialize(enemyAI.EnemyData, settings.rank);
                }
            }
        }

        private List<GameObject> GetEligibleEnemyPrefabs(GameObject[] enemyPrefabs, DungeonSettings settings)
        {
            var eligible = new List<GameObject>();

            foreach (GameObject enemyPrefab in enemyPrefabs)
            {
                if (enemyPrefab == null) continue;

                EnemyAI enemyAI = enemyPrefab.GetComponent<EnemyAI>() ?? enemyPrefab.GetComponentInChildren<EnemyAI>(true);
                if (enemyAI == null || enemyAI.EnemyData == null)
                {
                    // Allow unconfigured prefabs instead of hard-failing the spawn.
                    eligible.Add(enemyPrefab);
                    continue;
                }

                if (enemyAI.EnemyData.minimumRank <= settings.rank)
                {
                    eligible.Add(enemyPrefab);
                }
            }

            if (eligible.Count > 0)
                return eligible;

            // Fallback: if all prefabs are above rank, pick the lowest minimum rank group.
            DungeonRank? fallbackRank = null;
            foreach (GameObject enemyPrefab in enemyPrefabs)
            {
                if (enemyPrefab == null) continue;

                EnemyAI enemyAI = enemyPrefab.GetComponent<EnemyAI>() ?? enemyPrefab.GetComponentInChildren<EnemyAI>(true);
                if (enemyAI?.EnemyData == null) continue;

                if (fallbackRank == null || enemyAI.EnemyData.minimumRank < fallbackRank.Value)
                    fallbackRank = enemyAI.EnemyData.minimumRank;
            }

            if (fallbackRank == null)
                return eligible;

            foreach (GameObject enemyPrefab in enemyPrefabs)
            {
                if (enemyPrefab == null) continue;
                EnemyAI enemyAI = enemyPrefab.GetComponent<EnemyAI>() ?? enemyPrefab.GetComponentInChildren<EnemyAI>(true);
                if (enemyAI?.EnemyData != null && enemyAI.EnemyData.minimumRank == fallbackRank.Value)
                {
                    eligible.Add(enemyPrefab);
                }
            }

            return eligible;
        }

        private void EnsureSpawnPointReferences()
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);

            if (lootSpawnPoints == null || lootSpawnPoints.Length == 0)
            {
                lootSpawnPoints = allChildren
                    .Where(t => t != transform && t.name.StartsWith("LootSpawn"))
                    .OrderBy(t => t.name)
                    .ToArray();
            }

            if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
            {
                enemySpawnPoints = allChildren
                    .Where(t => t != transform && (t.name.StartsWith("EnemySpawn") || t.name.StartsWith("BossSpawn")))
                    .OrderBy(t => t.name)
                    .ToArray();
            }

            if (playerSpawnPoint == null)
            {
                playerSpawnPoint = allChildren.FirstOrDefault(
                    t => t != transform && (t.name == "PlayerSpawnPoint" || t.name.StartsWith("PlayerSpawn")));
            }
        }

        #endregion

        #region NavMesh

        public void BakeNavMesh()
        {
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
            }
        }

        #endregion

        #region Room State

        public void OnPlayerEnter()
        {
            if (!isVisited)
            {
                isVisited = true;
                OnRoomEntered?.Invoke(this);
            }

            CheckRoomCleared();
        }

        public void OnEnemyKilled(GameObject enemy)
        {
            spawnedEnemies.Remove(enemy);
            CheckRoomCleared();
        }

        private void CheckRoomCleared()
        {
            if (isCleared) return;

            // Room is cleared when all enemies are gone
            spawnedEnemies.RemoveAll(e => e == null);
            
            if (spawnedEnemies.Count == 0)
            {
                isCleared = true;
                OnRoomCleared?.Invoke(this);
            }
        }

        #endregion

        #region Cleanup

        public void ClearRoom()
        {
            foreach (var enemy in spawnedEnemies)
            {
                if (enemy != null)
                    Destroy(enemy);
            }
            spawnedEnemies.Clear();

            foreach (var loot in spawnedLoot)
            {
                if (loot != null)
                    Destroy(loot);
            }
            spawnedLoot.Clear();
        }

        private void OnDestroy()
        {
            ClearRoom();
        }

        #endregion

        #region Trigger Detection

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnPlayerEnter();
            }
        }

        #endregion
    }
}
