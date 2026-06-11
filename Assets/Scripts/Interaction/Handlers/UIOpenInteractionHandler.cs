// Assets/Scripts/Interaction/Handlers/UIOpenInteractionHandler.cs
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Interaction.Core;
using Journey.Presentation;
using UnityEngine;

namespace Interaction.Handlers
{
    /// <summary>
    /// Handler para interacciones que abren un panel de UI (ej: MessageBoardUI).
    /// Bloquea _isInteracting mientras el panel esté abierto.
    /// Sigue el mismo patrón awaitable que InkInteractionHandler.
    /// </summary>
    public sealed class UIOpenInteractionHandler : IInteractionHandler
    {
        // Key: (instanceId del InteractableObject, verbo que lo activa)
        // Value: el MessageBoardUI que debe abrirse
        // 🟢 Dictionary — O(1) lookup, sin alloc en runtime
        private readonly Dictionary<(int instanceId, VerbType verb), MessageBoardUI> _panels = new();

        // ── Registro ─────────────────────────────────────────────

        /// <summary>
        /// Registra un vínculo entre un objeto de escena y el panel que debe abrir.
        /// Llamar desde UIInteractionBootstrap.Start().
        /// </summary>
        public void Register(InteractableObject target, VerbType verb, MessageBoardUI board)
        {
            if (target == null || board == null)
            {
                Debug.LogWarning("[UIOpenHandler] target o board nulos. Entrada ignorada.");
                return;
            }

            var key = (target.GetInstanceID(), verb);

            if (_panels.ContainsKey(key))
                Debug.LogWarning($"[UIOpenHandler] Sobreescribiendo entrada para '{target.DisplayName}' + {verb}.");

            _panels[key] = board;
        }

        // ── IInteractionHandler ──────────────────────────────────

        public bool CanHandle(InteractableObject target, VerbType verb)
            => target != null && _panels.ContainsKey((target.GetInstanceID(), verb));

        public async UniTask HandleAsync(InteractableObject target, VerbType verb, CancellationToken ct)
        {
            if (!_panels.TryGetValue((target.GetInstanceID(), verb), out var board))
                return;

            // Promesa que se resuelve cuando el jugador cierra el panel.
            // Mismo patrón que InkInteractionHandler — mantiene _isInteracting = true
            // durante toda la sesión de revisión de mensajes.            // ⚠️ SOLID: DIP — no conoce quién cierra, solo escucha el evento
            var tcs = new UniTaskCompletionSource();

            // Si la escena se descarga o se cancela externamente, cerramos limpio.
            ct.Register(() => tcs.TrySetCanceled());                  // 🟡 PERF — alloc de delegate solo en cancelación

            void OnClosed() => tcs.TrySetResult();
            board.OnBoardClosed += OnClosed;

            try
            {
                board.Open();
                await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                // No cerramos el panel aquí. Si el InteractionSystem se cancela
                // por razones internas, el panel debe seguir abierto.
                // Solo el botón "Cerrar" (o un cambio de fase) debe cerrarlo.
            }
            finally
            {
                board.OnBoardClosed -= OnClosed;                       // siempre desuscribir
            }
        }
    }
}
