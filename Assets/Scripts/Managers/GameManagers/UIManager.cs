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
    StatsPanel,
    TaxPanel
}

/// <summary>
/// Manages the main game UI including popups (load game, details, building selector, stats, taxes),
/// toolbar state, selected zone/tool tracking, demand gauge visualization, and optional first-session welcome briefing.
/// Implementation is split across partial files (<c>UIManager.PopupStack</c>, <c>UIManager.Hud</c>, <c>UIManager.Toolbar</c>,
/// <c>UIManager.Utilities</c>, <c>UIManager.Theme</c>, <c>UIManager.WelcomeBriefing</c>) for merge-friendly edits.
/// Coordinates with ZoneManager for zone selection, CursorManager for cursor state, and EconomyManager for tax display.
/// Grid coordinate debug text is refreshed in <see cref="LateUpdate"/> so it matches <see cref="GridManager.mouseGridPosition"/> after grid input runs.
/// </summary>
public partial class UIManager : MonoBehaviour
{
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
    #endregion

    #region State
    private GameObject ghostPreviewPrefab;
    private int ghostPreviewSize = 1;



    public bool bulldozeMode;
    public bool detailsMode;
    public string saveName;
    private string saveFolderPath;
    public float tooltipDisplayTime = 3f;
    #endregion

    #region UI References
    public Text populationText;
    public Text moneyText;
    public Text happinessText;
    public Text gridCoordinatesText;
    public Text cityPowerOutputText;
    public Text cityPowerConsumptionText;
    public Text dateText;
    public Text cityNameText;
    public Text residentialTaxText;
    public Text commercialTaxText;
    public Text industrialTaxText;
    public Text buttonMoneyText;
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

    /// <summary>HUD demand gauge fills (created at runtime under stat panels when theme is assigned).</summary>
    private Image demandResidentialBarFill;
    private Image demandCommercialBarFill;
    private Image demandIndustrialBarFill;

    private GameObject welcomeBriefingRoot;
    private Coroutine loadMenuFadeRoutine;
    #endregion

    /// <summary>Duration for CanvasGroup popup fades; clamped for safety.</summary>
    public float PopupFadeDurationSeconds => Mathf.Clamp(popupFadeDurationSeconds, 0.02f, 1f);

    #region Initialization
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

        EnsureConstructionCostTextExists();
        ApplyHudUiThemeIfConfigured();
        RequestToolbarChromeRefresh();
        TryShowWelcomeBriefingAfterStart();
    }

    /// <summary>
    /// Creates the construction cost text UI element at runtime if not assigned in the Inspector.
    /// </summary>
    /// <summary>
    /// Creates a floating <see cref="Text"/> near the cursor when unassigned (no panel box — readability from <see cref="Shadow"/> only).
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

        // Esc: dismiss welcome briefing first, then last opened pop-up, or close all if stack empty
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsWelcomeBriefingVisible())
            {
                DismissWelcomeBriefing();
                return;
            }

            if (popupStack.Count > 0)
            {
                PopupType last = popupStack.Pop();
                ClosePopup(last);
            }
            else
            {
                CloseAllPopups();
            }
        }
    }

    void LateUpdate()
    {
        if (toolbarChromeDirty)
        {
            toolbarChromeDirty = false;
            RefreshToolbarToolChrome();
        }

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
