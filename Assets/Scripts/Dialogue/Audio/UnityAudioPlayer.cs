using UnityEngine;

namespace Dialogue.Audio
{
    /// <summary>Implementación de reproducción de audio usando AudioSource de Unity.</summary>
    public sealed class UnityAudioPlayer : IAudioPlayer
    {
        /// <summary>Referencia al componente AudioSource.</summary>
        private readonly AudioSource _source;

        /// <summary>Indica si se está reproduciendo audio actualmente.</summary>
        public bool IsPlaying => _source.isPlaying;

        /// <summary>Inicializa el reproductor con un AudioSource.</summary>
        /// <param name="source">AudioSource a utilizar.</param>
        public UnityAudioPlayer(AudioSource source)
        {
            _source = source;
        }

        /// <summary>Reproduce un clip interrumpiendo cualquier reproducción actual.</summary>
        /// <param name="clip">Clip de audio a reproducir.</param>
        /// <param name="volume">Volumen de reproducción.</param>
        public void PlayInterrupt(AudioClip clip, float volume = 1f)
        {
            _source.Stop();
            _source.loop   = false;
            _source.clip   = clip;
            _source.volume = volume;
            _source.Play();
        }

        /// <summary>Reproduce un clip en bucle.</summary>
        /// <param name="clip">Clip de audio a reproducir.</param>
        /// <param name="volume">Volumen de reproducción.</param>
        public void PlayLooped(AudioClip clip, float volume = 1f)
        {
            if (_source.clip == clip && _source.isPlaying) return;

            _source.loop   = true;
            _source.clip   = clip;
            _source.volume = volume;
            _source.Play();
        }

        /// <summary>Detiene la reproducción y limpia el clip.</summary>
        public void Stop()
        {
            _source.Stop();
            _source.clip = null;
        }

        /// <summary>Ajusta el volumen de reproducción.</summary>
        public void SetVolume(float volume)
            => _source.volume = volume;
    }
}
