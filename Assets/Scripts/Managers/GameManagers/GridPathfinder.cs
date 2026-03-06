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
        /// A* path over walkable cells (grass or road). Prefers flat terrain; cardinal slopes cost more; diagonal slopes are impassable.
        /// Max 200 nodes explored. Returns path including start and end, or empty if not found.
        /// </summary>
        public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to)
        {
            return FindPathWithRoadSpacing(from, to, 0);
        }

        /// <summary>
        /// A* path with optional extra cost for cells close to existing roads, so paths tend to keep
        /// minDistanceFromRoad cells away and leave space for zones.
        /// When minDistanceFromRoad is 0, behaves like FindPath.
        /// </summary>
        public List<Vector2Int> FindPathWithRoadSpacing(Vector2Int from, Vector2Int to, int minDistanceFromRoad)
        {
            const int maxNodes = 200;
            const int roadProximityPenalty = 18;
            HashSet<Vector2Int> roadSet = null;
            if (minDistanceFromRoad > 0)
            {
                var roads = grid.GetAllRoadPositions();
                roadSet = new HashSet<Vector2Int>(roads);
            }

            if (!IsWalkable(from.x, from.y) || !IsWalkable(to.x, to.y))
                return new List<Vector2Int>();
            var open = new List<Vector2Int>();
            var closed = new HashSet<Vector2Int>();
            var gScore = new Dictionary<Vector2Int, int>();
            var fScore = new Dictionary<Vector2Int, int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            gScore[from] = 0;
            fScore[from] = Heuristic(from, to);
            open.Add(from);
            int explored = 0;
            while (open.Count > 0 && explored < maxNodes)
            {
                explored++;
                open.Sort((a, b) => (fScore.ContainsKey(a) ? fScore[a] : int.MaxValue).CompareTo(fScore.ContainsKey(b) ? fScore[b] : int.MaxValue));
                Vector2Int current = open[0];
                open.RemoveAt(0);
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
                    return path;
                }
                closed.Add(current);
                foreach (Vector2Int neighbor in GetWalkableNeighbors(current))
                {
                    if (closed.Contains(neighbor)) continue;
                    int stepCost = GetRoadStepCost(neighbor.x, neighbor.y);
                    if (stepCost == int.MaxValue) continue;
                    if (minDistanceFromRoad > 0 && roadSet != null)
                    {
                        int dist = MinManhattanDistanceToSet(neighbor.x, neighbor.y, roadSet);
                        if (dist > 0 && dist < minDistanceFromRoad)
                            stepCost += roadProximityPenalty;
                    }
                    int tentative = (gScore.ContainsKey(current) ? gScore[current] : int.MaxValue) + stepCost;
                    if (!gScore.ContainsKey(neighbor) || tentative < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentative;
                        fScore[neighbor] = tentative + Heuristic(neighbor, to);
                        if (!open.Contains(neighbor))
                            open.Add(neighbor);
                    }
                }
            }
            return new List<Vector2Int>();
        }

        private static int MinManhattanDistanceToSet(int x, int y, HashSet<Vector2Int> set)
        {
            int min = int.MaxValue;
            foreach (Vector2Int p in set)
            {
                int d = Mathf.Abs(x - p.x) + Mathf.Abs(y - p.y);
                if (d < min) min = d;
            }
            return min == int.MaxValue ? int.MaxValue : min;
        }

        private int GetRoadStepCost(int x, int y)
        {
            if (!IsWalkable(x, y)) return int.MaxValue;
            if (grid.terrainManager == null) return 1;
            TerrainSlopeType t = grid.terrainManager.GetTerrainSlopeTypeAt(x, y);
            switch (t)
            {
                case TerrainSlopeType.Flat: return 1;
                case TerrainSlopeType.North:
                case TerrainSlopeType.South:
                case TerrainSlopeType.East:
                case TerrainSlopeType.West: return 8;
                default: return int.MaxValue;
            }
        }

        private bool IsWalkable(int x, int y)
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return false;
            Cell c = grid.GetCell(x, y);
            if (c == null) return false;
            return c.zoneType == Zone.ZoneType.Grass || c.zoneType == Zone.ZoneType.Road;
        }

        private List<Vector2Int> GetWalkableNeighbors(Vector2Int p)
        {
            var list = new List<Vector2Int>();
            int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                int nx = p.x + dx[i], ny = p.y + dy[i];
                if (IsWalkable(nx, ny))
                    list.Add(new Vector2Int(nx, ny));
            }
            return list;
        }

        private static int Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
