using UnityEngine;
using UnityEngine.InputSystem;
using DungeonDredge.Core;

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
        
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;
        
        [Header("Mouse Look")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 85f;
        [SerializeField] private bool invertY = false;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactionMask;

        [Header("Shove Mechanics")]
        [SerializeField] private float shoveForce = 10f;
        [SerializeField] private float shoveRange = 3f;
        [SerializeField] private float shoveAngle = 60f;
        [SerializeField] private float shoveStunDuration = 1.5f;
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

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            
            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();
            
            if (staminaSystem == null)
                staminaSystem = GetComponent<StaminaSystem>();

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
                             staminaSystem.CanSprint && 
                             playerMovement.CanSprint &&
                             moveInput.y > 0; // Only sprint when moving forward

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
            if (playerMovement.IsGrounded && !playerMovement.IsCrouching) // Basic check before firing event/jump
            {
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
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionMask))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
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
