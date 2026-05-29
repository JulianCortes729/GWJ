// Assets/Scripts/Interaction/Handlers/InkInteractionHandler.cs
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Handlers
{
    /// <summary>
    /// Handler para interacciones que inician un knot de Ink.
    /// No conoce InteractionSystem — solo recibe lo que necesita por constructor.
    /// </summary>
    public sealed class InkInteractionHandler : IInteractionHandler
    {
        // ── Dependencia ──────────────────────────────────────────
        // Evento en vez de referencia directa a DialogueRunner.     // ⚠️ SOLID: DIP — dependemos de abstracción (Action), no de implementación concreta
        private readonly Action<string> _onKnotRequested;

        private readonly Func<CancellationToken, UniTask>  _waitForDialogueEnd; // 🔵 NET — en multijugador sería un RPC acknowledgment


        public InkInteractionHandler(Action<string> onKnotRequested, Func<CancellationToken, UniTask> waitForDialogueEnd)
        {
            _onKnotRequested = onKnotRequested ?? throw new ArgumentNullException(nameof(onKnotRequested));
            _waitForDialogueEnd = waitForDialogueEnd ?? throw new ArgumentNullException(nameof(waitForDialogueEnd));
        }

        // ── IInteractionHandler ──────────────────────────────────
        public bool CanHandle(InteractableObject target, VerbType verb)
        {
            if (target == null || target.Data == null) return false;
            return target.Data.GetKnot(verb) != null;              // tiene knot configurado para este verbo
        }

        public async UniTask HandleAsync(InteractableObject target, VerbType verb, CancellationToken ct)
        {
            var knot = target.Data.GetKnot(verb);
            Debug.Log($"[InkHandler] Knot solicitado='{knot}' para '{target.DisplayName}' verb={verb}");
            _onKnotRequested.Invoke(knot);

            // Esperamos a que el diálogo termine antes de permitir nuevas interacciones.  // ⚠️ UX: bloquea otras interacciones mientras el diálogo esté activo
            await _waitForDialogueEnd(ct);                           
        }
    }
}