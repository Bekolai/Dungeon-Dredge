using UnityEngine;
using DungeonDredge.Player;

namespace DungeonDredge.Dungeon
{
    public class Door : MonoBehaviour, IInteractable
    {
        [Header("Door Settings")]
        [SerializeField] private DoorDirection direction;
        [SerializeField] private bool isOpen = false;
        [SerializeField] private bool isLocked = false;

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

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Quaternion targetRotation;

        public DoorDirection Direction => direction;
        public bool IsOpen => isOpen;
        public bool IsLocked => isLocked;

        private void Start()
        {
            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
            targetRotation = isOpen ? openRotation : closedRotation;

            UpdateNavMeshObstacle();
        }

        private void Update()
        {
            // Smooth door movement
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation, 
                targetRotation, 
                Time.deltaTime * openSpeed);
        }

        public void Interact(PlayerController player)
        {
            if (isLocked)
            {
                PlaySound(lockedSound);
                return;
            }

            Toggle();
        }

        public string GetInteractionPrompt()
        {
            if (isLocked) return "Locked";
            return isOpen ? "Close Door" : "Open Door";
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
