using Territory.UI;
using Territory.UI.StudioControls;
using Territory.UI.Registry;
using Territory.Zones;
using Territory.Forests;

namespace Domains.UI.Services
{
    /// <summary>
    /// POCO service extracted from ToolbarDataAdapter (Stage 5.7 Tier-C NO-PORT).
    /// Owns: button-slot rebuild (slug→index), illumination writes, click dispatch,
    /// action-registry handlers, picker-bind handlers.
    /// No MonoBehaviour. No Unity lifecycle. Hub retains [SerializeField] refs + Awake/Update shell.
    /// Invariants #3 (no per-frame FindObjectOfType), #4 (no new singletons).
    /// autoReferenced:false — UI.Runtime; TerritoryDeveloper.Game auto-ref visible via overrideReferences:false.
    /// </summary>
    public class ToolbarAdapterService
    {
        // ── Slot arrays (owned by service, written on rebind) ────────────────────

        public IlluminatedButton[] ZoningButtons;
        public IlluminatedButton[] RoadButtons;
        public IlluminatedButton[] TerrainButtons;
        public IlluminatedButton[] BuildingButtons;
        public IlluminatedButton[] ForestButtons;
        public IlluminatedButton   BulldozeButton;

        // ── External refs wired from hub ─────────────────────────────────────────

        private UIManager         _uiManager;
        private UiActionRegistry  _actionRegistry;
        private UiBindRegistry    _bindRegistry;
        private UnityEngine.GameObject _subtypePickerRoot;

        // ── Subscribe tracking ───────────────────────────────────────────────────

        public bool Subscribed { get; private set; }

        // ── Init ─────────────────────────────────────────────────────────────────

        /// <summary>Wire external refs from hub Awake. Call before any other method.</summary>
        public void Wire(UIManager uiManager, UiActionRegistry actionRegistry,
                         UiBindRegistry bindRegistry, UnityEngine.GameObject subtypePickerRoot)
        {
            _uiManager         = uiManager;
            _actionRegistry    = actionRegistry;
            _bindRegistry      = bindRegistry;
            _subtypePickerRoot = subtypePickerRoot;
        }

        // ── Rebind ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Reset slot arrays and walk children of <paramref name="root"/> to assign
        /// IlluminatedButton slots by iconSpriteSlug. Call from hub Awake.
        /// </summary>
        public void RebindButtonsByIconSlug(UnityEngine.Transform root)
        {
            ZoningButtons    = null;
            RoadButtons      = null;
            TerrainButtons   = null;
            BuildingButtons  = null;
            ForestButtons    = null;
            BulldozeButton   = null;

            if (root == null) return;
            var buttons = root.GetComponentsInChildren<IlluminatedButton>(true);
            if (buttons == null || buttons.Length == 0) return;

            EnsureArray(ref ZoningButtons,   10);
            EnsureArray(ref RoadButtons,      1);
            EnsureArray(ref TerrainButtons,   1);
            EnsureArray(ref BuildingButtons,  2);
            EnsureArray(ref ForestButtons,    3);

            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                // BUG-62 — normalize iconSpriteSlug to lowercase on read.
                var slug = btn != null && btn.Detail != null
                    ? btn.Detail.iconSpriteSlug?.ToLowerInvariant()
                    : null;
                if (string.IsNullOrEmpty(slug)) continue;

                switch (slug)
                {
                    case "residential-button-64":    ZoningButtons[0]   = btn; break;
                    case "commercial-button-64":     ZoningButtons[3]   = btn; break;
                    case "industrial-button-64":     ZoningButtons[6]   = btn; break;
                    case "state-button-64":          ZoningButtons[9]   = btn; break;
                    case "power-buildings-button-64":BuildingButtons[0] = btn; break;
                    case "water-buildings-button-64":BuildingButtons[1] = btn; break;
                    case "roads-button-64":          RoadButtons[0]     = btn; break;
                    case "forest-button-64":         ForestButtons[0]   = btn; break;
                    case "bulldoze-button-64":       BulldozeButton     = btn; break;
                }
            }
        }

        // ── Click subscribe / unsubscribe ─────────────────────────────────────────

        /// <summary>Wire OnClick listeners. Hub calls from Awake after RebindButtonsByIconSlug.</summary>
        public void SubscribeClicks()
        {
            if (Subscribed) return;
            if (ZoningButtons != null)
            {
                for (int i = 0; i < ZoningButtons.Length; i++)
                {
                    int idx = i;
                    var btn = ZoningButtons[i];
                    if (btn == null) continue;
                    btn.OnClick.AddListener(() => OnZoningClick(idx));
                }
            }
            if (RoadButtons != null && RoadButtons.Length > 0 && RoadButtons[0] != null)
                RoadButtons[0].OnClick.AddListener(OnRoadClick);
            if (TerrainButtons != null && TerrainButtons.Length > 0 && TerrainButtons[0] != null)
                TerrainButtons[0].OnClick.AddListener(OnGrassClick);
            if (BuildingButtons != null)
            {
                for (int i = 0; i < BuildingButtons.Length; i++)
                {
                    int idx = i;
                    var btn = BuildingButtons[i];
                    if (btn == null) continue;
                    btn.OnClick.AddListener(() => OnBuildingClick(idx));
                }
            }
            if (ForestButtons != null)
            {
                for (int i = 0; i < ForestButtons.Length; i++)
                {
                    int idx = i;
                    var btn = ForestButtons[i];
                    if (btn == null) continue;
                    btn.OnClick.AddListener(() => OnForestClick(idx));
                }
            }
            if (BulldozeButton != null)
                BulldozeButton.OnClick.AddListener(OnBulldozeClick);
            Subscribed = true;
        }

        /// <summary>Remove all OnClick listeners. Hub calls from OnDestroy.</summary>
        public void UnsubscribeClicks()
        {
            if (!Subscribed) return;
            RemoveAll(ZoningButtons);
            RemoveAll(RoadButtons);
            RemoveAll(TerrainButtons);
            RemoveAll(BuildingButtons);
            RemoveAll(ForestButtons);
            if (BulldozeButton != null) BulldozeButton.OnClick.RemoveAllListeners();
            Subscribed = false;
        }

        private static void RemoveAll(IlluminatedButton[] arr)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
            {
                var btn = arr[i];
                if (btn != null) btn.OnClick.RemoveAllListeners();
            }
        }

        // ── Click dispatch ────────────────────────────────────────────────────────

        private void OnZoningClick(int index)
        {
            if (_uiManager == null) return;
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
            switch (index)
            {
                case 0: _uiManager.OnPowerFamilyButtonClicked(); break;
                case 1: _uiManager.OnWaterFamilyButtonClicked(); break;
            }
        }

        private void OnForestClick(int index)
        {
            if (_uiManager == null) return;
            switch (index)
            {
                case 0: _uiManager.OnForestsFamilyButtonClicked(); break;
                case 1: _uiManager.OnMediumForestButtonClicked(); break;
                case 2: _uiManager.OnDenseForestButtonClicked(); break;
            }
        }

        private void OnBulldozeClick()
        {
            if (_uiManager == null) return;
            _uiManager.OnBulldozeButtonClicked();
        }

        // ── Action registry ───────────────────────────────────────────────────────

        /// <summary>Register Wave B1 action ids. Hub calls from Awake after Wire.</summary>
        public void RegisterActions()
        {
            if (_actionRegistry == null) return;
            _actionRegistry.Register("action.tool-select",   OnActionToolSelect);
            _actionRegistry.Register("action.tool-deselect", OnActionToolDeselect);
            _actionRegistry.Register("action.subtype-open",  OnActionSubtypeOpen);
            _actionRegistry.Register("action.subtype-pick",  OnActionSubtypePick);
            _actionRegistry.Register("action.subtype-arm",   OnActionSubtypeArm);
            _actionRegistry.Register("action.subtype-disarm", OnActionSubtypeDisarm);
        }

        /// <summary>Initialize picker visibility bind. Hub calls from Awake after Wire.</summary>
        public void InitPickerBinds()
        {
            if (_bindRegistry == null) return;
            _bindRegistry.Set("toolSelection.activeFamily",  string.Empty);
            _bindRegistry.Set("toolSelection.activeSubtype", string.Empty);
            _bindRegistry.Set("toolSelection.stripVisible",  false);
            _bindRegistry.Subscribe<bool>("toolSelection.stripVisible", OnPickerVisibilityChanged);
        }

        private void OnPickerVisibilityChanged(bool visible)
        {
            if (_subtypePickerRoot != null)
                _subtypePickerRoot.SetActive(visible);
        }

        // ── Action handlers ───────────────────────────────────────────────────────

        private void OnActionToolSelect(object payload)
        {
            var family = payload as string ?? string.Empty;
            if (_bindRegistry != null)
            {
                _bindRegistry.Set("toolSelection.activeFamily", family);
                bool hasVariant = !string.IsNullOrEmpty(family)
                    && family != "DemolishCell" && family != "DemolishArea";
                _bindRegistry.Set("toolSelection.stripVisible", hasVariant);
            }
        }

        private void OnActionToolDeselect(object payload)
        {
            if (_bindRegistry != null)
            {
                _bindRegistry.Set("toolSelection.activeFamily",  string.Empty);
                _bindRegistry.Set("toolSelection.activeSubtype", string.Empty);
                _bindRegistry.Set("toolSelection.stripVisible",  false);
            }
        }

        private void OnActionSubtypeOpen(object payload)
        {
            if (_bindRegistry != null)
                _bindRegistry.Set("toolSelection.stripVisible", true);
        }

        private void OnActionSubtypePick(object payload)
        {
            if (_bindRegistry != null)
                _bindRegistry.Set("toolSelection.activeSubtype", payload as string ?? string.Empty);
        }

        private void OnActionSubtypeArm(object payload)
        {
            OnActionSubtypePick(payload);
        }

        private void OnActionSubtypeDisarm(object payload)
        {
            OnActionToolDeselect(payload);
        }

        // ── Illumination update (called from hub Update) ──────────────────────────

        /// <summary>
        /// Read UIManager selection state and write illumination alphas.
        /// Hub calls from Update; UIManager null guard is internal.
        /// </summary>
        public void UpdateIllumination()
        {
            if (_uiManager == null) return;

            bool bulldoze     = _uiManager.isBulldozeMode();
            bool buildingMode = _uiManager.IsBuildingPlacementMode();
            IBuilding selectedBuilding = _uiManager.GetSelectedBuilding();
            IForest   selectedForest   = _uiManager.GetSelectedForest();
            Zone.ZoneType zone         = _uiManager.GetSelectedZoneType();

            int activeBuildingIdx = -1;
            int activeForestIdx   = -1;
            int activeZoningIdx   = -1;
            bool roadActive       = false;
            bool grassActive      = false;

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
                        case Zone.ZoneType.Grass:                      grassActive = true; break;
                        case Zone.ZoneType.Road:                       roadActive  = true; break;
                        case Zone.ZoneType.ResidentialLightZoning:     activeZoningIdx = 0; break;
                        case Zone.ZoneType.ResidentialMediumZoning:    activeZoningIdx = 1; break;
                        case Zone.ZoneType.ResidentialHeavyZoning:     activeZoningIdx = 2; break;
                        case Zone.ZoneType.CommercialLightZoning:      activeZoningIdx = 3; break;
                        case Zone.ZoneType.CommercialMediumZoning:     activeZoningIdx = 4; break;
                        case Zone.ZoneType.CommercialHeavyZoning:      activeZoningIdx = 5; break;
                        case Zone.ZoneType.IndustrialLightZoning:      activeZoningIdx = 6; break;
                        case Zone.ZoneType.IndustrialMediumZoning:     activeZoningIdx = 7; break;
                        case Zone.ZoneType.IndustrialHeavyZoning:      activeZoningIdx = 8; break;
                        case Zone.ZoneType.StateServiceLightZoning:
                        case Zone.ZoneType.StateServiceMediumZoning:
                        case Zone.ZoneType.StateServiceHeavyZoning:    activeZoningIdx = 9; break;
                    }
                }
            }

            WriteAlphaArray(ZoningButtons,   activeZoningIdx);
            WriteAlphaArray(BuildingButtons, activeBuildingIdx);
            WriteAlphaArray(ForestButtons,   activeForestIdx);
            WriteAlphaSingle(RoadButtons,    roadActive);
            WriteAlphaSingle(TerrainButtons, grassActive);
            if (BulldozeButton != null)
                BulldozeButton.IlluminationAlpha = bulldoze ? 1f : 0f;
        }

        // ── Index resolution ──────────────────────────────────────────────────────

        private int ResolveBuildingIndex(IBuilding b)
        {
            if (b == null || BuildingButtons == null) return -1;
            var name = b.GetType().Name;
            if (name.IndexOf("Nuclear", System.StringComparison.OrdinalIgnoreCase) >= 0) return 0;
            if (name.IndexOf("Water",   System.StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            return -1;
        }

        private int ResolveForestIndex(IForest f)
        {
            if (f == null) return -1;
            var name = f.GetType().Name;
            if (name.IndexOf("Sparse", System.StringComparison.OrdinalIgnoreCase) >= 0) return 0;
            if (name.IndexOf("Medium", System.StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            if (name.IndexOf("Dense",  System.StringComparison.OrdinalIgnoreCase) >= 0) return 2;
            return -1;
        }

        // ── Static helpers ────────────────────────────────────────────────────────

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
    }
}
