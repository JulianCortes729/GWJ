using UnityEngine;

namespace Dialogue.Consequences.Handlers
{
    /// <summary>
    /// Comando: play_animation [nombreObjeto] [nombreAnimacion]
    /// Ejemplo en Ink: # play_animation personaje sorpresa
    /// Busca un Animator en el GameObject y dispara el trigger.
    /// </summary>
    public sealed class PlayAnimationHandler : IConsequenceHandler
    {
        public string CommandName => "play_animation";

        public void Execute(string[] parameters)
        {
            if (parameters.Length < 2)
            {
                Debug.LogWarning(
                    "[PlayAnimationHandler] Requiere 2 parámetros: " +
                    "nombreObjeto nombreAnimacion.");
                return;
            }

            string objectName    = parameters[0];
            string animationName = parameters[1];

            GameObject target = GameObject.Find(objectName); // 🟡 PERF — ver nota abajo
            if (target == null)
            {
                Debug.LogWarning(
                    $"[PlayAnimationHandler] GameObject '{objectName}' no encontrado.");
                return;
            }

            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning(
                    $"[PlayAnimationHandler] '{objectName}' no tiene Animator.");
                return;
            }

            animator.SetTrigger(animationName);
        }
    }
}
