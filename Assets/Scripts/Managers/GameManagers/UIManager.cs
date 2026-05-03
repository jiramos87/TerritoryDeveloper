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
    BondIssuance,
    InfoPanel,
    PauseMenu,
    SettingsScreen,
    SaveLoadScreen,
    NewGameScreen
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
    [SerializeField] private SubTypePickerModal subTypePickerModal;
    [SerializeField] private BudgetPanel budgetPanel;
    [SerializeField] private BondIssuanceModal bondIssuanceModal;

    [Header("Stage 8 Modal Roots")]
    [SerializeField] private GameObject infoPanelRoot;
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject settingsScreenRoot;
    [SerializeField] private GameObject saveLoadScreenRoot;
    [SerializeField] private GameObject newGameScreenRoot;
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
    [Tooltip("When set, shows estimated monthly surplus after envelope cap + bond repayment.")]
    [SerializeField] private Text hudEstimatedSurplusHintText;
    [Tooltip("When set, shows active bond summary; click opens read-only bond modal.")]
    [SerializeField] private Button bondHudBadgeButton;

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
    private BondLedgerService bondLedgerHud;
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
        if (bondIssuanceModal == null)
            bondIssuanceModal = FindObjectOfType<BondIssuanceModal>();
        if (bondLedgerHud == null)
            bondLedgerHud = FindObjectOfType<BondLedgerService>();

        EnsureEconomyHudRuntimeWiring();
        EnsureConstructionCostTextExists();
        if (bondHudBadgeButton != null)
        {
            bondHudBadgeButton.onClick.RemoveAllListeners();
            bondHudBadgeButton.onClick.AddListener(OpenBondDetailModal);
        }
        ApplyHudUiThemeIfConfigured();
        // Stage 11: RequestToolbarChromeRefresh() removed — toolbar tinting now baked into ThemedToolbarStrip.
        TryShowWelcomeBriefingAfterStart();
    }

    /// <summary>Current Zone S sub-type id; -1 = not picked yet.</summary>
    public int CurrentSubTypeId => currentSubTypeId;

    /// <summary>Set by <see cref="SubTypePickerModal"/> on selection.</summary>
    public void SetCurrentSubTypeId(int id) { currentSubTypeId = id; }

    /// <summary>Zone S placement service reference for <see cref="GridManager"/> routing.</summary>
    public ZoneSService ZoneSService => zoneSService;

    /// <summary>Open the sub-type picker modal for Zone S placement.</summary>
    public void OpenSubTypePicker()
    {
        if (subTypePickerModal != null)
        {
            subTypePickerModal.Show(this);
            RegisterPopupOpened(PopupType.SubTypePicker);
        }
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

    /// <summary>Open bond issuance modal (stacked popup).</summary>
    public void OpenBondIssuanceModal()
    {
        if (bondIssuanceModal != null)
        {
            bondIssuanceModal.ShowIssue(this);
            RegisterPopupOpened(PopupType.BondIssuance);
        }
    }

    /// <summary>Open read-only bond detail for active bond on city tier.</summary>
    public void OpenBondDetailModal()
    {
        if (bondIssuanceModal == null || economyManager == null) return;
        var ledger = FindObjectOfType<BondLedgerService>();
        if (ledger == null) return;
        int tier = economyManager.GetCityScaleTier();
        BondData bond = ledger.GetActiveBond(tier);
        if (bond == null) return;
        bondIssuanceModal.ShowReadOnly(this, bond);
        RegisterPopupOpened(PopupType.BondIssuance);
    }

    /// <summary>
    /// Instantiate bond modal, budget panel, and HUD surplus/bond widgets when scene has no Inspector wiring.
    /// UI parents under the first <see cref="Canvas"/> so layout stacks above world space.
    /// </summary>
    private void EnsureEconomyHudRuntimeWiring()
    {
        if (economyHudRuntimeWired)
            return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
            return;

        if (bondIssuanceModal == null)
        {
            GameObject go = new GameObject("BondIssuanceModal");
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsLastSibling();
            RectTransform brt = go.AddComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            bondIssuanceModal = go.AddComponent<BondIssuanceModal>();
        }

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

        if (bondHudBadgeButton == null)
        {
            GameObject badgeGo = new GameObject("BondHudBadge");
            badgeGo.transform.SetParent(canvas.transform, false);
            RectTransform art = badgeGo.AddComponent<RectTransform>();
            art.anchorMin = new Vector2(1f, 1f);
            art.anchorMax = new Vector2(1f, 1f);
            art.pivot = new Vector2(1f, 1f);
            art.anchoredPosition = new Vector2(-12f, -12f);
            art.sizeDelta = new Vector2(300f, 34f);
            var bg = badgeGo.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.16f, 0.22f, 0.92f);
            bg.raycastTarget = true;
            bondHudBadgeButton = badgeGo.AddComponent<Button>();
            bondHudBadgeButton.targetGraphic = bg;

            GameObject txtGo = new GameObject("Text");
            txtGo.transform.SetParent(badgeGo.transform, false);
            Text bt = txtGo.AddComponent<Text>();
            Font bfont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bfont != null) bt.font = bfont;
            bt.fontSize = 13;
            bt.color = Color.white;
            bt.alignment = TextAnchor.MiddleCenter;
            bt.raycastTarget = false;
            bt.text = "";
            RectTransform trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            badgeGo.SetActive(false);
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

        // Esc: dismiss welcome briefing first, then last opened pop-up; if stack empty AND running (MainScene) → open pause menu (Stage 12 trigger rewire). Falls through to legacy CloseAllPopups only when not in running scene.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsWelcomeBriefingVisible())
            {
                DismissWelcomeBriefing();
                return;
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
    }

    void LateUpdate()
    {
        // Stage 11: toolbarChromeDirty / RefreshToolbarToolChrome dispatch removed — ThemedToolbarStrip self-tints.
        if (cityStats == null)
            return;
        UpdateGridCoordinatesDebugText();
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
