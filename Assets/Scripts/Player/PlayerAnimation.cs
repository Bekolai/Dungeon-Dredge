using UnityEngine;
using DungeonDredge.Audio;

namespace DungeonDredge.Player
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerAnimation : MonoBehaviour
    {
        [Header("Animator Parameters")]
        [SerializeField] private string inputXParam = "InputX";
        [SerializeField] private string inputYParam = "InputY";
        [SerializeField] private string isCrouchingParam = "IsCrouching";
        [SerializeField] private string isSprintingParam = "IsSprinting";
        [SerializeField] private string isGroundedParam = "IsGrounded";
        [SerializeField] private string jumpTrigger = "Jump";
        [SerializeField] private string landTrigger = "Land";
        [SerializeField] private string shoveTrigger = "Shove";
        [SerializeField] private string pickupTrigger = "Pickup";

        [Header("Movement Animation Settings")]
        [Tooltip("How quickly the blend tree values interpolate")]
        [SerializeField] private float smoothTime = 0.1f;
        
        [Tooltip("Base walk speed from PlayerMovement - used to calculate speed ratio")]
        [SerializeField] private float baseSpeed = 5f;
        
        [Tooltip("Sprint multiplier from PlayerMovement")]
        [SerializeField] private float sprintMultiplier = 1.5f;

        [Header("Spine Aiming (FPS)")]
        [SerializeField] private bool enableSpineAiming = true;
        [Tooltip("Which bone to rotate for looking up/down. Usually Chest or UpperChest.")]
        [SerializeField] private HumanBodyBones spineBoneToRotate = HumanBodyBones.Chest;
        [Tooltip("Multiplier for how much the spine bends when standing. 1.0 = exact match.")]
        [SerializeField] private float spineBendMultiplier = 0.8f;
        [Tooltip("Multiplier for how much the spine bends when crouching.")]
        [SerializeField] private float crouchSpineBendMultiplier = 0.5f;
        [Tooltip("Offset rotation if the spine is twisted by default")]
        [SerializeField] private Vector3 spineRotationOffset = Vector3.zero;
        
        [Header("Debug")]
        [SerializeField] private bool showDebug = false;

        // Components
        private Animator animator;
        private Transform spineTransform;
        private PlayerController playerController;
        private PlayerMovement playerMovement;
        private FootstepSystem footstepSystem;

        // Spine smoothing
        private float currentSpineMultiplier;
        private float spineMultiplierVelocity;

        // Hash IDs for performance
        private int inputXHash;
        private int inputYHash;
        private int isCrouchingHash;
        private int isSprintingHash;
        private int isGroundedHash;
        private int jumpHash;
        private int landHash;
        private int shoveHash;
        private int pickupHash;

        // Smoothing
        private float currentInputX;
        private float currentInputY;
        private float inputXVelocity;
        private float inputYVelocity;

        // State tracking
        private bool wasGrounded;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            
            // ENSURE Root Motion is OFF so it doesn't fight the CharacterController
            animator.applyRootMotion = false;
            
            // Get spine transform for FPS aiming
            spineTransform = animator.GetBoneTransform(spineBoneToRotate);
            if (spineTransform == null)
                Debug.LogWarning($"[PlayerAnimation] Could not find bone {spineBoneToRotate} for spine aiming!");
            
            playerController = GetComponent<PlayerController>();
            playerMovement = GetComponent<PlayerMovement>();
            footstepSystem = GetComponent<FootstepSystem>();

            // Cache parameter hashes
            inputXHash = Animator.StringToHash(inputXParam);
            inputYHash = Animator.StringToHash(inputYParam);
            isCrouchingHash = Animator.StringToHash(isCrouchingParam);
            isSprintingHash = Animator.StringToHash(isSprintingParam);
            isGroundedHash = Animator.StringToHash(isGroundedParam);
            jumpHash = Animator.StringToHash(jumpTrigger);
            landHash = Animator.StringToHash(landTrigger);
            shoveHash = Animator.StringToHash(shoveTrigger);
            pickupHash = Animator.StringToHash(pickupTrigger);

            currentSpineMultiplier = spineBendMultiplier;
        }

        private void OnEnable()
        {
            if (playerController != null)
            {
                playerController.OnJump += HandleJump;
                playerController.OnShove += HandleShove;
                playerController.OnInteractTriggered += HandlePickup;
            }
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.OnJump -= HandleJump;
                playerController.OnShove -= HandleShove;
                playerController.OnInteractTriggered -= HandlePickup;
            }
        }

        private void Update()
        {
            UpdateMovementParameters();
            UpdateStateParameters();
        }

        private void LateUpdate()
        {
            ApplySpineAiming();
        }

        private void ApplySpineAiming()
        {
            if (!enableSpineAiming || spineTransform == null || playerController == null) return;

            // Determine target multiplier based on crouch state
            float targetMultiplier = playerMovement.IsCrouching ? crouchSpineBendMultiplier : spineBendMultiplier;
            
            // Smoothly interpolate the multiplier to avoid snapping when toggling crouch
            currentSpineMultiplier = Mathf.SmoothDamp(currentSpineMultiplier, targetMultiplier, ref spineMultiplierVelocity, 0.2f);

            // Get the camera pitch (-85 to +85 typically)
            float pitch = playerController.VerticalRotation;

            // Calculate final angle
            float angle = pitch * currentSpineMultiplier;

            // Apply rotation
            // We rotate around the character's Right axis (Vector3.right in local space usually, or transform.right in world)
            // Since this is LateUpdate, we are adding to the animation's pose.
            // Using RotateAround to pivot the bone.
            spineTransform.Rotate(Vector3.right, angle); // Assuming default Unity Humanoid X-axis is pitch
            
            // Apply constant offset if needed
            if (spineRotationOffset != Vector3.zero)
            {
                spineTransform.Rotate(spineRotationOffset);
            }
        }

        private void UpdateMovementParameters()
        {
            if (playerController == null || playerMovement == null || animator == null) return;

            // Get raw input from controller (-1 to 1)
            Vector2 rawInput = playerController.MoveInput;

            // Calculate the speed ratio based on encumbrance
            // CurrentSpeed already factors in encumbrance, crouch, sprint
            float maxPossibleSpeed = baseSpeed * sprintMultiplier;
            float speedRatio = playerMovement.CurrentSpeed / maxPossibleSpeed;

            // Scale input by speed ratio - this makes the blend tree reflect actual movement speed
            // When encumbered: speedRatio is low -> animations blend toward slower/idle
            // When sprinting: speedRatio is high -> animations blend toward faster
            float targetInputX = rawInput.x * speedRatio;
            float targetInputY = rawInput.y * speedRatio;

            // Only apply speed ratio if actually moving
            if (!playerMovement.IsMoving)
            {
                targetInputX = 0f;
                targetInputY = 0f;
            }

            // Smooth the values for natural transitions
            currentInputX = Mathf.SmoothDamp(currentInputX, targetInputX, ref inputXVelocity, smoothTime);
            currentInputY = Mathf.SmoothDamp(currentInputY, targetInputY, ref inputYVelocity, smoothTime);

            // Set animator parameters
            animator.SetFloat(inputXHash, currentInputX);
            animator.SetFloat(inputYHash, currentInputY);

            if (showDebug)
            {
                Debug.Log($"[PlayerAnimation] RawInput: {rawInput} | SpeedRatio: {speedRatio:F2} | " +
                          $"InputX: {currentInputX:F2} | InputY: {currentInputY:F2} | " +
                          $"CurrentSpeed: {playerMovement.CurrentSpeed:F2} | IsMoving: {playerMovement.IsMoving}");
            }
        }

        private void UpdateStateParameters()
        {
            if (playerMovement == null || animator == null) return;

            bool isGrounded = playerMovement.IsGrounded;

            // Trigger landing animation
            if (!wasGrounded && isGrounded)
            {
                animator.SetTrigger(landHash);
            }
            wasGrounded = isGrounded;

            // Update state booleans
            animator.SetBool(isCrouchingHash, playerMovement.IsCrouching);
            animator.SetBool(isSprintingHash, playerMovement.IsSprinting);
            animator.SetBool(isGroundedHash, isGrounded);
        }

        private void HandleJump()
        {
            if (animator != null)
                animator.SetTrigger(jumpHash);
        }

        private void HandleShove()
        {
            if (animator != null)
                animator.SetTrigger(shoveHash);
        }

        private void HandlePickup()
        {
            if (animator != null)
                animator.SetTrigger(pickupHash);
        }

        /// <summary>
        /// Animation event hook for generic footstep markers.
        /// </summary>
        public void AnimEvent_Footstep()
        {
            footstepSystem?.TriggerFootstep();
            playerMovement?.OnAnimationFootstep();
        }

        /// <summary>
        /// Animation event hook for left foot contact.
        /// </summary>
        public void AnimEvent_FootstepLeft()
        {
            footstepSystem?.TriggerLeftFootstep();
            playerMovement?.OnAnimationFootstep();
        }

        /// <summary>
        /// Animation event hook for right foot contact.
        /// </summary>
        public void AnimEvent_FootstepRight()
        {
            footstepSystem?.TriggerRightFootstep();
            playerMovement?.OnAnimationFootstep();
        }

        /// <summary>
        /// Animation event hook for landing impact.
        /// </summary>
        public void AnimEvent_LandFootstep()
        {
            footstepSystem?.PlayLandingSound();
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebug) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 180));
            GUILayout.BeginVertical("box");
            GUILayout.Label("Player Animation Debug");
            GUILayout.Label($"InputX: {currentInputX:F3}");
            GUILayout.Label($"InputY: {currentInputY:F3}");
            GUILayout.Label($"CurrentSpeed: {(playerMovement != null ? playerMovement.CurrentSpeed : 0):F2}");
            GUILayout.Label($"IsMoving: {(playerMovement != null ? playerMovement.IsMoving : false)}");
            GUILayout.Label($"IsSprinting: {(playerMovement != null ? playerMovement.IsSprinting : false)}");
            GUILayout.Label($"IsCrouching: {(playerMovement != null ? playerMovement.IsCrouching : false)}");
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif
    }
}
