using Interaction.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Interaction.Presentation
{
    /// <summary>
    /// Cursor de software: Image UI que sigue al mouse frame a frame.
    /// Cambia de sprite según el verbo activo en VerbSelectorSO.
    /// Muestra el nombre del objeto bajo el cursor (tooltip inline).
    /// </summary>
    public sealed class CursorController : MonoBehaviour
    {
        // ── Config ───────────────────────────────────────────────
        [SerializeField] private VerbSelectorSO _verbSelector;
        [SerializeField] private InteractionSystem _interactionSystem;

        [Header("UI")]
        [SerializeField] private RectTransform _cursorRoot;      // el RectTransform que se mueve
        [SerializeField] private Image         _cursorImage;
        [SerializeField] private TMPro.TextMeshProUGUI _tooltipText;

        [Header("Sprites por verbo")]
        [SerializeField] private Sprite _spriteDefault;
        [SerializeField] private Sprite _spriteLook;
        [SerializeField] private Sprite _spriteTalk;
        [SerializeField] private Sprite _spriteUse;

        [Header("Offset del tooltip respecto al cursor (px)")]
        [SerializeField] private Vector2 _tooltipOffset = new(20f, -20f);

        [Header("Hotspot (px desde esquina superior-izquierda del sprite)")]
        [SerializeField] private Vector2 _hotspot = Vector2.zero;

        // ── Cache ────────────────────────────────────────────────
        private Canvas _canvas;
        private RectTransform _canvasRect;

        // ── Unity Lifecycle ──────────────────────────────────────
        private void Awake()
        {
            _canvas     = GetComponentInParent<Canvas>();
            _canvasRect = _canvas.GetComponent<RectTransform>();

            // ✅ Fix: anclar el sprite por su esquina superior-izquierda (tip del cursor)
            _cursorRoot.pivot = new Vector2(0f, 1f);

            Cursor.visible   = false;                            // oculta cursor del OS
            Cursor.lockState = CursorLockMode.Confined;          // confina al área del juego

            if (_tooltipText != null)
                _tooltipText.text = string.Empty;
        }

        private void OnEnable()
        {
            _verbSelector.OnVerbChanged          += HandleVerbChanged;
            _interactionSystem.OnHoverChanged    += HandleHoverChanged;

            HandleVerbChanged(_verbSelector.ActiveVerb);          // sincroniza estado inicial
        }

        private void OnDisable()
        {
            _verbSelector.OnVerbChanged          -= HandleVerbChanged;
            _interactionSystem.OnHoverChanged    -= HandleHoverChanged;

            //Cursor.visible   = true;                             // restaura cursor del OS al salir
            //Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            MoveCursorToMouse();
        }

        // ── Movimiento ───────────────────────────────────────────
        private void MoveCursorToMouse()
        {
            // Convierte posición de pantalla a posición local del Canvas
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect,
                UnityEngine.InputSystem.Mouse.current.position.ReadValue(),
                _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                out var localPoint
            );

            //Restar el hotspot para que el TIP del sprite coincida con el mouse
            _cursorRoot.localPosition = localPoint - _hotspot;

            if (_tooltipText != null)
                _tooltipText.rectTransform.localPosition = localPoint + _tooltipOffset;
        }

        // ── Handlers ─────────────────────────────────────────────
        private void HandleVerbChanged(VerbType verb)
        {
            _cursorImage.sprite = verb switch
            {
                VerbType.Look => _spriteLook,
                VerbType.Talk => _spriteTalk,
                VerbType.Use  => _spriteUse,
                _             => _spriteDefault
            };
        }

        private void HandleHoverChanged(string displayName)
        {
            if (_tooltipText == null) return;
            _tooltipText.text = displayName;                     // string vacío cuando no hay hover
        }
    }
}
