using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Journey.Core;

namespace Journey.Presentation
{
    /// <summary>
    /// Tarjeta visual para un mensaje individual.
    /// Muestra remitente, destinatario y texto.
    /// Expone tres acciones: descifrar, leer al aire u omitir.
    /// No conoce JourneyManager ni el sistema de minijuegos —
    /// delega hacia arriba via callbacks.                        // ⚠️ SOLID: SRP — vista tonta, cero lógica de gameplay
    /// </summary>
    public sealed class MessageCardUI : MonoBehaviour
    {
        // ── UI ───────────────────────────────────────────────────
        [Header("Textos")]
        [SerializeField] private TextMeshProUGUI _senderText;
        [SerializeField] private TextMeshProUGUI _recipientText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private TextMeshProUGUI _typeLabel;

        [Header("Botones")]
        [SerializeField] private Button _readButton;
        [SerializeField] private Button _skipButton;
        [SerializeField] private Button _decodeButton;           // visible solo si requiresDecoding

        [Header("Visual de estado")]
        [SerializeField] private GameObject _processedOverlay;  // panel semitransparente al procesar
        [SerializeField] private GameObject _lockedOverlay;     // panel que indica "sin descifrar"

        // ── Estado ───────────────────────────────────────────────
        private MessageType _messageType;

        // ── Callbacks ────────────────────────────────────────────
        private Action               _onRead;
        private Action               _onSkip;
        private Action<MessageType>  _onDecodeRequested;         // 📌 GDD — recibe MessageType para futura extensión por tipo

        // ── Lifecycle ────────────────────────────────────────────
        private void Awake()
        {
            _readButton.onClick.AddListener(HandleReadClicked);
            _skipButton.onClick.AddListener(HandleSkipClicked);
            _decodeButton.onClick.AddListener(HandleDecodeClicked);
        }

        private void OnDestroy()
        {
            _readButton.onClick.RemoveAllListeners();
            _skipButton.onClick.RemoveAllListeners();
            _decodeButton.onClick.RemoveAllListeners();
        }

        // ── API pública ───────────────────────────────────────────

        /// <summary>
        /// Inicializa la tarjeta con los datos del mensaje y los callbacks.
        /// Llamar después de obtenerla del pool — siempre antes de activarla.
        /// </summary>
        public void Initialize(
            string              sender,
            string              recipient,
            string              body,
            MessageType         messageType,
            bool                requiresDecoding,
            Action              onRead,
            Action              onSkip,
            Action<MessageType> onDecodeRequested)
        {
            if (_senderText    != null) _senderText.text    = sender;
            if (_recipientText != null) _recipientText.text = recipient;
            if (_typeLabel     != null) _typeLabel.text     = messageType.ToString();

            _messageType        = messageType;
            _onRead             = onRead;
            _onSkip             = onSkip;
            _onDecodeRequested  = onDecodeRequested;

            if (requiresDecoding)
            {
                // Mensaje bloqueado: mostramos el texto crudo opaco,
                // los botones de acción deshabilitados, y el botón de descifrar.
                // 📌 GDD — el jugador no puede leer al aire sin descifrar antes
                if (_bodyText != null) _bodyText.text = body;  // texto crudo visible pero bloqueado visualmente

                _readButton.interactable   = false;
                _skipButton.interactable   = false;
                _decodeButton.gameObject.SetActive(true);

                if (_lockedOverlay  != null) _lockedOverlay.SetActive(true);
            }
            else
            {
                // Mensaje limpio: texto visible, botones de acción habilitados.
                if (_bodyText != null) _bodyText.text = body;

                _readButton.interactable   = true;
                _skipButton.interactable   = true;
                _decodeButton.gameObject.SetActive(false);

                if (_lockedOverlay  != null) _lockedOverlay.SetActive(false);
            }
        }

        /// <summary>
        /// Llamado por MessageBoardUI cuando el minijuego de decodificación termina.
        /// Recibe el texto resultante — puede ser el correcto o uno con errores
        /// si el jugador decodificó mal.                         // 📌 GDD — errar en la deducción hace que el mensaje salga mal al aire
        /// </summary>
        public void SetDecoded(string decodedText)
        {
            if (_bodyText != null) _bodyText.text = decodedText;

            _readButton.interactable  = true;
            _skipButton.interactable  = true;
            _decodeButton.gameObject.SetActive(false);

            if (_lockedOverlay != null) _lockedOverlay.SetActive(false);
        }

        /// <summary>
        /// Restaura la tarjeta a su estado inicial limpio.
        /// Llamar desde el pool en actionOnGet antes de re-inicializar.   // 📌 fix CR-2 — el pool ensuciaba el estado entre usos
        /// </summary>
        public void Reset()
        {
            _readButton.interactable  = true;
            _skipButton.interactable  = true;
            _decodeButton.gameObject.SetActive(false);

            if (_processedOverlay != null) _processedOverlay.SetActive(false);
            if (_lockedOverlay    != null) _lockedOverlay.SetActive(false);

            if (_bodyText      != null) _bodyText.text      = string.Empty;
            if (_senderText    != null) _senderText.text    = string.Empty;
            if (_recipientText != null) _recipientText.text = string.Empty;
            if (_typeLabel     != null) _typeLabel.text     = string.Empty;

            _onRead            = null;
            _onSkip            = null;
            _onDecodeRequested = null;
        }

        /// <summary>
        /// Bloquea la lectura porque no hay slots de air time disponibles.
        /// Se llama desde MessageBoardUI al generar la tarjeta si AirTimeManager no tiene disponibilidad.
        /// </summary>
        public void LockReadBecauseNoTime()
        {
            _readButton.interactable = false;
            
            if (_lockedOverlay != null)
                _lockedOverlay.SetActive(true);
        }

        // ── Handlers ─────────────────────────────────────────────
        private void HandleReadClicked()
        {
            MarkAsProcessed();
            _onRead?.Invoke();
        }

        private void HandleSkipClicked()
        {
            MarkAsProcessed();
            _onSkip?.Invoke();
        }

        private void HandleDecodeClicked()
        {
            // Pasamos el tipo para que quien escuche pueda rutear
            // al minijuego correcto cuando haya más de uno.       // 📌 GDD — hoy SMS y Written usan el mismo, en el futuro puede cambiar
            _onDecodeRequested?.Invoke(_messageType);
        }

        // ── Visual ───────────────────────────────────────────────
        private void MarkAsProcessed()
        {
            _readButton.interactable = false;
            _skipButton.interactable = false;

            if (_processedOverlay != null)
                _processedOverlay.SetActive(true);
        }

        

    }
}