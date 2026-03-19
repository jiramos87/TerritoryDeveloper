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
    /// <summary>Cost for water slope cells (land adjacent to water). Path prefers to stay 1 cell from coast.</summary>
    public const int WaterSlopeCost = 500;
    /// <summary>Multiplier for slope costs in interstate pathfinding. Interstate strongly prefers flat paths over climbing hills.</summary>
    public const int InterstateSlopeMultiplier = 5;
    /// <summary>Bonus when continuing in same direction (reduces step cost). Rule E.</summary>
    public const int InterstateStraightnessBonus = 15;
    /// <summary>Penalty when changing direction (90° turn). Favors straight paths.</summary>
    public const int InterstateTurnPenalty = 5;
    /// <summary>Penalty when turning back immediately (zigzag: turn, 1 tile, turn opposite). Prefers single turn or going through water slope over S-curve.</summary>
    public const int InterstateZigzagPenalty = 500;
    /// <summary>
    /// Added when a step increases Manhattan distance to the goal. Curbs long flat detours when a shorter hill-crossing route is cheaper overall.
    /// </summary>
    public const int InterstateAwayFromGoalPenalty = 18;

    /// <summary>
    /// Returns step cost for interstate pathfinding. Uses higher cost for slopes so interstate prefers going around hills.
    /// </summary>
    public static int GetStepCostForInterstate(TerrainSlopeType slopeType, int heightDiff)
    {
        int cost = GetStepCost(slopeType, heightDiff);
        if (slopeType == TerrainSlopeType.Flat) return cost;
        return cost * InterstateSlopeMultiplier;
    }

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
