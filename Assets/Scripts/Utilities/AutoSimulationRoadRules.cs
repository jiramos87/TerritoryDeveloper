using Territory.Core;
using Territory.Zones;

namespace Territory.Utilities
{
    /// <summary>
    /// Shared predicates for AUTO simulation road growth (BUG-47): undeveloped light zoning and land cells
    /// that may be traversed or replaced by roads without duplicating logic across RoadCacheService,
    /// GridPathfinder, and AutoRoadBuilder.
    /// </summary>
    public static class AutoSimulationRoadRules
    {
        /// <summary>
        /// True when the cell uses R/C/I light zoning only (no medium/heavy).
        /// </summary>
        public static bool IsUndevelopedLightZoning(Cell c)
        {
            if (c == null) return false;
            return c.zoneType == Zone.ZoneType.ResidentialLightZoning
                || c.zoneType == Zone.ZoneType.CommercialLightZoning
                || c.zoneType == Zone.ZoneType.IndustrialLightZoning;
        }

        /// <summary>
        /// Land cells AUTO may plan roads through: grass, forest, or empty light zoning.
        /// Excludes road, interstate, buildings, water, and non-light zoning.
        /// </summary>
        public static bool IsAutoRoadLandCell(GridManager grid, int x, int y)
        {
            if (grid == null) return false;
            Cell c = grid.GetCell(x, y);
            if (c == null) return false;
            if (c.zoneType == Zone.ZoneType.Road || c.isInterstate) return false;
            if (grid.IsCellOccupiedByBuilding(x, y)) return false;
            if (c.zoneType == Zone.ZoneType.Grass || c.HasForest()) return true;
            return IsUndevelopedLightZoning(c);
        }
    }
}
