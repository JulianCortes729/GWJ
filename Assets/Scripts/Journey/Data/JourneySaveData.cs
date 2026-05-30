using System;
using System.Collections.Generic;

namespace Journey.Data
{
    /// <summary>
    /// Datos serializables a JSON para guardar el progreso entre sesiones.
    /// Solo tipos primitivos y colecciones simples — sin referencias a Unity.
    /// </summary>
    [Serializable]
    public sealed class JourneySaveData
    {
        /// <summary>Número de jornada actual (1-7).</summary>
        public int CurrentJourneyNumber = 1;

        /// <summary>Fase actual dentro de la jornada.</summary>
        public int CurrentPhase = 0;   // cast a JourneyPhase al leer

        /// <summary>
        /// Nombres de los mensajes (MessageDataSO.name) que el jugador
        /// ya leyó al aire. Usamos el nombre del asset como ID estable.
        /// </summary>
        public List<string> ReadMessageNames = new();

        /// <summary>
        /// Nombres de los mensajes que el jugador omitió activamente.
        /// </summary>
        public List<string> SkippedMessageNames = new();

        /// <summary>
        /// Si el celular fue cargado al cierre de la jornada anterior.
        /// Crítico para el Final Verdadero.
        /// </summary>
        public bool PhoneChargedLastNight = false;

        /// <summary>
        /// Estado serializado de Ink (story.state.ToJson()).
        /// Permite retomar la narrativa exactamente donde quedó.
        /// </summary>
        public string InkStateJson = string.Empty;

        /// <summary>Variables del DialogueStorageSO que deben persistir.</summary>
        public List<SavedBool>   SavedBools   = new();
        public List<SavedFloat>  SavedFloats  = new();
        public List<SavedString> SavedStrings = new();
    }

    // Wrappers serializables para el diccionario del StorageSO.
    // Dictionary<K,V> no es serializable por JsonUtility directamente.
    [Serializable] public sealed class SavedBool   { public string Key; public bool   Value; }
    [Serializable] public sealed class SavedFloat  { public string Key; public float  Value; }
    [Serializable] public sealed class SavedString { public string Key; public string Value; }
}
