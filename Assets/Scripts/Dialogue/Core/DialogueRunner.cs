using System;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;
using Dialogue.Data;

namespace Dialogue.Core
{
    // Orquestador central de P1.
    // Responsabilidad única: cargar stories de Ink, avanzar líneas,
    // exponer opciones y notificar al resto del juego via eventos.
    // No sabe nada de UI, audio ni consecuencias — eso es P2 y P3.

    public sealed class DialogueRunner : MonoBehaviour
    {
        // ─── Dependencias ─────────────────────────────────────────────

        [SerializeField] private DialogueStorageSO _storage;

        // ─── Eventos ──────────────────────────────────────────────────
        // ⚠️ SOLID — En P3 estos eventos migran a SO Event Channels
        // para desacoplar completamente del ConsequenceSystem.

        // Dispara cuando hay una nueva línea para mostrar.
        public event Action<DialogueLine> OnLineReady;

        // Dispara cuando Ink presenta opciones al jugador.
        public event Action<List<Choice>> OnChoicesReady;

        // Dispara cuando la conversación termina.
        public event Action OnDialogueEnded;

        public event Action<List<string>> OnTagsReady;  // ⚠️ SOLID — en P3 migra a SO Event Channel


        // ─── Estado interno ───────────────────────────────────────────

        private Story                  _currentStory;
        private DialogueVariableStorage _variableStorage;
        private bool                   _isRunning;

        public bool IsRunning => _isRunning;

        // ─── Lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            if (_storage == null)
            {
                Debug.LogError(
                    $"[DialogueRunner] _storage no asignado en " +
                    $"{gameObject.name}. Asigná el SO desde el Inspector.");
                return;
            }

            _variableStorage = new DialogueVariableStorage(_storage);
        }

        private void OnDestroy()
        {
            // Limpiamos el observer de Ink para evitar memory leaks.
            _variableStorage?.UnbindFromCurrentStory();
        }

        // ─── API pública ──────────────────────────────────────────────

        /// <summary>
        /// Carga una historia completa de Ink en memoria.
        /// Llamar UNA SOLA VEZ al iniciar la escena o al cambiar de habitación.
        /// No inicia ningún diálogo — solo prepara la historia para ser usada.
        /// </summary>
        public void LoadStory(TextAsset inkJsonAsset)
        {
            if (inkJsonAsset == null)
            {
                Debug.LogError("[DialogueRunner] inkJsonAsset es null.");
                return;
            }

            if (_isRunning)
            {
                Debug.LogWarning(
                    "[DialogueRunner] Se intentó cargar una historia mientras " +
                    "un diálogo estaba en curso. Terminá el diálogo primero.");
                return;
            }

            _currentStory = new Story(inkJsonAsset.text);
            _variableStorage.BindToStory(_currentStory);

            Debug.Log($"[DialogueRunner] Historia cargada: {inkJsonAsset.name}");
        }

        /// <summary>
        /// Salta a un knot específico e inicia el diálogo desde ahí.
        /// Requiere que LoadStory() haya sido llamado antes.
        /// Llamado por InteractionSystem vía OnKnotRequested.
        /// </summary>
        public void StartDialogueAtKnot(string knot)
        {
            if (_currentStory == null)
            {
                Debug.LogError(
                    "[DialogueRunner] No hay historia cargada. " +
                    "Llamá LoadStory() antes de StartDialogueAtKnot().");
                return;
            }

            if (_isRunning)
            {
                Debug.LogWarning(
                    "[DialogueRunner] Se intentó iniciar un diálogo mientras " +
                    "otro estaba en curso. Ignorado.");
                return;
            }

            if (string.IsNullOrEmpty(knot))
            {
                Debug.LogError("[DialogueRunner] Knot vacío o null.");
                return;
            }

            // Verifica que el knot existe en la historia antes de saltar      // evita crash silencioso de Ink
            if (!_currentStory.mainContentContainer.namedContent.ContainsKey(knot))
            {
                Debug.LogError(
                    $"[DialogueRunner] El knot '{knot}' no existe en la historia cargada. " +
                    $"Verificá el nombre en el archivo .ink.");
                return;
            }

            _currentStory.ChoosePathString(knot);
            _isRunning = true;

            Continue();
        }


        /// <summary>
        /// Mantiene compatibilidad con el sistema anterior.
        /// Carga Y arranca desde el principio de la historia.
        /// Usar solo si la historia no tiene knots (diálogo lineal completo).
        /// </summary>
        public void StartDialogue(TextAsset inkJsonAsset)
        {
            LoadStory(inkJsonAsset);

            if (_currentStory == null) return;

            _isRunning = true;
            Continue();
        }

        // Avanza al siguiente contenido de la Story.
        // Llamar desde la UI cuando el jugador confirma una línea.
        public void Continue()
        {
            if (!_isRunning || _currentStory == null) return;

            // Ink puede tener contenido listo para continuar
            // o puede estar esperando una elección del jugador.
            if (_currentStory.canContinue)
            {
                string rawLine = _currentStory.Continue();

                // Procesamos tags SIEMPRE, incluso en líneas vacías
                if (_currentStory.currentTags?.Count > 0)
                    OnTagsReady?.Invoke(_currentStory.currentTags);

                // Ink puede devolver líneas vacías entre bloques.
                // Las saltamos automáticamente.
                // 🟡 PERF — IsNullOrWhiteSpace no genera GC en .NET moderno.
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    Continue();
                    return;
                }

                if (_currentStory == null) return;

                OnLineReady?.Invoke(DialogueLine.Parse(rawLine));

            }
            else if (_currentStory.currentChoices.Count > 0)
            {
                // Ya estamos en un punto de elección.
                // La UI debería estar mostrando las opciones.
                // No hacemos nada hasta que el jugador elija.
                OnChoicesReady?.Invoke(_currentStory.currentChoices);
            }
            else
            {
                // Sin contenido y sin opciones: la story terminó.
                EndDialogue();
            }
        }

        // El jugador eligió una opción. Index corresponde a la
        // posición en la lista de currentChoices de Ink.
        public void ChooseOption(int choiceIndex)
        {
            if (!_isRunning || _currentStory == null) return;

            if (choiceIndex < 0 ||
                choiceIndex >= _currentStory.currentChoices.Count)
            {
                Debug.LogError(
                    $"[DialogueRunner] ChoiceIndex {choiceIndex} fuera " +
                    $"de rango. Opciones disponibles: " +
                    $"{_currentStory.currentChoices.Count}");
                return;
            }

            _currentStory.ChooseChoiceIndex(choiceIndex);
            Continue();
        }

        // ─── Privados ─────────────────────────────────────────────────

        private void EndDialogue()
        {
            _isRunning = false;
            OnDialogueEnded?.Invoke();
        }
    }
}
