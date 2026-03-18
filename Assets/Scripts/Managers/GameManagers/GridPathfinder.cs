using UnityEngine;
using System.Collections.Generic;
using Territory.Zones;
using Territory.Terrain;

namespace Territory.Core
{
    /// <summary>
    /// A* pathfinding over the grid for road building. Supports flat/slope terrain costs and
    /// optional road-spacing penalty so new roads keep distance from existing ones.
    /// Extracted from GridManager to reduce its responsibilities.
    /// </summary>
    public class GridPathfinder
    {
        private readonly GridManager grid;

        public GridPathfinder(GridManager grid)
        {
            this.grid = grid;
        }

        /// <summary>
        /// A* path over walkable cells (grass or road). Strongly prefers flat terrain; slopes cost 35-60 so paths
        /// tend to go around hills rather than cross them. Max nodes scales with Manhattan distance (min 200).
        /// Returns smoothed path including start and end, or empty if not found.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to)
        {
            return FindPathWithRoadSpacing(from, to, 0);
        }

        private static readonly int[] NeighborDx = { 1, -1, 0, 0 };
        private static readonly int[] NeighborDy = { 0, 0, 1, -1 };

        private readonly Vector2Int[] neighborBuffer = new Vector2Int[4];

        /// <summary>
        /// A* path with optional extra cost for cells close to existing roads, so paths tend to keep
        /// minDistanceFromRoad cells away and leave space for zones.
        /// When minDistanceFromRoad is 0, behaves like FindPath.
        /// Uses shared cost model from RoadPathCostConstants; maxNodes = max(200, manhattan * 4).
        /// </summary>
        public List<Vector2Int> FindPathWithRoadSpacing(Vector2Int from, Vector2Int to, int minDistanceFromRoad)
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

            if (!IsWalkable(from.x, from.y) || !IsWalkable(to.x, to.y))
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
                    return SmoothPath(path);
                }
                int neighborCount = GetWalkableNeighbors(current, neighborBuffer);
                for (int i = 0; i < neighborCount; i++)
                {
                    Vector2Int neighbor = neighborBuffer[i];
                    if (closed.Contains(neighbor)) continue;
                    int stepCost = GetRoadStepCost(current.x, current.y, neighbor.x, neighbor.y);
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

        private int GetRoadStepCost(int fromX, int fromY, int toX, int toY)
        {
            if (!IsWalkable(toX, toY)) return int.MaxValue;
            if (grid.terrainManager == null) return RoadPathCostConstants.Flat;

            if (grid.terrainManager.IsWaterSlopeCell(toX, toY))
                return RoadPathCostConstants.WaterSlopeCost;

            var heightMap = grid.terrainManager.GetHeightMap();
            int heightDiff = 0;
            if (heightMap != null)
            {
                int hFrom = heightMap.GetHeight(fromX, fromY);
                int hTo = heightMap.GetHeight(toX, toY);
                if (Mathf.Abs(hTo - hFrom) > 1) return int.MaxValue;
                heightDiff = Mathf.Abs(hTo - hFrom);
            }

            TerrainSlopeType t = grid.terrainManager.GetTerrainSlopeTypeAt(toX, toY);
            return RoadPathCostConstants.GetStepCost(t, heightDiff);
        }

        private bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return false;
            Cell c = grid.GetCell(x, y);
            if (c == null) return false;
            return c.zoneType == Zone.ZoneType.Grass || c.zoneType == Zone.ZoneType.Road;
        }

        private int GetWalkableNeighbors(Vector2Int p, Vector2Int[] buffer)
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                int nx = p.x + NeighborDx[i], ny = p.y + NeighborDy[i];
                if (IsWalkable(nx, ny))
                    buffer[count++] = new Vector2Int(nx, ny);
            }
            return count;
        }

        private static int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        /// <summary>
        /// Removes redundant zigzag points: when prev and next are cardinal neighbors,
        /// the middle point can be skipped if the direct step is valid.
        /// </summary>
        private List<Vector2Int> SmoothPath(List<Vector2Int> path)
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
                if (cardinalNeighbors && IsWalkable(next.x, next.y))
                {
                    int stepCost = GetRoadStepCost(prev.x, prev.y, next.x, next.y);
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
