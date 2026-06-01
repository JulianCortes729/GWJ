// Assets/Scripts/Scene/PlayerSpawnPoint.cs
using UnityEngine;

namespace Scene
{
    /// <summary>
    /// Marca un punto de aparición del player en la escena.
    /// Colocar uno por cada entrada posible (puerta norte, puerta sur, etc.).
    /// </summary>
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string _spawnId = "default";

        public string SpawnId => _spawnId;

        // Gizmo para ver el spawn point en el editor
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            Gizmos.DrawLine(
                transform.position,
                transform.position + Vector3.up * 0.5f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.6f,
                $"Spawn: {_spawnId}");
#endif
        }
    }
}
