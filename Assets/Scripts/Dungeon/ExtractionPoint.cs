using UnityEngine;
using DungeonDredge.Player;
using DungeonDredge.Core;

namespace DungeonDredge.Dungeon
{
    public class ExtractionPoint : MonoBehaviour, IInteractable
    {
        [Header("Extraction Settings")]
        [SerializeField] private float extractionTime = 3f;
        [SerializeField] private bool requireAllEnemiesCleared = false;

        [Header("Visual")]
        [SerializeField] private GameObject activeIndicator;
        [SerializeField] private GameObject progressIndicator;
        [SerializeField] private ParticleSystem extractionParticles;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip extractionStartSound;
        [SerializeField] private AudioClip extractionCompleteSound;

        // State
        private bool isExtracting = false;
        private float extractionProgress = 0f;
        private PlayerController extractingPlayer;

        // Events
        public System.Action<float> OnExtractionProgress;
        public System.Action OnExtractionComplete;
        public System.Action OnExtractionCancelled;

        public bool IsExtracting => isExtracting;
        public float ExtractionProgress => extractionProgress / extractionTime;

        private void Start()
        {
            if (activeIndicator != null)
                activeIndicator.SetActive(true);
            if (progressIndicator != null)
                progressIndicator.SetActive(false);
        }

        private void Update()
        {
            if (isExtracting)
            {
                UpdateExtraction();
            }
        }

        public void Interact(PlayerController player)
        {
            if (isExtracting)
            {
                CancelExtraction();
            }
            else
            {
                StartExtraction(player);
            }
        }

        public string GetInteractionPrompt()
        {
            if (isExtracting)
            {
                int percent = Mathf.RoundToInt(ExtractionProgress * 100);
                return $"Extracting... {percent}% (Release to cancel)";
            }

            if (requireAllEnemiesCleared && !AreAllEnemiesCleared())
            {
                return "Clear all enemies to extract";
            }

            return $"Hold to Extract ({extractionTime}s)";
        }

        private void StartExtraction(PlayerController player)
        {
            if (requireAllEnemiesCleared && !AreAllEnemiesCleared())
            {
                Debug.Log("Cannot extract - enemies remain!");
                return;
            }

            isExtracting = true;
            extractionProgress = 0f;
            extractingPlayer = player;

            // Visual feedback
            if (progressIndicator != null)
                progressIndicator.SetActive(true);
            if (extractionParticles != null)
                extractionParticles.Play();

            // Audio
            PlaySound(extractionStartSound);

            // Event
            EventBus.Publish(new ExtractionStartedEvent { Duration = extractionTime });
        }

        private void UpdateExtraction()
        {
            extractionProgress += Time.deltaTime;

            // Update visual
            OnExtractionProgress?.Invoke(ExtractionProgress);

            // Check completion
            if (extractionProgress >= extractionTime)
            {
                CompleteExtraction();
            }

            // Check if player moved away
            if (extractingPlayer != null)
            {
                float distance = Vector3.Distance(transform.position, extractingPlayer.transform.position);
                if (distance > 5f)
                {
                    CancelExtraction();
                }
            }
        }

        private void CompleteExtraction()
        {
            isExtracting = false;

            // Visual
            if (progressIndicator != null)
                progressIndicator.SetActive(false);
            if (extractionParticles != null)
                extractionParticles.Stop();

            // Audio
            PlaySound(extractionCompleteSound);

            // Notify game manager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerExtracted();
            }

            OnExtractionComplete?.Invoke();
        }

        public void CancelExtraction()
        {
            if (!isExtracting) return;

            isExtracting = false;
            extractionProgress = 0f;
            extractingPlayer = null;

            // Visual
            if (progressIndicator != null)
                progressIndicator.SetActive(false);
            if (extractionParticles != null)
                extractionParticles.Stop();

            OnExtractionCancelled?.Invoke();
        }

        private bool AreAllEnemiesCleared()
        {
            // Check if dungeon room is cleared
            Room room = GetComponentInParent<Room>();
            if (room != null)
            {
                return room.IsCleared;
            }

            // Fallback - check if any enemies exist
            return GameObject.FindGameObjectsWithTag("Enemy").Length == 0;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && isExtracting)
            {
                CancelExtraction();
            }
        }
    }
}
