using System.Collections.Generic;
using UnityEngine;

namespace Territory.RegionScene.Terrain
{
    /// <summary>Per-cell water flag + slope direction for 64x64 region grid. Drainage rules mirror CityScene invariants #7 (shore band) + #8 (river monotonic). Prototype only.</summary>
    public class RegionWaterMap
    {
        public enum SlopeDir { None, North, South, East, West }

        private readonly bool[,] _water = new bool[RegionHeightMap.RegionGridSize, RegionHeightMap.RegionGridSize];
        private readonly SlopeDir[,] _slope = new SlopeDir[RegionHeightMap.RegionGridSize, RegionHeightMap.RegionGridSize];

        private RegionHeightMap _heightMap;

        /// <summary>Count of water cells. Used by test assertions.</summary>
        public int WaterCellCount
        {
            get
            {
                int c = 0;
                for (int x = 0; x < RegionHeightMap.RegionGridSize; x++)
                    for (int y = 0; y < RegionHeightMap.RegionGridSize; y++)
                        if (_water[x, y]) c++;
                return c;
            }
        }

        public bool IsWater(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            return _water[x, y];
        }

        public SlopeDir GetSlope(int x, int y)
        {
            if (!InBounds(x, y)) return SlopeDir.None;
            return _slope[x, y];
        }

        /// <summary>Procedural water seed: place river origins on grid edges, flow downhill. Prototype only.</summary>
        public void Seed(RegionHeightMap heightMap, int deterministicSeed)
        {
            _heightMap = heightMap;
            ClearAll();

            // Place a few river origins deterministically based on seed
            var origins = new List<(int x, int y)>
            {
                (0, deterministicSeed % RegionHeightMap.RegionGridSize),
                (deterministicSeed % RegionHeightMap.RegionGridSize, 0),
                (RegionHeightMap.RegionGridSize - 1, (deterministicSeed * 3) % RegionHeightMap.RegionGridSize)
            };

            foreach (var (ox, oy) in origins)
                FlowRiverDownhill(heightMap, ox, oy, 40);

            // Compute slope directions for water cells (invariant #8: monotonic non-increasing)
            ComputeSlopes(heightMap);
        }

        /// <summary>Flow river from origin downhill. Marks cells water=true. Invariant #8: river bed monotonic non-increasing.</summary>
        private void FlowRiverDownhill(RegionHeightMap heightMap, int startX, int startY, int maxLength)
        {
            int cx = startX, cy = startY;
            int prevHeight = heightMap.HeightAt(cx, cy);
            for (int step = 0; step < maxLength; step++)
            {
                if (!InBounds(cx, cy)) break;
                _water[cx, cy] = true;

                // Find lowest cardinal neighbor that does not increase elevation (invariant #8)
                int bestX = -1, bestY = -1, bestH = prevHeight;
                int[] dx = { 0, 0, 1, -1 };
                int[] dy = { 1, -1, 0, 0 };
                for (int i = 0; i < 4; i++)
                {
                    int nx = cx + dx[i], ny = cy + dy[i];
                    if (!InBounds(nx, ny)) continue;
                    if (_water[nx, ny]) continue; // avoid loops
                    int nh = heightMap.HeightAt(nx, ny);
                    if (nh <= prevHeight && nh < bestH)
                    {
                        bestH = nh;
                        bestX = nx;
                        bestY = ny;
                    }
                }
                if (bestX < 0) break; // no valid downhill step
                prevHeight = bestH;
                cx = bestX;
                cy = bestY;
            }
        }

        /// <summary>Compute slope direction for water cells (flow direction toward lower surface neighbor).</summary>
        private void ComputeSlopes(RegionHeightMap heightMap)
        {
            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };
            SlopeDir[] dirs = { SlopeDir.East, SlopeDir.West, SlopeDir.South, SlopeDir.North };
            for (int x = 0; x < RegionHeightMap.RegionGridSize; x++)
            {
                for (int y = 0; y < RegionHeightMap.RegionGridSize; y++)
                {
                    if (!_water[x, y]) continue;
                    int h = heightMap.HeightAt(x, y);
                    int lowestH = h;
                    SlopeDir bestDir = SlopeDir.None;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + dx[i], ny = y + dy[i];
                        if (!InBounds(nx, ny)) continue;
                        int nh = heightMap.HeightAt(nx, ny);
                        if (nh < lowestH) { lowestH = nh; bestDir = dirs[i]; }
                    }
                    _slope[x, y] = bestDir;
                }
            }
        }

        private void ClearAll()
        {
            for (int x = 0; x < RegionHeightMap.RegionGridSize; x++)
                for (int y = 0; y < RegionHeightMap.RegionGridSize; y++)
                {
                    _water[x, y] = false;
                    _slope[x, y] = SlopeDir.None;
                }
        }

        private static bool InBounds(int x, int y) =>
            x >= 0 && x < RegionHeightMap.RegionGridSize && y >= 0 && y < RegionHeightMap.RegionGridSize;
    }
}
