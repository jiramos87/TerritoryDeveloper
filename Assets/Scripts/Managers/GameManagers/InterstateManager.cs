using System.Collections.Generic;
using UnityEngine;
using Territory.Core;
using Territory.Terrain;
using Territory.Zones;
using Territory.Geography;
using Territory.Utilities;

namespace Territory.Roads
{
/// <summary>
/// Manage Interstate Highway: gen at game start, connectivity checks, validation for player road placement (streets must grow from interstate).
/// Border entry/exit candidates ranked by flat land toward interior → routes avoid harsh corner climbs when alternatives exist.
/// Implements Domains.Roads.IInterstate facade (atomization Stage 16 / TECH-23789).
/// </summary>
public class InterstateManager : MonoBehaviour, Domains.Roads.IInterstate
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

    /// <summary>Border + position of interstate entry/exit points (set during gen or RebuildFromGrid).</summary>
    public Vector2Int? EntryPoint { get; private set; }
    public Vector2Int? ExitPoint { get; private set; }
    public int EntryBorder { get; private set; } = -1;
    public int ExitBorder { get; private set; } = -1;

    /// <summary>
    /// Whether player road network connected to interstate (updated monthly).
    /// </summary>
    public bool IsConnectedToInterstate => isConnectedToInterstate;

    /// <summary>
    /// Read-only list of grid positions part of interstate.
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
    /// Generate interstate route, place tiles via centralized terraform + resolve pipeline.
    /// Call after water map, before forest map.
    /// </summary>
    /// <param name="attemptOffset">Added to seed so each retry tries different paths.</param>
    /// <returns>True if path found + tiles placed successfully.</returns>
    public bool GenerateAndPlaceInterstate(int attemptOffset = 0)
    {
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
        GenerateInterstateRoute(attemptOffset);
        if (interstatePositions.Count == 0 || roadManager == null)
            return false;

        bool placed = roadManager.PlaceInterstateFromPath(interstatePositions);
        if (!placed)
            interstatePositions.Clear();
        return placed;
    }

    /// <summary>
    /// Try all border cells deterministically. Pick best valid path by cost (shortest/most economical).
    /// Entry + exit derived from best path, not chosen first.
    /// Call when random attempts fail. Guarantees interstate if any valid route exists.
    /// </summary>
    public bool TryGenerateInterstateDeterministic()
    {
        if (roadManager == null) roadManager = FindObjectOfType<RoadManager>();
        if (roadManager == null) return false;

        var heightMap = terrainManager != null ? terrainManager.GetHeightMap() : null;
        if (heightMap == null || gridManager == null) return false;

        int w = gridManager.width;
        int h = gridManager.height;
        List<int> bordersWithLand = GetBordersWithLand(w, h, heightMap, terrainManager);
        if (bordersWithLand.Count < 2) return false;

        int runSeed = System.Environment.TickCount ^ (int)(Time.realtimeSinceStartup * 1000);
        Random.InitState(runSeed);
        int randomEntryBorder = bordersWithLand[Random.Range(0, bordersWithLand.Count)];
        int exitBorder = TerritoryData.OppositeBorder(randomEntryBorder);
        if (!bordersWithLand.Contains(exitBorder))
        {
            foreach (int b in bordersWithLand)
            {
                if (b != randomEntryBorder) { exitBorder = b; break; }
            }
        }

        var entries = GetValidBorderCellsWithPreference(randomEntryBorder, w, h, heightMap);
        if (entries.Count == 0) return false;

        var exits = GetValidBorderCellsWithPreference(exitBorder, w, h, heightMap);
        if (exits.Count == 0) return false;

        var candidates = new List<(int borderA, int borderB, Vector2Int entry, Vector2Int exit)>();
        for (int ei = 0; ei < entries.Count; ei++)
        {
            for (int xi = 0; xi < exits.Count; xi++)
            {
                Vector2Int entry = entries[ei];
                Vector2Int exit = exits[xi];
                if (entry != exit)
                    candidates.Add((randomEntryBorder, exitBorder, entry, exit));
            }
        }

        candidates.Sort((a, b) =>
        {
            int manhattanA = Mathf.Abs(a.entry.x - a.exit.x) + Mathf.Abs(a.entry.y - a.exit.y);
            int manhattanB = Mathf.Abs(b.entry.x - b.exit.x) + Mathf.Abs(b.entry.y - b.exit.y);
            int cmp = manhattanA.CompareTo(manhattanB);
            if (cmp != 0) return cmp;
            int scoreA = ComputeInterstateBorderEndpointScore(a.entry, w, h, heightMap, terrainManager)
                + ComputeInterstateBorderEndpointScore(a.exit, w, h, heightMap, terrainManager);
            int scoreB = ComputeInterstateBorderEndpointScore(b.entry, w, h, heightMap, terrainManager)
                + ComputeInterstateBorderEndpointScore(b.exit, w, h, heightMap, terrainManager);
            cmp = scoreB.CompareTo(scoreA);
            if (cmp != 0) return cmp;
            if (a.entry.x != b.entry.x) return a.entry.x.CompareTo(b.entry.x);
            if (a.entry.y != b.entry.y) return a.entry.y.CompareTo(b.entry.y);
            if (a.exit.x != b.exit.x) return a.exit.x.CompareTo(b.exit.x);
            return a.exit.y.CompareTo(b.exit.y);
        });

        List<Vector2Int> bestPath = null;
        int bestCost = int.MaxValue;
        int bestBorderA = -1, bestBorderB = -1;
        const int maxDeterministicTries = 800;

        for (int i = 0; i < Mathf.Min(candidates.Count, maxDeterministicTries); i++)
        {
            var (borderA, borderB, entry, exit) = candidates[i];

            Random.InitState(InterstateGenSeed + 9999 + entry.x + entry.y * w + exit.x + exit.y * w);
            List<Vector2Int> path = FindInterstatePathAStar(entry, exit, w, h, heightMap);
            if (path == null || path.Count < 2 || path[path.Count - 1] != exit) continue;
            if (roadManager != null && !roadManager.ValidateBridgePath(path, heightMap)) continue;
            if (roadManager != null && !roadManager.ValidateInterstatePathForPlacement(path)) continue;

            int cost = ComputePathCost(path, heightMap);
            if (cost < bestCost)
            {
                bestCost = cost;
                bestPath = path;
                bestBorderA = borderA;
                bestBorderB = borderB;
            }
        }

        if (bestPath == null)
            return false;

        interstatePositions = bestPath;
        EntryPoint = interstatePositions[0];
        ExitPoint = interstatePositions[interstatePositions.Count - 1];
        EntryBorder = bestBorderA;
        ExitBorder = bestBorderB;

        bool placed = roadManager.PlaceInterstateFromPath(interstatePositions);
        if (!placed)
            interstatePositions.Clear();
        return placed;
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
    /// <summary>Base seed for interstate pathfinding. Combined with run-varying seed → each game yields different routes.</summary>
    const int InterstateGenSeed = 12345 + 100;

    /// <summary>
    /// Return border indices (0=South, 1=North, 2=West, 3=East) with ≥1 land cell valid for interstate.
    /// </summary>
    private static List<int> GetBordersWithLand(int w, int h, HeightMap heightMap, TerrainManager terrainManager)
    {
        var list = new List<int>();
        for (int b = 0; b < 4; b++)
        {
            if (HasAnyValidCellOnBorder(b, w, h, heightMap, terrainManager))
                list.Add(b);
        }
        return list;
    }

    private static bool HasAnyValidCellOnBorder(int border, int w, int h, HeightMap heightMap, TerrainManager terrainManager)
    {
        switch (border)
        {
            case 0:
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, 0, w, h, heightMap, terrainManager)) return true;
                break;
            case 1:
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, h - 1, w, h, heightMap, terrainManager)) return true;
                break;
            case 2:
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(0, y, w, h, heightMap, terrainManager)) return true;
                break;
            case 3:
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(w - 1, y, w, h, heightMap, terrainManager)) return true;
                break;
        }
        return false;
    }

    /// <summary>
    /// Generate interstate route + store positions. Retries until valid path exists (two land connections, path reaches exit).
    /// Does not place tiles — call GenerateAndPlaceInterstate after.
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
            return interstatePositions;
        if (terrainManager == null)
            return interstatePositions;
        HeightMap heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
            return interstatePositions;

        int runSeed = System.Environment.TickCount ^ (int)(Time.realtimeSinceStartup * 1000);
        Random.InitState(InterstateGenSeed + attemptOffset + runSeed);

        int w = gridManager.width;
        int h = gridManager.height;
        List<int> bordersWithLand = GetBordersWithLand(w, h, heightMap, terrainManager);
        if (bordersWithLand.Count < 2)
            return interstatePositions;

        var borderPairs = new List<(int a, int b)>();
        foreach (int b in bordersWithLand)
        {
            int opp = TerritoryData.OppositeBorder(b);
            if (bordersWithLand.Contains(opp) && b < opp)
                borderPairs.Add((b, opp));
        }
        if (borderPairs.Count == 0)
            return interstatePositions;

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

            Vector2Int? entry = GetValidBorderCell(borderA, w, h, heightMap);
            Vector2Int? exit = GetValidBorderCell(borderB, w, h, heightMap);
            if (!entry.HasValue || !exit.HasValue) continue;

            for (int pathTry = 0; pathTry < PathTriesPerPair; pathTry++)
            {
                Random.InitState(InterstateGenSeed + attemptOffset + runSeed + attempt * 100 + pathTry);
                List<Vector2Int> path = FindInterstatePathAStar(entry.Value, exit.Value, w, h, heightMap);
                if (path == null || path.Count < 2 || path[path.Count - 1] != exit.Value) continue;
                if (roadManager != null && !roadManager.ValidateBridgePath(path, heightMap)) continue;
                if (roadManager != null && !roadManager.ValidateInterstatePathForPlacement(path)) continue;

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

        if (bestPath == null)
            return interstatePositions;

        interstatePositions = bestPath;
        EntryPoint = interstatePositions[0];
        ExitPoint = interstatePositions[interstatePositions.Count - 1];
        EntryBorder = bestBorderA;
        ExitBorder = bestBorderB;
        return interstatePositions;
    }

    /// <summary>
    /// True if cell valid for interstate land: positive height + flat or cardinal ramp only (same stroke rule as streets; diagonal/corner-up excluded).
    /// </summary>
    private static bool IsCellAllowedForInterstate(int x, int y, int w, int h, HeightMap heightMap, TerrainManager terrainManager)
    {
        if (heightMap.GetHeight(x, y) <= 0)
            return false;
        if (terrainManager != null && terrainManager.IsWaterSlopeCell(x, y))
            return true;
        if (terrainManager != null)
        {
            TerrainSlopeType st = terrainManager.GetTerrainSlopeTypeAt(x, y);
            if (!RoadStrokeTerrainRules.IsLandSlopeAllowedForRoadStroke(st))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Return only allowed first step from border cell → road enters in cardinal (N-S or E-W) direction.
    /// </summary>
    private static Vector2Int? GetFirstStepFromBorder(Vector2Int start, int w, int h)
    {
        if (start.y == 0) return new Vector2Int(start.x, 1);
        if (start.y == h - 1) return new Vector2Int(start.x, h - 2);
        if (start.x == 0) return new Vector2Int(1, start.y);
        if (start.x == w - 1) return new Vector2Int(w - 2, start.y);
        return null;
    }

    /// <summary>
    /// Higher = better interstate endpoint. Favors low border height, land neighbors at h=1, flat mandatory first step inside map, first-step neighborhood with more h=1 cells (gentle entry before climbing).
    /// </summary>
    private static int ComputeInterstateBorderEndpointScore(Vector2Int c, int w, int h, HeightMap heightMap, TerrainManager terrainManager)
    {
        if (heightMap == null || !heightMap.IsValidPosition(c.x, c.y))
            return int.MinValue;
        if (terrainManager != null && terrainManager.IsWaterSlopeCell(c.x, c.y))
            return int.MinValue;

        int h0 = heightMap.GetHeight(c.x, c.y);
        if (terrainManager != null && terrainManager.IsRegisteredOpenWaterAt(c.x, c.y))
            return int.MinValue;

        int score = 0;
        if (h0 == 1)
            score += 10_000;
        else if (h0 == 2)
            score += 3_000;
        else
            score += 1_000;

        int flatAroundBorder = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = c.x + dx;
                int ny = c.y + dy;
                if (!heightMap.IsValidPosition(nx, ny)) continue;
                int nh = heightMap.GetHeight(nx, ny);
                if (terrainManager != null && terrainManager.IsRegisteredOpenWaterAt(nx, ny)) continue;
                if (nh == 1) flatAroundBorder++;
            }
        }
        score += flatAroundBorder * 500;

        Vector2Int? firstStep = GetFirstStepFromBorder(c, w, h);
        if (!firstStep.HasValue)
            return score;

        Vector2Int fs = firstStep.Value;
        if (!heightMap.IsValidPosition(fs.x, fs.y))
            return score;

        int h1 = heightMap.GetHeight(fs.x, fs.y);
        if (terrainManager == null || !terrainManager.IsRegisteredOpenWaterAt(fs.x, fs.y))
        {
            int stepDiff = Mathf.Abs(h1 - h0);
            score -= stepDiff * 2_000;
            if (h1 == 1)
                score += 4_000;

            int flatAroundFirst = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = fs.x + dx;
                    int ny = fs.y + dy;
                    if (!heightMap.IsValidPosition(nx, ny)) continue;
                    int nh = heightMap.GetHeight(nx, ny);
                    if (terrainManager != null && terrainManager.IsRegisteredOpenWaterAt(nx, ny)) continue;
                    if (nh == 1) flatAroundFirst++;
                }
            }
            score += flatAroundFirst * 200;
        }

        return score;
    }

    private void SortBorderCellsByInterstateEndpointQuality(List<Vector2Int> cells, int w, int h, HeightMap heightMap)
    {
        if (cells == null || cells.Count < 2) return;
        cells.Sort((a, b) =>
        {
            int sa = ComputeInterstateBorderEndpointScore(a, w, h, heightMap, terrainManager);
            int sb = ComputeInterstateBorderEndpointScore(b, w, h, heightMap, terrainManager);
            int cmp = sb.CompareTo(sa);
            if (cmp != 0) return cmp;
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        });
    }

    const int MaxRiverWidthForBridge = 5;

    /// <summary>
    /// True if stepping onto this water cell valid as bridge: toward end, narrow strip of water then solid ground.
    /// </summary>
    private static bool IsValidBridgeSegment(List<Vector2Int> path, Vector2Int waterCell, Vector2Int end, int w, int h, HeightMap heightMap)
    {
        if (path.Count == 0) return false;
        int dx = end.x - waterCell.x;
        int dy = end.y - waterCell.y;
        int stepX, stepY;
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
            stepY = 0;
        }
        else
        {
            stepX = 0;
            stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;
        }
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

    private static List<Vector2Int> GetValidBorderCells(int border, int w, int h, HeightMap heightMap, TerrainManager terrainManager)
    {
        var candidates = new List<Vector2Int>();
        switch (border)
        {
            case 0:
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, 0, w, h, heightMap, terrainManager)) candidates.Add(new Vector2Int(x, 0));
                break;
            case 1:
                for (int x = 0; x < w; x++)
                    if (IsCellAllowedForInterstate(x, h - 1, w, h, heightMap, terrainManager)) candidates.Add(new Vector2Int(x, h - 1));
                break;
            case 2:
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(0, y, w, h, heightMap, terrainManager)) candidates.Add(new Vector2Int(0, y));
                break;
            case 3:
                for (int y = 0; y < h; y++)
                    if (IsCellAllowedForInterstate(w - 1, y, w, h, heightMap, terrainManager)) candidates.Add(new Vector2Int(w - 1, y));
                break;
        }
        return candidates;
    }

    /// <summary>
    /// Return border cells for interstate: exclude water-slope tiles, sort by endpoint quality (flat interior + gentle mandatory first step).
    /// </summary>
    private List<Vector2Int> GetValidBorderCellsWithPreference(int border, int w, int h, HeightMap heightMap)
    {
        var raw = GetValidBorderCells(border, w, h, heightMap, terrainManager);
        if (raw.Count == 0) return raw;

        var filtered = new List<Vector2Int>();
        foreach (var c in raw)
        {
            if (terrainManager != null && terrainManager.IsWaterSlopeCell(c.x, c.y))
                continue;
            filtered.Add(c);
        }

        if (filtered.Count == 0)
            return raw;

        SortBorderCellsByInterstateEndpointQuality(filtered, w, h, heightMap);
        return filtered;
    }

    private Vector2Int? GetValidBorderCell(int border, int w, int h, HeightMap heightMap)
    {
        var candidates = GetValidBorderCellsWithPreference(border, w, h, heightMap);
        if (candidates.Count == 0) return null;
        int poolSize = Mathf.Min(candidates.Count, Mathf.Max(1, (candidates.Count + 2) / 3));
        int idx = Random.Range(0, poolSize);
        return candidates[idx];
    }

    /// <summary>
    /// A* pathfinding for interstate. Runs low-terrain + full-terrain A* when both reach goal, keeps lower <see cref="ComputePathCost"/> route (avoids mandatory long flat detours).
    /// Steps increasing Manhattan dist to goal add <see cref="RoadPathCostConstants.InterstateAwayFromGoalPenalty"/>. Falls back to BiasedWalkPath if both A* runs fail.
    /// Rule D: if primary path crosses hill, try parallel offset paths + pick lower cost.
    /// </summary>
    /// <summary>
    /// Run low-terrain + full-terrain A* when both reach <paramref name="end"/>; return path with lower <see cref="ComputePathCost"/>.
    /// Previously only low-terrain path used when it succeeded → forced long flat detours around hills.
    /// </summary>
    List<Vector2Int> PickLowerCostInterstateAStarPath(Vector2Int start, Vector2Int end, int w, int h, HeightMap heightMap)
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

    private List<Vector2Int> FindInterstatePathAStar(Vector2Int start, Vector2Int end, int w, int h, HeightMap heightMap)
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
            if (!IsCellAllowedForInterstate(offStart.x, offStart.y, w, h, heightMap, terrainManager)) continue;
            if (!IsCellAllowedForInterstate(offEnd.x, offEnd.y, w, h, heightMap, terrainManager)) continue;

            var altPath = PickLowerCostInterstateAStarPath(offStart, offEnd, w, h, heightMap);
            if (altPath == null || altPath.Count < 2 || altPath[altPath.Count - 1] != offEnd) continue;
            if (roadManager != null && !roadManager.ValidateBridgePath(altPath, heightMap)) continue;

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

    /// <summary>
    /// Remove redundant collinear points when direct step prev→next valid.
    /// Similar to GridPathfinder.SmoothPath but uses interstate validity checks.
    /// </summary>
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
            if (cardinalStep && IsDirectStepValid(prev, next, w, h, heightMap, pathEnd, terrainManager))
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

    private static bool IsDirectStepValid(Vector2Int from, Vector2Int to, int w, int h, HeightMap heightMap, Vector2Int pathEnd, TerrainManager terrainManager)
    {
        if (to.x < 0 || to.x >= w || to.y < 0 || to.y >= h) return false;
        int hFrom = heightMap.GetHeight(from.x, from.y);
        int hTo = heightMap.GetHeight(to.x, to.y);
        if (hTo == 0)
            return IsValidBridgeSegmentFrom(from, to, pathEnd, w, h, heightMap);
        if (hFrom > 0 && Mathf.Abs(hTo - hFrom) > 1) return false;
        return IsCellAllowedForInterstate(to.x, to.y, w, h, heightMap, terrainManager);
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

    private int ComputePathCost(List<Vector2Int> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 2 || heightMap == null || terrainManager == null) return int.MaxValue;
        int cost = 0;
        for (int i = 1; i < path.Count; i++)
        {
            var prev = path[i - 1];
            var curr = path[i];
            int hPrev = heightMap.GetHeight(prev.x, prev.y);
            int hCurr = heightMap.GetHeight(curr.x, curr.y);
            TerrainSlopeType terrain = hCurr == 0 ? TerrainSlopeType.Flat : terrainManager.GetTerrainSlopeTypeAt(curr.x, curr.y);
            int heightDiff = Mathf.Abs(hCurr - hPrev);
            cost += terrainManager.IsWaterSlopeCell(curr.x, curr.y)
                ? RoadPathCostConstants.WaterSlopeCost
                : RoadPathCostConstants.GetStepCostForInterstate(terrain, heightDiff);
        }
        return cost;
    }

    /// <summary>
    /// A* for interstate. When avoidHighTerrain true, block land cells at height &gt; 1 → path stays on flat/low terrain (no cut-through, no scaling hills).
    /// </summary>
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
                    ? IsCellAllowedForInterstate(neighbor.x, neighbor.y, w, h, heightMap, terrainManager)
                    : IsValidBridgeSegmentFrom(current, neighbor, end, w, h, heightMap);
                if (!allowed) continue;

                if (currentHeight > 0 && cellHeight > 0 && Mathf.Abs(cellHeight - currentHeight) > 1)
                    continue;

                TerrainSlopeType terrain = cellHeight == 0 ? TerrainSlopeType.Flat : terrainManager.GetTerrainSlopeTypeAt(neighbor.x, neighbor.y);
                int heightDiff = Mathf.Abs(cellHeight - currentHeight);
                int stepCost = terrainManager.IsWaterSlopeCell(neighbor.x, neighbor.y)
                    ? RoadPathCostConstants.WaterSlopeCost
                    : RoadPathCostConstants.GetStepCostForInterstate(terrain, heightDiff);

                int manCurr = Heuristic(current, end);
                int manNei = Heuristic(neighbor, end);
                if (manNei > manCurr)
                    stepCost += RoadPathCostConstants.InterstateAwayFromGoalPenalty;

                // Rule E: straightness bonus — prefer continuing in same direction (fewer zigzags).
                if (cameFrom.ContainsKey(current))
                {
                    Vector2Int dirFromPrev = new Vector2Int(current.x - cameFrom[current].x, current.y - cameFrom[current].y);
                    Vector2Int dirToNeighbor = new Vector2Int(neighbor.x - current.x, neighbor.y - current.y);
                    if (dirFromPrev == dirToNeighbor)
                        stepCost = Mathf.Max(1, stepCost - RoadPathCostConstants.InterstateStraightnessBonus);
                    else
                    {
                        stepCost += RoadPathCostConstants.InterstateTurnPenalty;
                        // Zigzag penalty: turn, 1 tile, turn back to original direction. Prefer single turn or water slope over S-curve.
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
        int stepX, stepY;
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            stepX = dx != 0 ? (dx > 0 ? 1 : -1) : 0;
            stepY = 0;
        }
        else
        {
            stepX = 0;
            stepY = dy != 0 ? (dy > 0 ? 1 : -1) : 0;
        }
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
                    allowed = IsCellAllowedForInterstate(c.x, c.y, w, h, heightMap, terrainManager);
                else if (cellHeight == 0)
                    allowed = IsValidBridgeSegment(path, c, end, w, h, heightMap);

                if (!allowed) continue;

                if (currentHeight > 0 && cellHeight > 0 && Mathf.Abs(cellHeight - currentHeight) > 1)
                    continue;

                TerrainSlopeType terrain = cellHeight == 0 ? TerrainSlopeType.Flat : terrainManager.GetTerrainSlopeTypeAt(c.x, c.y);
                int heightDiff = Mathf.Abs(cellHeight - currentHeight);
                int stepCost = terrainManager.IsWaterSlopeCell(c.x, c.y)
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
                CityCell cell = gridManager.GetCell(x, y);
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
    /// Check if given grid position is interstate cell.
    /// </summary>
    public bool IsInterstateAt(int x, int y)
    {
        if (gridManager == null) return false;
        CityCell cell = gridManager.GetCell(x, y);
        return cell != null && cell.isInterstate;
    }

    /// <summary>
    /// Check if given grid position is interstate cell.
    /// </summary>
    public bool IsInterstateAt(Vector2 gridPos)
    {
        return IsInterstateAt(Mathf.RoundToInt(gridPos.x), Mathf.RoundToInt(gridPos.y));
    }

    /// <summary>
    /// BFS from all interstate cells through road cells. Set isConnectedToInterstate if any player road reached.
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
        CityCell cell = gridManager.GetCell(gridX, gridY);
        return cell != null && cell.zoneType == Zone.ZoneType.Road;
    }

    /// <summary>
    /// Whether player can start placing street from this position (must touch interstate or connected road chain).
    /// </summary>
    public bool CanPlaceStreetFrom(Vector2 gridPosition)
    {
        int x = Mathf.RoundToInt(gridPosition.x);
        int y = Mathf.RoundToInt(gridPosition.y);
        return CanPlaceStreetFrom(x, y);
    }

    /// <summary>
    /// Whether player can start placing street from (x,y). Valid if any cardinal neighbor is interstate or road connected to interstate.
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
    /// BFS from (startX, startY) through road cells; return true if interstate cell reached.
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
    /// Set connectivity flag (e.g. on load from save).
    /// </summary>
    public void SetConnectedToInterstate(bool connected)
    {
        isConnectedToInterstate = connected;
    }
    #endregion
}
}
