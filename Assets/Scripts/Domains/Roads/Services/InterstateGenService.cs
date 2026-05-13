using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Terrain;
using Territory.Roads;

namespace Domains.Roads.Services
{
/// <summary>
/// Generates interstate routes via A* and biased-walk pathfinding.
/// Delegates cell conformance to InterstateConformanceService.
/// </summary>
public class InterstateGenService
{
    private const int MaxRouteAttempts = 80;
    private const int PathTriesPerPair = 8;
    public const int InterstateGenSeed = 12345 + 100;

    private readonly IGridManager _grid;
    private readonly ITerrainManager _terrain;
    private readonly IRoadManager _roads;
    private readonly InterstateConformanceService _conformance;

    public InterstateGenService(IGridManager grid, ITerrainManager terrain, IRoadManager roads, InterstateConformanceService conformance)
    {
        _grid = grid;
        _terrain = terrain;
        _roads = roads;
        _conformance = conformance;
    }

    /// <summary>Expose terrain height map for orchestrator use.</summary>
    public HeightMap GetHeightMapFromTerrain() => _terrain?.GetHeightMap();

    /// <summary>Place a pre-computed path via roads service. Returns false if roads null or placement fails.</summary>
    public bool PlaceCurrentPath(List<Vector2Int> path)
    {
        if (_roads == null || path == null || path.Count == 0) return false;
        return _roads.PlaceInterstateFromPath(path);
    }

    // ------------------------------------------------------------------
    // Route generation (stochastic + deterministic)
    // ------------------------------------------------------------------

    public List<Vector2Int> GenerateInterstateRoute(int attemptOffset, ref Vector2Int? entryPoint, ref Vector2Int? exitPoint, ref int entryBorder, ref int exitBorder)
    {
        var positions = new List<Vector2Int>();
        entryPoint = null;
        exitPoint = null;
        entryBorder = -1;
        exitBorder = -1;

        if (_grid == null || _terrain == null) return positions;
        HeightMap heightMap = _terrain.GetHeightMap();
        if (heightMap == null) return positions;

        int runSeed = System.Environment.TickCount ^ (int)(Time.realtimeSinceStartup * 1000);
        Random.InitState(InterstateGenSeed + attemptOffset + runSeed);

        int w = _grid.width;
        int h = _grid.height;
        List<int> bordersWithLand = _conformance.GetBordersWithLand(w, h, heightMap);
        if (bordersWithLand.Count < 2) return positions;

        var borderPairs = new List<(int a, int b)>();
        foreach (int b in bordersWithLand)
        {
            int opp = TerritoryData.OppositeBorder(b);
            if (bordersWithLand.Contains(opp) && b < opp)
                borderPairs.Add((b, opp));
        }
        if (borderPairs.Count == 0) return positions;

        int pairIdx = Random.Range(0, borderPairs.Count);
        int borderA = borderPairs[pairIdx].a;
        int borderB = borderPairs[pairIdx].b;

        List<Vector2Int> bestPath = null;
        int bestCost = int.MaxValue;
        int bestBorderA = borderA;
        int bestBorderB = borderB;

        for (int attempt = 0; attempt < MaxRouteAttempts; attempt++)
        {
            Random.InitState(InterstateGenSeed + attemptOffset + runSeed + attempt);

            Vector2Int? entry = _conformance.GetValidBorderCell(borderA, w, h, heightMap);
            Vector2Int? exit = _conformance.GetValidBorderCell(borderB, w, h, heightMap);
            if (!entry.HasValue || !exit.HasValue) continue;

            for (int pathTry = 0; pathTry < PathTriesPerPair; pathTry++)
            {
                Random.InitState(InterstateGenSeed + attemptOffset + runSeed + attempt * 100 + pathTry);
                List<Vector2Int> path = FindInterstatePathAStar(entry.Value, exit.Value, w, h, heightMap);
                if (path == null || path.Count < 2 || path[path.Count - 1] != exit.Value) continue;
                if (_roads != null && !_roads.ValidateBridgePath(path, heightMap)) continue;
                if (_roads != null && !_roads.ValidateInterstatePathForPlacement(path)) continue;

                int cost = ComputePathCost(path, heightMap);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestPath = path;
                    bestBorderA = borderA;
                    bestBorderB = borderB;
                }
                break;
            }
            if (bestPath != null) break;
        }

        if (bestPath == null) return positions;

        positions = bestPath;
        entryPoint = positions[0];
        exitPoint = positions[positions.Count - 1];
        entryBorder = bestBorderA;
        exitBorder = bestBorderB;
        return positions;
    }

    // ------------------------------------------------------------------
    // Path cost
    // ------------------------------------------------------------------

    public int ComputePathCost(List<Vector2Int> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 2 || heightMap == null || _terrain == null) return int.MaxValue;
        int cost = 0;
        for (int i = 1; i < path.Count; i++)
        {
            var prev = path[i - 1];
            var curr = path[i];
            int hPrev = heightMap.GetHeight(prev.x, prev.y);
            int hCurr = heightMap.GetHeight(curr.x, curr.y);
            TerrainSlopeType terrain = hCurr == 0 ? TerrainSlopeType.Flat : _terrain.GetTerrainSlopeTypeAt(curr.x, curr.y);
            int heightDiff = Mathf.Abs(hCurr - hPrev);
            cost += _terrain.IsWaterSlopeCell(curr.x, curr.y)
                ? RoadPathCostConstants.WaterSlopeCost
                : RoadPathCostConstants.GetStepCostForInterstate(terrain, heightDiff);
        }
        return cost;
    }

    // ------------------------------------------------------------------
    // A* pathfinding
    // ------------------------------------------------------------------

    public List<Vector2Int> FindInterstatePathAStar(Vector2Int start, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        var path = PickLowerCostInterstateAStarPath(start, end, w, h, heightMap);
        if (path == null || path.Count < 2 || path[path.Count - 1] != end)
            path = BiasedWalkPath(start, end, w, h, heightMap);
        if (path == null || path.Count < 2) return path;

        path = SmoothInterstatePath(path, w, h, heightMap, end);
        if (!PathCrossesHill(path, heightMap)) return path;

        int primaryCost = ComputePathCost(path, heightMap);
        Vector2Int dir = new Vector2Int(end.x - start.x, end.y - start.y);
        Vector2Int perp1 = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) ? new Vector2Int(0, 1) : new Vector2Int(1, 0);
        Vector2Int perp2 = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) ? new Vector2Int(0, -1) : new Vector2Int(-1, 0);

        const int maxParallelOffsetAttempts = 2;
        for (int attempt = 0; attempt < maxParallelOffsetAttempts; attempt++)
        {
            Vector2Int offset = attempt == 0 ? perp1 : perp2;
            Vector2Int offStart = new Vector2Int(start.x + offset.x, start.y + offset.y);
            Vector2Int offEnd = new Vector2Int(end.x + offset.x, end.y + offset.y);
            if (offStart.x < 0 || offStart.x >= w || offStart.y < 0 || offStart.y >= h) continue;
            if (offEnd.x < 0 || offEnd.x >= w || offEnd.y < 0 || offEnd.y >= h) continue;
            if (!_conformance.IsCellAllowedForInterstate(offStart.x, offStart.y, w, h, heightMap, checkSlopes: true)) continue;
            if (!_conformance.IsCellAllowedForInterstate(offEnd.x, offEnd.y, w, h, heightMap, checkSlopes: true)) continue;

            var altPath = PickLowerCostInterstateAStarPath(offStart, offEnd, w, h, heightMap);
            if (altPath == null || altPath.Count < 2 || altPath[altPath.Count - 1] != offEnd) continue;
            if (_roads != null && !_roads.ValidateBridgePath(altPath, heightMap)) continue;

            altPath = SmoothInterstatePath(altPath, w, h, heightMap, offEnd);
            int altCost = ComputePathCost(altPath, heightMap);
            if (altCost < primaryCost)
            {
                path = altPath;
                primaryCost = altCost;
            }
        }
        path = SmoothInterstatePath(path, w, h, heightMap, end);
        return path;
    }

    private List<Vector2Int> PickLowerCostInterstateAStarPath(Vector2Int start, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        var lowTerrain = RunInterstateAStar(start, end, w, h, heightMap, avoidHighTerrain: true);
        var anyTerrain = RunInterstateAStar(start, end, w, h, heightMap, avoidHighTerrain: false);
        bool lowOk = lowTerrain != null && lowTerrain.Count >= 2 && lowTerrain[lowTerrain.Count - 1] == end;
        bool anyOk = anyTerrain != null && anyTerrain.Count >= 2 && anyTerrain[anyTerrain.Count - 1] == end;
        if (!lowOk && !anyOk) return null;
        if (!lowOk) return anyTerrain;
        if (!anyOk) return lowTerrain;

        int cLow = ComputePathCost(lowTerrain, heightMap);
        int cAny = ComputePathCost(anyTerrain, heightMap);
        if (cAny < cLow) return anyTerrain;
        if (cLow < cAny) return lowTerrain;
        return lowTerrain.Count <= anyTerrain.Count ? lowTerrain : anyTerrain;
    }

    private List<Vector2Int> RunInterstateAStar(Vector2Int start, Vector2Int end, int w, int h, HeightMap heightMap, bool avoidHighTerrain = false)
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
                if (avoidHighTerrain && cellHeight > 1 && neighbor != end) continue;
                bool allowed = cellHeight > 0
                    ? _conformance.IsCellAllowedForInterstate(neighbor.x, neighbor.y, w, h, heightMap, checkSlopes: true)
                    : _conformance.IsValidBridgeSegmentFrom(current, neighbor, end, w, h, heightMap);
                if (!allowed) continue;

                if (currentHeight > 0 && cellHeight > 0 && Mathf.Abs(cellHeight - currentHeight) > 1)
                    continue;

                TerrainSlopeType terrain = cellHeight == 0 ? TerrainSlopeType.Flat : _terrain.GetTerrainSlopeTypeAt(neighbor.x, neighbor.y);
                int heightDiff = Mathf.Abs(cellHeight - currentHeight);
                int stepCost = _terrain.IsWaterSlopeCell(neighbor.x, neighbor.y)
                    ? RoadPathCostConstants.WaterSlopeCost
                    : RoadPathCostConstants.GetStepCostForInterstate(terrain, heightDiff);

                int manCurr = Heuristic(current, end);
                int manNei = Heuristic(neighbor, end);
                if (manNei > manCurr)
                    stepCost += RoadPathCostConstants.InterstateAwayFromGoalPenalty;

                if (cameFrom.ContainsKey(current))
                {
                    Vector2Int dirFromPrev = new Vector2Int(current.x - cameFrom[current].x, current.y - cameFrom[current].y);
                    Vector2Int dirToNeighbor = new Vector2Int(neighbor.x - current.x, neighbor.y - current.y);
                    if (dirFromPrev == dirToNeighbor)
                        stepCost = Mathf.Max(1, stepCost - RoadPathCostConstants.InterstateStraightnessBonus);
                    else
                    {
                        stepCost += RoadPathCostConstants.InterstateTurnPenalty;
                        if (cameFrom.ContainsKey(cameFrom[current]))
                        {
                            Vector2Int prevPrev = cameFrom[cameFrom[current]];
                            Vector2Int dirBeforeTurn = new Vector2Int(cameFrom[current].x - prevPrev.x, cameFrom[current].y - prevPrev.y);
                            if (dirFromPrev != dirBeforeTurn && dirToNeighbor == dirBeforeTurn)
                                stepCost += RoadPathCostConstants.InterstateZigzagPenalty;
                        }
                    }
                }
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
            Vector2Int? first = InterstateConformanceService.GetFirstStepFromBorder(start, w, h);
            if (first.HasValue) list.Add(first.Value);
            return list;
        }

        list.Add(new Vector2Int(current.x + 1, current.y));
        list.Add(new Vector2Int(current.x - 1, current.y));
        list.Add(new Vector2Int(current.x, current.y + 1));
        list.Add(new Vector2Int(current.x, current.y - 1));
        return list;
    }

    private List<Vector2Int> SmoothInterstatePath(List<Vector2Int> path, int w, int h, HeightMap heightMap, Vector2Int pathEnd)
    {
        if (path == null || path.Count < 3) return path;
        var result = new List<Vector2Int> { path[0] };
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2Int prev = result[result.Count - 1];
            Vector2Int curr = path[i];
            Vector2Int next = path[i + 1];
            int dx = next.x - prev.x;
            int dy = next.y - prev.y;
            bool cardinalStep = (Mathf.Abs(dx) == 1 && dy == 0) || (dx == 0 && Mathf.Abs(dy) == 1);
            if (cardinalStep && _conformance.IsDirectStepValid(prev, next, w, h, heightMap, pathEnd))
            {
                result.Add(next);
                i++;
                continue;
            }
            result.Add(curr);
        }
        if (result[result.Count - 1] != path[path.Count - 1])
            result.Add(path[path.Count - 1]);
        return result;
    }

    private static bool PathCrossesHill(List<Vector2Int> path, HeightMap heightMap)
    {
        if (path == null || heightMap == null) return false;
        foreach (var p in path)
        {
            if (heightMap.IsValidPosition(p.x, p.y) && heightMap.GetHeight(p.x, p.y) > 1)
                return true;
        }
        return false;
    }

    // ------------------------------------------------------------------
    // Biased-walk fallback
    // ------------------------------------------------------------------

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
                Vector2Int? firstStep = InterstateConformanceService.GetFirstStepFromBorder(start, w, h);
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
                if (Random.value < 0.08f)
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
                    allowed = _conformance.IsCellAllowedForInterstate(c.x, c.y, w, h, heightMap, checkSlopes: true);
                else if (cellHeight == 0)
                    allowed = _conformance.IsValidBridgeSegment(path, c, end, w, h, heightMap);

                if (!allowed) continue;

                if (currentHeight > 0 && cellHeight > 0 && Mathf.Abs(cellHeight - currentHeight) > 1)
                    continue;

                TerrainSlopeType terrain = cellHeight == 0 ? TerrainSlopeType.Flat : _terrain.GetTerrainSlopeTypeAt(c.x, c.y);
                int heightDiff = Mathf.Abs(cellHeight - currentHeight);
                int stepCost = _terrain.IsWaterSlopeCell(c.x, c.y)
                    ? RoadPathCostConstants.WaterSlopeCost
                    : RoadPathCostConstants.GetStepCostForInterstate(terrain, heightDiff);
                int turnCost = 0;
                if (path.Count >= 2)
                {
                    Vector2Int dirFromPrev = new Vector2Int(current.x - path[path.Count - 2].x, current.y - path[path.Count - 2].y);
                    Vector2Int dirToC = new Vector2Int(c.x - current.x, c.y - current.y);
                    if (dirFromPrev != dirToC)
                    {
                        turnCost = RoadPathCostConstants.InterstateTurnPenalty;
                        if (path.Count >= 3)
                        {
                            Vector2Int dirBeforeTurn = new Vector2Int(path[path.Count - 2].x - path[path.Count - 3].x, path[path.Count - 2].y - path[path.Count - 3].y);
                            if (dirFromPrev != dirBeforeTurn && dirToC == dirBeforeTurn)
                                turnCost += RoadPathCostConstants.InterstateZigzagPenalty;
                        }
                    }
                }
                int dist = Mathf.Abs(c.x - end.x) + Mathf.Abs(c.y - end.y);
                int score = dist + stepCost + turnCost;
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
}
}
