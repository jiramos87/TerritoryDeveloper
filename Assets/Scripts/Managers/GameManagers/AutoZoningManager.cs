using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;
using Territory.Economy;

namespace Territory.Simulation
{
/// <summary>
/// Automatically zones cells adjacent to roads during simulation steps based on demand.
/// Coordinates with GridManager for cell queries, ZoneManager for zone placement, and DemandManager for demand-driven decisions.
/// </summary>
public class AutoZoningManager : MonoBehaviour
{
    public GridManager gridManager;
    public ZoneManager zoneManager;
    public GrowthBudgetManager growthBudgetManager;
    public CityStats cityStats;
    public DemandManager demandManager;

    public int maxZonesPerTick = 5;
    /// <summary>Do not zone on Grass cells that are adjacent to a road edge with this many or fewer Grass neighbors (reserve for road growth). 0 = reserve only road cells with zero grass neighbors (maximize zoning).</summary>
    public int minGrassNeighborsForZoning = 0;

    private static readonly int[] Dx = { 1, -1, 0, 0 };
    private static readonly int[] Dy = { 0, 0, 1, -1 };

    private float centroidSumX, centroidSumY;
    private int centroidUrbanCount;
    private bool centroidDirty = true;

    string SimDateStr()
    {
        return cityStats != null ? cityStats.currentDate.ToString("yyyy-MM-dd") : "?";
    }

    void Start()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (zoneManager == null) zoneManager = FindObjectOfType<ZoneManager>();
        if (growthBudgetManager == null) growthBudgetManager = FindObjectOfType<GrowthBudgetManager>();
        if (cityStats == null) cityStats = FindObjectOfType<CityStats>();
        if (demandManager == null) demandManager = FindObjectOfType<DemandManager>();
        if (zoneManager != null)
            zoneManager.onUrbanCellChanged += OnUrbanCellChanged;
        if (gridManager != null)
        {
            gridManager.onUrbanCellsBulldozed += OnUrbanCellsBulldozed;
            gridManager.onGridRestored += OnGridRestored;
        }
    }

    void OnDestroy()
    {
        if (zoneManager != null)
            zoneManager.onUrbanCellChanged -= OnUrbanCellChanged;
        if (gridManager != null)
        {
            gridManager.onUrbanCellsBulldozed -= OnUrbanCellsBulldozed;
            gridManager.onGridRestored -= OnGridRestored;
        }
    }

    private void OnGridRestored()
    {
        centroidDirty = true;
    }

    private void OnUrbanCellChanged(Vector2 pos, bool isAdded)
    {
        if (isAdded)
        {
            centroidSumX += pos.x;
            centroidSumY += pos.y;
            centroidUrbanCount++;
        }
        else
        {
            centroidSumX -= pos.x;
            centroidSumY -= pos.y;
            centroidUrbanCount--;
        }
    }

    private void OnUrbanCellsBulldozed(IReadOnlyList<Vector2Int> positions)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            var p = positions[i];
            centroidSumX -= p.x;
            centroidSumY -= p.y;
            centroidUrbanCount--;
        }
    }

    public void ProcessTick()
    {
        string d = SimDateStr();
        if (zoneManager == null || growthBudgetManager == null || gridManager == null || cityStats == null)
        {
            return;
        }
        if (!cityStats.simulateGrowth)
            return;

        int budget = growthBudgetManager.GetAvailableBudget(GrowthCategory.Zoning);
        if (budget <= 0)
        {
            return;
        }

        Vector2 centroid = GetUrbanCentroid();
        List<Vector2Int> candidates = GetCandidatesAdjacentToRoad();
        if (candidates.Count == 0)
        {
            return;
        }

        candidates.Sort((a, b) =>
        {
            float da = Vector2.Distance(new Vector2(a.x, a.y), centroid);
            float db = Vector2.Distance(new Vector2(b.x, b.y), centroid);
            return da.CompareTo(db);
        });

        Zone.ZoneType[] zoneTypes = GetZoneTypesByDemand();
        int placed = 0;
        int skippedReserved = 0;
        int skippedOther = 0;
        foreach (Vector2Int p in candidates)
        {
            if (placed >= maxZonesPerTick) break;
            if (IsReservedForRoadExpansion(p))
            {
                skippedReserved++;
                continue;
            }
            Zone.ZoneType zoneType = zoneTypes[Random.Range(0, zoneTypes.Length)];
            var attrs = zoneManager.GetZoneAttributes(zoneType);
            if (attrs == null || attrs.ConstructionCost > budget)
            {
                skippedOther++;
                continue;
            }
            if (!demandManager.CanZoneTypeGrow(zoneType))
            {
                skippedOther++;
                continue;
            }
            Cell cell = gridManager.GetCell(p.x, p.y);
            if (cell == null || !gridManager.IsZoneableNeighbor(cell, p.x, p.y))
            {
                skippedOther++;
                continue;
            }
            if (growthBudgetManager.TrySpend(GrowthCategory.Zoning, attrs.ConstructionCost) && zoneManager.PlaceZoneAt(new Vector2(p.x, p.y), zoneType))
            {
                placed++;
                budget -= attrs.ConstructionCost;
            }
            else
                skippedOther++;
        }
    }

    private Vector2 GetUrbanCentroid()
    {
        if (centroidDirty)
        {
            centroidSumX = 0;
            centroidSumY = 0;
            centroidUrbanCount = 0;
            for (int x = 0; x < gridManager.width; x++)
            {
                for (int y = 0; y < gridManager.height; y++)
                {
                    Cell c = gridManager.GetCell(x, y);
                    if (c == null) continue;
                    if (c.zoneType != Zone.ZoneType.Grass && c.zoneType != Zone.ZoneType.Road &&
                        c.zoneType != Zone.ZoneType.None && c.zoneType != Zone.ZoneType.Water)
                    {
                        centroidSumX += x;
                        centroidSumY += y;
                        centroidUrbanCount++;
                    }
                }
            }
            centroidDirty = false;
        }
        if (centroidUrbanCount == 0) return new Vector2(gridManager.width / 2f, gridManager.height / 2f);
        return new Vector2(centroidSumX / centroidUrbanCount, centroidSumY / centroidUrbanCount);
    }

    private List<Vector2Int> GetCandidatesAdjacentToRoad()
    {
        var candidates = new HashSet<Vector2Int>();
        var edges = gridManager.GetRoadEdgePositions();
        for (int i = 0; i < edges.Count; i++)
        {
            Vector2Int edge = edges[i];
            for (int d = 0; d < 4; d++)
            {
                int nx = edge.x + Dx[d], ny = edge.y + Dy[d];
                if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height) continue;
                if (candidates.Contains(new Vector2Int(nx, ny))) continue;
                Cell c = gridManager.GetCell(nx, ny);
                if (c == null || !gridManager.IsZoneableNeighbor(c, nx, ny)) continue;
                candidates.Add(new Vector2Int(nx, ny));
            }
        }
        return new List<Vector2Int>(candidates);
    }

    /// <summary>True if this Grass cell is adjacent to a road edge that has few Grass neighbors; do not zone here to leave room for road growth.
    /// We cap at 1 so only true dead-ends are reserved; value 2 in scene would reserve almost everything along a linear road.</summary>
    private bool IsReservedForRoadExpansion(Vector2Int p)
    {
        int threshold = Mathf.Min(minGrassNeighborsForZoning, 1);
        for (int i = 0; i < 4; i++)
        {
            int nx = p.x + Dx[i], ny = p.y + Dy[i];
            if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height) continue;
            Cell neighbor = gridManager.GetCell(nx, ny);
            if (neighbor == null || neighbor.zoneType != Zone.ZoneType.Road) continue;
            int grassCount = gridManager.CountGrassNeighbors(nx, ny);
            if (grassCount <= threshold)
                return true;
        }
        return false;
    }

    private Zone.ZoneType[] GetZoneTypesByDemand()
    {
        var list = new List<Zone.ZoneType>();
        if (demandManager == null)
        {
            list.Add(Zone.ZoneType.ResidentialLightZoning);
            list.Add(Zone.ZoneType.CommercialLightZoning);
            list.Add(Zone.ZoneType.IndustrialLightZoning);
            return list.ToArray();
        }
        float r = demandManager.GetResidentialDemand().demandLevel;
        float c = demandManager.GetCommercialDemand().demandLevel;
        float i = demandManager.GetIndustrialDemand().demandLevel;
        if (r > 0) list.Add(Zone.ZoneType.ResidentialLightZoning);
        if (c > 0) list.Add(Zone.ZoneType.CommercialLightZoning);
        if (i > 0) list.Add(Zone.ZoneType.IndustrialLightZoning);
        if (list.Count == 0)
        {
            list.Add(Zone.ZoneType.ResidentialLightZoning);
            list.Add(Zone.ZoneType.CommercialLightZoning);
            list.Add(Zone.ZoneType.IndustrialLightZoning);
        }
        return list.ToArray();
    }
}
}
