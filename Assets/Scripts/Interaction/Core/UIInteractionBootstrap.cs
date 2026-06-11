// Assets/Scripts/Interaction/Core/UIInteractionBootstrap.cs
using System;
using Interaction.Core;
using Interaction.Handlers;
using Journey.Presentation;
using UnityEngine;

namespace Interaction.Core
{
    /// <summary>
    /// Entrada de configuración en el Inspector:
    /// qué objeto + qué verbo → qué MessageBoardUI abre.
    /// </summary>
    [Serializable]
    public struct UIInteractionEntry
    {
        [Tooltip("Objeto del escritorio que el jugador puede interactuar.")]
        public InteractableObject Target;

        [Tooltip("Verbo que activa la apertura del panel. Generalmente 'Use'.")]
        public VerbType Verb;

        [Tooltip("Panel de mensajes que se abre al interactuar con este objeto.")]
        public MessageBoardUI Board;
    }

    /// <summary>
    /// Bootstrap de escena para el sistema de apertura de paneles UI via interacción.
    /// Patrón idéntico a DialogueSystemBootstrap y SceneTransitionBootstrap.
    /// Colocar en un GameObject vacío de la escena junto a los otros bootstraps.
    /// </summary>
    public sealed class UIInteractionBootstrap : MonoBehaviour
    {
        // ── Dependencias ─────────────────────────────────────────
        [SerializeField] private InteractionSystem    _interactionSystem;

        [Header("Mapeo: Objeto → Panel")]
        [Tooltip("Una entrada por objeto interactuable del escritorio.")]
        [SerializeField] private UIInteractionEntry[] _entries = Array.Empty<UIInteractionEntry>();

        // ── Lifecycle ─────────────────────────────────────────────
        private void Awake()
        {
            if (_interactionSystem == null)
                _interactionSystem = FindAnyObjectByType<InteractionSystem>(); // 🟡 PERF — solo en Awake
        }

        private void Start()
        {
            // Start() garantiza que todos los Awake() corrieron.
            // InkHandler ya está registrado. UIOpenHandler va en segunda posición.
            var handler = new UIOpenInteractionHandler();

            foreach (var entry in _entries)
            {
                if (entry.Target == null || entry.Board == null)
                {
                    Debug.LogWarning("[UIInteractionBootstrap] Entrada incompleta ignorada. " +
                                     "Revisá que Target y Board estén asignados.");
                    continue;
                }

                handler.Register(entry.Target, entry.Verb, entry.Board);
                Debug.Log($"[UIInteractionBootstrap] Registrado: " +
                          $"'{entry.Target.DisplayName}' + {entry.Verb} → {entry.Board.name}");
            }

            _interactionSystem.RegisterUIOpenHandler(handler);
        }
    }
}
