// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using UnityEngine;
using System.Collections.Generic;
using Territory.Zones;
using Territory.Terrain;
using Territory.Utilities;
using Territory.Utilities.Compute;

namespace Territory.Core
{
    /// <summary>
    /// A* pathfinding over grid for road building. Flat/slope terrain costs + optional road-spacing penalty
    /// → new roads keep distance from existing ones. Extracted from <see cref="GridManager"/> to reduce responsibilities.
    /// </summary>
    public class GridPathfinder
    {
        private readonly GridManager grid;

        public GridPathfinder(GridManager grid)
        {
            this.grid = grid;
        }

        /// <summary>
        /// A* path over walkable cells (grass or road). Strongly prefers flat terrain; slopes cost 35-60 → paths
        /// go around hills vs cross them. Max nodes scales with Manhattan distance (min 200).
        /// Returns smoothed path including start + end; empty if not found.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to)
        {
            return FindPathWithRoadSpacing(from, to, 0);
        }

        /// <summary>
        /// A* for AUTO sim only: walkable cells include undeveloped light zoning. Manual draw → <see cref="FindPath"/>.
        /// </summary>
        public List<Vector2Int> FindPathForAutoSimulation(Vector2Int from, Vector2Int to)
        {
            return FindPathWithRoadSpacingForAutoSimulation(from, to, 0);
        }

        /// <summary>
        /// A* with road-spacing penalty for AUTO sim. Allows undeveloped light zoning as walkable.
        /// </summary>
        public List<Vector2Int> FindPathWithRoadSpacingForAutoSimulation(Vector2Int from, Vector2Int to, int minDistanceFromRoad)
        {
            return FindPathWithRoadSpacingCore(from, to, minDistanceFromRoad, allowUndevelopedLightZoning: true);
        }

        private static readonly int[] NeighborDx = { 1, -1, 0, 0 };
        private static readonly int[] NeighborDy = { 0, 0, 1, -1 };

        private readonly Vector2Int[] neighborBuffer = new Vector2Int[4];

        /// <summary>
        /// A* path with optional extra cost for cells close to existing roads → paths keep
        /// <paramref name="minDistanceFromRoad"/> cells away + leave space for zones.
        /// <paramref name="minDistanceFromRoad"/>=0 → behaves like <see cref="FindPath"/>.
        /// Uses shared cost model from <see cref="RoadPathCostConstants"/>; maxNodes = max(200, manhattan * 4).
        /// </summary>
        public List<Vector2Int> FindPathWithRoadSpacing(Vector2Int from, Vector2Int to, int minDistanceFromRoad)
        {
            return FindPathWithRoadSpacingCore(from, to, minDistanceFromRoad, allowUndevelopedLightZoning: false);
        }

        private List<Vector2Int> FindPathWithRoadSpacingCore(Vector2Int from, Vector2Int to, int minDistanceFromRoad, bool allowUndevelopedLightZoning)
        {
            int manhattan = Mathf.Abs(to.x - from.x) + Mathf.Abs(to.y - from.y);
            int maxNodes = Mathf.Max(200, manhattan * 4);
            const int roadProximityPenalty = 18;
            HashSet<Vector2Int> roadSet = null;
            if (minDistanceFromRoad > 0)
            {
                var roads = grid.GetAllRoadPositions();
                roadSet = new HashSet<Vector2Int>(roads);
            }

            if (!IsWalkable(from.x, from.y, allowUndevelopedLightZoning) || !IsWalkable(to.x, to.y, allowUndevelopedLightZoning))
                return new List<Vector2Int>();
            var open = new MinHeap();
            var closed = new HashSet<Vector2Int>();
            var gScore = new Dictionary<Vector2Int, int>();
            var fScore = new Dictionary<Vector2Int, int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            gScore[from] = 0;
            fScore[from] = Heuristic(from, to);
            open.Enqueue(from, fScore[from]);
            int explored = 0;
            while (open.Count > 0 && explored < maxNodes)
            {
                explored++;
                Vector2Int current = open.Dequeue();
                if (closed.Contains(current)) continue;
                closed.Add(current);
                if (current == to)
                {
                    var path = new List<Vector2Int>();
                    while (cameFrom.ContainsKey(current))
                    {
                        path.Add(current);
                        current = cameFrom[current];
                    }
                    path.Add(from);
                    path.Reverse();
                    return SmoothPath(path, allowUndevelopedLightZoning);
                }
                int neighborCount = GetWalkableNeighbors(current, neighborBuffer, allowUndevelopedLightZoning);
                for (int i = 0; i < neighborCount; i++)
                {
                    Vector2Int neighbor = neighborBuffer[i];
                    if (closed.Contains(neighbor)) continue;
                    int stepCost = GetRoadStepCost(current.x, current.y, neighbor.x, neighbor.y, allowUndevelopedLightZoning);
                    if (stepCost == int.MaxValue) continue;
                    if (minDistanceFromRoad > 0 && roadSet != null)
                    {
                        int dist = MinManhattanDistanceToSet(neighbor.x, neighbor.y, roadSet, minDistanceFromRoad);
                        if (dist > 0 && dist < minDistanceFromRoad)
                            stepCost += roadProximityPenalty;
                    }
                    int tentative = (gScore.ContainsKey(current) ? gScore[current] : int.MaxValue) + stepCost;
                    if (!gScore.ContainsKey(neighbor) || tentative < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentative;
                        int f = tentative + Heuristic(neighbor, to);
                        fScore[neighbor] = f;
                        open.Enqueue(neighbor, f);
                    }
                }
            }
            return new List<Vector2Int>();
        }

        private static int MinManhattanDistanceToSet(int x, int y, HashSet<Vector2Int> set, int earlyOutThreshold = int.MaxValue)
        {
            int min = int.MaxValue;
            foreach (Vector2Int p in set)
            {
                int d = Mathf.Abs(x - p.x) + Mathf.Abs(y - p.y);
                if (d < min) min = d;
                if (min < earlyOutThreshold) return min;
            }
            return min == int.MaxValue ? int.MaxValue : min;
        }

        private int GetRoadStepCost(int fromX, int fromY, int toX, int toY, bool allowUndevelopedLightZoning)
        {
            if (!IsWalkable(toX, toY, allowUndevelopedLightZoning)) return int.MaxValue;
            if (grid.terrainManager == null) return RoadPathCostConstants.Flat;

            var tm = grid.terrainManager;
            int hFrom = 0;
            int hTo = 0;
            var heightMap = tm.GetHeightMap();
            if (heightMap != null)
            {
                hFrom = heightMap.GetHeight(fromX, fromY);
                hTo = heightMap.GetHeight(toX, toY);
            }

            bool coastalFrom = tm.IsRegisteredOpenWaterAt(fromX, fromY) || tm.IsWaterSlopeCell(fromX, fromY)
                || tm.IsDryShoreOrRimMembershipEligible(fromX, fromY);
            bool coastalTo = tm.IsRegisteredOpenWaterAt(toX, toY) || tm.IsWaterSlopeCell(toX, toY)
                || tm.IsDryShoreOrRimMembershipEligible(toX, toY);

            var ctx = new PathfindingCostKernel.PathfindingMoveContext(
                hFrom,
                hTo,
                tm.GetTerrainSlopeTypeAt(toX, toY),
                tm.IsWaterSlopeCell(toX, toY),
                coastalFrom,
                coastalTo);
            return PathfindingCostKernel.GetOrdinaryRoadMoveCost(in ctx);
        }

        private bool IsWalkable(int x, int y, bool allowUndevelopedLightZoning)
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return false;
            CityCell c = grid.GetCell(x, y);
            if (c == null) return false;
            if (c.zoneType == Zone.ZoneType.Road)
            {
                if (grid.terrainManager != null && !grid.terrainManager.CanPlaceRoad(x, y, allowWaterSlopeForWaterBridgeTrace: true))
                    return false;
                if (!IsRoadPathfindingLandSlopeAllowed(x, y))
                    return false;
                return true;
            }
            if (c.zoneType == Zone.ZoneType.Grass)
            {
                if (grid.terrainManager != null && !grid.terrainManager.CanPlaceRoad(x, y, allowWaterSlopeForWaterBridgeTrace: true))
                    return false;
                if (!IsRoadPathfindingLandSlopeAllowed(x, y))
                    return false;
                return true;
            }
            if (allowUndevelopedLightZoning && AutoSimulationRoadRules.IsAutoRoadLandCell(grid, x, y))
            {
                if (grid.terrainManager != null && !grid.terrainManager.CanPlaceRoad(x, y, allowWaterSlopeForWaterBridgeTrace: true))
                    return false;
                if (!IsRoadPathfindingLandSlopeAllowed(x, y))
                    return false;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Roads may only cross flat or cardinal ramp land slopes (same rule as <see cref="RoadStrokeTerrainRules"/>).
        /// </summary>
        private bool IsRoadPathfindingLandSlopeAllowed(int x, int y)
        {
            if (grid.terrainManager == null)
                return true;
            HeightMap hm = grid.terrainManager.GetHeightMap();
            if (hm != null && hm.IsValidPosition(x, y) && hm.GetHeight(x, y) < 0)
                return true;
            if (grid.terrainManager.IsWaterSlopeCell(x, y))
                return true;
            TerrainSlopeType st = grid.terrainManager.GetTerrainSlopeTypeAt(x, y);
            return RoadStrokeTerrainRules.IsLandSlopeAllowedForRoadStroke(st);
        }

        private int GetWalkableNeighbors(Vector2Int p, Vector2Int[] buffer, bool allowUndevelopedLightZoning)
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                int nx = p.x + NeighborDx[i], ny = p.y + NeighborDy[i];
                if (IsWalkable(nx, ny, allowUndevelopedLightZoning))
                    buffer[count++] = new Vector2Int(nx, ny);
            }
            return count;
        }

        private static int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        /// <summary>
        /// Remove redundant zigzag points: when prev + next cardinal neighbors, skip middle if direct step valid.
        /// </summary>
        private List<Vector2Int> SmoothPath(List<Vector2Int> path, bool allowUndevelopedLightZoning)
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
                bool cardinalNeighbors = (Mathf.Abs(dx) == 1 && dy == 0) || (dx == 0 && Mathf.Abs(dy) == 1);
                if (cardinalNeighbors && IsWalkable(next.x, next.y, allowUndevelopedLightZoning))
                {
                    int stepCost = GetRoadStepCost(prev.x, prev.y, next.x, next.y, allowUndevelopedLightZoning);
                    if (stepCost != int.MaxValue)
                    {
                        result.Add(next);
                        i++;
                        continue;
                    }
                }
                result.Add(curr);
            }
            Vector2Int last = path[path.Count - 1];
            if (result.Count == 0 || result[result.Count - 1] != last)
                result.Add(last);
            return result;
        }
    }
}
