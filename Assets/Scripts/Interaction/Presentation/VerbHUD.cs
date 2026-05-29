// Assets/Scripts/Interaction/Presentation/VerbHUD.cs
using System.Collections.Generic;
using System.Linq;
using Interaction.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Interaction.Presentation
{
    /// <summary>
    /// Menú radial de verbos. Aparece al hacer click derecho sobre un objeto.
    /// Se posiciona sobre el objeto clickeado.
    /// Comunica la selección a VerbSelectorSO y cierra el menú.
    /// No conoce InteractionSystem directamente.              // ⚠️ SOLID: DIP
    /// </summary>
    public sealed class VerbHUD : MonoBehaviour
    {
        // ── Config ───────────────────────────────────────────────
        [SerializeField] private VerbSelectorSO _verbSelector;
        [SerializeField] private InteractionSystem _interactionSystem;

        [Header("Radial")]
        [SerializeField] private GameObject      _radialRoot;        // panel que se activa/desactiva
        [SerializeField] private RadialMenuItem  _itemLook;
        [SerializeField] private RadialMenuItem  _itemTalk;
        [SerializeField] private RadialMenuItem  _itemUse;

        [Header("Sprites de íconos por verbo")]
        [SerializeField] private Sprite _iconLook;
        [SerializeField] private Sprite _iconTalk;
        [SerializeField] private Sprite _iconUse;

        [Header("Radio de distribución de ítems (px)")]
        [SerializeField] private float _radius = 80f;

        // ── Cache ────────────────────────────────────────────────
        private Canvas          _canvas;
        private RectTransform   _canvasRect;
        private readonly List<RadialMenuItem> _items = new(3);

        // ── Estado ───────────────────────────────────────────────
        private bool _isOpen;


        private InteractableObject _radialTarget;


        private int _openedOnFrame = -1;


        // ── Unity Lifecycle ──────────────────────────────────────
        private void Awake()
        {
            _canvas     = GetComponentInParent<Canvas>();
            _canvasRect = _canvas.GetComponent<RectTransform>();

            InitializeItems();
            _radialRoot.SetActive(false);
        }

        private void OnEnable()
        {
            _interactionSystem.OnRadialRequested += HandleRadialRequested;   // click derecho sobre objeto
            _verbSelector.OnVerbChanged          += HandleVerbChanged;

            foreach (var item in _items)
                item.OnSelected += HandleItemSelected;
        }

        private void OnDisable()
        {
            _interactionSystem.OnRadialRequested -= HandleRadialRequested;
            _verbSelector.OnVerbChanged          -= HandleVerbChanged;

            foreach (var item in _items)
                item.OnSelected -= HandleItemSelected;
        }

        private void Update()
        {
            // Cerrar el menú si el jugador hace click derecho de nuevo
            // o presiona Escape
            if (!_isOpen) return;

            if (Time.frameCount == _openedOnFrame) return;          // ← guard de frame


            if (Mouse.current.rightButton.wasPressedThisFrame ||
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        // ── Setup ────────────────────────────────────────────────
        private void InitializeItems()
        {
            _itemLook.Initialize(VerbType.Look, "Mirar", _iconLook);
            _itemTalk.Initialize(VerbType.Talk, "Hablar", _iconTalk);
            _itemUse.Initialize(VerbType.Use,  "Usar",   _iconUse);

            _items.Add(_itemLook);
            _items.Add(_itemTalk);
            _items.Add(_itemUse);

            DistributeRadially();
        }

        /// <summary>
        /// Distribuye los ítems en arco superior (180°) centrado en el origen del radialRoot.
        /// Arco superior porque el menú se abre sobre el objeto, no debajo.
        /// </summary>
        private void DistributeRadially()
        {
            // 3 ítems en arco de 180°: -90°, 90°, y 90° en el centro = 0°
            // Ángulos: izquierda=150°, arriba=90°, derecha=30° (en grados, eje Y arriba)
            float[] angles = { 150f, 90f, 30f };

            for (var i = 0; i < _items.Count; i++)
            {
                var rad = angles[i] * Mathf.Deg2Rad;
                var offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _radius;
                _items[i].GetComponent<RectTransform>().anchoredPosition = offset;
            }
        }

        // ── Abrir / Cerrar ───────────────────────────────────────
        private void HandleRadialRequested(Vector2 screenPosition, InteractableObject target)
        {
            _radialTarget = target;
            Debug.Log($"[VerbHUD] HandleRadialRequested target='{target.DisplayName}' screenPos={screenPosition}");
            Debug.Log($"[VerbHUD] HandleRadialRequested target='{target.DisplayName}' id={target.GetInstanceID()}");
            // Filtra ítems según verbos soportados por el objeto
            foreach (var item in _items)
            {
                var supported = target.Data.Supports(item.Verb);
                item.gameObject.SetActive(supported);
                item.SetActiveVerb(item.Verb == _verbSelector.ActiveVerb && supported);
            }


            PositionAtScreen(screenPosition);
            _openedOnFrame = Time.frameCount;                       // ← registrar frame de apertura
            _radialRoot.SetActive(true);
            _isOpen = true;
            _interactionSystem.NotifyRadialOpened();  

        }

        private void Close()
        {
            _radialRoot.SetActive(false);
            _isOpen = false;
            _interactionSystem.NotifyRadialClosed();

        }

        /// <summary>
        /// Convierte posición de pantalla a posición local del Canvas
        /// para que el menú aparezca exactamente donde está el cursor.
        /// </summary>
        private void PositionAtScreen(Vector2 screenPosition)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                screenPosition,
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out var localPoint
            );

            _radialRoot.GetComponent<RectTransform>().localPosition = localPoint;
        }

        // ── Handlers ─────────────────────────────────────────────
        private void HandleItemSelected(VerbType verb)
        {
            
            _verbSelector.SetVerb(verb);
            Close();

            Debug.Log($"[VerbHUD] Item seleccionado: {verb} — ejecutar sobre objeto bajo cursor");

            Debug.Log($"[VerbHUD] Selección actual target id={_radialTarget?.GetInstanceID() ?? -1}");
            // Ejecutar la interacción inmediatamente con el objeto
            // que estaba bajo el cursor cuando se abrió el radial.   // 📌 GDD: flujo radial → ejecución directa sin click extra
            _interactionSystem.ExecuteInteraction(_radialTarget, verb);
            
            // Opción B: resetear a verbo default después de ejecutar
            _verbSelector.ResetToDefault();
        }

        private void HandleVerbChanged(VerbType verb)
        {
            foreach (var item in _items)
                item.SetActiveVerb(item.Verb == verb);
        }
    }
}
