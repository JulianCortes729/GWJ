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

        // Inicia una conversación desde un TextAsset .ink compilado a JSON.
        // inkJsonAsset es el archivo .ink.json que genera Ink al compilar.
        public void StartDialogue(TextAsset inkJsonAsset)
        {
            if (_isRunning)
            {
                Debug.LogWarning(
                    "[DialogueRunner] Se intentó iniciar un diálogo " +
                    "mientras otro estaba en curso. Ignorado.");
                return;
            }

            if (inkJsonAsset == null)
            {
                Debug.LogError(
                    "[DialogueRunner] inkJsonAsset es null. " +
                    "Asigná un archivo .ink.json válido.");
                return;
            }

            _currentStory = new Story(inkJsonAsset.text);
            _variableStorage.BindToStory(_currentStory);
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



                // Ink puede devolver líneas vacías entre bloques.
                // Las saltamos automáticamente.
                // 🟡 PERF — IsNullOrWhiteSpace no genera GC en .NET moderno.
                if (string.IsNullOrWhiteSpace(rawLine))
                {
                    Continue();
                    return;
                }

                OnLineReady?.Invoke(DialogueLine.Parse(rawLine));

                if (_currentStory == null) return;

                // Después de avanzar, Ink puede haber llegado
                // a un punto de elección. Lo notificamos de inmediato.
                //if (!_currentStory.canContinue && _currentStory.currentChoices.Count > 0)
                //   OnChoicesReady?.Invoke(_currentStory.currentChoices);
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
            _variableStorage.UnbindFromCurrentStory();
            _currentStory = null;
            OnDialogueEnded?.Invoke();
        }
    }
}
