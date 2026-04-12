using UnityEngine;
using System.Collections.Generic;
using Territory.Zones;

namespace Territory.Core
{
/// <summary>
/// Contract for grid ops: cell access, coord conversion, grid state queries.
/// Read this interface to understand <see cref="GridManager"/> public API without full impl.
/// </summary>
public interface IGridManager
{
    Cell GetCell(int x, int y);
    bool IsValidGridPosition(Vector2 position);
    bool IsCellOccupiedByBuilding(int x, int y);
    Vector2 GetWorldPosition(int x, int y);
    Vector2 GetWorldPositionVector(int x, int y, int heightLevel);
    Vector2 GetGridPosition(Vector2 worldPoint);
    Vector2 GetCellWorldPosition(Cell cell);
    GameObject GetGridCell(Vector2 gridPosition);
    int SetTileSortingOrder(GameObject tile, Zone.ZoneType zoneType = Zone.ZoneType.Grass);
    void SetRoadSortingOrder(GameObject tile, int x, int y);
    int GetRoadSortingOrderForCell(int x, int y, int heightLevel);
    void SetZoningTileSortingOrder(GameObject tile, int x, int y);
    bool canPlaceBuilding(Vector2 position, int buildingSize);
    bool canPlaceBuilding(Vector2 position, int buildingSize, bool isWaterPlant);
    void InvalidateRoadCache();
    void AddRoadToCache(Vector2Int pos);
    void RemoveRoadFromCache(Vector2Int pos);
    List<Vector2Int> GetAllRoadPositions();
    List<Vector2Int> GetRoadEdgePositions();
    List<Vector2Int> FindPath(Vector2Int from, Vector2Int to);
    List<Vector2Int> FindPathWithRoadSpacing(Vector2Int from, Vector2Int to, int minDistanceFromRoad);
    List<Vector2Int> FindPathForAutoSimulation(Vector2Int from, Vector2Int to);
    List<Vector2Int> FindPathWithRoadSpacingForAutoSimulation(Vector2Int from, Vector2Int to, int minDistanceFromRoad);
    bool IsAdjacentToRoad(int x, int y);
    HashSet<Vector2Int> GetCellsWithinDistanceOfRoad(int maxDistance);
    bool IsWithinDistanceOfRoad(int x, int y, int maxDistance);
    int CountGrassNeighbors(int gx, int gy);
    bool DemolishCellAt(Vector2 position, bool withAnimation = true);
}
}
