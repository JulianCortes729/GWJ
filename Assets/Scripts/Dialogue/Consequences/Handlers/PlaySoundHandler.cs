using UnityEngine;
using Dialogue.Audio;


namespace Dialogue.Consequences.Handlers
{
    /// <summary>
    /// Comando: play_sound [nombreClip]
    /// Ejemplo en Ink: # play_sound estatica
    /// Delega al AudioManager cuando esté implementado (P4).
    /// Por ahora loguea la intención.
    /// </summary>
    public sealed class PlaySoundHandler : IConsequenceHandler
    {
        /// <summary>Identificador del comando para reproducir sonidos.</summary>
        public string CommandName => "play_sound";

        /// <summary>Reproduce un clip de sonido mediante el AudioManager.</summary>
        /// <param name="parameters">Array con la clave del clip como primer elemento.</param>
        public void Execute(string[] parameters)
        {
             if (parameters.Length < 1)
            {
                Debug.LogWarning("[PlaySoundHandler] Requiere al menos 1 parámetro: claveClip [interrupt].");
                return;
            }

            if (AudioManager.Instance == null)
            {
                Debug.LogError("[PlaySoundHandler] AudioManager.Instance es null.");
                return;
            }
            AudioManager.Instance.PlaySfx(parameters[0]);
        }
    }
}
