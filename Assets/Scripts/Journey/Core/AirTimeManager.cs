using System;
using UnityEngine;

namespace Journey.Core
{
    /// <summary>
    /// Gestiona el tiempo de aire disponible (Slots) por jornada.
    /// Es la fuente de verdad para saber si el jugador aún puede leer mensajes.
    /// </summary>
    public sealed class AirTimeManager : MonoBehaviour
    {
        private int _maxSlots;
        private int _usedSlots;

        // Eventos para actualizar la UI visual de "Tiempo Restante"
        public event Action<int, int> OnSlotsChanged; 
        
        // Evento crítico para bloquear la interacción en el tablero
        public event Action OnSlotsDepleted;          

        public bool HasAvailableSlots => _usedSlots < _maxSlots;

        public void Initialize(int maxSlots)
        {
            _maxSlots = maxSlots;
            _usedSlots = 0;
            OnSlotsChanged?.Invoke(_usedSlots, _maxSlots);
        }

        public bool TryConsumeSlot()
        {
            if (!HasAvailableSlots) 
                return false;

            _usedSlots++;
            OnSlotsChanged?.Invoke(_usedSlots, _maxSlots);

            // 📌 GDD — Si nos quedamos sin tiempo, avisamos inmediatamente
            if (!HasAvailableSlots)
            {
                OnSlotsDepleted?.Invoke();
            }

            return true;
        }
    }
}