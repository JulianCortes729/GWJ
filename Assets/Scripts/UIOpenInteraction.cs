using UnityEngine;

namespace Journey.Interaction
{
    /// <summary>
    /// Utilidad rápida para testear apertura de UI desde el menú radial.
    /// </summary>
    public class UIOpenInteraction : MonoBehaviour
    {
        [SerializeField] private GameObject _panelToOpen;

        // Llamá a este método desde un UnityEvent en tu InteractableObject,
        // o conectalo directamente a un IInteractionHandler temporal.
        public void OpenPanel()
        {
            if (_panelToOpen != null)
                _panelToOpen.SetActive(true);
        }
    }
}
