using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;
using Territory.Economy;

namespace Territory.Simulation
{
/// <summary>
/// Automatically zones cells adjacent to roads during simulation steps based on demand.
/// Uses UrbanMetrics for sector-coherent zone selection (R/C/I by urban ring) and neighbor influence.
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

    private UrbanMetrics urbanMetrics;

    private const float COHERENCE_BOOST = 1.6f;
    [Header("Industrial separation (BUG-07)")]
    [SerializeField] float industrialSeparation = 0.15f;
    private const int MaxRoadDistanceForZoning = 3;

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

        if (gridManager != null)
        {
            urbanMetrics = new UrbanMetrics(gridManager.width, gridManager.height);
            urbanMetrics.RecalculateFromGrid(gridManager);
        }

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

    public UrbanMetrics GetUrbanMetrics()
    {
        return urbanMetrics;
    }

    private void OnGridRestored()
    {
        if (urbanMetrics != null && gridManager != null)
            urbanMetrics.RecalculateFromGrid(gridManager);
    }

    private void OnUrbanCellChanged(Vector2 pos, bool isAdded)
    {
        if (urbanMetrics == null) return;
        if (isAdded)
            urbanMetrics.OnUrbanCellAdded(pos);
        else
            urbanMetrics.OnUrbanCellRemoved(pos);
    }

    private void OnUrbanCellsBulldozed(IReadOnlyList<Vector2Int> positions)
    {
        if (urbanMetrics != null)
            urbanMetrics.OnUrbanCellsBulldozed(positions);
    }

    public void ProcessTick()
    {
        if (zoneManager == null || growthBudgetManager == null || gridManager == null || cityStats == null)
            return;

        if (urbanMetrics != null && gridManager != null)
            urbanMetrics.RecalculateFromGrid(gridManager);

        if (!cityStats.simulateGrowth)
            return;

        int budget = growthBudgetManager.GetAvailableBudget(GrowthCategory.Zoning);
        if (budget <= 0)
        {
            return;
        }

        Vector2 centroid = urbanMetrics != null ? urbanMetrics.GetCentroid() : new Vector2(gridManager.width / 2f, gridManager.height / 2f);
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

            UrbanRing ring = urbanMetrics != null ? urbanMetrics.GetUrbanRing(new Vector2(p.x, p.y)) : UrbanRing.Mid;
            if (!ShouldZoneInRing(ring))
            {
                skippedOther++;
                continue;
            }

            Zone.ZoneType zoneType = SelectZoneType(p);
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

    private bool ShouldZoneInRing(UrbanRing ring)
    {
        switch (ring)
        {
            case UrbanRing.Core:
            case UrbanRing.Inner:
                return true;
            case UrbanRing.Mid:
                return Random.value < 0.85f;
            case UrbanRing.Outer:
                return Random.value < 0.65f;
            case UrbanRing.Edge:
                return Random.value < 0.50f;
            case UrbanRing.Rural:
                return Random.value < 0.25f;
            default:
                return true;
        }
    }

    private Zone.ZoneType SelectZoneType(Vector2Int candidate)
    {
        UrbanRing ring = urbanMetrics != null ? urbanMetrics.GetUrbanRing(new Vector2(candidate.x, candidate.y)) : UrbanRing.Mid;
        RingZoneProbabilities probs = urbanMetrics != null ? urbanMetrics.GetBaseZoneProbabilities(ring) : new RingZoneProbabilities { residential = 0.33f, commercial = 0.33f, industrial = 0.34f };

        float r = probs.residential;
        float c = probs.commercial;
        float i = probs.industrial;

        ApplyNeighborInfluence(ref r, ref c, ref i, candidate.x, candidate.y);

        if (demandManager != null)
        {
            if (demandManager.GetResidentialDemand().demandLevel <= 0) r = 0;
            if (demandManager.GetCommercialDemand().demandLevel <= 0) c = 0;
            if (demandManager.GetIndustrialDemand().demandLevel <= 0) i = 0;
        }

        if (r <= 0 && c <= 0 && i <= 0)
        {
            if (demandManager != null)
            {
                if (demandManager.GetResidentialDemand().demandLevel > 0) r = 0.33f;
                if (demandManager.GetCommercialDemand().demandLevel > 0) c = 0.33f;
                if (demandManager.GetIndustrialDemand().demandLevel > 0) i = 0.33f;
            }
            if (r <= 0 && c <= 0 && i <= 0)
            {
                r = 0.33f;
                c = 0.33f;
                i = 0.34f;
            }
        }

        float total = r + c + i;
        if (total <= 0) return Zone.ZoneType.ResidentialLightZoning;

        r /= total;
        c /= total;
        i /= total;

        float roll = Random.value;
        Zone.ZoneType baseType;
        if (roll < r)
            baseType = Zone.ZoneType.ResidentialLightZoning;
        else if (roll < r + c)
            baseType = Zone.ZoneType.CommercialLightZoning;
        else
            baseType = Zone.ZoneType.IndustrialLightZoning;

        return ApplyDensityToZoneType(baseType, ring);
    }

    /// <summary>Applies Light/Medium/Heavy density based on urban ring (FEAT-29).</summary>
    private Zone.ZoneType ApplyDensityToZoneType(Zone.ZoneType lightType, UrbanRing ring)
    {
        RingZoningDensity density = urbanMetrics != null ? urbanMetrics.GetZoningDensityForRing(ring) : new RingZoningDensity { lightProb = 0.90f, mediumProb = 0.08f, heavyProb = 0.02f };
        float roll = Random.value;
        if (roll < density.lightProb) return lightType;
        if (roll < density.lightProb + density.mediumProb) return LightToMedium(lightType);
        return LightToHeavy(lightType);
    }

    private static Zone.ZoneType LightToMedium(Zone.ZoneType lightType)
    {
        switch (lightType)
        {
            case Zone.ZoneType.ResidentialLightZoning: return Zone.ZoneType.ResidentialMediumZoning;
            case Zone.ZoneType.CommercialLightZoning: return Zone.ZoneType.CommercialMediumZoning;
            case Zone.ZoneType.IndustrialLightZoning: return Zone.ZoneType.IndustrialMediumZoning;
            default: return lightType;
        }
    }

    private static Zone.ZoneType LightToHeavy(Zone.ZoneType lightType)
    {
        switch (lightType)
        {
            case Zone.ZoneType.ResidentialLightZoning: return Zone.ZoneType.ResidentialHeavyZoning;
            case Zone.ZoneType.CommercialLightZoning: return Zone.ZoneType.CommercialHeavyZoning;
            case Zone.ZoneType.IndustrialLightZoning: return Zone.ZoneType.IndustrialHeavyZoning;
            default: return lightType;
        }
    }

    private void ApplyNeighborInfluence(ref float r, ref float c, ref float i, int x, int y)
    {
        int rCount = 0, cCount = 0, iCount = 0;
        for (int d = 0; d < 4; d++)
        {
            int nx = x + Dx[d], ny = y + Dy[d];
            if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height) continue;
            Cell neighbor = gridManager.GetCell(nx, ny);
            if (neighbor == null) continue;

            if (IsResidentialZoneOrBuilding(neighbor.zoneType)) rCount++;
            else if (IsCommercialZoneOrBuilding(neighbor.zoneType)) cCount++;
            else if (IsIndustrialZoneOrBuilding(neighbor.zoneType)) iCount++;
        }

        r *= Mathf.Pow(COHERENCE_BOOST, rCount);
        c *= Mathf.Pow(COHERENCE_BOOST, cCount);
        i *= Mathf.Pow(COHERENCE_BOOST, iCount);

        if (rCount > 0 || cCount > 0)
            i *= industrialSeparation;
        if (iCount > 0)
        {
            r *= industrialSeparation;
            c *= industrialSeparation;
        }
    }

    private static bool IsResidentialZoneOrBuilding(Zone.ZoneType t)
    {
        return t == Zone.ZoneType.ResidentialLightZoning || t == Zone.ZoneType.ResidentialMediumZoning || t == Zone.ZoneType.ResidentialHeavyZoning ||
               t == Zone.ZoneType.ResidentialLightBuilding || t == Zone.ZoneType.ResidentialMediumBuilding || t == Zone.ZoneType.ResidentialHeavyBuilding;
    }

    private static bool IsCommercialZoneOrBuilding(Zone.ZoneType t)
    {
        return t == Zone.ZoneType.CommercialLightZoning || t == Zone.ZoneType.CommercialMediumZoning || t == Zone.ZoneType.CommercialHeavyZoning ||
               t == Zone.ZoneType.CommercialLightBuilding || t == Zone.ZoneType.CommercialMediumBuilding || t == Zone.ZoneType.CommercialHeavyBuilding;
    }

    private static bool IsIndustrialZoneOrBuilding(Zone.ZoneType t)
    {
        return t == Zone.ZoneType.IndustrialLightZoning || t == Zone.ZoneType.IndustrialMediumZoning || t == Zone.ZoneType.IndustrialHeavyZoning ||
               t == Zone.ZoneType.IndustrialLightBuilding || t == Zone.ZoneType.IndustrialMediumBuilding || t == Zone.ZoneType.IndustrialHeavyBuilding;
    }

    private List<Vector2Int> GetCandidatesAdjacentToRoad()
    {
        var nearRoad = gridManager.GetCellsWithinDistanceOfRoad(MaxRoadDistanceForZoning);
        var candidates = new List<Vector2Int>();
        foreach (var p in nearRoad)
        {
            if (p.x < 0 || p.x >= gridManager.width || p.y < 0 || p.y >= gridManager.height) continue;
            Cell c = gridManager.GetCell(p.x, p.y);
            if (c == null || !gridManager.IsZoneableNeighbor(c, p.x, p.y)) continue;
            candidates.Add(p);
        }
        return candidates;
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
}
}
