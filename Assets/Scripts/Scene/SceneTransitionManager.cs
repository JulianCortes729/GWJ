// Assets/Scripts/Scene/SceneTransitionManager.cs
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Scene
{
    /// <summary>
    /// Singleton que maneja todas las transiciones entre escenas.
    /// Hace fade out → carga escena → fade in.
    /// Persiste entre escenas junto al AudioManager.
    /// </summary>
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        // ── Config ───────────────────────────────────────────────
        [Header("Fade")]
        [SerializeField] private CanvasGroup _fadeCanvasGroup;
        [SerializeField] private float       _fadeDuration = 0.5f;

        // ── Singleton ────────────────────────────────────────────
        public static SceneTransitionManager Instance { get; private set; }

        // ── Estado ───────────────────────────────────────────────
        private bool   _isTransitioning;
        private string _pendingSpawnId;                             // 📌 GDD — qué punto de entrada usar en la escena nueva

        public static string PendingSpawnId { get; private set; }  // leído por PlayerSpawnHandler al iniciar escena

        // ── Lifecycle ────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_fadeCanvasGroup == null)
                Debug.LogError("[SceneTransitionManager] _fadeCanvasGroup no asignado.", this);

            _fadeCanvasGroup.alpha          = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // ── API pública ──────────────────────────────────────────
        /// <summary>
        /// Inicia la transición hacia una nueva escena.
        /// spawnId identifica en qué PlayerSpawnPoint aparece el player.
        /// </summary>
        public void TransitionTo(string sceneName, string spawnId)
        {
            if (_isTransitioning) return;
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneTransitionManager] sceneName vacío.", this);
                return;
            }

            PendingSpawnId   = spawnId;
            _isTransitioning = true;

            TransitionAsync(sceneName).Forget();
        }

        // ── Privados ─────────────────────────────────────────────
        private async UniTaskVoid TransitionAsync(string sceneName)
        {
            // Bloquea input durante la transición
            _fadeCanvasGroup.blocksRaycasts = true;

            await FadeAsync(0f, 1f);                                // fade out — pantalla se pone negra

            await SceneManager.LoadSceneAsync(sceneName);           // carga la escena nueva
                                                                    // OnSceneLoaded se dispara aquí

            await FadeAsync(1f, 0f);                                // fade in — aparece la nueva escena

            _fadeCanvasGroup.blocksRaycasts = false;
            _isTransitioning = false;
        }

        private async UniTask FadeAsync(float from, float to)
        {
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed                    += Time.deltaTime;
                _fadeCanvasGroup.alpha      = Mathf.Lerp(from, to, elapsed / _fadeDuration);
                await UniTask.Yield(PlayerLoopTiming.Update);       // 🟢 zero GC — yield sin alloc
            }

            _fadeCanvasGroup.alpha = to;                            // garantiza valor exacto al terminar
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            // El PlayerSpawnHandler de la nueva escena lee PendingSpawnId en su Start()
            Debug.Log($"[SceneTransitionManager] Escena cargada: {scene.name} | SpawnId: {PendingSpawnId}");
        }
    }
}