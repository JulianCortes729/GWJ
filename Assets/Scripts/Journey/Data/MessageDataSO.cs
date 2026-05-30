using UnityEngine;
using Journey.Core;

namespace Journey.Data
{
    /// <summary>
    /// Datos de un mensaje individual.
    /// Un asset por mensaje. Se referencia desde JourneyDataSO.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Message_",
        menuName = "Sintonía Directa/Journey/Message")]
    public sealed class MessageDataSO : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Remitente del mensaje. Se muestra en la lista.")]
        [SerializeField] private string _sender      = string.Empty;

        [Tooltip("Destinatario. Puede ser una persona, una estancia, o 'Oyentes'.")]
        [SerializeField] private string _recipient   = string.Empty;

        [Tooltip("Texto completo del mensaje tal como llega (puede tener faltas, ruido, etc.).")]
        [SerializeField] [TextArea(3, 8)] private string _rawText = string.Empty;

        [Header("Tipo y mecánica")]
        [SerializeField] private MessageType _type = MessageType.Written;

        [Tooltip("Si true, el jugador debe decodificarlo antes de leerlo al aire.")]
        [SerializeField] private bool _requiresDecoding = false;

        [Tooltip("Si true, este mensaje es narrativamente crítico (afecta el Final Verdadero).")]
        [SerializeField] private bool _isCritical = false;

        [Header("Ink")]
        [Tooltip("Knot de Ink que se dispara si el mensaje se lee al aire.")]
        [SerializeField] private string _onAirKnot     = string.Empty;

        [Tooltip("Knot de Ink que se dispara si el mensaje se omite.")]
        [SerializeField] private string _onSkippedKnot = string.Empty;

        // ── API de lectura ────────────────────────────────────────
        public string      Sender           => _sender;
        public string      Recipient        => _recipient;
        public string      RawText          => _rawText;
        public MessageType Type             => _type;
        public bool        RequiresDecoding => _requiresDecoding;
        public bool        IsCritical       => _isCritical;
        public string      OnAirKnot        => _onAirKnot;
        public string      OnSkippedKnot    => _onSkippedKnot;
    }
}
