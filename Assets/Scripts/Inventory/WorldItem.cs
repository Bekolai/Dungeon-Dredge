using UnityEngine;
using DungeonDredge.Player;

namespace DungeonDredge.Inventory
{
    public class WorldItem : MonoBehaviour, IInteractable
    {
        [Header("Item")]
        [SerializeField] private ItemData itemData;

        [Header("Visual")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private GameObject highlightEffect;
        [SerializeField] private float bobSpeed = 1f;
        [SerializeField] private float bobAmount = 0.1f;
        [SerializeField] private float rotateSpeed = 30f;

        [Header("Physics")]
        [SerializeField] private bool usePhysics = true;
        [SerializeField] private Rigidbody rb;

        private Vector3 startPosition;
        private bool isGrounded = false;

        public ItemData ItemData => itemData;

        private void Start()
        {
            startPosition = transform.position;

            // Ensure loot is always raycastable even if a prefab forgot colliders.
            if (GetComponentInChildren<Collider>() == null)
            {
                gameObject.AddComponent<SphereCollider>();
            }

            if (rb == null)
                rb = GetComponent<Rigidbody>();

            // Set rarity color glow
            if (itemData != null && highlightEffect != null)
            {
                var particleSystem = highlightEffect.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    var main = particleSystem.main;
                    main.startColor = ItemData.GetRarityColor(itemData.rarity);
                }
            }
        }

        private void Update()
        {
            // Visual effects when grounded or no physics
            if (!usePhysics || isGrounded)
            {
                // Gentle rotation
                transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

                // Bobbing
                float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Mark as grounded when hitting floor
            if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                isGrounded = true;
                startPosition = transform.position;
                
                if (rb != null)
                {
                    rb.isKinematic = true;
                }
            }
        }

        public void Interact(PlayerController player)
        {
            var inventory = player.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                if (inventory.TryPickupItem(itemData))
                {
                    // Successfully picked up
                    OnPickedUp();
                }
                else
                {
                    // Inventory full
                    Debug.Log("Inventory full!");
                }
            }
        }

        public string GetInteractionPrompt()
        {
            if (itemData == null) return "Pick up";
            return $"Pick up {itemData.itemName} ({itemData.weight}kg)";
        }

        private void OnPickedUp()
        {
            // Play pickup effect
            if (highlightEffect != null)
            {
                highlightEffect.transform.SetParent(null);
                var ps = highlightEffect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop();
                    Destroy(highlightEffect, ps.main.duration);
                }
            }

            Destroy(gameObject);
        }

        public void SetItemData(ItemData data)
        {
            itemData = data;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && highlightEffect != null)
            {
                highlightEffect.SetActive(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && highlightEffect != null)
            {
                highlightEffect.SetActive(false);
            }
        }
    }
}
