// Assets/Scripts/Interaction/Data/InteractableDataSO.cs
using System;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Data
{
    /// <summary>
    /// Configuración compartida para un tipo de objeto interactuable.
    /// Un asset por tipo: "Teléfono", "Mesa de radio", "Contestador", etc.
    /// Múltiples instancias en escena pueden referenciar el mismo asset.
    /// </summary>
    [CreateAssetMenu(fileName = "Interactable_", menuName = "Esquel/Interaction/InteractableData")]
    public sealed class InteractableDataSO : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Nombre que se muestra en el cursor al hacer hover.")]
        [SerializeField] private string _displayName = "Objeto";

        [Header("Verbos habilitados")]
        [Tooltip("Qué acciones puede recibir este objeto.")]
        [SerializeField] private bool _canLook = true;
        [SerializeField] private bool _canTalk = false;
        [SerializeField] private bool _canUse  = false;

        [Header("Ink — Knots por verbo")]
        [Tooltip("Knot de Ink que se inicia al usar 'Mirar'. Dejar vacío si no tiene diálogo.")]
        [SerializeField] private string _lookKnot  = string.Empty;
        [SerializeField] private string _talkKnot  = string.Empty;
        [SerializeField] private string _useKnot   = string.Empty;

        [Header("Consecuencias directas por verbo")]
        [Tooltip("Tags de consecuencia en formato 'comando param1 param2'. Se ejecutan sin pasar por Ink.")]
        [SerializeField] private string[] _lookConsequences  = Array.Empty<string>();
        [SerializeField] private string[] _talkConsequences  = Array.Empty<string>();
        [SerializeField] private string[] _useConsequences   = Array.Empty<string>();


        [Header("Transición de escena por verbo")]
        [Tooltip("Si está configurado, al usar ese verbo se transiciona a la escena indicada. " +
                 "Tiene prioridad más baja que Ink y consecuencias — solo aplica si los anteriores están vacíos.")]
        [SerializeField] private SceneTransitionData _lookSceneTransition;
        [SerializeField] private SceneTransitionData _talkSceneTransition;
        [SerializeField] private SceneTransitionData _useSceneTransition;

        // ── API de lectura ───────────────────────────────────────
        public string DisplayName => _displayName;

        public bool CanLook => _canLook;
        public bool CanTalk => _canTalk;
        public bool CanUse  => _canUse;

        /// <summary>Devuelve true si este verbo está habilitado para el objeto.</summary>
        public bool Supports(VerbType verb) => verb switch
        {
            VerbType.Look => _canLook,
            VerbType.Talk => _canTalk,
            VerbType.Use  => _canUse,
            _             => false
        };

        /// <summary>
        /// Devuelve el knot de Ink asociado al verbo.
        /// Retorna null si no hay knot configurado.
        /// </summary>
        public string GetKnot(VerbType verb) => verb switch
        {
            VerbType.Look => string.IsNullOrEmpty(_lookKnot) ? null : _lookKnot,
            VerbType.Talk => string.IsNullOrEmpty(_talkKnot) ? null : _talkKnot,
            VerbType.Use  => string.IsNullOrEmpty(_useKnot)  ? null : _useKnot,
            _             => null
        };

        /// <summary>
        /// Devuelve las consecuencias directas del verbo.
        /// Puede ser array vacío — nunca null.
        /// </summary>
        public string[] GetConsequences(VerbType verb) => verb switch
        {
            VerbType.Look => _lookConsequences  ?? Array.Empty<string>(),
            VerbType.Talk => _talkConsequences  ?? Array.Empty<string>(),
            VerbType.Use  => _useConsequences   ?? Array.Empty<string>(),
            _             => Array.Empty<string>()
        };


        /// <summary>
        /// Devuelve los datos de transición de escena para el verbo dado.
        /// Retorna null si no está configurado o no es válido.
        /// </summary>
        public SceneTransitionData GetSceneTransition(VerbType verb)
        {
            var data = verb switch
            {
                VerbType.Look => _lookSceneTransition,
                VerbType.Talk => _talkSceneTransition,
                VerbType.Use  => _useSceneTransition,
                _             => null
            };
 
            return data is { IsValid: true } ? data : null;
        }
    }
}
