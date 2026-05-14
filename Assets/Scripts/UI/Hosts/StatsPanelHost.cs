using Territory.Audio;
using Territory.Economy;
using Territory.Simulation;
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
    /// Iter-12 (Effort 1 §16.3) — tab switching swaps real content per tab; chart renders
    /// from StatsHistoryRecorder; 2-col rows populated from CityStats + Recorder snapshots.
    /// </summary>
    public sealed class StatsPanelHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        StatsPanelVM _vm;
        ModalCoordinator _coordinator;
        CityStats _cityStats;
        StatsHistoryRecorder _historyRecorder;

        Button _btnClose, _tabPop, _tabServices, _tabEcon;
        VisualElement _viewPop, _viewServices, _viewEcon;
        VisualElement _chartPop, _chartServices, _chartEcon;
        VisualElement _rowsPop, _rowsServices, _rowsEcon;

        float _refreshTimer;
        const float RefreshIntervalSec = 0.5f;

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

                _viewPop      = root.Q<VisualElement>("population-view");
                _viewServices = root.Q<VisualElement>("services-view");
                _viewEcon     = root.Q<VisualElement>("economy-view");

                _chartPop      = root.Q<VisualElement>("population-chart");
                _chartServices = root.Q<VisualElement>("services-chart");
                _chartEcon     = root.Q<VisualElement>("economy-chart");

                _rowsPop      = root.Q<VisualElement>("population-rows");
                _rowsServices = root.Q<VisualElement>("services-rows");
                _rowsEcon     = root.Q<VisualElement>("economy-rows");

                if (_btnClose    != null) _btnClose.clicked    += OnClose;
                if (_tabPop      != null) _tabPop.clicked      += () => OnTabSelected(StatsPanelVM.StatsTab.Population);
                if (_tabServices != null) _tabServices.clicked += () => OnTabSelected(StatsPanelVM.StatsTab.Services);
                if (_tabEcon     != null) _tabEcon.clicked     += () => OnTabSelected(StatsPanelVM.StatsTab.Economy);

                // iter-23 (Effort 2) — hover + click blips on close + 3 tabs.
                ToolkitBlipBinder.BindClickAndHover(_btnClose,    BlipId.UiButtonClick, BlipId.UiButtonHover);
                ToolkitBlipBinder.BindClickAndHover(_tabPop,      BlipId.UiButtonClick, BlipId.UiButtonHover);
                ToolkitBlipBinder.BindClickAndHover(_tabServices, BlipId.UiButtonClick, BlipId.UiButtonHover);
                ToolkitBlipBinder.BindClickAndHover(_tabEcon,     BlipId.UiButtonClick, BlipId.UiButtonHover);
            }
            else
                Debug.LogWarning("[StatsPanelHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("stats-panel", _doc.rootVisualElement);

            _cityStats = FindObjectOfType<CityStats>();
            _historyRecorder = FindObjectOfType<StatsHistoryRecorder>();
            OnTabSelected(StatsPanelVM.StatsTab.Population);
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
            if (_cityStats == null) _cityStats = FindObjectOfType<CityStats>();
            if (_historyRecorder == null) _historyRecorder = FindObjectOfType<StatsHistoryRecorder>();
        }

        void Update()
        {
            // Only refresh when panel is visible.
            if (_doc == null || _doc.rootVisualElement == null) return;
            var root = _doc.rootVisualElement;
            if (root.style.display.value == DisplayStyle.None) return;

            _refreshTimer += Time.unscaledDeltaTime;
            if (_refreshTimer < RefreshIntervalSec) return;
            _refreshTimer = 0f;
            RefreshActiveTabContent();
        }

        void OnDisable()
        {
            if (_btnClose != null) _btnClose.clicked -= OnClose;
            ToolkitBlipBinder.UnbindAll(_btnClose);
            ToolkitBlipBinder.UnbindAll(_tabPop);
            ToolkitBlipBinder.UnbindAll(_tabServices);
            ToolkitBlipBinder.UnbindAll(_tabEcon);
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

            SetActiveTabClass(_tabPop,      tab == StatsPanelVM.StatsTab.Population);
            SetActiveTabClass(_tabServices, tab == StatsPanelVM.StatsTab.Services);
            SetActiveTabClass(_tabEcon,     tab == StatsPanelVM.StatsTab.Economy);

            SetDisplay(_viewPop,      tab == StatsPanelVM.StatsTab.Population);
            SetDisplay(_viewServices, tab == StatsPanelVM.StatsTab.Services);
            SetDisplay(_viewEcon,     tab == StatsPanelVM.StatsTab.Economy);

            RefreshActiveTabContent();
        }

        void RefreshActiveTabContent()
        {
            if (_vm == null) return;
            switch (_vm.ActiveTab)
            {
                case StatsPanelVM.StatsTab.Population: RenderPopulation(); break;
                case StatsPanelVM.StatsTab.Services:   RenderServices();   break;
                case StatsPanelVM.StatsTab.Economy:    RenderEconomy();    break;
            }
        }

        // ── Population tab ────────────────────────────────────────────────────
        void RenderPopulation()
        {
            RenderLineChart(_chartPop, "population", colorEconomy: false);
            ClearRows(_rowsPop);
            if (_cityStats == null) return;
            AddRow(_rowsPop, "City",       _cityStats.cityName ?? "—");
            AddRow(_rowsPop, "Date",       _cityStats.currentDate.ToString("yyyy-MM"));
            AddRow(_rowsPop, "Population", FormatInt(_cityStats.population));
            AddRow(_rowsPop, "Happiness",  $"{_cityStats.happiness:F0} / 100");
            AddRow(_rowsPop, "Pollution",  $"{_cityStats.pollution:F0}");
            AddRow(_rowsPop, "R / C / I zones",
                $"{_cityStats.residentialZoneCount} / {_cityStats.commercialZoneCount} / {_cityStats.industrialZoneCount}");
            AddRow(_rowsPop, "R / C / I buildings",
                $"{_cityStats.residentialBuildingCount} / {_cityStats.commercialBuildingCount} / {_cityStats.industrialBuildingCount}");
            AddRow(_rowsPop, "Forest cover", $"{_cityStats.forestCoveragePercentage:F1}%");
        }

        // ── Services tab ──────────────────────────────────────────────────────
        void RenderServices()
        {
            if (_chartServices == null) return;
            _chartServices.Clear();
            // 10 service rows. Saturation 0..1 from StatsHistoryRecorder snapshot
            // (proxied to happiness for managers not yet wired).
            var services = new (string label, string seriesId)[]
            {
                ("Power",     "svc.power"),
                ("Water",     "svc.water"),
                ("Waste",     "svc.waste"),
                ("Police",    "svc.police"),
                ("Fire",      "svc.fire"),
                ("Health",    "svc.health"),
                ("Education", "svc.education"),
                ("Parks",     "svc.parks"),
                ("Transit",   "svc.transit"),
                ("Roads",     "svc.roads"),
            };
            foreach (var (label, sid) in services)
            {
                float v = _historyRecorder != null ? _historyRecorder.GetCurrentSnapshot(sid) : 0f;
                _chartServices.Add(BuildServiceBarRow(label, Mathf.Clamp01(v)));
            }

            ClearRows(_rowsServices);
            if (_cityStats != null)
            {
                AddRow(_rowsServices, "Power supply",    FormatInt(_cityStats.cityPowerOutput));
                AddRow(_rowsServices, "Power demand",    FormatInt(_cityStats.cityPowerConsumption));
                AddRow(_rowsServices, "Water supply",    FormatInt(_cityStats.cityWaterOutput));
                AddRow(_rowsServices, "Water demand",    FormatInt(_cityStats.cityWaterConsumption));
                AddRow(_rowsServices, "Roads",           FormatInt(_cityStats.roadCount));
                AddRow(_rowsServices, "Forest cells",    FormatInt(_cityStats.forestCellCount));
                AddRow(_rowsServices, "Happiness",       $"{_cityStats.happiness:F0}");
                AddRow(_rowsServices, "Pollution",       $"{_cityStats.pollution:F0}");
            }
        }

        // ── Economy tab ───────────────────────────────────────────────────────
        void RenderEconomy()
        {
            RenderLineChart(_chartEcon, "economy", colorEconomy: true);
            ClearRows(_rowsEcon);
            if (_cityStats == null) return;
            AddRow(_rowsEcon, "Money",   "$" + FormatInt(_cityStats.money));
            AddRow(_rowsEcon, "R zones", FormatInt(_cityStats.residentialZoneCount));
            AddRow(_rowsEcon, "C zones", FormatInt(_cityStats.commercialZoneCount));
            AddRow(_rowsEcon, "I zones", FormatInt(_cityStats.industrialZoneCount));
            AddRow(_rowsEcon, "Total buildings",
                FormatInt(_cityStats.residentialBuildingCount + _cityStats.commercialBuildingCount + _cityStats.industrialBuildingCount));
            AddRow(_rowsEcon, "Communes", FormatInt(_cityStats.communes != null ? _cityStats.communes.Count : 0));
            float income = 0f;
            if (_historyRecorder != null) income = _historyRecorder.GetCurrentSnapshot("economy");
            AddRow(_rowsEcon, "Income / mo", "$" + FormatInt(Mathf.RoundToInt(income)));
        }

        // ── Chart + row helpers ───────────────────────────────────────────────
        void RenderLineChart(VisualElement chart, string seriesId, bool colorEconomy)
        {
            if (chart == null) return;
            chart.Clear();
            float[] series = _historyRecorder != null
                ? _historyRecorder.GetRange("12mo", seriesId)
                : System.Array.Empty<float>();
            if (series.Length == 0) return;
            float max = 1f;
            for (int i = 0; i < series.Length; i++) if (series[i] > max) max = series[i];

            for (int i = 0; i < series.Length; i++)
            {
                var col = new VisualElement();
                col.AddToClassList("stats-panel__chart-line-col");
                var fill = new VisualElement();
                fill.AddToClassList("stats-panel__chart-line-fill");
                if (colorEconomy) fill.AddToClassList("stats-panel__chart-line-fill--economy");
                float pct = Mathf.Clamp01(series[i] / max);
                fill.style.height = new StyleLength(new Length(pct * 100f, LengthUnit.Percent));
                col.Add(fill);
                chart.Add(col);
            }
        }

        static VisualElement BuildServiceBarRow(string label, float saturation01)
        {
            var row = new VisualElement();
            row.AddToClassList("stats-panel__chart-bar-row");
            var lbl = new Label(label);
            lbl.AddToClassList("stats-panel__chart-bar-label");
            row.Add(lbl);
            var track = new VisualElement();
            track.AddToClassList("stats-panel__chart-bar-track");
            var fill = new VisualElement();
            fill.AddToClassList("stats-panel__chart-bar-fill");
            fill.style.width = new StyleLength(new Length(Mathf.Clamp01(saturation01) * 100f, LengthUnit.Percent));
            track.Add(fill);
            row.Add(track);
            return row;
        }

        void ClearRows(VisualElement container) { if (container != null) container.Clear(); }

        void AddRow(VisualElement container, string label, string value)
        {
            if (container == null) return;
            var row = new VisualElement();
            row.AddToClassList("stats-panel__row");
            var lbl = new Label(label); lbl.AddToClassList("stats-panel__row-label");
            var val = new Label(value); val.AddToClassList("stats-panel__row-value");
            row.Add(lbl); row.Add(val);
            container.Add(row);
        }

        static void SetActiveTabClass(Button btn, bool active)
        {
            if (btn == null) return;
            btn.EnableInClassList("stats-panel__tab--active", active);
        }

        static void SetDisplay(VisualElement ve, bool show)
        {
            if (ve == null) return;
            ve.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        static string FormatInt(int v) => v.ToString("N0");
    }
}
