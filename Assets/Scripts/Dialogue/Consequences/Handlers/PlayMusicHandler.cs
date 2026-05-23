using UnityEngine;
using Dialogue.Audio;

namespace Dialogue.Consequences.Handlers
{
    public sealed class PlayMusicHandler : IConsequenceHandler
    {
        public string CommandName => "play_music";

        public void Execute(string[] parameters)
        {
            if (parameters.Length < 1)
            {
                Debug.LogWarning("[PlayMusicHandler] Requiere 1 parámetro: claveClip.");
                return;
            }

            if (AudioManager.Instance == null)
            {
                Debug.LogError("[PlayMusicHandler] AudioManager.Instance es null.");
                return;
            }

            AudioManager.Instance.PlayMusic(parameters[0]);
        }
    }
}
