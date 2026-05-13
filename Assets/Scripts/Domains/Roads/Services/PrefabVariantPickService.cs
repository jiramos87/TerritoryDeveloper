using UnityEngine;
using Territory.Core;
using Territory.Terrain;
using Territory.Roads;

namespace Domains.Roads.Services
{
/// <summary>
/// Prefab variant selection by slope, terrain type, and world-position anchoring.
/// Extracted from PrefabResolverService (Stage 7.3 Tier-E split).
/// </summary>
internal class PrefabVariantPickService
{
    private readonly IGridManager gridManager;
    private readonly ITerrainManager terrainManager;
    private readonly IRoadManager roadManager;
    private readonly PrefabLookupService lookup;

    private static readonly int[] DirX = { 1, -1, 0, 0 };
    private static readonly int[] DirY = { 0, 0, 1, -1 };

    internal PrefabVariantPickService(IGridManager grid, ITerrainManager terrain, IRoadManager roads, PrefabLookupService lookupSvc)
    {
        gridManager = grid;
        terrainManager = terrain;
        roadManager = roads;
        lookup = lookupSvc;
    }

    internal bool IsCardinalSlopeRoadPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        return prefab == roadManager.roadTilePrefabNorthSlope || prefab == roadManager.roadTilePrefabSouthSlope
            || prefab == roadManager.roadTilePrefabEastSlope || prefab == roadManager.roadTilePrefabWestSlope;
    }

    internal bool IsDiagonalRoadPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        return prefab == roadManager.roadTilePrefabElbowUpLeft || prefab == roadManager.roadTilePrefabElbowUpRight
            || prefab == roadManager.roadTilePrefabElbowDownLeft || prefab == roadManager.roadTilePrefabElbowDownRight;
    }

    internal static bool TerrainSlopeIsDiagonalOrCornerUp(TerrainSlopeType slopeType)
    {
        return slopeType == TerrainSlopeType.NorthEast || slopeType == TerrainSlopeType.NorthWest
            || slopeType == TerrainSlopeType.SouthEast || slopeType == TerrainSlopeType.SouthWest
            || slopeType == TerrainSlopeType.NorthEastUp || slopeType == TerrainSlopeType.NorthWestUp
            || slopeType == TerrainSlopeType.SouthEastUp || slopeType == TerrainSlopeType.SouthWestUp;
    }

    internal GameObject TrySlopeFromPostTerraform(TerrainSlopeType postSlope, bool isHorizontal)
    {
        if (postSlope == TerrainSlopeType.Flat) return null;
        switch (postSlope)
        {
            case TerrainSlopeType.North: return roadManager.roadTilePrefabNorthSlope;
            case TerrainSlopeType.South: return roadManager.roadTilePrefabSouthSlope;
            case TerrainSlopeType.East: return roadManager.roadTilePrefabEastSlope;
            case TerrainSlopeType.West: return roadManager.roadTilePrefabWestSlope;
            case TerrainSlopeType.SouthEastUp: return isHorizontal ? roadManager.roadTilePrefabEastSlope : roadManager.roadTilePrefabSouthSlope;
            case TerrainSlopeType.NorthEastUp: return isHorizontal ? roadManager.roadTilePrefabEastSlope : roadManager.roadTilePrefabNorthSlope;
            case TerrainSlopeType.SouthWestUp: return isHorizontal ? roadManager.roadTilePrefabWestSlope : roadManager.roadTilePrefabSouthSlope;
            case TerrainSlopeType.NorthWestUp: return isHorizontal ? roadManager.roadTilePrefabWestSlope : roadManager.roadTilePrefabNorthSlope;
            default: return null;
        }
    }

    internal GameObject TrySlopeForStraight(TerrainSlopeType postSlope, bool isHorizontal)
    {
        if (postSlope == TerrainSlopeType.Flat) return null;
        bool isOrthogonal = postSlope == TerrainSlopeType.North || postSlope == TerrainSlopeType.South
            || postSlope == TerrainSlopeType.East || postSlope == TerrainSlopeType.West;
        bool isCornerSlope = postSlope == TerrainSlopeType.SouthEastUp || postSlope == TerrainSlopeType.NorthEastUp
            || postSlope == TerrainSlopeType.SouthWestUp || postSlope == TerrainSlopeType.NorthWestUp;
        if (!isOrthogonal && !isCornerSlope) return null;
        return TrySlopeFromPostTerraform(postSlope, isHorizontal);
    }

    internal GameObject TryGetSlopePrefabForCell(Vector2 currGridPos, int currentHeight)
    {
        Vector2? slopeDir = GetTerrainSlopeDirection(currGridPos, currentHeight);
        if (slopeDir.HasValue) return GetSlopePrefabForDirection(slopeDir.Value);
        if (terrainManager == null) return null;
        int x = (int)currGridPos.x;
        int y = (int)currGridPos.y;
        TerrainSlopeType slopeType = terrainManager.GetTerrainSlopeTypeAt(x, y);
        Vector2? diagonalFallback = GetOrthogonalDirectionForDiagonalSlope(slopeType);
        if (diagonalFallback.HasValue) return GetSlopePrefabForDirection(diagonalFallback.Value);
        return null;
    }

    internal GameObject TryGetSlopePrefabForStraightSegment(Vector2 currGridPos, int currentHeight, bool isHorizontalLine, Vector2? neighborAlongRoad = null)
    {
        if (terrainManager == null) return null;
        int x = (int)currGridPos.x;
        int y = (int)currGridPos.y;
        TerrainSlopeType slopeType = terrainManager.GetTerrainSlopeTypeAt(x, y);
        if (slopeType == TerrainSlopeType.Flat) return null;

        bool isDiagonalSlope = slopeType == TerrainSlopeType.SouthEast || slopeType == TerrainSlopeType.SouthWest
            || slopeType == TerrainSlopeType.NorthEast || slopeType == TerrainSlopeType.NorthWest
            || slopeType == TerrainSlopeType.NorthEastUp || slopeType == TerrainSlopeType.NorthWestUp
            || slopeType == TerrainSlopeType.SouthEastUp || slopeType == TerrainSlopeType.SouthWestUp;

        if (isDiagonalSlope)
        {
            if (neighborAlongRoad.HasValue)
            {
                GameObject travelRamp = TryRampRoadPrefabFromPrevTravel(currGridPos, currentHeight, neighborAlongRoad.Value);
                if (travelRamp != null) return travelRamp;
            }

            Vector2? diagonalDir = null;
            bool isDiagonalDownslope = slopeType == TerrainSlopeType.SouthEast || slopeType == TerrainSlopeType.SouthWest
                || slopeType == TerrainSlopeType.NorthEast || slopeType == TerrainSlopeType.NorthWest;

            if (isDiagonalDownslope && neighborAlongRoad.HasValue)
            {
                int nx = (int)neighborAlongRoad.Value.x;
                int ny = (int)neighborAlongRoad.Value.y;
                if (nx >= 0 && nx < gridManager.width && ny >= 0 && ny < gridManager.height)
                {
                    TerrainSlopeType neighborSlope = terrainManager.GetTerrainSlopeTypeAt(nx, ny);
                    if (IsComplementaryDiagonalPair(slopeType, neighborSlope))
                    {
                        int neighborHeight = lookup.GetNeighborHeight(x, y, nx - x, ny - y);
                        if (neighborHeight != int.MinValue)
                        {
                            TerrainSlopeType lowerCellSlope = currentHeight <= neighborHeight ? slopeType : neighborSlope;
                            diagonalDir = GetOrthogonalDirectionForDiagonalSlope(lowerCellSlope, isHorizontalLine);
                        }
                    }
                }
            }

            if (!diagonalDir.HasValue) diagonalDir = GetOrthogonalDirectionForDiagonalSlope(slopeType, isHorizontalLine);
            if (diagonalDir.HasValue) return GetSlopePrefabForDirection(diagonalDir.Value);
            return null;
        }

        Vector2? slopeDir = GetTerrainSlopeDirection(currGridPos, currentHeight);
        if (slopeDir.HasValue)
        {
            int dx = Mathf.RoundToInt(slopeDir.Value.x);
            int dy = Mathf.RoundToInt(slopeDir.Value.y);
            bool slopeParallelToLine = isHorizontalLine ? (dx != 0 && dy == 0) : (dx == 0 && dy != 0);
            if (slopeParallelToLine) return GetSlopePrefabForDirection(slopeDir.Value);
        }
        return null;
    }

    internal Vector2 GetWorldPositionForPrefab(int x, int y, GameObject prefab, int terrainHeight, TerrainSlopeType slopeType)
    {
        if (terrainHeight == 0) return gridManager.GetWorldPositionVector(x, y, 1);

        bool anchorAtUpperLikeElbow = IsDiagonalRoadPrefab(prefab)
            || (IsCardinalSlopeRoadPrefab(prefab) && TerrainSlopeIsDiagonalOrCornerUp(slopeType));
        if (!anchorAtUpperLikeElbow) return gridManager.GetWorldPosition(x, y);
        if (slopeType == TerrainSlopeType.Flat) return gridManager.GetWorldPosition(x, y);

        int upperX = x, upperY = y;
        bool foundFromPlan = TryGetUpperNeighborFromSlopeType(slopeType, x, y, out upperX, out upperY);
        if (!foundFromPlan)
        {
            Vector2? slopeDir = GetTerrainSlopeDirection(new Vector2(x, y), terrainHeight);
            if (slopeDir.HasValue)
            {
                upperX = x + Mathf.RoundToInt(slopeDir.Value.x);
                upperY = y + Mathf.RoundToInt(slopeDir.Value.y);
            }
            else return gridManager.GetWorldPosition(x, y);
        }

        if (upperX < 0 || upperX >= gridManager.width || upperY < 0 || upperY >= gridManager.height)
            return gridManager.GetWorldPosition(x, y);

        CityCell upperCell = gridManager.GetCell(upperX, upperY);
        if (upperCell == null) return gridManager.GetWorldPosition(x, y);

        int upperHeight = upperCell.GetCellInstanceHeight();
        return gridManager.GetWorldPositionVector(upperX, upperY, upperHeight);
    }

    private static TerrainSlopeType GetSlopeTypeFromTravelVector(int dx, int dy)
    {
        if (Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0) return dx > 0 ? TerrainSlopeType.North : TerrainSlopeType.South;
        if (dy != 0) return dy > 0 ? TerrainSlopeType.West : TerrainSlopeType.East;
        return TerrainSlopeType.Flat;
    }

    private GameObject TryRampRoadPrefabFromPrevTravel(Vector2 currGridPos, int currentHeight, Vector2 prevGridPos)
    {
        Vector2 dir = currGridPos - prevGridPos;
        int dx = Mathf.RoundToInt(dir.x);
        int dy = Mathf.RoundToInt(dir.y);
        if (dx == 0 && dy == 0) return null;

        int px = Mathf.RoundToInt(prevGridPos.x);
        int py = Mathf.RoundToInt(prevGridPos.y);
        if (px < 0 || px >= gridManager.width || py < 0 || py >= gridManager.height) return null;
        CityCell prevCell = gridManager.GetCell(px, py);
        if (prevCell == null) return null;

        int x = Mathf.RoundToInt(currGridPos.x);
        int y = Mathf.RoundToInt(currGridPos.y);
        int nx = x + dx, ny = y + dy;
        int dSeg;
        if (nx >= 0 && nx < gridManager.width && ny >= 0 && ny < gridManager.height)
        {
            CityCell nextCell = gridManager.GetCell(nx, ny);
            dSeg = nextCell != null ? nextCell.GetCellInstanceHeight() - currentHeight : currentHeight - prevCell.GetCellInstanceHeight();
        }
        else
            dSeg = currentHeight - prevCell.GetCellInstanceHeight();

        TerrainSlopeType rampSlopeType;
        if (dSeg > 0) rampSlopeType = GetSlopeTypeFromTravelVector(-dx, -dy);
        else if (dSeg < 0) rampSlopeType = GetSlopeTypeFromTravelVector(dx, dy);
        else return null;

        if (rampSlopeType == TerrainSlopeType.Flat) return null;
        bool isHorizontal = Mathf.Abs(dx) >= Mathf.Abs(dy);
        return TrySlopeFromPostTerraform(rampSlopeType, isHorizontal);
    }

    private static bool IsComplementaryDiagonalPair(TerrainSlopeType a, TerrainSlopeType b)
    {
        return (a == TerrainSlopeType.SouthEast && b == TerrainSlopeType.NorthWest)
            || (a == TerrainSlopeType.NorthWest && b == TerrainSlopeType.SouthEast)
            || (a == TerrainSlopeType.SouthWest && b == TerrainSlopeType.NorthEast)
            || (a == TerrainSlopeType.NorthEast && b == TerrainSlopeType.SouthWest);
    }

    private Vector2? GetOrthogonalDirectionForDiagonalSlope(TerrainSlopeType slopeType, bool? isHorizontalLine = null)
    {
        switch (slopeType)
        {
            case TerrainSlopeType.NorthEast:
                if (isHorizontalLine == true) return new Vector2(0, -1);
                if (isHorizontalLine == false) return new Vector2(-1, 0);
                return new Vector2(-1, 0);
            case TerrainSlopeType.NorthWest:
                if (isHorizontalLine == true) return new Vector2(0, 1);
                if (isHorizontalLine == false) return new Vector2(-1, 0);
                return new Vector2(-1, 0);
            case TerrainSlopeType.SouthEast:
                if (isHorizontalLine == true) return new Vector2(0, -1);
                if (isHorizontalLine == false) return new Vector2(1, 0);
                return new Vector2(1, 0);
            case TerrainSlopeType.SouthWest:
                if (isHorizontalLine == true) return new Vector2(0, 1);
                if (isHorizontalLine == false) return new Vector2(1, 0);
                return new Vector2(1, 0);
            case TerrainSlopeType.SouthEastUp:
                if (isHorizontalLine == true) return new Vector2(1, 0);
                if (isHorizontalLine == false) return new Vector2(0, 1);
                return new Vector2(0, 1);
            case TerrainSlopeType.NorthEastUp:
                if (isHorizontalLine == true) return new Vector2(-1, 0);
                if (isHorizontalLine == false) return new Vector2(0, 1);
                return new Vector2(0, 1);
            case TerrainSlopeType.SouthWestUp:
                if (isHorizontalLine == true) return new Vector2(1, 0);
                if (isHorizontalLine == false) return new Vector2(0, -1);
                return new Vector2(0, -1);
            case TerrainSlopeType.NorthWestUp:
                if (isHorizontalLine == true) return new Vector2(-1, 0);
                if (isHorizontalLine == false) return new Vector2(0, -1);
                return new Vector2(0, -1);
            default: return null;
        }
    }

    internal Vector2? GetTerrainSlopeDirection(Vector2 currGridPos, int currentHeight)
    {
        if (currentHeight == 0) return null;
        int x = (int)currGridPos.x;
        int y = (int)currGridPos.y;
        Vector2? directionToHigher = null;
        for (int i = 0; i < 4; i++)
        {
            int nh = lookup.GetNeighborHeight(x, y, DirX[i], DirY[i]);
            if (nh == int.MinValue) continue;
            if (nh - currentHeight == 1) directionToHigher = new Vector2(DirX[i], DirY[i]);
        }
        if (!directionToHigher.HasValue) return null;
        int dxi = Mathf.RoundToInt(directionToHigher.Value.x);
        int dyi = Mathf.RoundToInt(directionToHigher.Value.y);
        bool isCardinal = (Mathf.Abs(dxi) == 1 && dyi == 0) || (dxi == 0 && Mathf.Abs(dyi) == 1);
        return isCardinal ? (Vector2?)directionToHigher.Value : null;
    }

    private GameObject GetSlopePrefabForDirection(Vector2 cardinalDirection)
    {
        int dx = Mathf.RoundToInt(cardinalDirection.x);
        int dy = Mathf.RoundToInt(cardinalDirection.y);
        if (dx == 1 && dy == 0) return roadManager.roadTilePrefabSouthSlope;
        if (dx == -1 && dy == 0) return roadManager.roadTilePrefabNorthSlope;
        if (dx == 0 && dy == 1) return roadManager.roadTilePrefabEastSlope;
        if (dx == 0 && dy == -1) return roadManager.roadTilePrefabWestSlope;
        return null;
    }

    private bool TryGetUpperNeighborFromSlopeType(TerrainSlopeType slopeType, int x, int y, out int upperX, out int upperY)
    {
        upperX = x; upperY = y;
        switch (slopeType)
        {
            case TerrainSlopeType.North: upperX = x + 1; upperY = y; return true;
            case TerrainSlopeType.South: upperX = x - 1; upperY = y; return true;
            case TerrainSlopeType.East: upperX = x; upperY = y - 1; return true;
            case TerrainSlopeType.West: upperX = x; upperY = y + 1; return true;
            case TerrainSlopeType.SouthEast: upperX = x + 1; upperY = y + 1; return true;
            case TerrainSlopeType.SouthWest: upperX = x + 1; upperY = y - 1; return true;
            case TerrainSlopeType.NorthEast: upperX = x - 1; upperY = y + 1; return true;
            case TerrainSlopeType.NorthWest: upperX = x - 1; upperY = y - 1; return true;
            case TerrainSlopeType.SouthEastUp: upperX = x + 1; upperY = y; return true;
            case TerrainSlopeType.NorthEastUp: upperX = x - 1; upperY = y; return true;
            case TerrainSlopeType.SouthWestUp: upperX = x + 1; upperY = y; return true;
            case TerrainSlopeType.NorthWestUp: upperX = x - 1; upperY = y; return true;
            default: return false;
        }
    }
}
}
