namespace Journey.Core
{
    /// <summary>
    /// Fases que componen una jornada de radio.
    /// El JourneyManager transita entre estas en orden.
    /// </summary>
    public enum JourneyPhase
    {
        None          = 0,
        PreProduction = 1,   // Revisar mensajes, armar la grilla
        OnAir         = 2,   // Transmisión en vivo, operación de consola
        Closing       = 3,   // Cierre, cargar celular, charla con Mónica
        Ended         = 4    // Jornada terminada, listo para guardar y avanzar
    }
}
