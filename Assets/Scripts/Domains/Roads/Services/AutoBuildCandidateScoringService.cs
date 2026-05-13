using UnityEngine;
using System.Collections.Generic;
using Territory.Core;
using Territory.Roads;
using Territory.Zones;
using Random = UnityEngine.Random;

namespace Domains.Roads.Services
{
/// <summary>
/// Candidate-scoring sub-service: road-edge candidate selection, cluster detection, segment analysis.
/// Extracted from AutoBuildService (Stage 7.0 Tier-E split). No MonoBehaviour.
/// </summary>
public class AutoBuildCandidateScoringService
{
    private IGridManager _gridManager;
    private IUrbanCentroidService _urbanCentroidService;
    private AutoBuildSimRulesService _simRules;

    public AutoBuildCandidateScoringService(
        IGridManager gridManager,
        IUrbanCentroidService urbanCentroidService,
        AutoBuildSimRulesService simRules)
    {
        _gridManager = gridManager;
        _urbanCentroidService = urbanCentroidService;
        _simRules = simRules;
    }

    public void RefreshDependencies(
        IGridManager gridManager,
        IUrbanCentroidService urbanCentroidService,
        AutoBuildSimRulesService simRules)
    {
        _gridManager = gridManager;
        _urbanCentroidService = urbanCentroidService;
        _simRules = simRules;
    }

    public Vector2Int GetRoadDirectionAtEdge(Vector2Int edge, HashSet<Vector2Int> roadSet)
    {
        int roadX = 0, roadY = 0;
        int[] dx = AutoBuildSimRulesService.Dx;
        int[] dy = AutoBuildSimRulesService.Dy;
        for (int d = 0; d < 4; d++)
        {
            var n = new Vector2Int(edge.x + dx[d], edge.y + dy[d]);
            if (roadSet.Contains(n))
            {
                roadX += dx[d];
                roadY += dy[d];
            }
        }
        if (roadX == 0 && roadY == 0) return Vector2Int.zero;
        if (Mathf.Abs(roadX) >= Mathf.Abs(roadY))
            return new Vector2Int(roadX > 0 ? 1 : -1, 0);
        return new Vector2Int(0, roadY > 0 ? 1 : -1);
    }

    public int HowFarWeCanBuild(Vector2Int start, Vector2Int dir)
    {
        int count = 0;
        int x = start.x, y = start.y;
        int w = _gridManager.width, h = _gridManager.height;
        while (x >= 0 && x < w && y >= 0 && y < h)
        {
            if (!_simRules.IsCellPlaceableForRoad(x, y))
                break;
            if (!_simRules.IsSuitableForRoad(x, y, dir))
                break;
            CityCell c = _gridManager.GetCell(x, y);
            bool isWater = c != null && c.GetCellInstanceHeight() == 0;
            if (isWater)
            {
                int landStep = -1;
                for (int k = 1; k <= AutoBuildSimRulesService.MaxBridgeWaterTiles; k++)
                {
                    int nx = x + k * dir.x, ny = y + k * dir.y;
                    if (nx < 0 || nx >= w || ny < 0 || ny >= h) break;
                    CityCell nc = _gridManager.GetCell(nx, ny);
                    if (nc == null || nc.GetCellInstanceHeight() == 0) continue;
                    if (!_simRules.IsCellPlaceableForRoad(nx, ny) || !_simRules.IsSuitableForRoad(nx, ny, dir)) continue;
                    landStep = k;
                    break;
                }
                if (landStep < 0)
                    break;
                count += (1 + landStep);
                x += (landStep + 1) * dir.x;
                y += (landStep + 1) * dir.y;
                continue;
            }
            count++;
            x += dir.x;
            y += dir.y;
        }
        return count;
    }

    public List<List<Vector2Int>> GetRoadClusters(List<Vector2Int> all)
    {
        int[] dx = AutoBuildSimRulesService.Dx;
        int[] dy = AutoBuildSimRulesService.Dy;
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
                    int nx = c.x + dx[i], ny = c.y + dy[i];
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

    public List<(Vector2Int origin, Vector2Int dir, int length)> GetStraightSegmentsFromGrid(HashSet<Vector2Int> roadSet, List<Vector2Int> edges)
    {
        var segments = new List<(Vector2Int origin, Vector2Int dir, int length)>();
        var seen = new HashSet<(int ox, int oy, int dx, int dy)>();
        int w = _gridManager.width, h = _gridManager.height;

        foreach (Vector2Int edge in edges)
        {
            Vector2Int roadDir = GetRoadDirectionAtEdge(edge, roadSet);
            if (roadDir.x == 0 && roadDir.y == 0) continue;

            Vector2Int pos = edge;
            while (true)
            {
                int px = pos.x - roadDir.x, py = pos.y - roadDir.y;
                if (px < 0 || px >= w || py < 0 || py >= h) break;
                Vector2Int prev = new Vector2Int(px, py);
                if (!roadSet.Contains(prev)) break;
                pos = prev;
            }
            Vector2Int origin = pos;

            int length = 0;
            pos = origin;
            while (pos.x >= 0 && pos.x < w && pos.y >= 0 && pos.y < h && roadSet.Contains(pos))
            {
                length++;
                pos = new Vector2Int(pos.x + roadDir.x, pos.y + roadDir.y);
            }

            if (length < 2) continue;

            Vector2Int end = new Vector2Int(origin.x + (length - 1) * roadDir.x, origin.y + (length - 1) * roadDir.y);
            Vector2Int canonOrigin = (origin.x < end.x || (origin.x == end.x && origin.y <= end.y)) ? origin : end;
            Vector2Int canonDir = (origin.x < end.x || (origin.x == end.x && origin.y <= end.y)) ? roadDir : new Vector2Int(-roadDir.x, -roadDir.y);
            var key = (canonOrigin.x, canonOrigin.y, canonDir.x, canonDir.y);
            if (seen.Contains(key)) continue;
            seen.Add(key);

            segments.Add((canonOrigin, canonDir, length));
        }
        return segments;
    }

    public bool IsSegmentFullyBlocked(Vector2Int origin, Vector2Int dir, int length, HashSet<Vector2Int> roadSet)
    {
        Vector2Int perp = new Vector2Int(-dir.y, dir.x);
        int w = _gridManager.width, h = _gridManager.height;

        for (int k = 0; k <= length - 2; k++)
        {
            for (int j = 1; j <= 4; j++)
            {
                Vector2Int left = new Vector2Int(origin.x + k * dir.x + j * perp.x, origin.y + k * dir.y + j * perp.y);
                Vector2Int right = new Vector2Int(origin.x + k * dir.x - j * perp.x, origin.y + k * dir.y - j * perp.y);

                foreach (Vector2Int cell in new[] { left, right })
                {
                    if (cell.x < 0 || cell.x >= w || cell.y < 0 || cell.y >= h) continue;
                    if (roadSet.Contains(cell)) continue;

                    CityCell c = _gridManager.GetCell(cell.x, cell.y);
                    if (c == null) return false;
                    if (c.GetCellInstanceHeight() == 0) continue;
                    if (c.zoneType == Zone.ZoneType.Grass || c.HasForest()) return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Score and rank road edges; return sorted candidates with (edge, params, validDirections).
    /// Caller drives the actual project execution.
    /// </summary>
    public List<(Vector2Int edge, RingStreetParams @params, UrbanRing ring, List<(Vector2Int dir, Vector2Int tip, int len)> dirs)>
        RankCandidateEdges(
            List<Vector2Int> edges,
            HashSet<Vector2Int> roadSet,
            int effectiveMinStreetLength,
            int minParallelSpacingFromEdge,
            int minEdgeSpacing,
            int coreInnerMinEdgeSpacing)
    {
        RingStreetParams fallbackParams = new RingStreetParams
        {
            minLength = effectiveMinStreetLength,
            maxLength = 20,
            parallelSpacing = minParallelSpacingFromEdge,
            parallelSpacingMin = minParallelSpacingFromEdge,
            parallelSpacingMax = minParallelSpacingFromEdge
        };

        var withScore = new List<KeyValuePair<Vector2Int, float>>(edges.Count);
        bool centroidShifted = _urbanCentroidService != null && _urbanCentroidService.CentroidShiftedRecently;
        foreach (Vector2Int e in edges)
        {
            UrbanRing eRing = _urbanCentroidService != null ? _urbanCentroidService.GetUrbanRing(new Vector2(e.x, e.y)) : UrbanRing.Mid;
            int ringPriority = AutoBuildSimRulesService.GetRingPriority(eRing);
            if (centroidShifted && (eRing == UrbanRing.Inner))
                ringPriority += 3;
            int grass = _simRules.CountGrassNeighbors(e);
            RingStreetParams edgeParams = _urbanCentroidService != null ? _urbanCentroidService.GetStreetParamsForRing(eRing) : fallbackParams;
            int spacing = AutoBuildSimRulesService.GetEffectiveParallelSpacing(edgeParams);
            float bestUtil = 0f;
            int[] dx = AutoBuildSimRulesService.Dx;
            int[] dy = AutoBuildSimRulesService.Dy;
            for (int d = 0; d < 4; d++)
            {
                int nx = e.x + dx[d], ny = e.y + dy[d];
                if (nx < 0 || nx >= _gridManager.width || ny < 0 || ny >= _gridManager.height) continue;
                if (!_simRules.IsCellPlaceableForRoad(nx, ny)) continue;
                Vector2Int dir = new Vector2Int(dx[d], dy[d]);
                if (!_simRules.IsSuitableForRoad(nx, ny, dir)) continue;
                Vector2Int rdE = GetRoadDirectionAtEdge(e, roadSet);
                Vector2Int? exclE = (rdE.x != 0 || rdE.y != 0) && (dir.x * rdE.x + dir.y * rdE.y) == 0 ? (Vector2Int?)rdE : null;
                if (_simRules.HasParallelRoadTooClose(e, dir, spacing, roadSet, exclE)) continue;
                Vector2Int tip = new Vector2Int(nx, ny);
                int len = HowFarWeCanBuild(tip, dir);
                int scoreMin = edgeParams.minLength;
                if (_simRules.IsEdgeOnInterstate(e)) scoreMin = Mathf.Min(scoreMin, 2);
                if (len < scoreMin && len >= 2 && _simRules.IsDirectionBlockedBySlopeOrWater(tip, dir, len)) scoreMin = 2;
                if (len < scoreMin) continue;
                float u = _simRules.CalculateDirectionUtility(e, dir, 5, spacing, roadSet);
                if (u > bestUtil) bestUtil = u;
            }
            float score = ringPriority * 100f + grass * 5f + bestUtil * 10f;
            withScore.Add(new KeyValuePair<Vector2Int, float>(e, score));
        }
        withScore.Sort((a, b) => b.Value.CompareTo(a.Value));

        var result = new List<(Vector2Int edge, RingStreetParams @params, UrbanRing ring, List<(Vector2Int dir, Vector2Int tip, int len)> dirs)>();
        var consideredEdges = new HashSet<Vector2Int>();

        foreach (var kv in withScore)
        {
            Vector2Int edge = kv.Key;
            UrbanRing edgeRing = _urbanCentroidService != null ? _urbanCentroidService.GetUrbanRing(new Vector2(edge.x, edge.y)) : UrbanRing.Mid;
            RingStreetParams @params = fallbackParams;
            if (_urbanCentroidService != null)
            {
                @params = _urbanCentroidService.GetStreetParamsForRing(edgeRing);
                if (edgeRing != UrbanRing.Inner)
                    @params.minLength = Mathf.Max(@params.minLength, effectiveMinStreetLength);
                if (edgeRing == UrbanRing.Mid || edgeRing == UrbanRing.Outer || edgeRing == UrbanRing.Rural)
                    @params.minLength = Mathf.Max(@params.minLength, 3);
            }
            else
            {
                @params.parallelSpacing = minParallelSpacingFromEdge;
                @params.parallelSpacingMin = minParallelSpacingFromEdge;
                @params.parallelSpacingMax = minParallelSpacingFromEdge;
            }
            if (_simRules.IsEdgeOnInterstate(edge))
                @params.minLength = Mathf.Min(@params.minLength, 2);

            int effectiveEdgeSpacing = _simRules.GetEffectiveMinEdgeSpacing(edgeRing, minEdgeSpacing, coreInnerMinEdgeSpacing);
            if (effectiveEdgeSpacing > 0)
            {
                bool tooClose = false;
                foreach (Vector2Int c in consideredEdges)
                {
                    if (Mathf.Abs(edge.x - c.x) + Mathf.Abs(edge.y - c.y) < effectiveEdgeSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;
                consideredEdges.Add(edge);
            }

            int[] dx2 = AutoBuildSimRulesService.Dx;
            int[] dy2 = AutoBuildSimRulesService.Dy;
            var validDirections = new List<(Vector2Int dir, Vector2Int tip, int len)>();
            for (int d = 0; d < 4; d++)
            {
                int nx = edge.x + dx2[d], ny = edge.y + dy2[d];
                if (nx < 0 || nx >= _gridManager.width || ny < 0 || ny >= _gridManager.height)
                    continue;
                if (!_simRules.IsCellPlaceableForRoad(nx, ny))
                    continue;
                Vector2Int dir = new Vector2Int(dx2[d], dy2[d]);
                Vector2Int tip = new Vector2Int(nx, ny);
                if (!_simRules.IsSuitableForRoad(nx, ny, dir))
                    continue;
                int spacing = AutoBuildSimRulesService.GetEffectiveParallelSpacing(@params);
                Vector2Int rdEdge = GetRoadDirectionAtEdge(edge, roadSet);
                Vector2Int? exclEdge = (rdEdge.x != 0 || rdEdge.y != 0) && (dir.x * rdEdge.x + dir.y * rdEdge.y) == 0 ? (Vector2Int?)rdEdge : null;
                if (_simRules.HasParallelRoadTooClose(edge, dir, spacing, roadSet, exclEdge))
                    continue;
                int len = HowFarWeCanBuild(tip, dir);
                int effectiveMin = @params.minLength;
                if (len < effectiveMin && len >= 2 && _simRules.IsDirectionBlockedBySlopeOrWater(tip, dir, len))
                    effectiveMin = 2;
                if (len < effectiveMin)
                    continue;
                validDirections.Add((dir, tip, len));
            }

            if (validDirections.Count > 0)
            {
                UrbanRing ringForSort = edgeRing;
                Vector2Int roadDir = GetRoadDirectionAtEdge(edge, roadSet);
                int sortSpacing = AutoBuildSimRulesService.GetEffectiveParallelSpacing(@params);
                validDirections.Sort((a, b) =>
                {
                    if (ringForSort == UrbanRing.Inner)
                    {
                        bool aPerp = roadDir.x == 0 && roadDir.y == 0 ? false : (a.dir.x * roadDir.x + a.dir.y * roadDir.y) == 0;
                        bool bPerp = roadDir.x == 0 && roadDir.y == 0 ? false : (b.dir.x * roadDir.x + b.dir.y * roadDir.y) == 0;
                        if (aPerp != bPerp) return aPerp ? -1 : 1;
                    }
                    float utilA = _simRules.CalculateDirectionUtility(edge, a.dir, 5, sortSpacing, roadSet);
                    float utilB = _simRules.CalculateDirectionUtility(edge, b.dir, 5, sortSpacing, roadSet);
                    return utilB.CompareTo(utilA);
                });
            }

            result.Add((edge, @params, edgeRing, validDirections));
        }
        return result;
    }
}
}
