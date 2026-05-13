// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.IO;
using System.Collections.Generic;
using Territory.Core;
using Territory.Roads;
using Territory.Zones;
using Territory.Economy;
using Territory.Terrain;
using Territory.Timing;
using Territory.Persistence;
using Territory.Forests;
using Territory.Buildings;
using Territory.Utilities;
using Territory.UI.Registry;

namespace Territory.UI
{
public enum PopupType
{
    LoadGame,
    Details,
    BuildingSelector,
    TaxPanel,
    SubTypePicker,
    BudgetPanel,
    InfoPanel,
    PauseMenu,
    SettingsScreen,
    SaveLoadScreen,
    NewGameScreen,
    // TECH-14102 / Stage 8 D9: tool-selected counts as escape-stack frame.
    // Esc-stack priority newest-first: SubTypePicker > ToolSelected > {modals} > {menus} > PauseMenu (fallback).
    ToolSelected
}

/// <summary>
/// Main game UI: popups (load game, details, building selector, stats, taxes), toolbar state, selected zone/tool, demand gauges, first-session welcome briefing.
/// Split across partials (<c>UIManager.PopupStack</c>, <c>UIManager.Hud</c>, <c>UIManager.Toolbar</c>, <c>UIManager.Utilities</c>, <c>UIManager.Theme</c>, <c>UIManager.WelcomeBriefing</c>) for merge-friendly edits.
/// Coords with ZoneManager (zone selection), CursorManager (cursor state), EconomyManager (tax display).
/// Grid coord debug text refreshed in <see cref="LateUpdate"/> → matches <see cref="GridManager.mouseGridPosition"/> after grid input runs.
/// </summary>
public partial class UIManager : MonoBehaviour
{
    /// <summary>Cached scene-singleton for trigger-side modal pushes (Stage 12 / game-ui-design-system). Set in Awake; cleared in OnDestroy.</summary>
    public static UIManager Instance { get; private set; }

    #region Dependencies
    public ZoneManager zoneManager;
    public CursorManager cursorManager;
    public GridManager gridManager;
    public TimeManager timeManager;
    public EconomyManager economyManager;
    public DetailsPopupController detailsPopupController;
    public GameManager gameManager;

    public TerrainManager terrainManager;
    public BuildingSelectorMenuController buildingSelectorMenuController;
    public CityStats cityStats;
    private WaterManager waterManager;
    [SerializeField] private EmploymentManager employmentManager;
    [SerializeField] private DemandManager demandManager;

    [Header("Mini-map")]
    [SerializeField] private MiniMapController miniMapController;

    [Header("Zone S")]
    [SerializeField] private ZoneSService zoneSService;
    [SerializeField] private SubtypePickerController subtypePickerController;
    [SerializeField] private BudgetPanel budgetPanel;

    [Header("Stage 8 Modal Roots")]
    [SerializeField] private GameObject infoPanelRoot;
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject settingsScreenRoot;
    [SerializeField] private GameObject saveLoadScreenRoot;
    [SerializeField] private GameObject newGameScreenRoot;

    [Header("Modal Coordination (Wave B4 — TECH-27095)")]
    [SerializeField] private Territory.UI.Modals.ModalCoordinator _modalCoordinator;
    #endregion

    #region State
    private GameObject ghostPreviewPrefab;
    private int ghostPreviewSize = 1;

    /// <summary>Transient sub-type id for Zone S placement; -1 = must pick.</summary>
    private int currentSubTypeId = -1;

    public bool bulldozeMode;
    public bool detailsMode;
    public string saveName;
    private string saveFolderPath;
    public float tooltipDisplayTime = 3f;
    #endregion

    #region UI References
    // Stage 6 (game-ui-design-system): legacy HUD-bar Text fields decommissioned —
    // populationText / moneyText / happinessText / cityNameText / buttonMoneyText replaced by
    // baked StudioControl SO refs on hud-bar prefab driven by HudBarDataAdapter.
    public Text gridCoordinatesText;
    public Text cityPowerOutputText;
    public Text cityPowerConsumptionText;
    public Text dateText;
    public Text residentialTaxText;
    public Text commercialTaxText;
    public Text industrialTaxText;
    public Text detailsNameText;
    public Text detailsOccupancyText;
    public Text detailsHappinessText;
    public Text detailsPowerOutputText;
    public Text detailsPowerConsumptionText;
    public Text detailsDateBuiltText;
    public Text detailsBuildingTypeText;
    public Text detailsSortingOrderText;
    public Text detailsDesirabilityText;
    public Text GameSavedText;
    public Text unemploymentRateText;
    public Text totalJobsText;
    public Text demandResidentialText;
    public Text demandCommercialText;
    public Text demandIndustrialText;
    public Text demandFeedbackText;
    public Text totalJobsCreatedText;
    public Text availableJobsText;
    public Text jobsTakenText;
    public Text cityWaterOutputText;
    public Text cityWaterConsumptionText;
    public Text insufficientFundsText;

    [Header("Construction Cost")]
    [Tooltip("Floating text near cursor showing construction cost before placement. Create under Canvas, assign here.")]
    public Text constructionCostText;
    [Tooltip("Offset from mouse position (pixels). Default: 24 right, -24 down.")]
    public Vector2 constructionCostOffset = new Vector2(24, -24);

    [Header("HUD — economy hints (optional)")]
    [Tooltip("When set, shows estimated monthly surplus after envelope cap.")]
    [SerializeField] private Text hudEstimatedSurplusHintText;

    [Header("Debug (optional)")]
    [SerializeField] private GameDebugInfoBuilder gameDebugInfoBuilder;
    [Tooltip("If set, use full debug text (coordinates + cell + placement). Otherwise only coordinates.")]
    [SerializeField] private bool useFullDebugText = true;
    [Tooltip("Optional. If set, shows cell debug info (height, zone, water, etc.) when tile details are open.")]
    [SerializeField] private Text detailsDebugText;

    public Image detailsImage;

    [Header("Selected types")]
    private Zone.ZoneType selectedZoneType;
    private IBuilding selectedBuilding;
    private IForest selectedForest;
    private ForestSelectionData selectedForestData;
    private Coroutine hideTooltipCoroutine;

    public GameObject powerPlantAPrefab;
    public GameObject savedGameButtonPrefab;
    public GameObject waterPumpPrefab;
    public GameObject denseForestPrefab;
    public GameObject mediumForestPrefab;
    public GameObject sparseForestPrefab;

    [Header("Demolition Animation")]
    [SerializeField] private GameObject demolitionExplosionPrefab;

    public GameObject loadGameMenu;
    public Transform savedGamesListContainer;
    public GameObject demandWarningPanel;
    public GameObject insufficientFundsPanel;

    [Header("Pop-up stack (Esc: close last opened, then close all)")]
    [SerializeField] private DataPopupController dataPopupController;
    private Stack<PopupType> popupStack = new Stack<PopupType>();

    [Header("Popup motion (CanvasGroup fade)")]
    [SerializeField] private float popupFadeDurationSeconds = 0.12f;

    [Header("Welcome briefing (first session)")]
    [SerializeField] private bool showWelcomeBriefingOnFirstRun = true;

    /// <summary>HUD demand gauge fills (created at runtime under stat panels when theme assigned).</summary>
    private Image demandResidentialBarFill;
    private Image demandCommercialBarFill;
    private Image demandIndustrialBarFill;

    private GameObject welcomeBriefingRoot;
    private Coroutine loadMenuFadeRoutine;
    private bool economyHudRuntimeWired;
    #endregion

    /// <summary>CanvasGroup popup fade duration; clamped for safety.</summary>
    public float PopupFadeDurationSeconds => Mathf.Clamp(popupFadeDurationSeconds, 0.02f, 1f);

    #region Initialization
    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        if (cityStats == null)
        {
            cityStats = FindObjectOfType<CityStats>();
        }

        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();

        if (employmentManager == null)
            employmentManager = FindObjectOfType<EmploymentManager>();
        if (demandManager == null)
            demandManager = FindObjectOfType<DemandManager>();
        if (gameDebugInfoBuilder == null)
            gameDebugInfoBuilder = FindObjectOfType<GameDebugInfoBuilder>();

        selectedZoneType = Zone.ZoneType.Grass;
        bulldozeMode = false;

        saveFolderPath = Application.persistentDataPath;

        if (zoneSService == null)
            zoneSService = FindObjectOfType<ZoneSService>();

        EnsureEconomyHudRuntimeWiring();
        EnsureConstructionCostTextExists();
        EnsureBakedPanelsRuntimeWiring();
        EnsureEconomyHudBindPublisher();
        ApplyHudUiThemeIfConfigured();
        // Stage 11: RequestToolbarChromeRefresh() removed — toolbar tinting now baked into ThemedToolbarStrip.
        TryShowWelcomeBriefingAfterStart();
        RegisterAutoModeAction();
    }

    /// <summary>
    /// Stage 10 hotfix — instantiate baked panel prefabs (budget-panel, stats-panel, pause-menu, save-load-view)
    /// under "UI Canvas/Modals" + "UI Canvas/SubViews" anchors. Register each with ModalCoordinator
    /// so the coordinator owns SetActive on open/close. Mirrors MainMenuController sub-view swap pattern.
    /// Idempotent — re-finding instances by name avoids dup-instantiation across scene reloads.
    /// </summary>
    private void EnsureBakedPanelsRuntimeWiring()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Ensure ModalCoordinator exists in scene (host for SetActive toggling).
        if (_modalCoordinator == null)
            _modalCoordinator = FindObjectOfType<Territory.UI.Modals.ModalCoordinator>();
        if (_modalCoordinator == null)
        {
            var coordGo = new GameObject("ModalCoordinator");
            coordGo.transform.SetParent(canvas.transform, false);
            _modalCoordinator = coordGo.AddComponent<Territory.UI.Modals.ModalCoordinator>();
        }

        // Find/create canvas-layer parent transforms.
        Transform modalsParent   = FindOrCreateCanvasLayer(canvas.transform, "Modals");
        Transform subViewsParent = FindOrCreateCanvasLayer(canvas.transform, "SubViews");

        // panel slug → (parent, adapter component type)
        InstantiateBakedPanel("budget-panel",   modalsParent,   typeof(Territory.UI.Modals.BudgetPanelAdapter));
        InstantiateBakedPanel("stats-panel",    modalsParent,   typeof(Territory.UI.Modals.StatsPanelAdapter));
        InstantiateBakedPanel("pause-menu",     modalsParent,   typeof(Territory.UI.Modals.PauseMenuDataAdapter));
        InstantiateBakedPanel("save-load-view", subViewsParent, typeof(Territory.UI.Modals.SaveLoadScreenDataAdapter));

        // Retire legacy saveLoadScreenRoot — new save-load-view replaces it.
        if (saveLoadScreenRoot != null)
        {
            saveLoadScreenRoot.SetActive(false);
            Destroy(saveLoadScreenRoot);
            saveLoadScreenRoot = null;
        }

        // Stage 13 hotfix — retire legacy pauseMenuRoot (full-screen scene GameObject).
        // Baked pause-menu under Canvas/Modals replaces it; Esc routing in UIManager.PopupStack
        // now drives ModalCoordinator.TryOpen("pause-menu") instead of pauseMenuRoot.SetActive.
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(false);
            Destroy(pauseMenuRoot);
            pauseMenuRoot = null;
        }
    }

    private static Transform FindOrCreateCanvasLayer(Transform canvasRoot, string layerName)
    {
        var existing = canvasRoot.Find(layerName);
        if (existing != null) return existing;

        var go = new GameObject(layerName, typeof(RectTransform));
        go.transform.SetParent(canvasRoot, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go.transform;
    }

    /// <summary>
    /// Stage 10 hotfix — attach EconomyHudBindPublisher so baked HUD-bar BUDGET button text
    /// receives economyManager.totalBudget + economyManager.budgetDelta values.
    /// </summary>
    private void EnsureEconomyHudBindPublisher()
    {
        var existing = FindObjectOfType<Territory.UI.HUD.EconomyHudBindPublisher>();
        if (existing != null) return;
        gameObject.AddComponent<Territory.UI.HUD.EconomyHudBindPublisher>();
    }

    private void InstantiateBakedPanel(string slug, Transform parent, System.Type adapterType)
    {
        if (parent == null || string.IsNullOrEmpty(slug)) return;

        // Idempotency: if already instantiated under parent, just ensure adapter + registration.
        Transform existing = parent.Find(slug);
        GameObject panelGo = existing != null ? existing.gameObject : null;

        if (panelGo == null)
        {
#if UNITY_EDITOR
            string assetPath = $"Assets/UI/Prefabs/Generated/{slug}.prefab";
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[UIManager] EnsureBakedPanelsRuntimeWiring — prefab missing at {assetPath} for slug={slug}; skipping.");
                return;
            }
            panelGo = Instantiate(prefab, parent);
            panelGo.name = slug;
#else
            Debug.LogWarning($"[UIManager] EnsureBakedPanelsRuntimeWiring — runtime prefab load not implemented for slug={slug}; bake outputs are Editor-only assets.");
            return;
#endif
        }

        // Ensure adapter component attached.
        if (adapterType != null && panelGo.GetComponent(adapterType) == null)
            panelGo.AddComponent(adapterType);

        // Register with ModalCoordinator so it owns SetActive on open/close.
        if (_modalCoordinator != null)
            _modalCoordinator.RegisterPanel(slug, panelGo);
        else
            panelGo.SetActive(false);
    }

    /// <summary>
    /// Stage 10 hotfix — register action.auto-mode-toggle on UiActionRegistry.
    /// Flips cityStats.simulateGrowth and publishes uiManager.isAutoMode bind for visual reflection.
    /// No panel opens.
    /// </summary>
    private void RegisterAutoModeAction()
    {
        var actionRegistry = FindObjectOfType<UiActionRegistry>();
        if (actionRegistry == null) return;
        var bindRegistry = FindObjectOfType<UiBindRegistry>();
        actionRegistry.Register("action.auto-mode-toggle", _ =>
        {
            if (cityStats == null) return;
            cityStats.simulateGrowth = !cityStats.simulateGrowth;
            bindRegistry?.Set("uiManager.isAutoMode", cityStats.simulateGrowth);
        });
        // Seed initial bind value so subscribers reflect current state.
        if (cityStats != null) bindRegistry?.Set("uiManager.isAutoMode", cityStats.simulateGrowth);
    }

    /// <summary>Current Zone S sub-type id; -1 = not picked yet.</summary>
    public int CurrentSubTypeId => currentSubTypeId;

    /// <summary>Set by <see cref="SubtypePickerController"/> on selection.</summary>
    public void SetCurrentSubTypeId(int id) { currentSubTypeId = id; }

    /// <summary>Zone S placement service reference for <see cref="GridManager"/> routing.</summary>
    public ZoneSService ZoneSService => zoneSService;

    /// <summary>TECH-10500: open the unified subtype picker for the given tool family (R/C/I density tiers or Zone S catalog).</summary>
    public void ShowSubtypePicker(ToolFamily family) => ShowSubtypePicker(family, int.MinValue);

    /// <summary>TECH-10500: open picker with a pre-selected default tile (key = (int)Zone.ZoneType for R/C/I, subTypeId for State).</summary>
    public void ShowSubtypePicker(ToolFamily family, int defaultKey)
    {
        EnsureSubtypePickerRuntimeWiring();
        if (subtypePickerController != null)
        {
            subtypePickerController.Show(this, family, defaultKey);
            RegisterPopupOpened(PopupType.SubTypePicker);
        }
    }

    /// <summary>Backward-compat shim — defaults to StateService family (legacy Zone S entry point).</summary>
    public void OpenSubTypePicker() => ShowSubtypePicker(ToolFamily.StateService);

    /// <summary>Picker controller accessor for PopupStack close routing.</summary>
    public SubtypePickerController SubtypePickerController => subtypePickerController;

    private void EnsureSubtypePickerRuntimeWiring()
    {
        if (subtypePickerController != null) return;
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;
        GameObject go = new GameObject("SubtypePickerController");
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();
        subtypePickerController = go.AddComponent<SubtypePickerController>();
    }

    /// <summary>Open budget panel from HUD.</summary>
    public void OpenBudgetPanel()
    {
        if (budgetPanel != null)
        {
            budgetPanel.Show();
            RegisterPopupOpened(PopupType.BudgetPanel);
        }
    }

    /// <summary>
    /// Instantiate budget panel and HUD surplus widget when scene has no Inspector wiring.
    /// UI parents under the first <see cref="Canvas"/> so layout stacks above world space.
    /// </summary>
    private void EnsureEconomyHudRuntimeWiring()
    {
        if (economyHudRuntimeWired)
            return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        if (budgetPanel == null)
        {
            GameObject go = new GameObject("BudgetPanel");
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsLastSibling();
            RectTransform prt = go.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            budgetPanel = go.AddComponent<BudgetPanel>();
        }

        if (hudEstimatedSurplusHintText == null)
        {
            GameObject hintGo = new GameObject("HudEstimatedSurplusHint");
            hintGo.transform.SetParent(canvas.transform, false);
            RectTransform hrt = hintGo.AddComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.5f, 1f);
            hrt.anchorMax = new Vector2(0.5f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, -96f);
            hrt.sizeDelta = new Vector2(900f, 26f);
            Text ht = hintGo.AddComponent<Text>();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) ht.font = font;
            ht.fontSize = 14;
            ht.color = new Color(0.85f, 0.92f, 1f);
            ht.alignment = TextAnchor.MiddleCenter;
            ht.horizontalOverflow = HorizontalWrapMode.Wrap;
            ht.verticalOverflow = VerticalWrapMode.Truncate;
            ht.raycastTarget = false;
            hudEstimatedSurplusHintText = ht;
        }

        economyHudRuntimeWired = true;
    }

    /// <summary>
    /// Create construction cost text UI at runtime if Inspector left unassigned.
    /// Floating <see cref="Text"/> near cursor (no panel box — readability via <see cref="Shadow"/> only).
    /// </summary>
    private void EnsureConstructionCostTextExists()
    {
        if (constructionCostText != null)
            return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        GameObject costObj = new GameObject("ConstructionCostText");
        costObj.transform.SetParent(canvas.transform, false);

        Text text = costObj.AddComponent<Text>();
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont != null)
            text.font = defaultFont;
        else if (demandFeedbackText != null)
            text.font = demandFeedbackText.font;
        text.fontSize = 16;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform rt = costObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(150, 40);
        rt.anchoredPosition = Vector2.zero;

        Shadow shadow = costObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(1, -1);

        constructionCostText = text;
        constructionCostText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (cityStats != null)
        {
            UpdateUI();
        }

        // Esc: dispatched to HandleEscapePress() — extracted for EditMode test reach (TECH-14102 / Stage 8 D9).
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscapePress();
        }
    }

    /// <summary>
    /// Iterative escape state machine (TECH-14102 / Stage 8 D9).
    /// Frame priority newest-first via popupStack: SubTypePicker > ToolSelected > {modals} > {menus} > PauseMenu (fallback).
    /// One press pops one frame. Pause-menu = fallback when stack empty AND running scene (buildIndex==1).
    /// </summary>
    public void HandleEscapePress()
    {
        if (IsWelcomeBriefingVisible())
        {
            DismissWelcomeBriefing();
            return;
        }

        // Stage 13 hotfix — consult ModalCoordinator before popupStack/PauseMenu fallback.
        // popupStack + ModalCoordinator._openModals are independent state stores; baked
        // panels (stats/budget/pause) register via ModalCoordinator only, so Esc would
        // otherwise stack pause-menu on top of an already-open modal.
        var modalCoord = FindObjectOfType<Territory.UI.Modals.ModalCoordinator>();
        if (modalCoord != null && modalCoord.IsAnyExclusiveOpen())
        {
            // Pause-menu navigated-view: in sub-view (settings/save/load), Esc = back to root.
            // Only when root visible does Esc close the modal.
            if (modalCoord.IsOpen("pause-menu"))
            {
                var pauseAdapter = FindObjectOfType<Territory.UI.Modals.PauseMenuDataAdapter>();
                if (pauseAdapter != null && pauseAdapter.TryHandleBackButton()) return;
            }
            foreach (var slug in new[] { "stats-panel", "budget-panel", "pause-menu" })
            {
                if (modalCoord.IsOpen(slug)) { modalCoord.Close(slug); return; }
            }
        }

        if (popupStack.Count > 0)
        {
            ClosePopup(popupStack.Peek());
        }
        else if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 1)
        {
            OpenPopup(PopupType.PauseMenu);
        }
        else
        {
            CloseAllPopups();
        }
    }

    void LateUpdate()
    {
        // Stage 11: toolbarChromeDirty / RefreshToolbarToolChrome dispatch removed — ThemedToolbarStrip self-tints.
        // BUG-60: dropped `cityStats == null` guard — CellDataPanel debug text is grid-only,
        // does not depend on cityStats; pre-cityStats frames now still render hover coords.
        UpdateCellDataPanelText();
    }
    #endregion

}

[System.Serializable]
public struct ForestSelectionData
{
    public Forest.ForestType forestType;
    public GameObject prefab;
}
}
