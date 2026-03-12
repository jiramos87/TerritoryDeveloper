using UnityEngine;
using System.Collections.Generic;
using System;
using Territory.Core;
using Territory.Roads;
using Territory.Zones;
using Territory.Economy;
using Territory.Timing;
using Territory.UI;
using Territory.Utilities;

namespace Territory.Simulation
{
/// <summary>
/// Manages urban expansion proposals that extend the city's road and zone network.
/// Periodically evaluates growth conditions, generates road layout proposals from existing
/// road edges, and creates zone proposals adjacent to new roads. Coordinates with GridManager
/// for pathfinding, RoadManager for road placement, and DemandManager for demand-based decisions.
/// </summary>
public class UrbanizationProposalManager : MonoBehaviour
{
    public GridManager gridManager;
    public RoadManager roadManager;
    public ZoneManager zoneManager;
    public CityStats cityStats;
    public DemandManager demandManager;
    public UrbanCentroidService urbanCentroidService;

    public int proposalEvaluationIntervalDays = 30;
    public int minDistanceFromCentroid = 15;
    public int proposalLayoutSize = 5;
    public GameObject proposalUIPrefab;

    private List<UrbanizationProposal> pendingProposals = new List<UrbanizationProposal>();
    private List<GameObject> previewRoots = new List<GameObject>();
    private List<GameObject> proposalUIs = new List<GameObject>();
    private int lastProposalDay = -9999;
    private HashSet<string> usedProposalNames = new HashSet<string>();
    private System.Random rng = new System.Random();

    void Start()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
        if (zoneManager == null) zoneManager = FindObjectOfType<ZoneManager>();
        if (cityStats == null) cityStats = FindObjectOfType<CityStats>();
        if (demandManager == null) demandManager = FindObjectOfType<DemandManager>();
        if (urbanCentroidService == null) urbanCentroidService = FindObjectOfType<UrbanCentroidService>();
    }

    public void ProcessTick()
    {
        if (cityStats == null || !cityStats.simulateGrowth) return;
        var timeManager = FindObjectOfType<TimeManager>();
        if (timeManager == null) return;
        int day = timeManager.GetCurrentDate().Day;
        int daysSinceEpoch = (timeManager.GetCurrentDate().Year - 2024) * 365 + timeManager.GetCurrentDate().Month * 31 + day;
        if (daysSinceEpoch - lastProposalDay < proposalEvaluationIntervalDays) return;

        lastProposalDay = daysSinceEpoch;
        int maxProposals = Mathf.Max(0, cityStats.population / 500);
        if (pendingProposals.Count >= maxProposals) return;

        var proposal = GenerateProposal();
        if (proposal != null)
        {
            pendingProposals.Add(proposal);
            CreatePreview(proposal);
            CreateProposalUI(proposal);
        }
    }

    public List<UrbanizationProposal> GetPendingProposals()
    {
        return new List<UrbanizationProposal>(pendingProposals);
    }

    public void RestorePendingProposals(List<UrbanizationProposal> proposals)
    {
        if (proposals == null) return;
        pendingProposals.Clear();
        ClearAllPreviewsAndUIs();
        foreach (var p in proposals)
        {
            if (p.Status != ProposalStatus.Pending) continue;
            p.Status = ProposalStatus.Pending;
            pendingProposals.Add(p);
            CreatePreview(p);
            CreateProposalUI(p);
        }
    }

    private UrbanizationProposal GenerateProposal()
    {
        Vector2 centroid = urbanCentroidService != null ? urbanCentroidService.GetCentroid() : GetFallbackCentroid();
        Vector2Int? anchor = FindRemoteAnchor(centroid);
        if (!anchor.HasValue) return null;

        int size = proposalLayoutSize;
        var cells = new List<ProposedCell>();
        int roadCost = RoadManager.RoadCostPerTile;
        int zoneCost = 2;
        int totalCost = 0;

        for (int ox = 0; ox < size; ox++)
        {
            for (int oy = 0; oy < size; oy++)
            {
                bool isRoad = (oy == 0 || oy == size - 1);
                var cell = new ProposedCell();
                cell.Offset = new Vector2Int(ox, oy);
                if (isRoad)
                {
                    cell.ZoneType = Zone.ZoneType.Road;
                    totalCost += roadCost;
                }
                else
                {
                    cell.ZoneType = GetZoneTypeByDemand();
                    totalCost += zoneCost;
                }
                cells.Add(cell);
            }
        }

        Vector2Int connectTo = FindNearestRoadOrInterstate(anchor.Value);
        Vector2Int pathStart = anchor.Value + new Vector2Int(size / 2, size);
        var path = gridManager.FindPath(pathStart, connectTo);
        if (path != null && path.Count > 0)
        {
            foreach (var p in path)
            {
                if (p.x < anchor.Value.x || p.x >= anchor.Value.x + size || p.y < anchor.Value.y || p.y >= anchor.Value.y + size)
                {
                    totalCost += roadCost;
                    var pc = new ProposedCell();
                    pc.Offset = p - anchor.Value;
                    pc.ZoneType = Zone.ZoneType.Road;
                    cells.Add(pc);
                }
            }
        }

        if (totalCost > cityStats.money) return null;

        string name = CityNameGenerator.Generate(rng);
        usedProposalNames.Add(name);

        var proposal = new UrbanizationProposal();
        proposal.proposalName = name;
        proposal.AnchorPosition = anchor.Value;
        proposal.cells = cells;
        proposal.totalCost = totalCost;
        proposal.Status = ProposalStatus.Pending;
        return proposal;
    }

    private Zone.ZoneType GetZoneTypeByDemand()
    {
        if (demandManager == null) return Zone.ZoneType.ResidentialLightZoning;
        float r = demandManager.GetResidentialDemand().demandLevel;
        float c = demandManager.GetCommercialDemand().demandLevel;
        float i = demandManager.GetIndustrialDemand().demandLevel;
        if (r >= c && r >= i && r > 0) return Zone.ZoneType.ResidentialLightZoning;
        if (c >= i && c > 0) return Zone.ZoneType.CommercialLightZoning;
        return Zone.ZoneType.IndustrialLightZoning;
    }

    private Vector2 GetFallbackCentroid()
    {
        if (gridManager == null) return Vector2.zero;
        return new Vector2(gridManager.width / 2f, gridManager.height / 2f);
    }

    private Vector2Int? FindRemoteAnchor(Vector2 centroid)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            int ax = UnityEngine.Random.Range(0, gridManager.width - proposalLayoutSize + 1);
            int ay = UnityEngine.Random.Range(0, gridManager.height - proposalLayoutSize + 1);
            Vector2 anchorCenter = new Vector2(ax + proposalLayoutSize / 2f, ay + proposalLayoutSize / 2f);
            if (urbanCentroidService != null)
            {
                UrbanRing ring = urbanCentroidService.GetUrbanRing(anchorCenter);
                if (ring == UrbanRing.Inner || ring == UrbanRing.Mid)
                    continue;
            }
            else
            {
                float dist = Vector2.Distance(anchorCenter, centroid);
                if (dist < minDistanceFromCentroid) continue;
            }
            bool allGrass = true;
            for (int ox = 0; ox < proposalLayoutSize && allGrass; ox++)
                for (int oy = 0; oy < proposalLayoutSize && allGrass; oy++)
                {
                    Cell c = gridManager.GetCell(ax + ox, ay + oy);
                    if (c == null || c.zoneType != Zone.ZoneType.Grass) allGrass = false;
                }
            if (allGrass) return new Vector2Int(ax, ay);
        }
        return null;
    }

    private Vector2Int FindNearestRoadOrInterstate(Vector2Int from)
    {
        var roads = gridManager.GetAllRoadPositions();
        if (roads.Count == 0) return from;
        Vector2Int nearest = roads[0];
        int best = (roads[0].x - from.x) * (roads[0].x - from.x) + (roads[0].y - from.y) * (roads[0].y - from.y);
        foreach (var r in roads)
        {
            int d = (r.x - from.x) * (r.x - from.x) + (r.y - from.y) * (r.y - from.y);
            if (d < best) { best = d; nearest = r; }
        }
        return nearest;
    }

    private void CreatePreview(UrbanizationProposal proposal)
    {
        GameObject root = new GameObject("ProposalPreview_" + proposal.proposalName);
        previewRoots.Add(root);
        foreach (var cell in proposal.cells)
        {
            Vector2Int world = proposal.AnchorPosition + cell.Offset;
            if (world.x < 0 || world.x >= gridManager.width || world.y < 0 || world.y >= gridManager.height) continue;
            Cell c = gridManager.GetCell(world.x, world.y);
            if (c == null) continue;
            Vector3 worldPos = c.transformPosition;
            GameObject prefab = cell.ZoneType == Zone.ZoneType.Road
                ? roadManager.roadTilePrefab1
                : zoneManager.GetRandomZonePrefab(cell.ZoneType, 1);
            if (prefab == null) continue;
            GameObject tile = Instantiate(prefab, worldPos, Quaternion.identity);
            tile.transform.SetParent(root.transform);
            var sr = tile.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1, 1, 1, 0.5f);
            var collider = tile.GetComponent<Collider2D>();
            if (collider != null) Destroy(collider);
        }
    }

    private void CreateProposalUI(UrbanizationProposal proposal)
    {
        if (proposalUIPrefab == null) return;
        GameObject ui = Instantiate(proposalUIPrefab);
        proposalUIs.Add(ui);
        var controller = ui.GetComponent<ProposalUIController>();
        if (controller != null) controller.SetProposal(proposal, this);
        Cell anchorCell = gridManager.GetCell(proposal.AnchorPosition.x, proposal.AnchorPosition.y);
        if (anchorCell != null)
        {
            Vector2 pos = anchorCell.transformPosition;
            ui.transform.position = new Vector3(pos.x, pos.y, 0f) + Vector3.up * 2f;
            var canvas = ui.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace) { }
        }
    }

    public void AcceptProposal(UrbanizationProposal proposal)
    {
        if (proposal == null || !cityStats.CanAfford(proposal.totalCost)) return;
        int idx = pendingProposals.IndexOf(proposal);
        if (idx < 0) return;

        cityStats.RemoveMoney(proposal.totalCost);
        var cellPositions = new List<Vector2Int>();
        foreach (var cell in proposal.cells)
        {
            Vector2Int world = proposal.AnchorPosition + cell.Offset;
            if (world.x < 0 || world.x >= gridManager.width || world.y < 0 || world.y >= gridManager.height) continue;
            if (cell.ZoneType == Zone.ZoneType.Road)
                roadManager.PlaceRoadTileAt(new Vector2(world.x, world.y));
            else
                zoneManager.PlaceZoneAt(new Vector2(world.x, world.y), cell.ZoneType);
            cellPositions.Add(world);
        }

        var commune = new CommuneData();
        commune.communeName = proposal.proposalName;
        commune.Anchor = proposal.AnchorPosition;
        commune.SetCellPositions(cellPositions);
        cityStats.communes.Add(commune);
        proposal.Status = ProposalStatus.Accepted;

        RemoveProposalAt(idx);
        if (GameNotificationManager.Instance != null)
            GameNotificationManager.Instance.PostInfo("Built " + proposal.proposalName);
    }

    public void RejectProposal(UrbanizationProposal proposal)
    {
        if (proposal == null) return;
        int idx = pendingProposals.IndexOf(proposal);
        if (idx < 0) return;
        proposal.Status = ProposalStatus.Rejected;
        RemoveProposalAt(idx);
    }

    private void RemoveProposalAt(int idx)
    {
        if (idx < 0 || idx >= pendingProposals.Count) return;
        pendingProposals.RemoveAt(idx);
        if (idx < previewRoots.Count && previewRoots[idx] != null)
            Destroy(previewRoots[idx]);
        previewRoots.RemoveAt(idx);
        if (idx < proposalUIs.Count && proposalUIs[idx] != null)
            Destroy(proposalUIs[idx]);
        proposalUIs.RemoveAt(idx);
    }

    private void ClearAllPreviewsAndUIs()
    {
        foreach (var go in previewRoots)
            if (go != null) Destroy(go);
        previewRoots.Clear();
        foreach (var go in proposalUIs)
            if (go != null) Destroy(go);
        proposalUIs.Clear();
    }
}
}
