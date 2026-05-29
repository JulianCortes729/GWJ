// Assets/Scripts/Interaction/Handlers/ConsequenceInteractionHandler.cs
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Interaction.Core;
using UnityEngine;

namespace Interaction.Handlers
{
    /// <summary>
    /// Handler para interacciones que disparan consecuencias directas
    /// sin pasar por Ink. Usa el ConsequenceSystem ya existente.
    /// </summary>
    public sealed class ConsequenceInteractionHandler : IInteractionHandler
    {
        private readonly Action<string[]> _onConsequencesRequested;  // ⚠️ SOLID: DIP — Action en vez de referencia a ConsequenceSystem

        public ConsequenceInteractionHandler(Action<string[]> onConsequencesRequested)
        {
            _onConsequencesRequested = onConsequencesRequested ?? throw new ArgumentNullException(nameof(onConsequencesRequested));
        }

        // ── IInteractionHandler ──────────────────────────────────
        public bool CanHandle(InteractableObject target, VerbType verb)
        {
            if (target == null || target.Data == null) return false;
            var consequences = target.Data.GetConsequences(verb);
            return consequences != null && consequences.Length > 0;
        }

        public UniTask HandleAsync(InteractableObject target, VerbType verb, CancellationToken ct)
        {
            var consequences = target.Data.GetConsequences(verb);
            Debug.Log($"[ConsequenceHandler] Consequences=[{string.Join(',', consequences)}] para '{target.DisplayName}' verb={verb}");
            _onConsequencesRequested.Invoke(consequences);
            return UniTask.CompletedTask;
        }
    }
}