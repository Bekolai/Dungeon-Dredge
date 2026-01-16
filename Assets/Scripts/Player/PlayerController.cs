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

        // Input values
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool sprintInput;
        private bool crouchInput;
        private float verticalRotation = 0f;

        // Input Actions
        private InputActionMap playerActionMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction crouchAction;
        private InputAction interactAction;
        private InputAction jumpAction;

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
                crouchAction.performed += OnCrouch;
                crouchAction.canceled += OnCrouch;
            }
            if (interactAction != null)
            {
                interactAction.performed += OnInteract;
            }
            if (jumpAction != null)
            {
                jumpAction.performed += OnJump;
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
                crouchAction.performed -= OnCrouch;
                crouchAction.canceled -= OnCrouch;
            }
            if (interactAction != null)
            {
                interactAction.performed -= OnInteract;
            }
            if (jumpAction != null)
            {
                jumpAction.performed -= OnJump;
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
            crouchInput = context.ReadValueAsButton();
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            TryInteract();
        }

        private void OnJump(InputAction.CallbackContext context)
        {
            playerMovement.TryJump();
        }

        #endregion

        private void TryInteract()
        {
            if (cameraTransform == null) return;

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionMask))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                interactable?.Interact(this);
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
