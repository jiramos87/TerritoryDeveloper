using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Territory.Core;
using Territory.Zones;
using Territory.Terrain;
using Territory.Roads;
using Territory.Forests;
using Territory.Simulation;

namespace Territory.UI
{
/// <summary>
/// Mini-map layer flags. Multiple layers active simultaneously.
/// </summary>
[System.Flags]
public enum MiniMapLayer
{
    None = 0,
    Streets = 1 << 0,
    Zones = 1 << 1,
    Forests = 1 << 2,
    Desirability = 1 << 3,
    Centroid = 1 << 4,
}

/// <summary>
/// Render procedural mini-map from grid cells + click-to-navigate.
/// Shows zones, roads, water, interstate (thicker), viewport rect.
/// Toggleable layers: streets, zones, forests, desirability, centroid.
/// Texture rebuilds on geography complete, grid restore, panel open, layer change (not fixed timer).
/// Hides during full-screen popups (LoadGame, BuildingSelector).
/// </summary>
public class MiniMapController : MonoBehaviour, IPointerClickHandler
{
    #region Dependencies
    public GridManager gridManager;
    public WaterManager waterManager;
    public InterstateManager interstateManager;
    public CameraController cameraController;
    public AutoZoningManager autoZoningManager;
    public UrbanCentroidService urbanCentroidService;
    #endregion

    #region UI References
    [Header("UI References")]
    public RawImage mapImage;
    public RectTransform viewportRect;
    public GameObject miniMapPanel;
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
    private static readonly Color ColorDesirabilityLow = new Color(0.9f, 0.2f, 0.2f);
    private static readonly Color ColorDesirabilityHigh = new Color(0.2f, 0.8f, 0.2f);
    private static readonly Color ColorForestSparse = new Color(0.4f, 0.7f, 0.35f);
    private static readonly Color ColorForestMedium = new Color(0.25f, 0.55f, 0.25f);
    private static readonly Color ColorForestDense = new Color(0.15f, 0.4f, 0.15f);
    private static readonly Color ColorCentroid = new Color(1f, 0f, 1f); // Magenta marker
    private static readonly Color ColorRingBoundary = new Color(0.6f, 0.9f, 1f); // Light cyan
    #endregion

    #region State
    private Texture2D mapTexture;
    private HashSet<Vector2Int> interstateSet;
    private HashSet<Vector2Int> roadSet;
    private bool wasVisibleBeforePopup = true;
    private float desirabilityMin;
    private float desirabilityMax;

    [SerializeField] private MiniMapLayer activeLayers = MiniMapLayer.Streets | MiniMapLayer.Zones;
    #endregion

    #region Public API
    /// <summary>True if mini-map panel visible.</summary>
    public bool IsVisible => (miniMapPanel != null ? miniMapPanel : gameObject).activeSelf;

    /// <summary>Show/hide mini-map panel. Rebuilds texture on open → stays current.</summary>
    public void SetVisible(bool visible)
    {
        GameObject target = miniMapPanel != null ? miniMapPanel : gameObject;
        target.SetActive(visible);
        if (visible)
            RebuildTexture();
    }

    /// <summary>Toggle given layer on/off.</summary>
    public void ToggleLayer(MiniMapLayer layer)
    {
        activeLayers ^= layer;
        RebuildTexture();
    }

    /// <summary>True if layer currently active.</summary>
    public bool IsLayerActive(MiniMapLayer layer)
    {
        return (activeLayers & layer) != 0;
    }

    /// <summary>Active layers bitmask.</summary>
    public MiniMapLayer GetActiveLayers()
    {
        return activeLayers;
    }

    /// <summary>Set active layers (used on save restore).</summary>
    public void SetActiveLayers(MiniMapLayer layers)
    {
        activeLayers = layers;
        RebuildTexture();
    }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (waterManager == null) waterManager = FindObjectOfType<WaterManager>();
        if (interstateManager == null) interstateManager = FindObjectOfType<InterstateManager>();
        if (cameraController == null) cameraController = FindObjectOfType<CameraController>();
        if (autoZoningManager == null) autoZoningManager = FindObjectOfType<AutoZoningManager>();
        if (urbanCentroidService == null) urbanCentroidService = FindObjectOfType<UrbanCentroidService>();
    }

    void Start()
    {
        if (gridManager != null && gridManager.onGridRestored != null)
            gridManager.onGridRestored += OnGridRestored;
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

        UpdateViewportRect();
    }
    #endregion

    #region Texture Rebuild
    private void OnGridRestored()
    {
        RebuildTexture();
    }

    /// <summary>Rebuild procedural map texture from cell data.</summary>
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

        if ((activeLayers & MiniMapLayer.Desirability) != 0)
            ComputeDesirabilityRange(w, h);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color c = GetCellColor(x, y);
                mapTexture.SetPixel(x, y, c);
            }
        }

        if ((activeLayers & MiniMapLayer.Centroid) != 0 && (urbanCentroidService != null || (autoZoningManager != null && autoZoningManager.GetUrbanMetrics() != null)))
        {
            if (urbanCentroidService != null)
            {
                urbanCentroidService.RecalculateFromGrid();
            }
            UrbanMetrics urbanMetrics = urbanCentroidService != null ? urbanCentroidService.GetUrbanMetrics() : autoZoningManager.GetUrbanMetrics();
            if (urbanMetrics != null && gridManager != null)
            {
                Vector2 centroid = urbanCentroidService != null ? urbanCentroidService.GetCentroid() : urbanMetrics.GetCentroid();
                float[] boundaries = urbanCentroidService != null
                    ? urbanCentroidService.GetRingBoundaryDistances()
                    : urbanMetrics.GetRingBoundaryDistances();

                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        Vector2 cellCenter = new Vector2(x + 0.5f, y + 0.5f);
                        float dist = Vector2.Distance(cellCenter, centroid);
                        foreach (float b in boundaries)
                        {
                            if (Mathf.Abs(dist - b) < 0.45f)
                            {
                                mapTexture.SetPixel(x, y, ColorRingBoundary);
                                break;
                            }
                        }
                    }
                }

                int cx = Mathf.Clamp(Mathf.RoundToInt(centroid.x), 0, w - 1);
                int cy = Mathf.Clamp(Mathf.RoundToInt(centroid.y), 0, h - 1);
                const int markerRadius = 2;
                for (int dx = -markerRadius; dx <= markerRadius; dx++)
                {
                    int px = cx + dx;
                    if (px >= 0 && px < w)
                        mapTexture.SetPixel(px, cy, ColorCentroid);
                }
                for (int dy = -markerRadius; dy <= markerRadius; dy++)
                {
                    int py = cy + dy;
                    if (py >= 0 && py < h)
                        mapTexture.SetPixel(cx, py, ColorCentroid);
                }
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

    /// <summary>Build road set by scanning grid directly (cell.zoneType == Road).
    /// Bypass cache → auto-generated, manual, restored roads all show correctly.</summary>
    private void BuildRoadSet()
    {
        roadSet = new HashSet<Vector2Int>();
        if (gridManager == null || !gridManager.isInitialized)
            return;
        int w = gridManager.width;
        int h = gridManager.height;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Cell c = gridManager.GetCell(x, y);
                if (c != null && c.zoneType == Zone.ZoneType.Road)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (interstateSet == null || !interstateSet.Contains(pos))
                        roadSet.Add(pos);
                }
            }
        }
    }

    private Color GetCellColor(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        bool isInterstate = interstateSet != null && interstateSet.Contains(pos);
        bool isRoad = roadSet != null && roadSet.Contains(pos);

        // 1. Water (always visible) — WaterMap first; ZoneType.Water covers sea-level terrain not yet in map.
        if (waterManager != null && waterManager.IsWaterAt(x, y))
            return ColorWater;

        // 2. Streets (if active) — roads and interstate
        if ((activeLayers & MiniMapLayer.Streets) != 0)
        {
            if (isInterstate) return ColorInterstate;
            if (isRoad) return ColorRoad;
        }

        Cell cell = gridManager.GetCell(x, y);
        if (cell == null)
            return ColorGrass;

        if (cell.GetZoneType() == Zone.ZoneType.Water)
            return ColorWater;

        Zone.ZoneType zt = cell.GetZoneType();

        // 3. Zones (if active) — buildings and zoning
        if ((activeLayers & MiniMapLayer.Zones) != 0)
        {
            if (IsBuilding(zt)) return ColorBuilding;
            if (IsResidentialZoning(zt)) return ColorResidential;
            if (IsCommercialZoning(zt)) return ColorCommercial;
            if (IsIndustrialZoning(zt)) return ColorIndustrial;
        }

        // 4. Forests (if active)
        if ((activeLayers & MiniMapLayer.Forests) != 0 && cell.HasForest())
            return GetForestColor(cell.GetForestType());

        // 5. Desirability (if active) — roads keep normal colors per user requirement
        if ((activeLayers & MiniMapLayer.Desirability) != 0)
        {
            if (isInterstate) return ColorInterstate;
            if (isRoad) return ColorRoad;
            return GetDesirabilityColor(cell.desirability);
        }

        // 6. Fallback
        return ColorGrass;
    }

    /// <summary>Compute min/max desirability for land cells (excludes water, roads) → dynamic color scaling.</summary>
    private void ComputeDesirabilityRange(int w, int h)
    {
        desirabilityMin = float.MaxValue;
        desirabilityMax = float.MinValue;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                if (waterManager != null && waterManager.IsWaterAt(x, y))
                    continue;
                Vector2Int pos = new Vector2Int(x, y);
                if (interstateSet != null && interstateSet.Contains(pos))
                    continue;
                if (roadSet != null && roadSet.Contains(pos))
                    continue;
                Cell cell = gridManager.GetCell(x, y);
                if (cell == null) continue;
                if (cell.GetZoneType() == Zone.ZoneType.Water)
                    continue;
                float d = cell.desirability;
                if (d < desirabilityMin) desirabilityMin = d;
                if (d > desirabilityMax) desirabilityMax = d;
            }
        }
        if (desirabilityMin > desirabilityMax)
        {
            desirabilityMin = 0f;
            desirabilityMax = 20f;
        }
    }

    private Color GetDesirabilityColor(float desirability)
    {
        float range = desirabilityMax - desirabilityMin;
        float t = range > 0.001f
            ? Mathf.Clamp01((desirability - desirabilityMin) / range)
            : 0f;
        return Color.Lerp(ColorDesirabilityLow, ColorDesirabilityHigh, t);
    }

    private static Color GetForestColor(Forest.ForestType forestType)
    {
        switch (forestType)
        {
            case Forest.ForestType.Sparse: return ColorForestSparse;
            case Forest.ForestType.Medium: return ColorForestMedium;
            case Forest.ForestType.Dense: return ColorForestDense;
            default: return ColorForestMedium;
        }
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
