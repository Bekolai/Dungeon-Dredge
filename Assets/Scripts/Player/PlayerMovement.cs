using UnityEngine;
using DungeonDredge.Core;

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
        [SerializeField] private float lightThreshold = 0.4f;      // < 40%
        [SerializeField] private float normalThreshold = 1.0f;     // 40-100%
        [SerializeField] private float heavyThreshold = 1.2f;      // 100-120%
        [SerializeField] private float maxOverloadThreshold = 1.4f; // 120-140% (max)

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float gravity = -20f;

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

        // State
        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;
        private bool isCrouching;
        private float currentHeight;
        private float swayTimer;

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
        }

        public void Move(Vector3 direction, bool sprint, bool crouch)
        {
            // Ground check
            isGrounded = controller.isGrounded;
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Small negative to keep grounded
            }

            // Handle crouching
            HandleCrouch(crouch);

            // Calculate speed based on encumbrance
            float speedMultiplier = encumbranceCurve.Evaluate(currentWeightRatio);
            float targetSpeed = baseWalkSpeed * speedMultiplier;

            // Apply sprint or crouch multipliers
            IsSprinting = false;
            if (sprint && CanSprint && !isCrouching)
            {
                targetSpeed *= sprintMultiplier;
                IsSprinting = true;
            }
            else if (isCrouching)
            {
                targetSpeed *= crouchMultiplier;
            }

            CurrentSpeed = targetSpeed;

            // Move
            IsMoving = direction.magnitude > 0.1f;
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

        public void TryJump()
        {
            if (isGrounded && !isCrouching && currentTier != EncumbranceTier.Snail)
            {
                // Reduce jump based on encumbrance
                float jumpMultiplier = encumbranceCurve.Evaluate(currentWeightRatio);
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity) * jumpMultiplier;

                // Generate noise
                GenerateNoise(1.0f);
            }
        }

        private void HandleCrouch(bool wantsCrouch)
        {
            float targetHeight = wantsCrouch ? crouchHeight : normalHeight;
            
            // Smoothly transition height
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            controller.height = currentHeight;
            
            // Adjust center
            controller.center = new Vector3(0, currentHeight / 2f, 0);

            isCrouching = currentHeight < (normalHeight + crouchHeight) / 2f;
        }

        private void HandleFootsteps()
        {
            // Footstep timing based on speed
            float interval = footstepInterval / (CurrentSpeed / baseWalkSpeed);
            footstepTimer += Time.deltaTime;

            if (footstepTimer >= interval)
            {
                footstepTimer = 0f;
                
                // Calculate noise based on tier
                float noiseIntensity = GetNoiseIntensityForTier();
                
                // Reduce noise when crouching
                if (isCrouching)
                    noiseIntensity *= 0.3f;

                GenerateNoise(noiseIntensity);

                // Play footstep audio
                PlayFootstepSound();
            }
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
            currentWeightRatio = Mathf.Clamp(ratio, 0f, maxOverloadThreshold);
            
            // Determine encumbrance tier
            EncumbranceTier newTier;
            if (ratio < lightThreshold)
                newTier = EncumbranceTier.Light;
            else if (ratio < normalThreshold)
                newTier = EncumbranceTier.Medium;
            else if (ratio < heavyThreshold)
                newTier = EncumbranceTier.Heavy;
            else
                newTier = EncumbranceTier.Snail;

            if (newTier != currentTier)
            {
                currentTier = newTier;
                EventBus.Publish(new EncumbranceChangedEvent
                {
                    WeightRatio = ratio,
                    Tier = currentTier
                });
            }
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
