using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 4.0 (TECH-32919) — MonoBehaviour Host for stats-panel UI Toolkit migration.
    /// Wires VM + chart VisualElement update cadence.
    /// Registers panel slug in ModalCoordinator migrated branch.
    /// Legacy StatsPanelAdapter remains alive until Stage 6 quarantine.
    /// </summary>
    public sealed class StatsPanelHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        StatsPanelVM _vm;
        ModalCoordinator _coordinator;
        Button _btnClose, _tabPop, _tabServices, _tabEcon;

        void OnEnable()
        {
            _vm = new StatsPanelVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
            {
                _doc.rootVisualElement.SetCompatDataSource(_vm);
                var root = _doc.rootVisualElement;
                _btnClose    = root.Q<Button>("btn-close");
                _tabPop      = root.Q<Button>("tab-population");
                _tabServices = root.Q<Button>("tab-services");
                _tabEcon     = root.Q<Button>("tab-economy");
                if (_btnClose    != null) _btnClose.clicked    += OnClose;
                if (_tabPop      != null) _tabPop.clicked      += () => OnTabSelected(StatsPanelVM.StatsTab.Population);
                if (_tabServices != null) _tabServices.clicked += () => OnTabSelected(StatsPanelVM.StatsTab.Services);
                if (_tabEcon     != null) _tabEcon.clicked     += () => OnTabSelected(StatsPanelVM.StatsTab.Economy);
            }
            else
                Debug.LogWarning("[StatsPanelHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("stats-panel", _doc.rootVisualElement);
        }
        void Start()
        {
            // Iter-7: retry registration when ModalCoordinator is created
            // by UIManager.Start (runs after Host.OnEnable).
            if (_coordinator == null)
            {
                _coordinator = FindObjectOfType<ModalCoordinator>();
                if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                    _coordinator.RegisterMigratedPanel("stats-panel", _doc.rootVisualElement);
            }
        }


        void OnDisable()
        {
            if (_btnClose != null) _btnClose.clicked -= OnClose;
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void WireCommands()
        {
            _vm.CloseCommand = OnClose;
            _vm.SelectPopulationTab = () => OnTabSelected(StatsPanelVM.StatsTab.Population);
            _vm.SelectServicesTab = () => OnTabSelected(StatsPanelVM.StatsTab.Services);
            _vm.SelectEconomyTab = () => OnTabSelected(StatsPanelVM.StatsTab.Economy);
        }

        void OnClose()
        {
            if (_coordinator != null)
                _coordinator.HideMigrated("stats-panel");
            else
                gameObject.SetActive(false);
        }

        void OnTabSelected(StatsPanelVM.StatsTab tab)
        {
            if (_vm == null) return;
            _vm.ActiveTab = tab;
            Debug.Log($"[StatsPanelHost] Tab selected: {tab}");
            // Chart update cadence hook — stub; wire StatsHistoryRecorder data push here.
        }
    }
}
