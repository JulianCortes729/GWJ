using System.Collections.Generic;
using UnityEngine;
using Dialogue.Core;
using Dialogue.Consequences.Handlers;

namespace Dialogue.Consequences
{
    /// <summary>Gestiona la ejecución de consecuencias desde tags de diálogo.</summary>
    public sealed class ConsequenceSystem : MonoBehaviour
    {
        /// <summary>Referencia al gestor de diálogos.</summary>
        [SerializeField] private DialogueRunner _runner;

        /// <summary>Registro de handlers de consecuencias.</summary>
        private ConsequenceRegistry _registry;

        // ── Lifecycle ────────────────────────────────────────────

        /// <summary>Inicializa el registro y los handlers predefinidos.</summary>
        private void Awake()
        {
            if (_runner == null)
            {
                Debug.LogError(
                    $"[ConsequenceSystem] _runner no asignado en {gameObject.name}.");
                return;
            }

            _registry = new ConsequenceRegistry();
            RegisterBuiltInHandlers();
        }

        /// <summary>Suscribe al evento de línea lista.</summary>
        private void OnEnable()
        {
            //_runner.OnLineReady += HandleLineReady;
            _runner.OnTagsReady += ProcessTags;
        }

        /// <summary>Desuscribe del evento de línea lista.</summary>
        private void OnDisable()
        {
            //_runner.OnLineReady -= HandleLineReady;
            _runner.OnTagsReady -= ProcessTags;
        }

        // ── Registro ─────────────────────────────────────────────

        /// <summary>
        /// Registra los handlers incluidos en el proyecto.
        /// Para agregar un comando nuevo: instanciá su handler y llamá Register().
        /// No toques nada más.
        /// </summary>
        private void RegisterBuiltInHandlers()
        {
            Register(new SetObjectStateHandler());
            Register(new SetObjectVisibilityHandler());
            Register(new PlaySoundHandler());
            Register(new PlayAnimationHandler());
            Register(new PlayMusicHandler());  
            Register(new StopMusicHandler());   
        }

        /// <summary>
        /// API pública para que sistemas externos registren handlers en runtime.
        /// </summary>
        public void Register(IConsequenceHandler handler)
            => _registry.Register(handler);

        /// <summary>Desregistra un handler por nombre de comando.</summary>
        /// <param name="commandName">Nombre del comando a desregistrar.</param>
        public void Unregister(string commandName)
            => _registry.Unregister(commandName);

        // ── Procesamiento de tags ─────────────────────────────────

        /// <summary>Manejador del evento de línea lista desde DialogueRunner.</summary>
        /// <param name="line">Línea de diálogo actual.</param>
        private void HandleLineReady(Data.DialogueLine line)
        {
            // 📌 GDD — Ink adjunta tags a la línea actual via story.currentTags
            // Los tags llegan por evento junto con la línea.
            // ConsequenceSystem los procesa de forma independiente a la UI.
        }

        /// <summary>Procesa los tags de la línea actual.</summary>
        /// <param name="tags">Lista de tags a procesar.</param>
        public void ProcessTags(List<string> tags)
        {
            if (tags == null || tags.Count == 0) return;

            foreach (string tag in tags)
                ProcessSingleTag(tag);
        }

        /// <summary>Procesa un tag individual en formato "command param1 param2".</summary>
        /// <param name="tag">Tag a procesar.</param>
        private void ProcessSingleTag(string tag)
        {
            Debug.Log($"[ConsequenceSystem] Tag recibido: '{tag}'");

            // Formato esperado: "command_name param1 param2"
            // Ejemplo: "set_object_state radio encendida"
            // Ink entrega el tag sin los símbolos # propios de su sintaxis.

            if (string.IsNullOrWhiteSpace(tag)) return;

            string[] parts = tag.Trim().Split(' ');

            if (parts.Length == 0) return;

            string   commandName = parts[0].ToLowerInvariant();
            string[] parameters  = parts.Length > 1
                ? parts[1..]
                : System.Array.Empty<string>();

            _registry.Dispatch(commandName, parameters);
        }
    }
}
