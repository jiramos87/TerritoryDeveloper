using System.Collections.Generic;
using UnityEngine;

namespace Territory.Terrain
{
    /// <summary>
    /// Shared plateau spill checks used by <see cref="WaterMap"/> depression-fill.
    /// <see cref="TerrainManager"/> uses this to carve minimal bowls so procedural lake budgets can be met.
    /// </summary>
    public static class LakeFeasibility
    {
        /// <summary>Must match <c>WaterMap</c> border treatment for spill height.</summary>
        private const int OutsideMapSpillHeight = 6;

        private static int BorderNeighborHeight(int x, int y, HeightMap hm)
        {
            if (hm.IsValidPosition(x, y))
                return hm.GetHeight(x, y);
            return OutsideMapSpillHeight;
        }

        /// <summary>
        /// Plateau spill height — same algorithm as <c>WaterMap.GetPlateauSpillHeight</c>:
        /// minimum height over cardinal neighbors outside the seed's same-height 4-connected component.
        /// </summary>
        public static int GetPlateauSpillHeight(int x, int y, HeightMap hm)
        {
            int w = hm.Width;
            int h = hm.Height;
            if (!hm.IsValidPosition(x, y))
                return int.MinValue;

            int seedH = hm.GetHeight(x, y);
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            int spill = int.MaxValue;
            var q = new Queue<Vector2Int>();
            var inPlateau = new bool[w, h];
            q.Enqueue(new Vector2Int(x, y));
            inPlateau[x, y] = true;
            int plateauCount = 0;

            while (q.Count > 0)
            {
                var c = q.Dequeue();
                plateauCount++;
                for (int i = 0; i < 4; i++)
                {
                    int nx = c.x + dx[i];
                    int ny = c.y + dy[i];
                    if (!hm.IsValidPosition(nx, ny))
                    {
                        spill = Mathf.Min(spill, BorderNeighborHeight(nx, ny, hm));
                        continue;
                    }
                    if (inPlateau[nx, ny])
                        continue;
                    int nh = hm.GetHeight(nx, ny);
                    if (nh == seedH)
                    {
                        inPlateau[nx, ny] = true;
                        q.Enqueue(new Vector2Int(nx, ny));
                    }
                    else
                    {
                        spill = Mathf.Min(spill, nh);
                    }
                }
            }

            if (plateauCount == w * h)
                return seedH;

            if (spill == int.MaxValue)
                return seedH;
            return spill;
        }

        /// <summary>True if water could sit above the cell floor (spill strictly above terrain height).</summary>
        public static bool PassesSpillTest(int x, int y, HeightMap hm)
        {
            if (!hm.IsValidPosition(x, y))
                return false;
            int spill = GetPlateauSpillHeight(x, y, hm);
            return spill > hm.GetHeight(x, y);
        }

        /// <summary>Counts grid cells where <see cref="PassesSpillTest"/> holds.</summary>
        public static int CountSpillPassingCells(HeightMap hm)
        {
            int w = hm.Width;
            int h = hm.Height;
            int n = 0;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    if (PassesSpillTest(x, y, hm))
                        n++;
                }
            }
            return n;
        }

        /// <summary>
        /// Forces a strict cardinal bowl: center one step above <see cref="TerrainManager.MIN_HEIGHT"/>,
        /// four neighbors at least one step higher — enough for spill &gt; center height.
        /// Only raises neighbor heights (never lowers rims). Requires 1 ≤ x &lt; w-1, 1 ≤ y &lt; h-1.
        /// </summary>
        public static void CarveMinimalCardinalBowl(HeightMap hm, int x, int y)
        {
            int w = hm.Width;
            int h = hm.Height;
            if (x < 1 || y < 1 || x >= w - 1 || y >= h - 1)
                return;

            int center = TerrainManager.MIN_HEIGHT + 1;
            int rim = center + 1;
            hm.SetHeight(x, y, center);
            hm.SetHeight(x + 1, y, Mathf.Max(hm.GetHeight(x + 1, y), rim));
            hm.SetHeight(x - 1, y, Mathf.Max(hm.GetHeight(x - 1, y), rim));
            hm.SetHeight(x, y + 1, Mathf.Max(hm.GetHeight(x, y + 1), rim));
            hm.SetHeight(x, y - 1, Mathf.Max(hm.GetHeight(x, y - 1), rim));
        }
    }
}
