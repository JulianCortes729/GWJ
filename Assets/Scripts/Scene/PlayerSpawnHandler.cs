// Assets/Scripts/Scene/PlayerSpawnHandler.cs
using UnityEngine;

namespace Scene
{
    /// <summary>
    /// Colocar en cada escena. Posiciona al player local en el spawn correcto.
    /// No necesita referencia al player — lo busca por tag en su propia escena.
    /// </summary>
    public sealed class PlayerSpawnHandler : MonoBehaviour
    {
        [SerializeField] private string _defaultSpawnId = "default";

        private void Start()
        {
            // Busca el player en esta escena por tag — no depende de referencias cruzadas
            var player = GameObject.FindWithTag("Player");          // 🟡 PERF — solo en Start, aceptable
            if (player == null)
            {
                Debug.LogError("[PlayerSpawnHandler] No se encontró GameObject con tag 'Player' en la escena.", this);
                return;
            }

            string spawnId = string.IsNullOrEmpty(SceneTransitionManager.PendingSpawnId)
                ? _defaultSpawnId
                : SceneTransitionManager.PendingSpawnId;

            var spawnPoints = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None);

            foreach (var point in spawnPoints)
            {
                if (point.SpawnId != spawnId) continue;

                player.transform.position = point.transform.position;
                Debug.Log($"[PlayerSpawnHandler] Player posicionado en spawn '{spawnId}'.");
                return;
            }

            Debug.LogWarning($"[PlayerSpawnHandler] SpawnPoint '{spawnId}' no encontrado. El player queda en su posición inicial.");
        }
    }
}