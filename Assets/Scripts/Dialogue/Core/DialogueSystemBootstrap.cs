// Assets/Scripts/Dialogue/Core/DialogueSystemBootstrap.cs
using UnityEngine;
using Dialogue.Core;
using Interaction.Core;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Dialogue.Core
{
    /// <summary>
    /// Conecta InteractionSystem con DialogueRunner.
    /// Responsabilidad única: escuchar OnKnotRequested y
    /// delegar a DialogueRunner.StartDialogueAtKnot().
    /// Vive en el mismo GameObject que DialogueRunner.
    /// </summary>
    public sealed class DialogueSystemBootstrap : MonoBehaviour
    {
        // ─── Dependencias ─────────────────────────────────────────────
        [SerializeField] private DialogueRunner   _dialogueRunner;
        [SerializeField] private InteractionSystem _interactionSystem;
        [SerializeField] private TextAsset         _inkJsonAsset;      // el .ink.json de la escena actual


         // ─── Estado ───────────────────────────────────────────────────
        // Fuente de la promesa "el diálogo terminó".
        // Se crea cuando arranca el diálogo, se resuelve cuando termina.
        private UniTaskCompletionSource _dialogueEndTcs;             // 🟡 PERF — una alloc por diálogo, fuera del hot path


        // ─── Lifecycle ────────────────────────────────────────────────
        private void Awake()
        {
            ValidateDependencies();
        }

        private void Start()
        {
            // Start() garantiza que todos los Awake() ya corrieron
            // antes de cargar la historia                              // 📌 lifecycle correcto
            _dialogueRunner.LoadStory(_inkJsonAsset);

            // Construimos los handlers DESPUÉS de tener la referencia
            // a DialogueRunner, pasando los dos delegates.
            _interactionSystem.RegisterInkHandler(
                knot           => _dialogueRunner.StartDialogueAtKnot(knot),
                WaitForDialogueEndAsync
            );
        }

        private void OnEnable()
        {
            _dialogueRunner.OnDialogueEnded += HandleDialogueEnded; // Este método se llama cuando DialogueRunner emite OnDialogueEnded
        }

        private void OnDisable()
        {
            _dialogueRunner.OnDialogueEnded -= HandleDialogueEnded;
        }

        // ─── Puente awaitable ─────────────────────────────────────────

        /// <summary>
        /// Retorna una UniTask que se completa cuando DialogueRunner
        /// emite OnDialogueEnded. InteractAsync la awaita para mantener
        /// _isInteracting = true durante toda la conversación.
        /// </summary>
        private UniTask WaitForDialogueEndAsync(CancellationToken ct)
        {
            _dialogueEndTcs = new UniTaskCompletionSource();

            // Si el CancellationToken se dispara (ej: escena se descarga),
            // resolvemos la promesa para no dejar a InteractAsync colgado.
            ct.Register(() => _dialogueEndTcs?.TrySetCanceled());   // 🟡 PERF — alloc de delegate, pero solo en cancelación

            return _dialogueEndTcs.Task;
        }

        // ─── Handlers ─────────────────────────────────────────────────
       // private void HandleKnotRequested(string knot)
        //{
        //    _dialogueRunner.StartDialogueAtKnot(knot);
       // }

        private void HandleDialogueEnded()
        {
            _dialogueEndTcs?.TrySetResult();
            _dialogueEndTcs = null;
        }

        // ─── Validación ───────────────────────────────────────────────
        private void ValidateDependencies()
        {
            if (_dialogueRunner == null)
                Debug.LogError("[DialogueSystemBootstrap] DialogueRunner no asignado.", this);

            if (_interactionSystem == null)
                Debug.LogError("[DialogueSystemBootstrap] InteractionSystem no asignado.", this);

            if (_inkJsonAsset == null)
                Debug.LogError("[DialogueSystemBootstrap] inkJsonAsset no asignado.", this);
        }
    }
}
