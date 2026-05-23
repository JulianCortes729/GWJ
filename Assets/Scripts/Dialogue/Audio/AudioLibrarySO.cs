using System.Collections.Generic;
using UnityEngine;

namespace Dialogue.Audio
{
    /// <summary>Librería centralizada de clips de audio organizada por categoría.</summary>
    [CreateAssetMenu(
        fileName = "AudioLibrary",
        menuName = "Audio/Audio Library")]
    public sealed class AudioLibrarySO : ScriptableObject
    {
        /// <summary>Lista de clips de efectos de sonido.</summary>
        [SerializeField] private List<AudioClipEntry> _sfxClips   = new();
        
        /// <summary>Lista de clips de música.</summary>
        [SerializeField] private List<AudioClipEntry> _musicClips = new();
        
        /// <summary>Lista de clips de voz.</summary>
        [SerializeField] private List<AudioClipEntry> _voiceClips = new();

        /// <summary>Mapa indexado de efectos de sonido.</summary>
        private Dictionary<string, AudioClip> _sfxMap;
        
        /// <summary>Mapa indexado de música.</summary>
        private Dictionary<string, AudioClip> _musicMap;
        
        /// <summary>Mapa indexado de voces.</summary>
        private Dictionary<string, AudioClip> _voiceMap;

        /// <summary>Construye los mapas al cargar el asset.</summary>
        private void OnEnable() => BuildMaps();

        /// <summary>Construye todos los diccionarios indexados desde las listas.</summary>
        private void BuildMaps()
        {
            _sfxMap   = BuildMap(_sfxClips);
            _musicMap = BuildMap(_musicClips);
            _voiceMap = BuildMap(_voiceClips);
        }

        /// <summary>Construye un diccionario a partir de una lista de entradas, validando duplicados y clips nulos.</summary>
        private static Dictionary<string, AudioClip> BuildMap(
            List<AudioClipEntry> entries)
        {
            var map = new Dictionary<string, AudioClip>(entries.Count);
            foreach (AudioClipEntry entry in entries)
            {
                if (entry.Clip == null)
                {
                    Debug.LogWarning(
                        $"[AudioLibrarySO] Entrada con clave '{entry.Key}' tiene clip nulo.");
                    continue;
                }
                if (map.ContainsKey(entry.Key))
                {
                    Debug.LogWarning(
                        $"[AudioLibrarySO] Clave duplicada '{entry.Key}'. Ignorada.");
                    continue;
                }
                map[entry.Key] = entry.Clip;
            }
            return map;
        }

        /// <summary>Intenta obtener un clip de efecto de sonido por clave.</summary>
        public bool TryGetSfx(string key, out AudioClip clip)
            => _sfxMap.TryGetValue(key, out clip);

        /// <summary>Intenta obtener un clip de música por clave.</summary>
        public bool TryGetMusic(string key, out AudioClip clip)
            => _musicMap.TryGetValue(key, out clip);

        /// <summary>Intenta obtener un clip de voz por clave.</summary>
        public bool TryGetVoice(string key, out AudioClip clip)
            => _voiceMap.TryGetValue(key, out clip);

        /// <summary>Reconstruye los mapas al validar en el editor.</summary>
#if UNITY_EDITOR
        private void OnValidate() => BuildMaps();
#endif
    }
}
