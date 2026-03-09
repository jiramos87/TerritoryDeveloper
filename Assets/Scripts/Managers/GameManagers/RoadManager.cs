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
    #endregion

    #region Road Drawing State
    private bool isDrawingRoad = false;
    private Vector2 startPosition;
    private static readonly int[] DirX = { 1, -1, 0, 0 };
    private static readonly int[] DirY = { 0, 0, 1, -1 };
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
    private List<Vector2> adjacentRoadTiles = new List<Vector2>();

    /// <summary>
    /// Populates the road tile prefabs list from the individual prefab fields.
    /// </summary>
    public void Initialize()
    {
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
        if (calculateCost)
        {
            int totalCost = CalculateTotalCost(previewRoadGridPositions.Count);

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

        for (int i = 0; i < previewRoadGridPositions.Count; i++)
        {
            Vector2 gridPos = previewRoadGridPositions[i];

            PlaceRoadTile(gridPos, i, false);

            UpdateAdjacentRoadPrefabs(gridPos, i);
        }

        if (calculateCost)
        {
            int roadPowerConsumption = previewRoadGridPositions.Count * ZoneAttributes.Road.PowerConsumption;
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
        foreach (Vector2 adjacentRoadTile in adjacentRoadTiles)
        {
            if (!isAdjacentRoadInPreview(adjacentRoadTile))
            {
                RefreshRoadPrefabAt(adjacentRoadTile);
            }
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

        bool hasLeft = IsRoadAt(gridPos + new Vector2(-1, 0));
        bool hasRight = IsRoadAt(gridPos + new Vector2(1, 0));
        bool hasUp = IsRoadAt(gridPos + new Vector2(0, 1));
        bool hasDown = IsRoadAt(gridPos + new Vector2(0, -1));

        Vector2 prevGridPos = gridPos;
        if (hasLeft && hasRight && !hasUp && !hasDown)
            prevGridPos = gridPos + new Vector2(-1, 0);
        else if (hasUp && hasDown && !hasLeft && !hasRight)
            prevGridPos = gridPos + new Vector2(0, -1);
        else
        {
            Vector2[] dirs = { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(0, -1) };
            foreach (Vector2 d in dirs)
            {
                if (IsRoadAt(gridPos + d))
                {
                    prevGridPos = gridPos + d;
                    break;
                }
            }
        }

        GameObject correctRoadPrefab = GetCorrectRoadPrefab(prevGridPos, gridPos, false, false);
        DestroyPreviousRoadTile(cell, gridPos);

        Cell cellComponent = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        if (cellComponent == null) return;
        cellComponent.RemoveForestForBuilding();
        int roadPlacedAtHeight = 0;
        int terrainHeight = cellComponent.GetCellInstanceHeight();

        Vector2 worldPos;
        if (terrainHeight == 0)
        {
            roadPlacedAtHeight = 1;
            worldPos = gridManager.GetWorldPositionVector((int)gridPos.x, (int)gridPos.y, roadPlacedAtHeight);
        }
        else
        {
            worldPos = gridManager.GetWorldPosition((int)gridPos.x, (int)gridPos.y);
        }

        GameObject roadTile = Instantiate(correctRoadPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);
        roadTile.GetComponent<SpriteRenderer>().color = cellComponent.isInterstate
            ? new Color(0.78f, 0.78f, 0.88f, 1f)
            : new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;

        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);
        gridManager.SetRoadSortingOrder(roadTile, (int)gridPos.x, (int)gridPos.y);
    }

    void DrawPreviewLine(List<Vector2> path)
    {
        ClearPreview();

        // Filter to in-bounds cells to avoid null reference and preserve connectivity
        List<Vector2> filteredPath = new List<Vector2>();
        for (int i = 0; i < path.Count; i++)
        {
            Vector2 gridPos = path[i];
            if (gridManager.GetCell((int)gridPos.x, (int)gridPos.y) != null)
                filteredPath.Add(gridPos);
        }

        for (int i = 0; i < filteredPath.Count; i++)
        {
            Vector2 gridPos = filteredPath[i];
            DrawPreviewRoadTile(gridPos, filteredPath, i, true);
        }
    }

    /// <summary>
    /// Returns grid cells from start to end using Bresenham. Diagonal steps are split into
    /// two cardinal steps (staircase) so road placement matches interstate-style elbows.
    /// </summary>
    List<Vector2> GetLine(Vector2 start, Vector2 end)
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

    void ClearPreview(bool isEnd = false)
    {
        foreach (GameObject previewTile in previewRoadTiles)
        {
            Destroy(previewTile);
        }
        previewRoadTiles.Clear();
        previewRoadGridPositions.Clear();
    }

    void DrawPreviewRoadTile(Vector2 gridPos, List<Vector2> path, int i, bool isCenterRoadTile = true)
    {
        Cell cell = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        if (cell == null)
            return;

        Vector2 prevGridPos = i == 0
            ? (path.Count > 1 ? 2 * gridPos - path[1] : gridPos)
            : path[i - 1];

        bool isPreview = true;

        GameObject roadPrefab = GetCorrectRoadPrefab(prevGridPos, gridPos, isCenterRoadTile, isPreview, path, i);

        int roadPlacedAtHeight = 0;
        int terrainHeight = cell.GetCellInstanceHeight();
        Vector2 worldPos;

        if (terrainHeight == 0)
        {
            roadPlacedAtHeight = 1;
            worldPos = gridManager.GetWorldPositionVector((int)gridPos.x, (int)gridPos.y, roadPlacedAtHeight);
        }
        else
        {
            worldPos = gridManager.GetWorldPosition((int)gridPos.x, (int)gridPos.y);
        }

        GameObject previewTile = Instantiate(
            roadPrefab,
            worldPos,
            Quaternion.identity
        );

        SetPreviewRoadTileDetails(previewTile);

        previewRoadTiles.Add(previewTile);

        previewRoadGridPositions.Add(new Vector2(gridPos.x, gridPos.y));

        previewTile.transform.SetParent(cell.gameObject.transform);
    }
    #endregion

    #region Road Prefab Selection
    GameObject GetCorrectRoadPrefab(Vector2 prevGridPos, Vector2 currGridPos, bool isCenterRoadTile = true, bool isPreview = false, List<Vector2> path = null, int pathIndex = -1)
    {
        Vector2 direction = currGridPos - prevGridPos;
        Cell cell = gridManager.GetCell((int)currGridPos.x, (int)currGridPos.y);
        if (cell == null)
            return roadTilePrefab1;

        int height = cell.GetCellInstanceHeight();

        // Diagonal direction: use elbow prefab (same logic as interstate)
        int dx = Mathf.RoundToInt(direction.x);
        int dy = Mathf.RoundToInt(direction.y);
        if (dx != 0 && dy != 0)
        {
            if (dx == 1 && dy == 1) return roadTilePrefabElbowUpLeft;
            if (dx == 1 && dy == -1) return roadTilePrefabElbowDownLeft;
            if (dx == -1 && dy == 1) return roadTilePrefabElbowUpRight;
            if (dx == -1 && dy == -1) return roadTilePrefabElbowDownRight;
        }

        if (isPreview && path != null && pathIndex >= 0 && pathIndex < path.Count)
        {
            bool pathLeft = IsPathNeighbor(path, pathIndex, -1, 0);
            bool pathRight = IsPathNeighbor(path, pathIndex, 1, 0);
            bool pathUp = IsPathNeighbor(path, pathIndex, 0, 1);
            bool pathDown = IsPathNeighbor(path, pathIndex, 0, -1);
            if (pathLeft || pathRight || pathUp || pathDown)
            {
                if (pathLeft && pathUp && !pathRight && !pathDown) return roadTilePrefabElbowDownRight;
                if (pathRight && pathUp && !pathLeft && !pathDown) return roadTilePrefabElbowDownLeft;
                if (pathLeft && pathDown && !pathRight && !pathUp) return roadTilePrefabElbowUpRight;
                if (pathRight && pathDown && !pathLeft && !pathUp) return roadTilePrefabElbowUpLeft;
            }
        }

        if (isPreview)
        {
            if (height == 0)
            {
                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                    return roadTileBridgeHorizontal;
                return roadTileBridgeVertical;
            }
            Vector2? slopeDir = GetTerrainSlopeDirection(currGridPos, height);
            if (slopeDir.HasValue)
            {
                GameObject slopePrefab = GetSlopePrefabForDirection(slopeDir.Value);
                if (slopePrefab != null) return slopePrefab;
            }

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                if (height == 0)
                {
                    return roadTileBridgeHorizontal;
                }
                return roadTilePrefab2;
            }
            else
            {
                if (height == 0)
                {
                    return roadTileBridgeVertical;
                }
                return roadTilePrefab1;
            }
        }

        bool hasLeft = IsRoadAt(currGridPos + new Vector2(-1, 0));
        bool hasRight = IsRoadAt(currGridPos + new Vector2(1, 0));
        bool hasUp = IsRoadAt(currGridPos + new Vector2(0, 1));
        bool hasDown = IsRoadAt(currGridPos + new Vector2(0, -1));

        if (isCenterRoadTile)
        {
            UpdateAdjacentRoadTilesArray(currGridPos, hasLeft, hasRight, hasUp, hasDown, isPreview);
        }

        if (hasLeft && hasRight && hasUp && hasDown)
        {
            return roadTilePrefabCrossing;
        }
        else if (hasLeft && hasRight && hasUp && !hasDown)
        {
            return roadTilePrefabTIntersectionDown;
        }
        else if (hasLeft && hasRight && hasDown && !hasUp)
        {
            return roadTilePrefabTIntersectionUp;
        }
        else if (hasUp && hasDown && hasLeft && !hasRight)
        {
            return roadTilePrefabTIntersectionRight;
        }
        else if (hasUp && hasDown && hasRight && !hasLeft)
        {
            return roadTilePrefabTIntersectionLeft;
        }
        else if (hasLeft && hasUp && !hasRight && !hasDown)
        {
            return roadTilePrefabElbowDownRight;
        }
        else if (hasRight && hasUp && !hasLeft && !hasDown)
        {
            return roadTilePrefabElbowDownLeft;
        }
        else if (hasLeft && hasDown && !hasRight && !hasUp)
        {
            return roadTilePrefabElbowUpRight;
        }
        else if (hasRight && hasDown && !hasLeft && !hasUp)
        {
            return roadTilePrefabElbowUpLeft;
        }
        else if (hasLeft && hasRight && !hasUp && !hasDown)
        {
            GameObject slopePrefab = TryGetSlopePrefabForStraightSegment(currGridPos, height, true);
            if (slopePrefab != null) return slopePrefab;
            if (height == 0) return roadTileBridgeHorizontal;
            return roadTilePrefab2;
        }
        else if (hasUp && hasDown && !hasLeft && !hasRight)
        {
            GameObject slopePrefab = TryGetSlopePrefabForStraightSegment(currGridPos, height, false);
            if (slopePrefab != null) return slopePrefab;
            if (height == 0) return roadTileBridgeVertical;
            return roadTilePrefab1;
        }
        else if (hasLeft || hasRight)
        {
            GameObject slopePrefab = TryGetSlopePrefabForStraightSegment(currGridPos, height, true);
            if (slopePrefab != null) return slopePrefab;
            if (height == 0)
            {
                return roadTileBridgeHorizontal;
            }
            return roadTilePrefab2;
        }
        else if (hasUp || hasDown)
        {
            GameObject slopePrefab = TryGetSlopePrefabForStraightSegment(currGridPos, height, false);
            if (slopePrefab != null) return slopePrefab;
            if (height == 0)
            {
                return roadTileBridgeVertical;
            }
            return roadTilePrefab1;
        }

        // If no intersection or elbow, fall back to horizontal/vertical

        GameObject fallbackSlope = TryGetSlopePrefabForCell(currGridPos, height);
        if (fallbackSlope != null) return fallbackSlope;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (height == 0)
            {
                return roadTileBridgeHorizontal;
            }
            return roadTilePrefab2;
        }
        else
        {
            if (height == 0)
            {
                return roadTileBridgeVertical;
            }
            return roadTilePrefab1;
        }
    }

    /// <summary>
    /// True if the path has a neighbor at curr + (offsetX, offsetY) — used for preview elbow detection.
    /// </summary>
    bool IsPathNeighbor(List<Vector2> path, int pathIndex, int offsetX, int offsetY)
    {
        if (path == null || pathIndex < 0 || pathIndex >= path.Count) return false;
        Vector2 curr = path[pathIndex];
        Vector2 neighbor = new Vector2(curr.x + offsetX, curr.y + offsetY);
        if (pathIndex > 0 && path[pathIndex - 1] == neighbor) return true;
        if (pathIndex < path.Count - 1 && path[pathIndex + 1] == neighbor) return true;
        return false;
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
    /// Maps grid cardinal direction (from current cell toward the higher neighbor) to slope road prefab.
    /// Prefab names follow visual slope direction. Grid axes don't match visual N/S/E/W 1:1, so we swap:
    /// (1,0) and (-1,0) map to South/North prefabs; (0,1) and (0,-1) map to East/West prefabs.
    /// </summary>
    GameObject GetSlopePrefabForDirection(Vector2 cardinalDirection)
    {
        int dx = Mathf.RoundToInt(cardinalDirection.x);
        int dy = Mathf.RoundToInt(cardinalDirection.y);
        if (dx == 1 && dy == 0) return roadTilePrefabSouthSlope;
        if (dx == -1 && dy == 0) return roadTilePrefabNorthSlope;
        if (dx == 0 && dy == 1) return roadTilePrefabEastSlope;
        if (dx == 0 && dy == -1) return roadTilePrefabWestSlope;
        return null;
    }

    GameObject TryGetSlopePrefabForCell(Vector2 currGridPos, int currentHeight)
    {
        Vector2? slopeDir = GetTerrainSlopeDirection(currGridPos, currentHeight);
        if (!slopeDir.HasValue) return null;
        return GetSlopePrefabForDirection(slopeDir.Value);
    }

    /// <summary>
    /// Returns slope road prefab only when terrain slope direction is parallel to the road line.
    /// For horizontal lines use slope only if terrain slopes E-W; for vertical lines only if N-S.
    /// Prevents lateral slopes from showing wrong slope prefab on straight segments.
    /// </summary>
    GameObject TryGetSlopePrefabForStraightSegment(Vector2 currGridPos, int currentHeight, bool isHorizontalLine)
    {
        Vector2? slopeDir = GetTerrainSlopeDirection(currGridPos, currentHeight);
        if (!slopeDir.HasValue) return null;
        int dx = Mathf.RoundToInt(slopeDir.Value.x);
        int dy = Mathf.RoundToInt(slopeDir.Value.y);
        bool slopeParallelToLine = isHorizontalLine ? (dx != 0 && dy == 0) : (dx == 0 && dy != 0);
        if (!slopeParallelToLine) return null;
        return GetSlopePrefabForDirection(slopeDir.Value);
    }

    /// <summary>
    /// Returns the correct road prefab, world position and sorting order for the single-cell ghost preview at the given grid position.
    /// Used when hovering with the road tool (no line drawn): slope cells get slope prefab, water gets bridge at height 1, else flat road.
    /// </summary>
    public void GetRoadGhostPreviewForCell(Vector2 gridPos, out GameObject prefab, out Vector2 worldPos, out int sortingOrder)
    {
        int x = (int)gridPos.x;
        int y = (int)gridPos.y;
        prefab = roadTilePrefab1;
        worldPos = gridManager.GetWorldPosition(x, y);
        sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, 0);

        Cell cell = gridManager.GetCell((int)gridPos.x, (int)gridPos.y);
        if (cell == null) return;

        int height = cell.GetCellInstanceHeight();

        if (height == 0)
        {
            prefab = roadTileBridgeVertical;
            worldPos = gridManager.GetWorldPositionVector(x, y, 1);
            sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, 1);
            return;
        }

        GameObject slopePrefab = TryGetSlopePrefabForCell(gridPos, height);
        if (slopePrefab != null)
        {
            prefab = slopePrefab;
            worldPos = gridManager.GetWorldPosition(x, y);
            sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, height);
            return;
        }

        prefab = roadTilePrefab1;
        worldPos = gridManager.GetWorldPosition(x, y);
        sortingOrder = gridManager.GetRoadSortingOrderForCell(x, y, height);
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

    void UpdateAdjacentRoadTilesArray(Vector2 currGridPos, bool hasLeft, bool hasRight, bool hasUp, bool hasDown, bool isPreview)
    {
        adjacentRoadTiles.Clear();

        if (hasLeft)
        {
            adjacentRoadTiles.Add(new Vector2(currGridPos.x - 1, currGridPos.y));
        }
        if (hasRight)
        {
            adjacentRoadTiles.Add(new Vector2(currGridPos.x + 1, currGridPos.y));
        }
        if (hasUp)
        {
            adjacentRoadTiles.Add(new Vector2(currGridPos.x, currGridPos.y + 1));
        }
        if (hasDown)
        {
            adjacentRoadTiles.Add(new Vector2(currGridPos.x, currGridPos.y - 1));
        }
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
        int roadPlacedAtHeight = 0;
        int terrainHeight = cellComponent.GetCellInstanceHeight();

        Vector2 worldPos;
        if (terrainHeight == 0)
        {
            roadPlacedAtHeight = 1;
            worldPos = gridManager.GetWorldPositionVector((int)gridPos.x, (int)gridPos.y, roadPlacedAtHeight);
        }
        else
        {
            worldPos = gridManager.GetWorldPosition((int)gridPos.x, (int)gridPos.y);
        }

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
                if (zone != null)
                    toDestroy.Add((child.gameObject, zone));
            }
            foreach (var t in toDestroy)
            {
                if (t.zone.zoneCategory == Zone.ZoneCategory.Zoning)
                    zoneManager.removeZonedPositionFromList(gridPos, t.zone.zoneType);
                if (t.zone.zoneType == Zone.ZoneType.Road)
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
        int roadPlacedAtHeight = 0;
        int terrainHeight = cellComponent.GetCellInstanceHeight();

        Vector2 worldPos;
        if (terrainHeight == 0)
        {
            roadPlacedAtHeight = 1;
            worldPos = gridManager.GetWorldPositionVector((int)gridPos.x, (int)gridPos.y, roadPlacedAtHeight);
        }
        else
        {
            worldPos = gridManager.GetWorldPosition((int)gridPos.x, (int)gridPos.y);
        }

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
    /// </summary>
    public GameObject GetCorrectRoadPrefabForPath(Vector2 prevGridPos, Vector2 currGridPos)
    {
        return GetCorrectRoadPrefab(prevGridPos, currGridPos, true, false);
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
        Vector2 worldPos;
        if (terrainHeight == 0)
            worldPos = gridManager.GetWorldPositionVector(gx, gy, 1);
        else
            worldPos = gridManager.GetWorldPosition(gx, gy);

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
        Vector2 worldPos;
        if (terrainHeight == 0)
            worldPos = gridManager.GetWorldPositionVector(gridPos.x, gridPos.y, 1);
        else
            worldPos = gridManager.GetWorldPosition(gridPos.x, gridPos.y);

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

        var toDestroy = new List<GameObject>();
        foreach (Transform child in cell.transform)
        {
            if (child.GetComponent<Zone>() != null)
                toDestroy.Add(child.gameObject);
        }
        foreach (GameObject go in toDestroy)
            Destroy(go);

        int terrainHeight = cellComponent.GetCellInstanceHeight();
        Vector2 worldPos;
        if (terrainHeight == 0)
            worldPos = gridManager.GetWorldPositionVector(gridPos.x, gridPos.y, 1);
        else
            worldPos = gridManager.GetWorldPosition(gridPos.x, gridPos.y);

        GameObject roadTile = Instantiate(newPrefab, worldPos, Quaternion.identity);
        roadTile.transform.SetParent(cellComponent.gameObject.transform);

        if (keepInterstateTint)
            roadTile.GetComponent<SpriteRenderer>().color = new Color(0.78f, 0.78f, 0.88f, 1f);
        else
            roadTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);

        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
        UpdateRoadCellAttributes(cellComponent, roadTile, Zone.ZoneType.Road);

        gridManager.SetRoadSortingOrder(roadTile, gridPos.x, gridPos.y);
    }
    #endregion
}
}
