using Territory.Terrain;

/// <summary>
/// Shared cost constants + helpers for road pathfinding (Interstate, AutoRoad, manual).
/// Aligns <c>GridPathfinder</c> + <c>InterstateManager</c> → both prefer flat, penalize slopes consistently.
/// </summary>
public static class RoadPathCostConstants
{
    public const int Flat = 1;
    public const int CardinalSlope = 60;
    public const int DiagonalSlope = 35;
    public const int HeightDiffPenalty = 25;
    /// <summary>Cost for water slope cells (land adj water). Path prefers to stay 1 cell from coast.</summary>
    public const int WaterSlopeCost = 500;
    /// <summary>Slope cost multiplier for interstate pathfinding. Interstate strongly prefers flat over climbing hills.</summary>
    public const int InterstateSlopeMultiplier = 5;
    /// <summary>Bonus for continuing same direction (reduces step cost). Rule E.</summary>
    public const int InterstateStraightnessBonus = 15;
    /// <summary>Penalty on direction change (90° turn). Favors straight paths.</summary>
    public const int InterstateTurnPenalty = 5;
    /// <summary>Penalty on immediate back-turn (zigzag: turn, 1 tile, turn opposite). Prefers single turn or water slope over S-curve.</summary>
    public const int InterstateZigzagPenalty = 500;
    /// <summary>
    /// Added when step increases Manhattan distance to goal. Curbs long flat detours when shorter hill-crossing route cheaper overall.
    /// </summary>
    public const int InterstateAwayFromGoalPenalty = 18;

    /// <summary>
    /// Return step cost for interstate pathfinding. Higher slope cost → interstate routes around hills.
    /// </summary>
    public static int GetStepCostForInterstate(TerrainSlopeType slopeType, int heightDiff)
    {
        int cost = GetStepCost(slopeType, heightDiff);
        if (slopeType == TerrainSlopeType.Flat) return cost;
        return cost * InterstateSlopeMultiplier;
    }

    /// <summary>
    /// Return step cost for moving onto cell at given slope + height diff.
    /// Water cells must pass <c>TerrainSlopeType.Flat</c> + heightDiff 0.
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
