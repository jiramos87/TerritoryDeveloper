// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Terrain;
using Territory.Zones;
using Territory.Buildings;
using Territory.Utilities;
using Domains.Grid.Services;

namespace Territory.Geography
{
/// <summary>
/// Sorting-order recalculation extracted from GeographyManager.
/// Absorbs full sorting body so GeographyManager becomes a single-line delegate.
/// Inv #9 (cliff visible faces south + east only) preserved via CalculateWaterSlopeSortingOrder / CalculateSlopeSortingOrder
/// delegated to ITerrainManager on the same write paths.
/// </summary>
public class GeographySortingOrderService
{
    private readonly GridManager _gridManager;
    private readonly TerrainManager _terrainManager;

    public GeographySortingOrderService(GridManager gridManager, TerrainManager terrainManager)
    {
        _gridManager = gridManager;
        _terrainManager = terrainManager;
    }

    // --- Adjacent-building helpers ---

    /// <summary>
    /// Cell (x,y) immediately east of multi-cell building → return true + building sorting order.
    /// </summary>
    public bool TryGetEastAdjacentBuildingOrder(int x, int y, out int buildingOrder)
    {
        buildingOrder = 0;
        if (_gridManager == null || x <= 0) return false;
        int width = _gridManager.width;
        int height = _gridManager.height;
        if (y < 0 || y >= height) return false;

        int westX = x - 1;
        CityCell westCell = _gridManager.GetCell(westX, y);
        if (westCell == null || westCell.buildingSize <= 1) return false;

        GameObject pivotObj = _gridManager.GetBuildingPivotCell(westCell);
        if (pivotObj == null) return false;
        CityCell pivotCell = pivotObj.GetComponent<CityCell>();
        if (pivotCell == null || pivotCell.buildingSize <= 1) return false;

        _gridManager.GetBuildingFootprintOffset(pivotCell.buildingSize, out int offsetX, out int offsetY);
        int maxFx = (int)pivotCell.x - offsetX + pivotCell.buildingSize - 1;
        if (westX != maxFx) return false;

        buildingOrder = pivotCell.sortingOrder;
        return true;
    }

    /// <summary>
    /// Cell (x,y) immediately south of multi-cell building → return true + building sorting order.
    /// </summary>
    public bool TryGetSouthAdjacentBuildingOrder(int x, int y, out int buildingOrder)
    {
        buildingOrder = 0;
        if (_gridManager == null) return false;
        int width = _gridManager.width;
        int height = _gridManager.height;
        if (x < 0 || x >= width - 1 || y < 0 || y >= height) return false;

        int northX = x + 1;
        CityCell northCell = _gridManager.GetCell(northX, y);
        if (northCell == null || northCell.buildingSize <= 1) return false;

        GameObject pivotObj = _gridManager.GetBuildingPivotCell(northCell);
        if (pivotObj == null) return false;
        CityCell pivotCell = pivotObj.GetComponent<CityCell>();
        if (pivotCell == null || pivotCell.buildingSize <= 1) return false;

        _gridManager.GetBuildingFootprintOffset(pivotCell.buildingSize, out int offsetX, out int offsetY);
        int minFx = (int)pivotCell.x - offsetX;
        if (northX != minFx) return false;

        buildingOrder = pivotCell.sortingOrder;
        return true;
    }

    /// <summary>
    /// Cell (x,y) immediately west of multi-cell building → return true + building sorting order.
    /// </summary>
    public bool TryGetWestAdjacentBuildingOrder(int x, int y, out int buildingOrder)
    {
        buildingOrder = 0;
        if (_gridManager == null) return false;
        int width = _gridManager.width;
        int height = _gridManager.height;
        if (x < 0 || x >= width || y < 0 || y >= height - 1) return false;

        int eastY = y + 1;
        CityCell eastCell = _gridManager.GetCell(x, eastY);
        if (eastCell == null || eastCell.buildingSize <= 1) return false;

        GameObject pivotObj = _gridManager.GetBuildingPivotCell(eastCell);
        if (pivotObj == null) return false;
        CityCell pivotCell = pivotObj.GetComponent<CityCell>();
        if (pivotCell == null || pivotCell.buildingSize <= 1) return false;

        _gridManager.GetBuildingFootprintOffset(pivotCell.buildingSize, out int offsetX, out int offsetY);
        int minFy = (int)pivotCell.y - offsetY;
        if (eastY != minFy) return false;

        buildingOrder = pivotCell.sortingOrder;
        return true;
    }

    /// <summary>
    /// Max sorting order of any content on cell (x,y).
    /// </summary>
    public int GetCellMaxContentSortingOrder(int x, int y)
    {
        if (_gridManager == null || _terrainManager == null) return int.MinValue;
        int width = _gridManager.width;
        int height = _gridManager.height;
        if (x < 0 || x >= width || y < 0 || y >= height) return int.MinValue;

        GameObject cellObj = _gridManager.gridArray[x, y];
        CityCell cell = _gridManager.GetCell(x, y);
        if (cell == null) return int.MinValue;

        int terrainOrder = _terrainManager.CalculateTerrainSortingOrder(x, y, cell.height);
        int maxOrder = terrainOrder;

        if (cellObj.GetComponent<SpriteRenderer>() != null)
            maxOrder = Mathf.Max(maxOrder, terrainOrder);

        for (int i = 0; i < cellObj.transform.childCount; i++)
        {
            GameObject child = cellObj.transform.GetChild(i).gameObject;
            if (child.GetComponent<SpriteRenderer>() == null) continue;

            int order;
            if (_terrainManager.IsWaterSlopeObject(child))
                order = _terrainManager.CalculateWaterSlopeSortingOrder(x, y);
            else if (_terrainManager.IsShoreBayObject(child))
                order = _terrainManager.CalculateShoreBaySortingOrder(x, y);
            else if (cell.forestObject != null && cell.forestObject == child)
                order = terrainOrder + 5;
            else
            {
                Zone zone = child.GetComponent<Zone>();
                if (zone != null)
                {
                    if (zone.zoneType == Zone.ZoneType.Road) order = terrainOrder + GridSortingOrderService.ROAD_SORTING_OFFSET;
                    else if (zone.zoneCategory == Zone.ZoneCategory.Zoning) order = terrainOrder + 0;
                    else if (zone.zoneCategory == Zone.ZoneCategory.Building) order = terrainOrder + 10;
                    else order = terrainOrder;
                }
                else if (child.GetComponent<PowerPlant>() != null || child.GetComponent<WaterPlant>() != null)
                {
                    if (cell.buildingSize > 1)
                        order = GetMultiCellBuildingMaxSortingOrder(x, y, cell.buildingSize);
                    else
                        order = terrainOrder + 10;
                }
                else
                    order = terrainOrder;
            }
            maxOrder = Mathf.Max(maxOrder, order);
        }
        return maxOrder;
    }

    /// <summary>
    /// Max sorting order over multi-cell building footprint. Capped so building draws behind "front" adjacent cells.
    /// </summary>
    public int GetMultiCellBuildingMaxSortingOrder(int pivotX, int pivotY, int buildingSize)
    {
        if (_gridManager == null || _terrainManager == null || buildingSize <= 1)
            return _terrainManager != null ? _terrainManager.CalculateTerrainSortingOrder(pivotX, pivotY, 0) + 10 : 0;

        _gridManager.GetBuildingFootprintOffset(buildingSize, out int offsetX, out int offsetY);
        int minFx = pivotX - offsetX;
        int minFy = pivotY - offsetY;
        int maxFx = minFx + buildingSize - 1;
        int maxFy = minFy + buildingSize - 1;

        int maxOrder = int.MinValue;
        int width = _gridManager.width;
        int height = _gridManager.height;

        for (int x = 0; x < buildingSize; x++)
        {
            for (int y = 0; y < buildingSize; y++)
            {
                int gridX = pivotX + x - offsetX;
                int gridY = pivotY + y - offsetY;
                if (gridX < 0 || gridX >= width || gridY < 0 || gridY >= height) continue;

                CityCell cell = _gridManager.GetCell(gridX, gridY);
                if (cell == null) continue;

                int cellHeight = cell.height;
                if (_terrainManager.GetHeightMap() != null)
                    cellHeight = _terrainManager.GetHeightMap().GetHeight(gridX, gridY);

                int order = _terrainManager.CalculateBuildingSortingOrder(gridX, gridY, cellHeight);
                if (order > maxOrder) maxOrder = order;
            }
        }

        // Front = left or top. Back = south-east face only: right column (ax==maxFx+1) and bottom row (ay==maxFy+1).
        int minFrontAdjacentContentOrder = int.MaxValue;
        int maxBackAdjacentContentOrder = int.MinValue;
        for (int ax = minFx - 1; ax <= maxFx + 1; ax++)
        {
            for (int ay = minFy - 1; ay <= maxFy + 1; ay++)
            {
                if (ax >= minFx && ax <= maxFx && ay >= minFy && ay <= maxFy) continue;
                if (ax < 0 || ax >= width || ay < 0 || ay >= height) continue;
                int contentOrder = GetCellMaxContentSortingOrder(ax, ay);
                if (contentOrder == int.MinValue) continue;
                bool isFront = (ax < minFx) || (ay < minFy);
                bool isBackSouthEast = (ax == maxFx + 1 && ay >= minFy && ay <= maxFy + 1) || (ay == maxFy + 1 && ax >= minFx && ax <= maxFx + 1);
                if (isFront && contentOrder < minFrontAdjacentContentOrder)
                    minFrontAdjacentContentOrder = contentOrder;
                if (isBackSouthEast && contentOrder > maxBackAdjacentContentOrder)
                    maxBackAdjacentContentOrder = contentOrder;
            }
        }
        // Apply floor first so we're always in front of back south-east tiles (use +2 buffer to avoid grass overlapping building)
        if (maxBackAdjacentContentOrder != int.MinValue)
        {
            int orderInFrontOfBack = maxBackAdjacentContentOrder + 2;
            if (orderInFrontOfBack > maxOrder)
                maxOrder = orderInFrontOfBack;
        }
        // Cap only when it wouldn't hide the building (same logic as GridManager)
        if (minFrontAdjacentContentOrder != int.MaxValue)
        {
            int orderBehindFront = minFrontAdjacentContentOrder - 1;
            bool skipCapForVisibility = orderBehindFront < maxOrder && maxOrder > minFrontAdjacentContentOrder;
            if (orderBehindFront < maxOrder && !skipCapForVisibility)
                maxOrder = orderBehindFront;
        }

        // Floor: building must always draw on top of content on its own footprint (e.g. grass left during LoadGame).
        int maxFootprintContentOrder = int.MinValue;
        for (int fx = minFx; fx <= maxFx; fx++)
        {
            for (int fy = minFy; fy <= maxFy; fy++)
            {
                if (fx < 0 || fx >= width || fy < 0 || fy >= height) continue;
                int contentOrder = GetCellMaxContentSortingOrder(fx, fy);
                if (contentOrder != int.MinValue && contentOrder > maxFootprintContentOrder)
                    maxFootprintContentOrder = contentOrder;
            }
        }
        if (maxFootprintContentOrder != int.MinValue && maxOrder < maxFootprintContentOrder + 1)
            maxOrder = maxFootprintContentOrder + 1;

        return maxOrder != int.MinValue ? maxOrder : _terrainManager.CalculateTerrainSortingOrder(pivotX, pivotY, 0) + 10;
    }

    /// <summary>
    /// Recalculate sprite sorting orders for all grid cells based on height + content type.
    /// Preserves Inv #9 (cliff visible faces south + east only) via terrain manager delegation.
    /// </summary>
    public void ReCalculateSortingOrderBasedOnHeight()
    {
        if (_gridManager == null)
            return;

        for (int x = 0; x < _gridManager.width; x++)
        {
            for (int y = 0; y < _gridManager.height; y++)
            {
                GameObject cell = _gridManager.gridArray[x, y];
                CityCell cellComponent = _gridManager.GetCell(x, y);

                if (cellComponent == null)
                {
                    DebugHelper.LogWarning($"CityCell component missing at ({x}, {y})");
                    continue;
                }

                if (cellComponent.sortingOrder == -1001)
                    continue;

                List<GameObject> objectsToSort = new List<GameObject>();

                if (cell.GetComponent<SpriteRenderer>() != null)
                    objectsToSort.Add(cell);

                for (int i = 0; i < cell.transform.childCount; i++)
                {
                    GameObject child = cell.transform.GetChild(i).gameObject;
                    if (child.GetComponent<SpriteRenderer>() != null)
                        objectsToSort.Add(child);
                }

                int maxCellSortingOrder = cellComponent.sortingOrder;
                foreach (GameObject obj in objectsToSort)
                {
                    int newSortingOrder;
                    if (_terrainManager != null && _terrainManager.IsWaterSlopeObject(obj))
                    {
                        newSortingOrder = _terrainManager.CalculateWaterSlopeSortingOrder(x, y);
                    }
                    else if (_terrainManager != null && _terrainManager.IsShoreBayObject(obj))
                    {
                        newSortingOrder = _terrainManager.CalculateShoreBaySortingOrder(x, y);
                    }
                    else if (_terrainManager != null && _terrainManager.IsSeaLevelWaterObject(obj))
                    {
                        // Water below terrain at height 1; offset < DEPTH_MULTIPLIER (100) so depth ordering works with water slopes
                        newSortingOrder = _terrainManager.CalculateTerrainSortingOrder(x, y, 0) - 50;
                    }
                    else if (_terrainManager != null && _terrainManager.IsLandSlopeObject(obj))
                    {
                        newSortingOrder = _terrainManager.CalculateSlopeSortingOrder(x, y, cellComponent.height);
                    }
                    else
                    {
                        int terrainOrder = _terrainManager != null
                            ? _terrainManager.CalculateTerrainSortingOrder(x, y, cellComponent.height)
                            : 0;
                        if (cellComponent.forestObject != null && cellComponent.forestObject == obj)
                        {
                            newSortingOrder = terrainOrder + 5;
                        }
                        else
                        {
                            Zone zone = obj.GetComponent<Zone>();
                            if (zone != null)
                            {
                                if (zone.zoneType == Zone.ZoneType.Water)
                                {
                                    // Bridge cell: ensure water is explicitly below the bridge
                                    bool hasRoadInCell = false;
                                    for (int j = 0; j < cell.transform.childCount; j++)
                                    {
                                        Zone z = cell.transform.GetChild(j).GetComponent<Zone>();
                                        if (z != null && z.zoneType == Zone.ZoneType.Road) { hasRoadInCell = true; break; }
                                    }
                                    if (hasRoadInCell && _terrainManager != null)
                                    {
                                        int bridgeOrder = _terrainManager.CalculateTerrainSortingOrder(x, y, 1) + GridSortingOrderService.ROAD_SORTING_OFFSET;
                                        newSortingOrder = bridgeOrder - 100; // Guarantee water renders below bridge
                                    }
                                    else
                                    {
                                        // Water below terrain at height 1; offset < DEPTH_MULTIPLIER (100) so depth ordering works with water slopes
                                        newSortingOrder = _terrainManager.CalculateTerrainSortingOrder(x, y, 0) - 50;
                                    }
                                }
                                else if (zone.zoneType == Zone.ZoneType.Road)
                                {
                                    // Bridge over water: use height 1 so bridge renders above water (matches SetRoadSortingOrder)
                                    int effectiveHeight = (cellComponent.height == 0) ? 1 : cellComponent.height;
                                    newSortingOrder = (_terrainManager != null
                                        ? _terrainManager.CalculateTerrainSortingOrder(x, y, effectiveHeight)
                                        : 0) + GridSortingOrderService.ROAD_SORTING_OFFSET;
                                }
                                else if (zone.zoneCategory == Zone.ZoneCategory.Zoning)
                                    newSortingOrder = terrainOrder + 0;
                                else if (zone.zoneCategory == Zone.ZoneCategory.Building)
                                {
                                    // Multi-cell buildings: use max order over footprint
                                    if (cellComponent.buildingSize > 1)
                                        newSortingOrder = GetMultiCellBuildingMaxSortingOrder(x, y, cellComponent.buildingSize);
                                    else
                                        newSortingOrder = terrainOrder + 10;
                                }
                                else
                                    newSortingOrder = terrainOrder;
                            }
                            else if (obj.GetComponent<PowerPlant>() != null || obj.GetComponent<WaterPlant>() != null)
                            {
                                if (cellComponent.buildingSize > 1)
                                    newSortingOrder = GetMultiCellBuildingMaxSortingOrder(x, y, cellComponent.buildingSize);
                                else
                                    newSortingOrder = terrainOrder + 10;
                            }
                            else
                            {
                                newSortingOrder = terrainOrder;
                            }
                        }
                        // Cells east/south/west of a multi-cell building: draw their content on top. Skip for water.
                        Zone zoneForBoost = obj.GetComponent<Zone>();
                        if (zoneForBoost == null || zoneForBoost.zoneType != Zone.ZoneType.Water)
                        {
                            if (TryGetEastAdjacentBuildingOrder(x, y, out int eastBuildingOrder))
                                newSortingOrder = Mathf.Max(newSortingOrder, eastBuildingOrder + 1);
                            if (TryGetSouthAdjacentBuildingOrder(x, y, out int southBuildingOrder))
                                newSortingOrder = Mathf.Max(newSortingOrder, southBuildingOrder + 1);
                            if (TryGetWestAdjacentBuildingOrder(x, y, out int westBuildingOrder))
                                newSortingOrder = Mathf.Max(newSortingOrder, westBuildingOrder + 1);
                        }
                    }
                    SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
                    foreach (SpriteRenderer sr in renderers)
                    {
                        if (sr != null)
                            sr.sortingOrder = newSortingOrder;
                    }
                    if (newSortingOrder > maxCellSortingOrder)
                        maxCellSortingOrder = newSortingOrder;
                }
                cellComponent.sortingOrder = maxCellSortingOrder;
            }
        }
    }
}
}
