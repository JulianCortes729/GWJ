using UnityEngine;
using Journey.Data;

namespace Journey.Data
{
    /// <summary>
    /// Configuración completa de una jornada.
    /// Un asset por jornada (Jornada1, Jornada2, etc.).
    /// Define mensajes disponibles y knots de Ink por fase.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Journey_",
        menuName = "Sintonía Directa/Journey/Journey Data")]
    public sealed class JourneyDataSO : ScriptableObject
    {
        [Header("Identidad")]
        [SerializeField] private int    _journeyNumber = 1;
        [SerializeField] private string _title         = string.Empty;

        [Header("Mensajes disponibles en PreProducción")]
        [Tooltip("Todos los mensajes que Manu puede revisar antes de salir al aire.")]
        [SerializeField] private MessageDataSO[] _messages = System.Array.Empty<MessageDataSO>();

        [Header("Ink — Knots de apertura por fase")]
        [Tooltip("Knot que arranca al iniciar PreProducción (contexto del día, diálogo con Mónica).")]
        [SerializeField] private string _preProductionKnot = string.Empty;

        [Tooltip("Knot que arranca al salir al aire.")]
        [SerializeField] private string _onAirKnot         = string.Empty;

        [Tooltip("Knot que arranca al cerrar la jornada.")]
        [SerializeField] private string _closingKnot       = string.Empty;

        [Header("Ink — Asset de la jornada")]
        [Tooltip("El .ink.json compilado que contiene todos los knots de esta jornada.")]
        [SerializeField] private TextAsset _inkAsset = null;

        // ── API de lectura ────────────────────────────────────────
        public int             JourneyNumber      => _journeyNumber;
        public string          Title              => _title;
        public MessageDataSO[] Messages           => _messages;
        public string          PreProductionKnot  => _preProductionKnot;
        public string          OnAirKnot          => _onAirKnot;
        public string          ClosingKnot        => _closingKnot;
        public TextAsset       InkAsset           => _inkAsset;
    }
}