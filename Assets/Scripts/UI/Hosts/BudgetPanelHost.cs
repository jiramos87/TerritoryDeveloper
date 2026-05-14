using Territory.Core;
using Territory.Economy;
using Territory.Simulation;
using Territory.UI.Modals;
using Territory.UI.ViewModels;
using Territory.Zones;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// Stage 4.0 (TECH-32917) — Host for budget-panel UI Toolkit migration.
    /// Effort 8 (post iter-31): live tax slider wiring + appended auto-growth strip.
    ///   - Tax sliders (residential / commercial / industrial) route ValueChanged →
    ///     EconomyManager.SetTaxRate per zone class; HUD surplus reflects on next tick.
    ///   - Apply commits + closes; Cancel restores VM from EconomyManager snapshot + closes.
    ///   - Auto-growth strip appended at runtime: 2 sliders (AUTO road / AUTO zoning,
    ///     0..100% of monthly available) drive GrowthBudgetManager.SetCategoryPercent.
    /// </summary>
    public sealed class BudgetPanelHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        BudgetPanelVM _vm;
        ModalCoordinator _coordinator;
        EconomyManager _economy;
        GrowthBudgetManager _growth;
        CityStats _cityStats;

        Slider _taxR, _taxC, _taxI;
        Label _treasuryLabel;
        Label _taxRValue, _taxCValue, _taxIValue;
        Slider _autoRoadSlider, _autoZoneSlider;
        Label _autoRoadLabel, _autoZoneLabel;

        int _initialTaxR, _initialTaxC, _initialTaxI;
        int _initialRoadPct, _initialZonePct;

        void OnEnable()
        {
            _vm = new BudgetPanelVM();
            WireCommands();

            if (_doc != null && _doc.rootVisualElement != null)
            {
                var rootEl = _doc.rootVisualElement;
                rootEl.style.position = Position.Absolute;
                rootEl.style.top = 0; rootEl.style.left = 0;
                rootEl.style.right = 0; rootEl.style.bottom = 0;
                rootEl.pickingMode = PickingMode.Ignore;
                rootEl.SetCompatDataSource(_vm);
            }
            else
                Debug.LogWarning("[BudgetPanelHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");

            _coordinator = FindObjectOfType<ModalCoordinator>();
            if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                _coordinator.RegisterMigratedPanel("budget-panel", _doc.rootVisualElement);

            _economy = FindObjectOfType<EconomyManager>();
            _growth = FindObjectOfType<GrowthBudgetManager>();
            _cityStats = FindObjectOfType<CityStats>();

            if (_doc != null && _doc.rootVisualElement != null)
            {
                var root = _doc.rootVisualElement;
                _treasuryLabel = root.Q<Label>("treasury");
                _taxR = root.Q<Slider>("tax-residential");
                _taxC = root.Q<Slider>("tax-commercial");
                _taxI = root.Q<Slider>("tax-industrial");
                // Tax value labels live next to each slider in the same row — Q by class + position.
                var sections = root.Q<VisualElement>("tax-section");
                if (sections != null)
                {
                    var valueLabels = sections.Query<Label>(className: "budget-panel__row-value").ToList();
                    if (valueLabels.Count >= 1) _taxRValue = valueLabels[0];
                    if (valueLabels.Count >= 2) _taxCValue = valueLabels[1];
                    if (valueLabels.Count >= 3) _taxIValue = valueLabels[2];
                }
                if (_taxR != null) _taxR.RegisterValueChangedCallback(OnTaxRChanged);
                if (_taxC != null) _taxC.RegisterValueChangedCallback(OnTaxCChanged);
                if (_taxI != null) _taxI.RegisterValueChangedCallback(OnTaxIChanged);

                SeedSlidersFromManagers();
                BuildAutoGrowthStrip(root);
            }
        }

        void Start()
        {
            if (_coordinator == null)
            {
                _coordinator = FindObjectOfType<ModalCoordinator>();
                if (_coordinator != null && _doc != null && _doc.rootVisualElement != null)
                    _coordinator.RegisterMigratedPanel("budget-panel", _doc.rootVisualElement);
            }
        }

        void OnDisable()
        {
            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void WireCommands()
        {
            _vm.CloseCommand = OnClose;
            _vm.ApplyCommand = OnApply;
            _vm.CancelCommand = OnCancel;
        }

        void SeedSlidersFromManagers()
        {
            if (_economy != null)
            {
                _initialTaxR = _economy.GetResidentialTax();
                _initialTaxC = _economy.GetCommercialTax();
                _initialTaxI = _economy.GetIndustrialTax();
                if (_taxR != null) _taxR.SetValueWithoutNotify(_initialTaxR);
                if (_taxC != null) _taxC.SetValueWithoutNotify(_initialTaxC);
                if (_taxI != null) _taxI.SetValueWithoutNotify(_initialTaxI);
            }
            if (_growth != null)
            {
                _initialRoadPct = _growth.GetCategoryPercent(GrowthCategory.Roads);
                _initialZonePct = _growth.GetCategoryPercent(GrowthCategory.Zoning);
            }
        }

        void OnTaxRChanged(ChangeEvent<float> e)
        {
            if (_economy == null) return;
            _economy.SetTaxRate(Zone.ZoneType.ResidentialLightZoning, Mathf.RoundToInt(e.newValue));
        }
        void OnTaxCChanged(ChangeEvent<float> e)
        {
            if (_economy == null) return;
            _economy.SetTaxRate(Zone.ZoneType.CommercialLightZoning, Mathf.RoundToInt(e.newValue));
        }
        void OnTaxIChanged(ChangeEvent<float> e)
        {
            if (_economy == null) return;
            _economy.SetTaxRate(Zone.ZoneType.IndustrialLightZoning, Mathf.RoundToInt(e.newValue));
        }

        void BuildAutoGrowthStrip(VisualElement root)
        {
            var host = root.Q<VisualElement>("budget-panel") ?? root;
            // Append a new section between forecast + actions.
            var section = new VisualElement { name = "auto-growth-section" };
            section.AddToClassList("budget-panel__section");
            section.style.marginTop = 8f;

            var header = new Label("Auto-growth");
            header.AddToClassList("budget-panel__section-header");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = Hex("#3a2f1c");
            section.Add(header);

            section.Add(BuildPctRow("AUTO road", out _autoRoadSlider, out _autoRoadLabel, _initialRoadPct, OnAutoRoadChanged));
            section.Add(BuildPctRow("AUTO zoning", out _autoZoneSlider, out _autoZoneLabel, _initialZonePct, OnAutoZoneChanged));

            // Insert before actions row if present, else append.
            var actions = host.Q<VisualElement>("actions");
            if (actions != null && actions.parent != null)
            {
                var parent = actions.parent;
                int idx = parent.IndexOf(actions);
                parent.Insert(idx, section);
            }
            else host.Add(section);
        }

        VisualElement BuildPctRow(string label, out Slider slider, out Label valueLabel, int initialPct,
                                  System.Action<int> onChange)
        {
            var row = new VisualElement();
            row.AddToClassList("budget-panel__row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 4f;

            var name = new Label(label);
            name.style.color = Hex("#3a2f1c");
            name.style.fontSize = 12f;
            name.style.minWidth = 110f;
            row.Add(name);

            slider = new Slider(0, 100) { value = Mathf.Clamp(initialPct, 0, 100) };
            slider.style.flexGrow = 1f;
            slider.style.flexShrink = 1f;
            slider.style.maxWidth = 260f;
            row.Add(slider);

            valueLabel = new Label($"{Mathf.RoundToInt(slider.value)}%");
            valueLabel.style.minWidth = 40f;
            valueLabel.style.color = Hex("#6b5a3d");
            valueLabel.style.fontSize = 12f;
            row.Add(valueLabel);

            var capturedLbl = valueLabel;
            slider.RegisterValueChangedCallback(e =>
            {
                int pct = Mathf.RoundToInt(e.newValue);
                capturedLbl.text = $"{pct}%";
                onChange?.Invoke(pct);
            });
            return row;
        }

        void OnAutoRoadChanged(int pct)
        {
            if (_growth != null) _growth.SetCategoryPercent(GrowthCategory.Roads, pct);
        }
        void OnAutoZoneChanged(int pct)
        {
            if (_growth != null) _growth.SetCategoryPercent(GrowthCategory.Zoning, pct);
        }

        void OnClose()
        {
            if (_coordinator != null) _coordinator.HideMigrated("budget-panel");
            else gameObject.SetActive(false);
        }

        void OnApply()
        {
            // Sliders already pushed live changes — Apply just records baseline + closes.
            if (_economy != null)
            {
                _initialTaxR = _economy.GetResidentialTax();
                _initialTaxC = _economy.GetCommercialTax();
                _initialTaxI = _economy.GetIndustrialTax();
            }
            if (_growth != null)
            {
                _initialRoadPct = _growth.GetCategoryPercent(GrowthCategory.Roads);
                _initialZonePct = _growth.GetCategoryPercent(GrowthCategory.Zoning);
            }
            GameNotificationManager.Instance?.PostSuccess("Budget applied");
            OnClose();
        }

        void Update()
        {
            if (_treasuryLabel != null && _cityStats != null)
                _treasuryLabel.text = $"${_cityStats.money:N0}";
            if (_taxRValue != null && _taxR != null) _taxRValue.text = $"{Mathf.RoundToInt(_taxR.value)}%";
            if (_taxCValue != null && _taxC != null) _taxCValue.text = $"{Mathf.RoundToInt(_taxC.value)}%";
            if (_taxIValue != null && _taxI != null) _taxIValue.text = $"{Mathf.RoundToInt(_taxI.value)}%";
        }

        void OnCancel()
        {
            // Restore initial values on cancel.
            if (_economy != null)
            {
                _economy.SetTaxRate(Zone.ZoneType.ResidentialLightZoning, _initialTaxR);
                _economy.SetTaxRate(Zone.ZoneType.CommercialLightZoning, _initialTaxC);
                _economy.SetTaxRate(Zone.ZoneType.IndustrialLightZoning, _initialTaxI);
                if (_taxR != null) _taxR.SetValueWithoutNotify(_initialTaxR);
                if (_taxC != null) _taxC.SetValueWithoutNotify(_initialTaxC);
                if (_taxI != null) _taxI.SetValueWithoutNotify(_initialTaxI);
            }
            if (_growth != null)
            {
                _growth.SetCategoryPercent(GrowthCategory.Roads, _initialRoadPct);
                _growth.SetCategoryPercent(GrowthCategory.Zoning, _initialZonePct);
                if (_autoRoadSlider != null) _autoRoadSlider.SetValueWithoutNotify(_initialRoadPct);
                if (_autoZoneSlider != null) _autoZoneSlider.SetValueWithoutNotify(_initialZonePct);
                if (_autoRoadLabel != null) _autoRoadLabel.text = $"{_initialRoadPct}%";
                if (_autoZoneLabel != null) _autoZoneLabel.text = $"{_initialZonePct}%";
            }
            OnClose();
        }

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    }
}
