using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Journey.Core;
using Journey.Data;

namespace Journey.Presentation
{
    /// <summary>
    /// Tablero de mensajes de PreProducción para una fuente específica.
    /// Cada fuente del escritorio (computadora, celular, notas, contestador)
    /// tiene su propia instancia con su _sourceType configurado.
    /// Solo muestra mensajes del tipo que le corresponde.          // 📌 GDD — computadora=Email, celular=SMS, notas=Written, contestador=Call
    /// Escucha OnMessagesLoaded y genera una tarjeta por mensaje filtrado.
    /// Escucha OnPhaseChanged para mostrarse/ocultarse según la fase.
    /// Orquesta el flujo de decodificación: recibe la solicitud de la
    /// tarjeta, lanza el minijuego, y devuelve el resultado a la tarjeta.
    /// No toma decisiones narrativas — delega todo al JourneyManager.
    /// </summary>
    public sealed class MessageBoardUI : MonoBehaviour
    {
        // ── Dependencias ──────────────────────────────────────────
        [Header("Sistemas")]
        [SerializeField] private JourneyManager _journeyManager;
        [SerializeField] private AirTimeManager _airTimeManager;

        [Header("Fuente")]
        [Tooltip("Tipo de mensaje que muestra esta instancia. " +
                 "Computadora=Email | Celular=SMS | Notas=Written | Contestador=Call")]
        [SerializeField] private MessageType _sourceType = MessageType.Email; // 📌 GDD — cada objeto del escritorio filtra su tipo

        [Header("UI")]
        [SerializeField] private GameObject    _boardPanel;
        [SerializeField] private Transform     _cardsContainer;
        [SerializeField] private MessageCardUI _cardPrefab;

        // ── Pool ──────────────────────────────────────────────────
        // 🟢 POOL — las tarjetas se crean en PreProducción y se
        // devuelven al avanzar de fase. En steady state: zero allocs.
        private ObjectPool<MessageCardUI>    _cardPool;
        private readonly List<MessageCardUI> _activeCards = new();

        // ── Lifecycle ─────────────────────────────────────────────
        private void Awake()
        {
            ValidateDependencies();
            InitializePool();
            _boardPanel.SetActive(false);
        }

        private void OnEnable()
        {
            _journeyManager.OnMessagesLoaded += HandleMessagesLoaded;
            _journeyManager.OnPhaseChanged   += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            _journeyManager.OnMessagesLoaded -= HandleMessagesLoaded;
            _journeyManager.OnPhaseChanged   -= HandlePhaseChanged;
        }

        // ── Handlers ─────────────────────────────────────────────

        private void HandleMessagesLoaded(IReadOnlyList<MessageDataSO> messages)
        {
            ReturnAllCardsToPool();

            foreach (var message in messages)
            {
                if (message == null) continue;

                // Cada instancia de MessageBoardUI solo muestra su tipo.
                // La computadora no sabe que existen SMS, ni el celular que existen emails.
                if (message.Type != _sourceType) continue;          // 📌 GDD — filtro por fuente física del mensaje

                // Capturamos la referencia para el closure.             // 📌 sin esto todos los botones capturan el último mensaje del loop
                var captured = message;

                MessageCardUI card = _cardPool.Get();
                card.transform.SetParent(_cardsContainer, false);

                // Capturamos también la referencia a la tarjeta para
                // poder llamar SetDecoded() cuando el minijuego termine.
                var capturedCard = card;

                card.Initialize(
                    sender:           captured.Sender,
                    recipient:        captured.Recipient,
                    body:             captured.RawText,
                    messageType:      captured.Type,
                    requiresDecoding: captured.RequiresDecoding,
                    onRead:           () => _journeyManager.ReadMessage(captured),
                    onSkip:           () => _journeyManager.SkipMessage(captured),
                    onDecodeRequested: type => HandleDecodeRequested(type, capturedCard)
                );

                // 🔴 FIX: Sincronizamos el estado actual apenas nace la tarjeta para evitar que se pueda leer al aire sin tiempo
                if (!_airTimeManager.HasAvailableSlots)
                {
                    card.LockReadBecauseNoTime();
                }

                _activeCards.Add(card);
            }
        }

        private void HandlePhaseChanged(JourneyPhase phase)
        {
            // El tablero solo es visible en PreProducción.              // 📌 GDD — en OnAir y Closing el foco es la consola
            bool isPreProduction = phase == JourneyPhase.PreProduction;
            _boardPanel.SetActive(isPreProduction);

            if (!isPreProduction)
                ReturnAllCardsToPool();
        }

        /// <summary>
        /// Lanza el minijuego de decodificación correspondiente al tipo de mensaje.
        /// Hoy SMS y Written usan el mismo minijuego.
        /// Cuando existan minijuegos distintos por tipo, este método rutea según messageType
        /// sin tocar MessageCardUI.                              // 📌 GDD — preparado para extensión futura
        /// </summary>
        private void HandleDecodeRequested(MessageType messageType, MessageCardUI card)
        {
            // 🟡 PERF — stub: cuando el minijuego esté implementado,
            // esta llamada se reemplaza por la apertura real del minijuego
            // y SetDecoded() se llama en su callback de completado.

            // Por ahora simulamos una decodificación exitosa para poder
            // testear el flujo completo sin el minijuego implementado.
            // TODO P10 — reemplazar por llamada al DecodingMinigame real.
            Debug.Log($"[MessageBoardUI] Decodificación solicitada. Tipo: {messageType}. Stub activo.");
            card.SetDecoded("[TEXTO DECODIFICADO — stub P10]");
        }

        // ── Pool ──────────────────────────────────────────────────
        private void InitializePool()
        {
            _cardPool = new ObjectPool<MessageCardUI>(
                createFunc:      CreateCard,
                actionOnGet:     card =>
                {
                    card.Reset();                                // fix CR-2 — limpia estado sucio antes de reutilizar
                    card.gameObject.SetActive(true);
                },
                actionOnRelease: card => card.gameObject.SetActive(false),
                actionOnDestroy: card =>
                {
                    if (card != null) Destroy(card.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 8,    // 📌 GDD — máximo de mensajes por jornada estimado
                maxSize:         16
            );
        }

        private MessageCardUI CreateCard()
        {
            var card = Instantiate(_cardPrefab, _cardsContainer);
            card.gameObject.SetActive(false);
            return card;
        }

        private void ReturnAllCardsToPool()
        {
            foreach (var card in _activeCards)
                _cardPool.Release(card);

            _activeCards.Clear(); // 🟡 PERF — Clear() no genera GC en List<T>
        }

        // ── Validación ────────────────────────────────────────────
        private void ValidateDependencies()
        {
            if (_journeyManager == null)
                Debug.LogError("[MessageBoardUI] JourneyManager no asignado.", this);

            if (_boardPanel == null)
                Debug.LogError("[MessageBoardUI] BoardPanel no asignado.", this);

            if (_cardsContainer == null)
                Debug.LogError("[MessageBoardUI] CardsContainer no asignado.", this);

            if (_cardPrefab == null)
                Debug.LogError("[MessageBoardUI] CardPrefab no asignado.", this);

            if (_airTimeManager == null)
                Debug.LogError("[MessageBoardUI] AirTimeManager no asignado.", this);
        }
    }
}