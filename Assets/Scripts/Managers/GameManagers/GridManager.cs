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
using Domains.Grid.Services;

namespace Territory.Core
{
/// <summary>
/// Thin hub for isometric 2D grid. Delegates to service layer extracted in Stage 3.0.
/// Body methods live in GridManager.Impl.cs (partial class).
/// Invariant #5: cellArray carve-out — raw cellArray access preserved in Impl for save/restore performance paths.
/// </summary>
[DefaultExecutionOrder(-100)]
public partial class GridManager : MonoBehaviour, IGridManager
{
    #region Dependencies
    public ZoneManager zoneManager;
    public UIManager uiManager;
    public CityStats cityStats;
    public CursorManager cursorManager;
    public TerrainManager terrainManager;
    /// <summary>Interface accessor for cross-asmdef consumers (Domains.* / Core.*) that can't ref concrete TerrainManager.</summary>
    public ITerrainManager Terrain => terrainManager;
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
    private CellAccessService cellAccessService;
    private GridQueryService _gridQueryService;

    /// <summary>Fired when urban cells (buildings) bulldozed.</summary>
    public System.Action<System.Collections.Generic.IReadOnlyList<Vector2Int>> onUrbanCellsBulldozed;
    /// <summary>Fired when grid restored from save.</summary>
    public System.Action onGridRestored;
    /// <summary>Alt+Click world-select action.</summary>
    public System.Action<Vector2Int> _worldSelectAction;
    #endregion

    #region Grid Configuration
    public int width, height;
    int IGridManager.width => width;
    int IGridManager.height => height;
    public float tileWidth = 1f;
    public float tileHeight = 0.5f;
    public float halfWidth;
    public float halfHeight;
    public GameObject[,] gridArray;
    public CityCell[,] cellArray;
    public Vector2 mouseGridPosition;
    public Vector2 selectedPoint = new Vector2(-1, -1);
    private Vector2 pendingRightClickGridPosition = new Vector2(-1, -1);
    public int mouseGridHeight;
    public int mouseGridSortingOrder;
    public bool isInitialized = false;

    #region Parent-scale identity
    public string ParentRegionId { get; private set; }
    public string ParentCountryId { get; private set; }
    private bool _parentIdsHydrated;
    private System.Collections.Generic.IReadOnlyList<NeighborCityStub> _neighborStubs = System.Array.Empty<NeighborCityStub>();
    private bool _neighborStubsHydrated;
    #endregion

    [Header("Chunk Culling")]
    public int chunkSize = 16;
    private GameObject[,] chunkObjects;
    private bool[,] chunkActiveState;
    private int chunksX, chunksY;
    [SerializeField] private Camera cachedCamera;
    private int skipChunkCullingFramesRemaining = 0;
    #endregion

    #region Sorting Order delegates
    /// <summary>Legacy tile sorting order formula.</summary>
    public int SetTileSortingOrder(GameObject tile, Zone.ZoneType zoneType = Zone.ZoneType.Grass)
        => sortingService.SetTileSortingOrder(tile, zoneType);
    /// <summary>Zoning tile sorting via TerrainManager.</summary>
    public void SetZoningTileSortingOrder(GameObject tile, int x, int y)
        => sortingService.SetZoningTileSortingOrder(tile, x, y);
    /// <summary>Zone building sorting via TerrainManager.</summary>
    public void SetZoneBuildingSortingOrder(GameObject tile, int x, int y)
        => sortingService.SetZoneBuildingSortingOrder(tile, x, y);
    /// <summary>Multi-cell building sorting via max over footprint.</summary>
    public void SetZoneBuildingSortingOrder(GameObject tile, int pivotX, int pivotY, int buildingSize)
        => sortingService.SetZoneBuildingSortingOrder(tile, pivotX, pivotY, buildingSize);
    /// <summary>Road tile sorting order at (x,y) at given height.</summary>
    public int GetRoadSortingOrderForCell(int x, int y, int height)
        => sortingService.GetRoadSortingOrderForCell(x, y, height);
    /// <summary>Road tile sorting via TerrainManager.</summary>
    public void SetRoadSortingOrder(GameObject tile, int x, int y)
        => sortingService.SetRoadSortingOrder(tile, x, y);
    /// <summary>Sea-level tile sorting order.</summary>
    public int SetResortSeaLevelOrder(GameObject tile, CityCell cell)
        => sortingService.SetResortSeaLevelOrder(tile, cell);
    #endregion

    #region Road Cache + Pathfinding delegates
    /// <summary>Mark road cache stale.</summary>
    public void InvalidateRoadCache() => roadCache.Invalidate();
    /// <summary>Add road pos to cache.</summary>
    public void AddRoadToCache(Vector2Int pos) => roadCache.AddRoad(pos);
    /// <summary>Remove road pos from cache.</summary>
    public void RemoveRoadFromCache(Vector2Int pos) => roadCache.RemoveRoad(pos);
    /// <summary>All grid positions containing road.</summary>
    public List<Vector2Int> GetAllRoadPositions() => roadCache.GetAllRoadPositions();
    /// <summary>Road positions as HashSet for O(1) Contains.</summary>
    public HashSet<Vector2Int> GetRoadPositionsAsHashSet() => roadCache.GetRoadPositionsAsHashSet();
    /// <summary>Road positions with expandable cardinal neighbor.</summary>
    public List<Vector2Int> GetRoadEdgePositions() => roadCache.GetRoadEdgePositions();
    /// <summary>Cells one step beyond each road edge.</summary>
    public HashSet<Vector2Int> GetRoadExtensionCells() => roadCache.GetRoadExtensionCells();
    /// <summary>Axial corridor beyond road edges.</summary>
    public HashSet<Vector2Int> GetRoadAxialCorridorCells() => roadCache.GetRoadAxialCorridorCells();
    /// <summary>Count zoneable cardinal neighbors of (gx,gy).</summary>
    public int CountGrassNeighbors(int gx, int gy) => roadCache.CountGrassNeighbors(gx, gy);
    /// <summary>Count road cardinal neighbors of (gx,gy).</summary>
    public int CountRoadNeighbors(int gx, int gy) => roadCache.CountRoadNeighbors(gx, gy);
    /// <summary>True if neighbor cell valid for zoning.</summary>
    public bool IsZoneableNeighbor(CityCell c, int x, int y) => roadCache.IsZoneableNeighbor(c, x, y);
    /// <summary>True if ≥1 cardinal neighbor of (x,y) is road.</summary>
    public bool IsAdjacentToRoad(int x, int y) => roadCache.IsAdjacentToRoad(x, y);
    /// <summary>All cells within maxDistance of any road.</summary>
    public HashSet<Vector2Int> GetCellsWithinDistanceOfRoad(int maxDistance) => roadCache.GetCellsWithinDistanceOfRoad(maxDistance);
    /// <summary>True if (x,y) within maxDistance of any road cell.</summary>
    public bool IsWithinDistanceOfRoad(int x, int y, int maxDistance) => roadCache.IsWithinDistanceOfRoad(x, y, maxDistance);
    /// <summary>A* path over walkable cells.</summary>
    public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to) => pathfinder.FindPath(from, to);
    /// <summary>A* path with road-spacing penalty.</summary>
    public List<Vector2Int> FindPathWithRoadSpacing(Vector2Int from, Vector2Int to, int minDistanceFromRoad) => pathfinder.FindPathWithRoadSpacing(from, to, minDistanceFromRoad);
    /// <summary>A* for AUTO simulation.</summary>
    public List<Vector2Int> FindPathForAutoSimulation(Vector2Int from, Vector2Int to) => pathfinder.FindPathForAutoSimulation(from, to);
    /// <summary>A* with road-spacing for AUTO simulation.</summary>
    public List<Vector2Int> FindPathWithRoadSpacingForAutoSimulation(Vector2Int from, Vector2Int to, int minDistanceFromRoad) => pathfinder.FindPathWithRoadSpacingForAutoSimulation(from, to, minDistanceFromRoad);
    #endregion

    #region Building Placement delegates
    /// <summary>Reason building placement fails at pos, or null if OK.</summary>
    public string GetBuildingPlacementFailReason(Vector2 gridPosition, int buildingSize, bool isWaterPlant)
        => placementService.GetBuildingPlacementFailReason(gridPosition, buildingSize, isWaterPlant);
    /// <summary>True if building can be placed at pos.</summary>
    public bool canPlaceBuilding(Vector2 gridPosition, int buildingSize)
        => placementService.CanPlaceBuilding(gridPosition, buildingSize);
    /// <summary>True if building can be placed at pos with explicit water plant flag.</summary>
    public bool canPlaceBuilding(Vector2 gridPosition, int buildingSize, bool isWaterPlant)
        => placementService.CanPlaceBuilding(gridPosition, buildingSize, isWaterPlant);
    /// <summary>Place building programmatically.</summary>
    public bool PlaceBuildingProgrammatic(Vector2 gridPos, IBuilding buildingTemplate)
        => placementService.PlaceBuildingProgrammatic(gridPos, buildingTemplate);
    #endregion

    #region Cell Access delegates
    /// <summary>CityCell at grid coords, or null if out of bounds.</summary>
    public CityCell GetCell(int x, int y)
        => x >= 0 && x < width && y >= 0 && y < height ? cellArray[x, y] : null;
    /// <summary>Typed accessor for CellBase subclasses.</summary>
    public T GetCell<T>(int x, int y) where T : CellBase
        => x < 0 || x >= width || y < 0 || y >= height ? null : cellArray[x, y] as T;
    /// <summary>GameObject for grid cell at pos, or null if out of bounds.</summary>
    public GameObject GetGridCell(Vector2 gridPos)
        => gridPos.x < 0 || gridPos.x >= gridArray.GetLength(0) || gridPos.y < 0 || gridPos.y >= gridArray.GetLength(1)
           ? null : gridArray[(int)gridPos.x, (int)gridPos.y];
    /// <summary>True if cell on outer edge of grid.</summary>
    public bool isBorderCell(int x, int y)
        => cellAccessService != null ? cellAccessService.IsBorderCell(x, y) : (x == 0 || x == width - 1 || y == 0 || y == height - 1);
    /// <summary>True if cell occupied by building.</summary>
    public bool IsCellOccupiedByBuilding(int x, int y)
        => x >= 0 && x < width && y >= 0 && y < height && cellArray[x, y] != null &&
           (cellArray[x, y].occupiedBuilding != null || IsZoneTypeBuilding(cellArray[x, y].zoneType));
    /// <summary>True if grid pos inside grid bounds.</summary>
    public bool IsValidGridPosition(Vector2 gridPosition)
        => _gridQueryService != null
            ? _gridQueryService.IsValidGridPosition(gridPosition)
            : ((int)gridPosition.x >= 0 && (int)gridPosition.x < width &&
               (int)gridPosition.y >= 0 && (int)gridPosition.y < height);
    #endregion
}
}
