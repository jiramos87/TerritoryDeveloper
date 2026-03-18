using UnityEngine;

namespace Territory.Terrain
{
/// <summary>
/// Contract for terrain heightmap access, slope queries, and sorting order calculations.
/// Read this interface to understand TerrainManager's public API without reading its full implementation.
/// </summary>
public interface ITerrainManager
{
    HeightMap GetOrCreateHeightMap();
    TerrainSlopeType GetTerrainSlopeTypeAt(int x, int y);
    bool CanPlaceBuildingInTerrain(Vector2 gridPosition, int size, out string failReason, bool allowCoastalSlope = false, bool allowWaterInFootprint = false);
    bool CanPlaceRoad(int x, int y);
    int CalculateTerrainSortingOrder(int x, int y, int height);
    int CalculateSlopeSortingOrder(int x, int y, int height);
    int CalculateBuildingSortingOrder(int x, int y, int height);
    void ModifyTerrain(int x, int y, int newHeight);
    bool RestoreTerrainForCell(int x, int y, HeightMap useHeightMap = null, bool forceFlat = false, TerrainSlopeType? forceSlopeType = null);
}
}
