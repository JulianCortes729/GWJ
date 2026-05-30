namespace Journey.Core
{
    /// <summary>
    /// Tipo de soporte físico/digital del mensaje.
    /// Determina qué mecánica de decodificación aplica.
    /// </summary>
    public enum MessageType
    {
        Written  = 0,   // Nota escrita a mano — puede tener letra ilegible
        SMS      = 1,   // SMS al celular — puede tener faltas de ortografía
        Call     = 2,   // Llamado telefónico — puede tener ruido de línea
        Email    = 3    // Correo electrónico — texto limpio por defecto
    }
}
