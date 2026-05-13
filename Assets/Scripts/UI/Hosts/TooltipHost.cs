using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32924) — MonoBehaviour Host for tooltip popover panel.
    /// Positions tooltip near cursor; shows/hides on demand.
    /// </summary>
    public sealed class TooltipHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        TooltipVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new TooltipVM();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[TooltipHost] UIDocument or rootVisualElement null on enable.");
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        /// <summary>Show tooltip with label + optional body text at screen position.</summary>
        public void ShowAt(string label, string text, float screenX, float screenY)
        {
            if (_vm == null) return;
            _vm.Label = label;
            _vm.Text = text;
            _vm.PositionX = screenX;
            _vm.PositionY = screenY;
            if (_coordinator != null)
                _coordinator.Show("tooltip");
        }

        public void Hide()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("tooltip");
        }
    }
}
