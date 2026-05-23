using UnityEngine;

namespace Dialogue.Consequences.Handlers
{
    /// <summary>
    /// Comando: set_object_state [nombreObjeto] [estado]
    /// Ejemplo en Ink: # set_object_state radio encendida
    /// Busca un GameObject por nombre y llama SetState(estado) si implementa IStateful.
    /// </summary>
    public sealed class SetObjectStateHandler : IConsequenceHandler
    {
        public string CommandName => "set_object_state";

        public void Execute(string[] parameters)
        {
            if (parameters.Length < 2)
            {
                Debug.LogWarning(
                    "[SetObjectStateHandler] Requiere 2 parámetros: " +
                    "nombreObjeto estado.");
                return;
            }

            string objectName = parameters[0];
            string state      = parameters[1];

            GameObject target = GameObject.Find(objectName); // 🟡 PERF — ver nota abajo
            if (target == null)
            {
                Debug.LogWarning(
                    $"[SetObjectStateHandler] GameObject '{objectName}' no encontrado.");
                return;
            }

            IStateful stateful = target.GetComponent<IStateful>();
            if (stateful == null)
            {
                Debug.LogWarning(
                    $"[SetObjectStateHandler] '{objectName}' no implementa IStateful.");
                return;
            }

            stateful.SetState(state);
        }
    }
}
