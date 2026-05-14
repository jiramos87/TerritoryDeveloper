using Territory.Audio;
using Territory.Economy;
using Territory.Timing;
using Territory.UI.Modals;
using Territory.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Territory.UI.Hosts
{
    /// <summary>
    /// MonoBehaviour Host for hud-bar top-strip — pre-migration parity layout (3 clusters).
    /// Left: pause button + mini-map preview. Center: city name + pop/money + surplus caption.
    /// Right: zoom +/− buttons, money tall, AUTO toggle, MAP toggle.
    /// Wires every button.clicked to the real manager (TimeManager / CameraController /
    /// UIManager / ModalCoordinator). No `binding-path` attributes — Unity 2022 native data
    /// binding is not used in runtime mode.
    /// </summary>
    public sealed class HudBarHost : MonoBehaviour
    {
        [SerializeField] UIDocument _doc;

        [Header("Producers (Inspector first; Awake fallback)")]
        [SerializeField] CityStats _cityStats;
        [SerializeField] EconomyManager _economyManager;
        [SerializeField] TimeManager _timeManager;
        [SerializeField] CameraController _cameraController;
        [SerializeField] ModalCoordinator _modalCoordinator;
        [SerializeField] UIManager _uiManager;
        [SerializeField] MiniMapController _miniMapController;
        int _miniMapRefreshFrame;

        HudBarVM _vm;

        Label _cityNameLbl;
        Label _popLbl;
        Label _moneyLbl;
        Label _surplusLbl;
        Label _moneyTallLbl;
        Button _btnPause;
        Button _btnZoomIn;
        Button _btnZoomOut;
        Button _btnAuto;
        Button _btnMap;
        Button _btnStats;
        Button _btnSpeed1, _btnSpeed2, _btnSpeed3;
        VisualElement _miniMap;

        void Awake()
        {
            if (_cityStats == null)        _cityStats        = FindObjectOfType<CityStats>();
            if (_economyManager == null)   _economyManager   = FindObjectOfType<EconomyManager>();
            if (_timeManager == null)      _timeManager      = FindObjectOfType<TimeManager>();
            if (_cameraController == null) _cameraController = FindObjectOfType<CameraController>();
            if (_modalCoordinator == null) _modalCoordinator = FindObjectOfType<ModalCoordinator>();
            if (_uiManager == null)        _uiManager        = FindObjectOfType<UIManager>();
            if (_miniMapController == null) _miniMapController = FindObjectOfType<MiniMapController>();
        }

        void OnEnable()
        {
            _vm = new HudBarVM();

            if (_doc == null || _doc.rootVisualElement == null)
            {
                Debug.LogWarning("[HudBarHost] UIDocument or rootVisualElement null on enable — check PanelSettings wiring.");
                return;
            }

            var root = _doc.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.top = 0;
            root.style.left = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            // Iter-5: pass clicks through the now-fullscreen root to game-world / inner UI
            // children; .hud-bar element still picks normally.
            root.pickingMode = PickingMode.Ignore;
            root.SetCompatDataSource(_vm);

            _cityNameLbl  = root.Q<Label>("hud-city-name");
            _popLbl       = root.Q<Label>("hud-pop");
            _moneyLbl     = root.Q<Label>("hud-money");
            _surplusLbl   = root.Q<Label>("hud-surplus");
            _moneyTallLbl = root.Q<Label>("hud-money-2");
            _miniMap      = root.Q<VisualElement>("hud-mini-map");

            _btnPause   = root.Q<Button>("hud-pause");
            _btnZoomIn  = root.Q<Button>("hud-zoom-in");
            _btnZoomOut = root.Q<Button>("hud-zoom-out");
            _btnAuto    = root.Q<Button>("hud-auto");
            _btnMap     = root.Q<Button>("hud-map");
            _btnStats   = root.Q<Button>("hud-btn-stats");
            _btnSpeed1  = root.Q<Button>("btn-speed1");
            _btnSpeed2  = root.Q<Button>("btn-speed2");
            _btnSpeed3  = root.Q<Button>("btn-speed3");

            if (_btnPause   != null) _btnPause.clicked   += OnPause;
            if (_btnZoomIn  != null) _btnZoomIn.clicked  += OnZoomIn;
            if (_btnZoomOut != null) _btnZoomOut.clicked += OnZoomOut;
            if (_btnAuto    != null) _btnAuto.clicked    += OnAuto;
            if (_btnMap     != null) _btnMap.clicked     += OnMap;
            if (_btnStats   != null) _btnStats.clicked   += OnStats;
            if (_btnSpeed1  != null) _btnSpeed1.clicked  += OnSpeed1;
            if (_btnSpeed2  != null) _btnSpeed2.clicked  += OnSpeed2;
            if (_btnSpeed3  != null) _btnSpeed3.clicked  += OnSpeed3;

            // iter-22 (Effort 2) — hover + click blips on every HUD button.
            BindHudBlips();

            PushSnapshot();
        }

        void BindHudBlips()
        {
            var btns = new[] { _btnPause, _btnZoomIn, _btnZoomOut, _btnAuto, _btnMap, _btnStats, _btnSpeed1, _btnSpeed2, _btnSpeed3 };
            foreach (var b in btns)
                ToolkitBlipBinder.BindClickAndHover(b, BlipId.UiButtonClick, BlipId.UiButtonHover);
        }

        void UnbindHudBlips()
        {
            var btns = new[] { _btnPause, _btnZoomIn, _btnZoomOut, _btnAuto, _btnMap, _btnStats, _btnSpeed1, _btnSpeed2, _btnSpeed3 };
            foreach (var b in btns) ToolkitBlipBinder.UnbindAll(b);
        }

        void OnDisable()
        {
            if (_btnPause   != null) _btnPause.clicked   -= OnPause;
            if (_btnZoomIn  != null) _btnZoomIn.clicked  -= OnZoomIn;
            if (_btnZoomOut != null) _btnZoomOut.clicked -= OnZoomOut;
            if (_btnAuto    != null) _btnAuto.clicked    -= OnAuto;
            if (_btnMap     != null) _btnMap.clicked     -= OnMap;
            if (_btnStats   != null) _btnStats.clicked   -= OnStats;
            if (_btnSpeed1  != null) _btnSpeed1.clicked  -= OnSpeed1;
            if (_btnSpeed2  != null) _btnSpeed2.clicked  -= OnSpeed2;
            if (_btnSpeed3  != null) _btnSpeed3.clicked  -= OnSpeed3;

            UnbindHudBlips();

            if (_doc != null && _doc.rootVisualElement != null)
                _doc.rootVisualElement.SetCompatDataSource(null);
        }

        void Update()
        {
            PushSnapshot();
            RefreshMiniMapPreview();
        }

        void RefreshMiniMapPreview()
        {
            if (_miniMap == null) return;
            if (_miniMapController == null) _miniMapController = FindObjectOfType<MiniMapController>();
            if (_miniMapController == null) return;
            _miniMapRefreshFrame++;
            if (_miniMapRefreshFrame < 30) return; // ~2Hz at 60fps
            _miniMapRefreshFrame = 0;
            _miniMapController.RebuildTexture();
            var tex = _miniMapController.MapTexture;
            if (tex != null)
                _miniMap.style.backgroundImage = new StyleBackground(tex);
        }

        void PushSnapshot()
        {
            if (_vm == null) return;

            string moneyStr = "$0";
            string cityName = "—";
            string popStr = "Pop 0";

            if (_cityStats != null)
            {
                moneyStr = $"${_cityStats.money:N0}";
                cityName = string.IsNullOrEmpty(_cityStats.cityName) ? "—" : _cityStats.cityName;
                popStr = $"Pop {_cityStats.population:N0}";
                _vm.AutoMode = _cityStats.simulateGrowth;
            }
            _vm.Money = moneyStr;
            _vm.CityName = cityName;
            _vm.Population = popStr;

            int delta = 0;
            if (_economyManager != null)
            {
                delta = _economyManager.GetMonthlyIncomeDelta();
                _vm.BudgetDelta = delta >= 0 ? $"+{delta:N0}" : $"{delta:N0}";
            }
            string deltaStr = delta >= 0 ? $"+${delta:N0}" : $"-${System.Math.Abs(delta):N0}";
            _vm.Surplus = $"Est. monthly surplus: Δ {deltaStr}";

            if (_timeManager != null)
                _vm.Date = _timeManager.GetCurrentDate().ToString("MMM yyyy");

            if (_cityNameLbl   != null) _cityNameLbl.text   = _vm.CityName;
            if (_popLbl        != null) _popLbl.text        = _vm.Population;
            if (_moneyLbl      != null) _moneyLbl.text      = _vm.Money;
            if (_surplusLbl    != null) _surplusLbl.text    = _vm.Surplus;
            if (_moneyTallLbl  != null) _moneyTallLbl.text  = _vm.Money;

            if (_btnAuto != null)
                _btnAuto.EnableInClassList("text-btn--active", _vm.AutoMode);

            // Iter-13 (Effort 1 §16.4) — mirror active speed on speed1/2/3 + pause buttons.
            int idx = _timeManager != null ? _timeManager.CurrentTimeSpeedIndex : -1;
            if (_btnPause  != null) _btnPause.EnableInClassList("icon-btn--active",  idx == 0);
            if (_btnSpeed1 != null) _btnSpeed1.EnableInClassList("icon-btn--active", idx == 1);
            if (_btnSpeed2 != null) _btnSpeed2.EnableInClassList("icon-btn--active", idx == 2);
            if (_btnSpeed3 != null) _btnSpeed3.EnableInClassList("icon-btn--active", idx == 3);
        }

        // Iter-13 (Effort 1 §16.4) — pause button now toggles speed 0 ↔ last-active speed.
        int _preResumeSpeedIndex = 2; // default resume speed = 1.0×.

        void OnPause()
        {
            if (_timeManager == null) return;
            int cur = _timeManager.CurrentTimeSpeedIndex;
            if (cur != 0)
            {
                _preResumeSpeedIndex = cur;
                _timeManager.SetTimeSpeedIndex(0);
            }
            else
            {
                _timeManager.SetTimeSpeedIndex(_preResumeSpeedIndex);
            }
        }

        void OnZoomIn()
        {
            if (_cameraController != null) _cameraController.ZoomIn();
        }

        void OnZoomOut()
        {
            if (_cameraController != null) _cameraController.ZoomOut();
        }

        void OnAuto()
        {
            if (_cityStats != null)
            {
                _cityStats.simulateGrowth = !_cityStats.simulateGrowth;
                _vm.AutoMode = _cityStats.simulateGrowth;
            }
        }

        void OnMap()
        {
            if (_modalCoordinator == null) return;
            if (_modalCoordinator.IsOpen("map-panel"))
            {
                _modalCoordinator.HideMigrated("map-panel");
            }
            else
            {
                _modalCoordinator.Show("map-panel");
            }
        }

        void OnStats()
        {
            if (_modalCoordinator == null) return;
            if (_modalCoordinator.IsOpen("stats-panel"))
                _modalCoordinator.HideMigrated("stats-panel");
            else
                _modalCoordinator.Show("stats-panel");
        }

        void OnSpeed1() { if (_timeManager != null) _timeManager.SetTimeSpeedIndex(1); }
        void OnSpeed2() { if (_timeManager != null) _timeManager.SetTimeSpeedIndex(2); }
        void OnSpeed3() { if (_timeManager != null) _timeManager.SetTimeSpeedIndex(3); }
    }
}
