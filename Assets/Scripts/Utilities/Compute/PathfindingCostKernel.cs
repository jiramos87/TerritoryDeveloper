using UnityEngine;
using Territory.Terrain;

namespace Territory.Utilities.Compute
{
    /// <summary>
    /// Read-only ordinary-road step cost (GridPathfinder / manual road preview alignment). No <see cref="Territory.Core.GridManager"/> —
    /// callers supply heights, slope type, and coastal eligibility from <see cref="TerrainManager"/> queries (TECH-39 §7.11.3).
    /// </summary>
    public static class PathfindingCostKernel
    {
        /// <summary>Inputs for one cardinal step onto <c>(toX, toY)</c>; coastal flags match <see cref="TerrainManager"/> shore/rim eligibility used in pathfinding.</summary>
        public readonly struct PathfindingMoveContext
        {
            public readonly int HeightFrom;
            public readonly int HeightTo;
            public readonly TerrainSlopeType SlopeTypeAtTo;
            public readonly bool IsWaterSlopeCellAtTo;
            public readonly bool CoastalEligibilityFrom;
            public readonly bool CoastalEligibilityTo;

            public PathfindingMoveContext(
                int heightFrom,
                int heightTo,
                TerrainSlopeType slopeTypeAtTo,
                bool isWaterSlopeCellAtTo,
                bool coastalEligibilityFrom,
                bool coastalEligibilityTo)
            {
                HeightFrom = heightFrom;
                HeightTo = heightTo;
                SlopeTypeAtTo = slopeTypeAtTo;
                IsWaterSlopeCellAtTo = isWaterSlopeCellAtTo;
                CoastalEligibilityFrom = coastalEligibilityFrom;
                CoastalEligibilityTo = coastalEligibilityTo;
            }
        }

        /// <summary>Matches <see cref="Territory.Core.GridPathfinder"/> height / water-slope / slope-type rules before road-spacing penalty.</summary>
        public static int GetOrdinaryRoadMoveCost(in PathfindingMoveContext ctx)
        {
            if (ctx.IsWaterSlopeCellAtTo)
                return RoadPathCostConstants.WaterSlopeCost;

            int absDh = Mathf.Abs(ctx.HeightTo - ctx.HeightFrom);
            int heightDiff;
            if (absDh > 1)
            {
                if (!ctx.CoastalEligibilityFrom && !ctx.CoastalEligibilityTo)
                    return int.MaxValue;
                heightDiff = 1;
            }
            else
                heightDiff = absDh;

            return RoadPathCostConstants.GetStepCost(ctx.SlopeTypeAtTo, heightDiff);
        }
    }
}
