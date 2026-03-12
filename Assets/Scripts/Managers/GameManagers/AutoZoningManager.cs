using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;
using Territory.Economy;

namespace Territory.Simulation
{
/// <summary>
/// Automatically zones cells along completed road segments during simulation steps.
/// Uses segment-based strip zoning: reads PendingZoningSegments from AutoRoadBuilder,
/// selects zone type by demand and density by urban ring, zones rectangular strips
/// perpendicular to each segment.
/// </summary>
public class AutoZoningManager : MonoBehaviour
{
    public GridManager gridManager;
    public ZoneManager zoneManager;
    public GrowthBudgetManager growthBudgetManager;
    public CityStats cityStats;
    public DemandManager demandManager;
    public AutoRoadBuilder autoRoadBuilder;
    public UrbanCentroidService urbanCentroidService;

    /// <summary>Max cells to zone per simulation tick across all segments.</summary>
    public int maxZonedCellsPerTick = 32;
    /// <summary>Safety cap per tick; actual limit is driven by growth budget. Kept high so budget controls volume.</summary>
    private const int MaxZonedCellsPerTickSafetyCap = 300;

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
        if (autoRoadBuilder == null) autoRoadBuilder = FindObjectOfType<AutoRoadBuilder>();
        if (urbanCentroidService == null) urbanCentroidService = FindObjectOfType<UrbanCentroidService>();

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

    /// <summary>Returns UrbanMetrics from service for backward compatibility (e.g. MiniMapController).</summary>
    public UrbanMetrics GetUrbanMetrics()
    {
        return urbanCentroidService != null ? urbanCentroidService.GetUrbanMetrics() : null;
    }

    private void OnGridRestored()
    {
        if (urbanCentroidService != null)
            urbanCentroidService.RecalculateFromGrid();
        if (autoRoadBuilder != null && autoRoadBuilder.PendingZoningSegments != null)
            autoRoadBuilder.PendingZoningSegments.Clear();
    }

    private void OnUrbanCellChanged(Vector2 pos, bool isAdded)
    {
    }

    private void OnUrbanCellsBulldozed(IReadOnlyList<Vector2Int> positions)
    {
    }

    public void ProcessTick()
    {
        if (zoneManager == null || growthBudgetManager == null || gridManager == null || cityStats == null)
            return;

        if (!cityStats.simulateGrowth)
            return;

        int budget = growthBudgetManager.GetAvailableBudget(GrowthCategory.Zoning);
        if (budget <= 0)
            return;

        if (autoRoadBuilder == null || autoRoadBuilder.PendingZoningSegments == null)
            return;

        var pendingSegments = autoRoadBuilder.PendingZoningSegments;
        int placedThisTick = 0;
        var toRemove = new List<int>();

        for (int i = 0; i < pendingSegments.Count; i++)
        {
            if (placedThisTick >= MaxZonedCellsPerTickSafetyCap || budget <= 0)
                break;

            var seg = pendingSegments[i];
            if (seg.segment.length < 2)
            {
                toRemove.Add(i);
                continue;
            }

            int placed = ZoneSegmentStrip(ref seg, ref placedThisTick, ref budget);
            pendingSegments[i] = seg;

            if (seg.zonedUpToIndex >= seg.segment.length - 2)
                toRemove.Add(i);
        }

        for (int r = toRemove.Count - 1; r >= 0; r--)
            pendingSegments.RemoveAt(toRemove[r]);
    }

    /// <summary>Zones strips along a segment. Returns cells placed this call.</summary>
    private int ZoneSegmentStrip(ref AutoRoadBuilder.PendingZoningSegment seg, ref int placedThisTick, ref int budget)
    {
        int L = seg.segment.length;
        Vector2Int origin = seg.segment.origin;
        Vector2Int dir = seg.segment.dir;
        UrbanRing ring = seg.segment.ring;
        Vector2Int perp = new Vector2Int(-dir.y, dir.x);

        Zone.ZoneType zoneBase = SelectZoneTypeForSegment(seg.segment);
        Zone.ZoneType zoneType = UrbanMetrics.ApplyDensityByRing(zoneBase, ring);

        var attrs = zoneManager.GetZoneAttributes(zoneType);
        if (attrs == null)
            return 0;

        int placed = 0;
        int lastK = seg.zonedUpToIndex;

        for (int k = seg.zonedUpToIndex + 1; k <= L - 2; k++)
        {
            if (placedThisTick >= MaxZonedCellsPerTickSafetyCap || budget < attrs.ConstructionCost)
                break;

            bool leftDone = true;
            bool rightDone = true;

            for (int j = 1; j <= 4 && placedThisTick < MaxZonedCellsPerTickSafetyCap && budget >= attrs.ConstructionCost; j++)
            {
                Vector2Int cellLeft = new Vector2Int(origin.x + k * dir.x + j * perp.x, origin.y + k * dir.y + j * perp.y);
                if (CanZoneCell(cellLeft))
                {
                    if (growthBudgetManager.TrySpend(GrowthCategory.Zoning, attrs.ConstructionCost) && zoneManager.PlaceZoneAt(new Vector2(cellLeft.x, cellLeft.y), zoneType))
                    {
                        placed++;
                        placedThisTick++;
                        budget -= attrs.ConstructionCost;
                    }
                    else
                        leftDone = false;
                }
            }

            for (int j = 1; j <= 4 && placedThisTick < MaxZonedCellsPerTickSafetyCap && budget >= attrs.ConstructionCost; j++)
            {
                Vector2Int cellRight = new Vector2Int(origin.x + k * dir.x - j * perp.x, origin.y + k * dir.y - j * perp.y);
                if (CanZoneCell(cellRight))
                {
                    if (growthBudgetManager.TrySpend(GrowthCategory.Zoning, attrs.ConstructionCost) && zoneManager.PlaceZoneAt(new Vector2(cellRight.x, cellRight.y), zoneType))
                    {
                        placed++;
                        placedThisTick++;
                        budget -= attrs.ConstructionCost;
                    }
                    else
                        rightDone = false;
                }
            }

            if (leftDone && rightDone)
                lastK = k;
        }

        seg.zonedUpToIndex = lastK;
        return placed;
    }

    /// <summary>True if cell can be zoned: in bounds, not water, Grass or has forest, not road/interstate.</summary>
    private bool CanZoneCell(Vector2Int cell)
    {
        if (cell.x < 0 || cell.x >= gridManager.width || cell.y < 0 || cell.y >= gridManager.height)
            return false;

        Cell c = gridManager.GetCell(cell.x, cell.y);
        if (c == null)
            return false;

        if (c.GetCellInstanceHeight() == 0)
            return false;

        if (c.zoneType != Zone.ZoneType.Grass && !c.HasForest())
            return false;

        if (c.zoneType == Zone.ZoneType.Road || c.isInterstate)
            return false;

        if (!gridManager.IsZoneableNeighbor(c, cell.x, cell.y))
            return false;

        return true;
    }

    /// <summary>Selects zone type (R, C, or I) for the segment based on demand. Equal probability among types with demand > 0. Industrial is excluded in Inner ring.</summary>
    private Zone.ZoneType SelectZoneTypeForSegment(AutoRoadBuilder.CompletedSegment segment)
    {
        float r = demandManager != null ? demandManager.GetResidentialDemand().demandLevel : 0f;
        float c = demandManager != null ? demandManager.GetCommercialDemand().demandLevel : 0f;
        float i = demandManager != null ? demandManager.GetIndustrialDemand().demandLevel : 0f;

        var candidates = new List<Zone.ZoneType>();
        if (r > 0f) candidates.Add(Zone.ZoneType.ResidentialLightZoning);
        if (c > 0f) candidates.Add(Zone.ZoneType.CommercialLightZoning);
        if (i > 0f) candidates.Add(Zone.ZoneType.IndustrialLightZoning);

        if (segment.ring == UrbanRing.Inner)
            candidates.RemoveAll(z => z == Zone.ZoneType.IndustrialLightZoning);

        if (candidates.Count == 0)
            return Zone.ZoneType.ResidentialLightZoning;

        return candidates[Random.Range(0, candidates.Count)];
    }
}
}
