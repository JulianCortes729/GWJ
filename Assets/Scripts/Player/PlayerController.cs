// Assets/Scripts/Player/PlayerController.cs
using Cysharp.Threading.Tasks;
using Dialogue.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    /// <summary>
    /// Mueve al personaje hacia el punto clickeado en el suelo.
    /// Click izquierdo siempre mueve — la interacción es exclusiva del click derecho.
    /// No conoce InteractionSystem ni DialogueRunner directamente.
    /// Se comunica con ellos vía interfaces/eventos.             // ⚠️ SOLID: DIP
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        // ── Config ───────────────────────────────────────────────
        [Header("Movimiento")]
        [SerializeField] private float _moveSpeed = 3f;

        [Header("Referencias")]
        [SerializeField] private Camera         _camera;
        [SerializeField] private LayerMask      _groundLayer;
        [SerializeField] private DialogueRunner _dialogueRunner;    // solo para saber si hay diálogo activo

        [Header("Threshold de llegada (unidades de mundo)")]
        [SerializeField] private float _arrivalThreshold = 0.05f;

        // ── Estado interno ───────────────────────────────────────
        private Rigidbody2D _rb;
        private Vector2     _destination;
        private bool        _isMoving;
        public bool IsMoving => _isMoving;


        // ── Cache de raycast ─────────────────────────────────────
        private readonly RaycastHit2D[] _groundBuffer = new RaycastHit2D[4]; // 🟢 POOL — NonAlloc, zero GC
        private float _colliderHalfHeight;  // mitad de la altura del collider — offset pies→centro


        // ── Unity Lifecycle ──────────────────────────────────────
        private void Awake()
        {
            _rb          = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;                                  // 📌 GDD — vista cenital/lateral sin gravedad
            _rb.freezeRotation = true;

            if (_camera == null)
                _camera = Camera.main;
            
            // Calcula el offset entre el centro del player y sus pies
            // para posicionarlo correctamente sobre el suelo           // 🟡 PERF — calculado una sola vez en Awake
            var col = GetComponent<Collider2D>();
            if (col != null)
                _colliderHalfHeight = col.bounds.size.y * 0.5f;
            else
                Debug.LogWarning("[PlayerController] No se encontró Collider2D para calcular offset de suelo.");
                    }

        private void FixedUpdate()
        {
            if (!_isMoving) return;
            MoveTowardsDestination();
        }

        // ── Input — asignar en PlayerInput → Click → performed ──
        /// <summary>
        /// Asignar al evento "Click" performed en el PlayerInput de este GameObject.
        /// </summary>
        public void OnMoveClickPerformed()
        {
            if (_dialogueRunner != null && _dialogueRunner.IsRunning) return;

            var screenPos = Mouse.current.position.ReadValue();
            var worldPos  = _camera.ScreenToWorldPoint(screenPos);
            worldPos.z    = 0f;

            // Lanzamos un raycast vertical hacia abajo desde la X del cursor
            // La Y de origen es alta para asegurarnos de estar por encima de cualquier suelo
            var rayOrigin = new Vector2(worldPos.x, worldPos.y + 50f);     // 🟡 PERF — +50 cubre cualquier escena razonable

            int hitCount = Physics2D.RaycastNonAlloc(
                rayOrigin,
                Vector2.down,                   // ← dirección vertical hacia abajo
                _groundBuffer,
                100f,                           // distancia máxima del raycast
                _groundLayer
            );

            if (hitCount == 0)
            {
                // No hay suelo bajo la X del cursor — ignorar
                // Esto cubre los bordes del escenario donde no hay geometría
                Debug.Log("[PlayerController] Click fuera del área pisable.");
                return;
            }

            // Tomamos el primer hit — la Y real del suelo bajo el cursor
            var groundHit = _groundBuffer[0];
            var destination = new Vector2(worldPos.x, groundHit.point.y + _colliderHalfHeight);

            SetDestination(destination);
        }

        // ── Movimiento ───────────────────────────────────────────
        private void SetDestination(Vector2 destination)
        {
            _destination = destination;
            _isMoving    = true;
        }

        private void MoveTowardsDestination()
        {
            var current   = _rb.position;
            var direction = (_destination - current);
            var distance  = direction.magnitude;

            if (distance <= _arrivalThreshold)
            {
                _rb.MovePosition(_destination);
                _isMoving = false;
                _rb.velocity = Vector2.zero;
                return;
            }

            // MovePosition sobre Rigidbody2D — correcto para física 2D                // 🟡 PERF — llamar siempre en FixedUpdate, nunca en Update
            _rb.MovePosition(current + direction.normalized * (_moveSpeed * Time.fixedDeltaTime));
        }

        /// <summary>
        /// Cancela el movimiento en curso. Llamar al iniciar un diálogo
        /// o al abrir el menú radial si se desea detener al personaje.
        /// </summary>
        public void StopMovement()
        {
            _isMoving          = false;
            _rb.velocity = Vector2.zero;
        }
    }
}
