using Territory.Terrain;

/// <summary>
/// Shared cost constants and helpers for road pathfinding (Interstate, AutoRoad, manual).
/// Aligns GridPathfinder and InterstateManager so both prefer flat terrain and penalize slopes consistently.
/// </summary>
public static class RoadPathCostConstants
{
    public const int Flat = 1;
    public const int CardinalSlope = 60;
    public const int DiagonalSlope = 35;
    public const int HeightDiffPenalty = 25;

    /// <summary>
    /// Returns step cost for moving onto a cell with the given terrain slope and height difference.
    /// Water cells should pass TerrainSlopeType.Flat and heightDiff 0.
    /// </summary>
    public static int GetStepCost(TerrainSlopeType slopeType, int heightDiff)
    {
        int baseCost;
        switch (slopeType)
        {
            case TerrainSlopeType.Flat: baseCost = Flat; break;
            case TerrainSlopeType.North:
            case TerrainSlopeType.South:
            case TerrainSlopeType.East:
            case TerrainSlopeType.West: baseCost = CardinalSlope; break;
            case TerrainSlopeType.NorthEast:
            case TerrainSlopeType.NorthWest:
            case TerrainSlopeType.SouthEast:
            case TerrainSlopeType.SouthWest:
            case TerrainSlopeType.NorthEastUp:
            case TerrainSlopeType.NorthWestUp:
            case TerrainSlopeType.SouthEastUp:
            case TerrainSlopeType.SouthWestUp: baseCost = DiagonalSlope; break;
            default: return int.MaxValue;
        }
        if (heightDiff == 1) baseCost += HeightDiffPenalty;
        return baseCost;
    }
}
