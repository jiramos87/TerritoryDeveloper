using Territory.Core;
using Territory.Zones;

namespace Territory.Utilities
{
    /// <summary>
    /// Shared predicates for AUTO sim road growth: undeveloped light zoning + land cells
    /// that roads may traverse or replace. Avoids duplication across <c>RoadCacheService</c>,
    /// <c>GridPathfinder</c>, <c>AutoRoadBuilder</c>.
    /// </summary>
    public static class AutoSimulationRoadRules
    {
        /// <summary>
        /// True when cell uses R/C/I light zoning only (no medium/heavy).
        /// </summary>
        public static bool IsUndevelopedLightZoning(CityCell c)
        {
            if (c == null) return false;
            return c.zoneType == Zone.ZoneType.ResidentialLightZoning
                || c.zoneType == Zone.ZoneType.CommercialLightZoning
                || c.zoneType == Zone.ZoneType.IndustrialLightZoning;
        }

        /// <summary>
        /// Land cells AUTO may route roads through: grass, forest, or empty light zoning.
        /// Excludes: road, interstate, buildings, water, non-light zoning.
        /// </summary>
        public static bool IsAutoRoadLandCell(IGridManager grid, int x, int y)
        {
            if (grid == null) return false;
            CityCell c = grid.GetCell(x, y);
            if (c == null) return false;
            if (c.zoneType == Zone.ZoneType.Road || c.isInterstate) return false;
            if (grid.IsCellOccupiedByBuilding(x, y)) return false;
            if (c.zoneType == Zone.ZoneType.Grass || c.HasForest()) return true;
            return IsUndevelopedLightZoning(c);
        }
    }
}
