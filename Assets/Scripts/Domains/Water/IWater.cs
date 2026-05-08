using System.Collections.Generic;
using Territory.Terrain;

namespace Domains.Water
{
    /// <summary>
    /// Public facade interface for the Water domain. Consumers bind to this interface only
    /// — never to WaterManager or concrete service classes directly.
    /// Stage 4 surface: WaterMapService + ShoreService extracted in tracer slice.
    /// Invariants #7 (shore band), #8 (river bed monotonic), guardrail #6 (RefreshShoreTerrainAfterWaterUpdate) preserved.
    /// </summary>
    public interface IWater
    {
        /// <summary>True if cell at (x,y) is registered water.</summary>
        bool IsWaterAt(int x, int y);

        /// <summary>Returns water body id at (x,y); 0 = dry.</summary>
        int GetWaterBodyId(int x, int y);

        /// <summary>Returns surface height at water cell; -1 if dry.</summary>
        int GetSurfaceHeightAt(int x, int y);

        /// <summary>Creates a new river water body and returns its id.</summary>
        int CreateRiverWaterBody(int surfaceHeight);

        /// <summary>Assigns dry cell to existing river body.</summary>
        bool TryAssignCellToRiverBody(int x, int y, int bodyId);

        /// <summary>Returns read-only dictionary of all registered water bodies.</summary>
        IReadOnlyDictionary<int, WaterBody> GetBodies();

        /// <summary>Merge adjacent same-surface bodies (rivers+rivers, lakes+lakes, lake+sea).</summary>
        void MergeAdjacentBodiesWithSameSurface();
    }
}
