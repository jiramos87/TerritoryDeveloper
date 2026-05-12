using UnityEngine; using System.Collections; using System.Collections.Generic;
using Territory.Core; using Territory.Terrain; using Territory.Economy;
using Territory.UI; using Territory.Zones; using Territory.Utilities;
using Domains.Forests.Services; using Domains.Registry;

namespace Territory.Forests
{
/// <summary>THIN hub — delegates logic to <see cref="ForestService"/>. Serialized fields UNCHANGED (locked #3).</summary>
public class ForestManager : MonoBehaviour, IForestManager
{
    [Header("References")]
    public GridManager gridManager; public WaterManager waterManager; public CityStats cityStats;
    public EconomyManager economyManager; public UIManager uiManager;
    public GameNotificationManager gameNotificationManager; public TerrainManager terrainManager;
    [Header("Forest Prefabs")]
    public GameObject sparseForestPrefab; public GameObject mediumForestPrefab; public GameObject denseForestPrefab;
    [Header("Forest Slope Prefabs (Medium)")]
    public GameObject forestNorthSlopePrefab; public GameObject forestSouthSlopePrefab;
    public GameObject forestEastSlopePrefab;  public GameObject forestWestSlopePrefab;
    public GameObject forestNorthEastSlopePrefab; public GameObject forestNorthWestSlopePrefab;
    public GameObject forestSouthEastSlopePrefab; public GameObject forestSouthWestSlopePrefab;
    public GameObject forestNorthEastUpSlopePrefab; public GameObject forestNorthWestUpSlopePrefab;
    public GameObject forestSouthEastUpSlopePrefab; public GameObject forestSouthWestUpSlopePrefab;
    [Header("Forest Configuration")]
    public float desirabilityPerAdjacentForest = 2.0f; public float demandBoostPercentage = 0.5f;
    private ForestMap forestMap;
    private ForestService _svc;

    void Start()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (waterManager == null) waterManager = FindObjectOfType<WaterManager>();
        if (cityStats == null) cityStats = FindObjectOfType<CityStats>();
        if (economyManager == null) economyManager = FindObjectOfType<EconomyManager>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        if (terrainManager == null && gridManager?.terrainManager != null) terrainManager = gridManager.terrainManager;
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        _svc = new ForestService();
        var reg = FindObjectOfType<ServiceRegistry>();
        _svc.WireDependencies(reg?.Resolve<Domains.Grid.IGrid>(), reg?.Resolve<Domains.Water.IWater>(),
            reg?.Resolve<Domains.Economy.IEconomy>(), reg?.Resolve<Domains.Terrain.ITerrain>(),
            gridManager != null ? gridManager.width : 0, gridManager != null ? gridManager.height : 0);
    }

    public void InitializeForestMap()
    {
        if (gridManager == null) return;
        if (terrainManager == null && gridManager.terrainManager != null) terrainManager = gridManager.terrainManager;
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (waterManager == null && gridManager.waterManager != null) waterManager = gridManager.waterManager;
        if (waterManager == null) waterManager = FindObjectOfType<WaterManager>();
        if (terrainManager != null) terrainManager.EnsureHeightMapLoaded();
        forestMap = new ForestMap(gridManager.width, gridManager.height);
        forestMap.InitializeFromIntMatrix(_svc.BuildInitialForestCells(gridManager.width, gridManager.height));
        if (terrainManager != null) { terrainManager.EnsureHeightMapLoaded(); if (terrainManager.GetHeightMap() != null) UpdateForestVisuals(); else StartCoroutine(DeferredUpdateForestVisuals()); } else UpdateForestVisuals();
        UpdateForestStatistics(); UpdateAllCellDesirability();
    }
    public bool IsForestAt(int x, int y) => forestMap != null && forestMap.GetForestType(x, y) != Forest.ForestType.None;
    public Forest.ForestType GetForestTypeAt(int x, int y) => forestMap != null ? forestMap.GetForestType(x, y) : Forest.ForestType.None;
    public bool PlaceForest(Vector2 gridPosition, IForest selectedForest)
    {
        int x = (int)gridPosition.x, y = (int)gridPosition.y;
        if (!gridManager.IsValidGridPosition(gridPosition)) { DebugHelper.LogWarning($"Cannot place forest at invalid position: ({x}, {y})"); return false; }
        CityCell cell = gridManager.GetCell(x, y);
        if (cell == null || forestMap == null || !forestMap.IsValidPosition(x, y)) return false;
        if (forestMap.GetForestType(x, y) != Forest.ForestType.None) return false;
        if (waterManager != null && waterManager.IsWaterAt(x, y)) return false;
        if (_svc.IsRiverOrCoastEdge(x, y) || (terrainManager != null && terrainManager.IsWaterSlopeCell(x, y))) return false;
        if (!_svc.CanPlaceOnCell(cell)) return false;
        if (!_svc.CanAffordForest(selectedForest.ConstructionCost)) { gameNotificationManager.PostInfo($"Insufficient funds to place {selectedForest.ForestType} forest! Cost: {selectedForest.ConstructionCost}"); return false; }
        if (!_svc.HasSufficientWaterForForest(selectedForest.WaterConsumption)) { gameNotificationManager.PostInfo($"Insufficient water to place {selectedForest.ForestType} forest! Required: {selectedForest.WaterConsumption}"); return false; }
        GameObject fp = GetForestPrefabForCell(x, y, selectedForest.ForestType); if (fp == null) return false;
        GameObject fo = Instantiate(fp, cell.transformPosition, fp.transform.rotation); fo.transform.SetParent(cell.gameObject.transform);
        SetForestSortingOrder(fo, x, y, cell.GetCellInstanceHeight()); cell.SetTree(true, selectedForest.ForestType.ToString(), fo); forestMap.SetForestType(x, y, selectedForest.ForestType);
        if (selectedForest.ConstructionCost > 0) economyManager.SpendMoney(selectedForest.ConstructionCost);
        if (selectedForest.WaterConsumption > 0) waterManager.AddWaterConsumption(selectedForest.WaterConsumption);
        UpdateAdjacentDesirability(x, y, true); UpdateForestStatistics();
        var fm = selectedForest as MonoBehaviour; if (fm != null && fm.gameObject != fo) Destroy(fm.gameObject); return true;
    }

    public bool RemoveForestFromCell(int x, int y, bool refundCost = false)
    {
        if (forestMap == null || !forestMap.IsValidPosition(x, y)) return false;
        Forest.ForestType ct = forestMap.GetForestType(x, y);
        if (ct == Forest.ForestType.None) return false;
        CityCell cell = gridManager.GetCell(x, y);
        int wr = _svc.GetWaterConsumptionForForestType(ct), cr = _svc.GetConstructionCostForForestType(ct);
        cell.SetTree(false); forestMap.SetForestType(x, y, Forest.ForestType.None);
        if (refundCost) { if (wr > 0) waterManager.RemoveWaterConsumption(wr); if (cr > 0) economyManager.AddMoney(cr / 2); }
        UpdateAdjacentDesirability(x, y, false); UpdateForestStatistics(); return true;
    }

    public void RestoreForestAt(int x, int y, Forest.ForestType forestType, string forestPrefabName, bool updateStats = true, int? savedSpriteSortingOrder = null)
    {
        if (forestMap == null || gridManager == null || !forestMap.IsValidPosition(x, y)) return;
        CityCell cell = gridManager.GetCell(x, y); if (cell == null) return;
        if (cell.forestObject != null) { Destroy(cell.forestObject); cell.forestObject = null; }
        forestMap.SetForestType(x, y, forestType); if (forestType == Forest.ForestType.None) return;
        GameObject fp = GetForestPrefabForCell(x, y, forestType); if (fp == null) return;
        GameObject fo = Instantiate(fp, cell.transformPosition, fp.transform.rotation);
        fo.transform.SetParent(cell.gameObject.transform);
        if (savedSpriteSortingOrder.HasValue) { SpriteRenderer sr = fo.GetComponent<SpriteRenderer>(); if (sr != null) sr.sortingOrder = savedSpriteSortingOrder.Value; }
        else SetForestSortingOrder(fo, x, y, cell.height);
        cell.SetForest(forestType, fp.name, fo);
        UpdateAdjacentDesirability(x, y, true); if (updateStats) UpdateForestStatistics();
    }

    public void RefreshForestStatistics() => UpdateForestStatistics();
    public ForestMap GetForestMap() => forestMap;
    public float GetForestDemandBoost() { if (forestMap == null) return 0f; var c = forestMap.GetForestTypeCounts(); int t = 0; foreach (var v in c.Values) t += v; return t * demandBoostPercentage; }
    public ForestStatistics GetForestStatistics()
    {
        if (forestMap == null) return new ForestStatistics();
        var counts = forestMap.GetForestTypeCounts(); int total = 0; foreach (var v in counts.Values) total += v;
        return new ForestStatistics { totalForestCells = total, forestCoveragePercentage = forestMap.GetForestCoveragePercentage(),
            sparseForestCount = counts.ContainsKey(Forest.ForestType.Sparse) ? counts[Forest.ForestType.Sparse] : 0,
            mediumForestCount = counts.ContainsKey(Forest.ForestType.Medium) ? counts[Forest.ForestType.Medium] : 0,
            denseForestCount  = counts.ContainsKey(Forest.ForestType.Dense)  ? counts[Forest.ForestType.Dense]  : 0 };
    }

    public void UpdateForestVisuals()
    {
        if (forestMap == null || gridManager == null) return;
        for (int x = 0; x < gridManager.width; x++)
            for (int y = 0; y < gridManager.height; y++)
            { Forest.ForestType ft = forestMap.GetForestType(x, y); if (ft != Forest.ForestType.None) PlaceForestVisual(x, y, ft); }
    }

    private void PlaceForestVisual(int x, int y, Forest.ForestType ft)
    {
        CityCell cell = gridManager.GetCell(x, y); if (cell.hasTree) return;
        GameObject fp = GetForestPrefabForCell(x, y, ft); if (fp == null) return;
        GameObject fo = Instantiate(fp, cell.transformPosition, fp.transform.rotation);
        fo.transform.SetParent(cell.gameObject.transform); SetForestSortingOrder(fo, x, y, cell.height);
        cell.SetTree(true, ft.ToString(), fo);
    }

    private IEnumerator DeferredUpdateForestVisuals() { yield return null; if (terrainManager != null) terrainManager.EnsureHeightMapLoaded(); if (forestMap != null && gridManager != null) UpdateForestVisuals(); }

    private void UpdateAdjacentDesirability(int cx, int cy, bool added)
    {
        if (forestMap == null || gridManager == null) return;
        foreach (var pos in forestMap.GetPositionsAdjacentToForest(cx, cy))
        {
            CityCell cell = gridManager.GetCell(pos.x, pos.y); if (cell == null) continue;
            if (added) cell.closeForestCount++; else cell.closeForestCount = Mathf.Max(0, cell.closeForestCount - 1);
            cell.UpdateDesirability();
        }
    }

    private void UpdateAllCellDesirability()
    {
        if (forestMap == null || gridManager == null) return;
        for (int x = 0; x < gridManager.width; x++) for (int y = 0; y < gridManager.height; y++)
        { CityCell cell = gridManager.GetCell(x, y); if (cell == null) continue; cell.closeForestCount = forestMap.GetAdjacentForestCount(x, y); cell.UpdateDesirability(); }
    }

    private void EnsureForestMapForManualPlacement()
    {
        if (forestMap != null || gridManager == null) return;
        if (terrainManager == null && gridManager.terrainManager != null) terrainManager = gridManager.terrainManager;
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (waterManager == null && gridManager.waterManager != null) waterManager = gridManager.waterManager;
        if (waterManager == null) waterManager = FindObjectOfType<WaterManager>();
        if (terrainManager != null) terrainManager.EnsureHeightMapLoaded();
        forestMap = new ForestMap(gridManager.width, gridManager.height);
    }

    private void UpdateForestStatistics()
    {
        if (forestMap == null || cityStats == null) return;
        if (cityStats.GetComponent<CityStats>()) cityStats.SendMessage("UpdateForestStats", GetForestStatistics(), SendMessageOptions.DontRequireReceiver);
    }

    private void SetForestSortingOrder(GameObject fo, int x, int y, int h)
    {
        int order = _svc != null ? _svc.GetForestSortingOrder(x, y, h) : -(y * 10 + x) - (h * 100) - 50;
        foreach (SpriteRenderer sr in fo.GetComponentsInChildren<SpriteRenderer>()) { if (sr != null) sr.sortingOrder = order; }
    }

    private GameObject GetPrefabForForestType(Forest.ForestType ft) { switch (ft) { case Forest.ForestType.Sparse: return sparseForestPrefab; case Forest.ForestType.Medium: return mediumForestPrefab; case Forest.ForestType.Dense: return denseForestPrefab; default: return null; } }

    private GameObject GetForestPrefabForCell(int x, int y, Forest.ForestType ft)
    { if (terrainManager == null) return GetPrefabForForestType(ft); TerrainSlopeType st = terrainManager.GetTerrainSlopeTypeAt(x, y); if (st == TerrainSlopeType.Flat) return GetPrefabForForestType(ft); GameObject sp = GetSlopeForestPrefab(st); return sp ?? GetPrefabForForestType(ft); }

    private GameObject GetSlopeForestPrefab(TerrainSlopeType st)
    { switch (st) { case TerrainSlopeType.North: return forestNorthSlopePrefab; case TerrainSlopeType.South: return forestSouthSlopePrefab; case TerrainSlopeType.East: return forestEastSlopePrefab; case TerrainSlopeType.West: return forestWestSlopePrefab; case TerrainSlopeType.NorthEast: return forestNorthEastSlopePrefab; case TerrainSlopeType.NorthWest: return forestNorthWestSlopePrefab; case TerrainSlopeType.SouthEast: return forestSouthEastSlopePrefab; case TerrainSlopeType.SouthWest: return forestSouthWestSlopePrefab; case TerrainSlopeType.NorthEastUp: return forestNorthEastUpSlopePrefab; case TerrainSlopeType.NorthWestUp: return forestNorthWestUpSlopePrefab; case TerrainSlopeType.SouthEastUp: return forestSouthEastUpSlopePrefab; case TerrainSlopeType.SouthWestUp: return forestSouthWestUpSlopePrefab; default: return null; } }
}

/// <summary>Forest statistics with type-specific counts.</summary>
[System.Serializable]
public struct ForestStatistics
{
    public int totalForestCells; public float forestCoveragePercentage;
    public int sparseForestCount; public int mediumForestCount; public int denseForestCount;
}
}
