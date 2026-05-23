using UnityEngine;
using Dialogue.Audio;

namespace Dialogue.Consequences.Handlers
{
    public sealed class StopMusicHandler : IConsequenceHandler
    {
        public string CommandName => "stop_music";

        public void Execute(string[] parameters)
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogError("[StopMusicHandler] AudioManager.Instance es null.");
                return;
            }

            AudioManager.Instance.StopMusic();
        }
    }
}
