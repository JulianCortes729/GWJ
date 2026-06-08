// Assets/Scripts/Interaction/Handlers/SceneTransitionInteractionHandler.cs
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Interaction.Core;
using Scene;
using UnityEngine;

namespace Interaction.Handlers
{
    /// <summary>
    /// Handler para interacciones que disparan una transición de escena.
    /// Se registra desde SceneTransitionBootstrap igual que InkInteractionHandler.
    /// Prioridad en la lista de handlers: después de Ink y consecuencias,
    /// ya que una transición es una acción terminal — no hay nada después.
    /// </summary>
    public sealed class SceneTransitionInteractionHandler : IInteractionHandler
    {
        // Delegate en vez de referencia directa a SceneTransitionManager.   // ⚠️ SOLID: DIP
        private readonly Action<string, string> _onTransitionRequested;      // sceneName, spawnId

        public SceneTransitionInteractionHandler(Action<string, string> onTransitionRequested)
        {
            _onTransitionRequested = onTransitionRequested
                ?? throw new ArgumentNullException(nameof(onTransitionRequested));
        }

        // ── IInteractionHandler ──────────────────────────────────

        public bool CanHandle(InteractableObject target, VerbType verb)
        {
            if (target?.Data == null) return false;
            return target.Data.GetSceneTransition(verb) != null;
        }

        public UniTask HandleAsync(InteractableObject target, VerbType verb, CancellationToken ct)
        {
            var transition = target.Data.GetSceneTransition(verb);

            Debug.Log($"[SceneTransitionHandler] Transicionando a '{transition.SceneName}' spawn='{transition.SpawnId}'");
            _onTransitionRequested.Invoke(transition.SceneName, transition.SpawnId);

            // La transición hace fade + carga de escena de forma asíncrona
            // internamente. Desde la perspectiva del handler, la acción
            // se disparó y el frame actual termina normalmente.
            // _isInteracting en InteractionSystem volverá a false al completarse,
            // pero la escena ya habrá cambiado — no hay riesgo de re-entrada.
            return UniTask.CompletedTask;
        }
    }
}
