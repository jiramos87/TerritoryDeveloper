using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32923) — MonoBehaviour Host for overlay-toggle-strip HUD panel.
    /// Wires toggle change callbacks; stub — wire OverlayManager in next pass.
    /// </summary>
    public sealed class OverlayToggleStripHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        OverlayToggleStripVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new OverlayToggleStripVM();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[OverlayToggleStripHost] UIDocument or rootVisualElement null on enable.");
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }
    }
}
