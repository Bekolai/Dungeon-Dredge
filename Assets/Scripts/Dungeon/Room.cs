using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;
using DungeonDredge.Inventory;

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

        [Header("Connections")]
        private Dictionary<DoorDirection, Room> connectedRooms = new Dictionary<DoorDirection, Room>();

        // Spawned objects
        private List<GameObject> spawnedEnemies = new List<GameObject>();
        private List<GameObject> spawnedLoot = new List<GameObject>();

        // State
        private bool isCleared = false;
        private bool isVisited = false;

        // Properties
        public RoomType Type => roomType;
        public Vector2Int GridPosition => gridPosition;
        public bool IsCleared => isCleared;
        public bool IsVisited => isVisited;
        public Transform PlayerSpawnPoint => playerSpawnPoint;

        // Events
        public System.Action<Room> OnRoomEntered;
        public System.Action<Room> OnRoomCleared;

        public void Initialize(RoomType type, Vector2Int position)
        {
            roomType = type;
            gridPosition = position;
        }

        public void SetDoor(DoorDirection direction, bool active)
        {
            Door door = GetDoor(direction);
            if (door != null)
            {
                door.gameObject.SetActive(active);
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

            // Calculate enemy count based on density
            int maxEnemies = Mathf.Min(settings.maxEnemiesPerRoom, enemySpawnPoints.Length);
            int enemyCount = Mathf.RoundToInt(maxEnemies * settings.enemyDensity);
            enemyCount = Mathf.Max(settings.minEnemiesPerRoom, enemyCount);

            for (int i = 0; i < enemyCount; i++)
            {
                Transform spawnPoint = enemySpawnPoints[i % enemySpawnPoints.Length];
                GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

                GameObject enemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
                spawnedEnemies.Add(enemy);
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
