using System;
using UnityEngine;

namespace Dialogue.Audio
{
    /// <summary>Almacena una referencia a un clip de audio asociado a una clave.</summary>
    [Serializable]
    public sealed class AudioClipEntry
    {
        /// <summary>Identificador único del clip de audio.</summary>
        [SerializeField] private string    _key;
        
        /// <summary>Clip de audio serializable en el editor.</summary>
        [SerializeField] private AudioClip _clip;

        /// <summary>Obtiene el identificador del clip de audio.</summary>
        public string    Key  => _key;
        
        /// <summary>Obtiene el clip de audio.</summary>
        public AudioClip Clip => _clip;
    }
}