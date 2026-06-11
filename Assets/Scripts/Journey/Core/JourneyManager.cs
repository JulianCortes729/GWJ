using System;
using System.Collections.Generic;
using UnityEngine;
using Journey.Data;
using Dialogue.Core;
using Dialogue.Data;

namespace Journey.Core
{
    public sealed class JourneyManager : MonoBehaviour
    {
        // ── Dependencias ──────────────────────────────────────────
        [Header("Datos de jornadas")]
        [SerializeField] private JourneyDataSO[] _journeys = Array.Empty<JourneyDataSO>();

        [Header("Sistemas")]
        [SerializeField] private DialogueRunner    _dialogueRunner;
        [SerializeField] private DialogueStorageSO _dialogueStorage;
        [SerializeField] private AirTimeManager    _airTimeManager;

        // ── Eventos públicos ──────────────────────────────────────
        public event Action<JourneyPhase>                    OnPhaseChanged;
        public event Action<IReadOnlyList<MessageDataSO>>    OnMessagesLoaded;
        public event Action<MessageDataSO>                   OnMessageRead;
        public event Action<MessageDataSO>                   OnMessageSkipped;
        public event Action<int>                             OnJourneyCompleted;

        // ── Estado interno ────────────────────────────────────────
        private JourneyPhase    _currentPhase   = JourneyPhase.None;
        private JourneyDataSO   _currentJourney = null;
        private JourneySaveData _saveData       = null;

        private readonly List<MessageDataSO> _availableMessages   = new();
        private readonly HashSet<string>     _readMessageNames    = new();
        private readonly HashSet<string>     _skippedMessageNames = new();

        // ── Propiedades públicas ──────────────────────────────────
        public JourneyPhase                 CurrentPhase   => _currentPhase;
        public JourneyDataSO                CurrentJourney => _currentJourney;
        public IReadOnlyList<MessageDataSO> Messages       => _availableMessages;
        public bool                         HasSave        => SaveSystem.HasSave();

        // ── Lifecycle ─────────────────────────────────────────────
        private void Awake()
        {
            ValidateDependencies();
        }

        private void Start()
        {
            // Start() garantiza que DialogueSystemBootstrap.Start() ya registró
            // el InkHandler antes de que LoadJourney() llame LoadStory().      // 📌 orden de lifecycle: todos los Start() corren después de todos los Awake()
            if (SaveSystem.HasSave())
                LoadGame();
            else
                StartNewGame();
        }

        // ── API pública ───────────────────────────────────────────
        public void StartNewGame()
        {
            SaveSystem.Delete();
            _saveData = new JourneySaveData();
            LoadJourney(0);
            EnterPhase(JourneyPhase.PreProduction);
        }

        public void LoadGame()
        {
            _saveData = SaveSystem.Load() ?? new JourneySaveData();
            RestoreDialogueStorage();

            int journeyIndex = Mathf.Clamp(
                _saveData.CurrentJourneyNumber - 1, 0, _journeys.Length - 1);

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

        public void ReadMessage(MessageDataSO message)
        {
            if (message == null) return;
            if (_readMessageNames.Contains(message.name)) return;

            _readMessageNames.Add(message.name);
            _saveData.ReadMessageNames.Add(message.name);
            OnMessageRead?.Invoke(message);

            if (!string.IsNullOrEmpty(message.OnAirKnot))
                _dialogueRunner.StartDialogueAtKnot(message.OnAirKnot);
        }

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

        public void SetPhoneCharged(bool charged)
        {
            _saveData.PhoneChargedLastNight = charged;
        }

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
                Debug.LogError($"[JourneyManager] Índice {index} fuera de rango.");
                return;
            }

            _currentJourney = _journeys[index];
            _availableMessages.Clear();
            _readMessageNames.Clear();
            _skippedMessageNames.Clear();

            foreach (var msg in _currentJourney.Messages)
            {
                if (msg != null) _availableMessages.Add(msg);
            }

            // JourneyManager es el único responsable de cargar la historia.
            // DialogueSystemBootstrap NO llama LoadStory().               // 📌 ownership claro — evita race condition en Start()
            if (_currentJourney.InkAsset != null)
                _dialogueRunner.LoadStory(_currentJourney.InkAsset);
            else
                Debug.LogWarning($"[JourneyManager] Jornada '{_currentJourney.name}' no tiene InkAsset asignado.");

            RestoreInkState();
        }

        private void EnterPhase(JourneyPhase phase)
        {
            _currentPhase          = phase;
            _saveData.CurrentPhase = (int)phase;

            Debug.Log($"[JourneyManager] Fase: {phase}");
            OnPhaseChanged?.Invoke(phase);

            switch (phase)
            {
                case JourneyPhase.PreProduction:
                    if (_airTimeManager != null && _currentJourney != null)
                        _airTimeManager.Initialize(_currentJourney.MaxAirTimeSlots);

                    OnMessagesLoaded?.Invoke(_availableMessages);

                    // LoadStory() ya corrió en LoadJourney() — la historia
                    // está lista cuando llegamos acá.                      // 📌 orden garantizado
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
            foreach (var msg in _availableMessages)
            {
                if (!msg.IsCritical) continue;
                if (_readMessageNames.Contains(msg.name)) continue;
                if (_skippedMessageNames.Contains(msg.name)) continue;
                SkipMessage(msg);
            }

            SaveProgress();

            int completed = _currentJourney?.JourneyNumber ?? 0;
            OnJourneyCompleted?.Invoke(completed);

            int nextIndex = completed;
            if (nextIndex < _journeys.Length)
                LoadJourney(nextIndex);
            else
                Debug.Log("[JourneyManager] Juego completado.");
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

        private void SerializeInkState()   { /* 📌 GDD — implementar con DialogueRunner.GetStoryState() */ }
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