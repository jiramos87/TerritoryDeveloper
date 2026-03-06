using UnityEngine;
using System.Collections.Generic;
using Territory.Zones;
using Territory.Terrain;

namespace Territory.Core
{
    /// <summary>
    /// Lazily-cached index of road positions and road-edge (frontier) positions.
    /// Also provides adjacency and zoneability queries used by auto-zoning and auto-road building.
    /// Extracted from GridManager to reduce its responsibilities.
    /// </summary>
    public class RoadCacheService
    {
        private static readonly int[] Dx = { 1, -1, 0, 0 };
        private static readonly int[] Dy = { 0, 0, 1, -1 };

        private readonly GridManager grid;
        private List<Vector2Int> cachedRoadPositions;
        private HashSet<Vector2Int> cachedRoadPositionsSet;
        private List<Vector2Int> cachedRoadEdgePositions;
        private bool roadCacheDirty = true;

        public RoadCacheService(GridManager grid)
        {
            this.grid = grid;
        }

        /// <summary>
        /// Marks the cached road positions as stale so they are rebuilt on the next query.
        /// </summary>
        public void Invalidate()
        {
            roadCacheDirty = true;
        }

        /// <summary>
        /// Adds a road position to the cache incrementally. Call when a road tile is placed.
        /// </summary>
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
            }
            roadCacheDirty = false;
        }

        /// <summary>
        /// Removes a road position from the cache incrementally. Call when a road tile is demolished.
        /// </summary>
        public void RemoveRoad(Vector2Int pos)
        {
            if (cachedRoadPositionsSet == null) return;
            if (cachedRoadPositionsSet.Remove(pos))
            {
                cachedRoadPositions.Remove(pos);
                cachedRoadEdgePositions = null;
            }
        }

        /// <summary>
        /// Returns all grid positions that contain a road, using a lazily rebuilt cache.
        /// </summary>
        public List<Vector2Int> GetAllRoadPositions()
        {
            if (!roadCacheDirty && cachedRoadPositions != null)
                return cachedRoadPositions;
            cachedRoadPositions = new List<Vector2Int>();
            for (int x = 0; x < grid.width; x++)
            {
                for (int y = 0; y < grid.height; y++)
                {
                    Cell c = grid.GetCell(x, y);
                    if (c != null && c.zoneType == Zone.ZoneType.Road)
                        cachedRoadPositions.Add(new Vector2Int(x, y));
                }
            }
            cachedRoadEdgePositions = null;
            cachedRoadPositionsSet = new HashSet<Vector2Int>(cachedRoadPositions);
            roadCacheDirty = false;
            return cachedRoadPositions;
        }

        /// <summary>
        /// Returns road positions as a HashSet for O(1) Contains lookups.
        /// </summary>
        public HashSet<Vector2Int> GetRoadPositionsAsHashSet()
        {
            GetAllRoadPositions();
            return cachedRoadPositionsSet;
        }

        /// <summary>
        /// Returns road positions that have at least one expandable (grass/forest/sea-level) cardinal neighbor, i.e. the road frontier.
        /// </summary>
        public List<Vector2Int> GetRoadEdgePositions()
        {
            if (cachedRoadEdgePositions != null && !roadCacheDirty)
                return cachedRoadEdgePositions;
            List<Vector2Int> all = roadCacheDirty ? GetAllRoadPositions() : (cachedRoadPositions ?? GetAllRoadPositions());
            cachedRoadEdgePositions = new List<Vector2Int>();
            foreach (Vector2Int p in all)
            {
                bool hasExpandableNeighbor = false;
                if (grid.IsValidGridPosition(new Vector2(p.x + 1, p.y))) { Cell n = grid.GetCell(p.x + 1, p.y); if (n != null && (n.zoneType == Zone.ZoneType.Grass || n.HasForest() || n.GetCellInstanceHeight() == 0)) hasExpandableNeighbor = true; }
                if (!hasExpandableNeighbor && grid.IsValidGridPosition(new Vector2(p.x - 1, p.y))) { Cell n = grid.GetCell(p.x - 1, p.y); if (n != null && (n.zoneType == Zone.ZoneType.Grass || n.HasForest() || n.GetCellInstanceHeight() == 0)) hasExpandableNeighbor = true; }
                if (!hasExpandableNeighbor && grid.IsValidGridPosition(new Vector2(p.x, p.y + 1))) { Cell n = grid.GetCell(p.x, p.y + 1); if (n != null && (n.zoneType == Zone.ZoneType.Grass || n.HasForest() || n.GetCellInstanceHeight() == 0)) hasExpandableNeighbor = true; }
                if (!hasExpandableNeighbor && grid.IsValidGridPosition(new Vector2(p.x, p.y - 1))) { Cell n = grid.GetCell(p.x, p.y - 1); if (n != null && (n.zoneType == Zone.ZoneType.Grass || n.HasForest() || n.GetCellInstanceHeight() == 0)) hasExpandableNeighbor = true; }
                if (hasExpandableNeighbor)
                    cachedRoadEdgePositions.Add(p);
            }
            return cachedRoadEdgePositions;
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
                    Cell c = grid.GetCell(nx, ny);
                    if (c != null && IsZoneableNeighbor(c, nx, ny))
                        count++;
                }
            }
            return count;
        }

        /// <summary>True if this neighbor cell is valid for zoning (Grass, Forest, or Flat/N-S/E-W slope).</summary>
        public bool IsZoneableNeighbor(Cell c, int x, int y)
        {
            if (c == null) return false;
            if (c.zoneType == Zone.ZoneType.Grass || c.HasForest()) return true;
            if (grid.terrainManager == null) return false;
            TerrainSlopeType slope = grid.terrainManager.GetTerrainSlopeTypeAt(x, y);
            return slope == TerrainSlopeType.Flat || slope == TerrainSlopeType.North || slope == TerrainSlopeType.South || slope == TerrainSlopeType.East || slope == TerrainSlopeType.West;
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
                    Cell c = grid.GetCell(nx, ny);
                    if (c != null && c.zoneType == Zone.ZoneType.Road)
                        return true;
                }
            }
            return false;
        }
    }
}
