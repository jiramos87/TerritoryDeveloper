// long-file-allowed: legacy hub — scope outside current atomization plan; deferred to future sweep
using UnityEngine;
using System.Collections.Generic;
using Territory.Zones;
using Territory.Terrain;
using Territory.Utilities;

namespace Territory.Core
{
    /// <summary>
    /// Lazy-cached index of road positions + road-edge (frontier) positions.
    /// Also adjacency + zoneability queries used by auto-zoning + auto-road building.
    /// Extracted from GridManager → reduce its responsibilities.
    /// </summary>
    public class RoadCacheService
    {
        private static readonly int[] Dx = { 1, -1, 0, 0 };
        private static readonly int[] Dy = { 0, 0, 1, -1 };
        /// <summary>Diagonal directions for axial corridor. Roads connect diagonally via elbows.</summary>
        private static readonly int[] Dx8 = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] Dy8 = { 0, 0, 1, -1, 1, -1, 1, -1 };

        private readonly GridManager grid;
        private List<Vector2Int> cachedRoadPositions;
        private HashSet<Vector2Int> cachedRoadPositionsSet;
        private List<Vector2Int> cachedRoadEdgePositions;
        private HashSet<Vector2Int> cachedRoadExtensionCells;
        private HashSet<Vector2Int> cachedRoadAxialCorridor;
        private HashSet<Vector2Int> cachedCellsWithinDistanceOfRoad;
        private int cachedCellsWithinDistanceMax = -1;
        private bool roadCacheDirty = true;

        /// <summary>Cells reserved for road extension. Extend corridor this many cells beyond each segment end.</summary>
        private const int AxialCorridorLength = 8;

        public RoadCacheService(GridManager grid)
        {
            this.grid = grid;
        }

        /// <summary>Mark cached road positions stale → rebuilt on next query.</summary>
        public void Invalidate()
        {
            roadCacheDirty = true;
            cachedRoadExtensionCells = null;
            cachedRoadAxialCorridor = null;
            cachedCellsWithinDistanceOfRoad = null;
            cachedCellsWithinDistanceMax = -1;
        }

        /// <summary>Add road position to cache incrementally. Call on road tile placed.</summary>
        public void AddRoad(Vector2Int pos)
        {
            if (cachedRoadPositionsSet == null)
            {
                cachedRoadPositions = new List<Vector2Int>();
                cachedRoadPositionsSet = new HashSet<Vector2Int>();
            }
            if (cachedRoadPositionsSet.Add(pos))
            {
                cachedRoadPositions.Add(pos);
                cachedRoadEdgePositions = null;
                cachedRoadExtensionCells = null;
                cachedRoadAxialCorridor = null;
                cachedCellsWithinDistanceOfRoad = null;
                cachedCellsWithinDistanceMax = -1;
            }
            roadCacheDirty = false;
        }

        /// <summary>Remove road position from cache incrementally. Call on road tile demolished.</summary>
        public void RemoveRoad(Vector2Int pos)
        {
            if (cachedRoadPositionsSet == null) return;
            if (cachedRoadPositionsSet.Remove(pos))
            {
                cachedRoadPositions.Remove(pos);
                cachedRoadEdgePositions = null;
                cachedRoadExtensionCells = null;
                cachedRoadAxialCorridor = null;
                cachedCellsWithinDistanceOfRoad = null;
                cachedCellsWithinDistanceMax = -1;
            }
        }

        /// <summary>Return all grid positions containing road. Lazy-rebuilt cache.</summary>
        public List<Vector2Int> GetAllRoadPositions()
        {
            if (!roadCacheDirty && cachedRoadPositions != null)
                return cachedRoadPositions;
            cachedRoadPositions = new List<Vector2Int>();
            for (int x = 0; x < grid.width; x++)
            {
                for (int y = 0; y < grid.height; y++)
                {
                    CityCell c = grid.GetCell(x, y);
                    if (c != null && c.zoneType == Zone.ZoneType.Road)
                        cachedRoadPositions.Add(new Vector2Int(x, y));
                }
            }
            cachedRoadEdgePositions = null;
            cachedRoadExtensionCells = null;
            cachedRoadAxialCorridor = null;
            cachedCellsWithinDistanceOfRoad = null;
            cachedCellsWithinDistanceMax = -1;
            cachedRoadPositionsSet = new HashSet<Vector2Int>(cachedRoadPositions);
            roadCacheDirty = false;
            return cachedRoadPositions;
        }

        /// <summary>Return road positions as HashSet for O(1) Contains lookups.</summary>
        public HashSet<Vector2Int> GetRoadPositionsAsHashSet()
        {
            GetAllRoadPositions();
            return cachedRoadPositionsSet;
        }

        /// <summary>True if cardinal neighbor can accept AUTO road growth: grass, forest, sea-level, or undeveloped light zoning (BUG-47).</summary>
        private bool IsExpandableNeighborForRoadFrontier(int nx, int ny)
        {
            if (nx < 0 || nx >= grid.width || ny < 0 || ny >= grid.height) return false;
            CityCell n = grid.GetCell(nx, ny);
            if (n == null) return false;
            if (n.zoneType == Zone.ZoneType.Grass || n.HasForest() || n.GetCellInstanceHeight() == 0)
                return true;
            return AutoSimulationRoadRules.IsAutoRoadLandCell(grid, nx, ny);
        }

        /// <summary>Return road positions with ≥1 expandable cardinal neighbor (road frontier).</summary>
        public List<Vector2Int> GetRoadEdgePositions()
        {
            if (cachedRoadEdgePositions != null && !roadCacheDirty)
                return cachedRoadEdgePositions;
            List<Vector2Int> all = roadCacheDirty ? GetAllRoadPositions() : (cachedRoadPositions ?? GetAllRoadPositions());
            cachedRoadEdgePositions = new List<Vector2Int>();
            foreach (Vector2Int p in all)
            {
                bool hasExpandableNeighbor = false;
                if (grid.IsValidGridPosition(new Vector2(p.x + 1, p.y)) && IsExpandableNeighborForRoadFrontier(p.x + 1, p.y)) hasExpandableNeighbor = true;
                if (!hasExpandableNeighbor && grid.IsValidGridPosition(new Vector2(p.x - 1, p.y)) && IsExpandableNeighborForRoadFrontier(p.x - 1, p.y)) hasExpandableNeighbor = true;
                if (!hasExpandableNeighbor && grid.IsValidGridPosition(new Vector2(p.x, p.y + 1)) && IsExpandableNeighborForRoadFrontier(p.x, p.y + 1)) hasExpandableNeighbor = true;
                if (!hasExpandableNeighbor && grid.IsValidGridPosition(new Vector2(p.x, p.y - 1)) && IsExpandableNeighborForRoadFrontier(p.x, p.y - 1)) hasExpandableNeighbor = true;
                if (hasExpandableNeighbor)
                    cachedRoadEdgePositions.Add(p);
            }
            return cachedRoadEdgePositions;
        }

    /// <summary>
    /// Return cells one step beyond each road edge in natural extension direction.
    /// AutoZoningManager must not zone these → AutoRoadBuilder can extend roads unblocked.
    /// For each edge E + road neighbor R: extDir = E - R, extCell = E + extDir.
    /// </summary>
        public HashSet<Vector2Int> GetRoadExtensionCells()
        {
            if (cachedRoadExtensionCells != null && !roadCacheDirty)
                return cachedRoadExtensionCells;
            var roadSet = GetRoadPositionsAsHashSet();
            var edges = GetRoadEdgePositions();
            cachedRoadExtensionCells = new HashSet<Vector2Int>();
            foreach (Vector2Int e in edges)
            {
                for (int d = 0; d < 4; d++)
                {
                    int rx = e.x + Dx[d], ry = e.y + Dy[d];
                    if (rx < 0 || rx >= grid.width || ry < 0 || ry >= grid.height) continue;
                    if (!roadSet.Contains(new Vector2Int(rx, ry))) continue;
                    int extDirX = e.x - rx;
                    int extDirY = e.y - ry;
                    int extX = e.x + extDirX;
                    int extY = e.y + extDirY;
                    if (extX < 0 || extX >= grid.width || extY < 0 || extY >= grid.height) continue;
                    CityCell extCell = grid.GetCell(extX, extY);
                    if (extCell == null) continue;
                    bool extOk = extCell.GetCellInstanceHeight() == 0
                        || extCell.zoneType == Zone.ZoneType.Grass
                        || extCell.HasForest()
                        || AutoSimulationRoadRules.IsAutoRoadLandCell(grid, extX, extY);
                    if (!extOk)
                        continue;
                    cachedRoadExtensionCells.Add(new Vector2Int(extX, extY));
                }
            }
            return cachedRoadExtensionCells;
        }

        /// <summary>Dominant road direction at (rx,ry): (1,0), (-1,0), (0,1), or (0,-1). Zero if no road neighbors.</summary>
        private Vector2Int GetRoadDirectionAt(int rx, int ry, HashSet<Vector2Int> roadSet)
        {
            int roadX = 0, roadY = 0;
            for (int d = 0; d < 4; d++)
            {
                int nx = rx + Dx[d], ny = ry + Dy[d];
                if (nx < 0 || nx >= grid.width || ny < 0 || ny >= grid.height) continue;
                if (roadSet.Contains(new Vector2Int(nx, ny)))
                {
                    roadX += Dx[d];
                    roadY += Dy[d];
                }
            }
            if (roadX == 0 && roadY == 0) return Vector2Int.zero;
            if (Mathf.Abs(roadX) >= Mathf.Abs(roadY))
                return new Vector2Int(roadX > 0 ? 1 : -1, 0);
            return new Vector2Int(0, roadY > 0 ? 1 : -1);
        }

        /// <summary>True if vector (dx,dy) from road R to cell P is perpendicular to road direction (lateral).</summary>
        private bool IsLateralDirection(int dx, int dy, Vector2Int roadDir)
        {
            if (roadDir.x == 0 && roadDir.y == 0) return false;
            int dot = dx * roadDir.x + dy * roadDir.y;
            return dot == 0;
        }

        /// <summary>Return cells in axial corridor (extension of road segments). AutoZoningManager must not zone these (BUG-47).
        /// Searches 8-directional for road neighbors but only emits corridor lines in cardinal directions
        /// (|dirX|+|dirY|==1). Diagonal-direction corridors overcapture perpendicular-strip cells and cause zoning gaps.
        /// Inner r2 elbow-projection loop removed: at T/Y/X junctions it emitted corridors in the perpendicular direction,
        /// blocking the j=1 lateral zoning strip on cells adjacent to (but not at) the junction. The direct r-neighbor
        /// scan is sufficient for all valid elbow/endpoint cases.</summary>
        public HashSet<Vector2Int> GetRoadAxialCorridorCells()
        {
            if (cachedRoadAxialCorridor != null && !roadCacheDirty)
                return cachedRoadAxialCorridor;
            var roadSet = GetRoadPositionsAsHashSet();
            var edges = GetRoadEdgePositions();
            cachedRoadAxialCorridor = new HashSet<Vector2Int>();
            foreach (Vector2Int e in edges)
            {
                for (int d = 0; d < 8; d++)
                {
                    int rx = e.x + Dx8[d], ry = e.y + Dy8[d];
                    if (rx < 0 || rx >= grid.width || ry < 0 || ry >= grid.height) continue;
                    var rPos = new Vector2Int(rx, ry);
                    if (!roadSet.Contains(rPos)) continue;
                    int extDirX = e.x - rx;
                    int extDirY = e.y - ry;
                    // Only emit cardinal corridor lines; diagonal dirs overcapture perp-strip zoning cells.
                    if (Mathf.Abs(extDirX) + Mathf.Abs(extDirY) == 1)
                        AddCorridorLine(e.x, e.y, extDirX, extDirY, roadSet);
                }
            }
            return cachedRoadAxialCorridor;
        }

        private void AddCorridorLine(int ex, int ey, int dirX, int dirY, HashSet<Vector2Int> roadSet)
        {
            for (int k = 1; k <= AxialCorridorLength; k++)
            {
                int ax = ex + k * dirX;
                int ay = ey + k * dirY;
                if (ax < 0 || ax >= grid.width || ay < 0 || ay >= grid.height) break;
                if (roadSet.Contains(new Vector2Int(ax, ay))) break;
                cachedRoadAxialCorridor.Add(new Vector2Int(ax, ay));
            }
        }

        /// <summary>Number of cardinal neighbors of (gx,gy) that are roads.</summary>
        public int CountRoadNeighbors(int gx, int gy)
        {
            if (gx < 0 || gx >= grid.width || gy < 0 || gy >= grid.height) return 0;
            var roadSet = GetRoadPositionsAsHashSet();
            if (roadSet == null) return 0;
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                int nx = gx + Dx[i], ny = gy + Dy[i];
                if (nx >= 0 && nx < grid.width && ny >= 0 && ny < grid.height && roadSet.Contains(new Vector2Int(nx, ny)))
                    count++;
            }
            return count;
        }

        /// <summary>Number of cardinal neighbors of (gx,gy) that are zoneable (Grass, Forest, or Flat/N-S/E-W slope).</summary>
        public int CountGrassNeighbors(int gx, int gy)
        {
            if (gx < 0 || gx >= grid.width || gy < 0 || gy >= grid.height) return 0;
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                int nx = gx + Dx[i], ny = gy + Dy[i];
                if (nx >= 0 && nx < grid.width && ny >= 0 && ny < grid.height)
                {
                    CityCell c = grid.GetCell(nx, ny);
                    if (c != null && IsZoneableNeighbor(c, nx, ny))
                        count++;
                }
            }
            return count;
        }

        /// <summary>True if this neighbor cell is valid for zoning (Grass, Forest, or any slope including diagonal and corner).</summary>
        public bool IsZoneableNeighbor(CityCell c, int x, int y)
        {
            if (c == null) return false;
            if (c.zoneType == Zone.ZoneType.Grass || c.HasForest()) return true;
            if (grid.terrainManager == null) return false;
            TerrainSlopeType slope = grid.terrainManager.GetTerrainSlopeTypeAt(x, y);
            return slope == TerrainSlopeType.Flat || slope == TerrainSlopeType.North || slope == TerrainSlopeType.South
                || slope == TerrainSlopeType.East || slope == TerrainSlopeType.West
                || slope == TerrainSlopeType.NorthEast || slope == TerrainSlopeType.NorthWest
                || slope == TerrainSlopeType.SouthEast || slope == TerrainSlopeType.SouthWest
                || slope == TerrainSlopeType.NorthEastUp || slope == TerrainSlopeType.NorthWestUp
                || slope == TerrainSlopeType.SouthEastUp || slope == TerrainSlopeType.SouthWestUp;
        }

        /// <summary>True if at least one of the 4 cardinal neighbors of (x,y) is a road.</summary>
        public bool IsAdjacentToRoad(int x, int y)
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return false;
            for (int i = 0; i < 4; i++)
            {
                int nx = x + Dx[i], ny = y + Dy[i];
                if (nx >= 0 && nx < grid.width && ny >= 0 && ny < grid.height)
                {
                    CityCell c = grid.GetCell(nx, ny);
                    if (c != null && c.zoneType == Zone.ZoneType.Road)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Return all grid cells within maxDistance (Manhattan) of any road. Cached + invalidated on road change.
        /// Excludes road cells. Used for auto-zoning + building spawn eligibility.
        /// </summary>
        public HashSet<Vector2Int> GetCellsWithinDistanceOfRoad(int maxDistance)
        {
            if (cachedCellsWithinDistanceOfRoad != null && cachedCellsWithinDistanceMax == maxDistance)
                return cachedCellsWithinDistanceOfRoad;

            var roads = GetRoadPositionsAsHashSet();
            cachedCellsWithinDistanceOfRoad = new HashSet<Vector2Int>();
            cachedCellsWithinDistanceMax = maxDistance;

            if (roads == null || roads.Count == 0)
                return cachedCellsWithinDistanceOfRoad;

            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<(Vector2Int p, int dist)>();
            foreach (var r in roads)
            {
                queue.Enqueue((r, 0));
                visited.Add(r);
            }

            while (queue.Count > 0)
            {
                var (p, dist) = queue.Dequeue();
                if (dist > 0 && !roads.Contains(p))
                    cachedCellsWithinDistanceOfRoad.Add(p);
                if (dist >= maxDistance)
                    continue;
                for (int i = 0; i < 4; i++)
                {
                    int nx = p.x + Dx[i], ny = p.y + Dy[i];
                    if (nx < 0 || nx >= grid.width || ny < 0 || ny >= grid.height) continue;
                    var n = new Vector2Int(nx, ny);
                    if (visited.Add(n))
                        queue.Enqueue((n, dist + 1));
                }
            }
            return cachedCellsWithinDistanceOfRoad;
        }

        /// <summary>True if (x,y) is within maxDistance (Manhattan) of any road cell.</summary>
        public bool IsWithinDistanceOfRoad(int x, int y, int maxDistance)
        {
            if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) return false;
            return GetCellsWithinDistanceOfRoad(maxDistance).Contains(new Vector2Int(x, y));
        }
    }
}
