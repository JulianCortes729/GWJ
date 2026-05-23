namespace Dialogue.Consequences
{
    /// <summary>
    /// Contrato para objetos de escena que tienen estados nominales.
    /// Ejemplo: radio → "encendida", "apagada", "rota"
    /// </summary>
    public interface IStateful
    {
        void SetState(string state);
        string GetState();
    }
}
