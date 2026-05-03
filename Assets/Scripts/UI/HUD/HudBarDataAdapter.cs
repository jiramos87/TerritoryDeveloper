using UnityEngine;
using Territory.Economy;
using Territory.Simulation;
using Territory.Timing;
using Territory.UI.Juice;
using Territory.UI.StudioControls;
using Territory.UI.Themed;

namespace Territory.UI.HUD
{
    /// <summary>
    /// Bridges live sim producers (<see cref="CityStats"/> SO money/population/happiness;
    /// <see cref="EconomyManager"/> finance live values; <see cref="TimeManager"/> speed index)
    /// into baked StudioControl SO refs on the new <c>hud-bar</c> prefab.
    /// </summary>
    /// <remarks>
    /// Read-only consumer. All refs cached in <see cref="Awake"/> (invariant #3 — never
    /// <see cref="MonoBehaviour.FindObjectOfType{T}()"/> in <see cref="Update"/>). Per-channel
    /// null-check on producer refs (guardrail #14) so partial init still surfaces ready channels.
    /// MonoBehaviour producers fall back to <see cref="MonoBehaviour.FindObjectOfType{T}()"/> in
    /// <see cref="Awake"/> when Inspector slot empty (invariant #4); SO ref must be Inspector-assigned.
    /// </remarks>
    public class HudBarDataAdapter : MonoBehaviour
    {
        // ── Producer refs (invariants #3 + #4 — Inspector + Awake fallback for MonoBehaviours)

        [Header("Producers")]
        [SerializeField] private CityStats _cityStats;
        [SerializeField] private EconomyManager _economyManager;
        [SerializeField] private TimeManager _timeManager;

        [Header("Sim controllers (AUTO toggle target)")]
        [SerializeField] private AutoZoningManager _autoZoningManager;
        [SerializeField] private AutoRoadBuilder _autoRoadBuilder;

        [Header("UI handlers")]
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private CameraController _cameraController;

        // ── Theme cache (invariant #3 — caching contract regardless of immediate read)

        [Header("Theme")]
        [SerializeField] private UiTheme _uiTheme;

        // ── Consumer refs (StudioControl variants on baked hud-bar prefab)

        [Header("Consumers — readouts")]
        [SerializeField] private SegmentedReadout _moneyReadout;
        [SerializeField] private SegmentedReadout _populationReadout; // optional — null-tolerant
        [SerializeField] private VUMeter _happinessMeter;
        [SerializeField] private NeedleBallistics _happinessNeedle; // optional — preferred input surface for happiness; falls back to none if absent
        [SerializeField] private ThemedLabel _cityNameLabel; // center cluster — populated from cityStats.cityName

        [Header("Consumers — left cluster (file ops)")]
        [SerializeField] private IlluminatedButton _newButton;
        [SerializeField] private IlluminatedButton _saveButton;
        [SerializeField] private IlluminatedButton _loadButton;

        [Header("Consumers — right cluster (controls)")]
        [SerializeField] private IlluminatedButton _autoButton;
        [SerializeField] private IlluminatedButton _zoomInButton;
        [SerializeField] private IlluminatedButton _zoomOutButton;
        [SerializeField] private IlluminatedButton _statsButton;
        [SerializeField] private IlluminatedButton _miniMapButton;

        [Header("Stats + MiniMap roots — toggle on click")]
        [SerializeField] private GameObject _cityStatsRoot;
        [SerializeField] private GameObject _miniMapRoot;

        [Header("Speed cluster — index 0 = paused, 1..4 = 0.5x/1x/2x/4x")]
        [SerializeField] private IlluminatedButton[] _speedButtons; // length 5: paused / 0.5x / 1x / 2x / 4x

        private string _lastBoundCityName;

        private void Awake()
        {
            // MonoBehaviour producers — Inspector first, FindObjectOfType fallback (invariant #4).
            if (_economyManager == null) _economyManager = FindObjectOfType<EconomyManager>();
            if (_timeManager == null) _timeManager = FindObjectOfType<TimeManager>();
            if (_cityStats == null) _cityStats = FindObjectOfType<CityStats>();
            if (_autoZoningManager == null) _autoZoningManager = FindObjectOfType<AutoZoningManager>();
            if (_autoRoadBuilder == null) _autoRoadBuilder = FindObjectOfType<AutoRoadBuilder>();
            if (_uiManager == null) _uiManager = FindObjectOfType<UIManager>();
            if (_cameraController == null) _cameraController = FindObjectOfType<CameraController>();

            // Self-wire button slots by IR iconSpriteSlug — resilient against bake-time reordering
            // of physical button instances. Inspector slots referencing legacy buttons (no slug match)
            // are preserved as-is.
            RebindButtonsByIconSlug();

            WireClickHandlers();
        }

        // Slug-to-slot map for hud-bar IR. Adapter clears ALL Inspector array slots, then walks child
        // IlluminatedButton components and matches IlluminatedButtonDetail.iconSpriteSlug. Inspector
        // refs from pre-bake scene serialization can resolve to physical buttons whose icon meaning
        // changed after re-bake — leaving them attached causes click handlers to fire stale actions
        // (e.g. Save/New game on visually-Stats click). Hard reset is the only safe path: slugs not
        // present in IR (NEW/SAVE/LOAD/AUTO/MINIMAP) leave their slot null + their handler unwired,
        // which is correct because those visuals do not exist in the baked output.
        private void RebindButtonsByIconSlug()
        {
            // Hard reset — drop all Inspector slot bindings before slug-walk.
            _newButton = null;
            _saveButton = null;
            _loadButton = null;
            _autoButton = null;
            _zoomInButton = null;
            _zoomOutButton = null;
            _statsButton = null;
            _miniMapButton = null;
            _speedButtons = null;

            var buttons = GetComponentsInChildren<IlluminatedButton>(true);
            if (buttons == null || buttons.Length == 0) return;

            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                var slug = btn != null && btn.Detail != null ? btn.Detail.iconSpriteSlug : null;
                if (string.IsNullOrEmpty(slug)) continue;

                switch (slug)
                {
                    case "stats-button-64": _statsButton = btn; break;
                    case "zoom-in-button-1-64": _zoomInButton = btn; break;
                    case "zoom-out-button-1-64": _zoomOutButton = btn; break;
                    case "pause-button-1-64": EnsureSpeedSlot(0, btn); break;
                    case "speed-1-button-1-64": EnsureSpeedSlot(1, btn); break;
                    case "speed-2-button-1-64": EnsureSpeedSlot(2, btn); break;
                    case "speed-3-button-1-64": EnsureSpeedSlot(3, btn); break;
                    case "speed-4-button-1-64": EnsureSpeedSlot(4, btn); break;
                }
            }
        }

        private void EnsureSpeedSlot(int index, IlluminatedButton btn)
        {
            if (_speedButtons == null || _speedButtons.Length < 5)
            {
                var resized = new IlluminatedButton[5];
                if (_speedButtons != null)
                {
                    for (int i = 0; i < _speedButtons.Length && i < 5; i++) resized[i] = _speedButtons[i];
                }
                _speedButtons = resized;
            }
            _speedButtons[index] = btn;
        }

        private void WireClickHandlers()
        {
            if (_newButton != null)
            {
                _newButton.OnClick.RemoveListener(HandleNewClick);
                _newButton.OnClick.AddListener(HandleNewClick);
            }
            if (_saveButton != null)
            {
                _saveButton.OnClick.RemoveListener(HandleSaveClick);
                _saveButton.OnClick.AddListener(HandleSaveClick);
            }
            if (_loadButton != null)
            {
                _loadButton.OnClick.RemoveListener(HandleLoadClick);
                _loadButton.OnClick.AddListener(HandleLoadClick);
            }
            if (_autoButton != null)
            {
                _autoButton.OnClick.RemoveListener(HandleAutoClick);
                _autoButton.OnClick.AddListener(HandleAutoClick);
            }
            if (_zoomInButton != null)
            {
                _zoomInButton.OnClick.RemoveListener(HandleZoomInClick);
                _zoomInButton.OnClick.AddListener(HandleZoomInClick);
            }
            if (_zoomOutButton != null)
            {
                _zoomOutButton.OnClick.RemoveListener(HandleZoomOutClick);
                _zoomOutButton.OnClick.AddListener(HandleZoomOutClick);
            }
            if (_statsButton != null)
            {
                _statsButton.OnClick.RemoveListener(HandleStatsClick);
                _statsButton.OnClick.AddListener(HandleStatsClick);
            }
            if (_miniMapButton != null)
            {
                _miniMapButton.OnClick.RemoveListener(HandleMiniMapClick);
                _miniMapButton.OnClick.AddListener(HandleMiniMapClick);
            }
            WireSpeedButtonClicks();
        }

        private void WireSpeedButtonClicks()
        {
            if (_speedButtons == null) return;
            for (int i = 0; i < _speedButtons.Length; i++)
            {
                var btn = _speedButtons[i];
                if (btn == null) continue;
                int captured = i;
                btn.OnClick.AddListener(() => HandleSpeedClick(captured));
            }
        }

        private void HandleSpeedClick(int index)
        {
            if (_timeManager == null) return;
            _timeManager.SetTimeSpeedIndex(index);
        }

        private void HandleNewClick()
        {
            if (_uiManager != null) _uiManager.OnNewGameButtonClicked();
        }

        private void HandleSaveClick()
        {
            if (_uiManager != null) _uiManager.OnSaveGameButtonClicked();
        }

        private void HandleLoadClick()
        {
            if (_uiManager != null) _uiManager.OpenPopup(PopupType.SaveLoadScreen);
        }

        private void HandleAutoClick()
        {
            // Single toggle drives both auto-zoning + auto-road-building.
            // Read state from AutoZoningManager (treated as primary); flip both to inverse.
            bool enable = _autoZoningManager == null ? true : !_autoZoningManager.enabled;
            if (_autoZoningManager != null) _autoZoningManager.enabled = enable;
            if (_autoRoadBuilder != null) _autoRoadBuilder.enabled = enable;
        }

        private void HandleZoomInClick()
        {
            if (_cameraController != null) _cameraController.ZoomIn();
        }

        private void HandleZoomOutClick()
        {
            if (_cameraController != null) _cameraController.ZoomOut();
        }

        private void HandleStatsClick()
        {
            if (_cityStatsRoot != null) _cityStatsRoot.SetActive(!_cityStatsRoot.activeSelf);
        }

        private void HandleMiniMapClick()
        {
            if (_miniMapRoot != null) _miniMapRoot.SetActive(!_miniMapRoot.activeSelf);
        }

        private void Update()
        {
            // money channel
            if (_cityStats != null && _moneyReadout != null)
            {
                _moneyReadout.CurrentValue = _cityStats.money;
            }

            // population channel (optional consumer)
            if (_cityStats != null && _populationReadout != null)
            {
                _populationReadout.CurrentValue = _cityStats.population;
            }

            // happiness channel — preferred path is NeedleBallistics.TargetValue (Stage 5 contract);
            // VUMeter has no direct Value setter (read-only Detail). When the needle juice
            // sibling absent, write nothing — VUMeter ignored.
            if (_cityStats != null && _happinessNeedle != null)
            {
                _happinessNeedle.TargetValue = _cityStats.happiness;
            }

            // city name channel — only rebind when value changes (avoid TMP rebuilds every frame).
            if (_cityStats != null && _cityNameLabel != null)
            {
                string name = _cityStats.cityName;
                if (name != _lastBoundCityName)
                {
                    _cityNameLabel.Detail = string.IsNullOrEmpty(name) ? "—" : name;
                    _lastBoundCityName = name;
                }
            }

            // AUTO illumination mirrors AutoZoningManager.enabled (treated as primary).
            if (_autoButton != null && _autoZoningManager != null)
            {
                _autoButton.IlluminationAlpha = _autoZoningManager.enabled ? 1f : 0f;
            }

            // speed channel — exactly-one-illuminated mirroring TimeManager.CurrentTimeSpeedIndex
            if (_timeManager != null && _speedButtons != null && _speedButtons.Length > 0)
            {
                int idx = _timeManager.CurrentTimeSpeedIndex;
                if (idx >= 0 && idx < _speedButtons.Length)
                {
                    for (int i = 0; i < _speedButtons.Length; i++)
                    {
                        var btn = _speedButtons[i];
                        if (btn == null) continue;
                        btn.IlluminationAlpha = (i == idx) ? 1f : 0f;
                    }
                }
            }
        }
    }
}
