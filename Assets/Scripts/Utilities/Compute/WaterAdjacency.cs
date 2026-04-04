namespace Territory.Utilities.Compute
{
    /// <summary>
    /// Read-only open-water queries for predicate helpers (TECH-39 §7.11.5). Use <see cref="TerrainOpenWaterMapView"/> from gameplay code.
    /// </summary>
    public interface IOpenWaterMapView
    {
        bool IsValidGridPosition(int x, int y);
        /// <summary>Registered open water per <see cref="Territory.Terrain.TerrainManager.IsRegisteredOpenWaterAt"/> (water map authority).</summary>
        bool IsRegisteredOpenWaterAt(int x, int y);
    }

    /// <summary>
    /// Moore-neighborhood (8) predicates relative to geography / water topology helpers.
    /// </summary>
    public static class WaterAdjacency
    {
        private static readonly int[] MooreDx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] MooreDy = { 0, 0, 1, -1, 1, -1, 1, -1 };

        /// <summary>True if any Moore neighbor of <paramref name="x"/>,<paramref name="y"/> is registered open water.</summary>
        public static bool IsMooreAdjacentToOpenWater(int x, int y, IOpenWaterMapView view)
        {
            if (view == null)
                return false;
            for (int i = 0; i < MooreDx.Length; i++)
            {
                int nx = x + MooreDx[i];
                int ny = y + MooreDy[i];
                if (!view.IsValidGridPosition(nx, ny))
                    continue;
                if (view.IsRegisteredOpenWaterAt(nx, ny))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// <see cref="IOpenWaterMapView"/> over <see cref="Territory.Terrain.TerrainManager"/> (same assembly; no extra manager logic).
    /// </summary>
    public readonly struct TerrainOpenWaterMapView : IOpenWaterMapView
    {
        private readonly Territory.Terrain.TerrainManager _terrain;

        public TerrainOpenWaterMapView(Territory.Terrain.TerrainManager terrain)
        {
            _terrain = terrain;
        }

        public bool IsValidGridPosition(int x, int y)
        {
            if (_terrain == null)
                return false;
            var hm = _terrain.GetHeightMap();
            return hm != null && hm.IsValidPosition(x, y);
        }

        public bool IsRegisteredOpenWaterAt(int x, int y)
        {
            return _terrain != null && _terrain.IsRegisteredOpenWaterAt(x, y);
        }
    }
}
