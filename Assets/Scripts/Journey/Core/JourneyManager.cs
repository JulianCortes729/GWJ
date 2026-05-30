using System;
using System.Collections.Generic;
using UnityEngine;
using Journey.Data;
using Dialogue.Core;
using Dialogue.Data;

namespace Journey.Core
{
    /// <summary>
    /// Orquestador central de una jornada.
    /// Responsabilidad: transitar entre fases, notificar al resto
    /// del juego, y coordinar guardado/carga de progreso.
    /// No sabe nada de UI ni de cómo se ven las fases.
    /// </summary>
    public sealed class JourneyManager : MonoBehaviour
    {
        // ── Dependencias ──────────────────────────────────────────
        [Header("Datos de jornadas")]
        [Tooltip("Todas las jornadas del juego en orden. Índice 0 = Jornada 1.")]
        [SerializeField] private JourneyDataSO[] _journeys = Array.Empty<JourneyDataSO>();

        [Header("Sistemas")]
        [SerializeField] private DialogueRunner    _dialogueRunner;
        [SerializeField] private DialogueStorageSO _dialogueStorage;

        // ── Eventos públicos ──────────────────────────────────────
        // Los sistemas de UI escuchan estos eventos.             // ⚠️ SOLID: OCP — agregar listener = cero cambios acá

        /// <summary>Dispara al entrar a cualquier fase nueva.</summary>
        public event Action<JourneyPhase> OnPhaseChanged;

        /// <summary>Dispara al cargar la lista de mensajes de PreProducción.</summary>
        public event Action<IReadOnlyList<MessageDataSO>> OnMessagesLoaded;

        /// <summary>Dispara cuando un mensaje es marcado como leído al aire.</summary>
        public event Action<MessageDataSO> OnMessageRead;

        /// <summary>Dispara cuando un mensaje es omitido.</summary>
        public event Action<MessageDataSO> OnMessageSkipped;

        /// <summary>Dispara al completar y guardar una jornada.</summary>
        public event Action<int> OnJourneyCompleted;   // int = número de jornada completada

        // ── Estado interno ────────────────────────────────────────
        private JourneyPhase    _currentPhase    = JourneyPhase.None;
        private JourneyDataSO   _currentJourney  = null;
        private JourneySaveData _saveData        = null;

        private readonly List<MessageDataSO> _availableMessages  = new();
        private readonly HashSet<string>     _readMessageNames   = new();   // 🟢 HashSet — Contains() O(1)
        private readonly HashSet<string>     _skippedMessageNames = new();

        // ── Propiedades públicas ──────────────────────────────────
        public JourneyPhase           CurrentPhase     => _currentPhase;
        public JourneyDataSO          CurrentJourney   => _currentJourney;
        public IReadOnlyList<MessageDataSO> Messages   => _availableMessages;
        public bool                   HasSave          => SaveSystem.HasSave();

        // ── Lifecycle ─────────────────────────────────────────────
        private void Awake()
        {
            ValidateDependencies();
        }

        // ── API pública ───────────────────────────────────────────

        /// <summary>
        /// Inicia una partida nueva desde la Jornada 1.
        /// Borra cualquier guardado existente.
        /// </summary>
        public void StartNewGame()
        {
            SaveSystem.Delete();
            _saveData = new JourneySaveData();
            LoadJourney(0);
            EnterPhase(JourneyPhase.PreProduction); // ← esta línea faltaba

        }

        /// <summary>
        /// Carga la partida guardada y reanuda donde quedó.
        /// </summary>
        public void LoadGame()
        {
            _saveData = SaveSystem.Load() ?? new JourneySaveData();

            RestoreDialogueStorage();

            int journeyIndex = Mathf.Clamp(_saveData.CurrentJourneyNumber - 1, 0, _journeys.Length - 1);
            

            // Si la jornada guardada ya terminó, avanzamos a la siguiente
            var savedPhase = (JourneyPhase)_saveData.CurrentPhase;
            if (savedPhase == JourneyPhase.Ended)
            {
                journeyIndex = Mathf.Clamp(journeyIndex + 1, 0, _journeys.Length - 1);
                savedPhase   = JourneyPhase.PreProduction;
                _saveData.CurrentPhase = (int)JourneyPhase.PreProduction;
            }

            LoadJourney(journeyIndex);
            EnterPhase(savedPhase);

        }

        /// <summary>
        /// Avanza a la siguiente fase de la jornada actual.
        /// PreProduction → OnAir → Closing → Ended.
        /// </summary>
        public void AdvancePhase()
        {
            var next = _currentPhase switch
            {
                JourneyPhase.None          => JourneyPhase.PreProduction,
                JourneyPhase.PreProduction => JourneyPhase.OnAir,
                JourneyPhase.OnAir         => JourneyPhase.Closing,
                JourneyPhase.Closing       => JourneyPhase.Ended,
                _                          => JourneyPhase.Ended
            };

            EnterPhase(next);
        }

        /// <summary>
        /// Marca un mensaje como leído al aire y dispara su knot de Ink.
        /// </summary>
        public void ReadMessage(MessageDataSO message)
        {
            if (message == null) return;
            if (_readMessageNames.Contains(message.name)) return;   // ya fue leído

            _readMessageNames.Add(message.name);
            _saveData.ReadMessageNames.Add(message.name);

            OnMessageRead?.Invoke(message);

            if (!string.IsNullOrEmpty(message.OnAirKnot))
                _dialogueRunner.StartDialogueAtKnot(message.OnAirKnot);
        }

        /// <summary>
        /// Marca un mensaje como omitido y dispara su knot de Ink si tiene uno.
        /// </summary>
        public void SkipMessage(MessageDataSO message)
        {
            if (message == null) return;
            if (_skippedMessageNames.Contains(message.name)) return;

            _skippedMessageNames.Add(message.name);
            _saveData.SkippedMessageNames.Add(message.name);

            OnMessageSkipped?.Invoke(message);

            if (!string.IsNullOrEmpty(message.OnSkippedKnot))
                _dialogueRunner.StartDialogueAtKnot(message.OnSkippedKnot);
        }

        /// <summary>
        /// Registra si el celular fue cargado al cierre de esta jornada.
        /// Crítico para el Final Verdadero.
        /// </summary>
        public void SetPhoneCharged(bool charged)
        {
            _saveData.PhoneChargedLastNight = charged;
        }

        /// <summary>
        /// Guarda el estado completo en disco.
        /// Llamar al final de Closing antes de avanzar de jornada.
        /// </summary>
        public void SaveProgress()
        {
            if (_saveData == null) return;

            _saveData.CurrentJourneyNumber = _currentJourney?.JourneyNumber ?? 1;
            _saveData.CurrentPhase         = (int)_currentPhase;

            SerializeDialogueStorage();
            SerializeInkState();

            SaveSystem.Save(_saveData);
        }

        // ── Privados — Navegación ─────────────────────────────────

        private void LoadJourney(int index)
        {
            if (index < 0 || index >= _journeys.Length)
            {
                Debug.LogError($"[JourneyManager] Índice de jornada {index} fuera de rango.");
                return;
            }

            _currentJourney = _journeys[index];

            _availableMessages.Clear();
            _readMessageNames.Clear();
            _skippedMessageNames.Clear();

            // Restauramos los mensajes ya leídos/omitidos si venimos de un save
            foreach (var msg in _currentJourney.Messages)
            {
                if (msg == null) continue;
                _availableMessages.Add(msg);
            }

            if (_currentJourney.InkAsset != null)
                _dialogueRunner.LoadStory(_currentJourney.InkAsset);

            // Restauramos estado de Ink si viene de save
            RestoreInkState();
        }

        private void EnterPhase(JourneyPhase phase)
        {
            _currentPhase = phase;
            _saveData.CurrentPhase = (int)phase;

            Debug.Log($"[JourneyManager] Fase: {phase}");
            OnPhaseChanged?.Invoke(phase);

            switch (phase)
            {
                case JourneyPhase.PreProduction:
                    OnMessagesLoaded?.Invoke(_availableMessages);
                    TryStartKnot(_currentJourney?.PreProductionKnot);
                    break;

                case JourneyPhase.OnAir:
                    TryStartKnot(_currentJourney?.OnAirKnot);
                    break;

                case JourneyPhase.Closing:
                    TryStartKnot(_currentJourney?.ClosingKnot);
                    break;

                case JourneyPhase.Ended:
                    HandleJourneyEnded();
                    break;
            }
        }

        private void HandleJourneyEnded()
        {
            // Disparamos consecuencias de mensajes críticos omitidos          // 📌 GDD — afecta el Final Verdadero
            foreach (var msg in _availableMessages)
            {
                if (!msg.IsCritical) continue;
                if (_readMessageNames.Contains(msg.name)) continue;
                if (_skippedMessageNames.Contains(msg.name)) continue;

                // Mensaje crítico que no fue procesado — lo marcamos como omitido
                SkipMessage(msg);
            }

            SaveProgress();

            int completed = _currentJourney?.JourneyNumber ?? 0;
            OnJourneyCompleted?.Invoke(completed);

            // Avanzamos a la siguiente jornada si existe
            int nextIndex = completed; // JourneyNumber es 1-based, índice es 0-based
            if (nextIndex < _journeys.Length)
                LoadJourney(nextIndex);
            else
                Debug.Log("[JourneyManager] Juego completado. No hay más jornadas.");
        }

        private void TryStartKnot(string knot)
        {
            if (string.IsNullOrEmpty(knot)) return;
            if (_dialogueRunner == null) return;
            _dialogueRunner.StartDialogueAtKnot(knot);
        }

        // ── Privados — Serialización ──────────────────────────────

        private void SerializeDialogueStorage()
        {
            // 📌 GDD — persistimos solo las variables que Ink necesita
            // recordar entre sesiones (ej: ya_conocio_a_belisario).
            // En Editor estas están en los DebugBools/Floats/Strings.
#if UNITY_EDITOR
            _saveData.SavedBools.Clear();
            _saveData.SavedFloats.Clear();
            _saveData.SavedStrings.Clear();

            foreach (var kvp in _dialogueStorage.DebugBools)
                _saveData.SavedBools.Add(new SavedBool { Key = kvp.Key, Value = kvp.Value });

            foreach (var kvp in _dialogueStorage.DebugFloats)
                _saveData.SavedFloats.Add(new SavedFloat { Key = kvp.Key, Value = kvp.Value });

            foreach (var kvp in _dialogueStorage.DebugStrings)
                _saveData.SavedStrings.Add(new SavedString { Key = kvp.Key, Value = kvp.Value });
#endif
        }

        private void RestoreDialogueStorage()
        {
            if (_saveData == null) return;

            _dialogueStorage.ResetAll();

            foreach (var s in _saveData.SavedBools)   _dialogueStorage.SetBool(s.Key, s.Value);
            foreach (var s in _saveData.SavedFloats)  _dialogueStorage.SetFloat(s.Key, s.Value);
            foreach (var s in _saveData.SavedStrings) _dialogueStorage.SetString(s.Key, s.Value);
        }

        private void SerializeInkState()
        {
            // Guardamos el estado completo de Ink para poder retomarlo.
            // story.state.ToJson() captura posición en el flujo, variables,
            // y el stack de call frames de Ink.                            // ⚠️ RIESGO — ver nota en Fase 0
            // Por ahora dejamos esto para cuando el DialogueRunner
            // exponga acceso al state de la Story.
            // 📌 GDD — implementar en iteración siguiente si el scope lo permite
        }

        private void RestoreInkState()
        {
            if (string.IsNullOrEmpty(_saveData?.InkStateJson)) return;
            // 📌 GDD — implementar junto con SerializeInkState
        }

        // ── Validación ────────────────────────────────────────────
        private void ValidateDependencies()
        {
            if (_journeys == null || _journeys.Length == 0)
                Debug.LogError("[JourneyManager] No hay jornadas asignadas.", this);

            if (_dialogueRunner == null)
                Debug.LogError("[JourneyManager] DialogueRunner no asignado.", this);

            if (_dialogueStorage == null)
                Debug.LogError("[JourneyManager] DialogueStorage no asignado.", this);
        }
    }
}
