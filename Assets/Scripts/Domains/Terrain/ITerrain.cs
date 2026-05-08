using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Terrain;

namespace Domains.Terrain
{
    /// <summary>
    /// Public facade interface for the Terrain domain. Consumers bind to this interface only
    /// — never to TerrainManager or concrete service classes directly.
    /// Stage 2 surface: HeightMapService extracted in tracer slice; CliffService + TerrainSerializer follow.
    /// Invariants #1 (HeightMap/Cell sync), #7 (shore band), #9 (cliff faces) preserved via services.
    /// </summary>
    public interface ITerrain
    {
        /// <summary>Returns current heightmap, creating if null.</summary>
        HeightMap GetOrCreateHeightMap();

        /// <summary>Returns slope type at grid position.</summary>
        TerrainSlopeType GetTerrainSlopeTypeAt(int x, int y);

        /// <summary>True if building of given size can be placed at grid position per terrain constraints.</summary>
        bool CanPlaceBuildingInTerrain(Vector2 gridPosition, int size, out string failReason, bool allowCoastalSlope = false, bool allowWaterInFootprint = false);

        /// <summary>True if road can be placed at (x,y).</summary>
        bool CanPlaceRoad(int x, int y);

        /// <summary>Terrain sorting order for cell at (x,y,height).</summary>
        int CalculateTerrainSortingOrder(int x, int y, int height);

        /// <summary>Slope sorting order for cell at (x,y,height).</summary>
        int CalculateSlopeSortingOrder(int x, int y, int height);

        /// <summary>Building sorting order for cell at (x,y,height).</summary>
        int CalculateBuildingSortingOrder(int x, int y, int height);

        /// <summary>Modify terrain height at grid position.</summary>
        void ModifyTerrain(int x, int y, int newHeight);

        /// <summary>Restore terrain tile at (x,y).</summary>
        bool RestoreTerrainForCell(int x, int y, HeightMap useHeightMap = null, bool forceFlat = false, TerrainSlopeType? forceSlopeType = null, ISet<Vector2Int> terraformCutCorridorCells = null);
    }
}
