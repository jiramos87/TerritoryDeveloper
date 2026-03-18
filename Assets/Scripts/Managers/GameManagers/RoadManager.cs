using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using Territory.Core;
using Territory.Terrain;
using Territory.Economy;
using Territory.UI;
using Territory.Zones;

namespace Territory.Roads
{
/// <summary>
/// Manages road placement, drawing, and prefab selection on the grid. Handles road preview
/// during drag, selects correct road prefab based on neighbor connectivity, and coordinates
/// with TerrainManager for slope adaptation and InterstateManager for highway connections.
/// </summary>
public class RoadManager : MonoBehaviour, IRoadManager
{
    #region Dependencies
    public TerrainManager terrainManager;
    public GridManager gridManager;
    public CityStats cityStats;
    public UIManager uiManager;
    public ZoneManager zoneManager;
    public InterstateManager interstateManager;
    public TerraformingService terraformingService;
    #endregion

    #region Road Drawing State
    private bool isDrawingRoad = false;
    private Vector2 startPosition;
    private Dictionary<Vector2Int, int> previewTerraformedHeights = new Dictionary<Vector2Int, int>();
    private static readonly int[] DirX = { 1, -1, 0, 0 };
    private static readonly int[] DirY = { 0, 0, 1, -1 };
    private RoadPrefabResolver roadPrefabResolver;
    private PathTerraformPlan currentPreviewPlan;
    private List<RoadPrefabResolver.ResolvedRoadTile> previewResolvedTiles = new List<RoadPrefabResolver.ResolvedRoadTile>();
    #endregion

    #region Road Prefabs
    public List<GameObject> roadTilePrefabs;
    public GameObject roadTilePrefab1;
    public GameObject roadTilePrefab2;
    public GameObject roadTilePrefabCrossing;
    public GameObject roadTilePrefabTIntersectionUp;
    public GameObject roadTilePrefabTIntersectionDown;
    public GameObject roadTilePrefabTIntersectionLeft;
    public GameObject roadTilePrefabTIntersectionRight;
    public GameObject roadTilePrefabElbowUpLeft;
    public GameObject roadTilePrefabElbowUpRight;
    public GameObject roadTilePrefabElbowDownLeft;
    public GameObject roadTilePrefabElbowDownRight;
    public GameObject roadTileBridgeVertical;
    public GameObject roadTileBridgeHorizontal;
    public GameObject roadTilePrefabEastSlope;
    public GameObject roadTilePrefabWestSlope;
    public GameObject roadTilePrefabNorthSlope;
    public GameObject roadTilePrefabSouthSlope;
    private List<GameObject> previewRoadTiles = new List<GameObject>();
    private List<Vector2> previewRoadGridPositions = new List<Vector2>();

    /// <summary>
    /// Populates the road tile prefabs list from the individual prefab fields.
    /// </summary>
    public void Initialize()
    {
        if (gridManager != null && terrainManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        roadTilePrefabs = new List<GameObject>
        {
            roadTilePrefab1,
            roadTilePrefab2,
            roadTilePrefabCrossing,
            roadTilePrefabTIntersectionUp,
            roadTilePrefabTIntersectionDown,
            roadTilePrefabTIntersectionLeft,
            roadTilePrefabTIntersectionRight,
            roadTilePrefabElbowUpLeft,
            roadTilePrefabElbowUpRight,
            roadTilePrefabElbowDownLeft,
            roadTilePrefabElbowDownRight,
            roadTileBridgeVertical,
            roadTileBridgeHorizontal,
            roadTilePrefabEastSlope,
            roadTilePrefabWestSlope,
            roadTilePrefabNorthSlope,
            roadTilePrefabSouthSlope
        };
    }
    #endregion

    #region Road Drawing
    /// <summary>
    /// Handles the full road drawing input lifecycle: start on mouse down, preview line on drag, and place on mouse up.
    /// </summary>
    /// <param name="gridPosition">The current grid position under the cursor.</param>
    public void HandleRoadDrawing(Vector2 gridPosition)
    {
        Vector2 pos = new Vector2((int)gridPosition.x, (int)gridPosition.y);

        if (!terrainManager.CanPlaceRoad((int)pos.x, (int)pos.y))
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (interstateManager != null && !interstateManager.CanPlaceStreetFrom(pos))
            {
                if (GameNotificationManager.Instance != null)
                    GameNotificationManager.Instance.PostWarning("Streets must connect to the Interstate Highway or existing connected roads.");
                return;
            }
            isDrawingRoad = true;
            startPosition = pos;
            if (uiManager != null)
            {
                uiManager.HideGhostPreview();
            }
        }
        else if (isDrawingRoad && Input.GetMouseButton(0))
        {
            Vector2 currentMousePosition = pos;
            List<Vector2> path = GetLine(startPosition, currentMousePosition);
            DrawPreviewLine(path);
        }

        if (Input.GetMouseButtonUp(0) && isDrawingRoad)
        {
            isDrawingRoad = false;
            DrawRoadLine(true);
            ClearPreview(true);
            if (uiManager != null)
            {
                uiManager.RestoreGhostPreview();
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (gridManager == null || gridManager.cameraController == null || !gridManager.cameraController.WasLastRightClickAPan)
            {
                isDrawingRoad = false;
                ClearPreview();
                if (uiManager != null)
                {
                    uiManager.RestoreGhostPreview();
                }
            }
        }
    }

    void DrawRoadLine(bool calculateCost = true)
    {
        int tileCount = previewResolvedTiles.Count > 0 ? previewResolvedTiles.Count : previewRoadGridPositions.Count;
        if (calculateCost)
        {
            int totalCost = CalculateTotalCost(tileCount);

            // Check if player can afford the road
            if (!cityStats.CanAfford(totalCost))
            {
                uiManager.ShowInsufficientFundsTooltip("Road", totalCost);
                ClearPreview();
                isDrawingRoad = false;
                return;
            }

            // Deduct the cost if we can afford it
            cityStats.RemoveMoney(totalCost);
        }

        if (previewResolvedTiles.Count > 0)
        {
            for (int i = 0; i < previewResolvedTiles.Count; i++)
            {
                PlaceRoadTileFromResolved(previewResolvedTiles[i]);
                UpdateAdjacentRoadPrefabsAt(new Vector2(previewResolvedTiles[i].gridPos.x, previewResolvedTiles[i].gridPos.y));
            }
        }
        else
        {
            for (int i = 0; i < previewRoadGridPositions.Count; i++)
            {
                Vector2 gridPos = previewRoadGridPositions[i];
                PlaceRoadTile(gridPos, i, false);
                UpdateAdjacentRoadPrefabs(gridPos, i);
            }
        }

        if (calculateCost)
        {
            int roadPowerConsumption = tileCount * ZoneAttributes.Road.PowerConsumption;
            cityStats.AddPowerConsumption(roadPowerConsumption);
        }
    }

    int CalculateTotalCost(int tilesCount)
    {
        return GetRoadCostForTileCount(tilesCount);
    }

    /// <summary>
    /// Returns true when the player is actively drawing a road (mouse held after initial click).
    /// </summary>
    public bool IsDrawingRoad()
    {
        return isDrawingRoad;
    }

    /// <summary>
    /// Returns the number of tiles in the current road preview path (while drawing).
    /// </summary>
    public int GetPreviewRoadTileCount()
    {
        return previewRoadGridPositions.Count;
    }

    /// <summary>
    /// Returns the cost per road tile (50).
    /// </summary>
    public int GetRoadCostPerTile()
    {
        return 50;
    }

    /// <summary>
    /// Returns the total cost for placing the given number of road tiles.
    /// </summary>
    /// <param name="tilesCount">Number of road tiles.</param>
    /// <returns>Total construction cost.</returns>
    public int GetRoadCostForTileCount(int tilesCount)
    {
        return tilesCount * 50;
    }

    bool isAdjacentRoadInPreview(Vector2 gridPos)
    {
        foreach (Vector2 previewGridPos in previewRoadGridPositions)
        {
            if (gridPos == previewGridPos)
            {
                return true;
            }
        }
        return false;
    }

    void UpdateAdjacentRoadPrefabs(Vector2 gridPos, int i)
    {
        Vector2[] dirs = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };
        foreach (Vector2 d in dirs)
        {
            Vector2 n = gridPos + d;
            if (IsRoadAt(n) && !isAdjacentRoadInPreview(n))
                RefreshRoadPrefabAt(n);
        }
    }

    void IRoadManager.UpdateAdjacentRoadPrefabsAt(Vector2 gridPos) => UpdateAdjacentRoadPrefabsAt(gridPos);

    /// <summary>
    /// Refreshes prefabs of all road tiles adjacent to the given position so they connect correctly.
    /// Use after programmatic placement (e.g. AutoRoadBuilder) so existing roads update to T-junctions/crossings.
    /// Road cache is updated incrementally by the placement caller (AddRoadToCache).
    /// </summary>
    /// <param name="gridPos">Grid position of the newly placed road.</param>
    public void UpdateAdjacentRoadPrefabsAt(Vector2 gridPos)
    {
        var toRefresh = new List<Vector2>();
        Vector2[] dirs = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };
        foreach (Vector2 d in dirs)
        {
            Vector2 n = gridPos + d;
            if (IsRoadAt(n))
                toRefresh.Add(n);
        }
        foreach (Vector2 pos in toRefresh)
            RefreshRoadPrefabAt(pos);
    }

    void RefreshRoadPrefabAt(Vector2 gridPos)
    {
        if (gridManager.IsCellOccupiedByBuilding((int)gridPos.x, (int)gridPos.y))
            return;
        GameObject cell = gridManager.GetGridCell(gridPos);
        if (cell == null) return;
        Cell cellComponentCheck = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        if (cellComponentCheck == null) return;

        Vector2 prevGridPos = gridPos;
        Vector2[] dirs = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };
        foreach (Vector2 d in dirs)
        {
            if (IsRoadAt(gridPos + d))
            {
                prevGridPos = gridPos + d;
                break;
            }
        }

        if (roadPrefabResolver == null && gridManager != null && terrainManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);

        GameObject correctRoadPrefab;
        Vector2 worldPos;
        if (roadPrefabResolver != null)
        {
            var resolved = roadPrefabResolver.ResolveForCell(gridPos, prevGridPos);
            if (resolved.HasValue)
            {
                correctRoadPrefab = resolved.Value.prefab;
                worldPos = resolved.Value.worldPos;
            }
            else
            {
                correctRoadPrefab = roadTilePrefab1;
                worldPos = gridManager.GetWorldPosition((int)gridPos.x, (int)gridPos.y);
            }
        }
        else
        {
            correctRoadPrefab = GetCorrectRoadPrefab(prevGridPos, gridPos, false, false);
            int terrainHeight = cellComponentCheck.GetCellInstanceHeight();
            worldPos = GetRoadTileWorldPosition((int)gridPos.x, (int)gridPos.y, correctRoadPrefab, terrainHeight);
        }

        DestroyPreviousRoadTile(cell, gridPos);
        cellComponentCheck.RemoveForestForBuilding();

        GameObject roadTile = Instantiate(correctRoadPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponentCheck.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = cellComponentCheck.isInterstate
            ? new Color(0.78f, 0.78f, 0.88f, 1f)
            : new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;

        UpdateRoadCellAttributes(cellComponentCheck, roadTile, Zone.ZoneType.Road);
        gridManager.SetRoadSortingOrder(roadTile, (int)gridPos.x, (int)gridPos.y);
    }

    void DrawPreviewLine(List<Vector2> path)
    {
        ClearPreview(false);

        List<Vector2> filteredPath = new List<Vector2>();
        for (int i = 0; i < path.Count; i++)
        {
            Vector2 gridPos = path[i];
            if (gridManager.GetCell((int)gridPos.x, (int)gridPos.y) != null)
                filteredPath.Add(gridPos);
        }

        if (filteredPath.Count == 0) return;
        if (terraformingService == null || terrainManager == null) return;
        if (roadPrefabResolver == null && gridManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver == null) return;

        if (!IsPathFullyAdjacent(filteredPath))
        {
            if (GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Road path has gaps (e.g. over water). Draw a continuous path.");
            return;
        }

        var plan = terraformingService.ComputePathPlan(filteredPath);
        if (!plan.isValid)
        {
            if (GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Road cannot cross terrain with height difference greater than 1. Choose a different path.");
            return;
        }

        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null) return;

        if (!plan.Apply(heightMap, terrainManager))
        {
            if (GameNotificationManager.Instance != null)
                GameNotificationManager.Instance.PostWarning("Terrain cannot be modified safely (height difference would exceed 1). Choose a different path.");
            return;
        }
        currentPreviewPlan = plan;

        var resolved = roadPrefabResolver.ResolveForPath(filteredPath, plan);
        previewResolvedTiles.Clear();
        previewResolvedTiles.AddRange(resolved);

        for (int i = 0; i < resolved.Count; i++)
        {
            var tile = resolved[i];
            Cell cell = gridManager.GetCell(tile.gridPos.x, tile.gridPos.y);
            if (cell == null) continue;

            GameObject previewTile = Instantiate(tile.prefab, tile.worldPos, Quaternion.identity);
            SetPreviewRoadTileDetails(previewTile);
            previewRoadTiles.Add(previewTile);
            previewRoadGridPositions.Add(new Vector2(tile.gridPos.x, tile.gridPos.y));
            previewTile.transform.SetParent(cell.gameObject.transform);
        }
    }

    /// <summary>
    /// Returns grid cells from start to end. Uses A* pathfinding to prefer flat terrain and go around hills;
    /// falls back to Bresenham line when pathfinding finds no route (e.g. blocked by water).
    /// </summary>
    List<Vector2> GetLine(Vector2 start, Vector2 end)
    {
        Vector2Int from = new Vector2Int(Mathf.Clamp((int)start.x, 0, gridManager.width - 1), Mathf.Clamp((int)start.y, 0, gridManager.height - 1));
        Vector2Int to = new Vector2Int(Mathf.Clamp((int)end.x, 0, gridManager.width - 1), Mathf.Clamp((int)end.y, 0, gridManager.height - 1));

        if (gridManager != null)
        {
            var path = gridManager.FindPath(from, to);
            if (path != null && path.Count > 0)
            {
                var line = new List<Vector2>(path.Count);
                for (int i = 0; i < path.Count; i++)
                    line.Add(new Vector2(path[i].x, path[i].y));
                return line;
            }
        }

        return GetLineBresenham(start, end);
    }

    /// <summary>
    /// Bresenham line from start to end. Used as fallback when pathfinding finds no route.
    /// Diagonal steps are split into two cardinal steps (staircase) so road placement matches interstate-style elbows.
    /// </summary>
    List<Vector2> GetLineBresenham(Vector2 start, Vector2 end)
    {
        List<Vector2> line = new List<Vector2>();

        int x0 = Mathf.Clamp((int)start.x, 0, gridManager.width - 1);
        int y0 = Mathf.Clamp((int)start.y, 0, gridManager.height - 1);
        int x1 = Mathf.Clamp((int)end.x, 0, gridManager.width - 1);
        int y1 = Mathf.Clamp((int)end.y, 0, gridManager.height - 1);

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            line.Add(new Vector2(x0, y0));

            if (x0 == x1 && y0 == y1) break;

            int e2 = err * 2;
            bool movedX = false;
            bool movedY = false;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
                movedX = true;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
                movedY = true;
            }

            if (movedX && movedY)
            {
                line.Add(new Vector2(x0 - sx, y0));
            }
        }

        return line;
    }

    /// <summary>
    /// Returns true if every consecutive pair in the path is adjacent (within 1 cell).
    /// Used to reject paths with gaps (e.g. over water) that would create loose corners.
    /// </summary>
    bool IsPathFullyAdjacent(List<Vector2> path)
    {
        if (path == null || path.Count < 2) return true;
        for (int i = 1; i < path.Count; i++)
        {
            int dx = Mathf.Abs((int)path[i].x - (int)path[i - 1].x);
            int dy = Mathf.Abs((int)path[i].y - (int)path[i - 1].y);
            if (dx > 1 || dy > 1) return false;
        }
        return true;
    }

    void ClearPreview(bool isEnd = false)
    {
        if (!isEnd && currentPreviewPlan != null && terrainManager != null)
        {
            var heightMap = terrainManager.GetHeightMap();
            if (heightMap != null)
                currentPreviewPlan.Revert(heightMap, terrainManager);
            currentPreviewPlan = null;
        }
        previewTerraformedHeights.Clear();
        previewResolvedTiles.Clear();

        foreach (GameObject previewTile in previewRoadTiles)
        {
            Destroy(previewTile);
        }
        previewRoadTiles.Clear();
        previewRoadGridPositions.Clear();
    }

    #endregion

    #region Road Prefab Selection
    /// <summary>
    /// Returns the correct road prefab for a cell. Delegates to RoadPrefabResolver.ResolveForCell.
    /// </summary>
    GameObject GetCorrectRoadPrefab(Vector2 prevGridPos, Vector2 currGridPos, bool isCenterRoadTile = true, bool isPreview = false, List<Vector2> path = null, int pathIndex = -1)
    {
        if (roadPrefabResolver == null && gridManager != null && terrainManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver == null) return roadTilePrefab1;
        var resolved = roadPrefabResolver.ResolveForCell(currGridPos, prevGridPos);
        return resolved.HasValue ? resolved.Value.prefab : roadTilePrefab1;
    }

    /// <summary>
    /// Returns the height of the neighbor at (gridX + dx, gridY + dy), or int.MinValue if out of bounds.
    /// Only use cardinal offsets: (dx, dy) one of (±1, 0) or (0, ±1).
    /// </summary>
    int GetNeighborHeight(int gridX, int gridY, int dx, int dy)
    {
        int nx = gridX + dx;
        int ny = gridY + dy;
        if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height)
            return int.MinValue;
        Cell c = gridManager.GetCell(nx, ny);
        return c != null ? c.GetCellInstanceHeight() : int.MinValue;
    }

    /// <summary>
    /// Returns the cardinal direction for the slope prefab only when there is adjacent higher ground
    /// (so we're on a slope). When we only have a lower neighbor (first flat tile after a slope),
    /// returns null so a flat road prefab is used.
    /// </summary>
    Vector2? GetTerrainSlopeDirection(Vector2 currGridPos, int currentHeight)
    {
        if (currentHeight == 0) return null;
        int x = (int)currGridPos.x;
        int y = (int)currGridPos.y;
        Vector2? directionToHigher = null;
        for (int i = 0; i < 4; i++)
        {
            int nh = GetNeighborHeight(x, y, DirX[i], DirY[i]);
            if (nh == int.MinValue) continue;
            int diff = nh - currentHeight;
            if (diff == 1)
                directionToHigher = new Vector2(DirX[i], DirY[i]);
        }
        if (!directionToHigher.HasValue) return null;
        int dxi = Mathf.RoundToInt(directionToHigher.Value.x);
        int dyi = Mathf.RoundToInt(directionToHigher.Value.y);
        bool isCardinal = (Mathf.Abs(dxi) == 1 && dyi == 0) || (dxi == 0 && Mathf.Abs(dyi) == 1);
        return isCardinal ? (Vector2?)directionToHigher.Value : null;
    }

    /// <summary>
    /// Returns true if the prefab is a diagonal road (elbow, used when route is diagonal on sloped terrain).
    /// Only these prefabs use higher positioning for correct visual integration with the slope.
    /// </summary>
    bool IsDiagonalRoadPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        return prefab == roadTilePrefabElbowUpLeft || prefab == roadTilePrefabElbowUpRight
            || prefab == roadTilePrefabElbowDownLeft || prefab == roadTilePrefabElbowDownRight;
    }

    /// <summary>
    /// Returns the world position for a road tile. For diagonal road prefabs on sloped terrain,
    /// uses the upper cell's position so the ramp renders with more height in the same cell.
    /// Orthogonal slope prefabs (East/West/North/South) use the current cell position.
    /// </summary>
    Vector2 GetRoadTileWorldPosition(int x, int y, GameObject prefab, int terrainHeight)
    {
        if (terrainHeight == 0)
            return gridManager.GetWorldPositionVector(x, y, 1);

        if (!IsDiagonalRoadPrefab(prefab))
            return gridManager.GetWorldPosition(x, y);

        int upperX = x, upperY = y;
        Vector2? slopeDir = GetTerrainSlopeDirection(new Vector2(x, y), terrainHeight);
        if (slopeDir.HasValue)
        {
            upperX = x + Mathf.RoundToInt(slopeDir.Value.x);
            upperY = y + Mathf.RoundToInt(slopeDir.Value.y);
        }
        else if (terrainManager != null)
        {
            TerrainSlopeType slopeType = terrainManager.GetTerrainSlopeTypeAt(x, y);
            switch (slopeType)
            {
                case TerrainSlopeType.SouthEast: upperX = x + 1; upperY = y + 1; break;
                case TerrainSlopeType.SouthWest: upperX = x + 1; upperY = y - 1; break;
                case TerrainSlopeType.NorthEast: upperX = x - 1; upperY = y + 1; break;
                case TerrainSlopeType.NorthWest: upperX = x - 1; upperY = y - 1; break;
                case TerrainSlopeType.SouthEastUp: upperX = x + 1; upperY = y; break;
                case TerrainSlopeType.NorthEastUp: upperX = x - 1; upperY = y; break;
                case TerrainSlopeType.SouthWestUp: upperX = x + 1; upperY = y; break;
                case TerrainSlopeType.NorthWestUp: upperX = x - 1; upperY = y; break;
                default: return gridManager.GetWorldPosition(x, y);
            }
        }
        else
        {
            return gridManager.GetWorldPosition(x, y);
        }

        if (upperX < 0 || upperX >= gridManager.width || upperY < 0 || upperY >= gridManager.height)
            return gridManager.GetWorldPosition(x, y);

        Cell upperCell = gridManager.GetCell(upperX, upperY);
        if (upperCell == null)
            return gridManager.GetWorldPosition(x, y);

        int upperHeight = upperCell.GetCellInstanceHeight();
        return gridManager.GetWorldPositionVector(upperX, upperY, upperHeight);
    }

    /// <summary>
    /// Returns the correct road prefab, world position and sorting order for the single-cell ghost preview at the given grid position.
    /// Used when hovering with the road tool (no line drawn): slope cells get slope prefab, water gets bridge at height 1, else flat road.
    /// </summary>
    public void GetRoadGhostPreviewForCell(Vector2 gridPos, out GameObject prefab, out Vector2 worldPos, out int sortingOrder)
    {
        prefab = roadTilePrefab1;
        worldPos = gridManager.GetWorldPosition((int)gridPos.x, (int)gridPos.y);
        sortingOrder = gridManager.GetRoadSortingOrderForCell((int)gridPos.x, (int)gridPos.y, 0);

        if (roadPrefabResolver == null && gridManager != null && terrainManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver != null)
            roadPrefabResolver.ResolveForGhostPreview(gridPos, out prefab, out worldPos, out sortingOrder);
    }
    #endregion

    #region Road Placement
    bool IsRoadAt(Vector2 gridPos)
    {
        bool isRoad = false;
        int gridX = Mathf.RoundToInt(gridPos.x);
        int gridY = Mathf.RoundToInt(gridPos.y);

        if (gridX >= 0 && gridX < gridManager.width && gridY >= 0 && gridY < gridManager.height)
        {
            isRoad = IsAnyChildRoad(gridX, gridY);

            return isRoad;
        }

        return false;
    }

    bool IsAnyChildRoad(int gridX, int gridY)
    {
        var cell = gridManager.GetGridCell(new Vector2(gridX, gridY));
        if (cell == null || cell.transform.childCount == 0) return false;

        var cellComponent = gridManager.GetCell(gridX, gridY);
        if (cellComponent != null && cellComponent.zoneType == Zone.ZoneType.Road) return true;

        for (int i = 0; i < cell.transform.childCount; i++)
        {
            var zone = cell.transform.GetChild(i).GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Road)
                return true;
        }
        return false;
    }

    void SetPreviewRoadTileDetails(GameObject previewTile)
    {
        gridManager.SetTileSortingOrder(previewTile, Zone.ZoneType.Road);

        SetRoadTileZoneDetails(previewTile);
        previewTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);
    }

    void SetRoadTileZoneDetails(GameObject roadTile)
    {
        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
    }

    /// <summary>
    /// Places a single road tile from a resolved prefab. Used by path pipeline (manual draw, interstate, AutoRoadBuilder).
    /// </summary>
    public void PlaceRoadTileFromResolved(RoadPrefabResolver.ResolvedRoadTile resolved)
    {
        int x = resolved.gridPos.x;
        int y = resolved.gridPos.y;
        if (gridManager.IsCellOccupiedByBuilding(x, y))
            return;

        GameObject cell = gridManager.GetGridCell(new Vector2(x, y));
        Cell cellComponentCheck = gridManager.GetCell(x, y);
        if (cellComponentCheck != null && cellComponentCheck.isInterstate)
            return;
        if (cell == null || cellComponentCheck == null) return;

        DestroyPreviousRoadTile(cell, new Vector2(x, y));
        cellComponentCheck.RemoveForestForBuilding();

        GameObject roadTile = Instantiate(resolved.prefab, resolved.worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponentCheck.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;

        UpdateRoadCellAttributes(cellComponentCheck, roadTile, Zone.ZoneType.Road);
        gridManager.SetRoadSortingOrder(roadTile, x, y);
        gridManager.AddRoadToCache(resolved.gridPos);
    }

    void PlaceRoadTile(Vector2 gridPos, int i = 0, bool isAdjacent = false)
    {
        if (gridManager.IsCellOccupiedByBuilding((int)gridPos.x, (int)gridPos.y))
            return;

        GameObject cell = gridManager.GetGridCell(gridPos);
        Cell cellComponentCheck = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        if (cellComponentCheck != null && cellComponentCheck.isInterstate)
            return;

        bool isCenterRoadTile = !isAdjacent;
        bool isPreview = false;

        Vector2 prevGridPos = (i == 0 && previewRoadGridPositions.Count > 1)
            ? 2 * gridPos - previewRoadGridPositions[1]
            : (i == 0 ? gridPos : previewRoadGridPositions[i - 1]);

        GameObject correctRoadPrefab = GetCorrectRoadPrefab(
            prevGridPos,
            gridPos,
            isCenterRoadTile,
            isPreview,
            previewRoadGridPositions,
            i
        );

        DestroyPreviousRoadTile(cell, gridPos);

        Cell cellComponent = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        cellComponent.RemoveForestForBuilding();
        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition((int)gridPos.x, (int)gridPos.y, correctRoadPrefab, terrainHeight);

        GameObject roadTile = Instantiate(
            correctRoadPrefab,
            worldPos,
            Quaternion.identity
        );

        roadTile.transform.SetParent(cellComponent.gameObject.transform);

        roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone.ZoneType zoneType = Zone.ZoneType.Road;

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = zoneType;

        UpdateRoadCellAttributes(cellComponent, roadTile, zoneType);

        gridManager.SetRoadSortingOrder(roadTile, (int)gridPos.x, (int)gridPos.y);
        gridManager.AddRoadToCache(new Vector2Int((int)gridPos.x, (int)gridPos.y));
    }

    void DestroyPreviousRoadTile(GameObject cell, Vector2 gridPos)
    {
        if (cell.transform.childCount > 0)
        {
            var toDestroy = new List<(GameObject go, Zone zone)>();
            foreach (Transform child in cell.transform)
            {
                Zone zone = child.GetComponent<Zone>();
                if (zone != null && zone.zoneType == Zone.ZoneType.Road)
                    toDestroy.Add((child.gameObject, zone));
            }
            foreach (var t in toDestroy)
            {
                gridManager.RemoveRoadFromCache(new Vector2Int((int)gridPos.x, (int)gridPos.y));
                Destroy(t.go);
            }
        }
    }


    void UpdateRoadCellAttributes(Cell cellComponent, GameObject roadTile, Zone.ZoneType zoneType)
    {
        cellComponent.zoneType = zoneType;
        cellComponent.prefab = roadTile;
        cellComponent.prefabName = roadTile.name;
        cellComponent.buildingType = "Road";
        cellComponent.powerPlant = null;
        cellComponent.population = 0;
        cellComponent.powerConsumption = 0;
        cellComponent.happiness = 0;
        cellComponent.isPivot = false;
    }
    #endregion

    #region Road Update
    /// <summary>
    /// Returns true if a road can be placed at the given grid position (terrain, not building, not interstate).
    /// </summary>
    public bool CanPlaceRoadAt(Vector2 gridPos)
    {
        int gx = (int)gridPos.x;
        int gy = (int)gridPos.y;
        if (!terrainManager.CanPlaceRoad(gx, gy))
            return false;
        if (gridManager.IsCellOccupiedByBuilding(gx, gy))
            return false;
        Cell c = gridManager.GetCell(gx, gy);
        if (c != null && c.isInterstate)
            return false;
        return true;
    }

    bool IRoadManager.PlaceRoadTileAt(Vector2 gridPos) => PlaceRoadTileAt(gridPos);

    /// <summary>
    /// Places a single road tile at the given grid position. Uses existing road neighbors to pick prefab.
    /// Caller is responsible for affordability and budget. Returns true if placed.
    /// Updates road cache incrementally (AddRoadToCache).
    /// </summary>
    /// <param name="gridPos">Grid position to place the road.</param>
    public bool PlaceRoadTileAt(Vector2 gridPos)
    {
        if (!CanPlaceRoadAt(gridPos))
            return false;

        Vector2 prevGridPos = gridPos + new Vector2(0, 1);
        Vector2[] dirs = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };
        foreach (Vector2 d in dirs)
        {
            if (IsRoadAt(gridPos + d))
            {
                prevGridPos = gridPos + d;
                break;
            }
        }

        GameObject cell = gridManager.GetGridCell(gridPos);
        if (cell == null) return false;
        Cell cellComponentCheck = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        if (cellComponentCheck != null && cellComponentCheck.isInterstate)
            return false;

        bool isCenterRoadTile = true;
        bool isPreview = false;
        GameObject correctRoadPrefab = GetCorrectRoadPrefab(prevGridPos, gridPos, isCenterRoadTile, isPreview);

        DestroyPreviousRoadTile(cell, gridPos);

        Cell cellComponent = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        cellComponent.RemoveForestForBuilding();
        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition((int)gridPos.x, (int)gridPos.y, correctRoadPrefab, terrainHeight);

        GameObject roadTile = Instantiate(correctRoadPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;

        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        gridManager.SetRoadSortingOrder(roadTile, (int)gridPos.x, (int)gridPos.y);
        gridManager.AddRoadToCache(new Vector2Int((int)gridPos.x, (int)gridPos.y));
        UpdateAdjacentRoadPrefabsAt(gridPos);
        return true;
    }

    public const int RoadCostPerTile = 50;

    /// <summary>
    /// Returns the list of all road tile prefabs.
    /// </summary>
    /// <returns>The road tile prefabs list.</returns>
    public List<GameObject> GetRoadPrefabs()
    {
        return roadTilePrefabs;
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Returns the correct road prefab for a cell in a path (for interstate placement).
    /// When forceFlatCells contains currGridPos, returns flat road (horizontal/vertical) regardless of terrain slope.
    /// </summary>
    public GameObject GetCorrectRoadPrefabForPath(Vector2 prevGridPos, Vector2 currGridPos, HashSet<Vector2Int> forceFlatCells = null)
    {
        int gx = (int)currGridPos.x, gy = (int)currGridPos.y;
        var currInt = new Vector2Int(gx, gy);
        if (forceFlatCells != null && forceFlatCells.Contains(currInt))
        {
            Vector2 dir = currGridPos - prevGridPos;
            bool isHorizontal = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);
            GameObject flatPrefab = isHorizontal ? roadTilePrefab2 : roadTilePrefab1;
            Debug.Log($"[Road GetCorrectRoadPrefabForPath] ({gx},{gy}) prev=({prevGridPos.x},{prevGridPos.y}) forceFlat=>{(flatPrefab != null ? flatPrefab.name : "null")}");
            return flatPrefab;
        }
        GameObject result = GetCorrectRoadPrefab(prevGridPos, currGridPos, true, false);
        TerrainSlopeType slopeType = terrainManager != null ? terrainManager.GetTerrainSlopeTypeAt(gx, gy) : TerrainSlopeType.Flat;
        Debug.Log($"[Road GetCorrectRoadPrefabForPath] ({gx},{gy}) prev=({prevGridPos.x},{prevGridPos.y}) slopeType={slopeType} => roadPrefab={(result != null ? result.name : "null")}");
        return result;
    }

    /// <summary>
    /// Resolves road prefabs for a path using the terraform plan. Used by AutoRoadBuilder for path-based placement.
    /// </summary>
    public List<RoadPrefabResolver.ResolvedRoadTile> ResolvePathForRoads(List<Vector2> path, PathTerraformPlan plan)
    {
        if (roadPrefabResolver == null && gridManager != null && terrainManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver == null) return new List<RoadPrefabResolver.ResolvedRoadTile>();
        return roadPrefabResolver.ResolveForPath(path, plan);
    }

    /// <summary>
    /// Places interstate tiles along a path using the centralized terraform + resolve pipeline.
    /// Call from InterstateManager after route generation.
    /// </summary>
    /// <returns>True if placement succeeded (plan valid and Apply succeeded).</returns>
    public bool PlaceInterstateFromPath(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("[Road] PlaceInterstateFromPath: path null or empty.");
            return false;
        }
        if (roadPrefabResolver == null && gridManager != null && terrainManager != null)
            roadPrefabResolver = new RoadPrefabResolver(gridManager, terrainManager, this);
        if (roadPrefabResolver == null || terraformingService == null || terrainManager == null)
        {
            Debug.LogWarning($"[Road] PlaceInterstateFromPath: missing deps resolver={roadPrefabResolver != null} terraform={terraformingService != null} terrain={terrainManager != null}");
            return false;
        }

        var pathVec2 = new List<Vector2>();
        for (int i = 0; i < path.Count; i++)
            pathVec2.Add(new Vector2(path[i].x, path[i].y));

        var plan = terraformingService.ComputePathPlan(pathVec2);
        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
        {
            Debug.LogWarning("[Road] PlaceInterstateFromPath: heightMap null.");
            return false;
        }
        bool planValid = plan.isValid;
        bool applyOk = plan.Apply(heightMap, terrainManager);
        if (!planValid || !applyOk)
        {
            Debug.LogWarning($"[Road] PlaceInterstateFromPath FAILED: plan.isValid={planValid} Apply={applyOk}");
            return false;
        }

        var resolved = roadPrefabResolver.ResolveForPath(pathVec2, plan);
        Debug.Log($"[Road] PlaceInterstateFromPath: path.Count={path.Count} resolved.Count={resolved.Count}");
        for (int i = 0; i < resolved.Count; i++)
        {
            PlaceInterstateFromResolved(resolved[i]);
        }
        return true;
    }

    /// <summary>
    /// Places an interstate tile from a resolved road tile. Applies interstate tint and sets isInterstate on the cell.
    /// </summary>
    public void PlaceInterstateFromResolved(RoadPrefabResolver.ResolvedRoadTile resolved)
    {
        int x = resolved.gridPos.x;
        int y = resolved.gridPos.y;
        if (gridManager.IsCellOccupiedByBuilding(x, y))
        {
            Debug.Log($"[Road] PlaceInterstateFromResolved: ({x},{y}) skipped - cell occupied by building.");
            return;
        }

        GameObject cell = gridManager.GetGridCell(new Vector2(x, y));
        Cell cellComponent = gridManager.GetCell(x, y);
        if (cell == null || cellComponent == null)
        {
            Debug.LogWarning($"[Road] PlaceInterstateFromResolved: ({x},{y}) cell or cellComponent null.");
            return;
        }

        DestroyPreviousRoadTile(cell, new Vector2(x, y));
        cellComponent.RemoveForestForBuilding();

        GameObject prefab = resolved.prefab ?? roadTilePrefab1;
        GameObject roadTile = Instantiate(prefab, resolved.worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = new Color(0.78f, 0.78f, 0.88f, 1f);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        cellComponent.isInterstate = true;

        gridManager.SetRoadSortingOrder(roadTile, x, y);
        gridManager.AddRoadToCache(resolved.gridPos);
        Debug.Log($"[Road] PlaceInterstateFromResolved: ({x},{y}) placed prefab={prefab?.name ?? "null"}");
    }

    /// <summary>
    /// Place a single road tile for the interstate at currGridPos. Clears forest and applies interstate tint.
    /// </summary>
    public void PlaceInterstateTile(Vector2 prevGridPos, Vector2 currGridPos, bool isInterstate)
    {
        Vector2 gridPos = currGridPos;
        int gx = (int)gridPos.x;
        int gy = (int)gridPos.y;
        if (gridManager.IsCellOccupiedByBuilding(gx, gy)) return;

        GameObject cell = gridManager.GetGridCell(gridPos);
        Cell cellComponent = gridManager.GetCell(gx, gy);
        if (cellComponent == null) return;

        DestroyPreviousRoadTile(cell, gridPos);
        cellComponent.RemoveForestForBuilding();

        GameObject correctRoadPrefab = GetCorrectRoadPrefabForPath(prevGridPos, currGridPos);
        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition(gx, gy, correctRoadPrefab, terrainHeight);

        GameObject roadTile = Instantiate(correctRoadPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);

        if (isInterstate)
            roadTile.GetComponent<SpriteRenderer>().color = new Color(0.78f, 0.78f, 0.88f, 1f);
        else
            roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        if (isInterstate)
            cellComponent.isInterstate = true;

        gridManager.SetRoadSortingOrder(roadTile, gx, gy);
        gridManager.AddRoadToCache(new Vector2Int(gx, gy));
    }

    /// <summary>
    /// Restores a road tile from save data. Uses the exact prefab, applies interstate tint when needed,
    /// and sets correct sorting order. Call during RestoreGrid for Road cells.
    /// </summary>
    /// <param name="gridPos">Grid position to restore the road at.</param>
    /// <param name="prefab">Road prefab to instantiate (from saved prefabName).</param>
    /// <param name="isInterstate">Whether this cell is part of the interstate (applies gray tint).</param>
    public void RestoreRoadTile(Vector2Int gridPos, GameObject prefab, bool isInterstate)
    {
        GameObject cell = gridManager.GetGridCell(new Vector2(gridPos.x, gridPos.y));
        if (cell == null) return;
        Cell cellComponent = gridManager.GetCell(gridPos.x, gridPos.y);
        if (cellComponent == null) return;

        var toDestroy = new List<(GameObject go, Zone zone)>();
        foreach (Transform child in cell.transform)
        {
            Zone z = child.GetComponent<Zone>();
            if (z != null)
                toDestroy.Add((child.gameObject, z));
        }
        foreach (var t in toDestroy)
        {
            if (t.zone.zoneCategory == Zone.ZoneCategory.Zoning)
                zoneManager.removeZonedPositionFromList(new Vector2(gridPos.x, gridPos.y), t.zone.zoneType);
            Destroy(t.go);
        }

        cellComponent.RemoveForestForBuilding();

        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition(gridPos.x, gridPos.y, prefab, terrainHeight);

        GameObject roadTile = Instantiate(prefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);

        if (isInterstate)
            roadTile.GetComponent<SpriteRenderer>().color = new Color(0.78f, 0.78f, 0.88f, 1f);
        else
            roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        if (isInterstate)
            cellComponent.isInterstate = true;

        gridManager.SetRoadSortingOrder(roadTile, gridPos.x, gridPos.y);
        gridManager.AddRoadToCache(gridPos);
    }

    /// <summary>
    /// Replace the road tile at the given position with a new prefab (e.g. after all interstate tiles placed to fix junctions). Preserves isInterstate and tint.
    /// Road remains at same position, so cache does not need updating.
    /// </summary>
    public void ReplaceRoadTileAt(Vector2Int gridPos, GameObject newPrefab, bool keepInterstateTint)
    {
        GameObject cell = gridManager.GetGridCell(new Vector2(gridPos.x, gridPos.y));
        if (cell == null) return;
        Cell cellComponent = gridManager.GetCell(gridPos.x, gridPos.y);
        if (cellComponent == null) return;

        string terrainPrefabName = null;
        string fallbackNonRoad = null;
        var toDestroy = new List<GameObject>();
        foreach (Transform child in cell.transform)
        {
            Zone zone = child.GetComponent<Zone>();
            if (zone != null && zone.zoneType == Zone.ZoneType.Road)
                toDestroy.Add(child.gameObject);
            else
            {
                string name = child.gameObject.name.Replace("(Clone)", "");
                if (name.Contains("Slope") || name.Contains("Grass"))
                    terrainPrefabName = name;
                else if (fallbackNonRoad == null)
                    fallbackNonRoad = name;
            }
        }
        if (terrainPrefabName == null)
            terrainPrefabName = fallbackNonRoad;
        foreach (GameObject go in toDestroy)
            Destroy(go);

        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos = GetRoadTileWorldPosition(gridPos.x, gridPos.y, newPrefab, terrainHeight);

        if (keepInterstateTint && gridManager != null)
        {
            string terrainPrefab = terrainPrefabName ?? "null";
            TerrainSlopeType slopeType = terrainManager != null ? terrainManager.GetTerrainSlopeTypeAt(gridPos.x, gridPos.y) : TerrainSlopeType.Flat;
            string roadReason = slopeType == TerrainSlopeType.Flat
                ? "Flat terrain => flat road (RoadVertical/RoadHorizontal)"
                : $"Slope {slopeType} => slope road prefab";
            string terrainReason = slopeType == TerrainSlopeType.Flat
                ? "All neighbors same height => flat grass"
                : $"Neighbor height differences => slope prefab";
            Debug.Log($"[Interstate Result] ({gridPos.x},{gridPos.y}) roadPrefab={(newPrefab != null ? newPrefab.name : "null")} terrainPrefab={terrainPrefab} slopeType={slopeType} h={terrainHeight}");
            Debug.Log($"[Interstate Result] ({gridPos.x},{gridPos.y}) WHY road: {roadReason}. WHY terrain: {terrainReason}");
        }

        GameObject roadTile = Instantiate(newPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);

        if (keepInterstateTint)
            roadTile.GetComponent<SpriteRenderer>().color = new Color(0.78f, 0.78f, 0.88f, 1f);
        else
            roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone roadZone = roadTile.AddComponent<Zone>();
        roadZone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);

        gridManager.SetRoadSortingOrder(roadTile, gridPos.x, gridPos.y);
    }
    #endregion
}
}
