using UnityEngine;
using Dialogue.Data;

namespace Dialogue.Audio
{
    /// <summary>Gestor centralizado de audio con patrón Singleton.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        /// <summary>Librería de clips de audio.</summary>
        [Header("Library")]
        [SerializeField] private AudioLibrarySO _library;

        /// <summary>Volúmenes iniciales de cada categoría de audio.</summary>
        [Header("Volúmenes iniciales")]
        [SerializeField] [Range(0f, 1f)] private float _sfxVolume   = 1f;
        [SerializeField] [Range(0f, 1f)] private float _musicVolume  = 0.7f;
        [SerializeField] [Range(0f, 1f)] private float _voiceVolume  = 1f;

        /// <summary>Reproductores especializados por categoría.</summary>
        private IAudioPlayer _sfxPlayer;
        private IAudioPlayer _musicPlayer;
        private IAudioPlayer _voicePlayer;

        /// <summary>Instancia Singleton del AudioManager.</summary>
        public static AudioManager Instance { get; private set; }

        /// <summary>Inicializa el Singleton y crea los reproductores de audio.</summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_library == null)
            {
                Debug.LogError(
                    $"[AudioManager] _library no asignada en {gameObject.name}.");
                return;
            }

            _sfxPlayer   = CreatePlayer("SFX_Source");
            _musicPlayer = CreatePlayer("Music_Source");
            _voicePlayer = CreatePlayer("Voice_Source");

            _sfxPlayer.SetVolume(_sfxVolume);
            _musicPlayer.SetVolume(_musicVolume);
            _voicePlayer.SetVolume(_voiceVolume);
        }

        // ── API pública ────────────────────────────────────────────
        /// <summary>Reproduce un efecto de sonido, con opción de interrumpir.</summary>
        /// <param name="key">Clave del efecto de sonido.</param>
        /// <param name="interrupt">Indica si se debe interrumpir cualquier reproducción actual.</param>
        public void PlaySfx(string key, bool interrupt = false)
        {
            if (!_library.TryGetSfx(key, out AudioClip clip))
            {
                Debug.LogWarning($"[AudioManager] SFX '{key}' no encontrado en library.");
                return;
            }

            _sfxPlayer.PlayInterrupt(clip, _sfxVolume);
        }

        /// <summary>Reproduce música en bucle.</summary>
        public void PlayMusic(string key)
        {
            if (!_library.TryGetMusic(key, out AudioClip clip))
            {
                Debug.LogWarning($"[AudioManager] Music '{key}' no encontrado en library.");
                return;
            }
            _musicPlayer.PlayLooped(clip, _musicVolume);
        }

        /// <summary>Detiene la reproducción de música.</summary>
        public void StopMusic() => _musicPlayer.Stop();

        /// <summary>Reproduce la voz de un personaje.</summary>
        /// <param name="speaker">Nombre del personaje cuya voz se desea reproducir.</param>
        public void PlayVoice(string speaker)
        {
            if (!_library.TryGetVoice(speaker, out AudioClip clip))
            {
                // 📌 GDD — voz opcional: no todos los speakers tienen clip
                return;
            }
            _voicePlayer.PlayInterrupt(clip, _voiceVolume); // voces siempre interrumpen
        }

        /// <summary>Ajusta el volumen de efectos de sonido.</summary>
        /// <param name="volume">Volumen objetivo (0-1).</param>
        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            _sfxPlayer.SetVolume(_sfxVolume);
        }

        /// <summary>Ajusta el volumen de música.</summary>
        /// <param name="volume">Volumen objetivo (0-1).</param>
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            _musicPlayer.SetVolume(_musicVolume);
        }

        /// <summary>Ajusta el volumen de voces.</summary>
        /// <param name="volume">Volumen objetivo (0-1).</param>
        public void SetVoiceVolume(float volume)
        {
            _voiceVolume = Mathf.Clamp01(volume);
            _voicePlayer.SetVolume(_voiceVolume);
        }

        // ── Privados ───────────────────────────────────────────────

        /// <summary>Crea un reproductor de audio con AudioSource independiente.</summary>
        /// <param name="sourceName">Nombre del AudioSource.</param>
        private IAudioPlayer CreatePlayer(string sourceName)
        {
            var go     = new GameObject(sourceName);
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return new UnityAudioPlayer(source);
        }
    }
}
