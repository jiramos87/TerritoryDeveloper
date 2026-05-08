using Territory.Geography;

namespace Domains.Geography
{
    /// <summary>
    /// Public facade interface for the Geography domain. Consumers bind to this interface only
    /// — never to GeographyManager or concrete service classes directly.
    /// Stage 15 tracer slice: GeographyWaterDesirabilityService extracted; full pipeline methods + sorting order follow.
    /// Invariants #1 (HeightMap/Cell sync), #7 (shore band), #8 (water), #9 (cliff faces) preserved via services.
    /// </summary>
    public interface IGeography
    {
        /// <summary>True after full geography pipeline completes (init-race guard).</summary>
        bool IsInitialized { get; }

        /// <summary>True if placement is valid at (x,y) for given type.</summary>
        bool IsPositionSuitableForPlacement(int x, int y, PlacementType placementType);

        /// <summary>Environmental bonus data for cell (x,y).</summary>
        EnvironmentalBonus GetEnvironmentalBonus(int x, int y);

        /// <summary>Forest region stats around (centerX,centerY) within radius.</summary>
        ForestRegionInfo GetForestRegionInfo(int centerX, int centerY, int radius);

        /// <summary>Snapshot of current geography state (terrain, water, forest stats).</summary>
        GeographyData GetCurrentGeographyData();

        /// <summary>Recalculate all cell sorting orders based on height.</summary>
        void ReCalculateSortingOrderBasedOnHeight();
    }
}
