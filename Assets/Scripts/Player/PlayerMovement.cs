using UnityEngine;
using DungeonDredge.Core;
using DungeonDredge.Audio;

namespace DungeonDredge.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float baseWalkSpeed = 5f;
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float crouchMultiplier = 0.5f;

        [Header("Encumbrance Settings")]
        [SerializeField] private AnimationCurve encumbranceCurve;
        
        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpCooldown = 0.5f; // Cooldown after landing before can jump again

        [Header("Crouch Settings")]
        [SerializeField] private float normalHeight = 2f;
        [SerializeField] private float crouchHeight = 1f;
        [SerializeField] private float crouchTransitionSpeed = 10f;

        [Header("Camera Sway (Snail Tier)")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float maxSwayAmount = 0.5f;
        [SerializeField] private float swaySpeed = 2f;

        [Header("Audio")]
        [SerializeField] private float footstepInterval = 0.5f;
        [SerializeField] private AudioSource footstepAudioSource;
        [SerializeField] private FootstepSystem footstepSystem;
        [Tooltip("If true, footstep audio/noise should be triggered from animation events.")]
        [SerializeField] private bool useAnimationDrivenFootsteps = true;

        private StaminaSystem staminaSystem;

        // State
        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;
        private bool isCrouching;
        private float currentHeight;
        private float swayTimer;
        private bool hasJumped; // Track if we've jumped and are ascending
        private float jumpCooldownTimer; // Timer for jump cooldown after landing
        
        [Header("Ground Detection")]
        [SerializeField] private float groundCheckOffset = 0.1f;
        [SerializeField] private float groundCheckRadius = 0.4f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float groundCheckBuffer = 0.1f; // Buffer to prevent flickering when hovering

        // Encumbrance
        private float currentWeightRatio = 0f;
        private EncumbranceTier currentTier = EncumbranceTier.Light;

        // Footsteps
        private float footstepTimer;
        private float lastNoiseTime;

        // Properties
        public bool CanSprint => currentTier != EncumbranceTier.Heavy && 
                                  currentTier != EncumbranceTier.Snail;
        public bool IsGrounded => isGrounded;
        public bool IsCrouching => isCrouching;
        public bool IsMoving { get; private set; }
        public bool IsSprinting { get; private set; }
        public float CurrentSpeed { get; private set; }
        public EncumbranceTier CurrentTier => currentTier;
        public Vector3 Velocity => controller.velocity;
        public PlayerVoiceManager PlayerVoiceManager { get; private set; }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            currentHeight = normalHeight;

            // Set default encumbrance curve if not assigned
            if (encumbranceCurve == null || encumbranceCurve.length == 0)
            {
                encumbranceCurve = new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.4f, 1f),
                    new Keyframe(1f, 0.8f),
                    new Keyframe(1.2f, 0.5f),
                    new Keyframe(1.4f, 0.3f)
                );
            }

            if (cameraTransform == null)
                cameraTransform = Camera.main?.transform;

            if (footstepSystem == null)
                footstepSystem = GetComponent<FootstepSystem>();
            
            if (staminaSystem == null)
                staminaSystem = GetComponent<StaminaSystem>();

            if (PlayerVoiceManager == null)
                PlayerVoiceManager = GetComponent<PlayerVoiceManager>();
        }

        public void Move(Vector3 direction, bool sprint, bool crouch)
        {
            // Update jump cooldown timer
            if (jumpCooldownTimer > 0f)
            {
                jumpCooldownTimer -= Time.deltaTime;
            }
            
            // Ground check
            CheckGrounded();
            
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small negative to keep grounded
            }

            // Handle crouching (visual height transition)
            HandleCrouch(crouch);

            // Calculate speed based on encumbrance
            float speedMultiplier = encumbranceCurve.Evaluate(currentWeightRatio);
            float targetSpeed = baseWalkSpeed * speedMultiplier;

            // Block movement if exhausted in Snail tier
            if (currentTier == EncumbranceTier.Snail && staminaSystem != null && staminaSystem.IsExhausted)
            {
                targetSpeed = 0f;
            }

            // Apply sprint or crouch multipliers
            // Use crouch INPUT directly for speed (not isCrouching state which waits for height transition)
            IsSprinting = false;
            if (sprint && CanSprint && !crouch)
            {
                targetSpeed *= sprintMultiplier;
                IsSprinting = true;
            }
            else if (crouch)
            {
                targetSpeed *= crouchMultiplier;
            }

            CurrentSpeed = targetSpeed;

            // Move
            IsMoving = direction.magnitude > 0.1f && targetSpeed > 0.01f;
            if (IsMoving)
            {
                Vector3 move = direction.normalized * targetSpeed * Time.deltaTime;
                controller.Move(move);

                // Handle footsteps and noise
                HandleFootsteps();
            }

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            // Apply camera sway for Snail tier
            if (currentTier == EncumbranceTier.Snail && IsMoving)
            {
                ApplyCameraSway();
            }
            
        }

        private void CheckGrounded()
        {
            // If we've jumped and are ascending, we're definitely not grounded
            if (hasJumped && velocity.y > 0.1f)
            {
                isGrounded = false;
                return;
            }
            
            // Center of the bottom sphere of the capsule
            Vector3 spherePosition = transform.position + Vector3.down * (controller.height / 2f - controller.radius + groundCheckOffset);
            
            // Check if we're ascending - if so, don't consider grounded even if controller says we are
            bool isAscending = velocity.y > groundCheckBuffer;
            
            // If CharacterController is confident AND we're not ascending, trust it
            if (controller.isGrounded && !isAscending)
            {
                bool wasGroundedLocal = isGrounded;
                isGrounded = true;
                // Reset jump flag and cooldown when we land
                if (!wasGroundedLocal && velocity.y <= 0)
                {
                    hasJumped = false;
                    jumpCooldownTimer = jumpCooldown; // Start cooldown when landing
                }
                return;
            }
            
            // Fallback sphere cast for edges/slopes
            bool wasGrounded = isGrounded;
            bool sphereCheck = Physics.CheckSphere(spherePosition, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
            
            // Only consider grounded if sphere check passes AND we're not ascending
            isGrounded = sphereCheck && !isAscending;
            
            // Reset jump flag and cooldown when we land
            if (!wasGrounded && isGrounded && velocity.y <= 0)
            {
                hasJumped = false;
                jumpCooldownTimer = jumpCooldown; // Start cooldown when landing
            }
        }

        public void TryJump()
        {
            // Only allow jumping if grounded, not crouching, not in Snail tier, haven't already jumped, and cooldown has expired
            if (isGrounded && !isCrouching && currentTier != EncumbranceTier.Snail && !hasJumped && jumpCooldownTimer <= 0f)
            {
                // Mark that we've jumped
                hasJumped = true;
                
                // Force Unground
                isGrounded = false;
                
                // Reduce jump based on encumbrance
              //  float jumpMultiplier = encumbranceCurve.Evaluate(currentWeightRatio);
              //  velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity) * jumpMultiplier;
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                // Play jump sound
                PlayerVoiceManager?.PlayJumpSound();

                // Generate noise
                GenerateNoise(1.0f);
            }
        }

        private void HandleCrouch(bool wantsCrouch)
        {
            // Set crouch state directly from input (toggle)
            // This prevents animation flickering from height threshold oscillation
            isCrouching = wantsCrouch;
            
            float targetHeight = wantsCrouch ? crouchHeight : normalHeight;
            
            // Smoothly transition height for visuals/collision
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            controller.height = currentHeight;
            
            // Adjust center
            controller.center = new Vector3(0, currentHeight / 2f, 0);
        }

        private void HandleFootsteps()
        {
            if (useAnimationDrivenFootsteps && footstepSystem != null)
            {
                footstepTimer = 0f;
                return;
            }

            // Footstep timing based on speed
            float interval = footstepInterval / (CurrentSpeed / baseWalkSpeed);
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= interval)
            {
                footstepTimer = 0f;
                
                EmitFootstepNoise();

                // Prefer the shared footstep system when present.
                if (footstepSystem != null)
                    footstepSystem.TriggerFootstep();
                else
                    PlayFootstepSound();
            }
        }

        public void OnAnimationFootstep()
        {
            EmitFootstepNoise();
        }

        private void EmitFootstepNoise()
        {
            float noiseIntensity = GetNoiseIntensityForTier();
            if (isCrouching)
            {
                float speedRatio = CurrentSpeed / Mathf.Max(0.1f, baseWalkSpeed);

                // Crouch-walk is near silent, but "fast crouch movement" still carries risk.
                // This keeps stealth reliable at low speed while preventing free sprint-like crouch movement.
                if (speedRatio <= 0.38f)
                {
                    noiseIntensity *= 0.04f;
                }
                else
                {
                    float riskFactor = Mathf.InverseLerp(0.38f, 0.85f, speedRatio);
                    noiseIntensity *= Mathf.Lerp(0.12f, 0.45f, riskFactor);
                }
            }

            GenerateNoise(noiseIntensity);
        }

        private float GetNoiseIntensityForTier()
        {
            return currentTier switch
            {
                EncumbranceTier.Light => 0.2f,
                EncumbranceTier.Medium => 0.5f,
                EncumbranceTier.Heavy => 0.8f,
                EncumbranceTier.Snail => 1.0f,
                _ => 0.5f
            };
        }

        private void GenerateNoise(float intensity)
        {
            if (Time.time - lastNoiseTime < 0.1f) return;
            lastNoiseTime = Time.time;

            // Sprinting increases noise
            if (IsSprinting)
                intensity *= 1.5f;

            EventBus.Publish(new NoiseEvent
            {
                Position = transform.position,
                Intensity = intensity,
                Source = gameObject
            });
        }

        private void PlayFootstepSound()
        {
            if (footstepAudioSource == null) return;

            // Volume based on encumbrance tier
            float volume = GetNoiseIntensityForTier();
            if (isCrouching) volume *= 0.3f;

            footstepAudioSource.volume = volume;
            footstepAudioSource.pitch = Random.Range(0.9f, 1.1f);
            footstepAudioSource.Play();
        }

        private void ApplyCameraSway()
        {
            if (cameraTransform == null) return;

            swayTimer += Time.deltaTime * swaySpeed;
            
            float swayX = Mathf.Sin(swayTimer) * maxSwayAmount;
            float swayY = Mathf.Cos(swayTimer * 2f) * maxSwayAmount * 0.5f;

            Vector3 currentEuler = cameraTransform.localEulerAngles;
            cameraTransform.localEulerAngles = new Vector3(
                currentEuler.x,
                currentEuler.y,
                swayX
            );
        }

        /// <summary>
        /// Set the current weight ratio (0-1.4 range, where 1.0 = 100% capacity)
        /// </summary>
        public void SetWeightRatio(float ratio)
        {
            currentWeightRatio = Mathf.Clamp(ratio, 0f, EncumbranceUtils.MaxOverloadThreshold);
            
            EncumbranceTier newTier = EncumbranceUtils.GetTier(currentWeightRatio);
            Debug.Log("New Tier: " + newTier);

            if (newTier != currentTier)
            {
                currentTier = newTier;
            }

            // Publish event on every change so UI updates
            EventBus.Publish(new EncumbranceChangedEvent
            {
                WeightRatio = ratio,
                Tier = currentTier
            });
        }

        public float GetNoiseMultiplier()
        {
            float multiplier = GetNoiseIntensityForTier();
            if (isCrouching) multiplier *= 0.3f;
            if (IsSprinting) multiplier *= 1.5f;
            return multiplier;
        }
    }
}
