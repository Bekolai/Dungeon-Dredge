using UnityEngine;
using DungeonDredge.AI;
using DungeonDredge.Inventory;
using DungeonDredge.Core;

namespace DungeonDredge.Enemies
{
    /// <summary>
    /// Scalable component for handling enemy loot drops upon death.
    /// </summary>
    public class EnemyLoot : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _scatterForce = 1.5f;
        [SerializeField] private float _verticalOffset = 0.2f;

        /// <summary>
        /// Spawns loot based on the provided EnemyData.
        /// </summary>
        public void SpawnLoot(EnemyData data)
        {
            if (data == null || data.itemDrops == null || data.itemDrops.Length == 0)
                return;

            foreach (var drop in data.itemDrops)
            {
                if (drop == null || drop.item == null || drop.item.worldPrefab == null)
                    continue;

                if (Random.value > drop.dropChance)
                    continue;

                int quantity = Random.Range(
                    Mathf.Max(1, drop.minQuantity),
                    Mathf.Max(Mathf.Max(1, drop.minQuantity), drop.maxQuantity) + 1);

                for (int i = 0; i < quantity; i++)
                {
                    SpawnItem(drop.item);
                }
            }
        }

        private void SpawnItem(DungeonDredge.Inventory.ItemData item)
        {
            Vector3 scatter = new Vector3(
                Random.Range(-_scatterForce, _scatterForce),
                0.1f,
                Random.Range(-_scatterForce, _scatterForce)
            ) * 0.2f;

            Vector3 spawnPos = transform.position + Vector3.up * _verticalOffset + scatter;
            
            GameObject loot = Instantiate(item.worldPrefab, spawnPos, Quaternion.identity);
            
            // Allow for scatter movement if the world item has a rigidbody
            Rigidbody rb = loot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(scatter.normalized * _scatterForce, ForceMode.Impulse);
            }

            var worldItem = loot.GetComponent<WorldItem>() ?? loot.GetComponentInChildren<WorldItem>();
            if (worldItem != null)
            {
                worldItem.SetItemData(item);
            }
        }
    }
}
