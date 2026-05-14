using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32923) — MonoBehaviour Host for mini-map HUD panel.
    /// Displays minimap thumbnail; stub — wire MapRenderService in next pass.
    /// </summary>
    public sealed class MiniMapHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        MiniMapVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new MiniMapVM();

            if (_doc != null && _doc.rootVisualElement != null)
            {
                var rootEl = _doc.rootVisualElement;
                rootEl.style.position = Position.Absolute;
                rootEl.style.top = 0;
                rootEl.style.left = 0;
                rootEl.style.right = 0;
                rootEl.style.bottom = 0;
                rootEl.pickingMode = PickingMode.Ignore;
                rootEl.SetCompatDataSource(_vm);
            }
            else
                Debug.LogWarning("[MiniMapHost] UIDocument or rootVisualElement null on enable.");
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        /// <summary>Update cursor coordinates from world input.</summary>
        public void SetCursor(int x, int y)
        {
            if (_vm == null) return;
            _vm.CursorX = x;
            _vm.CursorY = y;
        }
    }
}
