using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dialogue.Consequences
{
    public interface IConsequenceHandler
    {
        /// <summary>
        /// Nombre del comando tal como aparece en Ink.
        /// Ejemplo: "set_object_state" para <<set_object_state "radio" "encendida">>
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// Ejecuta el comando con los parámetros extraídos del tag de Ink.
        /// </summary>
        void Execute(string[] parameters);
    }
}