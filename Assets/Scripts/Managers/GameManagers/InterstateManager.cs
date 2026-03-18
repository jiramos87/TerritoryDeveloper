using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Terrain;
using Territory.Zones;
using Territory.Geography;

namespace Territory.Roads
{
/// <summary>
/// Manages the Interstate Highway: generation at game start, connectivity checks,
/// and validation for player road placement (streets must grow from the interstate).
/// </summary>
public class InterstateManager : MonoBehaviour
{
    #region Dependencies
    public GridManager gridManager;
    public TerrainManager terrainManager;
    public RoadManager roadManager;
    public TerraformingService terraformingService;
    #endregion

    #region Prefabs and Configuration
    private List<Vector2Int> interstatePositions = new List<Vector2Int>();
    private bool isConnectedToInterstate;

    /// <summary>Border and position of the interstate entry/exit points (set during generation or RebuildFromGrid).</summary>
    public Vector2Int? EntryPoint { get; private set; }
    public Vector2Int? ExitPoint { get; private set; }
    public int EntryBorder { get; private set; } = -1;
    public int ExitBorder { get; private set; } = -1;

    /// <summary>
    /// Whether the player's road network is connected to the interstate (updated monthly).
    /// </summary>
    public bool IsConnectedToInterstate => isConnectedToInterstate;

    /// <summary>
    /// Read-only list of grid positions that are part of the interstate.
    /// </summary>
    public IReadOnlyList<Vector2Int> InterstatePositions => interstatePositions;

    void Awake()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
        if (terraformingService == null) terraformingService = FindObjectOfType<TerraformingService>();
    }
    #endregion

    #region Interstate Placement
    /// <summary>
    /// Generate interstate route, place tiles using centralized terraform + resolve pipeline.
    /// Call after water map, before forest map.
    /// </summary>
    /// <param name="attemptOffset">Added to seed so each retry tries different paths.</param>
    /// <returns>True if a path was found and tiles were placed successfully.</returns>
    public bool GenerateAndPlaceInterstate(int attemptOffset = 0)
    {
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
        GenerateInterstateRoute(attemptOffset);
        Debug.Log($"[Interstate] GenerateAndPlaceInterstate: path.Count={interstatePositions?.Count ?? 0} roadManager={(roadManager != null ? "ok" : "null")}");
        if (interstatePositions.Count == 0 || roadManager == null)
        {
            if (interstatePositions.Count == 0)
                Debug.LogWarning("[Interstate] GenerateAndPlaceInterstate: path empty, skipping placement.");
            if (roadManager == null)
                Debug.LogWarning("[Interstate] GenerateAndPlaceInterstate: roadManager null, skipping placement.");
            return false;
        }

        bool placed = roadManager.PlaceInterstateFromPath(interstatePositions);
        Debug.Log($"[Interstate] GenerateAndPlaceInterstate: PlaceInterstateFromPath completed, placed={placed}");
        if (!placed)
            interstatePositions.Clear();
        return placed;
    }

    /// <summary>
    /// Tries all border cells deterministically until a path is found and placed.
    /// Call when random attempts fail. Guarantees interstate if any valid route exists.
    /// Shuffles candidate order per game run so routes vary instead of always starting at (0,0).
    /// </summary>
    public bool TryGenerateInterstateDeterministic()
    {
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
        if (roadManager == null) return false;

        var heightMap = terrainManager != null ? terrainManager.GetHeightMap() : null;
        if (heightMap == null || gridManager == null) return false;

        int w = gridManager.width;
        int h = gridManager.height;
        List<int> bordersWithLand = GetBordersWithLand(w, h, heightMap);
        if (bordersWithLand.Count < 2) return false;

        var candidates = new List<(int borderA, int borderB, Vector2Int entry, Vector2Int exit)>();
        for (int ia = 0; ia < bordersWithLand.Count; ia++)
        {
            int borderA = bordersWithLand[ia];
            int borderB = TerritoryData.OppositeBorder(borderA);
            if (!bordersWithLand.Contains(borderB)) continue;

            var entries = GetValidBorderCells(borderA, w, h, heightMap);
            if (entries.Count == 0) continue;

            var exits = GetValidBorderCells(borderB, w, h, heightMap);
            if (exits.Count == 0) continue;

            for (int ei = 0; ei < entries.Count; ei++)
            {
                for (int xi = 0; xi < exits.Count; xi++)
                {
                    Vector2Int entry = entries[ei];
                    Vector2Int exit = exits[xi];
                    if (entry != exit)
                        candidates.Add((borderA, borderB, entry, exit));
                }
            }
        }

        int runSeed = System.Environment.TickCount ^ (int)(Time.realtimeSinceStartup * 1000);
        Random.InitState(runSeed);
        Shuffle(candidates);

        const int maxDeterministicTries = 800;
        for (int i = 0; i < Mathf.Min(candidates.Count, maxDeterministicTries); i++)
        {
            var (borderA, borderB, entry, exit) = candidates[i];

            Random.InitState(InterstateGenSeed + 9999 + entry.x + entry.y * w + exit.x + exit.y * w);
            List<Vector2Int> path = FindInterstatePathAStar(entry, exit, w, h, heightMap);
            if (path == null || path.Count < 2 || path[path.Count - 1] != exit) continue;

            interstatePositions = path;
            EntryPoint = interstatePositions[0];
            ExitPoint = interstatePositions[interstatePositions.Count - 1];
            EntryBorder = borderA;
            ExitBorder = borderB;

            bool placed = roadManager.PlaceInterstateFromPath(interstatePositions);
            if (placed)
            {
                Debug.Log($"[Interstate] Deterministic: placed at {i + 1} tries, entry=({entry.x},{entry.y}) exit=({exit.x},{exit.y})");
                return true;
            }
            interstatePositions.Clear();
        }

        Debug.LogWarning($"[Interstate] Deterministic fallback: no valid path after {candidates.Count} candidates.");
        return false;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
    #endregion

    #region Interstate Generation
    const int MaxRouteAttempts = 80;
    const int PathTriesPerPair = 8;
    /// <summary>Base seed for interstate pathfinding. Combined with run-varying seed so each game yields different routes.</summary>
    const int InterstateGenSeed = 12345 + 100;

    /// <summary>
    /// Returns border indices (0=South, 1=North, 2=West, 3=East) that have at least one land cell valid for interstate.
    /// </summary>
    private static List<int> GetBordersWithLand(int w, int h, HeightMap heightMap)
    {
        var list = new List<int>();
        for (int b = 0; b < 4; b++)
        {
            if (HasAnyValidCellOnBorder(b, w, h, heightMap))
                list.Add(b);
        }
        return list;
    }

    private static bool HasAnyValidCellOnBorder(int border, int w, int h, HeightMap heightMap)
    {
        switch (border)
        {
            case 0:
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, 0, w, h, heightMap)) return true;
                break;
            case 1:
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, h - 1, w, h, heightMap)) return true;
                break;
            case 2:
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(0, y, w, h, heightMap)) return true;
                break;
            case 3:
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(w - 1, y, w, h, heightMap)) return true;
                break;
        }
        return false;
    }

    /// <summary>
    /// Generate interstate route and store positions. Retries until valid path exists (two land connections, path reaches exit).
    /// Does not place tiles (call GenerateAndPlaceInterstate after).
    /// </summary>
    /// <param name="attemptOffset">Added to seed so each retry tries different paths.</param>
    public List<Vector2Int> GenerateInterstateRoute(int attemptOffset = 0)
    {
        interstatePositions.Clear();
        EntryPoint = null;
        ExitPoint = null;
        EntryBorder = -1;
        ExitBorder = -1;

        if (gridManager == null)
        {
            Debug.LogWarning("InterstateManager: gridManager is null. Cannot generate interstate.");
            return interstatePositions;
        }
        if (terrainManager == null)
        {
            Debug.LogWarning("InterstateManager: terrainManager is null. Cannot generate interstate.");
            return interstatePositions;
        }
        HeightMap heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
        {
            Debug.LogWarning("InterstateManager: heightMap is null. Cannot generate interstate.");
            return interstatePositions;
        }

        int runSeed = System.Environment.TickCount ^ (int)(Time.realtimeSinceStartup * 1000);
        Random.InitState(InterstateGenSeed + attemptOffset + runSeed);

        int w = gridManager.width;
        int h = gridManager.height;
        List<int> bordersWithLand = GetBordersWithLand(w, h, heightMap);
        if (bordersWithLand.Count < 2)
        {
            Debug.LogWarning("InterstateManager: Fewer than 2 borders have land cells. Cannot place interstate.");
            return interstatePositions;
        }

        RegionalMapManager regionManager = FindObjectOfType<RegionalMapManager>();
        for (int attempt = 0; attempt < MaxRouteAttempts; attempt++)
        {
            int borderA, borderB;
            int rA = 0, rB = 0;
            bool useRegion = regionManager != null && regionManager.TryGetInterstateBorders(out rA, out rB)
                && bordersWithLand.Contains(rA);
            int oppositeB = useRegion ? TerritoryData.OppositeBorder(rA) : -1;

            if (useRegion && bordersWithLand.Contains(oppositeB))
            {
                borderA = rA;
                borderB = oppositeB;
            }
            else
            {
                int ia = Random.Range(0, bordersWithLand.Count);
                borderA = bordersWithLand[ia];
                borderB = TerritoryData.OppositeBorder(borderA);
                if (!bordersWithLand.Contains(borderB)) continue;
            }

            Vector2Int? entry = GetValidBorderCell(borderA, w, h, heightMap);
            Vector2Int? exit = GetValidBorderCell(borderB, w, h, heightMap);
            if (!entry.HasValue || !exit.HasValue) continue;

            for (int pathTry = 0; pathTry < PathTriesPerPair; pathTry++)
            {
                List<Vector2Int> path = FindInterstatePathAStar(entry.Value, exit.Value, w, h, heightMap);
                if (path != null && path.Count >= 2 && path[path.Count - 1] == exit.Value)
                {
                    interstatePositions = path;
                    EntryPoint = interstatePositions[0];
                    ExitPoint = interstatePositions[interstatePositions.Count - 1];
                    EntryBorder = borderA;
                    ExitBorder = borderB;
                    Debug.Log($"[Interstate] GenerateInterstateRoute: path found, {path.Count} cells from ({EntryPoint.Value.x},{EntryPoint.Value.y}) to ({ExitPoint.Value.x},{ExitPoint.Value.y})");
                    return interstatePositions;
                }
            }
        }

        Debug.LogWarning("InterstateManager: Could not find valid path after " + MaxRouteAttempts + " attempts. Interstate not placed.");
        return interstatePositions;
    }

    /// <summary>
    /// True if cell is valid for interstate: not water. Flat, cardinal, diagonal and corner slopes allowed; terraforming handles diagonals.
    /// </summary>
    private static bool IsCellAllowedForInterstate(int x, int y, int w, int h, HeightMap heightMap)
    {
        return heightMap.GetHeight(x, y) > 0;
    }

    /// <summary>
    /// Returns the only allowed first step from a border cell so the road enters in a cardinal (N-S or E-W) direction.
    /// </summary>
    private static Vector2Int? GetFirstStepFromBorder(Vector2Int start, int w, int h)
    {
        if (start.y == 0) return new Vector2Int(start.x, 1);
        if (start.y == h - 1) return new Vector2Int(start.x, h - 2);
        if (start.x == 0) return new Vector2Int(1, start.y);
        if (start.x == w - 1) return new Vector2Int(w - 2, start.y);
        return null;
    }

    const int MaxRiverWidthForBridge = 5;

    /// <summary>
    /// True if stepping onto this water cell is valid as a bridge: toward end there is a narrow strip of water then solid ground.
    /// </summary>
    private static bool IsValidBridgeSegment(List<Vector2Int> path, Vector2Int waterCell, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        if (path.Count == 0) return false;
        int dx = end.x - waterCell.x;
        int dy = end.y - waterCell.y;
        int stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
        int stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;
        if (stepX == 0 && stepY == 0) return false;

        int px = waterCell.x;
        int py = waterCell.y;
        int waterCount = 0;
        while (px >= 0 && px < w && py >= 0 && py < h && waterCount <= MaxRiverWidthForBridge)
        {
            int height = heightMap.GetHeight(px, py);
            if (height > 0) return waterCount <= MaxRiverWidthForBridge;
            waterCount++;
            px += stepX;
            py += stepY;
        }
        return false;
    }

    private static List<Vector2Int> GetValidBorderCells(int border, int w, int h, HeightMap heightMap)
    {
        var candidates = new List<Vector2Int>();
        switch (border)
        {
            case 0:
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, 0, w, h, heightMap)) candidates.Add(new Vector2Int(x, 0));
                break;
            case 1:
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, h - 1, w, h, heightMap)) candidates.Add(new Vector2Int(x, h - 1));
                break;
            case 2:
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(0, y, w, h, heightMap)) candidates.Add(new Vector2Int(0, y));
                break;
            case 3:
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(w - 1, y, w, h, heightMap)) candidates.Add(new Vector2Int(w - 1, y));
                break;
        }
        return candidates;
    }

    private static Vector2Int? GetValidBorderCell(int border, int w, int h, HeightMap heightMap)
    {
        var candidates = GetValidBorderCells(border, w, h, heightMap);
        if (candidates.Count == 0) return null;
        int idx = Random.Range(0, candidates.Count);
        return candidates[idx];
    }

    /// <summary>
    /// A* pathfinding for interstate. Prefers flat terrain, allows bridge segments over water,
    /// rejects height diff &gt; 1. Falls back to BiasedWalkPath if A* finds no route.
    /// </summary>
    private List<Vector2Int> FindInterstatePathAStar(Vector2Int start, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        var path = RunInterstateAStar(start, end, w, h, heightMap);
        if (path != null && path.Count >= 2 && path[path.Count - 1] == end)
            return path;
        return BiasedWalkPath(start, end, w, h, heightMap);
    }

    private List<Vector2Int> RunInterstateAStar(Vector2Int start, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        int maxNodes = Mathf.Max(200, (Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y)) * 4);
        var open = new MinHeap();
        var closed = new HashSet<Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        gScore[start] = 0;
        open.Enqueue(start, Heuristic(start, end));
        int explored = 0;

        while (open.Count > 0 && explored < maxNodes)
        {
            explored++;
            Vector2Int current = open.Dequeue();
            if (closed.Contains(current)) continue;
            closed.Add(current);

            if (current == end)
            {
                var result = new List<Vector2Int>();
                while (cameFrom.ContainsKey(current))
                {
                    result.Add(current);
                    current = cameFrom[current];
                }
                result.Add(start);
                result.Reverse();
                return result;
            }

            int currentHeight = heightMap.GetHeight(current.x, current.y);
            var neighbors = GetInterstateNeighbors(current, cameFrom, start, end, w, h, heightMap);

            foreach (Vector2Int neighbor in neighbors)
            {
                if (closed.Contains(neighbor)) continue;
                if (neighbor.x < 0 || neighbor.x >= w || neighbor.y < 0 || neighbor.y >= h) continue;

                int cellHeight = heightMap.GetHeight(neighbor.x, neighbor.y);
                bool allowed = cellHeight > 0
                    ? IsCellAllowedForInterstate(neighbor.x, neighbor.y, w, h, heightMap)
                    : IsValidBridgeSegmentFrom(current, neighbor, end, w, h, heightMap);
                if (!allowed) continue;

                if (currentHeight > 0 && cellHeight > 0 && Mathf.Abs(cellHeight - currentHeight) > 1)
                    continue;

                TerrainSlopeType terrain = cellHeight == 0 ? TerrainSlopeType.Flat : terrainManager.GetTerrainSlopeTypeAt(neighbor.x, neighbor.y);
                int heightDiff = Mathf.Abs(cellHeight - currentHeight);
                int stepCost = RoadPathCostConstants.GetStepCost(terrain, heightDiff);
                int tentative = (gScore.ContainsKey(current) ? gScore[current] : int.MaxValue) + stepCost;
                if (!gScore.ContainsKey(neighbor) || tentative < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentative;
                    open.Enqueue(neighbor, tentative + Heuristic(neighbor, end));
                }
            }
        }
        return null;
    }

    private static int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector2Int> GetInterstateNeighbors(Vector2Int current, Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int start, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        var list = new List<Vector2Int>();
        int currentHeight = heightMap.GetHeight(current.x, current.y);

        if (currentHeight == 0 && cameFrom.ContainsKey(current))
        {
            Vector2Int prev = cameFrom[current];
            Vector2Int dir = new Vector2Int(current.x - prev.x, current.y - prev.y);
            list.Add(new Vector2Int(current.x + dir.x, current.y + dir.y));
            return list;
        }

        if (current == start)
        {
            Vector2Int? first = GetFirstStepFromBorder(start, w, h);
            if (first.HasValue) list.Add(first.Value);
            return list;
        }

        list.Add(new Vector2Int(current.x + 1, current.y));
        list.Add(new Vector2Int(current.x - 1, current.y));
        list.Add(new Vector2Int(current.x, current.y + 1));
        list.Add(new Vector2Int(current.x, current.y - 1));
        return list;
    }

    private static bool IsValidBridgeSegmentFrom(Vector2Int from, Vector2Int waterCell, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        if (heightMap.GetHeight(waterCell.x, waterCell.y) != 0) return false;
        int dx = end.x - waterCell.x;
        int dy = end.y - waterCell.y;
        int stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
        int stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;
        if (stepX == 0 && stepY == 0) return false;

        int px = waterCell.x, py = waterCell.y;
        int waterCount = 0;
        while (px >= 0 && px < w && py >= 0 && py < h && waterCount <= MaxRiverWidthForBridge)
        {
            if (heightMap.GetHeight(px, py) > 0) return waterCount <= MaxRiverWidthForBridge;
            waterCount++;
            px += stepX;
            py += stepY;
        }
        return false;
    }

    private List<Vector2Int> BiasedWalkPath(Vector2Int start, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        List<Vector2Int> path = new List<Vector2Int> { start };
        HashSet<Vector2Int> visited = new HashSet<Vector2Int> { start };
        Vector2Int current = start;
        int maxSteps = w * h;
        int steps = 0;

        while (current != end && steps < maxSteps)
        {
            List<Vector2Int> candidates = new List<Vector2Int>();

            if (path.Count >= 2 && heightMap.GetHeight(current.x, current.y) == 0)
            {
                Vector2Int bridgeDir = new Vector2Int(
                    path[path.Count - 1].x - path[path.Count - 2].x,
                    path[path.Count - 1].y - path[path.Count - 2].y);
                candidates.Add(new Vector2Int(current.x + bridgeDir.x, current.y + bridgeDir.y));
            }
            else if (path.Count == 1)
            {
                Vector2Int? firstStep = GetFirstStepFromBorder(start, w, h);
                if (firstStep.HasValue) candidates.Add(firstStep.Value);
            }
            else
            {
                int dx = end.x - current.x;
                int dy = end.y - current.y;
                int ax = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
                int ay = dy != 0 ? (dy > 0 ? 1 : -1) : 0;

                if (ax != 0) candidates.Add(new Vector2Int(current.x + ax, current.y));
                if (ay != 0) candidates.Add(new Vector2Int(current.x, current.y + ay));
                if (Random.value < 0.25f)
                {
                    if (ax != 0) { candidates.Add(new Vector2Int(current.x, current.y + 1)); candidates.Add(new Vector2Int(current.x, current.y - 1)); }
                    if (ay != 0) { candidates.Add(new Vector2Int(current.x + 1, current.y)); candidates.Add(new Vector2Int(current.x - 1, current.y)); }
                }
            }

            Vector2Int? next = null;
            int bestScore = int.MaxValue;
            int currentHeight = heightMap.GetHeight(current.x, current.y);
            foreach (Vector2Int c in candidates)
            {
                if (c.x < 0 || c.x >= w || c.y < 0 || c.y >= h) continue;
                if (visited.Contains(c)) continue;

                int cellHeight = heightMap.GetHeight(c.x, c.y);
                bool allowed = false;
                if (cellHeight > 0)
                    allowed = IsCellAllowedForInterstate(c.x, c.y, w, h, heightMap);
                else if (cellHeight == 0)
                    allowed = IsValidBridgeSegment(path, c, end, w, h, heightMap);

                if (!allowed) continue;

                if (currentHeight > 0 && cellHeight > 0 && Mathf.Abs(cellHeight - currentHeight) > 1)
                    continue;

                TerrainSlopeType terrain = cellHeight == 0 ? TerrainSlopeType.Flat : terrainManager.GetTerrainSlopeTypeAt(c.x, c.y);
                int heightDiff = Mathf.Abs(cellHeight - currentHeight);
                int stepCost = RoadPathCostConstants.GetStepCost(terrain, heightDiff);
                int dist = Mathf.Abs(c.x - end.x) + Mathf.Abs(c.y - end.y);
                int score = dist + stepCost;
                if (score < bestScore || (score == bestScore && Random.value > 0.5f))
                {
                    bestScore = score;
                    next = c;
                }
            }

            if (!next.HasValue)
            {
                if (path.Count <= 1) break;
                path.RemoveAt(path.Count - 1);
                current = path[path.Count - 1];
                steps++;
                continue;
            }

            path.Add(next.Value);
            visited.Add(next.Value);
            current = next.Value;
            steps++;
        }

        return path;
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Rebuild interstate positions list from grid (e.g. after load). Call after RestoreGrid.
    /// </summary>
    public void RebuildFromGrid()
    {
        interstatePositions.Clear();
        EntryPoint = null;
        ExitPoint = null;
        EntryBorder = -1;
        ExitBorder = -1;
        if (gridManager == null) return;
        int w = gridManager.width;
        int h = gridManager.height;
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Cell cell = gridManager.GetCell(x, y);
                if (cell != null && cell.isInterstate)
                    interstatePositions.Add(new Vector2Int(x, y));
            }
        }
        Vector2Int? firstBorder = null;
        int firstBorderIdx = -1;
        Vector2Int? secondBorder = null;
        int secondBorderIdx = -1;
        foreach (Vector2Int pos in interstatePositions)
        {
            int borderIdx = -1;
            if (pos.y == 0) borderIdx = 0;
            else if (pos.y == h - 1) borderIdx = 1;
            else if (pos.x == 0) borderIdx = 2;
            else if (pos.x == w - 1) borderIdx = 3;
            if (borderIdx < 0) continue;
            if (!firstBorder.HasValue)
            {
                firstBorder = pos;
                firstBorderIdx = borderIdx;
            }
            else if (!secondBorder.HasValue)
            {
                secondBorder = pos;
                secondBorderIdx = borderIdx;
                break;
            }
        }
        if (firstBorder.HasValue)
        {
            EntryPoint = firstBorder;
            EntryBorder = firstBorderIdx;
        }
        if (secondBorder.HasValue)
        {
            ExitPoint = secondBorder;
            ExitBorder = secondBorderIdx;
        }
    }

    /// <summary>
    /// Check if the given grid position is an interstate cell.
    /// </summary>
    public bool IsInterstateAt(int x, int y)
    {
        if (gridManager == null) return false;
        Cell cell = gridManager.GetCell(x, y);
        return cell != null && cell.isInterstate;
    }

    /// <summary>
    /// Check if the given grid position is an interstate cell.
    /// </summary>
    public bool IsInterstateAt(Vector2 gridPos)
    {
        return IsInterstateAt(Mathf.RoundToInt(gridPos.x), Mathf.RoundToInt(gridPos.y));
    }

    /// <summary>
    /// BFS from all interstate cells through road cells. Sets isConnectedToInterstate if any player road is reached.
    /// </summary>
    public void CheckInterstateConnectivity()
    {
        isConnectedToInterstate = false;
        if (gridManager == null || interstatePositions.Count == 0) return;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        foreach (Vector2Int p in interstatePositions)
        {
            queue.Enqueue(p);
            visited.Add(p);
        }

        int w = gridManager.width;
        int h = gridManager.height;
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            if (!IsInterstateAt(curr.x, curr.y))
            {
                isConnectedToInterstate = true;
                return;
            }

            for (int i = 0; i < 4; i++)
            {
                int nx = curr.x + dx[i];
                int ny = curr.y + dy[i];
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                var n = new Vector2Int(nx, ny);
                if (visited.Contains(n)) continue;
                if (!IsRoadAt(nx, ny)) continue;
                visited.Add(n);
                queue.Enqueue(n);
            }
        }
    }

    private bool IsRoadAt(int gridX, int gridY)
    {
        if (gridManager == null) return false;
        Cell cell = gridManager.GetCell(gridX, gridY);
        return cell != null && cell.zoneType == Zone.ZoneType.Road;
    }

    /// <summary>
    /// Whether the player can start placing a street from this position (must touch interstate or connected road chain).
    /// </summary>
    public bool CanPlaceStreetFrom(Vector2 gridPosition)
    {
        int x = Mathf.RoundToInt(gridPosition.x);
        int y = Mathf.RoundToInt(gridPosition.y);
        return CanPlaceStreetFrom(x, y);
    }

    /// <summary>
    /// Whether the player can start placing a street from (x,y). Valid if any cardinal neighbor is interstate or road connected to interstate.
    /// </summary>
    public bool CanPlaceStreetFrom(int x, int y)
    {
        if (gridManager == null) return false;
        int w = gridManager.width;
        int h = gridManager.height;
        if (x < 0 || x >= w || y < 0 || y >= h) return false;

        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
            if (IsInterstateAt(nx, ny)) return true;
            if (IsRoadAt(nx, ny) && IsStreetConnectedToInterstate(nx, ny)) return true;
        }
        return false;
    }

    /// <summary>
    /// BFS from (startX, startY) through road cells; returns true if an interstate cell is reached.
    /// </summary>
    private bool IsStreetConnectedToInterstate(int startX, int startY)
    {
        if (gridManager == null) return false;
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        visited.Add(new Vector2Int(startX, startY));

        int w = gridManager.width;
        int h = gridManager.height;
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            if (IsInterstateAt(curr.x, curr.y)) return true;

            for (int i = 0; i < 4; i++)
            {
                int nx = curr.x + dx[i];
                int ny = curr.y + dy[i];
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                var n = new Vector2Int(nx, ny);
                if (visited.Contains(n)) continue;
                if (!IsRoadAt(nx, ny)) continue;
                visited.Add(n);
                queue.Enqueue(n);
            }
        }
        return false;
    }

    /// <summary>
    /// Set connectivity flag (e.g. when loading from save).
    /// </summary>
    public void SetConnectedToInterstate(bool connected)
    {
        isConnectedToInterstate = connected;
    }
    #endregion
}
}
