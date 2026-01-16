using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using DungeonDredge.Core;

namespace DungeonDredge.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer mainMixer;
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";

        [Header("Music Sources")]
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;
        [SerializeField] private float musicCrossfadeTime = 2f;

        [Header("Ambient Source")]
        [SerializeField] private AudioSource ambientSource;

        [Header("UI Source")]
        [SerializeField] private AudioSource uiSource;

        [Header("Music Tracks")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip villageMusic;
        [SerializeField] private AudioClip dungeonCalmMusic;
        [SerializeField] private AudioClip dungeonTenseMusic;
        [SerializeField] private AudioClip chaseMusic;

        [Header("UI Sounds")]
        [SerializeField] private AudioClip buttonClick;
        [SerializeField] private AudioClip menuOpen;
        [SerializeField] private AudioClip menuClose;
        [SerializeField] private AudioClip itemPickup;
        [SerializeField] private AudioClip itemDrop;
        [SerializeField] private AudioClip questComplete;
        [SerializeField] private AudioClip levelUp;

        // State
        private AudioSource currentMusicSource;
        private float currentThreatLevel = 0f;
        private bool isCrossfading = false;

        // Volume
        private float masterVolume = 1f;
        private float musicVolume = 1f;
        private float sfxVolume = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            currentMusicSource = musicSourceA;
            LoadVolumeSettings();
        }

        private void Start()
        {
            // Subscribe to game state changes
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }

            // Subscribe to stealth events
            if (StealthManager.Instance != null)
            {
                StealthManager.Instance.OnThreatDetected += OnThreatDetected;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }

            if (StealthManager.Instance != null)
            {
                StealthManager.Instance.OnThreatDetected -= OnThreatDetected;
            }
        }

        private void Update()
        {
            UpdateDynamicMusic();
        }

        #region Music

        private void OnGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    PlayMusic(menuMusic);
                    break;
                case GameState.Village:
                    PlayMusic(villageMusic);
                    break;
                case GameState.Dungeon:
                    PlayMusic(dungeonCalmMusic);
                    break;
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null) return;
            if (currentMusicSource.clip == clip && currentMusicSource.isPlaying) return;

            if (isCrossfading) return;

            StartCoroutine(CrossfadeMusic(clip, loop));
        }

        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip, bool loop)
        {
            isCrossfading = true;

            AudioSource fadeOut = currentMusicSource;
            AudioSource fadeIn = currentMusicSource == musicSourceA ? musicSourceB : musicSourceA;

            fadeIn.clip = newClip;
            fadeIn.loop = loop;
            fadeIn.volume = 0f;
            fadeIn.Play();

            float elapsed = 0f;
            float startVolume = fadeOut.volume;

            while (elapsed < musicCrossfadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / musicCrossfadeTime;

                fadeOut.volume = Mathf.Lerp(startVolume, 0f, t);
                fadeIn.volume = Mathf.Lerp(0f, musicVolume, t);

                yield return null;
            }

            fadeOut.Stop();
            fadeOut.volume = musicVolume;

            currentMusicSource = fadeIn;
            isCrossfading = false;
        }

        private void OnThreatDetected(float distance, GameObject threat)
        {
            float maxDistance = 30f;
            currentThreatLevel = 1f - Mathf.Clamp01(distance / maxDistance);
        }

        private void UpdateDynamicMusic()
        {
            if (GameManager.Instance?.CurrentState != GameState.Dungeon) return;

            // Fade threat level
            if (StealthManager.Instance?.NearestThreat == null)
            {
                currentThreatLevel = Mathf.Lerp(currentThreatLevel, 0f, Time.deltaTime * 0.5f);
            }

            // Switch music based on threat
            if (currentThreatLevel > 0.7f)
            {
                if (currentMusicSource.clip != chaseMusic)
                {
                    PlayMusic(chaseMusic);
                }
            }
            else if (currentThreatLevel > 0.3f)
            {
                if (currentMusicSource.clip != dungeonTenseMusic)
                {
                    PlayMusic(dungeonTenseMusic);
                }
            }
            else
            {
                if (currentMusicSource.clip != dungeonCalmMusic)
                {
                    PlayMusic(dungeonCalmMusic);
                }
            }
        }

        public void StopMusic()
        {
            musicSourceA.Stop();
            musicSourceB.Stop();
        }

        #endregion

        #region Ambient

        public void PlayAmbient(AudioClip clip)
        {
            if (ambientSource == null || clip == null) return;

            ambientSource.clip = clip;
            ambientSource.loop = true;
            ambientSource.Play();
        }

        public void StopAmbient()
        {
            ambientSource?.Stop();
        }

        #endregion

        #region UI Sounds

        public void PlayButtonClick()
        {
            PlayUISound(buttonClick);
        }

        public void PlayMenuOpen()
        {
            PlayUISound(menuOpen);
        }

        public void PlayMenuClose()
        {
            PlayUISound(menuClose);
        }

        public void PlayItemPickup()
        {
            PlayUISound(itemPickup);
        }

        public void PlayItemDrop()
        {
            PlayUISound(itemDrop);
        }

        public void PlayQuestComplete()
        {
            PlayUISound(questComplete);
        }

        public void PlayLevelUp()
        {
            PlayUISound(levelUp);
        }

        private void PlayUISound(AudioClip clip)
        {
            if (uiSource == null || clip == null) return;
            uiSource.PlayOneShot(clip);
        }

        #endregion

        #region 3D Sound

        public void PlaySoundAt(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume * sfxVolume);
        }

        public AudioSource CreateTemporarySource(Vector3 position, AudioClip clip, float volume = 1f)
        {
            GameObject tempGO = new GameObject("TempAudio");
            tempGO.transform.position = position;

            AudioSource source = tempGO.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume * sfxVolume;
            source.spatialBlend = 1f;
            source.Play();

            Destroy(tempGO, clip.length + 0.1f);
            return source;
        }

        #endregion

        #region Volume Control

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            
            if (mainMixer != null)
            {
                mainMixer.SetFloat(masterVolumeParam, VolumeToDecibels(masterVolume));
            }
            
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            
            if (mainMixer != null)
            {
                mainMixer.SetFloat(musicVolumeParam, VolumeToDecibels(musicVolume));
            }

            // Update current music sources
            if (musicSourceA != null) musicSourceA.volume = musicVolume;
            if (musicSourceB != null) musicSourceB.volume = musicVolume;
            
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            
            if (mainMixer != null)
            {
                mainMixer.SetFloat(sfxVolumeParam, VolumeToDecibels(sfxVolume));
            }
            
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        }

        private float VolumeToDecibels(float volume)
        {
            return volume > 0 ? Mathf.Log10(volume) * 20f : -80f;
        }

        private void LoadVolumeSettings()
        {
            SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 1f));
            SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 1f));
            SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
        }

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;

        #endregion
    }
}
