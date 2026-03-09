using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Territory.Core;
using Territory.Zones;
using Territory.Terrain;
using Territory.Roads;

namespace Territory.UI
{
/// <summary>
/// Renders a procedural mini-map from grid cell data and provides click-to-navigate.
/// Displays zones, roads, water, interstate (thicker), and a viewport rectangle.
/// Hides during full-screen popups (LoadGame, BuildingSelector).
/// </summary>
public class MiniMapController : MonoBehaviour, IPointerClickHandler
{
    #region Dependencies
    public GridManager gridManager;
    public WaterManager waterManager;
    public InterstateManager interstateManager;
    public CameraController cameraController;
    #endregion

    #region UI References
    [Header("UI References")]
    public RawImage mapImage;
    public RectTransform viewportRect;
    public GameObject miniMapPanel;
    #endregion

    #region Configuration
    [Header("Configuration")]
    [Tooltip("Seconds between map texture rebuilds")]
    [SerializeField] private float rebuildInterval = 0.5f;
    #endregion

    #region Colors
    private static readonly Color ColorGrass = new Color(0.176f, 0.353f, 0.118f);      // #2D5A1E dark green
    private static readonly Color ColorResidential = new Color(0.298f, 0.686f, 0.314f); // #4CAF50
    private static readonly Color ColorCommercial = new Color(0.129f, 0.588f, 0.953f);  // #2196F3
    private static readonly Color ColorIndustrial = new Color(1f, 0.757f, 0.027f);      // #FFC107
    private static readonly Color ColorBuilding = new Color(0.102f, 0.102f, 0.102f);     // #1A1A1A black
    private static readonly Color ColorRoad = Color.white;
    private static readonly Color ColorInterstate = new Color(0.5f, 0.5f, 0.5f); // Gray, same thickness as roads
    private static readonly Color ColorWater = new Color(0.082f, 0.396f, 0.753f);       // #1565C0
    #endregion

    #region State
    private Texture2D mapTexture;
    private float rebuildTimer;
    private HashSet<Vector2Int> interstateSet;
    private HashSet<Vector2Int> roadSet;
    private bool wasVisibleBeforePopup = true;
    #endregion

    #region Public API
    /// <summary>Whether the mini-map panel is currently visible.</summary>
    public bool IsVisible => (miniMapPanel != null ? miniMapPanel : gameObject).activeSelf;

    /// <summary>Shows or hides the mini-map panel.</summary>
    public void SetVisible(bool visible)
    {
        GameObject target = miniMapPanel != null ? miniMapPanel : gameObject;
        target.SetActive(visible);
    }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (waterManager == null) waterManager = FindObjectOfType<WaterManager>();
        if (interstateManager == null) interstateManager = FindObjectOfType<InterstateManager>();
        if (cameraController == null) cameraController = FindObjectOfType<CameraController>();
    }

    void Start()
    {
        if (gridManager != null && gridManager.onGridRestored != null)
            gridManager.onGridRestored += OnGridRestored;

        rebuildTimer = 0f;
    }

    void OnDestroy()
    {
        if (gridManager != null && gridManager.onGridRestored != null)
            gridManager.onGridRestored -= OnGridRestored;

        if (mapTexture != null)
        {
            Destroy(mapTexture);
            mapTexture = null;
        }
    }

    void Update()
    {
        if (gridManager == null || !gridManager.isInitialized)
            return;

        GameObject panel = miniMapPanel != null ? miniMapPanel : gameObject;
        if (!panel.activeSelf)
            return;

        rebuildTimer += Time.unscaledDeltaTime;
        if (rebuildTimer >= rebuildInterval)
        {
            rebuildTimer = 0f;
            RebuildTexture();
        }

        UpdateViewportRect();
    }
    #endregion

    #region Texture Rebuild
    private void OnGridRestored()
    {
        rebuildTimer = 0f;
        RebuildTexture();
    }

    /// <summary>Rebuilds the procedural map texture from cell data.</summary>
    public void RebuildTexture()
    {
        if (gridManager == null || !gridManager.isInitialized || mapImage == null)
            return;

        int w = gridManager.width;
        int h = gridManager.height;

        if (mapTexture == null || mapTexture.width != w || mapTexture.height != h)
        {
            if (mapTexture != null)
                Destroy(mapTexture);
            mapTexture = new Texture2D(w, h);
            mapTexture.filterMode = FilterMode.Point;
        }

        BuildInterstateSet();
        BuildRoadSet();

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color c = GetCellColor(x, y);
                mapTexture.SetPixel(x, y, c);
            }
        }

        mapTexture.Apply();
        mapImage.texture = mapTexture;
    }

    private void BuildInterstateSet()
    {
        interstateSet = new HashSet<Vector2Int>();
        if (interstateManager != null && interstateManager.InterstatePositions != null)
        {
            foreach (var pos in interstateManager.InterstatePositions)
                interstateSet.Add(pos);
        }
    }

    private void BuildRoadSet()
    {
        roadSet = new HashSet<Vector2Int>();
        if (gridManager != null)
        {
            var roads = gridManager.GetAllRoadPositions();
            if (roads != null)
            {
                foreach (var pos in roads)
                {
                    if (!interstateSet.Contains(pos))
                        roadSet.Add(pos);
                }
            }
        }
    }

    private Color GetCellColor(int x, int y)
    {
        // Roads and bridges first (bridges are roads on water cells)
        if (interstateSet != null && interstateSet.Contains(new Vector2Int(x, y)))
            return ColorInterstate;

        if (roadSet != null && roadSet.Contains(new Vector2Int(x, y)))
            return ColorRoad;

        if (waterManager != null && waterManager.IsWaterAt(x, y))
            return ColorWater;

        Cell cell = gridManager.GetCell(x, y);
        if (cell == null)
            return ColorGrass;

        Zone.ZoneType zt = cell.GetZoneType();

        if (IsBuilding(zt))
            return ColorBuilding;

        if (IsResidentialZoning(zt))
            return ColorResidential;
        if (IsCommercialZoning(zt))
            return ColorCommercial;
        if (IsIndustrialZoning(zt))
            return ColorIndustrial;

        return ColorGrass;
    }

    private static bool IsBuilding(Zone.ZoneType zt)
    {
        return zt == Zone.ZoneType.Building ||
               zt == Zone.ZoneType.ResidentialLightBuilding || zt == Zone.ZoneType.ResidentialMediumBuilding || zt == Zone.ZoneType.ResidentialHeavyBuilding ||
               zt == Zone.ZoneType.CommercialLightBuilding || zt == Zone.ZoneType.CommercialMediumBuilding || zt == Zone.ZoneType.CommercialHeavyBuilding ||
               zt == Zone.ZoneType.IndustrialLightBuilding || zt == Zone.ZoneType.IndustrialMediumBuilding || zt == Zone.ZoneType.IndustrialHeavyBuilding;
    }

    private static bool IsResidentialZoning(Zone.ZoneType zt)
    {
        return zt == Zone.ZoneType.ResidentialLightZoning || zt == Zone.ZoneType.ResidentialMediumZoning || zt == Zone.ZoneType.ResidentialHeavyZoning;
    }

    private static bool IsCommercialZoning(Zone.ZoneType zt)
    {
        return zt == Zone.ZoneType.CommercialLightZoning || zt == Zone.ZoneType.CommercialMediumZoning || zt == Zone.ZoneType.CommercialHeavyZoning;
    }

    private static bool IsIndustrialZoning(Zone.ZoneType zt)
    {
        return zt == Zone.ZoneType.IndustrialLightZoning || zt == Zone.ZoneType.IndustrialMediumZoning || zt == Zone.ZoneType.IndustrialHeavyZoning;
    }
    #endregion

    #region Viewport Rect
    private void UpdateViewportRect()
    {
        if (viewportRect == null || gridManager == null || !gridManager.isInitialized)
            return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camPos = cam.transform.position;
        float orthoSize = cam.orthographicSize;
        float halfW = orthoSize * cam.aspect;
        float halfH = orthoSize;

        Vector2 bl = new Vector2(camPos.x - halfW, camPos.y - halfH);
        Vector2 br = new Vector2(camPos.x + halfW, camPos.y - halfH);
        Vector2 tl = new Vector2(camPos.x - halfW, camPos.y + halfH);
        Vector2 tr = new Vector2(camPos.x + halfW, camPos.y + halfH);

        int minGridX = Mathf.Min((int)gridManager.GetGridPosition(bl).x, (int)gridManager.GetGridPosition(br).x,
            (int)gridManager.GetGridPosition(tl).x, (int)gridManager.GetGridPosition(tr).x);
        int maxGridX = Mathf.Max((int)gridManager.GetGridPosition(bl).x, (int)gridManager.GetGridPosition(br).x,
            (int)gridManager.GetGridPosition(tl).x, (int)gridManager.GetGridPosition(tr).x);
        int minGridY = Mathf.Min((int)gridManager.GetGridPosition(bl).y, (int)gridManager.GetGridPosition(br).y,
            (int)gridManager.GetGridPosition(tl).y, (int)gridManager.GetGridPosition(tr).y);
        int maxGridY = Mathf.Max((int)gridManager.GetGridPosition(bl).y, (int)gridManager.GetGridPosition(br).y,
            (int)gridManager.GetGridPosition(tl).y, (int)gridManager.GetGridPosition(tr).y);

        int w = gridManager.width;
        int h = gridManager.height;

        float minNormX = Mathf.Clamp01(minGridX / (float)w);
        float maxNormX = Mathf.Clamp01((maxGridX + 1) / (float)w);
        float minNormY = Mathf.Clamp01(minGridY / (float)h);
        float maxNormY = Mathf.Clamp01((maxGridY + 1) / (float)h);

        viewportRect.anchorMin = new Vector2(minNormX, minNormY);
        viewportRect.anchorMax = new Vector2(maxNormX, maxNormY);
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
    }
    #endregion

    #region Click-to-Navigate
    public void OnPointerClick(PointerEventData eventData)
    {
        if (gridManager == null || cameraController == null || mapImage == null)
            return;

        RectTransform rect = mapImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return;

        Rect rectLocal = rect.rect;
        float normX = (localPoint.x - rectLocal.xMin) / rectLocal.width;
        float normY = (localPoint.y - rectLocal.yMin) / rectLocal.height;

        int w = gridManager.width;
        int h = gridManager.height;

        int gridX = Mathf.Clamp(Mathf.FloorToInt(normX * w), 0, w - 1);
        int gridY = Mathf.Clamp(Mathf.FloorToInt(normY * h), 0, h - 1);

        Vector2 worldPos = gridManager.GetWorldPosition(gridX, gridY);
        cameraController.MoveCameraToMapCenter(new Vector3(worldPos.x, worldPos.y, 0));
    }
    #endregion
}

}
