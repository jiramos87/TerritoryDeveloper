// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
// GridManager.Impl.cs — body methods for partial GridManager hub (Stage 3.0 atomization).
// Invariant #5 carve-out: cellArray accessed directly here for save/restore performance paths.
// Invariant #1: HeightMap≡Cell.height preserved verbatim in all write paths.
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Territory.Zones;
using Territory.UI;
using Territory.Economy;
using Territory.Terrain;
using Territory.Forests;
using Territory.Buildings;
using Territory.Roads;
using Territory.Timing;
using Territory.Utilities.Compute;
using Territory.Audio;
using Domains.Grid.Services;

namespace Territory.Core
{
public partial class GridManager
{
    #region Initialization
    /// <summary>Bootstrap grid: resolve deps, create cell/chunk arrays, generate terrain, center camera.</summary>
    public void InitializeGrid()
    {
        var depResult = Domains.Grid.Services.GridInitDependencyBinder.Validate(this);
        if (depResult.missing_count > 0)
        {
            foreach (var field in depResult.missing)
                Debug.LogError($"[GridManager] Inspector ref missing: {field}. Wire in CityScene inspector.");
            return;
        }

        halfWidth = tileWidth / 2f;
        halfHeight = tileHeight / 2f;

        if (width <= 0) width = 64;
        if (height <= 0) height = 64;

        if (zoneManager == null) zoneManager = FindObjectOfType<ZoneManager>();
        if (zoneManager != null) zoneManager.InitializeZonePrefabs();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        if (cityStats == null) cityStats = FindObjectOfType<CityStats>();
        if (cursorManager == null) cursorManager = FindObjectOfType<CursorManager>();
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (waterManager == null) waterManager = FindObjectOfType<WaterManager>();
        if (cameraController == null) cameraController = FindObjectOfType<CameraController>();
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
        if (roadManager != null) roadManager.Initialize();
        if (demandManager == null) demandManager = FindObjectOfType<DemandManager>();
        if (GameNotificationManager == null) GameNotificationManager = FindObjectOfType<GameNotificationManager>();
        if (forestManager == null) forestManager = FindObjectOfType<ForestManager>();
        if (interstateManager == null) interstateManager = FindObjectOfType<InterstateManager>();

        // sortingService created before CreateGrid() — SetTileSortingOrder used during grid creation
        sortingService = new GridSortingOrderService(this);
        CreateGrid();
        terrainManager.InitializeHeightMap();
        isInitialized = true;
        Vector3 centerWorldPosition = GetWorldPosition(width / 2, height / 2);
        cameraController.MoveCameraToMapCenter(centerWorldPosition);

        pathfinder = new GridPathfinder(this);
        placementService = new BuildingPlacementService(this, sortingService);
        chunkCulling = new ChunkCullingSystem(this, chunkSize, cachedCamera);
        cellAccessService = new CellAccessService(this);
        _gridQueryService = new GridQueryService(this);
        chunkCulling.chunkObjects = chunkObjects;
        chunkCulling.chunkActiveState = chunkActiveState;
        roadCache = new RoadCacheService(this);
    }

    void CreateGrid(bool createBaseTiles = true)
    {
        if (!zoneManager) zoneManager = FindObjectOfType<ZoneManager>();

        gridArray = new GameObject[width, height];
        cellArray = new CityCell[width, height];
        chunksX = Mathf.CeilToInt((float)width / chunkSize);
        chunksY = Mathf.CeilToInt((float)height / chunkSize);
        chunkObjects = new GameObject[chunksX, chunksY];
        chunkActiveState = new bool[chunksX, chunksY];

        for (int cx = 0; cx < chunksX; cx++)
        {
            for (int cy = 0; cy < chunksY; cy++)
            {
                GameObject chunk = new GameObject($"Chunk_{cx}_{cy}");
                chunk.transform.SetParent(transform);
                chunkObjects[cx, cy] = chunk;
                chunkActiveState[cx, cy] = true;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject gridCell = new GameObject($"Cell_{x}_{y}");
                float posX = (x - y) * (tileWidth / 2);
                float posY = (x + y) * (tileHeight / 2);
                gridCell.transform.position = new Vector3(posX, posY, 0);
                gridCell.transform.SetParent(chunkObjects[x / chunkSize, y / chunkSize].transform);

                CellData cellData = new CellData(x, y, 1);
                GameObject tilePrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, 1);
                if (tilePrefab == null && zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0)
                    tilePrefab = zoneManager.grassPrefabs[0];
                if (tilePrefab != null) { cellData.prefab = tilePrefab; cellData.prefabName = tilePrefab.name; }

                CityCell cellComponent = gridCell.AddComponent<CityCell>();
                cellComponent.SetCellData(cellData);
                gridArray[x, y] = gridCell;
                cellArray[x, y] = cellComponent;

                if (createBaseTiles)
                {
                    GameObject zoneTile = Instantiate(tilePrefab, gridCell.transform.position, Quaternion.identity);
                    SetTileSortingOrder(zoneTile, Zone.ZoneType.Grass);
                    Zone zoneComponent = zoneTile.GetComponent<Zone>();
                    if (zoneComponent == null) { zoneComponent = zoneTile.AddComponent<Zone>(); zoneComponent.zoneType = Zone.ZoneType.Grass; }
                }
            }
        }
    }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (cachedCamera == null) cachedCamera = Camera.main;
    }

    void Update()
    {
        try
        {
            if (!isInitialized) return;
            if (gridArray == null || gridArray.Length == 0) return;
            if (EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 worldPoint = ScreenPointToWorldOnGridPlane(cachedCamera, Input.mousePosition);
            CityCell mouseGridCell = GetMouseGridCell(worldPoint);
            if (mouseGridCell == null) return;
            mouseGridPosition = new Vector2(mouseGridCell.x, mouseGridCell.y);
            mouseGridHeight = mouseGridCell.GetCellInstanceHeight();
            mouseGridSortingOrder = mouseGridCell.sortingOrder;

            if (mouseGridPosition.x == -1 && mouseGridPosition.y == -1) return;
            if (!IsValidGridPosition(mouseGridPosition)) return;

            if (Input.GetMouseButtonDown(0))
            {
                selectedPoint = mouseGridPosition;
                BlipEngine.Play(BlipId.WorldCellSelected);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                pendingRightClickGridPosition = mouseGridPosition;
            }
            else if (Input.GetMouseButtonUp(1) && cameraController != null && !cameraController.WasLastRightClickAPan && IsValidGridPosition(pendingRightClickGridPosition))
            {
                selectedPoint = pendingRightClickGridPosition;
                BlipEngine.Play(BlipId.WorldCellSelected);
            }

            if (uiManager.isBulldozeMode()) HandleBulldozerMode(mouseGridPosition);
            if (uiManager.IsDetailsMode() || Input.GetKey(KeyCode.LeftShift)) HandleShowTileDetails(mouseGridPosition);

            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                if (Input.GetMouseButtonDown(0))
                    _worldSelectAction?.Invoke(new Vector2Int((int)mouseGridPosition.x, (int)mouseGridPosition.y));
            }

            HandleRaycast(mouseGridPosition);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Update error: " + ex);
        }
    }

    void LateUpdate()
    {
        if (!isInitialized || chunkObjects == null) return;
        if (skipChunkCullingFramesRemaining > 0) { skipChunkCullingFramesRemaining--; return; }
        chunkCulling?.UpdateVisibility();
    }
    #endregion

    #region Parent-scale identity
    /// <summary>Set parent region + country ids. One-shot per lifecycle.</summary>
    public void HydrateParentIds(string regionId, string countryId)
    {
        if (string.IsNullOrEmpty(regionId) || string.IsNullOrEmpty(countryId)) { Debug.LogError("[GridManager] HydrateParentIds: regionId or countryId is null/empty — skipping hydration."); return; }
        if (_parentIdsHydrated) { Debug.LogError("[GridManager] HydrateParentIds: already hydrated — duplicate call ignored."); return; }
        ParentRegionId = regionId;
        ParentCountryId = countryId;
        _parentIdsHydrated = true;
    }

    /// <summary>Populate neighbor-city stub cache. One-shot per lifecycle.</summary>
    public void HydrateNeighborStubs(System.Collections.Generic.IEnumerable<NeighborCityStub> stubs)
    {
        if (stubs == null) { Debug.LogError("[GridManager] HydrateNeighborStubs: stubs argument is null — skipping hydration."); return; }
        if (_neighborStubsHydrated) { Debug.LogError("[GridManager] HydrateNeighborStubs: already hydrated — duplicate call ignored."); return; }
        _neighborStubs = new System.Collections.Generic.List<NeighborCityStub>(stubs).AsReadOnly();
        _neighborStubsHydrated = true;
    }

    /// <summary>First NeighborCityStub whose borderSide matches side, or null.</summary>
    public NeighborCityStub? GetNeighborStub(BorderSide side)
    {
        foreach (var stub in _neighborStubs) { if (stub.borderSide == side) return stub; }
        return null;
    }
    #endregion

    #region CityCell Queries
    bool IsZoneTypeBuilding(Zone.ZoneType zoneType)
        => cellAccessService != null
            ? cellAccessService.IsZoneTypeBuilding(zoneType)
            : (zoneType == Zone.ZoneType.Building ||
               zoneType == Zone.ZoneType.ResidentialLightBuilding || zoneType == Zone.ZoneType.ResidentialMediumBuilding || zoneType == Zone.ZoneType.ResidentialHeavyBuilding ||
               zoneType == Zone.ZoneType.CommercialLightBuilding || zoneType == Zone.ZoneType.CommercialMediumBuilding || zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
               zoneType == Zone.ZoneType.IndustrialLightBuilding || zoneType == Zone.ZoneType.IndustrialMediumBuilding || zoneType == Zone.ZoneType.IndustrialHeavyBuilding);

    /// <summary>Footprint offset for building (even = 0,0; odd = buildingSize/2).</summary>
    public void GetBuildingFootprintOffset(int buildingSize, out int offsetX, out int offsetY)
    {
        if (cellAccessService != null) { cellAccessService.GetBuildingFootprintOffset(buildingSize, out offsetX, out offsetY); return; }
        offsetX = buildingSize % 2 == 0 ? 0 : buildingSize / 2;
        offsetY = buildingSize % 2 == 0 ? 0 : buildingSize / 2;
    }

    /// <summary>Pivot cell of multi-cell building, or grid cell for single-cell.</summary>
    public GameObject GetBuildingPivotCell(CityCell cell)
    {
        if (cell == null) return null;
        if (cell.occupiedBuilding == null || cell.buildingSize <= 1) return GetGridCell(new Vector2(cell.x, cell.y));
        int size = cell.buildingSize;
        int cx = (int)cell.x;
        int cy = (int)cell.y;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int px = cx - i; int py = cy - j;
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    CityCell pivotCandidate = cellArray[px, py];
                    if (pivotCandidate != null && pivotCandidate.isPivot) return GetGridCell(new Vector2(px, py));
                }
            }
        }
        return GetGridCell(new Vector2(cx, cy));
    }

    /// <summary>Serialize every cell → CellData list for saving.</summary>
    public List<CellData> GetGridData()
    {
        var gridData = new List<CellData>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                CityCell cell = cellArray[x, y];
                if (cell != null) gridData.Add(cell.GetCellData());
            }
        return gridData;
    }

    /// <summary>World-space pos where building placed (preview + actual).</summary>
    public Vector2 GetBuildingPlacementWorldPosition(Vector2 gridPos, int buildingSize)
    {
        CityCell pivotCell = cellArray[(int)gridPos.x, (int)gridPos.y];
        if (buildingSize <= 1) return pivotCell.transformPosition;
        GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);
        Vector2 sum = Vector2.zero;
        int count = 0;
        for (int x = 0; x < buildingSize; x++)
            for (int y = 0; y < buildingSize; y++)
            {
                int gridX = (int)gridPos.x + x - offsetX;
                int gridY = (int)gridPos.y + y - offsetY;
                if (gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
                {
                    CityCell c = cellArray[gridX, gridY];
                    sum += GetCellWorldPosition(c);
                    count++;
                }
            }
        return count > 0 ? sum / count : pivotCell.transformPosition;
    }

    /// <summary>User-facing demand feedback string for zone type.</summary>
    public string GetDemandFeedback(Zone.ZoneType zoneType)
    {
        if (demandManager == null) return "";
        float demandLevel = demandManager.GetDemandLevel(zoneType);
        bool canGrow = demandManager.CanZoneTypeGrow(zoneType);
        Zone.ZoneType buildingType = zoneManager.GetBuildingZoneType(zoneType);
        bool isResidential = zoneManager.IsResidentialBuilding(buildingType);
        bool hasJobsAvailable = !isResidential || demandManager.CanPlaceResidentialBuilding();
        bool needsResidential = zoneManager.IsCommercialOrIndustrialBuilding(buildingType);
        bool hasResidentialSupport = !needsResidential || demandManager.CanPlaceCommercialOrIndustrialBuilding(buildingType);
        if (canGrow && hasJobsAvailable && hasResidentialSupport) return $"✓ Demand: {demandLevel:F0}%";
        if (!canGrow) return $"✗ Low Demand: {demandLevel:F0}%";
        if (!hasJobsAvailable) return $"✗ No Jobs Available (Demand: {demandLevel:F0}%)";
        if (!hasResidentialSupport) return $"✗ Need Residents First (Demand: {demandLevel:F0}%)";
        return "";
    }

    /// <summary>Set terrain height of cell at grid pos.</summary>
    public void SetCellHeight(Vector2 gridPos, int height, bool skipWaterMembershipRefresh = false)
    {
        CityCell cell = cellArray[(int)gridPos.x, (int)gridPos.y];
        cell.SetCellInstanceHeight(height);
        if (skipWaterMembershipRefresh) return;
        if (waterManager == null) waterManager = FindObjectOfType<WaterManager>();
        if (waterManager != null) waterManager.OnLandCellHeightCommitted((int)gridPos.x, (int)gridPos.y);
    }
    #endregion

    #region Coordinate Conversion
    /// <summary>Screen pos → world X/Y on Z=0 plane.</summary>
    public static Vector2 ScreenPointToWorldOnGridPlane(Camera cam, Vector3 screenPosition)
    {
        if (cam == null) return Vector2.zero;
        Ray ray = cam.ScreenPointToRay(screenPosition);
        const float planeZ = 0f;
        float dz = ray.direction.z;
        if (Mathf.Abs(dz) > 1e-5f)
        {
            float t = (planeZ - ray.origin.z) / dz;
            if (t > -1e-3f) { Vector3 hit = ray.origin + ray.direction * t; return new Vector2(hit.x, hit.y); }
        }
        float depth = Mathf.Abs(cam.transform.position.z - planeZ);
        Vector3 p = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        return new Vector2(p.x, p.y);
    }

    /// <summary>World-space point → isometric grid coords (ignores height).</summary>
    public Vector2 GetGridPosition(Vector2 worldPoint)
    {
        Vector2Int g = IsometricGridMath.WorldToGridPlanar(worldPoint, tileWidth, tileHeight);
        return new Vector2(g.x, g.y);
    }

    /// <summary>Grid coords + height → world-space pos.</summary>
    public Vector2 GetWorldPositionVector(int gridX, int gridY, int height)
        => IsometricGridMath.GridToWorldPlanar(gridX, gridY, tileWidth, tileHeight, height);

    private Vector2 GetWorldPositionVectorDown(int gridX, int gridY, int height)
    {
        float heightOffset = (height - 1) * (tileHeight / 2);
        float posX = (gridX - gridY) * (tileWidth / 2);
        float posY = (gridX + gridY) * (tileHeight / 2) - heightOffset;
        return new Vector2(posX, posY);
    }

    /// <summary>World-space pos for cell at (gridX, gridY) accounting for terrain height.</summary>
    public Vector2 GetWorldPosition(int gridX, int gridY)
    {
        CityCell cell = cellArray[gridX, gridY];
        int h = cell.GetCellInstanceHeight();
        return GetWorldPositionVector(gridX, gridY, h);
    }

    /// <summary>World-space pos of cell accounting for terrain height.</summary>
    public Vector2 GetCellWorldPosition(CityCell cell)
    {
        int h = cell.GetCellInstanceHeight();
        return GetWorldPositionVector(cell.x, cell.y, h);
    }

    private int GetTerrainHeightForGridCell(int gridX, int gridY)
    {
        CityCell c = GetCell(gridX, gridY);
        return c != null ? c.GetCellInstanceHeight() : 1;
    }

    int GetMaxTerrainHeightInNeighborhood3x3(int centerX, int centerY)
    {
        int maxH = 1;
        int[] mx = { 0, 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] my = { 0, 0, 0, 1, -1, 1, -1, 1, -1 };
        for (int i = 0; i < 9; i++)
        {
            int x = centerX + mx[i]; int y = centerY + my[i];
            if (x < 0 || x >= width || y < 0 || y >= height) continue;
            maxH = Mathf.Max(maxH, GetTerrainHeightForGridCell(x, y));
        }
        return maxH;
    }

    private Vector2 RefineGridPositionForTerrainHeight(Vector2 worldPoint)
    {
        if (width <= 0 || height <= 0) return GetGridPosition(worldPoint);
        Vector2 grid = GetGridPosition(worldPoint);
        const int maxIterations = 4;
        for (int iter = 0; iter < maxIterations; iter++)
        {
            int gx = Mathf.Clamp(Mathf.RoundToInt(grid.x), 0, width - 1);
            int gy = Mathf.Clamp(Mathf.RoundToInt(grid.y), 0, height - 1);
            int h = GetMaxTerrainHeightInNeighborhood3x3(gx, gy);
            float heightOffset = (h - 1) * (tileHeight / 2f);
            Vector2 planePoint = new Vector2(worldPoint.x, worldPoint.y - heightOffset);
            Vector2 next = GetGridPosition(planePoint);
            if (Mathf.Approximately(next.x, grid.x) && Mathf.Approximately(next.y, grid.y)) return next;
            grid = next;
        }
        return grid;
    }

    private bool TryGetCellBaseTileScreenBounds(CityCell cell, Camera cam, out Rect screenRect)
    {
        screenRect = new Rect(0, 0, 0, 0);
        if (cell == null || cam == null) return false;
        SpriteRenderer tileRenderer = null;
        SpriteRenderer anyRenderer = null;
        for (int i = 0; i < cell.gameObject.transform.childCount; i++)
        {
            Transform child = cell.gameObject.transform.GetChild(i);
            Zone zone = child.GetComponent<Zone>();
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (anyRenderer == null) anyRenderer = sr;
                if (zone != null && (zone.zoneType == Zone.ZoneType.Grass || zone.zoneType == Zone.ZoneType.Road || zone.zoneCategory == Zone.ZoneCategory.Zoning))
                { tileRenderer = sr; break; }
            }
        }
        SpriteRenderer boundsSource = tileRenderer != null ? tileRenderer : anyRenderer;
        Bounds worldBounds;
        if (boundsSource != null) worldBounds = boundsSource.bounds;
        else { Vector2 center = cell.transformPosition; worldBounds = new Bounds(center, new Vector3(tileWidth, tileHeight, 0f)); }
        Vector3 worldMin = worldBounds.min; Vector3 worldMax = worldBounds.max;
        float zRef = worldBounds.center.z;
        Vector3 p0 = cam.WorldToScreenPoint(new Vector3(worldMin.x, worldMin.y, zRef));
        Vector3 p1 = cam.WorldToScreenPoint(new Vector3(worldMax.x, worldMin.y, zRef));
        Vector3 p2 = cam.WorldToScreenPoint(new Vector3(worldMin.x, worldMax.y, zRef));
        Vector3 p3 = cam.WorldToScreenPoint(new Vector3(worldMax.x, worldMax.y, zRef));
        float minX = Mathf.Min(p0.x, p1.x, p2.x, p3.x); float maxX = Mathf.Max(p0.x, p1.x, p2.x, p3.x);
        float minY = Mathf.Min(p0.y, p1.y, p2.y, p3.y); float maxY = Mathf.Max(p0.y, p1.y, p2.y, p3.y);
        const float insetFactor = 0.6f;
        float w = maxX - minX; float h = maxY - minY;
        float insetW = w * (1f - insetFactor) * 0.5f; float insetH = h * (1f - insetFactor) * 0.5f;
        minX += insetW; maxX -= insetW; minY += insetH; maxY -= insetH;
        screenRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    /// <summary>Resolve world-space point → best-matching cell via screen-space hit testing.</summary>
    public CityCell GetCellFromWorldPoint(Vector2 worldPoint, Vector2 gridPos)
    {
        Camera cam = cachedCamera;
        if (cam == null) return null;
        int gridX = Mathf.Clamp(Mathf.RoundToInt(gridPos.x), 0, Mathf.Max(0, width - 1));
        int gridY = Mathf.Clamp(Mathf.RoundToInt(gridPos.y), 0, Mathf.Max(0, height - 1));
        List<CityCell> candidates = new List<CityCell>();
        int[] dx = { 0, 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] dy = { 0, 0, 0, 1, -1, 1, -1, 1, -1 };
        for (int i = 0; i < 9; i++) { CityCell c = GetCell(gridX + dx[i], gridY + dy[i]); if (c != null) candidates.Add(c); }
        Vector2 mouseScreen = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<CityCell> rectHits = new List<CityCell>();
        foreach (CityCell cell in candidates) { if (!TryGetCellBaseTileScreenBounds(cell, cam, out Rect screenRect)) continue; if (screenRect.Contains(mouseScreen)) rectHits.Add(cell); }
        CityCell chosen = null;
        if (rectHits.Count == 1) { chosen = rectHits[0]; }
        else if (rectHits.Count > 1)
        {
            chosen = PickCellHitClosestOnScreen(mouseScreen, rectHits, cam);
            CityCell refineHit = null;
            for (int i = 0; i < rectHits.Count; i++) { CityCell c = rectHits[i]; if (c != null && c.x == gridX && c.y == gridY) { refineHit = c; break; } }
            if (refineHit != null && chosen != null && refineHit != chosen)
            {
                float dPick = GetCellScreenDistanceSqToMouse(chosen, mouseScreen, cam);
                float dRef = GetCellScreenDistanceSqToMouse(refineHit, mouseScreen, cam);
                const float tieEpsSq = 0.04f;
                if (Mathf.Abs(dPick - dRef) <= tieEpsSq) chosen = refineHit;
            }
        }
        else if (candidates.Count > 0) chosen = GetClosestCellByScreenDistance(mouseScreen, candidates, cam);
        return chosen;
    }

    float GetCellScreenDistanceSqToMouse(CityCell cell, Vector2 mouseScreen, Camera cam)
    {
        if (cell == null || cam == null) return float.MaxValue;
        Vector2 worldCenter = GetCellWorldPosition(cell);
        Vector3 sc = cam.WorldToScreenPoint(new Vector3(worldCenter.x, worldCenter.y, cell.transform.position.z));
        float dx = mouseScreen.x - sc.x; float dy = mouseScreen.y - sc.y;
        return dx * dx + dy * dy;
    }

    private CityCell PickCellHitClosestOnScreen(Vector2 mouseScreen, List<CityCell> hits, Camera cam)
    {
        if (hits == null || hits.Count == 0 || cam == null) return null;
        CityCell best = null; float bestDistSq = float.MaxValue; int bestOrder = int.MinValue;
        const float tiePixels = 4f; float tieEpsSq = tiePixels * tiePixels;
        foreach (CityCell cell in hits)
        {
            if (cell == null) continue;
            float distSq = GetCellScreenDistanceSqToMouse(cell, mouseScreen, cam);
            bool better = best == null;
            if (!better && distSq < bestDistSq - 0.001f) better = true;
            else if (!better && Mathf.Abs(distSq - bestDistSq) <= tieEpsSq && cell.sortingOrder > bestOrder) better = true;
            if (better) { best = cell; bestDistSq = distSq; bestOrder = cell.sortingOrder; }
        }
        return best;
    }

    private CityCell GetClosestCellByScreenDistance(Vector2 mouseScreen, List<CityCell> candidates, Camera cam)
    {
        if (cam == null || candidates == null || candidates.Count == 0) return null;
        CityCell closest = null; float minDistSq = float.MaxValue;
        foreach (CityCell cell in candidates)
        {
            Vector2 worldCenter = GetCellWorldPosition(cell);
            Vector3 screenCenter = cam.WorldToScreenPoint(new Vector3(worldCenter.x, worldCenter.y, 0f));
            float dx = mouseScreen.x - screenCenter.x; float dy = mouseScreen.y - screenCenter.y;
            float distSq = dx * dx + dy * dy;
            if (distSq < minDistSq) { minDistSq = distSq; closest = cell; }
        }
        return closest;
    }

    /// <summary>Mouse world point → grid coords. Corrects for terrain height.</summary>
    public Vector2 GetGridPositionWithHeight(Vector2 mouseWorldPoint)
    {
        Vector2 gridPos = RefineGridPositionForTerrainHeight(mouseWorldPoint);
        CityCell cell = GetCellFromWorldPoint(mouseWorldPoint, gridPos);
        if (cell == null) return gridPos;
        return new Vector2(cell.x, cell.y);
    }

    /// <summary>CityCell under mouse via screen-space hit testing.</summary>
    public CityCell GetMouseGridCell(Vector2 mouseWorldPoint)
    {
        Vector2 gridPos = RefineGridPositionForTerrainHeight(mouseWorldPoint);
        return GetCellFromWorldPoint(mouseWorldPoint, gridPos);
    }
    #endregion

    #region Input and Placement Handlers
    bool IsInWaterPlacementMode() => uiManager.GetSelectedZoneType() == Zone.ZoneType.Water;

    void HandleBulldozerMode(Vector2 gridPosition)
    {
        if (Input.GetMouseButtonDown(0)) HandleBulldozerClick(gridPosition);
    }

    void HandleBulldozerClick(Vector2 gridPosition)
    {
        GameObject cell = gridArray[(int)gridPosition.x, (int)gridPosition.y];
        CityCell cellComponent = cellArray[(int)gridPosition.x, (int)gridPosition.y];
        Zone.ZoneType zoneType = cellComponent.zoneType;
        if (!CanBulldoze(cellComponent)) return;
        HandleBulldozeTile(zoneType, cell);
    }

    void RestoreCellAttributes(CityCell cellComponent)
    {
        cellComponent.buildingType = null; cellComponent.powerPlant = null; cellComponent.occupiedBuilding = null;
        cellComponent.population = 0; cellComponent.powerConsumption = 0; cellComponent.waterConsumption = 0;
        cellComponent.happiness = 0; cellComponent.zoneType = Zone.ZoneType.Grass; cellComponent.buildingSize = 1;
        cellComponent.prefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass);
        cellComponent.prefabName = cellComponent.prefab.name; cellComponent.isPivot = false; cellComponent.SetTree(false);
    }

    void RestoreTile(GameObject cell)
    {
        CityCell cellComponent = cell.GetComponent<CityCell>();
        List<Transform> toDestroy = new List<Transform>();
        for (int i = 0; i < cell.transform.childCount; i++)
        {
            Transform child = cell.transform.GetChild(i);
            Zone zone = child.GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Grass) toDestroy.Add(child);
        }
        foreach (Transform t in toDestroy) Destroy(t.gameObject);
        GameObject zoneTile = Instantiate(zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass), cellComponent.transformPosition, Quaternion.identity);
        zoneTile.transform.SetParent(cell.transform);
        int sortingOrder;
        if (terrainManager != null)
        {
            sortingOrder = terrainManager.CalculateTerrainSortingOrder((int)cellComponent.x, (int)cellComponent.y, cellComponent.height);
            SpriteRenderer sr = zoneTile.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = sortingOrder;
            cellComponent.SetCellInstanceSortingOrder(sortingOrder);
        }
        else sortingOrder = SetTileSortingOrder(zoneTile, Zone.ZoneType.Grass);
    }

    void BulldozeTile(GameObject cell)
    {
        CityCell cellComponent = cell.GetComponent<CityCell>();
        int preSortingOrder = cellComponent.sortingOrder;
        if (cellComponent.forestType != Forest.ForestType.None && forestManager != null)
            forestManager.RemoveForestFromCell((int)cellComponent.x, (int)cellComponent.y);
        RestoreCellAttributes(cellComponent);
        DestroyCellChildren(cell, new Vector2(cellComponent.x, cellComponent.y));
        if (uiManager != null) uiManager.ShowDemolitionAnimation(cell, preSortingOrder);
    }

    void BulldozeBuildingTiles(GameObject cell, Zone.ZoneType zoneType, bool showAnimation = true)
    {
        CityCell cellComponent = cell.GetComponent<CityCell>();
        int buildingSize = cellComponent.buildingSize;
        int preSortingOrder = cellComponent.sortingOrder;
        if (showAnimation && uiManager != null) uiManager.ShowDemolitionAnimationCentered(cell, buildingSize, preSortingOrder);
        bool isBuilding = zoneType == Zone.ZoneType.ResidentialLightBuilding || zoneType == Zone.ZoneType.ResidentialMediumBuilding ||
            zoneType == Zone.ZoneType.ResidentialHeavyBuilding || zoneType == Zone.ZoneType.CommercialLightBuilding ||
            zoneType == Zone.ZoneType.CommercialMediumBuilding || zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
            zoneType == Zone.ZoneType.IndustrialLightBuilding || zoneType == Zone.ZoneType.IndustrialMediumBuilding || zoneType == Zone.ZoneType.IndustrialHeavyBuilding;
        if (buildingSize > 1)
        {
            GameObject pivotCellObj = cellComponent.isPivot ? cell : GetBuildingPivotCell(cellComponent);
            if (pivotCellObj == null) pivotCellObj = cell;
            CityCell pivotCell = pivotCellObj.GetComponent<CityCell>();
            GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);
            List<Vector2Int> footprint = isBuilding ? new List<Vector2Int>() : null;
            for (int x = 0; x < buildingSize; x++)
                for (int y = 0; y < buildingSize; y++)
                {
                    int gridX = (int)pivotCell.x + x - offsetX; int gridY = (int)pivotCell.y + y - offsetY;
                    if (gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
                    {
                        if (footprint != null) footprint.Add(new Vector2Int(gridX, gridY));
                        BulldozeTileWithoutAnimation(gridArray[gridX, gridY]);
                    }
                }
            if (footprint != null && footprint.Count > 0) onUrbanCellsBulldozed?.Invoke(footprint);
        }
        else
        {
            BulldozeTileWithoutAnimation(cell);
            if (isBuilding) onUrbanCellsBulldozed?.Invoke(new List<Vector2Int> { new Vector2Int((int)cellComponent.x, (int)cellComponent.y) });
        }
    }

    void BulldozeTileWithoutAnimation(GameObject cell)
    {
        CityCell cellComponent = cell.GetComponent<CityCell>();
        if (cellComponent.forestType != Forest.ForestType.None && forestManager != null)
            forestManager.RemoveForestFromCell((int)cellComponent.x, (int)cellComponent.y);
        RestoreCellAttributes(cellComponent);
        DestroyCellChildren(cell, new Vector2(cellComponent.x, cellComponent.y));
        int gx = (int)cellComponent.x; int gy = (int)cellComponent.y;
        HeightMap hm = terrainManager != null ? terrainManager.GetOrCreateHeightMap() : null;
        bool restoredWaterSlope = hm != null && terrainManager.RestoreTerrainForCell(gx, gy, hm);
        if (!restoredWaterSlope) RestoreTile(cell);
        if (roadManager != null) roadManager.UpdateAdjacentRoadPrefabsAt(new Vector2(gx, gy));
    }

    void HandleBuildingStatsReset(CityCell cellComponent, Zone.ZoneType zoneType)
    {
        string buildingType = cellComponent.GetBuildingType();
        if (buildingType == "PowerPlant") { PowerPlant powerPlant = (PowerPlant)cellComponent.powerPlant; cityStats.UnregisterPowerPlant(powerPlant); }
        if (buildingType == "WaterPlant") { WaterPlant waterPlant = (WaterPlant)cellComponent.waterPlant; if (waterManager != null && waterPlant != null) waterManager.UnregisterWaterPlant(waterPlant); }
        if (zoneType != Zone.ZoneType.Grass) cityStats.HandleBuildingDemolition(zoneType, zoneManager.GetZoneAttributes(zoneType));
    }

    void HandleBulldozeTile(Zone.ZoneType zoneType, GameObject cell, bool showAnimation = true)
    {
        CityCell cellComponent = cell.GetComponent<CityCell>();
        HandleBuildingStatsReset(cellComponent, zoneType);
        BulldozeBuildingTiles(cell, zoneType, showAnimation);
    }

    /// <summary>Demolish building/zoning at grid pos. Returns true if demolished.</summary>
    public bool DemolishCellAt(Vector2 gridPosition, bool showAnimation = true)
    {
        int gx = (int)gridPosition.x; int gy = (int)gridPosition.y;
        if (gx < 0 || gx >= width || gy < 0 || gy >= height) return false;
        GameObject cell = gridArray[gx, gy]; CityCell cellComponent = cellArray[gx, gy];
        if (cellComponent == null || !CanBulldoze(cellComponent)) return false;
        Zone.ZoneType zoneType = cellComponent.zoneType;
        if (zoneType == Zone.ZoneType.Road) { cellComponent.ClearRoadRouteHints(); RemoveRoadFromCache(new Vector2Int(gx, gy)); }
        HandleBulldozeTile(zoneType, cell, showAnimation);
        return true;
    }

    /// <summary>Direct API: demolish cell at grid coord. Returns true if demolished.</summary>
    public bool DemolishAt(Vector2Int grid)
        => DemolishCellAt(new Vector2(grid.x, grid.y), showAnimation: true);

    bool CanBulldoze(CityCell cell)
    {
        if (cell == null) return false;
        if (cell.isInterstate) { if (GameNotificationManager != null) GameNotificationManager.PostWarning("The Interstate Highway cannot be demolished."); return false; }
        if (cell.forestType != Forest.ForestType.None) return true;
        switch (cell.zoneType)
        {
            case Zone.ZoneType.Road: case Zone.ZoneType.Building:
            case Zone.ZoneType.ResidentialLightBuilding: case Zone.ZoneType.ResidentialMediumBuilding: case Zone.ZoneType.ResidentialHeavyBuilding:
            case Zone.ZoneType.CommercialLightBuilding: case Zone.ZoneType.CommercialMediumBuilding: case Zone.ZoneType.CommercialHeavyBuilding:
            case Zone.ZoneType.IndustrialLightBuilding: case Zone.ZoneType.IndustrialMediumBuilding: case Zone.ZoneType.IndustrialHeavyBuilding:
            case Zone.ZoneType.ResidentialLightZoning: case Zone.ZoneType.ResidentialMediumZoning: case Zone.ZoneType.ResidentialHeavyZoning:
            case Zone.ZoneType.CommercialLightZoning: case Zone.ZoneType.CommercialMediumZoning: case Zone.ZoneType.CommercialHeavyZoning:
            case Zone.ZoneType.IndustrialLightZoning: case Zone.ZoneType.IndustrialMediumZoning: case Zone.ZoneType.IndustrialHeavyZoning:
                return true;
            default: return false;
        }
    }

    void HandleShowTileDetails(Vector2 gridPosition)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (cellArray == null) return;
            int gx = (int)gridPosition.x; int gy = (int)gridPosition.y;
            if (gx < 0 || gy < 0 || gx >= cellArray.GetLength(0) || gy >= cellArray.GetLength(1)) return;
            CityCell cellComponent = cellArray[gx, gy];
            uiManager.ShowTileDetails(cellComponent);
        }
    }

    void HandleRaycast(Vector2 gridPosition)
    {
        Zone.ZoneType selectedZoneType = uiManager.GetSelectedZoneType();
        IBuilding selectedBuilding = uiManager.GetSelectedBuilding();
        IForest selectedForest = uiManager.GetSelectedForest();
        if (selectedZoneType == Zone.ZoneType.Road) roadManager.HandleRoadDrawing(gridPosition);
        else if (selectedZoneType == Zone.ZoneType.Water) HandleWaterPlacement(gridPosition);
        else if (selectedBuilding != null) HandleBuildingPlacement(gridPosition, selectedBuilding);
        else if (selectedForest != null) HandleForestPlacement(gridPosition, selectedForest);
        else if (IsStateServicePlacementMode()) HandleStateServicePlacement(gridPosition);
        else if (isInZoningMode()) zoneManager.HandleZoning(mouseGridPosition);
    }

    void HandleWaterPlacement(Vector2 gridPosition)
    {
        if (Input.GetMouseButton(0))
        {
            if (!cityStats.CanAfford(ZoneAttributes.Water.ConstructionCost)) { uiManager.ShowInsufficientFundsTooltip("Water", ZoneAttributes.Water.ConstructionCost); return; }
            if (waterManager != null) { waterManager.PlaceWater((int)gridPosition.x, (int)gridPosition.y); cityStats.RemoveMoney(ZoneAttributes.Water.ConstructionCost); }
        }
    }

    private bool IsStateServicePlacementMode() => ZoneManager.IsStateServiceZoneType(uiManager.GetSelectedZoneType());

    private void HandleStateServicePlacement(Vector2 gridPosition)
    {
        if (!Input.GetMouseButtonDown(0)) return;
        int subTypeId = uiManager.CurrentSubTypeId;
        if (subTypeId < 0) { uiManager.OpenSubTypePicker(); return; }
        var service = uiManager.ZoneSService;
        if (service == null) return;
        service.PlaceStateServiceZone((int)gridPosition.x, (int)gridPosition.y, subTypeId);
    }

    private bool isInZoningMode()
    {
        Zone.ZoneType sel = uiManager.GetSelectedZoneType();
        if (ZoneManager.IsStateServiceZoneType(sel)) return false;
        return sel != Zone.ZoneType.Grass && sel != Zone.ZoneType.Road && sel != Zone.ZoneType.None;
    }

    void HandleBuildingPlacement(Vector3 gridPosition, IBuilding selectedBuilding)
    {
        if (Input.GetMouseButtonDown(0)) placementService.PlaceBuilding(gridPosition, selectedBuilding);
    }

    void HandleForestPlacement(Vector2 gridPosition, IForest selectedForest)
    {
        if (Input.GetMouseButtonDown(0)) forestManager.PlaceForest(gridPosition, selectedForest);
    }
    #endregion

    #region CityCell Destruction and Attributes
    /// <summary>Destroy all non-terrain children of cell at grid pos.</summary>
    public void DestroyCellChildren(GameObject cell, Vector2 gridPosition)
        => DestroyCellChildren(cell, gridPosition, null, false);

    /// <summary>Destroy all children of cell except optional exclude.</summary>
    public void DestroyCellChildren(GameObject cell, Vector2 gridPosition, GameObject excludeFromDestroy)
        => DestroyCellChildren(cell, gridPosition, excludeFromDestroy, false);

    /// <summary>Destroy children of cell with optional flat-grass + exclusion control.</summary>
    public void DestroyCellChildren(GameObject cell, Vector2 gridPosition, GameObject excludeFromDestroy, bool destroyFlatGrass)
    {
        if (cell.transform.childCount == 0) return;
        var toDestroy = new List<GameObject>();
        foreach (Transform child in cell.transform)
        {
            if (excludeFromDestroy != null && child.gameObject == excludeFromDestroy) continue;
            Zone zone = child.GetComponent<Zone>();
            if (!destroyFlatGrass && zone != null && zone.zoneType == Zone.ZoneType.Grass) continue;
            if (terrainManager != null && (terrainManager.IsWaterSlopeObject(child.gameObject) || terrainManager.IsLandSlopeObject(child.gameObject))) continue;
            if (terrainManager != null && terrainManager.IsCliffStackTerrainObject(child.gameObject)) continue;
            if (zone != null && zone.zoneCategory == Zone.ZoneCategory.Zoning) zoneManager.removeZonedPositionFromList(gridPosition, zone.zoneType);
            toDestroy.Add(child.gameObject);
        }
        foreach (GameObject go in toDestroy) Destroy(go);
    }

    /// <summary>Destroy children except forest object (for zoning merge with forest).</summary>
    public void DestroyCellChildrenExceptForest(GameObject cell, Vector2 gridPosition)
    {
        if (cell.transform.childCount == 0) return;
        CityCell cellComponent = cellArray[(int)gridPosition.x, (int)gridPosition.y];
        GameObject forestObject = (cellComponent != null && cellComponent.HasForest()) ? cellComponent.forestObject : null;
        List<Transform> toDestroy = new List<Transform>();
        foreach (Transform child in cell.transform)
        {
            if (forestObject != null && child.gameObject == forestObject) continue;
            if (terrainManager != null && (terrainManager.IsWaterSlopeObject(child.gameObject) || terrainManager.IsLandSlopeObject(child.gameObject))) continue;
            if (terrainManager != null && terrainManager.IsCliffStackTerrainObject(child.gameObject)) continue;
            toDestroy.Add(child);
        }
        foreach (Transform child in toDestroy) { Zone zone = child.GetComponent<Zone>(); if (zone != null && zone.zoneCategory == Zone.ZoneCategory.Zoning) zoneManager.removeZonedPositionFromList(gridPosition, zone.zoneType); Destroy(child.gameObject); }
    }

    /// <summary>Overwrite cell zone type, population, power, happiness, prefab, building size.</summary>
    public void UpdateCellAttributes(CityCell cellComponent, Zone.ZoneType selectedZoneType, ZoneAttributes zoneAttributes, GameObject prefab, int buildingSize)
    {
        cellComponent.zoneType = selectedZoneType; cellComponent.population = zoneAttributes.Population;
        cellComponent.powerConsumption = zoneAttributes.PowerConsumption; cellComponent.waterConsumption = zoneAttributes.WaterConsumption;
        cellComponent.happiness = zoneAttributes.Happiness; cellComponent.prefab = prefab;
        cellComponent.prefabName = prefab.name; cellComponent.buildingType = prefab.name; cellComponent.buildingSize = buildingSize; cellComponent.isPivot = false;
    }

    private Vector2 FindCenterPosition(Vector2[] section)
    {
        float x = 0; float y = 0;
        foreach (Vector2 position in section) { x += position.x; y += position.y; }
        x /= section.Length; y /= section.Length;
        return new Vector2(x, y);
    }

    void UpdatePlacedBuildingCellAttributes(CityCell cell, int buildingSize, PowerPlant powerPlant, WaterPlant waterPlant, GameObject buildingPrefab, Zone.ZoneType zoneType = Zone.ZoneType.Building, GameObject building = null)
    {
        cell.occupiedBuilding = building; cell.buildingSize = buildingSize; cell.prefab = buildingPrefab;
        cell.prefabName = buildingPrefab.name; cell.zoneType = zoneType;
        if (powerPlant != null) { cell.buildingType = "PowerPlant"; cell.powerPlant = powerPlant; }
        if (waterPlant != null) { cell.buildingType = "WaterPlant"; cell.waterPlant = waterPlant; }
    }
    #endregion

    #region Save and Restore
    void SetGrassSortingOrderDirect(GameObject tile, int x, int y, int height, CityCell cell)
    {
        if (terrainManager != null)
        {
            int sortingOrder = terrainManager.CalculateTerrainSortingOrder(x, y, height);
            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = sortingOrder;
            cell.SetCellInstanceSortingOrder(sortingOrder);
        }
    }

    static void ApplySavedSpriteSorting(GameObject obj, int sortingOrder)
    {
        if (obj == null) return;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = sortingOrder;
    }

    static bool IsWaterShoreSavedCell(CellData cellData)
    {
        if (string.IsNullOrEmpty(cellData.prefabName)) return false;
        return cellData.prefabName.Contains("Bay") || cellData.prefabName.Contains("SlopeWater") || cellData.prefabName.Contains("UpslopeWater") || !string.IsNullOrEmpty(cellData.secondaryPrefabName);
    }

    int GetCellDataRestoreVisualPhase(CellData cellData)
    {
        if (zoneManager == null) return 1;
        Zone.ZoneType zoneType = zoneManager.GetZoneTypeFromZoneTypeString(cellData.zoneType);
        if (zoneType == Zone.ZoneType.Water) return 0;
        if (zoneType == Zone.ZoneType.Grass) return 1;
        if (ZoneManager.IsZoningType(zoneType)) return 2;
        if (zoneType == Zone.ZoneType.Road) return 3;
        if (IsZoneTypeBuilding(zoneType)) { if (cellData.buildingSize > 1 && !cellData.isPivot) return 5; return 4; }
        return 1;
    }

    List<CellData> SortCellDataForVisualRestore(List<CellData> gridData)
    {
        var list = new List<CellData>(gridData.Count);
        list.AddRange(gridData);
        list.Sort((a, b) => { int pa = GetCellDataRestoreVisualPhase(a); int pb = GetCellDataRestoreVisualPhase(b); if (pa != pb) return pa.CompareTo(pb); if (a.y != b.y) return a.y.CompareTo(b.y); return a.x.CompareTo(b.x); });
        return list;
    }

    GameObject FindBuildingRootOnPivotCell(CityCell cell, GameObject gridCell)
    {
        if (cell == null || gridCell == null) return null;
        if (cell.occupiedBuilding != null) return cell.occupiedBuilding;
        PowerPlant pp = gridCell.GetComponentInChildren<PowerPlant>(true);
        if (pp != null) return pp.gameObject;
        WaterPlant wp = gridCell.GetComponentInChildren<WaterPlant>(true);
        if (wp != null) return wp.gameObject;
        foreach (Transform child in gridCell.transform) { Zone z = child.GetComponent<Zone>(); if (z != null && z.zoneCategory == Zone.ZoneCategory.Building) return child.gameObject; }
        return null;
    }

    int RecalculateBuildingSortingAfterLoad(out int sortOrderChangeCount)
    {
        sortOrderChangeCount = 0;
        if (cellArray == null || sortingService == null || gridArray == null) return 0;
        int touched = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                CityCell cell = cellArray[x, y];
                if (cell == null) continue;
                if (cell.buildingSize > 1 && !cell.isPivot) continue;
                if (!IsZoneTypeBuilding(cell.zoneType)) continue;
                int buildingSize = cell.buildingSize > 0 ? cell.buildingSize : 1;
                GameObject gridCell = gridArray[x, y];
                GameObject buildingGo = FindBuildingRootOnPivotCell(cell, gridCell);
                if (buildingGo == null) continue;
                int orderBefore = cell.sortingOrder;
                sortingService.SetZoneBuildingSortingOrder(buildingGo, x, y, buildingSize);
                touched++;
                if (cell.sortingOrder != orderBefore) sortOrderChangeCount++;
            }
        return touched;
    }

    void RestoreGridCellVisuals(CellData cellData, GameObject cell)
    {
        Zone.ZoneType zoneType = zoneManager.GetZoneTypeFromZoneTypeString(cellData.zoneType);
        cell.transform.position = new Vector3(cellData.transformPosition.x, cellData.transformPosition.y, cell.transform.position.z);
        cellArray[cellData.x, cellData.y].transformPosition = cellData.transformPosition;

        if (zoneType == Zone.ZoneType.Water && waterManager != null)
        {
            CityCell cellComponent = cellArray[cellData.x, cellData.y];
            for (int i = cell.transform.childCount - 1; i >= 0; i--) Destroy(cell.transform.GetChild(i).gameObject);
            GameObject waterPrefab = waterManager.FindWaterPrefabByName(cellData.prefabName);
            if (waterPrefab == null) return;
            int surfaceHeight = waterManager.GetWaterSurfaceHeight(cellData.x, cellData.y);
            if (surfaceHeight < 0) surfaceHeight = waterManager.seaLevel;
            int visualSurfaceHeight = Mathf.Max(TerrainManager.MIN_HEIGHT, surfaceHeight - 1);
            float halfCellHeight = tileHeight * 0.25f;
            Vector2 waterSurfaceWorld = GetWorldPositionVector(cellData.x, cellData.y, visualSurfaceHeight);
            Vector2 waterTileWorldPos = waterSurfaceWorld + new Vector2(0f, halfCellHeight);
            GameObject waterTile = Instantiate(waterPrefab, waterTileWorldPos, Quaternion.identity);
            Zone zone = waterTile.AddComponent<Zone>(); zone.zoneType = Zone.ZoneType.Water; zone.zoneCategory = Zone.ZoneCategory.Water;
            waterTile.transform.SetParent(cell.transform);
            int waterSort = terrainManager != null ? terrainManager.CalculateTerrainSortingOrder(cellData.x, cellData.y, visualSurfaceHeight) : cellData.sortingOrder;
            ApplySavedSpriteSorting(waterTile, waterSort); cellComponent.SetCellInstanceSortingOrder(waterSort);
            return;
        }

        if (zoneType == Zone.ZoneType.Grass)
        {
            if (IsWaterShoreSavedCell(cellData) && terrainManager != null)
            {
                terrainManager.RestoreWaterShorePrefabsFromSave(cellData.x, cellData.y, cellData.prefabName, cellData.secondaryPrefabName ?? "", cellData.sortingOrder);
                if (forestManager != null) { Forest.ForestType parsedForestType = Forest.ForestType.None; if (!string.IsNullOrEmpty(cellData.forestType)) System.Enum.TryParse(cellData.forestType, out parsedForestType); int forestOrder = cellArray[cellData.x, cellData.y].sortingOrder + 5; forestManager.RestoreForestAt(cellData.x, cellData.y, parsedForestType, cellData.forestPrefabName, updateStats: false, savedSpriteSortingOrder: forestOrder); }
                return;
            }
            if (!string.IsNullOrEmpty(cellData.prefabName) && cellData.prefabName.Contains("Slope"))
            {
                GameObject slopePrefab = terrainManager != null ? terrainManager.FindTerrainPrefabByName(cellData.prefabName) : null;
                if (slopePrefab != null && terrainManager != null)
                {
                    terrainManager.PlaceSlopeFromPrefab(cellData.x, cellData.y, slopePrefab, cellData.height);
                    foreach (Transform child in cell.transform) { ApplySavedSpriteSorting(child.gameObject, cellData.sortingOrder); break; }
                    cellArray[cellData.x, cellData.y].SetCellInstanceSortingOrder(cellData.sortingOrder);
                }
                else
                {
                    GameObject fallbackGrass = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, 1) ?? (zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0 ? zoneManager.grassPrefabs[0] : null);
                    if (fallbackGrass != null)
                    {
                        for (int i = cell.transform.childCount - 1; i >= 0; i--) Destroy(cell.transform.GetChild(i).gameObject);
                        CityCell cc = cellArray[cellData.x, cellData.y];
                        GameObject zoneTile = Instantiate(fallbackGrass, cc.transformPosition, Quaternion.identity);
                        zoneTile.transform.SetParent(cell.transform); ApplySavedSpriteSorting(zoneTile, cellData.sortingOrder); cc.SetCellInstanceSortingOrder(cellData.sortingOrder);
                        Zone z = zoneTile.GetComponent<Zone>(); if (z == null) z = zoneTile.AddComponent<Zone>(); z.zoneType = Zone.ZoneType.Grass;
                    }
                }
                if (forestManager != null) { Forest.ForestType parsedForestType = Forest.ForestType.None; if (!string.IsNullOrEmpty(cellData.forestType)) System.Enum.TryParse(cellData.forestType, out parsedForestType); int forestOrder = cellData.sortingOrder + 5; forestManager.RestoreForestAt(cellData.x, cellData.y, parsedForestType, cellData.forestPrefabName, updateStats: false, savedSpriteSortingOrder: forestOrder); }
                return;
            }
            GameObject grassPrefab = terrainManager != null ? terrainManager.FindTerrainPrefabByName(cellData.prefabName) : null;
            if (grassPrefab == null) grassPrefab = zoneManager.FindPrefabByName(cellData.prefabName);
            if (grassPrefab == null) grassPrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, 1);
            if (grassPrefab == null) grassPrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, Mathf.Clamp(cellData.height, 1, 5));
            if (grassPrefab == null && zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0) grassPrefab = zoneManager.grassPrefabs[0];
            if (grassPrefab != null)
            {
                for (int i = cell.transform.childCount - 1; i >= 0; i--) Destroy(cell.transform.GetChild(i).gameObject);
                CityCell cellComponent = cellArray[cellData.x, cellData.y];
                GameObject zoneTile = Instantiate(grassPrefab, cellComponent.transformPosition, Quaternion.identity);
                zoneTile.transform.SetParent(cell.transform); ApplySavedSpriteSorting(zoneTile, cellData.sortingOrder); cellComponent.SetCellInstanceSortingOrder(cellData.sortingOrder);
                Zone zoneComponent = zoneTile.GetComponent<Zone>(); if (zoneComponent == null) { zoneComponent = zoneTile.AddComponent<Zone>(); zoneComponent.zoneType = Zone.ZoneType.Grass; }
            }
            if (forestManager != null) { Forest.ForestType parsedForestType = Forest.ForestType.None; if (!string.IsNullOrEmpty(cellData.forestType)) System.Enum.TryParse(cellData.forestType, out parsedForestType); int forestOrder = cellData.sortingOrder + 5; forestManager.RestoreForestAt(cellData.x, cellData.y, parsedForestType, cellData.forestPrefabName, updateStats: false, savedSpriteSortingOrder: forestOrder); }
            return;
        }

        GameObject tilePrefab = zoneManager.FindPrefabByName(cellData.prefabName);
        if (tilePrefab == null && terrainManager != null) tilePrefab = terrainManager.FindTerrainPrefabByName(cellData.prefabName);
        if (tilePrefab == null && zoneType == Zone.ZoneType.Road && roadManager != null) { var roadPrefabs = roadManager.GetRoadPrefabs(); if (roadPrefabs != null && roadPrefabs.Count > 0) tilePrefab = roadPrefabs[0]; }

        if (tilePrefab != null)
        {
            bool isMultiCellUtilityBuilding = cellData.buildingSize > 1 && cellData.isPivot && (tilePrefab.GetComponent<PowerPlant>() != null || tilePrefab.GetComponent<WaterPlant>() != null);
            if (isMultiCellUtilityBuilding) placementService.RestoreBuildingTile(tilePrefab, new Vector2(cellData.x, cellData.y), cellData.buildingSize);
            else if (zoneType == Zone.ZoneType.Road && roadManager != null) roadManager.RestoreRoadTile(new Vector2Int(cellData.x, cellData.y), tilePrefab, cellData.isInterstate, cellData.sortingOrder);
            else if (ZoneManager.IsZoningType(zoneType)) zoneManager.RestoreZoneTile(tilePrefab, cell, zoneType);
            else if (!(cellData.buildingSize > 1 && !cellData.isPivot))
            {
                zoneManager.PlaceZoneBuildingTile(tilePrefab, cell, cellData.buildingSize);
                CityCell cellComponent = cellArray[cellData.x, cellData.y];
                PowerPlant powerPlant = cell.GetComponentInChildren<PowerPlant>(); WaterPlant waterPlant = cell.GetComponentInChildren<WaterPlant>();
                UpdatePlacedBuildingCellAttributes(cellComponent, cellData.buildingSize, powerPlant, waterPlant, tilePrefab, zoneType, null);
                if (powerPlant != null) cityStats.RegisterPowerPlant(powerPlant);
                if (waterPlant != null && waterManager != null) { waterManager.RegisterWaterPlant(waterPlant); cityStats.cityWaterOutput = waterManager.GetTotalWaterOutput(); }
            }
        }
        zoneManager.addZonedTileToList(new Vector2(cellData.x, cellData.y), zoneType);
        if (forestManager != null) { Forest.ForestType parsedForestType = Forest.ForestType.None; if (!string.IsNullOrEmpty(cellData.forestType)) System.Enum.TryParse(cellData.forestType, out parsedForestType); int forestOrder = cellData.sortingOrder + 5; forestManager.RestoreForestAt(cellData.x, cellData.y, parsedForestType, cellData.forestPrefabName, updateStats: false, savedSpriteSortingOrder: forestOrder); }
    }

    /// <summary>Restore grid from save data. Two-pass: restore cell data, then place tiles.</summary>
    public void RestoreGrid(List<CellData> gridData)
    {
        if (gridData == null || cellArray == null) return;
        foreach (CellData cellData in gridData)
        {
            if (cellData.x < 0 || cellData.x >= width || cellData.y < 0 || cellData.y >= height) continue;
            cellArray[cellData.x, cellData.y].SetCellData(cellData);
        }
        List<CellData> sortedForVisuals = SortCellDataForVisualRestore(gridData);
        foreach (CellData cellData in sortedForVisuals)
        {
            if (cellData.x < 0 || cellData.x >= width || cellData.y < 0 || cellData.y >= height) continue;
            GameObject cell = gridArray[cellData.x, cellData.y];
            RestoreGridCellVisuals(cellData, cell);
        }
        RecalculateBuildingSortingAfterLoad(out _);
        if (forestManager != null) forestManager.RefreshForestStatistics();
        InvalidateRoadCache();
        onGridRestored?.Invoke();
        zoneManager.CalculateAvailableSquareZonedSections();
        RunPostLoadDiagnosticAndSafetyNet();
        skipChunkCullingFramesRemaining = 3;
    }

    void RunPostLoadDiagnosticAndSafetyNet()
    {
        if (cellArray == null || zoneManager == null) return;
        GameObject fallbackGrass = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, 1) ?? (zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0 ? zoneManager.grassPrefabs[0] : null);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                CityCell cellComponent = cellArray[x, y];
                if (cellComponent == null) continue;
                if (cellComponent.zoneType == Zone.ZoneType.Grass && gridArray[x, y].transform.childCount == 0)
                {
                    if (fallbackGrass != null)
                    {
                        GameObject zoneTile = Instantiate(fallbackGrass, cellComponent.transformPosition, Quaternion.identity);
                        zoneTile.transform.SetParent(gridArray[x, y].transform);
                        SetGrassSortingOrderDirect(zoneTile, x, y, cellComponent.height, cellComponent);
                        Zone z = zoneTile.GetComponent<Zone>(); if (z == null) { z = zoneTile.AddComponent<Zone>(); z.zoneType = Zone.ZoneType.Grass; }
                    }
                }
            }
    }

    /// <summary>Destroy all chunks + recreate fresh empty grid.</summary>
    public void ResetGrid()
    {
        if (chunkObjects != null) for (int cx = 0; cx < chunksX; cx++) for (int cy = 0; cy < chunksY; cy++) { if (chunkObjects[cx, cy] != null) Destroy(chunkObjects[cx, cy]); }
        zoneManager.ClearZonedPositions(); InvalidateRoadCache(); onGridRestored?.Invoke();
        cellArray = null; CreateGrid();
    }

    /// <summary>Clear grid + recreate fresh for Load.</summary>
    public void ResetGridForLoad()
    {
        if (chunkObjects != null) for (int cx = 0; cx < chunksX; cx++) for (int cy = 0; cy < chunksY; cy++) { if (chunkObjects[cx, cy] != null) Destroy(chunkObjects[cx, cy]); }
        zoneManager.ClearZonedPositions(); InvalidateRoadCache();
        cellArray = null; CreateGrid(createBaseTiles: true);
        if (chunkCulling != null)
        {
            chunkCulling.chunkObjects = chunkObjects; chunkCulling.chunkActiveState = chunkActiveState; chunkCulling.chunksX = chunksX; chunkCulling.chunksY = chunksY;
            int maxCx = Mathf.Min(chunksX, chunkObjects != null ? chunkObjects.GetLength(0) : 0);
            int maxCy = Mathf.Min(chunksY, chunkObjects != null ? chunkObjects.GetLength(1) : 0);
            for (int cx = 0; cx < maxCx; cx++)
                for (int cy = 0; cy < maxCy; cy++)
                {
                    if (chunkActiveState != null && cx < chunkActiveState.GetLength(0) && cy < chunkActiveState.GetLength(1)) chunkActiveState[cx, cy] = true;
                    if (chunkObjects[cx, cy] != null) chunkObjects[cx, cy].SetActive(true);
                }
        }
    }

    void DestroyPreviousZoning(GameObject cell)
    {
        if (cell.transform.childCount > 0) { var toDestroy = new List<GameObject>(); foreach (Transform child in cell.transform) toDestroy.Add(child.gameObject); foreach (GameObject go in toDestroy) Destroy(go); }
    }

    bool IsWithinGrid(Vector2 position)
    {
        foreach (GameObject cell in gridArray) { Vector3 position3D = new Vector3(position.x, position.y, 0); if (cell.transform.position == position3D) return true; }
        return false;
    }
    #endregion
}
}
