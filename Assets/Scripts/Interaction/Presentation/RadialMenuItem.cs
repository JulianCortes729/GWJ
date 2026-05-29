using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Interaction.Core;

namespace Interaction.Presentation
{
    /// <summary>
    /// Un ítem del menú radial. Representa un verbo (Mirar/Hablar/Usar).
    /// Notifica al VerbHUD cuando es seleccionado.
    /// No conoce VerbSelectorSO directamente — delega hacia arriba.  // ⚠️ SOLID: SRP
    /// </summary>
    public sealed class RadialMenuItem : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // ── Config ───────────────────────────────────────────────
        [SerializeField] private Image            _background;
        [SerializeField] private Image            _icon;
        [SerializeField] private TextMeshProUGUI  _label;

        [SerializeField] private Color _colorNormal    = new(0.15f, 0.15f, 0.15f, 0.92f);
        [SerializeField] private Color _colorHighlight = new(0.85f, 0.60f, 0.10f, 1.00f);

        // ── Estado ───────────────────────────────────────────────
        public VerbType Verb { get; private set; }

        // ── Evento hacia VerbHUD ─────────────────────────────────
        public event Action<VerbType> OnSelected;

        // ── API de inicialización ────────────────────────────────
        public void Initialize(VerbType verb, string label, Sprite icon)
        {
            Verb = verb;

            if (_label != null)     _label.text   = label;
            if (_icon  != null)     _icon.sprite   = icon;
            if (_background != null) _background.color = _colorNormal;
        }

        // ── IPointerHandlers ─────────────────────────────────────
        public void OnPointerEnter(PointerEventData _) => SetHighlight(true);
        public void OnPointerExit(PointerEventData _)  => SetHighlight(false);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            Debug.Log($"[RadialItem] Click en ítem: verb={Verb}");
            OnSelected?.Invoke(Verb);
        }

        // ── Visual ───────────────────────────────────────────────
        private void SetHighlight(bool active)
        {
            if (_background != null)
                _background.color = active ? _colorHighlight : _colorNormal;
        }

        public void SetActiveVerb(bool isActive)        // llamado por VerbHUD para marcar el verbo actual
        {
            SetHighlight(isActive);
        }
    }
}