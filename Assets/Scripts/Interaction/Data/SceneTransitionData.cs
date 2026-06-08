// Assets/Scripts/Interaction/Data/SceneTransitionData.cs
using System;
using UnityEngine;

namespace Interaction.Data
{
    /// <summary>
    /// Datos de transición de escena configurables por verbo en el Inspector.
    /// Usado por InteractableDataSO para puertas y salidas.
    /// </summary>
    [Serializable]
    public sealed class SceneTransitionData
    {
        [Tooltip("Nombre exacto de la escena destino (debe estar en Build Settings).")]
        [SerializeField] private string _sceneName  = string.Empty;

        [Tooltip("ID del spawn point donde aparece el player en la escena destino.")]
        [SerializeField] private string _spawnId    = "default";

        public string SceneName => _sceneName;
        public string SpawnId   => _spawnId;

        /// <summary>True si la transición está configurada con un nombre de escena válido.</summary>
        public bool IsValid => !string.IsNullOrEmpty(_sceneName);
    }
}