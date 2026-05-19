using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;
using Dialogue.Data;

namespace Dialogue.Core
{
    // Sincroniza las variables de Ink con el DialogueStorageSO.
    // Es el único punto del proyecto que conoce tanto a Ink
    // como al SO. Nadie más necesita saber cómo funciona Ink por dentro.

    public sealed class DialogueVariableStorage
    {
        // ─── Dependencias ─────────────────────────────────────────────

        private readonly DialogueStorageSO _storage;
        private Story                      _currentStory;

        // ─── Constructor ──────────────────────────────────────────────

        public DialogueVariableStorage(DialogueStorageSO storage)
        {
            _storage = storage;
        }

        // ─── API pública ──────────────────────────────────────────────

        // Llamar cada vez que se inicia una nueva Story de Ink.
        // Suscribe el observer que mantiene el SO sincronizado.
        public void BindToStory(Story story)
        {
            // Si había una story anterior, limpiamos el observer.
            UnbindFromCurrentStory();

            _currentStory = story;

            // Ink llama a este delegate cada vez que una variable
            // cambia dentro del script .ink en ejecución.
            // 🔵 NET — En un futuro multijugador, aquí se enviarían
            // los deltas de variables al servidor.
            _currentStory.variablesState.variableChangedEvent
                += OnInkVariableChanged;
        }

        public void UnbindFromCurrentStory()
        {
            if (_currentStory == null) return;

            _currentStory.variablesState.variableChangedEvent
                -= OnInkVariableChanged;

            _currentStory = null;
        }

        // Escribe una variable en Ink Y en el SO.
        // Usar cuando el juego necesita modificar el estado
        // narrativo desde fuera del script .ink.
        // Ejemplo: el jugador recogió un ítem y eso desbloquea
        // una rama de diálogo.
        public void SetVariable(string variableName, object value)
        {
            if (_currentStory == null)
            {
                Debug.LogWarning(
                    $"[DialogueVariableStorage] SetVariable llamado sin " +
                    $"una Story activa. Variable '{variableName}' ignorada.");
                return;
            }

            _currentStory.variablesState[variableName] = value;
            SyncVariableToStorage(variableName, value);
        }

        // ─── Sincronización Ink → SO ──────────────────────────────────

        private void OnInkVariableChanged(string variableName, object newValue)
            => SyncVariableToStorage(variableName, newValue);

        private void SyncVariableToStorage(string variableName, object value)
        {
            // Ink usa su propio sistema de tipos internos.
            // Los mapeamos a los tipos de nuestro SO.
            // 🟡 PERF — Pattern matching con boxing, inevitable
            // por la API de Ink. No está en hot path.
            switch (value)
            {
                case bool boolValue:
                    _storage.SetBool(variableName, boolValue);
                    break;

                case float floatValue:
                    _storage.SetFloat(variableName, floatValue);
                    break;

                case int intValue:
                    // Ink puede devolver ints en algunos contextos.
                    // Los almacenamos como float para consistencia.
                    _storage.SetFloat(variableName, intValue);
                    break;

                case string stringValue:
                    _storage.SetString(variableName, stringValue);
                    break;

                default:
                    Debug.LogWarning(
                        $"[DialogueVariableStorage] Tipo no soportado " +
                        $"para variable '{variableName}': {value?.GetType()}");
                    break;
            }
        }
    }
}