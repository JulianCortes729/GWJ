namespace Dialogue.Data
{
    // Representa una línea de diálogo ya procesada, lista para
    // ser consumida por la UI y el AudioManager.
    // Es un struct porque es datos puros sin identidad de referencia.
    public readonly struct DialogueLine
    {
        public readonly string Speaker; // Nombre del personaje. Vacío si es narración.
        public readonly string Text;    // Contenido de la línea.

        public DialogueLine(string speaker, string text)
        {
            Speaker = speaker;
            Text    = text;
        }

        // Parsea el formato "Personaje: Texto" que usamos en los .ink.
        // Si la línea no tiene ":", se trata como narración sin speaker.
        public static DialogueLine Parse(string rawLine)
        {
            int separatorIndex = rawLine.IndexOf(':');

            if (separatorIndex <= 0)
                return new DialogueLine(string.Empty, rawLine.Trim());

            string speaker = rawLine[..separatorIndex].Trim();
            string text    = rawLine[(separatorIndex + 1)..].Trim();

            return new DialogueLine(speaker, text);
        }
    }
}
