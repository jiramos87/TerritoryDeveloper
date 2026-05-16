using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Territory.Core;
using Territory.Terrain;
using Territory.Roads;
using Territory.Simulation;
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

    void Start()   { if (gridManager != null && gridManager.onGridRestored != null) gridManager.onGridRestored += OnGridRestored; }

    void OnDestroy()
    {
        if (gridManager != null && gridManager.onGridRestored != null) gridManager.onGridRestored -= OnGridRestored;
        if (_mapTexture != null) { Destroy(_mapTexture); _mapTexture = null; }
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
        if (gridManager == null || !gridManager.isInitialized) return;
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
