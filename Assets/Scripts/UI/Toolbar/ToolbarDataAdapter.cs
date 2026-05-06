using UnityEngine;
using Territory.Forests;
using Territory.Zones;
using Territory.UI.StudioControls;

namespace Territory.UI.Toolbar
{
    /// <summary>
    /// Bridges live <see cref="UIManager"/> selection state into baked
    /// <see cref="IlluminatedButton"/> consumer slots on the new <c>toolbar</c> prefab.
    /// Mirrors the Stage 6 <see cref="Territory.UI.HUD.HudBarDataAdapter"/> precedent —
    /// Inspector producer slot for <see cref="UIManager"/>, Inspector consumer arrays per
    /// tool kind, click events route to <see cref="UIManager"/> <c>On*ButtonClicked()</c>
    /// methods, <c>Update</c> mirrors active-tool selection into
    /// <see cref="IlluminatedButton.IlluminationAlpha"/>.
    /// </summary>
    /// <remarks>
    /// Read-only consumer plus event-bridge. Producer + theme refs cached in <see cref="Awake"/>
    /// (invariant #3 — never <see cref="MonoBehaviour.FindObjectOfType{T}()"/> in <see cref="Update"/>).
    /// Per-channel null tolerance (Stage 6 precedent) so partial scene wiring still surfaces ready
    /// channels without NRE. Inspector slot first; <see cref="Awake"/> falls back to
    /// <see cref="MonoBehaviour.FindObjectOfType{T}()"/> when null (invariant #4).
    /// </remarks>
    public class ToolbarDataAdapter : MonoBehaviour
    {
        // ── Producer ref (invariant #4 — Inspector + Awake fallback)

        [Header("Producer")]
        [SerializeField] private UIManager _uiManager;

        // ── Theme cache (invariant #3 — caching contract)

        [Header("Theme")]
        [SerializeField] private UiTheme _uiTheme;

        // ── Consumer refs (IlluminatedButton variants on baked toolbar prefab)

        /// <summary>Index 0..2 = Residential L/M/H Zoning; 3..5 = Commercial L/M/H Zoning; 6..8 = Industrial L/M/H Zoning; 9 = StateService Zoning.</summary>
        [Header("Consumers — zoning (length 10)")]
        [SerializeField] private IlluminatedButton[] _zoningButtons;

        [Header("Consumers — road (length 1: road-twoway)")]
        [SerializeField] private IlluminatedButton[] _roadButtons;

        [Header("Consumers — terrain (length 1: grass)")]
        [SerializeField] private IlluminatedButton[] _terrainButtons;

        /// <summary>Index 0 = nuclear-power-plant; 1 = water-pump-medium.</summary>
        [Header("Consumers — buildings (length 2)")]
        [SerializeField] private IlluminatedButton[] _buildingButtons;

        /// <summary>Index 0 = sparse; 1 = medium; 2 = dense.</summary>
        [Header("Consumers — forest (length 3)")]
        [SerializeField] private IlluminatedButton[] _forestButtons;

        [Header("Consumers — bulldoze")]
        [SerializeField] private IlluminatedButton _bulldozeButton;

        // ── Click subscription bookkeeping ──

        private bool _subscribed;

        private void Awake()
        {
            // MonoBehaviour producer — Inspector first, FindObjectOfType fallback (invariant #4).
            if (_uiManager == null) _uiManager = FindObjectOfType<UIManager>();
            // UiTheme is a ScriptableObject — Inspector-only assignment per StudioControlBase pattern.
            // No FindObjectOfType for SOs (Stage 6 precedent).

            // Self-wire button slots by IR iconSpriteSlug — resilient against bake-time reordering.
            // Maps single-tier baked icons to canonical adapter slot indices (residential→0,
            // commercial→3, industrial→6, state→9; power→0, water→1; forest sparse→0).
            RebindButtonsByIconSlug();

            SubscribeClicks();
        }

        private void RebindButtonsByIconSlug()
        {
            // Hard reset — drop all Inspector slot bindings before slug-walk. Pre-bake Inspector
            // refs can resolve to physical buttons whose icon meaning changed after re-bake; leaving
            // them attached causes click handlers to fire stale actions on visually-different
            // buttons. Slugs not present in IR leave their slot null → handler not wired → no
            // false-positive dispatch.
            _zoningButtons = null;
            _roadButtons = null;
            _terrainButtons = null;
            _buildingButtons = null;
            _forestButtons = null;
            _bulldozeButton = null;

            var buttons = GetComponentsInChildren<IlluminatedButton>(true);
            if (buttons == null || buttons.Length == 0) return;

            EnsureArray(ref _zoningButtons, 10);
            EnsureArray(ref _roadButtons, 1);
            EnsureArray(ref _terrainButtons, 1);
            EnsureArray(ref _buildingButtons, 2);
            EnsureArray(ref _forestButtons, 3);

            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                // BUG-62 — bake handler emits PascalCase iconSpriteSlug; switch literals lowercase. Normalize on read.
                var slug = btn != null && btn.Detail != null ? btn.Detail.iconSpriteSlug?.ToLowerInvariant() : null;
                if (string.IsNullOrEmpty(slug)) continue;

                switch (slug)
                {
                    case "residential-button-64": _zoningButtons[0] = btn; break;
                    case "commercial-button-64": _zoningButtons[3] = btn; break;
                    case "industrial-button-64": _zoningButtons[6] = btn; break;
                    case "state-button-64": _zoningButtons[9] = btn; break;
                    case "power-buildings-button-64": _buildingButtons[0] = btn; break;
                    case "water-buildings-button-64": _buildingButtons[1] = btn; break;
                    case "roads-button-64": _roadButtons[0] = btn; break;
                    case "forest-button-64": _forestButtons[0] = btn; break;
                    case "bulldoze-button-64": _bulldozeButton = btn; break;
                }
            }
        }

        private static void EnsureArray(ref IlluminatedButton[] arr, int length)
        {
            if (arr != null && arr.Length >= length) return;
            var resized = new IlluminatedButton[length];
            if (arr != null)
            {
                for (int i = 0; i < arr.Length && i < length; i++) resized[i] = arr[i];
            }
            arr = resized;
        }

        private void OnDestroy()
        {
            UnsubscribeClicks();
        }

        private void SubscribeClicks()
        {
            if (_subscribed) return;
            // Zoning slots — index drives ZoneType dispatch.
            if (_zoningButtons != null)
            {
                for (int i = 0; i < _zoningButtons.Length; i++)
                {
                    int idx = i;
                    var btn = _zoningButtons[i];
                    if (btn == null) continue;
                    btn.OnClick.AddListener(() => OnZoningClick(idx));
                }
            }
            if (_roadButtons != null && _roadButtons.Length > 0 && _roadButtons[0] != null)
            {
                _roadButtons[0].OnClick.AddListener(OnRoadClick);
            }
            if (_terrainButtons != null && _terrainButtons.Length > 0 && _terrainButtons[0] != null)
            {
                _terrainButtons[0].OnClick.AddListener(OnGrassClick);
            }
            if (_buildingButtons != null)
            {
                for (int i = 0; i < _buildingButtons.Length; i++)
                {
                    int idx = i;
                    var btn = _buildingButtons[i];
                    if (btn == null) continue;
                    btn.OnClick.AddListener(() => OnBuildingClick(idx));
                }
            }
            if (_forestButtons != null)
            {
                for (int i = 0; i < _forestButtons.Length; i++)
                {
                    int idx = i;
                    var btn = _forestButtons[i];
                    if (btn == null) continue;
                    btn.OnClick.AddListener(() => OnForestClick(idx));
                }
            }
            if (_bulldozeButton != null)
            {
                _bulldozeButton.OnClick.AddListener(OnBulldozeClick);
            }
            _subscribed = true;
        }

        private void UnsubscribeClicks()
        {
            if (!_subscribed) return;
            if (_zoningButtons != null)
            {
                for (int i = 0; i < _zoningButtons.Length; i++)
                {
                    var btn = _zoningButtons[i];
                    if (btn != null) btn.OnClick.RemoveAllListeners();
                }
            }
            if (_roadButtons != null)
            {
                for (int i = 0; i < _roadButtons.Length; i++)
                {
                    var btn = _roadButtons[i];
                    if (btn != null) btn.OnClick.RemoveAllListeners();
                }
            }
            if (_terrainButtons != null)
            {
                for (int i = 0; i < _terrainButtons.Length; i++)
                {
                    var btn = _terrainButtons[i];
                    if (btn != null) btn.OnClick.RemoveAllListeners();
                }
            }
            if (_buildingButtons != null)
            {
                for (int i = 0; i < _buildingButtons.Length; i++)
                {
                    var btn = _buildingButtons[i];
                    if (btn != null) btn.OnClick.RemoveAllListeners();
                }
            }
            if (_forestButtons != null)
            {
                for (int i = 0; i < _forestButtons.Length; i++)
                {
                    var btn = _forestButtons[i];
                    if (btn != null) btn.OnClick.RemoveAllListeners();
                }
            }
            if (_bulldozeButton != null) _bulldozeButton.OnClick.RemoveAllListeners();
            _subscribed = false;
        }

        // ── Click dispatch ──

        private void OnZoningClick(int index)
        {
            if (_uiManager == null) return;
            // Slots 0/3/6/9 = Family entry buttons (single icon per family in IR — TECH-10500).
            // Family click opens SubtypePicker for L/M/H tier selection; picker rows commit via
            // density handlers. Slots 1/2/4/5/7/8 stay null after RebindButtonsByIconSlug because
            // single-tier IR carries no per-density slugs — defensive density routes preserved
            // for legacy multi-tier rebake compatibility.
            switch (index)
            {
                case 0: _uiManager.OnResidentialFamilyButtonClicked(); break;
                case 1: _uiManager.OnMediumResidentialButtonClicked(); break;
                case 2: _uiManager.OnHeavyResidentialButtonClicked(); break;
                case 3: _uiManager.OnCommercialFamilyButtonClicked(); break;
                case 4: _uiManager.OnMediumCommercialButtonClicked(); break;
                case 5: _uiManager.OnHeavyCommercialButtonClicked(); break;
                case 6: _uiManager.OnIndustrialFamilyButtonClicked(); break;
                case 7: _uiManager.OnMediumIndustrialButtonClicked(); break;
                case 8: _uiManager.OnHeavyIndustrialButtonClicked(); break;
                case 9: _uiManager.OnStateServiceZoningButtonClicked(); break;
            }
        }

        private void OnRoadClick()
        {
            if (_uiManager == null) return;
            // Stage 9.8 (TECH-15897) — re-routes to family picker (universal-rule conformance).
            _uiManager.OnRoadsFamilyButtonClicked();
        }

        private void OnGrassClick()
        {
            if (_uiManager == null) return;
            _uiManager.OnGrassButtonClicked();
        }

        private void OnBuildingClick(int index)
        {
            if (_uiManager == null) return;
            // Stage 9.8 (TECH-15897) — re-routes to family pickers (universal-rule conformance).
            switch (index)
            {
                case 0: _uiManager.OnPowerFamilyButtonClicked(); break;
                case 1: _uiManager.OnWaterFamilyButtonClicked(); break;
            }
        }

        private void OnForestClick(int index)
        {
            if (_uiManager == null) return;
            // Stage 9.8 (TECH-15897) — index 0 (sparse/primary) re-routes to family picker.
            switch (index)
            {
                case 0: _uiManager.OnForestsFamilyButtonClicked(); break;
                // Legacy multi-tier paths preserved for rebake compatibility.
                case 1: _uiManager.OnMediumForestButtonClicked(); break;
                case 2: _uiManager.OnDenseForestButtonClicked(); break;
            }
        }

        private void OnBulldozeClick()
        {
            if (_uiManager == null) return;
            _uiManager.OnBulldozeButtonClicked();
        }

        // ── Active-tool mirror (Update) ──

        private void Update()
        {
            if (_uiManager == null) return;

            // Detection priority: bulldoze first → building placement → forest → terrain (grass) →
            // road → zoning → none. Matches legacy mutex (only one active at a time).
            bool bulldoze = _uiManager.isBulldozeMode();
            bool buildingMode = _uiManager.IsBuildingPlacementMode();
            IBuilding selectedBuilding = _uiManager.GetSelectedBuilding();
            IForest selectedForest = _uiManager.GetSelectedForest();
            Zone.ZoneType zone = _uiManager.GetSelectedZoneType();

            int activeBuildingIdx = -1;
            int activeForestIdx = -1;
            int activeZoningIdx = -1;
            bool roadActive = false;
            bool grassActive = false;

            if (!bulldoze)
            {
                if (buildingMode && selectedBuilding != null)
                {
                    activeBuildingIdx = ResolveBuildingIndex(selectedBuilding);
                }
                else if (selectedForest != null)
                {
                    activeForestIdx = ResolveForestIndex(selectedForest);
                }
                else
                {
                    switch (zone)
                    {
                        case Zone.ZoneType.Grass:
                            grassActive = true;
                            break;
                        case Zone.ZoneType.Road:
                            roadActive = true;
                            break;
                        case Zone.ZoneType.ResidentialLightZoning:
                            activeZoningIdx = 0; break;
                        case Zone.ZoneType.ResidentialMediumZoning:
                            activeZoningIdx = 1; break;
                        case Zone.ZoneType.ResidentialHeavyZoning:
                            activeZoningIdx = 2; break;
                        case Zone.ZoneType.CommercialLightZoning:
                            activeZoningIdx = 3; break;
                        case Zone.ZoneType.CommercialMediumZoning:
                            activeZoningIdx = 4; break;
                        case Zone.ZoneType.CommercialHeavyZoning:
                            activeZoningIdx = 5; break;
                        case Zone.ZoneType.IndustrialLightZoning:
                            activeZoningIdx = 6; break;
                        case Zone.ZoneType.IndustrialMediumZoning:
                            activeZoningIdx = 7; break;
                        case Zone.ZoneType.IndustrialHeavyZoning:
                            activeZoningIdx = 8; break;
                        case Zone.ZoneType.StateServiceLightZoning:
                        case Zone.ZoneType.StateServiceMediumZoning:
                        case Zone.ZoneType.StateServiceHeavyZoning:
                            activeZoningIdx = 9; break;
                    }
                }
            }

            // Mirror illumination — exactly-one-active across all consumer slots.
            WriteAlphaArray(_zoningButtons, activeZoningIdx);
            WriteAlphaArray(_buildingButtons, activeBuildingIdx);
            WriteAlphaArray(_forestButtons, activeForestIdx);
            WriteAlphaSingle(_roadButtons, roadActive);
            WriteAlphaSingle(_terrainButtons, grassActive);
            if (_bulldozeButton != null)
            {
                _bulldozeButton.IlluminationAlpha = bulldoze ? 1f : 0f;
            }
        }

        private static void WriteAlphaArray(IlluminatedButton[] arr, int activeIndex)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
            {
                var btn = arr[i];
                if (btn == null) continue;
                btn.IlluminationAlpha = (i == activeIndex) ? 1f : 0f;
            }
        }

        private static void WriteAlphaSingle(IlluminatedButton[] arr, bool active)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
            {
                var btn = arr[i];
                if (btn == null) continue;
                btn.IlluminationAlpha = active ? 1f : 0f;
            }
        }

        private int ResolveBuildingIndex(IBuilding b)
        {
            if (b == null || _buildingButtons == null) return -1;
            // Index assignment matches Inspector layout — adapter does not introspect concrete IBuilding type.
            // Resolution lives in scene wiring (T7.4) where Inspector slots are populated in the same
            // order as legacy nuclear / water-pump button registration.
            // Heuristic: type-name check for unambiguous routing.
            var name = b.GetType().Name;
            if (name.IndexOf("Nuclear", System.StringComparison.OrdinalIgnoreCase) >= 0) return 0;
            if (name.IndexOf("Water", System.StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            return -1;
        }

        private int ResolveForestIndex(IForest f)
        {
            if (f == null) return -1;
            var name = f.GetType().Name;
            if (name.IndexOf("Sparse", System.StringComparison.OrdinalIgnoreCase) >= 0) return 0;
            if (name.IndexOf("Medium", System.StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            if (name.IndexOf("Dense", System.StringComparison.OrdinalIgnoreCase) >= 0) return 2;
            return -1;
        }
    }
}
