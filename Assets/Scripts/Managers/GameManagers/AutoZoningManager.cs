using UnityEngine;
using System.Collections.Generic;

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
    }

    public void ProcessTick()
    {
        string d = SimDateStr();
        if (zoneManager == null || growthBudgetManager == null || gridManager == null || cityStats == null)
        {
            Debug.Log($"[Sim {d}] [AutoZoningManager] ProcessTick: missing refs, skip.");
            return;
        }
        if (!cityStats.simulateGrowth)
            return;

        int budget = growthBudgetManager.GetAvailableBudget(GrowthCategory.Zoning);
        if (budget <= 0)
        {
            Debug.Log($"[Sim {d}] [AutoZoningManager] ProcessTick: budget<=0 ({budget}), skip.");
            return;
        }

        Vector2 centroid = GetUrbanCentroid();
        List<Vector2Int> candidates = GetCandidatesAdjacentToRoad();
        if (candidates.Count == 0)
        {
            Debug.Log($"[Sim {d}] [AutoZoningManager] ProcessTick: 0 candidates adjacent to road, skip.");
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
        Debug.Log($"[Sim {d}] [AutoZoningManager] ProcessTick: candidates={candidates.Count}, skippedReserved={skippedReserved}, skippedOther={skippedOther}, placed={placed}, budgetLeft={budget}");
    }

    private Vector2 GetUrbanCentroid()
    {
        float sx = 0, sy = 0;
        int n = 0;
        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Cell c = gridManager.GetCell(x, y);
                if (c == null) continue;
                if (c.zoneType != Zone.ZoneType.Grass && c.zoneType != Zone.ZoneType.Road &&
                    c.zoneType != Zone.ZoneType.None && c.zoneType != Zone.ZoneType.Water)
                {
                    sx += x;
                    sy += y;
                    n++;
                }
            }
        }
        if (n == 0) return new Vector2(gridManager.width / 2f, gridManager.height / 2f);
        return new Vector2(sx / n, sy / n);
    }

    private List<Vector2Int> GetCandidatesAdjacentToRoad()
    {
        var list = new List<Vector2Int>();
        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Cell c = gridManager.GetCell(x, y);
                if (c == null || !gridManager.IsZoneableNeighbor(c, x, y)) continue;
                if (!gridManager.IsAdjacentToRoad(x, y)) continue;
                list.Add(new Vector2Int(x, y));
            }
        }
        return list;
    }

    /// <summary>True if this Grass cell is adjacent to a road edge that has few Grass neighbors; do not zone here to leave room for road growth.
    /// We cap at 1 so only true dead-ends are reserved; value 2 in scene would reserve almost everything along a linear road.</summary>
    private bool IsReservedForRoadExpansion(Vector2Int p)
    {
        int threshold = Mathf.Min(minGrassNeighborsForZoning, 1);
        int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = p.x + dx[i], ny = p.y + dy[i];
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
