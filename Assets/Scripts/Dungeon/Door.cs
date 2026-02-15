using UnityEngine;
using DungeonDredge.Player;

namespace DungeonDredge.Dungeon
{
    public enum DoorMode
    {
        Archway,        // Always open passage - no interaction, just visual
        Interactive,    // Standard door - click E to open/close
        AutoOpen,       // Opens automatically when player approaches
        Locked,         // Requires key or trigger to unlock
        Blocked         // Solid wall - no passage (used when no room on other side)
    }

    public class Door : MonoBehaviour, IInteractable
    {
        [Header("Door Mode")]
        [SerializeField] private DoorMode doorMode = DoorMode.Archway;
        
        [Header("Door Settings")]
        [SerializeField] private DoorDirection direction;
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool isLocked = false;

        [Header("Visual")]
        [Tooltip("The door panel/frame that moves. Leave empty for archway mode.")]
        [SerializeField] private GameObject doorPanel;
        [Tooltip("Wall segment that fills the doorway when blocked (no connection).")]
        [SerializeField] private GameObject wallFiller;
        
        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float openSpeed = 2f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip lockedSound;

        [Header("NavMesh")]
        [SerializeField] private UnityEngine.AI.NavMeshObstacle navMeshObstacle;

        [Header("Auto-Open Settings")]
        [SerializeField] private float autoOpenDistance = 3f;

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Quaternion targetRotation;
        private Transform playerTransform;

        public DoorDirection Direction => direction;
        public bool IsOpen => isOpen;
        public bool IsLocked => isLocked;
        public DoorMode Mode => doorMode;

        private void Start()
        {
            // Blocked mode: solid wall, no passage
            if (doorMode == DoorMode.Blocked)
            {
                isOpen = false;
                if (doorPanel != null) doorPanel.SetActive(false);
                if (wallFiller != null) wallFiller.SetActive(true);
                UpdateNavMeshObstacle();
                return;
            }

            // For all passable modes, hide wall filler
            if (wallFiller != null) wallFiller.SetActive(false);

            // Archway mode: always open, no door panel
            if (doorMode == DoorMode.Archway)
            {
                isOpen = true;
                if (doorPanel != null) doorPanel.SetActive(false);
                UpdateNavMeshObstacle();
                return;
            }

            // Locked mode starts locked
            if (doorMode == DoorMode.Locked)
            {
                isLocked = true;
            }

            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
            targetRotation = isOpen ? openRotation : closedRotation;

            UpdateNavMeshObstacle();
            
            // Find player for auto-open (may be spawned later at runtime)
            var player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                // Player is spawned at runtime - subscribe to event
                DungeonManager.OnPlayerSpawned += OnPlayerSpawned;
            }
        }

        private void OnPlayerSpawned(GameObject player)
        {
            if (player != null)
            {
                playerTransform = player.transform;
            }
            DungeonManager.OnPlayerSpawned -= OnPlayerSpawned;
        }

        private void OnDestroy()
        {
            DungeonManager.OnPlayerSpawned -= OnPlayerSpawned;
        }

        private void Update()
        {
            // Archway mode doesn't need updates
            if (doorMode == DoorMode.Archway) return;

            // Auto-open mode: check distance to player
            if (doorMode == DoorMode.AutoOpen && playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance < autoOpenDistance && !isOpen)
                {
                    Open();
                }
                else if (distance > autoOpenDistance * 1.5f && isOpen)
                {
                    Close();
                }
            }

            // Smooth door movement
            if (doorPanel != null)
            {
                doorPanel.transform.localRotation = Quaternion.Lerp(
                    doorPanel.transform.localRotation, 
                    targetRotation, 
                    Time.deltaTime * openSpeed);
            }
            else
            {
                transform.localRotation = Quaternion.Lerp(
                    transform.localRotation, 
                    targetRotation, 
                    Time.deltaTime * openSpeed);
            }
        }

        public void Interact(PlayerController player)
        {
            // Archway and auto-open don't respond to interaction
            if (doorMode == DoorMode.Archway || doorMode == DoorMode.AutoOpen)
                return;

            if (isLocked)
            {
                PlaySound(lockedSound);
                return;
            }

            Toggle();
        }

        public string GetInteractionPrompt()
        {
            if (doorMode == DoorMode.Archway) return "";
            if (doorMode == DoorMode.AutoOpen) return "";
            if (isLocked) return "Locked";
          /*   return isOpen ? "Close Door" : "Open Door"; */
          return null;
        }

        public bool CanInteract()
        {
            return doorMode == DoorMode.Interactive || doorMode == DoorMode.Locked;
        }

        public void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        public void Open()
        {
            if (isOpen || isLocked) return;

            isOpen = true;
            targetRotation = openRotation;
            
            if (animator != null)
            {
                animator.SetBool("IsOpen", true);
            }

            PlaySound(openSound);
            UpdateNavMeshObstacle();
        }

        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;
            targetRotation = closedRotation;

            if (animator != null)
            {
                animator.SetBool("IsOpen", false);
            }

            PlaySound(closeSound);
            UpdateNavMeshObstacle();
        }

        public void Lock()
        {
            isLocked = true;
            Close();
        }

        public void Unlock()
        {
            isLocked = false;
            if (doorMode == DoorMode.Locked)
            {
                doorMode = DoorMode.Interactive; // Downgrade to interactive after unlock
            }
        }

        /// <summary>
        /// Set the door mode. Useful for runtime configuration.
        /// </summary>
        public void SetMode(DoorMode mode)
        {
            doorMode = mode;
            
            if (mode == DoorMode.Blocked)
            {
                // Solid wall - no passage
                isOpen = false;
                isLocked = false;
                if (doorPanel != null) doorPanel.SetActive(false);
                if (wallFiller != null) wallFiller.SetActive(true);
            }
            else if (mode == DoorMode.Archway)
            {
                // Open passage - hide the door panel entirely for archways
                isOpen = true;
                isLocked = false;
                if (doorPanel != null) doorPanel.SetActive(false);
                if (wallFiller != null) wallFiller.SetActive(false);
            }
            else if (mode == DoorMode.Locked)
            {
                isLocked = true;
                if (doorPanel != null) doorPanel.SetActive(true);
                if (wallFiller != null) wallFiller.SetActive(false);
            }
            else
            {
                // Interactive or AutoOpen
                if (doorPanel != null) doorPanel.SetActive(true);
                if (wallFiller != null) wallFiller.SetActive(false);
            }
            
            UpdateNavMeshObstacle();
        }

        /// <summary>
        /// Block the doorway with a solid wall (no connection on other side)
        /// </summary>
        public void Block()
        {
            SetMode(DoorMode.Blocked);
        }

        /// <summary>
        /// Unblock and set to archway (open passage)
        /// </summary>
        public void Unblock()
        {
            SetMode(DoorMode.Archway);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void UpdateNavMeshObstacle()
        {
            if (navMeshObstacle != null)
            {
                navMeshObstacle.enabled = !isOpen;
            }
        }
    }
}
