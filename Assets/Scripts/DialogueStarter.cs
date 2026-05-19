using UnityEngine;
using Dialogue.Core;

public class DialogueStarter : MonoBehaviour
{
    [SerializeField] private DialogueRunner _runner;
    [SerializeField] private TextAsset      _inkJson;

    private void Start()
    {
        _runner.StartDialogue(_inkJson);
    }
}
