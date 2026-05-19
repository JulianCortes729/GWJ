using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dialogue.Presentation
{
    // Representa un botón de opción en el panel de diálogo.
    // Se inicializa con el texto y el callback de la elección.
    // Diseñado para ser usado con Object Pool en DialogueUIView.

    [RequireComponent(typeof(Button))]
    public sealed class DialogueChoiceButton : MonoBehaviour
    {
        // ─── Dependencias ─────────────────────────────────────────────

        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Button          _button;

        // ─── Lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            if (_label == null)
                Debug.LogError(
                    $"[DialogueChoiceButton] _label no asignado en {gameObject.name}.");

            if (_button == null)
                _button = GetComponent<Button>();
        }

        // ─── API pública ──────────────────────────────────────────────

        // Inicializa el botón con texto y callback.
        // onChosen se llama cuando el jugador clickea la opción.
        public void Initialize(string choiceText, Action onChosen)
        {
            _label.text = choiceText;

            // Limpiamos listeners anteriores antes de agregar el nuevo.
            // Crítico para el pool: el botón puede haber sido usado antes.
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onChosen?.Invoke());
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
