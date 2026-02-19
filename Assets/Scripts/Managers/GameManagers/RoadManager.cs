using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;

public class RoadManager : MonoBehaviour
{
    public TerrainManager terrainManager;
    public GridManager gridManager;
    public CityStats cityStats;
    public UIManager uiManager;
    public ZoneManager zoneManager;

    private bool isDrawingRoad = false;
    private Vector2 startPosition;

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

    public void HandleRoadDrawing(Vector2 gridPosition)
    {
        if (!terrainManager.CanPlaceRoad((int)gridPosition.x, (int)gridPosition.y))
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isDrawingRoad = true;
            startPosition = gridPosition;
            if (uiManager != null)
            {
                uiManager.HideGhostPreview();
            }
        }
        else if (isDrawingRoad && Input.GetMouseButton(0))
        {
            Vector3 currentMousePosition = gridPosition;
            DrawPreviewLine(startPosition, currentMousePosition);
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

        if (Input.GetMouseButtonDown(1))
        {
            isDrawingRoad = false;
            ClearPreview();
            if (uiManager != null)
            {
                uiManager.RestoreGhostPreview();
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
            bool isAdjacent = true;

            if (!isAdjacentRoadInPreview(adjacentRoadTile))
            {
                PlaceRoadTile(adjacentRoadTile, i, isAdjacent);
            }
        }
    }

    void DrawPreviewLine(Vector2 start, Vector2 end)
    {
        ClearPreview();
        List<Vector2> path = GetLine(start, end);

        for (int i = 0; i < path.Count; i++)
        {
            Vector2 gridPos = path[i];

            DrawPreviewRoadTile(gridPos, path, i, true);
        }
    }

    List<Vector2> GetLine(Vector2 start, Vector2 end)
    {
        List<Vector2> line = new List<Vector2>();

        int x0 = (int)start.x;
        int y0 = (int)start.y;
        int x1 = (int)end.x;
        int y1 = (int)end.y;

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
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
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
        Vector2 prevGridPos = i == 0 ? (path.Count > 1 ? path[1] : gridPos) : path[i - 1];

        bool isPreview = true;

        GameObject roadPrefab = GetCorrectRoadPrefab(prevGridPos, gridPos, isCenterRoadTile, isPreview, path, i);

        Cell cell = gridManager.GetGridCell(gridPos).GetComponent<Cell>();
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

    GameObject GetCorrectRoadPrefab(Vector2 prevGridPos, Vector2 currGridPos, bool isCenterRoadTile = true, bool isPreview = false, List<Vector2> path = null, int pathIndex = -1)
    {
        Vector2 direction = currGridPos - prevGridPos;
        Cell cell = gridManager.GetGridCell(currGridPos).GetComponent<Cell>();
        int height = cell.GetCellInstanceHeight();

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
        else if (hasLeft || hasRight)
        {
            GameObject slopePrefab = TryGetSlopePrefabForCell(currGridPos, height);
            if (slopePrefab != null) return slopePrefab;
            if (height == 0)
            {
                return roadTileBridgeHorizontal;
            }
            return roadTilePrefab2;
        }

        else if (hasUp || hasDown)
        {
            GameObject slopePrefab = TryGetSlopePrefabForCell(currGridPos, height);
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
    /// Returns the height of the neighbor at (gridX + dx, gridY + dy), or int.MinValue if out of bounds.
    /// Only use cardinal offsets: (dx, dy) one of (±1, 0) or (0, ±1).
    /// </summary>
    int GetNeighborHeight(int gridX, int gridY, int dx, int dy)
    {
        int nx = gridX + dx;
        int ny = gridY + dy;
        if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height)
            return int.MinValue;
        GameObject neighborCell = gridManager.GetGridCell(new Vector2(nx, ny));
        if (neighborCell == null) return int.MinValue;
        Cell c = neighborCell.GetComponent<Cell>();
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
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            int nh = GetNeighborHeight(x, y, dx[i], dy[i]);
            if (nh == int.MinValue) continue;
            int diff = nh - currentHeight;
            if (diff == 1)
                directionToHigher = new Vector2(dx[i], dy[i]);
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

        var cellComponent = cell.GetComponent<Cell>();
        if (cellComponent?.zoneType == Zone.ZoneType.Road) return true;

        return cell.transform
            .Cast<Transform>()
            .Select(child => child.GetComponent<Zone>())
            .Any(zone => zone != null && zone.zoneType == Zone.ZoneType.Road);
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
        SetPreviewTileCollider(previewTile);
        gridManager.SetTileSortingOrder(previewTile, Zone.ZoneType.Road);

        SetRoadTileZoneDetails(previewTile);
        previewTile.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);
    }

    void SetPreviewTileCollider(GameObject previewTile)
    {
        PolygonCollider2D collider = previewTile.AddComponent<PolygonCollider2D>();
        collider.points = GetRoadColliderPoints();
        collider.isTrigger = true;
    }

    void SetRoadTileZoneDetails(GameObject roadTile)
    {
        Zone zone = roadTile.AddComponent<Zone>();
        zone.zoneType = Zone.ZoneType.Road;
    }

    Vector2[] GetRoadColliderPoints()
    {
        Vector2[] points = new Vector2[4];
        points[0] = new Vector2(-0.5f, 0f);
        points[1] = new Vector2(0f, 0.25f);
        points[2] = new Vector2(0.5f, 0f);
        points[3] = new Vector2(0f, -0.25f);

        return points;
    }

    void PlaceRoadTile(Vector2 gridPos, int i = 0, bool isAdjacent = false)
    {
        if (gridManager.IsCellOccupiedByBuilding((int)gridPos.x, (int)gridPos.y))
            return;

        GameObject cell = gridManager.GetGridCell(gridPos);

        bool isCenterRoadTile = !isAdjacent;
        bool isPreview = false;

        Vector2 prevGridPos = isAdjacent
            ? (i == 0 ? gridPos : previewRoadGridPositions[i - 1])
            : new Vector2(0, 0);

        GameObject correctRoadPrefab = GetCorrectRoadPrefab(
            prevGridPos,
            gridPos,
            isCenterRoadTile,
            isPreview
        );

        DestroyPreviousRoadTile(cell, gridPos);

        Cell cellComponent = cell.GetComponent<Cell>();
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
    }

    void DestroyPreviousRoadTile(GameObject cell, Vector2 gridPos)
    {
        if (cell.transform.childCount > 0)
        {
            foreach (Transform child in cell.transform)
            {
                Zone zone = child.GetComponent<Zone>();
                if (zone != null)
                {
                    DestroyImmediate(child.gameObject);
                    if (zone.zoneCategory == Zone.ZoneCategory.Zoning)
                    {
                        zoneManager.removeZonedPositionFromList(gridPos, zone.zoneType);
                    }
                }
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

    public List<GameObject> GetRoadPrefabs()
    {
        return roadTilePrefabs;
    }
}
