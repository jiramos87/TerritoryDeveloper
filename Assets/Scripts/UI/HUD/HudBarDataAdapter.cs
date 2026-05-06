using UnityEngine;
using TMPro;
using Territory.Economy;
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
        [SerializeField] private IlluminatedButton _budgetButton; // FEAT-59 — left of AUTO; opens growth-budget panel.
        [SerializeField] private IlluminatedButton _zoomInButton;
        [SerializeField] private IlluminatedButton _zoomOutButton;
        [SerializeField] private IlluminatedButton _statsButton;
        [SerializeField] private IlluminatedButton _miniMapButton;

        [Header("Growth budget panel (FEAT-59)")]
        [SerializeField] private GrowthBudgetPanelController _budgetPanelController;
        [SerializeField] private GameObject _growthBudgetPanelRoot; // optional Inspector slot; controller self-spawns when null.

        [Header("Stats + MiniMap roots — toggle on click")]
        [SerializeField] private GameObject _cityStatsRoot;
        [SerializeField] private GameObject _miniMapRoot;
        [SerializeField] private MiniMapController _miniMapController;

        [Header("Speed cluster — index 0 = paused, 1..4 = 0.5x/1x/2x/4x")]
        [SerializeField] private IlluminatedButton[] _speedButtons; // length 5: paused / 0.5x / 1x / 2x / 4x

        private string _lastBoundCityName;

        private void Awake()
        {
            // MonoBehaviour producers — Inspector first, FindObjectOfType fallback (invariant #4).
            if (_economyManager == null) _economyManager = FindObjectOfType<EconomyManager>();
            if (_timeManager == null) _timeManager = FindObjectOfType<TimeManager>();
            if (_cityStats == null) _cityStats = FindObjectOfType<CityStats>();
            if (_uiManager == null) _uiManager = FindObjectOfType<UIManager>();
            if (_cameraController == null) _cameraController = FindObjectOfType<CameraController>();

            // FEAT-59 — growth-budget panel controller (Inspector first, FindObjectOfType fallback,
            // lazy-spawn host so panel/archetype defaults are reachable without scene wiring).
            if (_budgetPanelController == null) _budgetPanelController = FindObjectOfType<GrowthBudgetPanelController>(true);
            if (_budgetPanelController == null)
            {
                var go = new GameObject("GrowthBudgetPanelController");
                _budgetPanelController = go.AddComponent<GrowthBudgetPanelController>();
            }

            // Post-Stage-9.1 wrapper-flatten: _miniMapRoot SerializeField slot left null in baked hud-bar.
            // Resolve via MiniMapController.miniMapPanel (controller may live ON the panel itself).
            if (_miniMapController == null) _miniMapController = FindObjectOfType<MiniMapController>(true);
            if (_miniMapRoot == null && _miniMapController != null)
            {
                _miniMapRoot = _miniMapController.miniMapPanel != null
                    ? _miniMapController.miniMapPanel
                    : _miniMapController.gameObject;
            }

            // Self-wire button slots by IR iconSpriteSlug — resilient against bake-time reordering
            // of physical button instances. Inspector slots referencing legacy buttons (no slug match)
            // are preserved as-is.
            RebindButtonsByIconSlug();

            WireClickHandlers();

            // Cleanup: destroy any pre-existing RuntimeMiniMapButton from prior builds (corner button retired).
            Canvas hostCanvas = GetComponentInParent<Canvas>(true);
            if (hostCanvas == null) hostCanvas = FindObjectOfType<Canvas>();
            if (hostCanvas != null)
            {
                var stale = hostCanvas.transform.Find("RuntimeMiniMapButton");
                if (stale != null) Destroy(stale.gameObject);
            }
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
            _budgetButton = null;
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
                // BUG-62 — bake handler emits PascalCase iconSpriteSlug; switch literals lowercase. Normalize on read.
                var slug = btn != null && btn.Detail != null ? btn.Detail.iconSpriteSlug?.ToLowerInvariant() : null;
                if (string.IsNullOrEmpty(slug))
                {
                    // Caption-text fallback — empty slug buttons (MAP, AUTO) bake with TMP caption only.
                    var capTmp = btn != null ? btn.GetComponentInChildren<TextMeshProUGUI>(true) : null;
                    var cap = capTmp != null ? capTmp.text?.Trim().ToUpperInvariant() : null;
                    if (!string.IsNullOrEmpty(cap))
                    {
                        if (cap == "MAP" && _miniMapButton == null) _miniMapButton = btn;
                        else if (cap == "AUTO" && _autoButton == null) _autoButton = btn;
                        // FEAT-59 — caption-text fallback for BUDGET (sprite art deferred per Stage 9.9 B1c).
                        else if (cap == "BUDGET" && _budgetButton == null) _budgetButton = btn;
                    }
                    continue;
                }

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
                    // BUG-61 W6+W7 — AUTO toggle baked into hud-bar right slot via IR iconSpriteSlugs[].
                    // Sprite art pending; bake handler Step 16.G renders TMP "AUTO" caption fallback.
                    // Drives both AutoZoningManager + AutoRoadBuilder via HandleAutoClick.
                    case "auto-button-64": _autoButton = btn; break;
                    // FEAT-59 (Stage 9.9) — BUDGET button left of AUTO. Migration 0087 sprite slug.
                    // Bake handler renders TMP "BUDGET" caption fallback (B1c — PNG art deferred).
                    case "hud_bar_icon_budget": _budgetButton = btn; break;
                    // Bug #4 — preventive: bind if/when bake handler emits a minimap-button slug.
                    case "minimap-button-64": _miniMapButton = btn; break;
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
            if (_budgetButton != null)
            {
                _budgetButton.OnClick.RemoveListener(HandleBudgetClick);
                _budgetButton.OnClick.AddListener(HandleBudgetClick);
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
            // BUG-63 — flip cityStats.simulateGrowth (single source of truth ProcessTick gate).
            // AutoZoningManager + AutoRoadBuilder both read this bool — no MonoBehaviour.enabled flip needed.
            if (_cityStats == null) return;
            _cityStats.simulateGrowth = !_cityStats.simulateGrowth;
        }

        private void HandleBudgetClick()
        {
            // FEAT-59 — toggle growth-budget panel; controller self-spawns its panelRoot on first Show.
            if (_budgetPanelController == null) return;
            _budgetPanelController.Toggle();
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
            if (_miniMapController != null)
            {
                _miniMapController.SetVisible(!_miniMapController.IsVisible);
                return;
            }
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

            // AUTO illumination mirrors cityStats.simulateGrowth (BUG-63).
            if (_autoButton != null && _cityStats != null)
            {
                _autoButton.IlluminationAlpha = _cityStats.simulateGrowth ? 1f : 0f;
            }

            // BUDGET illumination mirrors panel visibility (FEAT-59).
            if (_budgetButton != null && _budgetPanelController != null)
            {
                _budgetButton.IlluminationAlpha = _budgetPanelController.IsVisible ? 1f : 0f;
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
