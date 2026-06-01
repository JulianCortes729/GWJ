// Assets/Scripts/Scene/SceneTransitionTrigger.cs
using Interaction.Core;
using Player;
using UnityEngine;

namespace Scene
{
    /// <summary>
    /// Colocar en el GameObject de una puerta o salida.
    /// Cuando el player entra al trigger, inicia la transición de escena.
    /// Requiere que el player tenga el tag "Player".
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SceneTransitionTrigger : MonoBehaviour
    {
        // ── Config ───────────────────────────────────────────────
        [Header("Destino")]
        [Tooltip("Nombre exacto de la escena destino (debe estar en Build Settings).")]
        [SerializeField] private string _targetScene = string.Empty;

        [Tooltip("ID del spawn point donde aparece el player en la escena destino.")]
        [SerializeField] private string _targetSpawnId = string.Empty;

        [Header("Comportamiento")]
        [Tooltip("Si true, el trigger se activa solo cuando el player llega caminando (click izquierdo). " +
                 "Si false, se activa al tocar el trigger por cualquier medio.")]
        [SerializeField] private bool _requiresMovement = true;

        // ── Validación ───────────────────────────────────────────
        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (!col.isTrigger)
            {
                Debug.LogWarning(
                    $"[SceneTransitionTrigger] El Collider2D en '{gameObject.name}' " +
                    $"no es trigger. Corregido automáticamente.", this);
                col.isTrigger = true;
            }

            if (string.IsNullOrEmpty(_targetScene))
                Debug.LogError(
                    $"[SceneTransitionTrigger] _targetScene vacío en '{gameObject.name}'.", this);
        }

        // ── Trigger ──────────────────────────────────────────────
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;                // 📌 GDD — solo el player activa transiciones

            if (SceneTransitionManager.Instance == null)
            {
                Debug.LogError("[SceneTransitionTrigger] SceneTransitionManager no encontrado.", this);
                return;
            }

            // Si _requiresMovement es true, verificamos que el player
            // está en movimiento (llegó caminando, no fue teleportado)  // 📌 GDD — fiel al comportamiento DOTT
            if (_requiresMovement)
            {
                var controller = other.GetComponent<PlayerController>();  // 🟡 PERF — GetComponent en trigger, no en Update
                if (controller != null && !controller.IsMoving) return;
            }

            SceneTransitionManager.Instance.TransitionTo(_targetScene, _targetSpawnId);
        }
    }
}