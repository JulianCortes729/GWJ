using UnityEngine;

namespace Dialogue.Consequences.Handlers
{
    /// <summary>
    /// Comando: set_object_visibility [nombreObjeto] [true/false]
    /// Ejemplo en Ink: # set_object_visibility llave false
    /// </summary>
    public sealed class SetObjectVisibilityHandler : IConsequenceHandler
    {
        public string CommandName => "set_object_visibility";

        public void Execute(string[] parameters)
        {
            if (parameters.Length < 2)
            {
                Debug.LogWarning(
                    "[SetObjectVisibilityHandler] Requiere 2 parámetros: " +
                    "nombreObjeto true/false.");
                return;
            }

            string objectName = parameters[0];

            if (!bool.TryParse(parameters[1], out bool visible))
            {
                Debug.LogWarning(
                    $"[SetObjectVisibilityHandler] Parámetro '{parameters[1]}' " +
                    $"no es un booleano válido.");
                return;
            }

            GameObject target = GameObject.Find(objectName); // 🟡 PERF — ver nota abajo
            if (target == null)
            {
                Debug.LogWarning(
                    $"[SetObjectVisibilityHandler] GameObject '{objectName}' no encontrado.");
                return;
            }

            target.SetActive(visible);
        }
    }
}