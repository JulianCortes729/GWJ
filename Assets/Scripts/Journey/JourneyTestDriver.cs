using UnityEngine;
using Journey.Core;
using Journey.Data;

namespace Journey.tests
{
    /// <summary>
    /// Test driver temporal para verificar P7 sin UI ni assets visuales.
    /// Sacar antes del build de producción.
    /// Todos los métodos se ejecutan desde el Inspector → click derecho
    /// sobre el componente → nombre del método.
    /// </summary>
    public sealed class JourneyTestDriver : MonoBehaviour
    {
        [Header("Referencia al sistema")]
        [SerializeField] private JourneyManager _journeyManager;

        [Header("Resultados (solo lectura)")]
        [SerializeField] private string _currentPhase    = "—";
        [SerializeField] private string _currentJourney  = "—";
        [SerializeField] private int    _messageCount    = 0;
        [SerializeField] private string _lastEvent       = "—";

        // ── Lifecycle ─────────────────────────────────────────────

        private void OnEnable()
        {
            if (_journeyManager == null) return;

            _journeyManager.OnPhaseChanged     += HandlePhaseChanged;
            _journeyManager.OnMessagesLoaded   += HandleMessagesLoaded;
            _journeyManager.OnMessageRead      += HandleMessageRead;
            _journeyManager.OnMessageSkipped   += HandleMessageSkipped;
            _journeyManager.OnJourneyCompleted += HandleJourneyCompleted;
        }

        private void OnDisable()
        {
            if (_journeyManager == null) return;

            _journeyManager.OnPhaseChanged     -= HandlePhaseChanged;
            _journeyManager.OnMessagesLoaded   -= HandleMessagesLoaded;
            _journeyManager.OnMessageRead      -= HandleMessageRead;
            _journeyManager.OnMessageSkipped   -= HandleMessageSkipped;
            _journeyManager.OnJourneyCompleted -= HandleJourneyCompleted;
        }

        // ── Comandos de test ──────────────────────────────────────

        [ContextMenu("TEST 1 — Nueva partida")]
        private void Test_StartNewGame()
        {
            Log("► StartNewGame()");
            _journeyManager.StartNewGame();
        }

        [ContextMenu("TEST 2 — Cargar partida guardada")]
        private void Test_LoadGame()
        {
            Log("► LoadGame()");
            _journeyManager.LoadGame();
        }

        [ContextMenu("TEST 3 — Avanzar fase")]
        private void Test_AdvancePhase()
        {
            Log("► AdvancePhase()");
            _journeyManager.AdvancePhase();
        }

        [ContextMenu("TEST 4 — Leer primer mensaje")]
        private void Test_ReadFirstMessage()
        {
            var messages = _journeyManager.Messages;
            if (messages == null || messages.Count == 0)
            {
                Log("✗ No hay mensajes disponibles. ¿Arrancaste la jornada?");
                return;
            }

            Log($"► ReadMessage({messages[0].name})");
            _journeyManager.ReadMessage(messages[0]);
        }

        [ContextMenu("TEST 5 — Omitir primer mensaje")]
        private void Test_SkipFirstMessage()
        {
            var messages = _journeyManager.Messages;
            if (messages == null || messages.Count == 0)
            {
                Log("✗ No hay mensajes disponibles.");
                return;
            }

            Log($"► SkipMessage({messages[0].name})");
            _journeyManager.SkipMessage(messages[0]);
        }

        [ContextMenu("TEST 6 — Marcar celular cargado")]
        private void Test_ChargePhone()
        {
            Log("► SetPhoneCharged(true)");
            _journeyManager.SetPhoneCharged(true);
        }

        [ContextMenu("TEST 7 — Guardar progreso manualmente")]
        private void Test_SaveProgress()
        {
            Log("► SaveProgress()");
            _journeyManager.SaveProgress();
        }

        [ContextMenu("TEST 8 — Flujo completo de jornada (automatizado)")]
        private void Test_FullJourneyFlow()
        {
            Log("═══ FLUJO COMPLETO ═══");
            Log("► StartNewGame()");
            _journeyManager.StartNewGame();
            // PreProduction se activa automáticamente

            Log("► ReadMessage(0) — si hay mensajes");
            var messages = _journeyManager.Messages;
            if (messages.Count > 0)
                _journeyManager.ReadMessage(messages[0]);

            Log("► AdvancePhase() → OnAir");
            _journeyManager.AdvancePhase();

            Log("► AdvancePhase() → Closing");
            _journeyManager.AdvancePhase();

            Log("► SetPhoneCharged(true)");
            _journeyManager.SetPhoneCharged(true);

            Log("► AdvancePhase() → Ended (guarda y avanza jornada)");
            _journeyManager.AdvancePhase();

            Log("═══ FIN FLUJO ═══");
        }

        [ContextMenu("TEST 9 — Verificar archivo de guardado")]
        private void Test_CheckSaveFile()
        {
            bool exists = SaveSystem.HasSave();
            Log(exists
                ? $"✓ Archivo de guardado existe."
                : "✗ No hay archivo de guardado.");

            if (exists)
            {
                var data = SaveSystem.Load();
                Log($"  Jornada guardada: {data.CurrentJourneyNumber}");
                Log($"  Fase guardada:    {(JourneyPhase)data.CurrentPhase}");
                Log($"  Mensajes leídos:  {data.ReadMessageNames.Count}");
                Log($"  Celular cargado:  {data.PhoneChargedLastNight}");
            }
        }

        [ContextMenu("TEST 10 — Borrar guardado")]
        private void Test_DeleteSave()
        {
            Log("► SaveSystem.Delete()");
            SaveSystem.Delete();
            Log("✓ Guardado borrado.");
        }

        // ── Handlers de eventos ───────────────────────────────────

        private void HandlePhaseChanged(JourneyPhase phase)
        {
            _currentPhase = phase.ToString();
            Log($"✓ OnPhaseChanged → {phase}");
        }

        private void HandleMessagesLoaded(System.Collections.Generic.IReadOnlyList<MessageDataSO> messages)
        {
            _messageCount = messages.Count;
            Log($"✓ OnMessagesLoaded → {messages.Count} mensaje(s)");
            for (int i = 0; i < messages.Count; i++)
                Log($"   [{i}] {messages[i].name} — {messages[i].Sender} → {messages[i].Recipient}");
        }

        private void HandleMessageRead(MessageDataSO message)
        {
            Log($"✓ OnMessageRead → {message.name} | crítico: {message.IsCritical}");
        }

        private void HandleMessageSkipped(MessageDataSO message)
        {
            Log($"✓ OnMessageSkipped → {message.name} | crítico: {message.IsCritical}");
        }

        private void HandleJourneyCompleted(int journeyNumber)
        {
            Log($"✓ OnJourneyCompleted → Jornada {journeyNumber} completada");
            _currentJourney = $"Jornada {journeyNumber} completada";
        }

        // ── Helper ────────────────────────────────────────────────

        private void Log(string message)
        {
            _lastEvent = message;
            UnityEngine.Debug.Log($"[JourneyTestDriver] {message}");
        }
    }
}