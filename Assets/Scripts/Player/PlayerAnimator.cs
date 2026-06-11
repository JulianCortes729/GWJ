// Assets/Scripts/Player/PlayerAnimator.cs
using Pathfinding;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Puente entre IAstarAI y el Animator del player.
    /// Lee la velocidad de A* frame a frame y la traduce a
    /// parámetros cardinales para el Animator top-down 4 direcciones.
    /// Agregar Left/Right: solo reemplazá el clip en el Animator — cero cambios acá.
    /// </summary>
    public sealed class PlayerAnimator : MonoBehaviour
    {
        // ── Config ───────────────────────────────────────────────
        [Tooltip("Velocidad mínima (unidades/s) para activar la animación de caminar.")]
        [SerializeField] private float _moveThreshold = 0.1f;

        [Tooltip("Asignar si el Animator está en un GameObject hijo (sprite separado). " +
                 "Dejar vacío si está en este mismo objeto.")]
        [SerializeField] private Animator _animator;

        // ── Hashes — zero GC en Update ───────────────────────────
        // 🟢 POOL — StringToHash cachea el ID del parámetro una sola vez.
        // SetFloat(int) no hace lookup de string en runtime.
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int DirXHash  = Animator.StringToHash("DirX");
        private static readonly int DirYHash  = Animator.StringToHash("DirY");

        // ── Dependencias ─────────────────────────────────────────
        private IAstarAI _ai;

        // ── Estado ───────────────────────────────────────────────
        // Guardamos la última dirección conocida para que el idle
        // quede orientado hacia donde estaba mirando al detenerse.
        private Vector2 _lastDirection = Vector2.down; // 📌 GDD — Manu arranca mirando hacia abajo

        // ── Lifecycle ────────────────────────────────────────────
        private void Awake()
        {
            _ai = GetComponentInParent<IAstarAI>(); // 🟡 PERF — solo en Awake, no en Update

            // Si no fue asignado desde el Inspector, lo buscamos en este GO.
            if (_animator == null)
                _animator = GetComponent<Animator>();

            if (_ai == null)
                Debug.LogError("[PlayerAnimator] IAstarAI no encontrado. " +
                               "Asegurate de que este componente esté en el Player " +
                               "o en un hijo del GameObject que tiene el AIPath.", this);

            if (_animator == null)
                Debug.LogError("[PlayerAnimator] Animator no encontrado.", this);
        }

        // ── Update — hot path ────────────────────────────────────
        private void Update()
        {
            if (_ai == null || _animator == null) return;

            Vector2 velocity = (Vector2)_ai.velocity; // 🟡 PERF — cast de Vector3 a Vector2, descarta Z
            float   speed    = velocity.magnitude;

            // Speed controla la transición Idle ↔ Walk en el Animator.
            _animator.SetFloat(SpeedHash, speed);

            // Solo actualizamos la dirección si realmente nos estamos moviendo.
            // Sin este guard, el idle perdería la orientación al detenerse.
            if (speed > _moveThreshold)
                _lastDirection = SnapToCardinal(velocity.normalized);

            _animator.SetFloat(DirXHash, _lastDirection.x);
            _animator.SetFloat(DirYHash, _lastDirection.y);
        }

        // ── Helpers ──────────────────────────────────────────────

        /// <summary>
        /// Convierte una dirección normalizada al cardinal más cercano.
        /// Resultado: exactamente Vector2.up/down/left/right.
        /// Esto permite condiciones limpias en el Animator (DirY > 0.5, etc.)
        /// sin depender de la precisión del float de velocidad de A*.
        /// </summary>
        private static Vector2 SnapToCardinal(Vector2 direction)
        {
            // Eje dominante: el de mayor magnitud absoluta.
            // Diagonal 45° → empate → el eje Y gana (up/down tiene prioridad visual).
            if (Mathf.Abs(direction.y) >= Mathf.Abs(direction.x))
                return direction.y >= 0f ? Vector2.up : Vector2.down;

            return direction.x >= 0f ? Vector2.right : Vector2.left;
        }
    }
}