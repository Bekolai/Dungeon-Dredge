using UnityEngine;
using UnityEngine.InputSystem;
using DungeonDredge.Core;
using DungeonDredge.Inventory;
using DungeonDredge.Village;

namespace DungeonDredge.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(StaminaSystem))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private StaminaSystem staminaSystem;
        [SerializeField] private PlayerStats playerStats;
        
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        
        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 85f;
        [SerializeField] private bool invertY = false;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 3f;
        [Tooltip("Leave at 'Nothing' (0) to use all layers. Set to specific layers to filter.")]
        [SerializeField] private LayerMask interactionMask;

        // Expose so HUD can use the same settings
        public float InteractionRange => interactionRange;
        public LayerMask EffectiveInteractionMask => 
            interactionMask.value == 0 ? (LayerMask)Physics.DefaultRaycastLayers : interactionMask;

        [Header("Shove Mechanics")]
        [SerializeField] private float shoveForce = 10f;
        [SerializeField] private float shoveRange = 3f;
        [SerializeField] private float shoveAngle = 60f;
        [SerializeField] private float shoveStunDuration = 1.5f;
        [SerializeField] private float shoveBaseDamage = 10f;
        [SerializeField] private float shoveCooldown = 1.0f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private Animator armAnimator; // Reference to player arms for animation

        // Input values
        private Vector2 moveInput;
        public Vector2 MoveInput => moveInput;
        private Vector2 lookInput;
        private bool sprintInput;
        private bool crouchInput;
        private float verticalRotation = 0f;
        public float VerticalRotation => verticalRotation; // Expose for animation aiming

        // Input Actions
        private InputActionMap playerActionMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction crouchAction;
        private InputAction interactAction;
        private InputAction jumpAction;
        private InputAction shoveAction;

        // Events
        public event System.Action OnJump;
        public event System.Action OnShove;
        public event System.Action OnInteractTriggered;

        private float lastShoveTime;

        // Components
        private CharacterController characterController;
        private LanternController lanternController;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();
            
            if (staminaSystem == null)
                staminaSystem = GetComponent<StaminaSystem>();

            if (playerStats == null)
                playerStats = GetComponent<PlayerStats>();

            lanternController = GetComponent<LanternController>();
            if (lanternController == null)
            {
                lanternController = gameObject.AddComponent<LanternController>();
            }

            if (cameraTransform == null)
                cameraTransform = Camera.main?.transform;

            SetupInputActions();
        }

        private void SetupInputActions()
        {
            if (inputActions == null) return;

            playerActionMap = inputActions.FindActionMap("Player");
            if (playerActionMap == null) return;

            moveAction = playerActionMap.FindAction("Move");
            lookAction = playerActionMap.FindAction("Look");
            sprintAction = playerActionMap.FindAction("Sprint");
            crouchAction = playerActionMap.FindAction("Crouch");
            interactAction = playerActionMap.FindAction("Interact");
            jumpAction = playerActionMap.FindAction("Jump");
            shoveAction = playerActionMap.FindAction("Shove");
        }

        private void OnEnable()
        {
            if (playerActionMap == null) return;

            playerActionMap.Enable();
            
            // Subscribe to input events
            if (moveAction != null)
            {
                moveAction.performed += OnMove;
                moveAction.canceled += OnMove;
            }
            if (lookAction != null)
            {
                lookAction.performed += OnLook;
                lookAction.canceled += OnLook;
            }
            if (sprintAction != null)
            {
                sprintAction.performed += OnSprint;
                sprintAction.canceled += OnSprint;
            }
            if (crouchAction != null)
            {
                // Use started instead of performed - fires only once on button press
                crouchAction.started += OnCrouch;
            }
            if (interactAction != null)
            {
                interactAction.performed += OnInteract;
            }
            if (jumpAction != null)
            {
                jumpAction.performed += OnJumpInput;
            }
            if (shoveAction != null)
            {
                shoveAction.performed += OnShoveInput;
            }
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.performed -= OnMove;
                moveAction.canceled -= OnMove;
            }
            if (lookAction != null)
            {
                lookAction.performed -= OnLook;
                lookAction.canceled -= OnLook;
            }
            if (sprintAction != null)
            {
                sprintAction.performed -= OnSprint;
                sprintAction.canceled -= OnSprint;
            }
            if (crouchAction != null)
            {
                crouchAction.started -= OnCrouch;
            }
            if (interactAction != null)
            {
                interactAction.performed -= OnInteract;
            }
            if (jumpAction != null)
            {
                jumpAction.performed -= OnJumpInput;
            }
            if (shoveAction != null)
            {
                shoveAction.performed -= OnShoveInput;
            }
            
            playerActionMap?.Disable();
        }

        private void Update()
        {
            if (GameManager.Instance != null && 
                GameManager.Instance.CurrentState == GameState.Paused)
            {
                return;
            }

            HandleMouseLook();
            HandleMovement();
        }

        private void HandleMouseLook()
        {
            if (cameraTransform == null) return;

            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity * (invertY ? 1f : -1f);

            // Horizontal rotation - rotate the player
            transform.Rotate(Vector3.up * mouseX);

            // Vertical rotation - rotate the camera
            verticalRotation += mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        private void HandleMovement()
        {
            // Calculate movement direction
            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            
            // Determine if we can sprint
            bool canSprint = sprintInput && 
                             playerMovement.CanSprint &&
                             moveInput.y > 0 &&
                             (staminaSystem.CanSprint || (playerMovement.IsSprinting && !staminaSystem.IsExhausted));

            // Pass to movement system
            playerMovement.Move(moveDirection, canSprint, crouchInput);
        }

        #region Input Callbacks

        private void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        private void OnLook(InputAction.CallbackContext context)
        {
            lookInput = context.ReadValue<Vector2>();
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            sprintInput = context.ReadValueAsButton();
        }

        private void OnCrouch(InputAction.CallbackContext context)
        {
            // Toggle crouch - using 'started' event ensures this only fires once per press
            crouchInput = !crouchInput;
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            TryInteract();
        }

        private void OnJumpInput(InputAction.CallbackContext context)
        {
            if (playerMovement.IsGrounded && !playerMovement.IsCrouching && playerMovement.CurrentTier != EncumbranceTier.Snail)
            {
                if (staminaSystem == null || !staminaSystem.TryUseJumpStamina())
                {
                    return;
                }

                OnJump?.Invoke();
                playerMovement.TryJump();
            }
        }

        private void OnShoveInput(InputAction.CallbackContext context)
        {
            TryShove();
        }

        #endregion

        private void TryInteract()
        {
            if (cameraTransform == null) return;

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            LayerMask effectiveMask = interactionMask.value == 0 ? Physics.DefaultRaycastLayers : interactionMask;
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, effectiveMask, QueryTriggerInteraction.Collide))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable == null)
                {
                    // Many loot prefabs use child colliders, so resolve interaction from the parent hierarchy too.
                    interactable = hit.collider.GetComponentInParent<IInteractable>();
                }
                if (interactable != null)
                {
                    OnInteractTriggered?.Invoke();
                    interactable.Interact(this);
                }
            }
        }

        private void TryShove()
        {
            if (Time.time - lastShoveTime < shoveCooldown) return;

            // Consume stamina
            if (staminaSystem != null && !staminaSystem.TryUseShoveStamina())
            {
                return;
            }
            
            lastShoveTime = Time.time;
            OnShove?.Invoke();
            
            PerformShove();
        }

        private void PerformShove()
        {
            // Play animation
            if (armAnimator != null)
            {
                armAnimator.SetTrigger("Shove");
            }

            // Play shove sound
            playerMovement.PlayerVoiceManager?.PlayShoveSound();

            // Find enemies in front
            Collider[] hits = Physics.OverlapSphere(transform.position, shoveRange, enemyLayer);
            bool hitSomething = false;

            foreach (var hit in hits)
            {
                // Check angle
                Vector3 directionToEnemy = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, directionToEnemy);

                if (angle <= shoveAngle / 2f)
                {
                    // Apply shove to EnemyAI
                    var enemy = hit.GetComponent<DungeonDredge.AI.EnemyAI>();
                    if (enemy != null)
                    {
                        Vector3 pushDirection = directionToEnemy + Vector3.up * 0.3f;
                        enemy.ApplyPush(pushDirection.normalized * shoveForce);
                        enemy.Stun(shoveStunDuration);

                        // Rank-based damage
                        DungeonRank playerRank = QuestManager.Instance != null ? QuestManager.Instance.CurrentRank : DungeonRank.F;
                        if (enemy.Rank < playerRank)
                        {
                            float damage = shoveBaseDamage;
                            if (playerStats != null)
                            {
                                // Strength increases damage (10% per level above 1)
                                damage *= (1f + (playerStats.Strength.level - 1) * 0.1f);
                            }
                            enemy.TakeDamage(damage);
                            Debug.Log($"Shove dealt {damage} damage to {enemy.EnemyName} (Enemy Rank: {enemy.Rank}, Player Rank: {playerRank})");
                        }
                        else
                        {
                            Debug.Log($"Shove dealt NO damage to {enemy.EnemyName} (Enemy Rank: {enemy.Rank}, Player Rank: {playerRank})");
                        }

                        hitSomething = true;
                    }

                    // Also push rigidbodies
                    Rigidbody rb = hit.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(directionToEnemy * shoveForce * 100f, ForceMode.Impulse);
                        hitSomething = true;
                    }
                }
            }
            
            if (hitSomething)
            {
                Debug.Log("Shove hit!");
                // Optional: Camera shake or hit sound
            }
        }

        /// <summary>
        /// Enable or disable player input (movement, look, interaction).
        /// Used when UI panels (inventory, menus) are open.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            if (playerActionMap == null) return;

            if (enabled)
            {
                playerActionMap.Enable();
            }
            else
            {
                playerActionMap.Disable();
                // Zero out inputs so the player stops moving
                moveInput = Vector2.zero;
                lookInput = Vector2.zero;
                sprintInput = false;
            }
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = sensitivity;
        }

        public void SetInvertY(bool invert)
        {
            invertY = invert;
        }
    }

    public interface IInteractable
    {
        void Interact(PlayerController player);
        string GetInteractionPrompt();
    }
}
