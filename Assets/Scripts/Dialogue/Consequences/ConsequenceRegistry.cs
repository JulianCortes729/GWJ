using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dialogue.Consequences
{
    /// <summary>Registro centralizado para handlers de consecuencias.</summary>
    public sealed class ConsequenceRegistry
    {
        /// <summary>Diccionario que mapea comandos a sus handlers.</summary>
        private readonly Dictionary<string, IConsequenceHandler> _handlers = new();

        /// <summary>Registra un nuevo handler de consecuencias.</summary>
        /// <param name="handler">Handler a registrar.</param>
        public void Register(IConsequenceHandler handler)
        {
            if (handler == null)
            {
                Debug.LogError("[ConsequenceRegistry] Handler nulo ignorado.");
                return;
            }

            if (_handlers.ContainsKey(handler.CommandName))
            {
                Debug.LogWarning(
                    $"[ConsequenceRegistry] Comando '{handler.CommandName}' " +
                    $"ya registrado. Sobreescribiendo.");
            }

            _handlers[handler.CommandName] = handler;
        }

        /// <summary>Desregistra un handler por nombre de comando.</summary>
        /// <param name="commandName">Nombre del comando a desregistrar.</param>
        public void Unregister(string commandName)
            => _handlers.Remove(commandName);



        /// <summary>Ejecuta el handler asociado a un comando.</summary>
        /// <param name="commandName">Nombre del comando a ejecutar.</param>
        /// <param name="parameters">Parámetros para el comando.</param>
        public void Dispatch(string commandName, string[] parameters)
        {
            if (_handlers.TryGetValue(commandName, out IConsequenceHandler handler))
            {
                handler.Execute(parameters);
                return;
            }

            Debug.LogWarning(
                $"[ConsequenceRegistry] Comando '{commandName}' " +
                $"no tiene handler registrado.");
        }


        /// <summary>Verifica si un comando tiene handler registrado.</summary>
        /// <param name="commandName">Nombre del comando.</param>
        /// <returns>True si el comando está registrado; false en caso contrario.</returns>
        public bool IsRegistered(string commandName)
            => _handlers.ContainsKey(commandName);
    }
}