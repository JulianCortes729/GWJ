// Assets/Scripts/Interaction/Core/IInteractionHandler.cs
using System.Threading;
using Cysharp.Threading.Tasks;
using Interaction.Core;

namespace Interaction.Handlers
{
    /// <summary>
    /// Contrato que debe cumplir cualquier handler de interacción.
    /// Permite agregar nuevos tipos de respuesta sin tocar InteractionSystem.
    /// OCP: abierto para extensión, cerrado para modificación.
    /// </summary>
    public interface IInteractionHandler
    {
        /// <summary>
        /// Retorna true si este handler puede procesar la interacción recibida.
        /// </summary>
        bool CanHandle(InteractableObject target, VerbType verb);

        /// <summary>
        /// Ejecuta la interacción. Async porque puede iniciar diálogo,
        /// animación, o esperar feedback del jugador.
        /// </summary>
        UniTask HandleAsync(InteractableObject target, VerbType verb, CancellationToken ct);
    }
}