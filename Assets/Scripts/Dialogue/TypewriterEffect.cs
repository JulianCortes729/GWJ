using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using System.Threading;

namespace Dialogue.Presentation
{
    public sealed class TypewriterEffect : MonoBehaviour
    {
        [SerializeField] private float _secondsPerCharacter = 0.03f;

        private CancellationTokenSource _cts;
        private bool                    _isPlaying;

        public bool IsPlaying => _isPlaying;

        // ── Lifecycle ────────────────────────────────────────────

        private void OnDestroy()
        {
            CancelAndDispose();
        }

        // ── API pública ──────────────────────────────────────────

        public async UniTask PlayAsync(
            TextMeshProUGUI targetText,
            string          fullText,
            CancellationToken externalToken = default)
        {
            CancelAndDispose();

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            CancellationToken token = _cts.Token;

            targetText.text             = fullText;   // 🟡 PERF — TMP parsea tags una sola vez aquí
            targetText.ForceMeshUpdate();                              // 🟡 PERF — fuerza recálculo de textInfo en este frame

            targetText.maxVisibleCharacters = 0;

            _isPlaying = true;

            int totalCharacters = targetText.textInfo.characterCount; // 🟢 conteo real post-parse

            try
            {
                for (int i = 0; i <= totalCharacters; i++)
                {
                    token.ThrowIfCancellationRequested();

                    targetText.maxVisibleCharacters = i;

                    await UniTask.Delay(
                        System.TimeSpan.FromSeconds(_secondsPerCharacter),
                        ignoreTimeScale: false,
                        cancellationToken: token);
                }
            }
            catch (System.OperationCanceledException)
            {
                // Skip solicitado — mostramos todo el texto
                targetText.maxVisibleCharacters = int.MaxValue;
            }
            finally
            {
                _isPlaying = false;
                CancelAndDispose();
            }
        }

        /// <summary>
        /// Salta la animación en curso y muestra el texto completo de inmediato.
        /// No-op si no hay animación activa.
        /// </summary>
        public void Skip()
        {
            if (!_isPlaying) return;
            CancelAndDispose();
        }

        // ── Privados ─────────────────────────────────────────────

        private void CancelAndDispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
