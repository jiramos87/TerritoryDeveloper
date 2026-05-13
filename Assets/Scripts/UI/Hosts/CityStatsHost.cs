using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 5.0 (TECH-32925) — MonoBehaviour Host for city-stats HUD panel.
    /// Wires city KPIs; stub — wire EconomyManager + CityStats in next pass.
    /// </summary>
    public sealed class CityStatsHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        CityStatsVM _vm;
        ModalCoordinator _coordinator;

        void OnEnable()
        {
            _vm = new CityStatsVM();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = _vm;
            else
                Debug.LogWarning("[CityStatsHost] UIDocument or rootVisualElement null on enable.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("city-stats", _doc.rootVisualElement);
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.dataSource = null;
        }

        /// <summary>Update all KPIs from external systems.</summary>
        public void SetStats(int population, int happiness, int funds, int day)
        {
            if (_vm == null) return;
            _vm.Population = population;
            _vm.Happiness = happiness;
            _vm.Funds = funds;
            _vm.Day = day;
        }
    }
}
