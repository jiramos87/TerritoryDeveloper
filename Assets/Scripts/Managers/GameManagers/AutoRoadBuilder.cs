using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Roads;
using Territory.Economy;
using Territory.Terrain;
using Territory.Zones;

namespace Territory.Simulation
{
/// <summary>
/// Automatically extends the road network during simulation steps by finding optimal paths from existing road edges.
/// Coordinates with GridManager for pathfinding, RoadManager for road placement, and TerrainManager for terrain constraints.
/// </summary>
public class AutoRoadBuilder : MonoBehaviour
{
    #region Dependencies
    public GridManager gridManager;
    public RoadManager roadManager;
    public GrowthBudgetManager growthBudgetManager;
    public CityStats cityStats;
    public InterstateManager interstateManager;
    public TerrainManager terrainManager;
    #endregion

    #region Configuration
    [Header("Budget per tick")]
    public int maxTilesPerTick = 10;

    [Header("Street project: linear growth")]
    public int minStreetLength = 8;
    /// <summary>When edges &lt; 4 (recovery), use this lower min length so short streets can open new fronts.</summary>
    public int minStreetLengthRecovery = 3;
    public int maxStreetLength = 20;
    public int maxActiveProjects = 5;
    public int tilesPerProjectPerTick = 3;
    /// <summary>When a street ends, chance to start a perpendicular street (forms blocks).</summary>
    [Range(0f, 1f)]
    public float chancePerpendicularAtEnd = 0.6f;
    /// <summary>Min tiles along a road before we allow a perpendicular branch (avoid dense grid).</summary>
    public int minSpacingForBranch = 6;
    /// <summary>Min distance from an existing parallel road when starting a new street from an edge (avoids adjacent parallel roads).</summary>
    public int minParallelSpacingFromEdge = 3;
    /// <summary>Min distance between two edges when starting new projects in the same tick (avoids multiple intersections in adjacent cells).</summary>
    public int minEdgeSpacing = 2;
    /// <summary>Ramas cada N: interval along the street to spawn a perpendicular branch. N is chosen at random per project between branchIntervalMin and branchIntervalMax (inclusive).</summary>
    public int branchIntervalMin = 4;
    public int branchIntervalMax = 8;
    /// <summary>Minimum length required to start a perpendicular branch (can be lower than minStreetLength so branches appear more often).</summary>
    public int minLengthForBranch = 3;
    /// <summary>Max water tiles in a straight line for auto bridge (bridge "less than 6 tiles" = up to 5 water cells).</summary>
    public const int MaxBridgeWaterTiles = 5;
    /// <summary>When connecting to interstate or between clusters, prefer paths that stay this many cells away from existing roads (0 = no preference).</summary>
    public int minRoadSpacingWhenConnecting = 2;

    private struct StreetProject
    {
        public Vector2Int tip;
        public Vector2Int dir;
        public int targetLength;
        public int builtLength;
        public int branchInterval;
        public int tilesSinceLastBranch;
    }

    private List<StreetProject> activeProjects = new List<StreetProject>();
    private static readonly int[] Dx = { 1, -1, 0, 0 };
    private static readonly int[] Dy = { 0, 0, 1, -1 };
    #endregion

    #region Road Extension Logic
    string SimDateStr()
    {
        return cityStats != null ? cityStats.currentDate.ToString("yyyy-MM-dd") : "?";
    }

    /// <summary>Places a road tile without invalidating cache. Caller must call gridManager.InvalidateRoadCache() once at end of batch.</summary>
    private bool PlaceRoadTileInBatch(Vector2 pos)
    {
        return roadManager.PlaceRoadTileAt(pos, invalidateCache: false);
    }

    void Start()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
        if (growthBudgetManager == null) growthBudgetManager = FindObjectOfType<GrowthBudgetManager>();
        if (cityStats == null) cityStats = FindObjectOfType<CityStats>();
        if (interstateManager == null) interstateManager = FindObjectOfType<InterstateManager>();
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
    }

    public void ProcessTick()
    {
        string d = SimDateStr();
        if (growthBudgetManager == null || roadManager == null || gridManager == null || cityStats == null)
        {
            Debug.Log($"[Sim {d}] [AutoRoadBuilder] ProcessTick: missing refs, skip.");
            return;
        }
        if (!cityStats.simulateGrowth)
            return;

        var edges = gridManager.GetRoadEdgePositions();
        var allRoads = gridManager.GetAllRoadPositions();
        var roadSet = new HashSet<Vector2Int>(allRoads);
        int edgeCount = edges.Count;
        int roadTilesTotal = allRoads.Count;
        int expropriatedCount = 0;
        if (edgeCount == 0)
            expropriatedCount = TryExpropriate(3, allRoads);
        else if (edgeCount < 4)
            expropriatedCount = TryExpropriate(1, allRoads);

        int available = growthBudgetManager.GetAvailableBudget(GrowthCategory.Roads);
        int costPerTile = RoadManager.RoadCostPerTile;
        int maxByBudget = costPerTile > 0 ? available / costPerTile : 0;
        int toPlace = Mathf.Min(maxTilesPerTick, maxByBudget);

        int placed = 0;
        int effectiveMinStreetLength = edgeCount < 4 ? minStreetLengthRecovery : minStreetLength;

        if (expropriatedCount > 0 && toPlace > 0)
        {
            placed = TryPlaceOneTileFromEdges(edges);
            if (placed > 0)
            {
                toPlace -= placed;
                if (toPlace <= 0)
                {
                    gridManager.InvalidateRoadCache();
                    return;
                }
            }
        }

        if (interstateManager != null && !interstateManager.IsConnectedToInterstate)
        {
            placed = TryConnectToInterstate(toPlace, allRoads);
            if (placed > 0)
            {
                gridManager.InvalidateRoadCache();
                return;
            }
        }

        List<List<Vector2Int>> clusters = GetRoadClusters(allRoads);
        if (clusters.Count > 1)
        {
            placed = TryConnectDisconnected(clusters, toPlace);
            if (placed > 0)
            {
                gridManager.InvalidateRoadCache();
                return;
            }
        }

        placed = AdvanceStreetProjects(toPlace, roadSet);
        if (placed > 0)
        {
            gridManager.InvalidateRoadCache();
            return;
        }

        int newProjectsStarted = 0;
        while (toPlace > 0 && activeProjects.Count < maxActiveProjects)
        {
            if (!TryStartNewStreetProject(ref toPlace, effectiveMinStreetLength, edges, roadSet))
                break;
            newProjectsStarted++;
        }
        if (newProjectsStarted == 0 && toPlace > 0 && activeProjects.Count < maxActiveProjects && effectiveMinStreetLength > minStreetLengthRecovery)
        {
            if (TryStartNewStreetProject(ref toPlace, minStreetLengthRecovery, edges, roadSet))
                newProjectsStarted++;
        }
        gridManager.InvalidateRoadCache();
    }

    /// <summary>Advance all active projects in a fixed direction; remove when done or blocked.</summary>
    private int AdvanceStreetProjects(int maxTiles, HashSet<Vector2Int> roadSet)
    {
        int totalPlaced = 0;
        int budget = maxTiles;
        for (int i = activeProjects.Count - 1; i >= 0; i--)
        {
            if (budget <= 0) break;
            int toPlaceThis = Mathf.Min(tilesPerProjectPerTick, budget);
            int placed = AdvanceOneProject(i, toPlaceThis, roadSet);
            totalPlaced += placed;
            budget -= placed;
            if (placed == 0 || activeProjects[i].builtLength >= activeProjects[i].targetLength)
            {
                if (activeProjects[i].builtLength >= minStreetLength && Random.value < chancePerpendicularAtEnd)
                    TryStartPerpendicularProject(activeProjects[i].tip, activeProjects[i].dir, roadSet);
                activeProjects.RemoveAt(i);
            }
        }
        return totalPlaced;
    }

    private int AdvanceOneProject(int index, int maxPlace, HashSet<Vector2Int> roadSet)
    {
        if (index < 0 || index >= activeProjects.Count) return 0;
        StreetProject p = activeProjects[index];
        int placed = 0;
        int w = gridManager.width, h = gridManager.height;
        for (int n = 0; n < maxPlace && p.builtLength < p.targetLength; n++)
        {
            if (p.builtLength > 0 && p.tilesSinceLastBranch >= p.branchInterval)
            {
                TryStartPerpendicularProject(p.tip, p.dir, roadSet);
                p.tilesSinceLastBranch = 0;
            }

            int tx = p.tip.x, ty = p.tip.y;
            if (tx < 0 || tx >= w || ty < 0 || ty >= h) break;
            Cell c = gridManager.GetCell(tx, ty);
            if (c != null && c.zoneType == Zone.ZoneType.Road)
                break;
            if (!IsCellPlaceableForRoad(tx, ty))
                break;
            if (!IsSuitableForRoad(tx, ty, p.dir))
                break;
            if (!growthBudgetManager.TrySpend(GrowthCategory.Roads, RoadManager.RoadCostPerTile))
                break;
            if (!PlaceRoadTileInBatch(new Vector2(tx, ty)))
                break;
            placed++;
            p.tip = new Vector2Int(p.tip.x + p.dir.x, p.tip.y + p.dir.y);
            p.builtLength++;
            p.tilesSinceLastBranch++;
        }
        activeProjects[index] = p;
        return placed;
    }

    private void TryStartPerpendicularProject(Vector2Int fromTip, Vector2Int previousDir, HashSet<Vector2Int> roadSet)
    {
        Vector2Int perp1 = new Vector2Int(-previousDir.y, previousDir.x);
        Vector2Int perp2 = new Vector2Int(previousDir.y, -previousDir.x);
        foreach (Vector2Int perp in new[] { perp1, perp2 })
        {
            Vector2Int start = new Vector2Int(fromTip.x + perp.x, fromTip.y + perp.y);
            if (start.x < 0 || start.x >= gridManager.width || start.y < 0 || start.y >= gridManager.height)
                continue;
            if (!IsCellPlaceableForRoad(start.x, start.y))
                continue;
            if (!IsSuitableForRoad(start.x, start.y, perp))
                continue;
            if (HasParallelRoadTooClose(fromTip, perp, minSpacingForBranch, roadSet, excludeAlongDir: previousDir))
                continue;
            int len = HowFarWeCanBuild(start, perp);
            if (len < minLengthForBranch) continue;
            if (activeProjects.Count >= maxActiveProjects) return;
            int targetLen = Mathf.Clamp(Random.Range(minLengthForBranch, maxStreetLength + 1), minLengthForBranch, len);
            int interval = Mathf.Clamp(Random.Range(branchIntervalMin, branchIntervalMax + 1), 1, 99);
            activeProjects.Add(new StreetProject { tip = start, dir = perp, targetLength = targetLen, builtLength = 0, branchInterval = interval, tilesSinceLastBranch = 0 });
            return;
        }
    }

    private bool TryStartNewStreetProject(ref int budgetRemaining, int effectiveMinStreetLength, List<Vector2Int> edges, HashSet<Vector2Int> roadSet)
    {
        if (edges.Count == 0) return false;
        int parallelSpacing = activeProjects.Count == 0 ? minParallelSpacingFromEdge : (activeProjects.Count < 2 ? Mathf.Max(minParallelSpacingFromEdge, minSpacingForBranch / 2) : minSpacingForBranch);
        var withScore = new List<KeyValuePair<Vector2Int, int>>(edges.Count);
        foreach (Vector2Int e in edges)
            withScore.Add(new KeyValuePair<Vector2Int, int>(e, CountGrassNeighbors(e)));
        withScore.Sort((a, b) => b.Value.CompareTo(a.Value));
        var consideredEdges = new HashSet<Vector2Int>();
        const int maxRejectLogsPerTick = 8;
        int rejectLogCount = 0;
        foreach (var kv in withScore)
        {
            Vector2Int edge = kv.Key;
            if (minEdgeSpacing > 0)
            {
                bool tooCloseToConsidered = false;
                foreach (Vector2Int c in consideredEdges)
                {
                    if (Mathf.Abs(edge.x - c.x) + Mathf.Abs(edge.y - c.y) < minEdgeSpacing)
                    {
                        tooCloseToConsidered = true;
                        break;
                    }
                }
                if (tooCloseToConsidered) continue;
                consideredEdges.Add(edge);
            }
            for (int d = 0; d < 4; d++)
            {
                int nx = edge.x + Dx[d], ny = edge.y + Dy[d];
                if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height)
                    continue;
                if (!IsCellPlaceableForRoad(nx, ny))
                {
                    continue;
                }
                Vector2Int dir = new Vector2Int(Dx[d], Dy[d]);
                Vector2Int tip = new Vector2Int(nx, ny);
                if (!IsSuitableForRoad(nx, ny, dir))
                {
                    continue;
                }
                if (HasParallelRoadTooClose(edge, dir, parallelSpacing, roadSet))
                {
                    continue;
                }
                int len = HowFarWeCanBuild(tip, dir);
                if (len < effectiveMinStreetLength)
                {

                    continue;
                }
                int targetLen = Mathf.Clamp(Random.Range(effectiveMinStreetLength, maxStreetLength + 1), effectiveMinStreetLength, len);
                int interval = Mathf.Clamp(Random.Range(branchIntervalMin, branchIntervalMax + 1), 1, 99);
                activeProjects.Add(new StreetProject { tip = tip, dir = dir, targetLength = targetLen, builtLength = 0, branchInterval = interval, tilesSinceLastBranch = 0 });
                int advance = AdvanceOneProject(activeProjects.Count - 1, Mathf.Min(tilesPerProjectPerTick, budgetRemaining), roadSet);
                budgetRemaining -= advance;
                return true;
            }
        }

        return false;
    }
    #endregion

    #region Road Placement
    /// <summary>
    /// When road edges are few, demolish up to maxCount buildings/zoning adjacent to roads to create outlets.
    /// When edges==0, call with maxCount 3 to create a corridor. Picks cells adjacent to the most road cells.
    /// Excludes PowerPlant and WaterPlant. Returns number demolished.
    /// </summary>
    private int TryExpropriate(int maxCount, List<Vector2Int> roadPositions)
    {
        if (roadPositions.Count == 0) return 0;

        var roadSet = new HashSet<Vector2Int>(roadPositions);
        var candidateScore = new Dictionary<Vector2Int, int>();

        for (int i = 0; i < roadPositions.Count; i++)
        {
            Vector2Int r = roadPositions[i];
            for (int d = 0; d < 4; d++)
            {
                int nx = r.x + Dx[d], ny = r.y + Dy[d];
                if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height) continue;
                var n = new Vector2Int(nx, ny);
                if (roadSet.Contains(n)) continue;
                Cell c = gridManager.GetCell(nx, ny);
                if (c == null || c.zoneType == Zone.ZoneType.Grass) continue;
                if (!candidateScore.ContainsKey(n)) candidateScore[n] = 0;
                candidateScore[n]++;
            }
        }

        var sorted = new List<KeyValuePair<Vector2Int, int>>(candidateScore.Count);
        foreach (var kv in candidateScore)
            sorted.Add(kv);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

        int demolished = 0;
        for (int i = 0; i < sorted.Count && demolished < maxCount; i++)
        {
            Vector2Int pos = sorted[i].Key;
            Cell cell = gridManager.GetCell(pos.x, pos.y);
            if (cell == null) continue;
            string buildingType = cell.GetBuildingType();
            if (buildingType == "PowerPlant" || buildingType == "WaterPlant") continue;
            if (gridManager.DemolishCellAt(new Vector2(pos.x, pos.y), showAnimation: false))
            {
                demolished++;
            }
        }

        return demolished;
    }

    /// <summary>Place at most one road tile from current road edges (used after expropriation to fill freed space first).</summary>
    private int TryPlaceOneTileFromEdges(List<Vector2Int> edges)
    {
        if (edges.Count == 0)
        {
            return 0;
        }
        var withScore = new List<KeyValuePair<Vector2Int, int>>(edges.Count);
        foreach (Vector2Int e in edges)
            withScore.Add(new KeyValuePair<Vector2Int, int>(e, CountGrassNeighbors(e)));
        withScore.Sort((a, b) => b.Value.CompareTo(a.Value));
        foreach (var kv in withScore)
        {
            Vector2Int edge = kv.Key;
            for (int d = 0; d < 4; d++)
            {
                int nx = edge.x + Dx[d], ny = edge.y + Dy[d];
                if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height)
                    continue;
                if (!IsCellPlaceableForRoad(nx, ny))
                    continue;
                if (!growthBudgetManager.TrySpend(GrowthCategory.Roads, RoadManager.RoadCostPerTile))
                    continue;
                if (PlaceRoadTileInBatch(new Vector2(nx, ny)))
                    return 1;
                return 0; // presupuesto gastado pero colocación falló
            }
        }
        return 0;
    }
    #endregion

    #region Path Planning
    private int HowFarWeCanBuild(Vector2Int start, Vector2Int dir)
    {
        int count = 0;
        int waterRun = 0;
        int x = start.x, y = start.y;
        int w = gridManager.width, h = gridManager.height;
        while (x >= 0 && x < w && y >= 0 && y < h)
        {
            if (!IsCellPlaceableForRoad(x, y))
                break;
            if (!IsSuitableForRoad(x, y, dir))
                break;
            Cell c = gridManager.GetCell(x, y);
            bool isWater = c != null && c.GetCellInstanceHeight() == 0;
            if (isWater)
            {
                int landStep = -1;
                for (int k = 1; k <= MaxBridgeWaterTiles; k++)
                {
                    int nx = x + k * dir.x, ny = y + k * dir.y;
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) break;
                    Cell nc = gridManager.GetCell(nx, ny);
                    if (nc == null || nc.GetCellInstanceHeight() == 0) continue;
                    if (!IsCellPlaceableForRoad(nx, ny) || !IsSuitableForRoad(nx, ny, dir)) continue;
                    landStep = k;
                    break;
                }
                if (landStep < 0)
                    break;
                count += (1 + landStep);
                x += (landStep + 1) * dir.x;
                y += (landStep + 1) * dir.y;
                waterRun = 0;
                continue;
            }
            waterRun = 0;
            count++;
            x += dir.x;
            y += dir.y;
        }
        return count;
    }

    /// <summary>
    /// True if a road can be placed at (x,y): cell exists, not building/road/interstate, (Grass or has forest or water for bridge), and RoadManager allows placement.
    /// </summary>
    private bool IsCellPlaceableForRoad(int x, int y)
    {
        Cell c = gridManager.GetCell(x, y);
        if (c == null || c.zoneType == Zone.ZoneType.Road) return false;
        if (gridManager.IsCellOccupiedByBuilding(x, y) || c.isInterstate) return false;
        bool isLandPlaceable = c.zoneType == Zone.ZoneType.Grass || c.HasForest();
        bool isWaterForBridge = c.GetCellInstanceHeight() == 0;
        if (!isLandPlaceable && !isWaterForBridge) return false;
        return roadManager.CanPlaceRoadAt(new Vector2(x, y));
    }

    /// <summary>Returns a short reason why the cell is not placeable for road (for debug logs).</summary>
    private string GetCellPlaceableRejectReason(int x, int y)
    {
        Cell c = gridManager.GetCell(x, y);
        if (c == null) return "null cell";
        if (c.zoneType == Zone.ZoneType.Road) return "already road";
        if (gridManager.IsCellOccupiedByBuilding(x, y)) return "building";
        if (c.isInterstate) return "interstate";
        bool isLand = c.zoneType == Zone.ZoneType.Grass || c.HasForest();
        bool isWater = c.GetCellInstanceHeight() == 0;
        if (!isLand && !isWater) return "zone not grass/water";
        if (terrainManager != null && !terrainManager.CanPlaceRoad(x, y)) return "terrain/slope";
        if (roadManager != null && !roadManager.CanPlaceRoadAt(new Vector2(x, y))) return "RoadManager reject";
        return "unknown";
    }

    /// <summary>
    /// Number of cardinal neighbors of this road cell that are Grass (available for extension or zoning).
    /// </summary>
    private int CountGrassNeighbors(Vector2Int roadPos)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            int nx = roadPos.x + Dx[i], ny = roadPos.y + Dy[i];
            if (nx < 0 || nx >= gridManager.width || ny < 0 || ny >= gridManager.height) continue;
            Cell c = gridManager.GetCell(nx, ny);
            if (c != null && (c.zoneType == Zone.ZoneType.Grass || c.HasForest())) count++;
        }
        return count;
    }

    /// <summary>
    /// True if (x,y) is valid for a road in direction streetDir: flat and diagonal allowed; cardinal slope only if road runs N-S or E-W to match slope prefabs. Water (height 0) is always suitable for bridge.
    /// </summary>
    private bool IsSuitableForRoad(int x, int y, Vector2Int streetDir)
    {
        Cell c = gridManager.GetCell(x, y);
        if (c != null && c.GetCellInstanceHeight() == 0)
            return true;
        if (terrainManager == null) return true;
        TerrainSlopeType slope = terrainManager.GetTerrainSlopeTypeAt(x, y);
        switch (slope)
        {
            case TerrainSlopeType.Flat:
                return true;
            case TerrainSlopeType.North:
            case TerrainSlopeType.South:
                return streetDir.x == 0;
            case TerrainSlopeType.East:
            case TerrainSlopeType.West:
                return streetDir.y == 0;
            default:
                return true;
        }
    }

    /// <summary>
    /// True if there is already a road parallel to dir within minSpacing tiles of edge (would create too-dense grid).
    /// When branching perpendicular from a parent street, pass excludeAlongDir = parent direction so we don't count the parent segment as "parallel".
    /// </summary>
    private bool HasParallelRoadTooClose(Vector2Int edge, Vector2Int dir, int minSpacing, HashSet<Vector2Int> roadSet, Vector2Int? excludeAlongDir = null)
    {
        Vector2Int perp = new Vector2Int(-dir.y, dir.x);
        HashSet<Vector2Int> excludeSet = null;
        if (excludeAlongDir.HasValue)
        {
            excludeSet = new HashSet<Vector2Int>();
            Vector2Int e = excludeAlongDir.Value;
            for (int k = -minSpacing; k <= minSpacing; k++)
            {
                if (k == 0) continue;
                Vector2Int p = new Vector2Int(edge.x + e.x * k, edge.y + e.y * k);
                excludeSet.Add(p);
            }
        }
        for (int s = 1; s <= minSpacing; s++)
        {
            Vector2Int offset = new Vector2Int(edge.x + perp.x * s, edge.y + perp.y * s);
            if (offset.x < 0 || offset.x >= gridManager.width || offset.y < 0 || offset.y >= gridManager.height)
                continue;
            if (excludeSet != null && excludeSet.Contains(offset)) continue;
            if (roadSet.Contains(offset))
                return true;
            Vector2Int otherSide = new Vector2Int(edge.x - perp.x * s, edge.y - perp.y * s);
            if (otherSide.x >= 0 && otherSide.x < gridManager.width && otherSide.y >= 0 && otherSide.y < gridManager.height)
            {
                if (excludeSet != null && excludeSet.Contains(otherSide)) continue;
                if (roadSet.Contains(otherSide))
                    return true;
            }
        }
        return false;
    }
    #endregion

    #region Utility Methods
    private int TryConnectToInterstate(int maxTiles, List<Vector2Int> roadPositions)
    {
        if (interstateManager == null || gridManager == null || roadManager == null) return 0;
        if (roadPositions.Count == 0) return 0;
        var interstatePositions = interstateManager.InterstatePositions;
        if (interstatePositions == null || interstatePositions.Count == 0) return 0;

        Vector2Int from = roadPositions[Random.Range(0, roadPositions.Count)];
        Vector2Int to = interstatePositions[Random.Range(0, interstatePositions.Count)];
        var path = minRoadSpacingWhenConnecting > 0
            ? gridManager.FindPathWithRoadSpacing(from, to, minRoadSpacingWhenConnecting)
            : gridManager.FindPath(from, to);
        if (path == null || path.Count <= 1)
        {
            if (minRoadSpacingWhenConnecting > 0)
                path = gridManager.FindPath(from, to);
            if (path == null || path.Count <= 1) return 0;
        }
        int placed = 0;
        foreach (Vector2Int p in path)
        {
            if (placed >= maxTiles) break;
            if (gridManager.GetCell(p.x, p.y)?.zoneType == Zone.ZoneType.Road) continue;
            if (growthBudgetManager.TrySpend(GrowthCategory.Roads, RoadManager.RoadCostPerTile) && PlaceRoadTileInBatch(new Vector2(p.x, p.y)))
                placed++;
        }
        return placed;
    }

    private List<List<Vector2Int>> GetRoadClusters(List<Vector2Int> all)
    {
        var roadSet = new HashSet<Vector2Int>(all);
        var visited = new HashSet<Vector2Int>();
        var clusters = new List<List<Vector2Int>>();
        foreach (Vector2Int p in all)
        {
            if (visited.Contains(p)) continue;
            var cluster = new List<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(p);
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                if (visited.Contains(c)) continue;
                visited.Add(c);
                cluster.Add(c);
                for (int i = 0; i < 4; i++)
                {
                    int nx = c.x + Dx[i], ny = c.y + Dy[i];
                    var n = new Vector2Int(nx, ny);
                    if (roadSet.Contains(n) && !visited.Contains(n))
                        queue.Enqueue(n);
                }
            }
            if (cluster.Count > 0)
                clusters.Add(cluster);
        }
        return clusters;
    }

    private int TryConnectDisconnected(List<List<Vector2Int>> clusters, int maxTiles)
    {
        if (clusters.Count < 2) return 0;
        Vector2Int a = clusters[0][Random.Range(0, clusters[0].Count)];
        Vector2Int b = clusters[1][Random.Range(0, clusters[1].Count)];
        var path = minRoadSpacingWhenConnecting > 0
            ? gridManager.FindPathWithRoadSpacing(a, b, minRoadSpacingWhenConnecting)
            : gridManager.FindPath(a, b);
        if (path == null || path.Count <= 1)
        {
            if (minRoadSpacingWhenConnecting > 0)
                path = gridManager.FindPath(a, b);
            if (path == null || path.Count <= 1) return 0;
        }
        int placed = 0;
        foreach (Vector2Int p in path)
        {
            if (placed >= maxTiles) break;
            if (gridManager.GetCell(p.x, p.y)?.zoneType == Zone.ZoneType.Road) continue;
            if (growthBudgetManager.TrySpend(GrowthCategory.Roads, RoadManager.RoadCostPerTile) && PlaceRoadTileInBatch(new Vector2(p.x, p.y)))
                placed++;
        }
        return placed;
    }
    #endregion
}
}
