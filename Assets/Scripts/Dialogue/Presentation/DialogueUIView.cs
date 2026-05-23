using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using Dialogue.Core;
using Dialogue.Data;
using Dialogue.Audio;
using Ink.Runtime;

namespace Dialogue.Presentation
{
    // Responsabilidad única: mostrar el estado del diálogo en pantalla.
    // No conoce a Ink. Solo escucha eventos del DialogueRunner
    // y actualiza la UI en consecuencia.
    // 📌 GDD — Panel inferior estilo Point & Click clásico.

    public sealed class DialogueUIView : MonoBehaviour
    {
        // ─── Dependencias ─────────────────────────────────────────────

        [Header("Runner")]
        [SerializeField] private DialogueRunner _runner;

        [Header("Panel")]
        [SerializeField] private GameObject _dialoguePanel;

        [Header("Textos")]
        [SerializeField] private TextMeshProUGUI _speakerNameText;
        [SerializeField] private TextMeshProUGUI _dialogueText;

        [Header("Opciones")]
        [SerializeField] private Transform             _choicesContainer;
        [SerializeField] private DialogueChoiceButton  _choiceButtonPrefab;

        [Header("Typewriter")]
        [SerializeField] private TypewriterEffect _typewriter;

        // ─── Pool de botones ──────────────────────────────────────────

        // 🟢 POOL — Evita GC al generar/destruir botones de opciones.
        // En un Point & Click las opciones aparecen y desaparecen
        // frecuentemente — el pool es imprescindible aquí.
        private ObjectPool<DialogueChoiceButton> _choiceButtonPool;

        // Botones actualmente visibles en pantalla.
        // Los guardamos para devolverlos al pool cuando terminen.
        private readonly List<DialogueChoiceButton> _activeChoiceButtons = new();

        private CancellationTokenSource _viewCts;


        // ─── Lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            ValidateDependencies();
            InitializePool();
            HidePanel();
        }

        private void OnEnable()
        {
            _runner.OnLineReady     += HandleLineReady;
            _runner.OnChoicesReady  += HandleChoicesReady;
            _runner.OnDialogueEnded += HandleDialogueEnded;
        }

        private void OnDisable()
        {
            _runner.OnLineReady     -= HandleLineReady;
            _runner.OnChoicesReady  -= HandleChoicesReady;
            _runner.OnDialogueEnded -= HandleDialogueEnded;
            CancelViewToken();
        }

        // ─── Handlers de eventos ──────────────────────────────────────

        private void HandleLineReady(DialogueLine line)
        {
            ShowPanel();
            ReturnAllChoicesToPool();

            // Si no hay speaker, ocultamos el nombre.
            // Útil para narración sin personaje.
            if (string.IsNullOrEmpty(line.Speaker))
            {
                _speakerNameText.gameObject.SetActive(false);
            }
            else
            {
                _speakerNameText.gameObject.SetActive(true);
                _speakerNameText.text = line.Speaker;
                AudioManager.Instance?.PlayVoice(line.Speaker); // voz por speaker
            }

            // 📌 GDD — Aqui se conecta el TypewriterEffect.
            PlayTypewriterAsync(line.Text).Forget(); // 🔵 fire-and-forget controlado

        }

        private void HandleChoicesReady(List<Choice> choices)
        {
            ShowPanel();
            ReturnAllChoicesToPool();

            // Ocultamos el texto de diálogo mientras se muestran opciones.
            // El jugador debe elegir antes de continuar.
            _dialogueText.text = string.Empty;
            _speakerNameText.gameObject.SetActive(false);

            for (int i = 0; i < choices.Count; i++)
            {
                // Capturamos el índice para el closure del botón.
                // Sin esto todos los botones llamarían con el último valor de i.
                int choiceIndex = i;

                DialogueChoiceButton button = _choiceButtonPool.Get();
                button.transform.SetParent(_choicesContainer, false);
                button.Initialize(
                    choices[i].text,
                    () => OnChoiceSelected(choiceIndex));
                button.Show();

                _activeChoiceButtons.Add(button);
            }
        }

        private void HandleDialogueEnded()
        {
            CancelViewToken();
            ReturnAllChoicesToPool();
            HidePanel();
        }

        // ─── Input del jugador ────────────────────────────────────────

        // Llamado desde un botón "Continuar" en la UI
        // o desde el InputReader en P3.
        public void OnContinuePressed()
        {
            // Solo avanzamos si no hay opciones activas.
            // Si hay opciones, el jugador debe elegir una.
            if (_activeChoiceButtons.Count > 0) return;
            if (!_runner.IsRunning) return;

            // Si el typewriter está corriendo, skip — no avanzamos todavía
            if (_typewriter.IsPlaying)
            {
                _typewriter.Skip();
                return;
            }

            _runner.Continue();
        }

        // ── Typewriter ───────────────────────────────────────────

        private async UniTaskVoid PlayTypewriterAsync(string text)
        {
            CancelViewToken();
            _viewCts = new CancellationTokenSource();

            await _typewriter.PlayAsync(_dialogueText, text, _viewCts.Token);
        }

         // ── Opciones ─────────────────────────────────────────────

        private void OnChoiceSelected(int choiceIndex)
        {
            ReturnAllChoicesToPool();
            _runner.ChooseOption(choiceIndex);
        }

        // ─── Pool ─────────────────────────────────────────────────────

        private void InitializePool()
        {
            // 🟢 POOL — defaultCapacity 4: suficiente para un Point & Click
            // donde raramente hay más de 4 opciones simultáneas.
            _choiceButtonPool = new ObjectPool<DialogueChoiceButton>(
                createFunc:      CreateChoiceButton,
                actionOnGet:     btn => btn.Show(),
                actionOnRelease: btn => btn.Hide(),
                actionOnDestroy: btn =>
                {
                    if (btn != null)
                        Destroy(btn.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 4,
                maxSize:         8);
        }

        private DialogueChoiceButton CreateChoiceButton()
        {
            // 🟢 POOL — Solo instanciamos cuando el pool necesita
            // un objeto nuevo. En steady state no hay allocations.
            DialogueChoiceButton button = Instantiate(
                _choiceButtonPrefab,
                _choicesContainer);

            button.Hide();
            return button;
        }

        private void ReturnAllChoicesToPool()
        {
            foreach (DialogueChoiceButton button in _activeChoiceButtons)
                _choiceButtonPool.Release(button);

            // 🟡 PERF — Clear() no genera GC en List<T>.
            _activeChoiceButtons.Clear();
        }

        // ─── Helpers de visibilidad ───────────────────────────────────

         private void CancelViewToken()
        {
            _viewCts?.Cancel();
            _viewCts?.Dispose();
            _viewCts = null;
        }

        private void ShowPanel() => _dialoguePanel.SetActive(true);
        private void HidePanel() => _dialoguePanel.SetActive(false);

        // ─── Validación ───────────────────────────────────────────────

        private void ValidateDependencies()
        {
            if (_runner == null)
                Debug.LogError(
                    $"[DialogueUIView] _runner no asignado en {gameObject.name}.");

            if (_dialoguePanel == null)
                Debug.LogError(
                    $"[DialogueUIView] _dialoguePanel no asignado en {gameObject.name}.");

            if (_speakerNameText == null)
                Debug.LogError(
                    $"[DialogueUIView] _speakerNameText no asignado en {gameObject.name}.");

            if (_dialogueText == null)
                Debug.LogError(
                    $"[DialogueUIView] _dialogueText no asignado en {gameObject.name}.");

            if (_choicesContainer == null)
                Debug.LogError(
                    $"[DialogueUIView] _choicesContainer no asignado en {gameObject.name}.");

            if (_choiceButtonPrefab == null)
                Debug.LogError(
                    $"[DialogueUIView] _choiceButtonPrefab no asignado en {gameObject.name}.");
        }
    }
}
