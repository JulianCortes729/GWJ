using UnityEngine;

namespace Dialogue.Audio
{
    /// <summary>Interfaz para reproducción de audio.</summary>
    public interface IAudioPlayer
    {
    
        /// <summary>Reproduce un clip interrumpiendo cualquier reproducción actual.</summary>
        /// <param name="clip">Clip de audio a reproducir.</param>
        /// <param name="volume">Volumen de reproducción (0-1).</param>
        void PlayInterrupt(AudioClip clip, float volume = 1f);

        
        /// <summary>Reproduce un clip de audio en bucle continuo.</summary>
        /// <param name="clip">Clip a reproducir.</param>
        /// <param name="volume">Volumen de reproducción (0-1).</param>
        void PlayLooped(AudioClip clip, float volume = 1f);
        
        /// <summary>Detiene la reproducción de audio.</summary>
        void Stop();
        
        /// <summary>Ajusta el volumen de reproducción.</summary>
        /// <param name="volume">Volumen objetivo (0-1).</param>
        void SetVolume(float volume);
        
        /// <summary>Indica si se está reproduciendo audio actualmente.</summary>
        bool IsPlaying { get; }
    }
}
