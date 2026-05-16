using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Territory.Core;
using Territory.Terrain;
using Territory.Roads;
using Territory.Simulation;
using Territory.SceneManagement;
using Territory.Services;
using Domains.UI.Services;

namespace Territory.UI
{
/// <summary>Mini-map layer flags. Multiple layers active simultaneously.</summary>
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

/// <summary>Scale-aware minimap render mode. City = city grid; Region = region cells. Stage 5.0 (TECH-37610).</summary>
public enum MinimapMode
{
    City,
    Region,
}

/// <summary>
/// Hub: render procedural mini-map + click/drag-to-navigate. Stage 5.6 THIN.
/// Color/classifier logic delegated to MiniMapService (_svc).
/// </summary>
public class MiniMapController : MonoBehaviour, IPointerClickHandler, IDragHandler
{
    public GridManager gridManager;
    public WaterManager waterManager;
    public InterstateManager interstateManager;
    public CameraController cameraController;
    public AutoZoningManager autoZoningManager;
    public UrbanCentroidService urbanCentroidService;

    [Header("UI References")]
    public RawImage mapImage;
    public RectTransform viewportRect;
    public GameObject miniMapPanel;

    private Texture2D _mapTexture;
    /// <summary>Effort 6 — read-only access to the built mini-map texture for UI Toolkit hosts.</summary>
    public Texture2D MapTexture => _mapTexture;
    [SerializeField] private MiniMapLayer activeLayers = MiniMapLayer.Streets | MiniMapLayer.Zones;
    private readonly MiniMapService _svc = new MiniMapService();

    // Stage 5.0 — scale-aware mode + dual-cache (TECH-37610).
    private MinimapMode _currentMode = MinimapMode.City;
    private Texture2D _cityCache;
    private Texture2D _regionCache;
    private bool _cityNeedsRegen;
    private bool _regionNeedsRegen;

    /// <summary>Current minimap mode (City or Region).</summary>
    public MinimapMode CurrentMode => _currentMode;

    /// <summary>Switch minimap mode. Displays cached texture during regen to prevent blank-flash.</summary>
    public void SetMode(MinimapMode mode)
    {
        if (_currentMode == mode) return;
        _currentMode = mode;

        // Display cached texture immediately while async regen queued.
        Texture2D cached = mode == MinimapMode.City ? _cityCache : _regionCache;
        if (cached != null && mapImage != null)
            mapImage.texture = cached;

        RebuildTexture();
    }

    /// <summary>Invalidate city cache (e.g. on PlayerCityDataUpdated).</summary>
    public void InvalidateCityCache()
    {
        _cityNeedsRegen = true;
        if (_currentMode == MinimapMode.City)
            RebuildTexture();
    }

    /// <summary>Invalidate region cache (e.g. on cell stream-in).</summary>
    public void InvalidateRegionCache()
    {
        _regionNeedsRegen = true;
        if (_currentMode == MinimapMode.Region)
            RebuildTexture();
    }

    // ── Public API ────────────────────────────────────────────────────────────────
    public bool IsVisible => (miniMapPanel != null ? miniMapPanel : gameObject).activeSelf;

    public void SetVisible(bool visible)
    {
        (miniMapPanel != null ? miniMapPanel : gameObject).SetActive(visible);
        if (visible) RebuildTexture();
    }

    public void ToggleLayer(MiniMapLayer layer)        { activeLayers ^= layer; RebuildTexture(); }
    public bool IsLayerActive(MiniMapLayer layer)      => (activeLayers & layer) != 0;
    public MiniMapLayer GetActiveLayers()              => activeLayers;
    public void SetActiveLayers(MiniMapLayer layers)   { activeLayers = layers; RebuildTexture(); }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────────
    void Awake()
    {
        // DEC-A29 hub-preservation: DontDestroyOnLoad — MinimapController persists across scene loads in CoreScene.
        DontDestroyOnLoad(this.gameObject);

        if (gridManager == null)         gridManager         = FindObjectOfType<GridManager>();
        if (waterManager == null)        waterManager        = FindObjectOfType<WaterManager>();
        if (interstateManager == null)   interstateManager   = FindObjectOfType<InterstateManager>();
        if (cameraController == null)    cameraController    = FindObjectOfType<CameraController>();
        if (autoZoningManager == null)   autoZoningManager   = FindObjectOfType<AutoZoningManager>();
        if (urbanCentroidService == null) urbanCentroidService = FindObjectOfType<UrbanCentroidService>();

        GameObject panelGo = miniMapPanel != null ? miniMapPanel : gameObject;
        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.anchorMin = new Vector2(1f, 0f); panelRt.anchorMax = new Vector2(1f, 0f);
            panelRt.pivot = new Vector2(1f, 0f); panelRt.sizeDelta = new Vector2(360f, 360f);
            panelRt.anchoredPosition = new Vector2(-24f, 24f); panelRt.localScale = Vector3.one;
        }
    }

    private IsoSceneContextService _contextService;

    void Start()
    {
        if (gridManager != null && gridManager.onGridRestored != null)
            gridManager.onGridRestored += OnGridRestored;

        // Stage 5.0 — subscribe to context changes for scale-aware mode switch.
        _contextService = FindObjectOfType<IsoSceneContextService>();
        if (_contextService != null)
            _contextService.ContextChanged += OnSceneContextChanged;
    }

    void OnDestroy()
    {
        if (gridManager != null && gridManager.onGridRestored != null) gridManager.onGridRestored -= OnGridRestored;
        if (_contextService != null) _contextService.ContextChanged -= OnSceneContextChanged;
        if (_mapTexture != null) { Destroy(_mapTexture); _mapTexture = null; }
        if (_cityCache  != null) { Destroy(_cityCache);  _cityCache  = null; }
        if (_regionCache != null) { Destroy(_regionCache); _regionCache = null; }
    }

    private void OnSceneContextChanged(IsoSceneContextService.SceneContext ctx)
    {
        if (ctx == IsoSceneContextService.SceneContext.City)
            SetMode(MinimapMode.City);
        else if (ctx == IsoSceneContextService.SceneContext.Region)
            SetMode(MinimapMode.Region);
    }

    void Update()
    {
        if (gridManager == null || !gridManager.isInitialized) return;
        if (!(miniMapPanel != null ? miniMapPanel : gameObject).activeSelf) return;
        UpdateViewportRect();
    }

    // ── Texture Rebuild ───────────────────────────────────────────────────────────
    private void OnGridRestored() => RebuildTexture();

    public void RebuildTexture()
    {
        // iter-41 — don't block when mapImage RawImage isn't wired (Effort 6 reads
        // the texture via MapTexture accessor for UI Toolkit binding).
        // Stage 5.0 — city mode requires gridManager; region mode can build without it (flat).
        if (_currentMode == MinimapMode.City && (gridManager == null || !gridManager.isInitialized)) return;
        if (_currentMode == MinimapMode.Region)
        {
            RebuildRegionTexture();
            return;
        }

        int w = gridManager.width, h = gridManager.height;

        if (_mapTexture == null || _mapTexture.width != w || _mapTexture.height != h)
        {
            if (_mapTexture != null) Destroy(_mapTexture);
            _mapTexture = new Texture2D(w, h) { filterMode = FilterMode.Point };
        }

        _svc.BuildInterstateSet(interstateManager);
        _svc.BuildRoadSet(gridManager);
        if ((activeLayers & MiniMapLayer.Desirability) != 0)
            _svc.ComputeDesirabilityRange(w, h, gridManager, waterManager);

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                _mapTexture.SetPixel(x, y, _svc.GetCellColor(x, y, activeLayers, gridManager, waterManager));

        if ((activeLayers & MiniMapLayer.Centroid) != 0 &&
            (urbanCentroidService != null || (autoZoningManager != null && autoZoningManager.GetUrbanMetrics() != null)))
        {
            if (urbanCentroidService != null) urbanCentroidService.RecalculateFromGrid();
            UrbanMetrics um = urbanCentroidService != null ? urbanCentroidService.GetUrbanMetrics() : autoZoningManager.GetUrbanMetrics();
            if (um != null)
            {
                Vector2 centroid = urbanCentroidService != null ? urbanCentroidService.GetCentroid() : um.GetCentroid();
                float[] boundaries = urbanCentroidService != null ? urbanCentroidService.GetRingBoundaryDistances() : um.GetRingBoundaryDistances();
                _svc.PaintCentroidOverlay(_mapTexture, w, h, centroid, boundaries);
            }
        }

        _mapTexture.Apply();

        // Cache city texture for mode-switch blank-flash prevention.
        if (_cityCache != null && (_cityCache.width != w || _cityCache.height != h))
        {
            Destroy(_cityCache);
            _cityCache = null;
        }
        if (_cityCache == null)
            _cityCache = new Texture2D(w, h) { filterMode = FilterMode.Point };
        Graphics.CopyTexture(_mapTexture, _cityCache);
        _cityNeedsRegen = false;

        if (mapImage != null) mapImage.texture = _mapTexture;
    }

    private void RebuildRegionTexture()
    {
        const int regionSize = 64;
        if (_mapTexture == null || _mapTexture.width != regionSize || _mapTexture.height != regionSize)
        {
            if (_mapTexture != null) Destroy(_mapTexture);
            _mapTexture = new Texture2D(regionSize, regionSize) { filterMode = FilterMode.Point };
        }

        // Prototype: flat green region texture. Real data binding post-streaming pipeline (Stage 6).
        var greenish = new Color(0.28f, 0.52f, 0.28f, 1f);
        for (int x = 0; x < regionSize; x++)
            for (int y = 0; y < regionSize; y++)
                _mapTexture.SetPixel(x, y, greenish);
        _mapTexture.Apply();

        // Cache region texture.
        if (_regionCache == null)
            _regionCache = new Texture2D(regionSize, regionSize) { filterMode = FilterMode.Point };
        Graphics.CopyTexture(_mapTexture, _regionCache);
        _regionNeedsRegen = false;

        if (mapImage != null) mapImage.texture = _mapTexture;
    }

    // ── Viewport + Navigation ─────────────────────────────────────────────────────
    private void UpdateViewportRect()
    {
        if (viewportRect == null || gridManager == null || !gridManager.isInitialized) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        MiniMapService.ComputeViewportAnchors(cam, gridManager, out Vector2 aMin, out Vector2 aMax);
        viewportRect.anchorMin = aMin; viewportRect.anchorMax = aMax;
        viewportRect.offsetMin = Vector2.zero; viewportRect.offsetMax = Vector2.zero;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gridManager == null || cameraController == null || mapImage == null) return;
        RectTransform rect = mapImage.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 lp)) return;
        Vector2Int g = MiniMapService.LocalPointToGrid(lp, rect.rect, gridManager);
        Vector2 wp = gridManager.GetWorldPosition(g.x, g.y);
        cameraController.MoveCameraToMapCenter(new Vector3(wp.x, wp.y, 0));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (gridManager == null || cameraController == null || mapImage == null) return;
        Vector2 sz = mapImage.rectTransform.rect.size;
        if (sz.x <= 0f || sz.y <= 0f) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        cameraController.PanCameraTo(MiniMapService.DragDeltaToTargetGrid(eventData.delta.x / sz.x, eventData.delta.y / sz.y, cam, gridManager));
    }

    public void ForwardLayerToggle(MiniMapLayer layer) => ToggleLayer(layer);

    public void EnforceRenderSize()
    {
        RectTransform rt = mapImage?.rectTransform;
        if (rt != null) rt.sizeDelta = new Vector2(360f, 324f);
    }
}

}
