using UnityEngine;
using Dialogue.Core;
using Dialogue.Data;
using System.Collections.Generic;
using Ink.Runtime;

public sealed class DialogueTest : MonoBehaviour
{
    [SerializeField] private DialogueRunner _runner;
    [SerializeField] private TextAsset      _inkJson;

    private void OnEnable()
    {
        _runner.OnLineReady     += HandleLine;
        _runner.OnChoicesReady  += HandleChoices;
        _runner.OnDialogueEnded += HandleEnd;
    }

    private void OnDisable()
    {
        _runner.OnLineReady     -= HandleLine;
        _runner.OnChoicesReady  -= HandleChoices;
        _runner.OnDialogueEnded -= HandleEnd;
    }

    private void Start()
    {
        _runner.StartDialogue(_inkJson);
    }

    // Ahora al recibir una línea, avanzamos automáticamente.
    // En P2 esto lo hace el jugador presionando un botón.
    private void HandleLine(DialogueLine line)
    {
        if (string.IsNullOrEmpty(line.Speaker))
            Debug.Log($"[Narración] {line.Text}");
        else
            Debug.Log($"[{line.Speaker}] {line.Text}");

        // Avanzamos al siguiente contenido automáticamente.
        _runner.Continue();
    }

    private void HandleChoices(List<Choice> choices)
    {
        Debug.Log("── Opciones disponibles ──");
        for (int i = 0; i < choices.Count; i++)
            Debug.Log($"  [{i}] {choices[i].text}");

        // Elegimos la primera opción automáticamente.
        _runner.ChooseOption(0);
    }

    private void HandleEnd()
    {
        Debug.Log("[DialogueTest] Diálogo terminado.");
    }
}