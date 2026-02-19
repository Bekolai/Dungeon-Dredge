using UnityEngine;
using DungeonDredge.Core;
using DungeonDredge.Inventory;
using DungeonDredge.Player;
using DungeonDredge.Tools;

namespace DungeonDredge.Dungeon
{
    public class MineableNode : MonoBehaviour, IInteractable
    {
        [Header("Mining")]
        [SerializeField] private int hitsToMine = 3;
        [SerializeField] private float interactionNoise = 0.45f;
        [Tooltip("If true, the player must have a pickaxe tool equipped to mine.")]
        [SerializeField] private bool requiresPickaxe = true;

        [Header("Drops")]
        [SerializeField] private ItemData dropItem;
        [SerializeField] private int minDrops = 1;
        [SerializeField] private int maxDrops = 3;
        [SerializeField] private Transform dropPoint;

        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject breakEffectPrefab;

        private int currentHits;

        private void Awake()
        {
            currentHits = Mathf.Max(1, hitsToMine);
            if (dropPoint == null)
            {
                dropPoint = transform;
            }

            if (GetComponentInChildren<Collider>() == null)
            {
                var collider = gameObject.AddComponent<SphereCollider>();
                collider.radius = 0.75f;
            }
        }

        public void Configure(ItemData itemData, int requiredHits, int minDropCount, int maxDropCount)
        {
            dropItem = itemData;
            hitsToMine = Mathf.Max(1, requiredHits);
            minDrops = Mathf.Max(1, minDropCount);
            maxDrops = Mathf.Max(minDrops, maxDropCount);
            currentHits = hitsToMine;
        }

        private void OnValidate()
        {
            hitsToMine = Mathf.Max(1, hitsToMine);
            minDrops = Mathf.Max(1, minDrops);
            maxDrops = Mathf.Max(minDrops, maxDrops);
        }

        public void Interact(PlayerController player)
        {
            if (player == null || dropItem == null)
            {
                return;
            }

            // Check pickaxe requirement
            if (requiresPickaxe && !HasPickaxeEquipped(player))
            {
                EventBus.Publish(new InventoryFeedbackEvent
                {
                    Message = "Equip a pickaxe to mine",
                    Duration = 1.5f,
                    IsWarning = true
                });
                return;
            }

            currentHits--;

            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, dropPoint.position, Quaternion.identity);
            }

            EventBus.Publish(new NoiseEvent
            {
                Position = transform.position,
                Intensity = interactionNoise,
                Source = player.gameObject
            });

            if (currentHits <= 0)
            {
                Mine(player);
            }
        }

        public string GetInteractionPrompt()
        {
            if (dropItem == null)
            {
                return "Mine";
            }

            string prompt = $"Mine {dropItem.itemName} ({Mathf.Max(0, currentHits)} hits)";

            if (requiresPickaxe)
            {
                prompt += " [Pickaxe]";
            }

            return prompt;
        }

        private bool HasPickaxeEquipped(PlayerController player)
        {
            var toolManager = player.GetComponent<ToolManager>();
            if (toolManager == null)
                toolManager = player.GetComponentInChildren<ToolManager>();

            if (toolManager == null) return false;

            // Check if the currently equipped tool is a pickaxe
            ToolBase current = toolManager.CurrentTool;
            if (current != null && current.ToolName != null &&
                current.ToolName.IndexOf("pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        private void Mine(PlayerController player)
        {
            if (breakEffectPrefab != null)
            {
                Instantiate(breakEffectPrefab, dropPoint.position, Quaternion.identity);
            }

            int quantity = Random.Range(Mathf.Max(1, minDrops), Mathf.Max(Mathf.Max(1, minDrops), maxDrops) + 1);
            var playerInventory = player.GetComponent<PlayerInventory>();

            for (int i = 0; i < quantity; i++)
            {
                bool addedToInventory = playerInventory != null && playerInventory.TryPickupItem(dropItem);
                if (!addedToInventory && dropItem.worldPrefab != null)
                {
                    Vector3 spread = new Vector3(Random.Range(-0.25f, 0.25f), 0.1f, Random.Range(-0.25f, 0.25f));
                    GameObject worldDrop = Instantiate(dropItem.worldPrefab, dropPoint.position + spread, Quaternion.identity);
                    var worldItem = worldDrop.GetComponent<WorldItem>() ?? worldDrop.GetComponentInChildren<WorldItem>();
                    worldItem?.SetItemData(dropItem);
                }
            }

            Destroy(gameObject);
        }
    }
}

