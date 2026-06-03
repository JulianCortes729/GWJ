// Assets/Scripts/Player/PlayerController.cs
using Dialogue.Core;
using Pathfinding; // ◄ El estándar gratuito de la industria
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    /// <summary>
    /// Mueve al personaje hacia el punto clickeado utilizando A* Pathfinding.
    /// Click izquierdo siempre mueve — la interacción es exclusiva del click derecho.
    /// No conoce InteractionSystem ni DialogueRunner directamente.
    /// Se comunica con ellos vía interfaces/eventos.             // ⚠️ SOLID: DIP
    /// </summary>
    [RequireComponent(typeof(IAstarAI))] // ◄ Requerimos cualquier componente de IA de A*
    public sealed class PlayerController : MonoBehaviour
    {
        // ── Config ───────────────────────────────────────────────
        [Header("Referencias")]
        [SerializeField] private Camera         _camera;
        [SerializeField] private DialogueRunner _dialogueRunner;

        // ── Estado interno ───────────────────────────────────────
        private IAstarAI _ai;
        
        // Verifica si está calculando ruta o si todavía no llegó al final
        public bool IsMoving => _ai.pathPending || !_ai.reachedDestination;

        // ── Unity Lifecycle ──────────────────────────────────────
        private void Awake()
        {
            // Usamos la interfaz genérica IAstarAI, que A* implementa en su componente AIPath
            _ai = GetComponent<IAstarAI>();

            if (_camera == null)
                _camera = Camera.main;
        }

        // ── Input — asignar en PlayerInput → Click → performed ──
        public void OnMoveClickPerformed()
        {
            if (_dialogueRunner != null && _dialogueRunner.IsRunning) return;

            var screenPos = Mouse.current.position.ReadValue();
            var worldPos  = _camera.ScreenToWorldPoint(screenPos);
            worldPos.z    = 0f;

            // Reactivamos el movimiento por si fue detenido por un diálogo
            _ai.canMove = true;
            
            // Delegamos la búsqueda de ruta al plugin
            _ai.destination = worldPos;
        }

        // ── Movimiento ───────────────────────────────────────────
        /// <summary>
        /// Cancela el movimiento en curso. Llamar al iniciar un diálogo
        /// o al abrir el menú radial si se desea detener al personaje.
        /// </summary>
        public void StopMovement()
        {
            _ai.canMove = false;
        }
    }
}