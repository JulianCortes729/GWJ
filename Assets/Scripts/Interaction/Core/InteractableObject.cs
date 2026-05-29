// Assets/Scripts/Interaction/Core/InteractableObject.cs
using System;
using Interaction.Data;
using UnityEngine;

namespace Interaction.Core
{
    /// <summary>
    /// Componente que marca un GameObject como interactuable.
    /// Datos de configuración viven en InteractableDataSO.
    /// Este componente solo expone la API que InteractionSystem necesita.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class InteractableObject : MonoBehaviour
    {
        // ── Config ───────────────────────────────────────────────
        [SerializeField] private InteractableDataSO _data;

        // ── Evento de solicitud de interacción ───────────────────
        // InteractionSystem escucha este evento.
        // InteractableObject no sabe quién lo escucha. // ⚠️ SOLID: OCP — agregar listeners = cero cambios acá
        public event Action<InteractableObject, VerbType> OnInteractionRequested;

        // ── Propiedades públicas ─────────────────────────────────
        public string DisplayName => _data != null ? _data.DisplayName : gameObject.name;
        public InteractableDataSO Data => _data;

        // ── Validación en Editor ─────────────────────────────────
        private void Awake()
        {
            if (_data == null)
                Debug.LogError($"[InteractableObject] '{gameObject.name}' no tiene InteractableDataSO asignado.", this);
        }

        // ── API pública ──────────────────────────────────────────
        /// <summary>
        /// Llamado por InteractionSystem cuando el jugador hace click
        /// con un verbo activo sobre este objeto.
        /// </summary>
        public void Interact(VerbType verb)
        {
            if (_data == null) return;

            if (!_data.Supports(verb))
            {
                // 📌 GDD: feedback al jugador de acción no soportada
                // Por ahora log; en P5.3 esto dispara línea de diálogo genérica
                Debug.Log($"[InteractableObject] '{DisplayName}' no soporta el verbo {verb}.");
                return;
            }

            OnInteractionRequested?.Invoke(this, verb);
        }
    }
}
