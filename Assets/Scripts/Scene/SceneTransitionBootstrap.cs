// Assets/Scripts/Scene/SceneTransitionBootstrap.cs
using Interaction.Core;
using UnityEngine;

namespace Scene
{
    /// <summary>
    /// Conecta InteractionSystem con SceneTransitionManager.
    /// Responsabilidad única: registrar SceneTransitionInteractionHandler
    /// en InteractionSystem una vez que ambos sistemas están inicializados.
    ///
    /// Patrón idéntico al de DialogueSystemBootstrap — Start() garantiza
    /// que todos los Awake() ya corrieron antes de registrar el handler.
    /// </summary>
    public sealed class SceneTransitionBootstrap : MonoBehaviour
    {
        // ── Dependencias ─────────────────────────────────────────
        [SerializeField] private InteractionSystem _interactionSystem;

        // ── Lifecycle ─────────────────────────────────────────────
        private void Awake()
        {
            if (_interactionSystem == null)
                _interactionSystem = FindAnyObjectByType<InteractionSystem>(); // 🟡 PERF — solo en Awake

            ValidateDependencies();
        }

        private void Start()
        {
            // Start() garantiza que SceneTransitionManager.Instance ya existe
            // (su Awake corrió antes que este Start).
            if (SceneTransitionManager.Instance == null)
            {
                Debug.LogError(
                    "[SceneTransitionBootstrap] SceneTransitionManager.Instance es null. " +
                    "Asegurate de que el prefab del SceneTransitionManager esté en la escena.", this);
                return;
            }

            // Registramos el handler pasando un delegate al singleton.     // ⚠️ SOLID: DIP
            _interactionSystem.RegisterSceneTransitionHandler(
                (sceneName, spawnId) =>
                    SceneTransitionManager.Instance.TransitionTo(sceneName, spawnId)
            );

            Debug.Log("[SceneTransitionBootstrap] SceneTransitionHandler registrado.");
        }

        // ── Validación ────────────────────────────────────────────
        private void ValidateDependencies()
        {
            if (_interactionSystem == null)
                Debug.LogError("[SceneTransitionBootstrap] InteractionSystem no asignado.", this);
        }
    }
}