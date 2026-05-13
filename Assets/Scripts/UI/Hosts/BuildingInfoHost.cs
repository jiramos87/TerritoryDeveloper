using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32924) — MonoBehaviour Host for building-info modal.
    /// Wires building data + demolish; stub — wire BuildingService in next pass.
    /// </summary>
    public sealed class BuildingInfoHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        BuildingInfoVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new BuildingInfoVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(_vm);
            else
                Debug.LogWarning("[BuildingInfoHost] UIDocument or rootVisualElement null on enable.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("building-info", _doc.rootVisualElement);
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void WireCommands()
        {
            _vm.CloseCommand = OnClose;
            _vm.DemolishCommand = OnDemolish;
        }

        /// <summary>Show building info for a given entity.</summary>
        public void ShowForBuilding(string name, string type, int level, int residents, string condition = "Good")
        {
            if (_vm == null) return;
            _vm.BuildingName = name;
            _vm.BuildingType = type;
            _vm.Level = level;
            _vm.Residents = residents;
            _vm.Condition = condition;
            if (_coordinator != null)
                _coordinator.Show("building-info");
        }

        void OnClose()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("building-info");
            else
                gameObject.SetActive(false);
        }

        void OnDemolish()
        {
            Debug.Log($"[BuildingInfoHost] Demolish: {_vm?.BuildingName} — stub; wire BuildingService.");
            OnClose();
        }
    }
}
