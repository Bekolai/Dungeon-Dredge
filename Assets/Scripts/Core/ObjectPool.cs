using UnityEngine;
using System.Collections.Generic;

namespace DungeonDredge.Core
{
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int initialSize = 10;
            public bool expandable = true;
        }

        [SerializeField] private List<Pool> pools;

        private Dictionary<string, Queue<GameObject>> poolDictionary;
        private Dictionary<string, Pool> poolSettings;
        private Dictionary<string, Transform> poolContainers;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePools();
        }

        private void InitializePools()
        {
            poolDictionary = new Dictionary<string, Queue<GameObject>>();
            poolSettings = new Dictionary<string, Pool>();
            poolContainers = new Dictionary<string, Transform>();

            foreach (Pool pool in pools)
            {
                // Create container
                GameObject container = new GameObject($"Pool_{pool.tag}");
                container.transform.SetParent(transform);
                poolContainers[pool.tag] = container.transform;

                // Create pool
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < pool.initialSize; i++)
                {
                    GameObject obj = CreatePooledObject(pool, container.transform);
                    objectPool.Enqueue(obj);
                }

                poolDictionary[pool.tag] = objectPool;
                poolSettings[pool.tag] = pool;
            }
        }

        private GameObject CreatePooledObject(Pool pool, Transform parent)
        {
            GameObject obj = Instantiate(pool.prefab, parent);
            obj.SetActive(false);

            // Add pooled object component for tracking
            var pooledObj = obj.AddComponent<PooledObject>();
            pooledObj.Initialize(pool.tag);

            return obj;
        }

        public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool with tag '{tag}' doesn't exist.");
                return null;
            }

            Queue<GameObject> pool = poolDictionary[tag];
            GameObject objectToSpawn;

            if (pool.Count == 0)
            {
                if (poolSettings[tag].expandable)
                {
                    objectToSpawn = CreatePooledObject(poolSettings[tag], poolContainers[tag]);
                }
                else
                {
                    Debug.LogWarning($"Pool '{tag}' is empty and not expandable.");
                    return null;
                }
            }
            else
            {
                objectToSpawn = pool.Dequeue();
            }

            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.SetActive(true);

            // Call spawn handler if exists
            var poolable = objectToSpawn.GetComponent<IPoolable>();
            poolable?.OnSpawn();

            return objectToSpawn;
        }

        public void Return(GameObject obj)
        {
            var pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                Debug.LogWarning("Object is not pooled. Destroying instead.");
                Destroy(obj);
                return;
            }

            string tag = pooledObj.PoolTag;

            // Call despawn handler
            var poolable = obj.GetComponent<IPoolable>();
            poolable?.OnDespawn();

            obj.SetActive(false);
            obj.transform.SetParent(poolContainers[tag]);
            poolDictionary[tag].Enqueue(obj);
        }

        public void ReturnAfterDelay(GameObject obj, float delay)
        {
            StartCoroutine(ReturnDelayed(obj, delay));
        }

        private System.Collections.IEnumerator ReturnDelayed(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
            {
                Return(obj);
            }
        }

        public void CreatePool(string tag, GameObject prefab, int size, bool expandable = true)
        {
            if (poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"Pool '{tag}' already exists.");
                return;
            }

            Pool pool = new Pool
            {
                tag = tag,
                prefab = prefab,
                initialSize = size,
                expandable = expandable
            };

            pools.Add(pool);

            // Create container
            GameObject container = new GameObject($"Pool_{tag}");
            container.transform.SetParent(transform);
            poolContainers[tag] = container.transform;

            // Create pool
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < size; i++)
            {
                GameObject obj = CreatePooledObject(pool, container.transform);
                objectPool.Enqueue(obj);
            }

            poolDictionary[tag] = objectPool;
            poolSettings[tag] = pool;
        }

        public void WarmPool(string tag, int count)
        {
            if (!poolDictionary.ContainsKey(tag)) return;

            for (int i = 0; i < count; i++)
            {
                GameObject obj = CreatePooledObject(poolSettings[tag], poolContainers[tag]);
                poolDictionary[tag].Enqueue(obj);
            }
        }

        public int GetPoolCount(string tag)
        {
            return poolDictionary.ContainsKey(tag) ? poolDictionary[tag].Count : 0;
        }
    }

    public class PooledObject : MonoBehaviour
    {
        public string PoolTag { get; private set; }

        public void Initialize(string tag)
        {
            PoolTag = tag;
        }

        public void ReturnToPool()
        {
            ObjectPool.Instance?.Return(gameObject);
        }

        public void ReturnToPoolAfter(float delay)
        {
            ObjectPool.Instance?.ReturnAfterDelay(gameObject, delay);
        }
    }

    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }
}
