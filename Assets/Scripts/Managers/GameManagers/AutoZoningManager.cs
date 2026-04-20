using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Zones;
using Territory.Economy;

namespace Territory.Simulation
{
/// <summary>
/// Auto-zone cells along completed road segments during sim steps.
/// Uses segment-based strip zoning: reads PendingZoningSegments from <see cref="AutoRoadBuilder"/>,
/// picks zone type by demand + density by urban ring, zones rectangular strips
/// perp to each segment.
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

    /// <summary>Max cells to zone per sim tick across all segments.</summary>
    public int maxZonedCellsPerTick = 32;
    /// <summary>Safety cap per tick; actual limit driven by growth budget. Kept high so budget controls volume.</summary>
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

    /// <summary>Return <see cref="UrbanMetrics"/> from service for back-compat (e.g. MiniMapController).</summary>
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
        if (cityStats.cityPowerOutput > 0 && !cityStats.GetCityPowerAvailability())
            return;

        int budget = growthBudgetManager.GetAvailableBudget(GrowthCategory.Zoning);
        if (budget <= 0)
            return;

        if (autoRoadBuilder == null || autoRoadBuilder.PendingZoningSegments == null)
            return;

        var pendingSegments = autoRoadBuilder.PendingZoningSegments;
        int placedThisTick = 0;
        var toRemove = new List<int>();

        var roadReservationCells = new HashSet<Vector2Int>(gridManager.GetRoadExtensionCells());
        roadReservationCells.UnionWith(gridManager.GetRoadAxialCorridorCells());

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

            int placed = ZoneSegmentStrip(ref seg, ref placedThisTick, ref budget, roadReservationCells);
            pendingSegments[i] = seg;

            // Segment complete when zonedUpToIndex has reached last cell index (L-1) — updated from L-2 (Fix A).
            if (seg.zonedUpToIndex >= seg.segment.length - 1)
                toRemove.Add(i);
        }

        for (int r = toRemove.Count - 1; r >= 0; r--)
            pendingSegments.RemoveAt(toRemove[r]);

        // Fix B — post-tick frontier re-scan: catch cells formerly in road reservation that have since
        // been freed, plus cells beyond segment endpoints that the strip loop never reached.
        // Cost bounded by |roadEdges| × 4 cardinal neighbors. Reuses CanZoneCell + existing budget gate.
        if (placedThisTick < MaxZonedCellsPerTickSafetyCap && budget > 0)
            ScanRoadFrontierForZoneable(ref placedThisTick, ref budget, roadReservationCells);
    }

    private static readonly int[] FrontierDx = { 1, -1, 0, 0 };
    private static readonly int[] FrontierDy = { 0, 0, 1, -1 };

    /// <summary>
    /// Post-tick frontier re-scan (Fix B). For each road-edge cell, check its 4 cardinal neighbors.
    /// Zone any neighbor that passes CanZoneCell and is not in roadReservationCells.
    /// Picks zone type by UrbanRing at the edge position.
    /// Bounded: iterates road edges × 4; respects MaxZonedCellsPerTickSafetyCap and growth budget.
    /// </summary>
    private void ScanRoadFrontierForZoneable(ref int placedThisTick, ref int budget, HashSet<Vector2Int> roadReservationCells)
    {
        var edges = gridManager.GetRoadEdgePositions();
        if (edges == null || edges.Count == 0)
            return;

        foreach (Vector2Int edge in edges)
        {
            if (placedThisTick >= MaxZonedCellsPerTickSafetyCap || budget <= 0)
                break;

            UrbanRing ring = urbanCentroidService != null
                ? urbanCentroidService.GetUrbanRing(new Vector2(edge.x, edge.y))
                : UrbanRing.Mid;
            Zone.ZoneType zoneBase = SelectZoneTypeForRing(ring);
            Zone.ZoneType zoneType = UrbanMetrics.ApplyDensityByRing(zoneBase, ring);

            var attrs = zoneManager.GetZoneAttributes(zoneType);
            if (attrs == null)
                continue;

            for (int d = 0; d < 4; d++)
            {
                if (placedThisTick >= MaxZonedCellsPerTickSafetyCap || budget < attrs.ConstructionCost)
                    break;

                Vector2Int neighbor = new Vector2Int(edge.x + FrontierDx[d], edge.y + FrontierDy[d]);
                if (!CanZoneCell(neighbor, roadReservationCells))
                    continue;

                if (zoneManager.PlaceZoneAt(new Vector2(neighbor.x, neighbor.y), zoneType))
                {
                    if (growthBudgetManager.TrySpend(GrowthCategory.Zoning, attrs.ConstructionCost))
                    {
                        placedThisTick++;
                        budget -= attrs.ConstructionCost;
                    }
                }
            }
        }
    }

    /// <summary>Zone strips along segment. Return cells placed this call.</summary>
    private int ZoneSegmentStrip(ref AutoRoadBuilder.PendingZoningSegment seg, ref int placedThisTick, ref int budget, HashSet<Vector2Int> roadReservationCells)
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

        // Check whether the origin end is a true endpoint (no road behind it → safe to zone k=0 perp strip).
        // If origin-dir is a road cell, this segment joins an existing road (T-joint) → skip k=0 to avoid
        // double-zoning the shared junction cell.
        Vector2Int behindOrigin = new Vector2Int(origin.x - dir.x, origin.y - dir.y);
        bool originIsEndpoint = behindOrigin.x < 0 || behindOrigin.x >= gridManager.width
            || behindOrigin.y < 0 || behindOrigin.y >= gridManager.height
            || gridManager.GetCell(behindOrigin.x, behindOrigin.y)?.zoneType != Zone.ZoneType.Road;

        // Extend upper bound from L-2 to L-1 to include the far endpoint perp strip (Fix A).
        for (int k = seg.zonedUpToIndex + 1; k <= L - 1; k++)
        {
            // Skip k=0 when origin is a T-joint (not a true endpoint) to avoid double-zoning the junction.
            if (k == 0 && !originIsEndpoint)
                continue;

            if (placedThisTick >= MaxZonedCellsPerTickSafetyCap || budget < attrs.ConstructionCost)
                break;

            bool leftDone = true;
            bool rightDone = true;

            for (int j = 1; j <= 4 && placedThisTick < MaxZonedCellsPerTickSafetyCap && budget >= attrs.ConstructionCost; j++)
            {
                Vector2Int cellLeft = new Vector2Int(origin.x + k * dir.x + j * perp.x, origin.y + k * dir.y + j * perp.y);
                if (CanZoneCell(cellLeft, roadReservationCells))
                {
                    if (zoneManager.PlaceZoneAt(new Vector2(cellLeft.x, cellLeft.y), zoneType))
                    {
                        if (growthBudgetManager.TrySpend(GrowthCategory.Zoning, attrs.ConstructionCost))
                        {
                            placed++;
                            placedThisTick++;
                            budget -= attrs.ConstructionCost;
                        }
                    }
                    else
                        leftDone = false;
                }
            }

            for (int j = 1; j <= 4 && placedThisTick < MaxZonedCellsPerTickSafetyCap && budget >= attrs.ConstructionCost; j++)
            {
                Vector2Int cellRight = new Vector2Int(origin.x + k * dir.x - j * perp.x, origin.y + k * dir.y - j * perp.y);
                if (CanZoneCell(cellRight, roadReservationCells))
                {
                    if (zoneManager.PlaceZoneAt(new Vector2(cellRight.x, cellRight.y), zoneType))
                    {
                        if (growthBudgetManager.TrySpend(GrowthCategory.Zoning, attrs.ConstructionCost))
                        {
                            placed++;
                            placedThisTick++;
                            budget -= attrs.ConstructionCost;
                        }
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

    /// <summary>True if cell zoneable: in bounds, not water, Grass or has forest, not road/interstate; not in road extension or axial corridor.</summary>
    private bool CanZoneCell(Vector2Int cell, HashSet<Vector2Int> roadReservationCells)
    {
        if (cell.x < 0 || cell.x >= gridManager.width || cell.y < 0 || cell.y >= gridManager.height)
            return false;

        if (roadReservationCells != null && roadReservationCells.Contains(cell))
            return false;

        CityCell c = gridManager.GetCell(cell.x, cell.y);
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

    /// <summary>
    /// Pick zone type (R, C, or I) for a given urban ring, based on current demand.
    /// Extracted from <see cref="SelectZoneTypeForSegment"/> to allow callers that know only the ring
    /// (e.g. post-tick frontier re-scan — Fix B).
    /// Industrial excluded in Inner ring.
    /// </summary>
    private Zone.ZoneType SelectZoneTypeForRing(UrbanRing ring)
    {
        float r = demandManager != null ? demandManager.GetResidentialDemand().demandLevel : 0f;
        float c = demandManager != null ? demandManager.GetCommercialDemand().demandLevel : 0f;
        float i = demandManager != null ? demandManager.GetIndustrialDemand().demandLevel : 0f;

        var candidates = new List<Zone.ZoneType>();
        if (r > 0f) candidates.Add(Zone.ZoneType.ResidentialLightZoning);
        if (c > 0f) candidates.Add(Zone.ZoneType.CommercialLightZoning);
        if (i > 0f) candidates.Add(Zone.ZoneType.IndustrialLightZoning);

        if (ring == UrbanRing.Inner)
            candidates.RemoveAll(z => z == Zone.ZoneType.IndustrialLightZoning);

        // Zone S is manual-only in MVP — see docs/zone-s-economy-exploration.md §Q2
        candidates.RemoveAll(z => ZoneManager.IsStateServiceZoneType(z));

        if (candidates.Count == 0)
            return Zone.ZoneType.ResidentialLightZoning;

        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>Pick zone type (R, C, or I) for segment based on demand. Equal prob among types with demand > 0. Industrial excluded in Inner ring.</summary>
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

        // Zone S is manual-only in MVP — see docs/zone-s-economy-exploration.md §Q2
        candidates.RemoveAll(z => ZoneManager.IsStateServiceZoneType(z));

        if (candidates.Count == 0)
            return Zone.ZoneType.ResidentialLightZoning;

        return candidates[Random.Range(0, candidates.Count)];
    }
}
}
