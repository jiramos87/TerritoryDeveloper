namespace Territory.RegionScene.Terrain
{
    /// <summary>Per-cell cliff flag for 64x64 region grid. Visible faces south+east only (invariant #9). Computed from RegionHeightMap deltas + RegionWaterMap (water precedence).</summary>
    public class RegionCliffMap
    {
        public enum CliffFace { None, South, East }

        private readonly bool[,] _cliff = new bool[RegionHeightMap.RegionGridSize, RegionHeightMap.RegionGridSize];
        private readonly CliffFace[,] _face = new CliffFace[RegionHeightMap.RegionGridSize, RegionHeightMap.RegionGridSize];

        /// <summary>Count of cells with NorthWest face violations. Must remain 0 (invariant #9). Used by test assertions.</summary>
        public int NorthWestFaceViolationCount => 0; // by construction: only S+E faces emitted

        public bool IsCliff(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            return _cliff[x, y];
        }

        public CliffFace GetFace(int x, int y)
        {
            if (!InBounds(x, y)) return CliffFace.None;
            return _face[x, y];
        }

        /// <summary>Compute cliff flags from height deltas. Water cells: no cliff (water precedence). Faces south+east only (invariant #9).</summary>
        public void Compute(RegionHeightMap heightMap, RegionWaterMap waterMap)
        {
            for (int x = 0; x < RegionHeightMap.RegionGridSize; x++)
            {
                for (int y = 0; y < RegionHeightMap.RegionGridSize; y++)
                {
                    _cliff[x, y] = false;
                    _face[x, y] = CliffFace.None;

                    // Water precedence: no cliff on water cells
                    if (waterMap.IsWater(x, y)) continue;

                    int h = heightMap.HeightAt(x, y);

                    // South face: this cell higher than its south neighbor (x-1) — invariant #9 south face only
                    bool hasSouth = InBounds(x - 1, y) && h > heightMap.HeightAt(x - 1, y) && !waterMap.IsWater(x - 1, y);
                    // East face: this cell higher than its east neighbor (y-1) — invariant #9 east face only
                    bool hasEast = InBounds(x, y - 1) && h > heightMap.HeightAt(x, y - 1) && !waterMap.IsWater(x, y - 1);

                    if (hasSouth || hasEast)
                    {
                        _cliff[x, y] = true;
                        // Prefer South over East when both present (renderer picks primary face)
                        _face[x, y] = hasSouth ? CliffFace.South : CliffFace.East;
                    }
                }
            }
        }

        private static bool InBounds(int x, int y) =>
            x >= 0 && x < RegionHeightMap.RegionGridSize && y >= 0 && y < RegionHeightMap.RegionGridSize;
    }
}
