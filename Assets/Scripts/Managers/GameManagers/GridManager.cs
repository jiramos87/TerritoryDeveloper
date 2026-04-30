using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
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

namespace Territory.Core
{
/// <summary>
/// Central hub for isometric 2D grid. Manages cell arrays, world↔grid conversion,
/// building placement + validation, bulldozing, sorting order, chunk culling, road cache, A* pathfinding.
/// Most managers depend on GridManager for cell access via GetCell(x,y) + coordinate utilities.
/// Mouse→cell: <see cref="RefineGridPositionForTerrainHeight"/> + screen-space disambiguation in <see cref="GetCellFromWorldPoint"/>.
/// Execution order -100 → <see cref="Update"/> runs before default UI + <see cref="mouseGridPosition"/> matches tools same frame.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GridManager : MonoBehaviour, IGridManager
{
    #region Dependencies
    public ZoneManager zoneManager;
    public UIManager uiManager;
    public CityStats cityStats;
    public CursorManager cursorManager;
    public TerrainManager terrainManager;
    public DemandManager demandManager;
    public WaterManager waterManager;
    public GameNotificationManager GameNotificationManager;
    public ForestManager forestManager;
    public CameraController cameraController;
    public RoadManager roadManager;
    public InterstateManager interstateManager;
    public BuildingSelectorMenuController buildingSelectorMenuController;

    // Helper services (initialized in InitializeGrid)
    private GridPathfinder pathfinder;
    private GridSortingOrderService sortingService;
    private BuildingPlacementService placementService;
    private ChunkCullingSystem chunkCulling;
    private RoadCacheService roadCache;

    /// <summary>Fired when urban cells (buildings) bulldozed. Args: grid positions list.</summary>
    public System.Action<System.Collections.Generic.IReadOnlyList<Vector2Int>> onUrbanCellsBulldozed;

    /// <summary>Fired when grid restored from save. Listeners must invalidate caches.</summary>
    public System.Action onGridRestored;
    #endregion

    #region Grid Configuration
    public int width, height;
    public float tileWidth = 1f; // Full width of the tile
    public float tileHeight = 0.5f; // Effective height due to isometric perspective

    public float halfWidth;
    public float halfHeight;
    public GameObject[,] gridArray;
    public CityCell[,] cellArray;
    public Vector2 mouseGridPosition;
    /// <summary>Last grid cell clicked (left/right). (-1,-1) if none.</summary>
    public Vector2 selectedPoint = new Vector2(-1, -1);
    /// <summary>Grid pos at right-click down. Sets selectedPoint on right-click up if not pan.</summary>
    private Vector2 pendingRightClickGridPosition = new Vector2(-1, -1);
    public int mouseGridHeight;
    public int mouseGridSortingOrder;

    public bool isInitialized = false;

    #region Parent-scale identity
    /// <summary>Parent region GUID. Set once via HydrateParentIds; read-only after that.</summary>
    public string ParentRegionId { get; private set; }
    /// <summary>Parent country GUID. Set once via HydrateParentIds; read-only after that.</summary>
    public string ParentCountryId { get; private set; }
    private bool _parentIdsHydrated;

    /// <summary>
    /// Set parent region + country ids for this city. Called by GameSaveManager on load and new-game.
    /// One-shot per GridManager lifecycle: duplicate call logs error and returns without overwriting.
    /// Null/empty args log error and return without setting.
    /// </summary>
    public void HydrateParentIds(string regionId, string countryId)
    {
        if (string.IsNullOrEmpty(regionId) || string.IsNullOrEmpty(countryId))
        {
            Debug.LogError("[GridManager] HydrateParentIds: regionId or countryId is null/empty — skipping hydration.");
            return;
        }
        if (_parentIdsHydrated)
        {
            Debug.LogError("[GridManager] HydrateParentIds: already hydrated — duplicate call ignored.");
            return;
        }
        ParentRegionId = regionId;
        ParentCountryId = countryId;
        _parentIdsHydrated = true;
    }

    /// <summary>
    /// Cached neighbor-city stubs for this city session. Set once via HydrateNeighborStubs.
    /// Read-only after hydration; empty until hydrated (not null).
    /// </summary>
    private System.Collections.Generic.IReadOnlyList<NeighborCityStub> _neighborStubs = System.Array.Empty<NeighborCityStub>();
    private bool _neighborStubsHydrated;

    /// <summary>
    /// Populate the neighbor-city stub cache for this session.
    /// Called by <see cref="Territory.Persistence.GameSaveManager"/> on NewGame and LoadGame,
    /// after <see cref="HydrateParentIds"/>. One-shot: duplicate call logs error and returns.
    /// Null input logs error and returns.
    /// </summary>
    public void HydrateNeighborStubs(System.Collections.Generic.IEnumerable<NeighborCityStub> stubs)
    {
        if (stubs == null)
        {
            Debug.LogError("[GridManager] HydrateNeighborStubs: stubs argument is null — skipping hydration.");
            return;
        }
        if (_neighborStubsHydrated)
        {
            Debug.LogError("[GridManager] HydrateNeighborStubs: already hydrated — duplicate call ignored.");
            return;
        }
        _neighborStubs = new System.Collections.Generic.List<NeighborCityStub>(stubs).AsReadOnly();
        _neighborStubsHydrated = true;
    }

    /// <summary>
    /// Return the first <see cref="NeighborCityStub"/> whose <c>borderSide</c> matches <paramref name="side"/>,
    /// or <c>null</c> if no stub is registered on that side. Linear scan over ≤4 entries (MVP cardinality).
    /// Null return is not an error — it means no neighbor city on that border.
    /// </summary>
    public NeighborCityStub? GetNeighborStub(BorderSide side)
    {
        foreach (var stub in _neighborStubs)
        {
            if (stub.borderSide == side)
                return stub;
        }
        return null;
    }
    #endregion

    [Header("Chunk Culling")]
    public int chunkSize = 16;
    private GameObject[,] chunkObjects;
    private bool[,] chunkActiveState;
    private int chunksX, chunksY;
    [SerializeField] private Camera cachedCamera;
    /// <summary>&gt; 0 → skip UpdateVisibility. Avoids mis-culling chunks right after Load.</summary>
    private int skipChunkCullingFramesRemaining = 0;
    #endregion

    #region Initialization
    /// <summary>
    /// Bootstrap grid: resolve deps, create cell/chunk arrays, generate terrain, center camera.
    /// </summary>
    public void InitializeGrid()
    {
        halfWidth = tileWidth / 2f;
        halfHeight = tileHeight / 2f;

        // Default to 64x64 when not set in Inspector (balance between area and performance)
        if (width <= 0) width = 64;
        if (height <= 0) height = 64;

        if (zoneManager == null)
            zoneManager = FindObjectOfType<ZoneManager>();
        if (zoneManager != null)
            zoneManager.InitializeZonePrefabs();

        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }

        if (cityStats == null)
        {
            cityStats = FindObjectOfType<CityStats>();
        }

        if (cursorManager == null)
        {
            cursorManager = FindObjectOfType<CursorManager>();
        }

        if (terrainManager == null)
        {
            terrainManager = FindObjectOfType<TerrainManager>();
        }

        if (waterManager == null)
        {
            waterManager = FindObjectOfType<WaterManager>();
        }

        if (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
        }

        if (roadManager == null)
            roadManager = FindObjectOfType<RoadManager>();
        if (roadManager != null)
            roadManager.Initialize();

        if (demandManager == null)
        {
            demandManager = FindObjectOfType<DemandManager>();
        }

        if (GameNotificationManager == null)
        {
            GameNotificationManager = FindObjectOfType<GameNotificationManager>();
        }

        if (forestManager == null)
        {
            forestManager = FindObjectOfType<ForestManager>();
        }

        if (interstateManager == null)
        {
            interstateManager = FindObjectOfType<InterstateManager>();
        }

        // Create sortingService before CreateGrid() so SetTileSortingOrder is available during grid creation
        sortingService = new GridSortingOrderService(this);

        CreateGrid();
        terrainManager.InitializeHeightMap();
        isInitialized = true;
        Vector3 centerWorldPosition = GetWorldPosition(
            width / 2, height / 2
        );

        cameraController.MoveCameraToMapCenter(centerWorldPosition);

        // Initialize helper services (sortingService already created above for CreateGrid)
        pathfinder = new GridPathfinder(this);
        placementService = new BuildingPlacementService(this, sortingService);
        chunkCulling = new ChunkCullingSystem(this, chunkSize, cachedCamera);
        chunkCulling.chunkObjects = chunkObjects;
        chunkCulling.chunkActiveState = chunkActiveState;
        roadCache = new RoadCacheService(this);
    }

    /// <param name="createBaseTiles">false → cells created without grass tiles (for Load). RestoreGrid places tiles from save.</param>
    void CreateGrid(bool createBaseTiles = true)
    {
        if (!zoneManager)
        {
            zoneManager = FindObjectOfType<ZoneManager>();
        }

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
                if (tilePrefab != null)
                {
                    cellData.prefab = tilePrefab;
                    cellData.prefabName = tilePrefab.name;
                }

                CityCell cellComponent = gridCell.AddComponent<CityCell>();
                cellComponent.SetCellData(cellData);

                gridArray[x, y] = gridCell;
                cellArray[x, y] = cellComponent;

                if (createBaseTiles)
                {
                    GameObject zoneTile = Instantiate(
                        tilePrefab,
                        gridCell.transform.position,
                        Quaternion.identity
                    );
                    SetTileSortingOrder(zoneTile, Zone.ZoneType.Grass);

                    Zone zoneComponent = zoneTile.GetComponent<Zone>();
                    if (zoneComponent == null)
                    {
                        zoneComponent = zoneTile.AddComponent<Zone>();
                        zoneComponent.zoneType = Zone.ZoneType.Grass;
                    }
                }
            }
        }
    }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (cachedCamera == null)
            cachedCamera = Camera.main;
    }

    void Update()
    {
        try
        {
            if (!isInitialized)
            {
                return;
            }
            if (gridArray == null || gridArray.Length == 0)
            {
                return;
            }
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (uiManager.isBulldozeMode())
                {
                    uiManager.ExitBulldozeMode();
                }
                else if (uiManager.IsDetailsMode())
                {
                    uiManager.ExitDetailsMode();
                }
                else if (uiManager.IsBuildingPlacementMode())
                {
                    uiManager.ExitBuildingPlacementMode();
                }
                buildingSelectorMenuController.DeselectAndUnpressAllButtons();
            }

            Vector2 worldPoint = ScreenPointToWorldOnGridPlane(cachedCamera, Input.mousePosition);

            // USE: Height-aware grid position calculation
            // mouseGridPosition = GetGridPosition(worldPoint);
            CityCell mouseGridCell = GetMouseGridCell(worldPoint);
            if (mouseGridCell == null)
            {
                return;
            }
            mouseGridPosition = new Vector2(mouseGridCell.x, mouseGridCell.y);
            mouseGridHeight = mouseGridCell.GetCellInstanceHeight();
            mouseGridSortingOrder = mouseGridCell.sortingOrder;

            if (mouseGridPosition.x == -1 && mouseGridPosition.y == -1)
            {
                return;
            }

            if (!IsValidGridPosition(mouseGridPosition))
            {
                return;
            }

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

            if (uiManager.isBulldozeMode())
            {
                HandleBulldozerMode(mouseGridPosition);
            }

            if (uiManager.IsDetailsMode() || Input.GetKey(KeyCode.LeftShift))
            {
                HandleShowTileDetails(mouseGridPosition);
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
        if (skipChunkCullingFramesRemaining > 0)
        {
            skipChunkCullingFramesRemaining--;
            return;
        }
        chunkCulling?.UpdateVisibility();
    }
    #endregion

    #region CityCell Queries
    /// <summary>
    /// True if grid pos inside grid bounds.
    /// </summary>
    /// <param name="gridPosition">Grid coords to validate.</param>
    /// <returns>True when x ∈ [0, width) and y ∈ [0, height).</returns>
    public bool IsValidGridPosition(Vector2 gridPosition)
    {
        int gridX = (int)gridPosition.x;
        int gridY = (int)gridPosition.y;

        return gridX >= 0 && gridX < width && gridY >= 0 && gridY < height;
    }

    /// <summary>
    /// True if cell occupied by building (any tile of multi-cell footprint).
    /// </summary>
    public bool IsCellOccupiedByBuilding(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;
        CityCell cell = cellArray[x, y];
        if (cell == null) return false;
        return cell.occupiedBuilding != null || IsZoneTypeBuilding(cell.zoneType);
    }

    bool IsZoneTypeBuilding(Zone.ZoneType zoneType)
    {
        return zoneType == Zone.ZoneType.Building ||
               zoneType == Zone.ZoneType.ResidentialLightBuilding || zoneType == Zone.ZoneType.ResidentialMediumBuilding || zoneType == Zone.ZoneType.ResidentialHeavyBuilding ||
               zoneType == Zone.ZoneType.CommercialLightBuilding || zoneType == Zone.ZoneType.CommercialMediumBuilding || zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
               zoneType == Zone.ZoneType.IndustrialLightBuilding || zoneType == Zone.ZoneType.IndustrialMediumBuilding || zoneType == Zone.ZoneType.IndustrialHeavyBuilding;
    }

    /// <summary>
    /// Footprint offset for building (even = 0,0; odd = buildingSize/2).
    /// </summary>
    public void GetBuildingFootprintOffset(int buildingSize, out int offsetX, out int offsetY)
    {
        if (buildingSize % 2 == 0)
        {
            offsetX = 0;
            offsetY = 0;
        }
        else
        {
            offsetX = buildingSize / 2;
            offsetY = buildingSize / 2;
        }
    }

    /// <summary>
    /// Return pivot cell of multi-cell building. If given cell inside footprint, find + return pivot (isPivot=true).
    /// </summary>
    public GameObject GetBuildingPivotCell(CityCell cell)
    {
        if (cell == null)
            return null;
        if (cell.occupiedBuilding == null || cell.buildingSize <= 1)
            return gridArray[(int)cell.x, (int)cell.y];

        int size = cell.buildingSize;
        int cx = (int)cell.x;
        int cy = (int)cell.y;

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int px = cx - i;
                int py = cy - j;
                if (px >= 0 && px < width && py >= 0 && py < height)
                {
                    CityCell pivotCandidate = cellArray[px, py];
                    if (pivotCandidate != null && pivotCandidate.isPivot)
                        return gridArray[px, py];
                }
            }
        }
        return gridArray[cx, cy];
    }
    #endregion

    #region Input and Placement Handlers
    bool IsInWaterPlacementMode()
    {
        return uiManager.GetSelectedZoneType() == Zone.ZoneType.Water;
    }

    void HandleBulldozerMode(Vector2 gridPosition)
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleBulldozerClick(gridPosition);
        }
    }

    void HandleBulldozerClick(Vector2 gridPosition)
    {
        GameObject cell = gridArray[(int)gridPosition.x, (int)gridPosition.y];
        CityCell cellComponent = cellArray[(int)gridPosition.x, (int)gridPosition.y];
        Zone.ZoneType zoneType = cellComponent.zoneType;

        if (!CanBulldoze(cellComponent))
        {
            return;
        }

        HandleBulldozeTile(zoneType, cell);
    }

    void RestoreCellAttributes(CityCell cellComponent)
    {
        cellComponent.buildingType = null;
        cellComponent.powerPlant = null;
        cellComponent.occupiedBuilding = null;
        cellComponent.population = 0;
        cellComponent.powerConsumption = 0;
        cellComponent.waterConsumption = 0;
        cellComponent.happiness = 0;
        cellComponent.zoneType = Zone.ZoneType.Grass;
        cellComponent.buildingSize = 1;
        cellComponent.prefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass);
        cellComponent.prefabName = cellComponent.prefab.name;
        cellComponent.isPivot = false;
        cellComponent.SetTree(false);
    }

    void RestoreTile(GameObject cell)
    {
        CityCell cellComponent = cell.GetComponent<CityCell>();

        // Ensure only one grass child: remove any existing grass tiles before adding the new one
        List<Transform> toDestroy = new List<Transform>();
        for (int i = 0; i < cell.transform.childCount; i++)
        {
            Transform child = cell.transform.GetChild(i);
            Zone zone = child.GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Grass)
                toDestroy.Add(child);
        }
        foreach (Transform t in toDestroy)
            Destroy(t.gameObject);

        GameObject zoneTile = Instantiate(
            zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass),
            cellComponent.transformPosition,
            Quaternion.identity
        );

        zoneTile.transform.SetParent(cell.transform);

        int sortingOrder;
        if (terrainManager != null)
        {
            sortingOrder = terrainManager.CalculateTerrainSortingOrder((int)cellComponent.x, (int)cellComponent.y, cellComponent.height);
            SpriteRenderer sr = zoneTile.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = sortingOrder;
            }
            cellComponent.SetCellInstanceSortingOrder(sortingOrder);
        }
        else
        {
            sortingOrder = SetTileSortingOrder(zoneTile, Zone.ZoneType.Grass);
        }
    }

    void BulldozeTile(GameObject cell)
    {
        CityCell cellComponent = cell.GetComponent<CityCell>();

        // Capture sorting order BEFORE demolition resets it
        int preSortingOrder = cellComponent.sortingOrder;

        if (cellComponent.forestType != Forest.ForestType.None && forestManager != null)
            forestManager.RemoveForestFromCell((int)cellComponent.x, (int)cellComponent.y);

        RestoreCellAttributes(cellComponent);

        DestroyCellChildren(cell, new Vector2(cellComponent.x, cellComponent.y));

        // Show the bulldoze animation using the pre-captured sorting order
        if (uiManager != null)
        {
            uiManager.ShowDemolitionAnimation(cell, preSortingOrder);
        }
    }

    void BulldozeBuildingTiles(GameObject cell, Zone.ZoneType zoneType, bool showAnimation = true)
    {
        CityCell cellComponent = cell.GetComponent<CityCell>();

        int buildingSize = cellComponent.buildingSize;

        // Capture sorting order BEFORE demolition resets it
        int preSortingOrder = cellComponent.sortingOrder;

        // Show animation before demolishing for better visual effect
        if (showAnimation && uiManager != null)
        {
            uiManager.ShowDemolitionAnimationCentered(cell, buildingSize, preSortingOrder);
        }

        bool isBuilding = zoneType == Zone.ZoneType.ResidentialLightBuilding || zoneType == Zone.ZoneType.ResidentialMediumBuilding ||
            zoneType == Zone.ZoneType.ResidentialHeavyBuilding || zoneType == Zone.ZoneType.CommercialLightBuilding ||
            zoneType == Zone.ZoneType.CommercialMediumBuilding || zoneType == Zone.ZoneType.CommercialHeavyBuilding ||
            zoneType == Zone.ZoneType.IndustrialLightBuilding || zoneType == Zone.ZoneType.IndustrialMediumBuilding ||
            zoneType == Zone.ZoneType.IndustrialHeavyBuilding;

        if (buildingSize > 1)
        {
            // If clicked cell is not pivot, find pivot to get correct footprint
            GameObject pivotCellObj = cellComponent.isPivot ? cell : GetBuildingPivotCell(cellComponent);
            if (pivotCellObj == null)
                pivotCellObj = cell;
            CityCell pivotCell = pivotCellObj.GetComponent<CityCell>();

            GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);

            List<Vector2Int> footprint = null;
            if (isBuilding)
                footprint = new List<Vector2Int>();

            for (int x = 0; x < buildingSize; x++)
            {
                for (int y = 0; y < buildingSize; y++)
                {
                    int gridX = (int)pivotCell.x + x - offsetX;
                    int gridY = (int)pivotCell.y + y - offsetY;

                    if (gridX >= 0 && gridX < width && gridY >= 0 && gridY < height)
                    {
                        if (footprint != null)
                            footprint.Add(new Vector2Int(gridX, gridY));
                        BulldozeTileWithoutAnimation(gridArray[gridX, gridY]);
                    }
                }
            }
            if (footprint != null && footprint.Count > 0)
                onUrbanCellsBulldozed?.Invoke(footprint);
        }
        else
        {
            BulldozeTileWithoutAnimation(cell);
            if (isBuilding)
                onUrbanCellsBulldozed?.Invoke(new List<Vector2Int> { new Vector2Int((int)cellComponent.x, (int)cellComponent.y) });
        }
    }

    // Create a version without animation for multi-tile cleanup
    void BulldozeTileWithoutAnimation(GameObject cell)
    {
        CityCell cellComponent = cell.GetComponent<CityCell>();

        if (cellComponent.forestType != Forest.ForestType.None && forestManager != null)
            forestManager.RemoveForestFromCell((int)cellComponent.x, (int)cellComponent.y);

        RestoreCellAttributes(cellComponent);
        DestroyCellChildren(cell, new Vector2(cellComponent.x, cellComponent.y));

        int gx = (int)cellComponent.x;
        int gy = (int)cellComponent.y;
        HeightMap hm = terrainManager != null ? terrainManager.GetOrCreateHeightMap() : null;
        bool restoredWaterSlope = hm != null && terrainManager.RestoreTerrainForCell(gx, gy, hm);
        if (!restoredWaterSlope)
            RestoreTile(cell);
        if (roadManager != null)
            roadManager.UpdateAdjacentRoadPrefabsAt(new Vector2(gx, gy));
    }

    void HandleBuildingStatsReset(CityCell cellComponent, Zone.ZoneType zoneType)
    {
        string buildingType = cellComponent.GetBuildingType();

        if (buildingType == "PowerPlant")
        {
            PowerPlant powerPlant = cellComponent.powerPlant;
            cityStats.UnregisterPowerPlant(powerPlant);
        }

        if (buildingType == "WaterPlant")
        {
            WaterPlant waterPlant = cellComponent.waterPlant;
            if (waterManager != null && waterPlant != null)
            {
                waterManager.UnregisterWaterPlant(waterPlant);
            }
        }

        if (zoneType != Zone.ZoneType.Grass)
        {
            cityStats.HandleBuildingDemolition(zoneType, zoneManager.GetZoneAttributes(zoneType));
        }
    }

    void HandleBulldozeTile(Zone.ZoneType zoneType, GameObject cell, bool showAnimation = true)
    {
        CityCell cellComponent = cell.GetComponent<CityCell>();

        HandleBuildingStatsReset(cellComponent, zoneType);

        BulldozeBuildingTiles(cell, zoneType, showAnimation);
    }

    /// <summary>
    /// Demolish building/zoning at grid pos. Same stats + logic as manual bulldoze.
    /// Returns true if demolished. showAnimation=false (e.g. expropriation) → road cache invalidated.
    /// </summary>
    public bool DemolishCellAt(Vector2 gridPosition, bool showAnimation = true)
    {
        int gx = (int)gridPosition.x;
        int gy = (int)gridPosition.y;
        if (gx < 0 || gx >= width || gy < 0 || gy >= height) return false;
        GameObject cell = gridArray[gx, gy];
        CityCell cellComponent = cellArray[gx, gy];
        if (cellComponent == null || !CanBulldoze(cellComponent)) return false;

        Zone.ZoneType zoneType = cellComponent.zoneType;
        if (zoneType == Zone.ZoneType.Road)
        {
            cellComponent.ClearRoadRouteHints();
            RemoveRoadFromCache(new Vector2Int(gx, gy));
        }
        HandleBulldozeTile(zoneType, cell, showAnimation);
        return true;
    }

    bool CanBulldoze(CityCell cell)
    {
        if (cell == null)
        {
            return false;
        }

        if (cell.isInterstate)
        {
            if (GameNotificationManager != null)
                GameNotificationManager.PostWarning("The Interstate Highway cannot be demolished.");
            return false;
        }

        if (cell.forestType != Forest.ForestType.None)
        {
            return true;
        }

        switch (cell.zoneType)
        {
            case Zone.ZoneType.Road:
            case Zone.ZoneType.Building:
            case Zone.ZoneType.ResidentialLightBuilding:
            case Zone.ZoneType.ResidentialMediumBuilding:
            case Zone.ZoneType.ResidentialHeavyBuilding:
            case Zone.ZoneType.CommercialLightBuilding:
            case Zone.ZoneType.CommercialMediumBuilding:
            case Zone.ZoneType.CommercialHeavyBuilding:
            case Zone.ZoneType.IndustrialLightBuilding:
            case Zone.ZoneType.IndustrialMediumBuilding:
            case Zone.ZoneType.IndustrialHeavyBuilding:
            case Zone.ZoneType.ResidentialLightZoning:
            case Zone.ZoneType.ResidentialMediumZoning:
            case Zone.ZoneType.ResidentialHeavyZoning:
            case Zone.ZoneType.CommercialLightZoning:
            case Zone.ZoneType.CommercialMediumZoning:
            case Zone.ZoneType.CommercialHeavyZoning:
            case Zone.ZoneType.IndustrialLightZoning:
            case Zone.ZoneType.IndustrialMediumZoning:
            case Zone.ZoneType.IndustrialHeavyZoning:
                return true;
            default:
                return false;
        }
    }

    void HandleShowTileDetails(Vector2 gridPosition)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (cellArray == null) return;
            int gx = (int)gridPosition.x;
            int gy = (int)gridPosition.y;
            if (gx < 0 || gy < 0 || gx >= cellArray.GetLength(0) || gy >= cellArray.GetLength(1))
                return;
            CityCell cellComponent = cellArray[gx, gy];
            uiManager.ShowTileDetails(cellComponent);
        }
    }

    void HandleRaycast(Vector2 gridPosition)
    {
        Zone.ZoneType selectedZoneType = uiManager.GetSelectedZoneType();
        IBuilding selectedBuilding = uiManager.GetSelectedBuilding();
        IForest selectedForest = uiManager.GetSelectedForest();

        if (selectedZoneType == Zone.ZoneType.Road)
        {
            roadManager.HandleRoadDrawing(gridPosition);
        }
        else if (selectedZoneType == Zone.ZoneType.Water)
        {
            HandleWaterPlacement(gridPosition);
        }
        else if (selectedBuilding != null)
        {
            HandleBuildingPlacement(gridPosition, selectedBuilding);
        }
        else if (selectedForest != null)
        {
            HandleForestPlacement(gridPosition, selectedForest);
        }
        else if (IsStateServicePlacementMode())
        {
            HandleStateServicePlacement(gridPosition);
        }
        else if (isInZoningMode())
        {
            zoneManager.HandleZoning(mouseGridPosition);
        }
    }

    void HandleWaterPlacement(Vector2 gridPosition)
    {
        if (Input.GetMouseButton(0))
        {
            // Check if player can afford water placement
            if (!cityStats.CanAfford(ZoneAttributes.Water.ConstructionCost))
            {
                uiManager.ShowInsufficientFundsTooltip("Water", ZoneAttributes.Water.ConstructionCost);
                return;
            }

            if (waterManager != null)
            {
                waterManager.PlaceWater((int)gridPosition.x, (int)gridPosition.y);

                // Deduct cost for water placement
                cityStats.RemoveMoney(ZoneAttributes.Water.ConstructionCost);
            }
        }
    }

    private bool IsStateServicePlacementMode()
    {
        return ZoneManager.IsStateServiceZoneType(uiManager.GetSelectedZoneType());
    }

    /// <summary>
    /// Single-click Zone S placement routed through <see cref="Territory.Economy.ZoneSService"/>.
    /// Invalid sub-type id (not yet picked) reopens picker.
    /// </summary>
    private void HandleStateServicePlacement(Vector2 gridPosition)
    {
        if (!Input.GetMouseButtonDown(0)) return;

        int subTypeId = uiManager.CurrentSubTypeId;
        if (subTypeId < 0)
        {
            uiManager.OpenSubTypePicker();
            return;
        }

        var service = uiManager.ZoneSService;
        if (service == null) return;

        service.PlaceStateServiceZone((int)gridPosition.x, (int)gridPosition.y, subTypeId);
    }

    private bool isInZoningMode()
    {
        Zone.ZoneType sel = uiManager.GetSelectedZoneType();
        if (ZoneManager.IsStateServiceZoneType(sel)) return false;
        return sel != Zone.ZoneType.Grass &&
          sel != Zone.ZoneType.Road &&
          sel != Zone.ZoneType.None;
    }

    void HandleBuildingPlacement(Vector3 gridPosition, IBuilding selectedBuilding)
    {
        if (Input.GetMouseButtonDown(0))
        {
            placementService.PlaceBuilding(gridPosition, selectedBuilding);
        }
    }

    void HandleForestPlacement(Vector2 gridPosition, IForest selectedForest)
    {
        if (Input.GetMouseButtonDown(0))
        {
            forestManager.PlaceForest(gridPosition, selectedForest);
        }
    }

    /// <summary>
    /// User-facing demand feedback string (e.g. "✓ Demand: 80%") for zone type. Considers employment + residential support.
    /// </summary>
    /// <param name="zoneType">Zone type to evaluate demand for.</param>
    /// <returns>Formatted demand string, or empty if no demand manager.</returns>
    public string GetDemandFeedback(Zone.ZoneType zoneType)
    {
        if (demandManager == null)
            return "";

        float demandLevel = demandManager.GetDemandLevel(zoneType);
        bool canGrow = demandManager.CanZoneTypeGrow(zoneType);

        // Check if it's a residential building type
        Zone.ZoneType buildingType = zoneManager.GetBuildingZoneType(zoneType);
        bool isResidential = zoneManager.IsResidentialBuilding(buildingType);
        bool hasJobsAvailable = !isResidential || demandManager.CanPlaceResidentialBuilding();

        // Check if it's a commercial/industrial building type
        bool needsResidential = zoneManager.IsCommercialOrIndustrialBuilding(buildingType);
        bool hasResidentialSupport = !needsResidential || demandManager.CanPlaceCommercialOrIndustrialBuilding(buildingType);

        string feedback = "";

        if (canGrow && hasJobsAvailable && hasResidentialSupport)
        {
            feedback = $"✓ Demand: {demandLevel:F0}%";
        }
        else if (!canGrow)
        {
            feedback = $"✗ Low Demand: {demandLevel:F0}%";
        }
        else if (!hasJobsAvailable)
        {
            feedback = $"✗ No Jobs Available (Demand: {demandLevel:F0}%)";
        }
        else if (!hasResidentialSupport)
        {
            feedback = $"✗ Need Residents First (Demand: {demandLevel:F0}%)";
        }

        return feedback;
    }
    #endregion

    #region CityCell Destruction and Attributes
    /// <summary>
    /// Destroy all non-terrain children of cell at grid pos. Convenience overload, excludes nothing.
    /// </summary>
    /// <param name="cell">CityCell GameObject whose children destroyed.</param>
    /// <param name="gridPosition">Grid coords (for zone list bookkeeping).</param>
    public void DestroyCellChildren(GameObject cell, Vector2 gridPosition)
    {
        DestroyCellChildren(cell, gridPosition, null, false);
    }

    /// <summary>
    /// Destroy all children of cell except optional exclude (e.g. building being placed).
    /// Skip terrain (flat grass) + slope children (land/water slope), unless
    /// <paramref name="destroyFlatGrass"/> true (building/utility placement → single visual layer).
    /// Used by DemolishCellAt (incl. expropriation showAnimation: false): any ZoneCategory.Zoning child
    /// removed from zone manager lists (removeZonedPositionFromList) before destroy.
    /// Brown cliff + water–water cascade stacks skipped (see <see cref="TerrainManager.IsCliffStackTerrainObject"/>)
    /// → building footprint cleanup does not strip map-border cliff stacks. Bulldoze paths still refresh cliffs via
    /// <see cref="TerrainManager.RestoreTerrainForCell"/>.
    /// </summary>
    public void DestroyCellChildren(GameObject cell, Vector2 gridPosition, GameObject excludeFromDestroy)
    {
        DestroyCellChildren(cell, gridPosition, excludeFromDestroy, false);
    }

    /// <param name="destroyFlatGrass">true → flat grass zone tiles destroyed too (building placement + load restore).</param>
    public void DestroyCellChildren(GameObject cell, Vector2 gridPosition, GameObject excludeFromDestroy, bool destroyFlatGrass)
    {
        if (cell.transform.childCount == 0) return;

        var toDestroy = new List<GameObject>();
        foreach (Transform child in cell.transform)
        {
            if (excludeFromDestroy != null && child.gameObject == excludeFromDestroy)
                continue;

            // Do not destroy flat terrain (grass) or slope tiles (land or water), unless replacing with a building/road overlay
            Zone zone = child.GetComponent<Zone>();
            if (!destroyFlatGrass && zone != null && zone.zoneType == Zone.ZoneType.Grass)
                continue;
            if (terrainManager != null && (terrainManager.IsWaterSlopeObject(child.gameObject) || terrainManager.IsLandSlopeObject(child.gameObject)))
                continue;
            if (terrainManager != null && terrainManager.IsCliffStackTerrainObject(child.gameObject))
                continue;

            if (zone != null && zone.zoneCategory == Zone.ZoneCategory.Zoning)
            {
                zoneManager.removeZonedPositionFromList(gridPosition, zone.zoneType);
            }

            toDestroy.Add(child.gameObject);
        }
        foreach (GameObject go in toDestroy)
            Destroy(go);
    }

    /// <summary>
    /// Same as <see cref="DestroyCellChildren"/> but preserves cell's forest object → zoning can merge with forest.
    /// Also preserves land/water slope tiles + cliff stack instances (same rules as <see cref="DestroyCellChildren"/>)
    /// → map-border cliffs not stripped when placing/restoring zoning overlays.
    /// </summary>
    public void DestroyCellChildrenExceptForest(GameObject cell, Vector2 gridPosition)
    {
        if (cell.transform.childCount == 0) return;

        CityCell cellComponent = cellArray[(int)gridPosition.x, (int)gridPosition.y];
        GameObject forestObject = (cellComponent != null && cellComponent.HasForest()) ? cellComponent.forestObject : null;

        List<Transform> toDestroy = new List<Transform>();
        foreach (Transform child in cell.transform)
        {
            if (forestObject != null && child.gameObject == forestObject)
                continue;
            if (terrainManager != null && (terrainManager.IsWaterSlopeObject(child.gameObject) || terrainManager.IsLandSlopeObject(child.gameObject)))
                continue;
            if (terrainManager != null && terrainManager.IsCliffStackTerrainObject(child.gameObject))
                continue;
            toDestroy.Add(child);
        }

        foreach (Transform child in toDestroy)
        {
            Zone zone = child.GetComponent<Zone>();
            if (zone != null && zone.zoneCategory == Zone.ZoneCategory.Zoning)
                zoneManager.removeZonedPositionFromList(gridPosition, zone.zoneType);
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Overwrite cell zone type, population, power consumption, happiness, prefab, building size.
    /// </summary>
    /// <param name="cellComponent">CityCell to update.</param>
    /// <param name="selectedZoneType">New zone type.</param>
    /// <param name="zoneAttributes">Attrs (population, power, happiness) to apply.</param>
    /// <param name="prefab">Prefab GameObject for this zone.</param>
    /// <param name="buildingSize">Footprint size (1 = single-cell).</param>
    public void UpdateCellAttributes(CityCell cellComponent, Zone.ZoneType selectedZoneType, ZoneAttributes zoneAttributes, GameObject prefab, int buildingSize)
    {
        cellComponent.zoneType = selectedZoneType;
        cellComponent.population = zoneAttributes.Population;
        cellComponent.powerConsumption = zoneAttributes.PowerConsumption;
        cellComponent.waterConsumption = zoneAttributes.WaterConsumption;
        cellComponent.happiness = zoneAttributes.Happiness;
        cellComponent.prefab = prefab;
        cellComponent.prefabName = prefab.name;
        cellComponent.buildingType = prefab.name;
        cellComponent.buildingSize = buildingSize;
        cellComponent.isPivot = false;
    }

    private Vector2 FindCenterPosition(Vector2[] section)
    {
        float x = 0;
        float y = 0;

        foreach (Vector2 position in section)
        {
            x += position.x;
            y += position.y;
        }

        x /= section.Length;
        y /= section.Length;

        return new Vector2(x, y);
    }
    #endregion

    #region Sorting Order
    /// <summary>
    /// Set tile sorting order via legacy grid-pos formula. New code → prefer TerrainManager methods.
    /// </summary>
    /// <param name="tile">Tile GameObject to update.</param>
    /// <param name="zoneType">Zone type → sorting layer offset.</param>
    /// <returns>Computed sorting order, or -1001 if tile outside grid.</returns>
    public int SetTileSortingOrder(GameObject tile, Zone.ZoneType zoneType = Zone.ZoneType.Grass)
        => sortingService.SetTileSortingOrder(tile, zoneType);

    /// <summary>
    /// Set sorting order for zoning tile (RCI overlay) via TerrainManager → renders below forest + buildings.
    /// </summary>
    public void SetZoningTileSortingOrder(GameObject tile, int x, int y)
        => sortingService.SetZoningTileSortingOrder(tile, x, y);

    /// <summary>
    /// Set sorting order for zone building (RCI) tile via TerrainManager → renders above forest + terrain.
    /// </summary>
    public void SetZoneBuildingSortingOrder(GameObject tile, int x, int y)
        => sortingService.SetZoneBuildingSortingOrder(tile, x, y);

    /// <summary>
    /// Set sorting order for multi-cell building = max over footprint → whole building renders in front of all covered terrain.
    /// </summary>
    public void SetZoneBuildingSortingOrder(GameObject tile, int pivotX, int pivotY, int buildingSize)
        => sortingService.SetZoneBuildingSortingOrder(tile, pivotX, pivotY, buildingSize);

    /// <summary>
    /// Sorting order for road tile at (x, y) at given height (e.g. 1 for bridge over water).
    /// </summary>
    public int GetRoadSortingOrderForCell(int x, int y, int height)
        => sortingService.GetRoadSortingOrderForCell(x, y, height);

    /// <summary>
    /// Set sorting order for road tile via TerrainManager → renders below forest + buildings.
    /// </summary>
    public void SetRoadSortingOrder(GameObject tile, int x, int y)
        => sortingService.SetRoadSortingOrder(tile, x, y);

    /// <summary>
    /// Set sorting order for sea-level tile → renders behind all land content.
    /// </summary>
    /// <param name="tile">Tile GameObject to update.</param>
    /// <param name="cell">CityCell this tile belongs to.</param>
    /// <returns>Computed sea-level sorting order.</returns>
    public int SetResortSeaLevelOrder(GameObject tile, CityCell cell)
        => sortingService.SetResortSeaLevelOrder(tile, cell);
    #endregion

    #region Coordinate Conversion
    /// <summary>
    /// Map screen pos → world X/Y on Z=0 plane where cell roots placed in grid creation.
    /// <c>Camera.ScreenToWorldPoint(Input.mousePosition)</c> leaves mouse z=0 → Unity treats as wrong depth + skews X/Y for orthographic picking.
    /// </summary>
    public static Vector2 ScreenPointToWorldOnGridPlane(Camera cam, Vector3 screenPosition)
    {
        if (cam == null)
            return Vector2.zero;

        Ray ray = cam.ScreenPointToRay(screenPosition);
        const float planeZ = 0f;
        float dz = ray.direction.z;
        if (Mathf.Abs(dz) > 1e-5f)
        {
            float t = (planeZ - ray.origin.z) / dz;
            if (t > -1e-3f)
            {
                Vector3 hit = ray.origin + ray.direction * t;
                return new Vector2(hit.x, hit.y);
            }
        }

        float depth = Mathf.Abs(cam.transform.position.z - planeZ);
        Vector3 p = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        return new Vector2(p.x, p.y);
    }

    /// <summary>
    /// World-space point → isometric grid coords (ignores height).
    /// </summary>
    /// <param name="worldPoint">World-space position.</param>
    /// <returns>Grid coords Vector2 (x, y).</returns>
    public Vector2 GetGridPosition(Vector2 worldPoint)
    {
        Vector2Int g = IsometricGridMath.WorldToGridPlanar(worldPoint, tileWidth, tileHeight);
        return new Vector2(g.x, g.y);
    }

    /// <summary>
    /// Terrain height at grid cell for mouse projection refinement (default 1 if out of bounds).
    /// </summary>
    private int GetTerrainHeightForGridCell(int gridX, int gridY)
    {
        CityCell c = GetCell(gridX, gridY);
        return c != null ? c.GetCellInstanceHeight() : 1;
    }

    /// <summary>
    /// Max instance height in 3×3 Moore neighborhood. Stabilizes <see cref="RefineGridPositionForTerrainHeight"/> at cliff/water edges
    /// where single-cell height sample alternates between high land + low shore.
    /// </summary>
    int GetMaxTerrainHeightInNeighborhood3x3(int centerX, int centerY)
    {
        int maxH = 1;
        int[] mx = { 0, 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] my = { 0, 0, 0, 1, -1, 1, -1, 1, -1 };
        for (int i = 0; i < 9; i++)
        {
            int x = centerX + mx[i];
            int y = centerY + my[i];
            if (x < 0 || x >= width || y < 0 || y >= height)
                continue;
            maxH = Mathf.Max(maxH, GetTerrainHeightForGridCell(x, y));
        }
        return maxH;
    }

    /// <summary>
    /// Refine <see cref="GetGridPosition"/>: strip vertical offset from <see cref="GetWorldPositionVector"/>
    /// for current terrain height estimate, iterate until stable. Fixes diagonal bias at elevated cells.
    /// Uses max height in 3×3 neighborhood → iteration does not bounce between mismatched shore/land heights.
    /// </summary>
    private Vector2 RefineGridPositionForTerrainHeight(Vector2 worldPoint)
    {
        if (width <= 0 || height <= 0)
            return GetGridPosition(worldPoint);

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
            if (Mathf.Approximately(next.x, grid.x) && Mathf.Approximately(next.y, grid.y))
                return next;
            grid = next;
        }
        return grid;
    }

    /// <summary>
    /// Screen-space bounds of cell's base tile (first child with Zone Grass/Road/Zoning + SpriteRenderer).
    /// If none, uses first child with SpriteRenderer (e.g. slope prefab) → hit-test works at all heights.
    /// Fallback: rect from cell.transformPosition + tile size when no such child.
    /// </summary>
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
                if (anyRenderer == null)
                    anyRenderer = sr;
                if (zone != null && (zone.zoneType == Zone.ZoneType.Grass || zone.zoneType == Zone.ZoneType.Road || zone.zoneCategory == Zone.ZoneCategory.Zoning))
                {
                    tileRenderer = sr;
                    break;
                }
            }
        }

        SpriteRenderer boundsSource = tileRenderer != null ? tileRenderer : anyRenderer;
        Bounds worldBounds;
        if (boundsSource != null)
        {
            worldBounds = boundsSource.bounds;
        }
        else
        {
            Vector2 center = cell.transformPosition;
            worldBounds = new Bounds(center, new Vector3(tileWidth, tileHeight, 0f));
        }

        Vector3 worldMin = worldBounds.min;
        Vector3 worldMax = worldBounds.max;
        float zRef = worldBounds.center.z;
        Vector3 p0 = cam.WorldToScreenPoint(new Vector3(worldMin.x, worldMin.y, zRef));
        Vector3 p1 = cam.WorldToScreenPoint(new Vector3(worldMax.x, worldMin.y, zRef));
        Vector3 p2 = cam.WorldToScreenPoint(new Vector3(worldMin.x, worldMax.y, zRef));
        Vector3 p3 = cam.WorldToScreenPoint(new Vector3(worldMax.x, worldMax.y, zRef));

        float minX = Mathf.Min(p0.x, p1.x, p2.x, p3.x);
        float maxX = Mathf.Max(p0.x, p1.x, p2.x, p3.x);
        float minY = Mathf.Min(p0.y, p1.y, p2.y, p3.y);
        float maxY = Mathf.Max(p0.y, p1.y, p2.y, p3.y);

        // Inset rect toward center so hit areas don't overlap with neighbors (isometric AABB overlap).
        // 0.6 reduces gaps vs 0.75 while still avoiding excessive overlap in cut-through scenarios.
        const float insetFactor = 0.6f;
        float width = maxX - minX;
        float height = maxY - minY;
        float insetW = width * (1f - insetFactor) * 0.5f;
        float insetH = height * (1f - insetFactor) * 0.5f;
        minX += insetW;
        maxX -= insetW;
        minY += insetH;
        maxY -= insetH;

        screenRect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    /// <summary>
    /// Resolve world-space point → best-matching cell via screen-space hit testing against neighbors.
    /// Multiple cells' inset rects contain mouse (isometric overlap) → pick hit whose projected world center closest to mouse
    /// (same rule as <see cref="PickCellHitClosestOnScreen"/>). Neighborhood center uses <see cref="Mathf.RoundToInt"/> on <paramref name="gridPos"/> (not truncation) to match refinement.
    /// If rounded refined-center cell also hits + screen distance ties winner within tiny epsilon, that cell wins (numeric tie-break only).
    /// </summary>
    /// <param name="worldPoint">World-space position.</param>
    /// <param name="gridPos">Initial grid pos estimate (from GetGridPosition).</param>
    /// <returns>CityCell whose base tile contains mouse, or null if none.</returns>
    public CityCell GetCellFromWorldPoint(Vector2 worldPoint, Vector2 gridPos)
    {
        Camera cam = cachedCamera;
        if (cam == null) return null;

        int gridX = Mathf.Clamp(Mathf.RoundToInt(gridPos.x), 0, Mathf.Max(0, width - 1));
        int gridY = Mathf.Clamp(Mathf.RoundToInt(gridPos.y), 0, Mathf.Max(0, height - 1));

        // 9 candidate cells: center + 8 neighbors (3x3)
        List<CityCell> candidates = new List<CityCell>();
        int[] dx = { 0, 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] dy = { 0, 0, 0, 1, -1, 1, -1, 1, -1 };
        for (int i = 0; i < 9; i++)
        {
            CityCell c = GetCell(gridX + dx[i], gridY + dy[i]);
            if (c != null) candidates.Add(c);
        }

        Vector2 mouseScreen = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<CityCell> rectHits = new List<CityCell>();
        foreach (CityCell cell in candidates)
        {
            if (!TryGetCellBaseTileScreenBounds(cell, cam, out Rect screenRect))
                continue;
            if (screenRect.Contains(mouseScreen))
                rectHits.Add(cell);
        }

        CityCell chosen = null;
        if (rectHits.Count == 1)
        {
            chosen = rectHits[0];
        }
        else if (rectHits.Count > 1)
        {
            chosen = PickCellHitClosestOnScreen(mouseScreen, rectHits, cam);
            CityCell refineHit = null;
            for (int i = 0; i < rectHits.Count; i++)
            {
                CityCell c = rectHits[i];
                if (c != null && c.x == gridX && c.y == gridY)
                {
                    refineHit = c;
                    break;
                }
            }
            if (refineHit != null && chosen != null && refineHit != chosen)
            {
                float dPick = GetCellScreenDistanceSqToMouse(chosen, mouseScreen, cam);
                float dRef = GetCellScreenDistanceSqToMouse(refineHit, mouseScreen, cam);
                const float tieEpsSq = 0.04f;
                if (Mathf.Abs(dPick - dRef) <= tieEpsSq)
                    chosen = refineHit;
            }
        }
        else if (candidates.Count > 0)
        {
            chosen = GetClosestCellByScreenDistance(mouseScreen, candidates, cam);
        }

        return chosen;
    }

    /// <summary>
    /// Squared screen-space distance from <paramref name="mouseScreen"/> to cell world center projected through <paramref name="cam"/>.
    /// </summary>
    float GetCellScreenDistanceSqToMouse(CityCell cell, Vector2 mouseScreen, Camera cam)
    {
        if (cell == null || cam == null)
            return float.MaxValue;
        Vector2 worldCenter = GetCellWorldPosition(cell);
        Vector3 sc = cam.WorldToScreenPoint(new Vector3(worldCenter.x, worldCenter.y, cell.transform.position.z));
        float dx = mouseScreen.x - sc.x;
        float dy = mouseScreen.y - sc.y;
        return dx * dx + dy * dy;
    }

    /// <summary>
    /// Multiple cells' screen rects contain cursor (isometric overlap) → pick one whose elevated
    /// world center projects closest to mouse. Ties break by higher <see cref="CityCell.sortingOrder"/>.
    /// </summary>
    private CityCell PickCellHitClosestOnScreen(Vector2 mouseScreen, List<CityCell> hits, Camera cam)
    {
        if (hits == null || hits.Count == 0 || cam == null)
            return null;

        CityCell best = null;
        float bestDistSq = float.MaxValue;
        int bestOrder = int.MinValue;
        const float tiePixels = 4f;
        float tieEpsSq = tiePixels * tiePixels;

        foreach (CityCell cell in hits)
        {
            if (cell == null)
                continue;

            float distSq = GetCellScreenDistanceSqToMouse(cell, mouseScreen, cam);

            bool better = best == null;
            if (!better && distSq < bestDistSq - 0.001f)
                better = true;
            else if (!better && Mathf.Abs(distSq - bestDistSq) <= tieEpsSq && cell.sortingOrder > bestOrder)
                better = true;

            if (better)
            {
                best = cell;
                bestDistSq = distSq;
                bestOrder = cell.sortingOrder;
            }
        }

        return best;
    }

    /// <summary>
    /// CityCell whose screen-space center closest to mouse position.
    /// Fallback when no cell's screen rect contains mouse (e.g. gaps between insets).
    /// </summary>
    private CityCell GetClosestCellByScreenDistance(Vector2 mouseScreen, List<CityCell> candidates, Camera cam)
    {
        if (cam == null || candidates == null || candidates.Count == 0) return null;

        CityCell closest = null;
        float minDistSq = float.MaxValue;

        foreach (CityCell cell in candidates)
        {
            Vector2 worldCenter = GetCellWorldPosition(cell);
            Vector3 screenCenter = cam.WorldToScreenPoint(new Vector3(worldCenter.x, worldCenter.y, 0f));
            float dx = mouseScreen.x - screenCenter.x;
            float dy = mouseScreen.y - screenCenter.y;
            float distSq = dx * dx + dy * dy;
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                closest = cell;
            }
        }
        return closest;
    }

    /// <summary>
    /// Mouse world point → grid coords. Corrects for terrain height via screen-space hit testing.
    /// </summary>
    /// <param name="mouseWorldPoint">Mouse world-space position.</param>
    /// <returns>Height-aware grid coords.</returns>
    public Vector2 GetGridPositionWithHeight(Vector2 mouseWorldPoint)
    {
        Vector2 gridPos = RefineGridPositionForTerrainHeight(mouseWorldPoint);

        CityCell cell = GetCellFromWorldPoint(mouseWorldPoint, gridPos);
        if (cell == null)
        {
            return gridPos;
        }

        Vector2 gridPosWithHeight = new Vector2(cell.x, cell.y);

        return gridPosWithHeight;
    }

    /// <summary>
    /// CityCell under mouse via screen-space hit testing. Closest-cell fallback when no rect contains mouse.
    /// </summary>
    /// <param name="mouseWorldPoint">Mouse world-space position.</param>
    /// <returns>CityCell under mouse, or null if outside grid.</returns>
    public CityCell GetMouseGridCell(Vector2 mouseWorldPoint)
    {
        Vector2 gridPos = RefineGridPositionForTerrainHeight(mouseWorldPoint);
        return GetCellFromWorldPoint(mouseWorldPoint, gridPos);
    }
    /// <summary>
    /// Grid coords + height → world-space pos (height shifts tile upward).
    /// </summary>
    /// <param name="gridX">Grid X.</param>
    /// <param name="gridY">Grid Y.</param>
    /// <param name="height">Terrain height level (1 = base).</param>
    /// <returns>World-space Vector2.</returns>
    public Vector2 GetWorldPositionVector(int gridX, int gridY, int height)
    {
        return IsometricGridMath.GridToWorldPlanar(gridX, gridY, tileWidth, tileHeight, height);
    }

    private Vector2 GetWorldPositionVectorDown(int gridX, int gridY, int height)
    {
        float heightOffset = (height - 1) * (tileHeight / 2);
        float posX = (gridX - gridY) * (tileWidth / 2);
        float posY = (gridX + gridY) * (tileHeight / 2) - heightOffset;
        return new Vector2(posX, posY);
    }

    /// <summary>
    /// World-space pos for cell at (gridX, gridY), accounting for current terrain height.
    /// </summary>
    /// <param name="gridX">Grid X.</param>
    /// <param name="gridY">Grid Y.</param>
    /// <returns>World-space Vector2.</returns>
    public Vector2 GetWorldPosition(int gridX, int gridY)
    {
        CityCell cell = cellArray[gridX, gridY];
        int height = cell.GetCellInstanceHeight();
        return GetWorldPositionVector(gridX, gridY, height);
    }

    /// <summary>
    /// World-space pos of cell, accounting for terrain height.
    /// </summary>
    /// <param name="cell">CityCell to get pos for.</param>
    /// <returns>World-space Vector2.</returns>
    public Vector2 GetCellWorldPosition(CityCell cell)
    {
        int height = cell.GetCellInstanceHeight();
        return GetWorldPositionVector(cell.x, cell.y, height);
    }

    /// <summary>
    /// World pos where building placed (preview + actual placement).
    /// Size 1 → pivot cell. Size &gt; 1 → center of footprint so preview + placement match.
    /// </summary>
    public Vector2 GetBuildingPlacementWorldPosition(Vector2 gridPos, int buildingSize)
    {
        CityCell pivotCell = cellArray[(int)gridPos.x, (int)gridPos.y];
        if (buildingSize <= 1)
            return pivotCell.transformPosition;

        GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);
        Vector2 sum = Vector2.zero;
        int count = 0;
        for (int x = 0; x < buildingSize; x++)
        {
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
        }
        return count > 0 ? sum / count : pivotCell.transformPosition;
    }

    /// <summary>
    /// GameObject for grid cell at pos, or null if out of bounds.
    /// </summary>
    /// <param name="gridPos">Grid coords.</param>
    /// <returns>CityCell GameObject, or null.</returns>
    public GameObject GetGridCell(Vector2 gridPos)
    {
        if (gridPos.x < 0 || gridPos.x >= gridArray.GetLength(0) ||
            gridPos.y < 0 || gridPos.y >= gridArray.GetLength(1))
        {
            return null;
        }
        return gridArray[(int)gridPos.x, (int)gridPos.y];
    }

    /// <summary>
    /// Set terrain height of cell at grid pos.
    /// </summary>
    /// <param name="gridPos">Grid coords.</param>
    /// <param name="height">New height value.</param>
    /// <param name="skipWaterMembershipRefresh">true → skip <see cref="WaterManager.OnLandCellHeightCommitted"/> (e.g. <see cref="TerrainManager.UpdateTileElevation"/> finishes membership in finally block).</param>
    public void SetCellHeight(Vector2 gridPos, int height, bool skipWaterMembershipRefresh = false)
    {
        CityCell cell = cellArray[(int)gridPos.x, (int)gridPos.y];
        cell.SetCellInstanceHeight(height);
        if (skipWaterMembershipRefresh)
            return;
        if (waterManager == null)
            waterManager = FindObjectOfType<WaterManager>();
        if (waterManager != null)
            waterManager.OnLandCellHeightCommitted((int)gridPos.x, (int)gridPos.y);
    }

    void DestroyPreviousZoning(GameObject cell)
    {
        if (cell.transform.childCount > 0)
        {
            var toDestroy = new List<GameObject>();
            foreach (Transform child in cell.transform)
                toDestroy.Add(child.gameObject);
            foreach (GameObject go in toDestroy)
                Destroy(go);
        }
    }

    bool IsWithinGrid(Vector2 position)
    {
        foreach (GameObject cell in gridArray)
        {
            Vector3 position3D = new Vector3(position.x, position.y, 0);
            if (cell.transform.position == position3D)
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Building Validation and Placement
    /// <summary>
    /// Reason why building placement would fail at pos, or null if would succeed.
    /// Used for debug UI + specific error messages.
    /// </summary>
    public string GetBuildingPlacementFailReason(Vector2 gridPosition, int buildingSize, bool isWaterPlant)
        => placementService.GetBuildingPlacementFailReason(gridPosition, buildingSize, isWaterPlant);

    /// <summary>
    /// True if building of given size can be placed at grid pos. Infers water plant status from currently selected building.
    /// </summary>
    /// <param name="gridPosition">Pivot grid coords for building.</param>
    /// <param name="buildingSize">Footprint size.</param>
    /// <returns>True if placement valid.</returns>
    public bool canPlaceBuilding(Vector2 gridPosition, int buildingSize)
        => placementService.CanPlaceBuilding(gridPosition, buildingSize);

    /// <summary>
    /// True if building of given size can be placed at grid pos, with explicit water plant flag.
    /// </summary>
    /// <param name="gridPosition">Pivot grid coords.</param>
    /// <param name="buildingSize">Footprint size.</param>
    /// <param name="isWaterPlant">true = water plant (relaxed water adjacency rules).</param>
    /// <returns>True if placement valid.</returns>
    public bool canPlaceBuilding(Vector2 gridPosition, int buildingSize, bool isWaterPlant)
        => placementService.CanPlaceBuilding(gridPosition, buildingSize, isWaterPlant);

    void UpdatePlacedBuildingCellAttributes(CityCell cell, int buildingSize, PowerPlant powerPlant, WaterPlant waterPlant, GameObject buildingPrefab, Zone.ZoneType zoneType = Zone.ZoneType.Building, GameObject building = null)
    {
        cell.occupiedBuilding = building;
        cell.buildingSize = buildingSize;
        cell.prefab = buildingPrefab;
        cell.prefabName = buildingPrefab.name;
        cell.zoneType = zoneType;

        if (powerPlant != null)
        {
            cell.buildingType = "PowerPlant";
            cell.powerPlant = powerPlant;
        }

        if (waterPlant != null)
        {
            cell.buildingType = "WaterPlant";
            cell.waterPlant = waterPlant;
        }
    }

    /// <summary>
    /// Place building programmatically (e.g. auto resource planner). Caller owns budget + affordability.
    /// Does not deduct money. Returns true if placed.
    /// </summary>
    public bool PlaceBuildingProgrammatic(Vector2 gridPos, IBuilding buildingTemplate)
        => placementService.PlaceBuildingProgrammatic(gridPos, buildingTemplate);
    #endregion

    #region Save and Restore
    /// <summary>
    /// Set sorting order on grass tile via grid coords directly. Avoids GetGridPosition
    /// → cannot account for height offset + would parent tile to wrong cell at h &gt; 1.
    /// </summary>
    void SetGrassSortingOrderDirect(GameObject tile, int x, int y, int height, CityCell cell)
    {
        if (terrainManager != null)
        {
            int sortingOrder = terrainManager.CalculateTerrainSortingOrder(x, y, height);
            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingOrder = sortingOrder;
            cell.SetCellInstanceSortingOrder(sortingOrder);
        }
    }

    static void ApplySavedSpriteSorting(GameObject obj, int sortingOrder)
    {
        if (obj == null) return;
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sortingOrder = sortingOrder;
    }

    /// <summary>Lake/coast shore cells saved with Bay, water-slope prefabs, or secondary shore prefab.</summary>
    static bool IsWaterShoreSavedCell(CellData cellData)
    {
        if (string.IsNullOrEmpty(cellData.prefabName))
            return false;
        return cellData.prefabName.Contains("Bay")
            || cellData.prefabName.Contains("SlopeWater")
            || cellData.prefabName.Contains("UpslopeWater")
            || !string.IsNullOrEmpty(cellData.secondaryPrefabName);
    }

    /// <summary>
    /// Restore order: water → terrain (grass/shore/slope) → zoning overlays → roads → building pivots/singles → multi-cell footprint non-pivots.
    /// </summary>
    int GetCellDataRestoreVisualPhase(CellData cellData)
    {
        if (zoneManager == null) return 1;
        Zone.ZoneType zoneType = zoneManager.GetZoneTypeFromZoneTypeString(cellData.zoneType);

        if (zoneType == Zone.ZoneType.Water) return 0;
        if (zoneType == Zone.ZoneType.Grass) return 1;
        if (ZoneManager.IsZoningType(zoneType)) return 2;
        if (zoneType == Zone.ZoneType.Road) return 3;
        if (IsZoneTypeBuilding(zoneType))
        {
            if (cellData.buildingSize > 1 && !cellData.isPivot) return 5;
            return 4;
        }
        return 1;
    }

    /// <summary>Stable sort of cell data for restore-visual pass → terrain before roads/buildings, pivot before footprint.</summary>
    List<CellData> SortCellDataForVisualRestore(List<CellData> gridData)
    {
        var list = new List<CellData>(gridData.Count);
        list.AddRange(gridData);
        list.Sort((a, b) =>
        {
            int pa = GetCellDataRestoreVisualPhase(a);
            int pb = GetCellDataRestoreVisualPhase(b);
            if (pa != pb) return pa.CompareTo(pb);
            if (a.y != b.y) return a.y.CompareTo(b.y);
            return a.x.CompareTo(b.x);
        });
        return list;
    }

    /// <summary>Find building root on pivot cell after load (occupiedBuilding or children).</summary>
    GameObject FindBuildingRootOnPivotCell(CityCell cell, GameObject gridCell)
    {
        if (cell == null || gridCell == null) return null;
        if (cell.occupiedBuilding != null) return cell.occupiedBuilding;
        PowerPlant pp = gridCell.GetComponentInChildren<PowerPlant>(true);
        if (pp != null) return pp.gameObject;
        WaterPlant wp = gridCell.GetComponentInChildren<WaterPlant>(true);
        if (wp != null) return wp.gameObject;
        foreach (Transform child in gridCell.transform)
        {
            Zone z = child.GetComponent<Zone>();
            if (z != null && z.zoneCategory == Zone.ZoneCategory.Building)
                return child.gameObject;
        }
        return null;
    }

    /// <summary>
    /// Re-apply sorting for all pivot buildings → neighbor caps match New Game after full terrain/water/roads restore.
    /// </summary>
    /// <returns>Number of buildings touched.</returns>
    int RecalculateBuildingSortingAfterLoad(out int sortOrderChangeCount)
    {
        sortOrderChangeCount = 0;
        if (cellArray == null || sortingService == null || gridArray == null) return 0;

        int touched = 0;

        for (int x = 0; x < width; x++)
        {
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
                if (cell.sortingOrder != orderBefore)
                    sortOrderChangeCount++;
            }
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
            for (int i = cell.transform.childCount - 1; i >= 0; i--)
                Destroy(cell.transform.GetChild(i).gameObject);

            GameObject waterPrefab = waterManager.FindWaterPrefabByName(cellData.prefabName);
            if (waterPrefab == null)
                return;

            int surfaceHeight = waterManager.GetWaterSurfaceHeight(cellData.x, cellData.y);
            if (surfaceHeight < 0)
                surfaceHeight = waterManager.seaLevel;

            int visualSurfaceHeight = Mathf.Max(TerrainManager.MIN_HEIGHT, surfaceHeight - 1);
            float halfCellHeight = tileHeight * 0.25f;
            Vector2 waterSurfaceWorld = GetWorldPositionVector(cellData.x, cellData.y, visualSurfaceHeight);
            Vector2 waterTileWorldPos = waterSurfaceWorld + new Vector2(0f, halfCellHeight);

            GameObject waterTile = Instantiate(waterPrefab, waterTileWorldPos, Quaternion.identity);
            Zone zone = waterTile.AddComponent<Zone>();
            zone.zoneType = Zone.ZoneType.Water;
            zone.zoneCategory = Zone.ZoneCategory.Water;
            waterTile.transform.SetParent(cell.transform);
            int waterSort = terrainManager != null
                ? terrainManager.CalculateTerrainSortingOrder(cellData.x, cellData.y, visualSurfaceHeight)
                : cellData.sortingOrder;
            ApplySavedSpriteSorting(waterTile, waterSort);
            cellComponent.SetCellInstanceSortingOrder(waterSort);
            return;
        }

        if (zoneType == Zone.ZoneType.Grass)
        {
            if (IsWaterShoreSavedCell(cellData) && terrainManager != null)
            {
                terrainManager.RestoreWaterShorePrefabsFromSave(
                    cellData.x, cellData.y,
                    cellData.prefabName,
                    cellData.secondaryPrefabName ?? "",
                    cellData.sortingOrder);
                if (forestManager != null)
                {
                    Forest.ForestType parsedForestType = Forest.ForestType.None;
                    if (!string.IsNullOrEmpty(cellData.forestType))
                        System.Enum.TryParse(cellData.forestType, out parsedForestType);
                    int forestOrder = cellArray[cellData.x, cellData.y].sortingOrder + 5;
                    forestManager.RestoreForestAt(cellData.x, cellData.y, parsedForestType, cellData.forestPrefabName, updateStats: false, savedSpriteSortingOrder: forestOrder);
                }
                return;
            }

            // Land slope cells (saved prefab name contains "Slope", not water-shore)
            if (!string.IsNullOrEmpty(cellData.prefabName) && cellData.prefabName.Contains("Slope"))
            {
                GameObject slopePrefab = terrainManager != null ? terrainManager.FindTerrainPrefabByName(cellData.prefabName) : null;
                if (slopePrefab != null && terrainManager != null)
                {
                    terrainManager.PlaceSlopeFromPrefab(cellData.x, cellData.y, slopePrefab, cellData.height);
                    foreach (Transform child in cell.transform)
                    {
                        ApplySavedSpriteSorting(child.gameObject, cellData.sortingOrder);
                        break;
                    }
                    cellArray[cellData.x, cellData.y].SetCellInstanceSortingOrder(cellData.sortingOrder);
                }
                else
                {
                    GameObject fallbackGrass = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, 1)
                        ?? (zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0 ? zoneManager.grassPrefabs[0] : null);
                    if (fallbackGrass != null)
                    {
                        for (int i = cell.transform.childCount - 1; i >= 0; i--)
                            Destroy(cell.transform.GetChild(i).gameObject);
                        CityCell cc = cellArray[cellData.x, cellData.y];
                        GameObject zoneTile = Instantiate(fallbackGrass, cc.transformPosition, Quaternion.identity);
                        zoneTile.transform.SetParent(cell.transform);
                        ApplySavedSpriteSorting(zoneTile, cellData.sortingOrder);
                        cc.SetCellInstanceSortingOrder(cellData.sortingOrder);
                        Zone z = zoneTile.GetComponent<Zone>();
                        if (z == null) z = zoneTile.AddComponent<Zone>();
                        z.zoneType = Zone.ZoneType.Grass;
                    }
                }
                if (forestManager != null)
                {
                    Forest.ForestType parsedForestType = Forest.ForestType.None;
                    if (!string.IsNullOrEmpty(cellData.forestType))
                        System.Enum.TryParse(cellData.forestType, out parsedForestType);
                    int forestOrder = cellData.sortingOrder + 5;
                    forestManager.RestoreForestAt(cellData.x, cellData.y, parsedForestType, cellData.forestPrefabName, updateStats: false, savedSpriteSortingOrder: forestOrder);
                }
                return;
            }

            // Replace CreateGrid grass with saved prefab (or place if missing).
            // Never destroy without placing: only destroy when we have a valid prefab to place.
            GameObject grassPrefab = terrainManager != null ? terrainManager.FindTerrainPrefabByName(cellData.prefabName) : null;
            if (grassPrefab == null)
                grassPrefab = zoneManager.FindPrefabByName(cellData.prefabName);
            if (grassPrefab == null)
                grassPrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, 1);
            if (grassPrefab == null)
                grassPrefab = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, Mathf.Clamp(cellData.height, 1, 5));
            if (grassPrefab == null && zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0)
                grassPrefab = zoneManager.grassPrefabs[0];

            if (grassPrefab != null)
            {
                // Destroy existing base tile only when we have a valid prefab to place
                for (int i = cell.transform.childCount - 1; i >= 0; i--)
                    Destroy(cell.transform.GetChild(i).gameObject);

                CityCell cellComponent = cellArray[cellData.x, cellData.y];
                GameObject zoneTile = Instantiate(grassPrefab, cellComponent.transformPosition, Quaternion.identity);
                zoneTile.transform.SetParent(cell.transform);
                ApplySavedSpriteSorting(zoneTile, cellData.sortingOrder);
                cellComponent.SetCellInstanceSortingOrder(cellData.sortingOrder);

                Zone zoneComponent = zoneTile.GetComponent<Zone>();
                if (zoneComponent == null)
                {
                    zoneComponent = zoneTile.AddComponent<Zone>();
                    zoneComponent.zoneType = Zone.ZoneType.Grass;
                }
            }
            if (forestManager != null)
            {
                Forest.ForestType parsedForestType = Forest.ForestType.None;
                if (!string.IsNullOrEmpty(cellData.forestType))
                    System.Enum.TryParse(cellData.forestType, out parsedForestType);
                int forestOrder = cellData.sortingOrder + 5;
                forestManager.RestoreForestAt(cellData.x, cellData.y, parsedForestType, cellData.forestPrefabName, updateStats: false, savedSpriteSortingOrder: forestOrder);
            }
            return;
        }

        GameObject tilePrefab = zoneManager.FindPrefabByName(cellData.prefabName);
        if (tilePrefab == null && terrainManager != null)
            tilePrefab = terrainManager.FindTerrainPrefabByName(cellData.prefabName);

        // Fallback for Road: use default road prefab when saved prefab not found (avoids black squares)
        if (tilePrefab == null && zoneType == Zone.ZoneType.Road && roadManager != null)
        {
            var roadPrefabs = roadManager.GetRoadPrefabs();
            if (roadPrefabs != null && roadPrefabs.Count > 0)
                tilePrefab = roadPrefabs[0];
        }

        if (tilePrefab != null)
        {
            bool isMultiCellUtilityBuilding = cellData.buildingSize > 1 && cellData.isPivot
                && (tilePrefab.GetComponent<PowerPlant>() != null || tilePrefab.GetComponent<WaterPlant>() != null);

            if (isMultiCellUtilityBuilding)
            {
                placementService.RestoreBuildingTile(tilePrefab, new Vector2(cellData.x, cellData.y), cellData.buildingSize);
            }
            else if (zoneType == Zone.ZoneType.Road && roadManager != null)
            {
                roadManager.RestoreRoadTile(new Vector2Int(cellData.x, cellData.y), tilePrefab, cellData.isInterstate, cellData.sortingOrder);
            }
            else if (ZoneManager.IsZoningType(zoneType))
            {
                zoneManager.RestoreZoneTile(tilePrefab, cell, zoneType);
            }
            else if (!(cellData.buildingSize > 1 && !cellData.isPivot))
            {
                zoneManager.PlaceZoneBuildingTile(tilePrefab, cell, cellData.buildingSize);
                CityCell cellComponent = cellArray[cellData.x, cellData.y];
                PowerPlant powerPlant = cell.GetComponentInChildren<PowerPlant>();
                WaterPlant waterPlant = cell.GetComponentInChildren<WaterPlant>();
                UpdatePlacedBuildingCellAttributes(cellComponent, cellData.buildingSize, powerPlant, waterPlant, tilePrefab, zoneType, null);
                if (powerPlant != null)
                    cityStats.RegisterPowerPlant(powerPlant);
                if (waterPlant != null && waterManager != null)
                {
                    waterManager.RegisterWaterPlant(waterPlant);
                    cityStats.cityWaterOutput = waterManager.GetTotalWaterOutput();
                }
            }
        }

        zoneManager.addZonedTileToList(new Vector2(cellData.x, cellData.y), zoneType);

        // Restore forest state
        if (forestManager != null)
        {
            Forest.ForestType parsedForestType = Forest.ForestType.None;
            if (!string.IsNullOrEmpty(cellData.forestType))
                System.Enum.TryParse(cellData.forestType, out parsedForestType);
            int forestOrder = cellData.sortingOrder + 5;
            forestManager.RestoreForestAt(cellData.x, cellData.y, parsedForestType, cellData.forestPrefabName, updateStats: false, savedSpriteSortingOrder: forestOrder);
        }
    }

    /// <summary>
    /// Restore grid from save data. Re-places all zones + buildings, recalculates available zoned sections.
    /// Two-pass: first restore all cell data (positions, attrs), then place tiles → building placement
    /// uses correct transformPosition + runtime refs not overwritten.
    /// </summary>
    /// <param name="gridData">List of serialized cell data from save file.</param>
    public void RestoreGrid(List<CellData> gridData)
    {
        if (gridData == null || cellArray == null) return;

        foreach (CellData cellData in gridData)
        {
            if (cellData.x < 0 || cellData.x >= width || cellData.y < 0 || cellData.y >= height)
                continue;
            cellArray[cellData.x, cellData.y].SetCellData(cellData);
        }

        List<CellData> sortedForVisuals = SortCellDataForVisualRestore(gridData);

        foreach (CellData cellData in sortedForVisuals)
        {
            if (cellData.x < 0 || cellData.x >= width || cellData.y < 0 || cellData.y >= height)
                continue;

            GameObject cell = gridArray[cellData.x, cellData.y];
            RestoreGridCellVisuals(cellData, cell);
        }

        RecalculateBuildingSortingAfterLoad(out _);

        if (forestManager != null)
            forestManager.RefreshForestStatistics();

        InvalidateRoadCache();
        onGridRestored?.Invoke();
        zoneManager.CalculateAvailableSquareZonedSections();

        // Post-load diagnostic: count empty cells and safety net
        RunPostLoadDiagnosticAndSafetyNet();

        skipChunkCullingFramesRemaining = 3;
    }

    void RunPostLoadDiagnosticAndSafetyNet()
    {
        if (cellArray == null || zoneManager == null) return;

        GameObject fallbackGrass = zoneManager.GetRandomZonePrefab(Zone.ZoneType.Grass, 1)
            ?? (zoneManager.grassPrefabs != null && zoneManager.grassPrefabs.Count > 0 ? zoneManager.grassPrefabs[0] : null);

        for (int x = 0; x < width; x++)
        {
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
                        Zone z = zoneTile.GetComponent<Zone>();
                        if (z == null) { z = zoneTile.AddComponent<Zone>(); z.zoneType = Zone.ZoneType.Grass; }
                    }
                }
            }
        }

    }

    /// <summary>
    /// Destroy all chunks + recreate fresh empty grid. Clears all zoned positions.
    /// </summary>
    public void ResetGrid()
    {
        if (chunkObjects != null)
        {
            for (int cx = 0; cx < chunksX; cx++)
            {
                for (int cy = 0; cy < chunksY; cy++)
                {
                    if (chunkObjects[cx, cy] != null)
                        Destroy(chunkObjects[cx, cy]);
                }
            }
        }

        zoneManager.ClearZonedPositions();
        InvalidateRoadCache();
        onGridRestored?.Invoke();

        cellArray = null;
        CreateGrid();
    }

    /// <summary>
    /// Clear grid from InitializeGeography + recreate fresh empty grid.
    /// Used at start of LoadGame → restoration runs on clean slate instead of
    /// overwriting randomly-initialized terrain. Does not invoke onGridRestored (RestoreGrid will).
    /// </summary>
    public void ResetGridForLoad()
    {
        if (chunkObjects != null)
        {
            for (int cx = 0; cx < chunksX; cx++)
            {
                for (int cy = 0; cy < chunksY; cy++)
                {
                    if (chunkObjects[cx, cy] != null)
                        Destroy(chunkObjects[cx, cy]);
                }
            }
        }

        zoneManager.ClearZonedPositions();
        InvalidateRoadCache();

        cellArray = null;
        CreateGrid(createBaseTiles: true);

        if (chunkCulling != null)
        {
            chunkCulling.chunkObjects = chunkObjects;
            chunkCulling.chunkActiveState = chunkActiveState;
            chunkCulling.chunksX = chunksX;
            chunkCulling.chunksY = chunksY;
            // Ensure all chunks start visible after load (CreateGrid sets chunkActiveState true, but reset explicitly for safety)
            int maxCx = Mathf.Min(chunksX, chunkObjects != null ? chunkObjects.GetLength(0) : 0);
            int maxCy = Mathf.Min(chunksY, chunkObjects != null ? chunkObjects.GetLength(1) : 0);
            for (int cx = 0; cx < maxCx; cx++)
            {
                for (int cy = 0; cy < maxCy; cy++)
                {
                    if (chunkActiveState != null && cx < chunkActiveState.GetLength(0) && cy < chunkActiveState.GetLength(1))
                        chunkActiveState[cx, cy] = true;
                    if (chunkObjects[cx, cy] != null)
                        chunkObjects[cx, cy].SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// CityCell at grid coords, or null if out of bounds.
    /// </summary>
    /// <param name="x">Grid X.</param>
    /// <param name="y">Grid Y.</param>
    /// <returns>CityCell component, or null.</returns>
    public CityCell GetCell(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
        {
            return cellArray[x, y];
        }
        return null;
    }

    /// <summary>
    /// Typed accessor for CellBase subclasses at grid coords.
    /// MVP: cellArray stores CityCell only; non-CityCell T returns null.
    /// </summary>
    /// <typeparam name="T">CellBase subclass (e.g. CityCell). Constrained to CellBase.</typeparam>
    /// <param name="x">Grid X.</param>
    /// <param name="y">Grid Y.</param>
    /// <returns>Cell cast to T, or null if out-of-range or type mismatch.</returns>
    public T GetCell<T>(int x, int y) where T : CellBase
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return cellArray[x, y] as T;
    }

    /// <summary>
    /// Serialize every cell in grid → list of CellData for saving.
    /// </summary>
    /// <returns>List containing one CellData per grid cell.</returns>
    public List<CellData> GetGridData()
    {
        List<CellData> gridData = new List<CellData>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CityCell cellComponent = cellArray[x, y];
                CellData cellData = cellComponent.GetCellData();
                gridData.Add(cellData);
            }
        }

        return gridData;
    }

    /// <summary>
    /// True if cell on outer edge of grid (first or last row/column).
    /// </summary>
    /// <param name="x">Grid X.</param>
    /// <param name="y">Grid Y.</param>
    /// <returns>True if cell on grid border.</returns>
    public bool isBorderCell(int x, int y)
    {
        if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
        {
            return true;
        }
        return false;
    }
    #endregion

    #region Road Cache and Pathfinding
    /// <summary>
    /// Mark cached road positions stale → rebuilt on next query.
    /// </summary>
    public void InvalidateRoadCache()
        => roadCache.Invalidate();

    /// <summary>
    /// Add road pos to cache incrementally. Call when road tile placed.
    /// </summary>
    public void AddRoadToCache(Vector2Int pos)
        => roadCache.AddRoad(pos);

    /// <summary>
    /// Remove road pos from cache incrementally. Call when road tile demolished.
    /// </summary>
    public void RemoveRoadFromCache(Vector2Int pos)
        => roadCache.RemoveRoad(pos);

    /// <summary>
    /// All grid positions containing road. Uses lazily rebuilt cache.
    /// </summary>
    /// <returns>Cached list of road positions.</returns>
    public List<Vector2Int> GetAllRoadPositions()
        => roadCache.GetAllRoadPositions();

    /// <summary>
    /// Road positions as HashSet for O(1) Contains lookups.
    /// </summary>
    public HashSet<Vector2Int> GetRoadPositionsAsHashSet()
        => roadCache.GetRoadPositionsAsHashSet();

    /// <summary>
    /// Road positions with ≥1 expandable (grass/forest/sea-level) cardinal neighbor = road frontier.
    /// </summary>
    /// <returns>Cached list of road edge positions.</returns>
    public List<Vector2Int> GetRoadEdgePositions()
        => roadCache.GetRoadEdgePositions();

    /// <summary>Cells one step beyond each road edge in natural extension direction. AutoZoningManager must not zone these.</summary>
    public HashSet<Vector2Int> GetRoadExtensionCells()
        => roadCache.GetRoadExtensionCells();

    /// <summary>Axial corridor beyond road edges for future street alignment. AutoZoningManager must not zone these.</summary>
    public HashSet<Vector2Int> GetRoadAxialCorridorCells()
        => roadCache.GetRoadAxialCorridorCells();

    /// <summary>Count of cardinal neighbors of (gx,gy) that are zoneable (Grass, Forest, Flat/N-S/E-W slope). Used for road-reservation in auto-zoning.</summary>
    public int CountGrassNeighbors(int gx, int gy)
        => roadCache.CountGrassNeighbors(gx, gy);

    /// <summary>Count of cardinal neighbors of (gx,gy) that are roads. Identifies axial termini for reserving perpendicular road generation.</summary>
    public int CountRoadNeighbors(int gx, int gy)
        => roadCache.CountRoadNeighbors(gx, gy);

    /// <summary>True if neighbor cell valid for zoning (Grass, Forest, Flat/N-S/E-W slope).</summary>
    public bool IsZoneableNeighbor(CityCell c, int x, int y)
        => roadCache.IsZoneableNeighbor(c, x, y);

    /// <summary>True if ≥1 of 4 cardinal neighbors of (x,y) is road.</summary>
    public bool IsAdjacentToRoad(int x, int y)
        => roadCache.IsAdjacentToRoad(x, y);

    /// <summary>All grid cells within maxDistance (Manhattan) of any road. Cached + invalidated when roads change.</summary>
    public HashSet<Vector2Int> GetCellsWithinDistanceOfRoad(int maxDistance)
        => roadCache.GetCellsWithinDistanceOfRoad(maxDistance);

    /// <summary>True if (x,y) within maxDistance (Manhattan) of any road cell.</summary>
    public bool IsWithinDistanceOfRoad(int x, int y, int maxDistance)
        => roadCache.IsWithinDistanceOfRoad(x, y, maxDistance);

    /// <summary>
    /// A* path over walkable cells (grass or road). Prefers flat; cardinal slopes cost more; diagonal slopes impassable.
    /// Max 200 nodes explored. Returns path incl. start + end, or empty if not found.
    /// </summary>
    public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to)
        => pathfinder.FindPath(from, to);

    /// <summary>
    /// A* path with optional extra cost for cells near existing roads → paths keep minDistanceFromRoad cells away, leave space for zones.
    /// minDistanceFromRoad = 0 → behaves like FindPath. &gt; 0 → adds penalty for stepping on cells within that Manhattan distance of any road.
    /// </summary>
    public List<Vector2Int> FindPathWithRoadSpacing(Vector2Int from, Vector2Int to, int minDistanceFromRoad)
        => pathfinder.FindPathWithRoadSpacing(from, to, minDistanceFromRoad);

    /// <summary>A* for AUTO simulation. Walkable set includes undeveloped light zoning.</summary>
    public List<Vector2Int> FindPathForAutoSimulation(Vector2Int from, Vector2Int to)
        => pathfinder.FindPathForAutoSimulation(from, to);

    /// <summary>A* with road-spacing for AUTO simulation. Undeveloped light zoning walkable.</summary>
    public List<Vector2Int> FindPathWithRoadSpacingForAutoSimulation(Vector2Int from, Vector2Int to, int minDistanceFromRoad)
        => pathfinder.FindPathWithRoadSpacingForAutoSimulation(from, to, minDistanceFromRoad);
    #endregion
}
}
