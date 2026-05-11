using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Territory.Core;
using Territory.Zones;
using Territory.Buildings;
using Domains.Registry;
using Domains.Cursor;
using Domains.Cursor.Services;

namespace Territory.UI
{
/// <summary>THIN hub — delegates every public concern to CursorService POCO. Path/class/namespace/[SerializeField] unchanged.</summary>
public class CursorManager : MonoBehaviour, ICursor, ICursorHub
{
    // ── [SerializeField] set UNCHANGED (locked) ──────────────────────────────────
    public Texture2D cursorTexture;
    public Texture2D bulldozerTexture;
    public Texture2D detailsTexture;
    public Vector2 hotSpot;
    public GridManager gridManager;
    [SerializeField] private PlacementValidator placementValidator;

    // ── Events (ICursor) ──────────────────────────────────────────────────────────
    public event Action<PlacementResult> PlacementResultChanged;
    public event Action<PlacementFailReason> PlacementReasonChanged;

    // ── Registry + service ────────────────────────────────────────────────────────
    private ServiceRegistry _registry;
    private CursorService _service;
    private Camera _cachedMainCamera;
    private UIManager _cachedUIManager;

    // ── ICursorHub — textures ──────────────────────────────────────────────────────
    Texture2D ICursorHub.CursorTexture => cursorTexture;
    Texture2D ICursorHub.BulldozerTexture => bulldozerTexture;
    Texture2D ICursorHub.DetailsTexture => detailsTexture;
    bool ICursorHub.IsPointerOverUI() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    void ICursorHub.FirePlacementResultChanged(PlacementResult r) { PlacementResultChanged?.Invoke(r); }
    void ICursorHub.FirePlacementReasonChanged(PlacementFailReason r) { PlacementReasonChanged?.Invoke(r); }

    // ── ICursorHub — grid/world passthrough ───────────────────────────────────────
    Vector2 ICursorHub.ScreenToWorldOnGrid(Camera cam, Vector3 screenPos) =>
        GridManager.ScreenPointToWorldOnGridPlane(cam, screenPos);
    CityCell ICursorHub.GetMouseCell(Vector2 worldPos) =>
        gridManager != null ? gridManager.GetMouseGridCell(worldPos) : null;
    CityCell ICursorHub.GetCell(int x, int y) =>
        gridManager != null ? gridManager.GetCell(x, y) : null;
    Vector2 ICursorHub.GetBuildingPlacementPos(Vector2 gridPos, int buildingSize) =>
        gridManager != null ? gridManager.GetBuildingPlacementWorldPosition(gridPos, buildingSize) : Vector2.zero;
    float ICursorHub.TileHeight => gridManager != null ? gridManager.tileHeight : 0.5f;

    // ── ICursorHub — road ghost ───────────────────────────────────────────────────
    bool ICursorHub.HasRoadManager => gridManager != null && gridManager.roadManager != null;
    void ICursorHub.GetRoadGhostPreview(Vector2 gridPos, out GameObject prefab, out Vector2 worldPos, out int sortingOrder)
    {
        if (gridManager != null && gridManager.roadManager != null)
            gridManager.roadManager.GetRoadGhostPreviewForCell(gridPos, out prefab, out worldPos, out sortingOrder);
        else
        {
            prefab = null;
            worldPos = Vector2.zero;
            sortingOrder = 0;
        }
    }

    // ── ICursorHub — selection probes ─────────────────────────────────────────────
    Zone.ZoneType ICursorHub.GetSelectedZoneType() =>
        _cachedUIManager != null ? _cachedUIManager.GetSelectedZoneType() : Zone.ZoneType.None;
    int ICursorHub.GetSelectedBuildingSize() =>
        _cachedUIManager?.GetSelectedBuilding()?.BuildingSize ?? 1;
    bool ICursorHub.IsSelectedBuildingWaterPlant() =>
        _cachedUIManager?.GetSelectedBuilding() is WaterPlant;

    // ── ICursorHub — placement ────────────────────────────────────────────────────
    PlacementResult ICursorHub.CanPlace(int assetId, int cellX, int cellY, int rotation, Zone.ZoneType zoneType) =>
        placementValidator != null
            ? placementValidator.CanPlace(assetId, cellX, cellY, rotation, zoneType)
            : PlacementResult.Allowed();

    void Awake()
    {
        _registry = FindObjectOfType<ServiceRegistry>();
        _service = new CursorService(this);
        _registry?.Register<ICursor>(this);
    }

    void Start()
    {
        hotSpot = Vector2.zero;
        _cachedMainCamera = Camera.main;
        _cachedUIManager = FindObjectOfType<UIManager>();
        if (placementValidator == null)
            placementValidator = FindObjectOfType<PlacementValidator>();
        PlacementResultChanged += _service.ApplyPreviewTint;
        PlacementReasonChanged += OnPlacementReasonForwardToTooltip;
        UnityEngine.Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
        // Wire grid dependency post-Awake (never resolve in Awake)
        _service.WireDependencies(_registry?.Resolve<Domains.Grid.IGrid>());
    }

    // ── Public delegates (ICursor) ────────────────────────────────────────────────
    public void SetBullDozerCursor() => _service.SetBullDozerCursor();
    public void SetDefaultCursor() => _service.SetDefaultCursor();
    public void SetDetailsCursor() => _service.SetDetailsCursor();
    public void ShowBuildingPreview(GameObject buildingPrefab, int buildingSize = 1) => _service.ShowBuildingPreview(buildingPrefab, buildingSize);
    public void RemovePreview() => _service.RemovePreview();

    void Update()
    {
        if (_cachedMainCamera == null) _cachedMainCamera = Camera.main;
        _service.UpdatePreview(_cachedMainCamera);
    }

    private void OnPlacementReasonForwardToTooltip(PlacementFailReason reason)
    {
        if (_cachedUIManager == null) return;
        _cachedUIManager.ShowPlacementReasonTooltip(reason);
    }

    private void OnDestroy()
    {
        PlacementResultChanged = null;
        PlacementReasonChanged = null;
    }
}
}
