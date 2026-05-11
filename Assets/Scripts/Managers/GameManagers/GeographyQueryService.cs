using Territory.Core;
using Territory.Forests;
using Territory.Utilities;

namespace Territory.Geography
{
/// <summary>
/// Query / utility methods extracted from GeographyManager.
/// Handles placement suitability, environmental bonus, forest region info, and geography data snapshots.
/// No Unity lifecycle — stateless helpers driven by manager references on hub.
/// </summary>
public class GeographyQueryService
{
    private readonly GeographyManager _hub;

    public GeographyQueryService(GeographyManager hub)
    {
        _hub = hub;
    }

    public bool IsPositionSuitableForPlacement(int x, int y, PlacementType placementType)
    {
        switch (placementType)
        {
            case PlacementType.Forest:
                if (_hub.waterManager != null && _hub.waterManager.IsWaterAt(x, y))
                    return false;
                if (_hub.forestManager != null && _hub.forestManager.IsForestAt(x, y))
                    return false;
                if (_hub.gridManager != null && _hub.gridManager.gridArray != null)
                {
                    CityCell cell = _hub.gridManager.GetCell(x, y);
                    if (cell != null && cell.occupiedBuilding != null)
                        return false;
                }
                return true;

            case PlacementType.Water:
                if (_hub.waterManager != null && _hub.waterManager.IsWaterAt(x, y))
                    return false;
                if (_hub.forestManager != null && _hub.forestManager.IsForestAt(x, y))
                    return false;
                return true;

            case PlacementType.Building:
                if (_hub.waterManager != null && _hub.waterManager.IsWaterAt(x, y))
                    return false;
                return true;

            case PlacementType.Zone:
                if (_hub.waterManager != null && _hub.waterManager.IsWaterAt(x, y))
                    return false;
                return true;

            case PlacementType.Infrastructure:
                if (_hub.waterManager != null && _hub.waterManager.IsWaterAt(x, y))
                    return false;
                if (_hub.gridManager != null && _hub.gridManager.IsCellOccupiedByBuilding(x, y))
                    return false;
                return true;

            default:
                return true;
        }
    }

    public EnvironmentalBonus GetEnvironmentalBonus(int x, int y)
    {
        EnvironmentalBonus bonus = new EnvironmentalBonus();
        if (_hub.gridManager != null && _hub.gridManager.gridArray != null)
        {
            if (x >= 0 && x < _hub.gridManager.width && y >= 0 && y < _hub.gridManager.height)
            {
                CityCell cell = _hub.gridManager.GetCell(x, y);
                if (cell != null)
                {
                    bonus.desirability = cell.desirability;
                    bonus.adjacentForests = cell.closeForestCount;
                    bonus.adjacentWater = cell.closeWaterCount;
                    bonus.forestType = cell.GetForestType();
                }
            }
        }
        return bonus;
    }

    public ForestRegionInfo GetForestRegionInfo(int centerX, int centerY, int radius)
    {
        ForestRegionInfo info = new ForestRegionInfo();
        if (_hub.forestManager == null) return info;

        ForestMap forestMap = _hub.forestManager.GetForestMap();
        if (forestMap == null) return info;

        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (!forestMap.IsValidPosition(x, y)) continue;
                Forest.ForestType forestType = forestMap.GetForestType(x, y);
                switch (forestType)
                {
                    case Forest.ForestType.Sparse: info.sparseCount++; break;
                    case Forest.ForestType.Medium: info.mediumCount++; break;
                    case Forest.ForestType.Dense:  info.denseCount++;  break;
                }
                info.totalCells++;
            }
        }

        info.forestCoverage = info.totalCells > 0
            ? (float)(info.sparseCount + info.mediumCount + info.denseCount) / info.totalCells * 100f
            : 0f;

        return info;
    }

    public GeographyData CreateGeographyData()
    {
        GeographyData data = new GeographyData();

        if (_hub.terrainManager != null)
        {
            data.hasTerrainData = true;
            data.terrainWidth = _hub.gridManager.width;
            data.terrainHeight = _hub.gridManager.height;
        }

        if (_hub.waterManager != null && _hub.waterManager.GetWaterMap() != null)
        {
            data.hasWaterData = true;
            data.waterCellCount = GetWaterCellCount();
        }

        if (_hub.forestManager != null && _hub.forestManager.GetForestMap() != null)
        {
            data.hasForestData = true;
            var stats = _hub.forestManager.GetForestStatistics();
            data.forestCellCount = stats.totalForestCells;
            data.forestCoveragePercentage = stats.forestCoveragePercentage;
            data.sparseForestCount = stats.sparseForestCount;
            data.mediumForestCount = stats.mediumForestCount;
            data.denseForestCount = stats.denseForestCount;
        }

        return data;
    }

    private int GetWaterCellCount()
    {
        int count = 0;
        if (_hub.waterManager == null || _hub.gridManager == null) return 0;
        for (int x = 0; x < _hub.gridManager.width; x++)
            for (int y = 0; y < _hub.gridManager.height; y++)
                if (_hub.waterManager.IsWaterAt(x, y))
                    count++;
        return count;
    }
}
}
