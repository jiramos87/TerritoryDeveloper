using System.Collections.Generic;
using UnityEngine;

namespace Territory.Terrain
{
/// <summary>
/// Contract for terrain heightmap access, slope queries, water-shore predicates, sorting order calc.
/// Core-leaf interface — Domains.* + Core.* may consume; Game.asmdef provides impl via concrete TerrainManager.
/// </summary>
public interface ITerrainManager
{
    HeightMap GetHeightMap();
    HeightMap GetOrCreateHeightMap();
    TerrainSlopeType GetTerrainSlopeTypeAt(int x, int y);
    bool CanPlaceBuildingInTerrain(Vector2 gridPosition, int size, out string failReason, bool allowCoastalSlope = false, bool allowWaterInFootprint = false);
    bool CanPlaceRoad(int x, int y);
    bool CanPlaceRoad(int x, int y, bool allowWaterSlopeForWaterBridgeTrace);
    int CalculateTerrainSortingOrder(int x, int y, int height);
    int CalculateSlopeSortingOrder(int x, int y, int height);
    int CalculateBuildingSortingOrder(int x, int y, int height);
    void ModifyTerrain(int x, int y, int newHeight);
    bool RestoreTerrainForCell(int x, int y, HeightMap useHeightMap = null, bool forceFlat = false, TerrainSlopeType? forceSlopeType = null, ISet<Vector2Int> terraformCutCorridorCells = null);
    bool ShouldSkipRoadTerraformSurfaceAt(int x, int y, HeightMap heightMap);
    bool IsWaterSlopeCell(int x, int y);
    bool IsRegisteredOpenWaterAt(int x, int y);
    bool IsWaterSlopeObject(GameObject obj);
    bool IsShoreBayObject(GameObject obj);
    bool IsLandSlopeObject(GameObject obj);
    int CalculateWaterSlopeSortingOrder(int x, int y);
    int CalculateShoreBaySortingOrder(int x, int y);
    IWaterManager Water { get; }
}
}
