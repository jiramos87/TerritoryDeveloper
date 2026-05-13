using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32923) — MonoBehaviour Host for zone-overlay HUD panel.
    /// Wires zone legend + active toggle; stub — wire ZoneService in next pass.
    /// </summary>
    public sealed class ZoneOverlayHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        ZoneOverlayVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new ZoneOverlayVM();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[ZoneOverlayHost] UIDocument or rootVisualElement null on enable.");
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }
    }
}
