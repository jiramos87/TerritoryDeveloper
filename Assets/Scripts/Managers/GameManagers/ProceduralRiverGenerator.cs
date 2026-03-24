using System.Collections.Generic;
using Territory.Core;
using UnityEngine;

namespace Territory.Terrain
{
    /// <summary>
    /// FEAT-38: procedural static rivers after lake/sea init. Only assigns dry cells; never modifies existing water bodies.
    /// Cross-stream <b>bed</b> width (lecho) is 1–3 cells of <b>water</b>; corridor width = bed + 2 (one dry shore strip per side) for terrain refresh and collision with <see cref="RiverBorderMargin"/>.
    /// Each cross-section gets one shared bed height and symmetric bank height; water bodies are split when surface height changes along the path (see <c>rivers.md</c> §4.3).
    /// Bed floor <c>H_bed</c> is <b>non-increasing</b> along the centerline from map entry toward exit so the river never climbs terrain (see <c>rivers.md</c> §4.4).
    /// Centerline and footprint avoid map borders except at designated entry/exit edges (see <see cref="RiverBorderMargin"/>).
    /// </summary>
    public static class ProceduralRiverGenerator
    {
        private const int MinRiverBedWidth = 1;
        private const int MaxRiverBedWidth = 3;

        /// <summary>
        /// Minimum distance from centerline to the perpendicular map edges so cross-section (max total 5) fits.
        /// Interior centerline never sits on east/west (N–S flow) or north/south (E–W flow) borders.
        /// </summary>
        private const int RiverBorderMargin = 2;

        private static readonly int[] D4x = { 0, 0, 1, -1 };
        private static readonly int[] D4y = { 1, -1, 0, 0 };

        public static void Generate(WaterManager waterManager, TerrainManager terrainManager, GridManager gridManager, System.Random rnd)
        {
            WaterMap wm = waterManager.GetWaterMap();
            HeightMap hm = terrainManager.GetHeightMap();
            if (wm == null || hm == null || gridManager == null)
                return;

            int gw = gridManager.width;
            int gh = gridManager.height;
            if (!CanPlaceRiversWithMargin(gw, gh))
                return;

            int maxL = Mathf.Max(1, Mathf.RoundToInt(1.5f * Mathf.Max(gw, gh)));

            int riverCount = rnd.Next(1, 4);
            var used = new HashSet<Vector2Int>();

            for (int r = 0; r < riverCount; r++)
            {
                bool nsAxis = rnd.Next(0, 2) == 0;
                List<Vector2Int> centerline = TryBuildCenterline(wm, hm, gw, gh, maxL, nsAxis, rnd, used);
                if (centerline == null || centerline.Count < 2)
                    centerline = BuildForcedCenterline(wm, gw, gh, nsAxis, rnd);

                if (centerline == null || centerline.Count < 2)
                    continue;

                int bedWidth = MinRiverBedWidth;
                int stepsSinceWidthChange = 0;
                var corridorFootprint = new HashSet<Vector2Int>();
                var waterFootprint = new HashSet<Vector2Int>();
                var crossSections = new List<RiverCrossSectionData>();
                int pathLen = centerline.Count;
                for (int i = 0; i < pathLen; i++)
                {
                    stepsSinceWidthChange++;
                    if (stepsSinceWidthChange >= 4 && rnd.NextDouble() < 0.3 && bedWidth < MaxRiverBedWidth)
                    {
                        bedWidth++;
                        stepsSinceWidthChange = 0;
                    }

                    Vector2Int prev = i > 0 ? centerline[i - 1] : centerline[i];
                    Vector2Int cur = centerline[i];
                    Vector2Int next = i + 1 < pathLen ? centerline[i + 1] : cur + (centerline[i] - centerline[i - 1]);
                    RiverCrossSectionData sec = BuildCrossSection(wm, gw, gh, prev, cur, next, bedWidth, nsAxis, i, pathLen);
                    crossSections.Add(sec);
                    foreach (Vector2Int p in sec.Bed)
                        waterFootprint.Add(p);
                    foreach (Vector2Int p in sec.AllCorridorCells())
                        corridorFootprint.Add(p);
                }

                foreach (var p in corridorFootprint)
                    used.Add(p);

                ApplyCrossSectionHeights(wm, hm, crossSections);

                int lastSurface = int.MinValue;
                int currentBodyId = -1;
                foreach (RiverCrossSectionData sec in crossSections)
                {
                    if (sec.Bed.Count == 0 || sec.AppliedBedHeight < 0)
                        continue;
                    int surface = Mathf.Min(TerrainManager.MAX_HEIGHT, sec.AppliedBedHeight + 1);
                    if (surface != lastSurface || currentBodyId < 0)
                    {
                        currentBodyId = wm.CreateRiverWaterBody(surface);
                        lastSurface = surface;
                    }

                    foreach (Vector2Int p in sec.Bed)
                    {
                        if (wm.IsWater(p.x, p.y))
                            continue;
                        wm.TryAssignCellToRiverBody(p.x, p.y, currentBodyId);
                    }
                }

                wm.MergeAdjacentBodiesAfterRiverPlacement();

                if (corridorFootprint.Count > 0)
                {
                    GetFootprintBounds(corridorFootprint, gw, gh, out int bx0, out int by0, out int bx1, out int by1);
                    terrainManager.ApplyHeightMapToRegion(bx0, by0, bx1, by1);
                }
            }
        }

        private static bool CanPlaceRiversWithMargin(int gw, int gh)
        {
            return gw >= 2 * RiverBorderMargin + 1 && gh >= 2 * RiverBorderMargin + 1;
        }

        private static void GetFootprintBounds(HashSet<Vector2Int> footprint, int gw, int gh, out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = gw;
            minY = gh;
            maxX = 0;
            maxY = 0;
            foreach (var p in footprint)
            {
                if (p.x < minX) minX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.x > maxX) maxX = p.x;
                if (p.y > maxY) maxY = p.y;
            }
            minX = Mathf.Max(0, minX - 2);
            minY = Mathf.Max(0, minY - 2);
            maxX = Mathf.Min(gw - 1, maxX + 2);
            maxY = Mathf.Min(gh - 1, maxY + 2);
        }

        /// <summary>One perpendicular strip: left shore, bed cells, right shore (see <c>rivers.md</c> §4.3).</summary>
        private sealed class RiverCrossSectionData
        {
            public readonly List<Vector2Int> Bed = new List<Vector2Int>(MaxRiverBedWidth);
            public Vector2Int LeftShore;
            public Vector2Int RightShore;
            public bool HasLeft;
            public bool HasRight;
            /// <summary>HeightMap floor under water after carve; <c>-1</c> if section skipped.</summary>
            public int AppliedBedHeight = -1;
            /// <summary>Shallow carve target before longitudinal clamp; <c>-1</c> if section has no bed.</summary>
            public int CandidateBedHeight = -1;

            public IEnumerable<Vector2Int> AllCorridorCells()
            {
                if (HasLeft)
                    yield return LeftShore;
                foreach (Vector2Int p in Bed)
                    yield return p;
                if (HasRight)
                    yield return RightShore;
            }
        }

        /// <param name="segmentIndex">Index along centerline (entry/exit allow border footprint only here).</param>
        private static RiverCrossSectionData BuildCrossSection(WaterMap wm, int gw, int gh, Vector2Int prev, Vector2Int cur, Vector2Int next, int bedWidth, bool flowIsNorthSouth, int segmentIndex, int pathLength)
        {
            var sec = new RiverCrossSectionData();
            bedWidth = Mathf.Clamp(bedWidth, MinRiverBedWidth, MaxRiverBedWidth);
            int total = bedWidth + 2;
            int left = -(total / 2);
            int right = (total - 1) / 2;
            int bedLeft = left + 1;
            int bedRight = right - 1;

            Vector2Int dir = next - cur;
            if (dir.x == 0 && dir.y == 0)
                dir = cur - prev;
            if (dir.x == 0 && dir.y == 0)
                dir = Vector2Int.down;

            bool widthAlongX = Mathf.Abs(dir.y) >= Mathf.Abs(dir.x);
            bool isFirstSegment = segmentIndex == 0;
            bool isLastSegment = segmentIndex == pathLength - 1;

            for (int d = left; d <= right; d++)
            {
                int wx = widthAlongX ? cur.x + d : cur.x;
                int wy = widthAlongX ? cur.y : cur.y + d;
                if (wx < 0 || wx >= gw || wy < 0 || wy >= gh)
                    continue;
                if (!IsFootprintCellAllowedOnBorder(wx, wy, gw, gh, flowIsNorthSouth, isFirstSegment, isLastSegment))
                    continue;
                if (wm.IsWater(wx, wy))
                    continue;
                var cell = new Vector2Int(wx, wy);
                if (d >= bedLeft && d <= bedRight)
                    sec.Bed.Add(cell);
                else if (d == left)
                {
                    sec.LeftShore = cell;
                    sec.HasLeft = true;
                }
                else if (d == right)
                {
                    sec.RightShore = cell;
                    sec.HasRight = true;
                }
            }

            return sec;
        }

        /// <summary>
        /// Shallow carve candidate per section, then <b>longitudinal</b> clamp: <c>H_bed[i] = min(candidate[i], H_bed[i-1])</c> from entry to exit (see <c>rivers.md</c> §4.4).
        /// Finally writes bed and symmetric bank heights.
        /// </summary>
        private static void ApplyCrossSectionHeights(WaterMap wm, HeightMap hm, List<RiverCrossSectionData> sections)
        {
            foreach (RiverCrossSectionData sec in sections)
            {
                sec.CandidateBedHeight = -1;
                sec.AppliedBedHeight = -1;
                if (sec.Bed.Count == 0)
                    continue;

                int minH = int.MaxValue;
                foreach (Vector2Int p in sec.Bed)
                {
                    if (wm.IsWater(p.x, p.y))
                        continue;
                    minH = Mathf.Min(minH, hm.GetHeight(p.x, p.y));
                }

                if (minH == int.MaxValue)
                    continue;

                int candidate = Mathf.Max(TerrainManager.MIN_HEIGHT, minH - Mathf.Min(2, minH - TerrainManager.MIN_HEIGHT));
                sec.CandidateBedHeight = candidate;
            }

            int prevBed = int.MaxValue;
            foreach (RiverCrossSectionData sec in sections)
            {
                if (sec.CandidateBedHeight < 0)
                    continue;
                int hBed = Mathf.Min(sec.CandidateBedHeight, prevBed);
                sec.AppliedBedHeight = hBed;
                prevBed = hBed;
            }

            foreach (RiverCrossSectionData sec in sections)
            {
                if (sec.AppliedBedHeight < 0)
                    continue;

                int hBed = sec.AppliedBedHeight;
                foreach (Vector2Int p in sec.Bed)
                {
                    if (!wm.IsWater(p.x, p.y))
                        hm.SetHeight(p.x, p.y, hBed);
                }

                if (hBed >= TerrainManager.MAX_HEIGHT)
                    continue;

                int bankH = hBed + 1;
                if (sec.HasLeft && !wm.IsWater(sec.LeftShore.x, sec.LeftShore.y))
                    hm.SetHeight(sec.LeftShore.x, sec.LeftShore.y, bankH);
                if (sec.HasRight && !wm.IsWater(sec.RightShore.x, sec.RightShore.y))
                    hm.SetHeight(sec.RightShore.x, sec.RightShore.y, bankH);
            }
        }

        /// <summary>
        /// N–S flow: never west/east map edges; north row only on first segment; south row only on last.
        /// E–W flow: never north/south map edges; west column only on first segment; east column only on last.
        /// </summary>
        private static bool IsFootprintCellAllowedOnBorder(int wx, int wy, int gw, int gh, bool flowIsNorthSouth, bool isFirstSegment, bool isLastSegment)
        {
            if (flowIsNorthSouth)
            {
                if (wx == 0 || wx == gw - 1)
                    return false;
                if (wy == 0 && !isFirstSegment)
                    return false;
                if (wy == gh - 1 && !isLastSegment)
                    return false;
                return true;
            }

            if (wy == 0 || wy == gh - 1)
                return false;
            if (wx == 0 && !isFirstSegment)
                return false;
            if (wx == gw - 1 && !isLastSegment)
                return false;
            return true;
        }

        private static List<Vector2Int> TryBuildCenterline(WaterMap wm, HeightMap hm, int gw, int gh, int maxL, bool nsAxis, System.Random rnd, HashSet<Vector2Int> avoid)
        {
            if (nsAxis)
                return TryBfsEdgeToEdge(wm, hm, gw, gh, maxL, true, rnd, avoid);
            return TryBfsEdgeToEdge(wm, hm, gw, gh, maxL, false, rnd, avoid);
        }

        private static List<Vector2Int> TryBfsEdgeToEdge(WaterMap wm, HeightMap hm, int gw, int gh, int maxL, bool northSouth, System.Random rnd, HashSet<Vector2Int> avoid)
        {
            int m = RiverBorderMargin;
            Vector2Int start = FindDryBorderStart(wm, gw, gh, northSouth, rnd);
            if (start.x < 0)
                return null;

            var goals = new HashSet<Vector2Int>();
            if (northSouth)
            {
                for (int x = m; x <= gw - 1 - m; x++)
                {
                    if (!wm.IsWater(x, gh - 1))
                        goals.Add(new Vector2Int(x, gh - 1));
                }
            }
            else
            {
                for (int y = m; y <= gh - 1 - m; y++)
                {
                    if (!wm.IsWater(gw - 1, y))
                        goals.Add(new Vector2Int(gw - 1, y));
                }
            }

            if (goals.Count == 0)
                return null;

            var q = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var dist = new Dictionary<Vector2Int, int>();
            q.Enqueue(start);
            dist[start] = 0;

            while (q.Count > 0)
            {
                Vector2Int p = q.Dequeue();
                if (goals.Contains(p) && dist[p] >= 3)
                    return ReconstructPath(cameFrom, start, p);
                if (dist[p] >= maxL)
                    continue;

                if (northSouth && p.y == 0 && p == start)
                {
                    TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, start, goals, p, p.x, p.y + 1, avoid, q, cameFrom, dist);
                    continue;
                }

                if (!northSouth && p.x == 0 && p == start)
                {
                    TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, start, goals, p, p.x + 1, p.y, avoid, q, cameFrom, dist);
                    continue;
                }

                int[] ord = { 0, 1, 2, 3 };
                ShuffleOrder(rnd, ord);
                for (int k = 0; k < 4; k++)
                {
                    int i = ord[k];
                    int nx = p.x + D4x[i];
                    int ny = p.y + D4y[i];
                    TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, start, goals, p, nx, ny, avoid, q, cameFrom, dist);
                }
            }

            return null;
        }

        private static void TryEnqueueRiverNeighbor(
            WaterMap wm, HeightMap hm, int gw, int gh, int margin, bool northSouth, Vector2Int start, HashSet<Vector2Int> goals,
            Vector2Int p, int nx, int ny, HashSet<Vector2Int> avoid, Queue<Vector2Int> q, Dictionary<Vector2Int, Vector2Int> cameFrom, Dictionary<Vector2Int, int> dist)
        {
            if (nx < 0 || nx >= gw || ny < 0 || ny >= gh)
                return;

            if (northSouth)
            {
                if (nx < margin || nx > gw - 1 - margin)
                    return;
                if (ny == 0)
                    return;
                if (ny == gh - 1 && !goals.Contains(new Vector2Int(nx, ny)))
                    return;
            }
            else
            {
                if (ny < margin || ny > gh - 1 - margin)
                    return;
                if (nx == 0)
                    return;
                if (nx == gw - 1 && !goals.Contains(new Vector2Int(nx, ny)))
                    return;
            }

            Vector2Int np = new Vector2Int(nx, ny);
            if (avoid.Contains(np))
                return;
            if (wm.IsWater(nx, ny))
                return;
            if (!CardinalStepHeightOk(hm, p.x, p.y, nx, ny))
                return;
            if (dist.ContainsKey(np))
                return;
            cameFrom[np] = p;
            dist[np] = dist[p] + 1;
            q.Enqueue(np);
        }

        private static void ShuffleOrder(System.Random rnd, int[] arr)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                int t = arr[i];
                arr[i] = arr[j];
                arr[j] = t;
            }
        }

        /// <summary>Returns <c>(-1,-1)</c> if no valid dry start in the margin band.</summary>
        private static Vector2Int FindDryBorderStart(WaterMap wm, int gw, int gh, bool northSouth, System.Random rnd)
        {
            int m = RiverBorderMargin;
            if (northSouth)
            {
                if (gw <= 2 * m)
                    return new Vector2Int(-1, -1);
                for (int t = 0; t < 120; t++)
                {
                    int x = rnd.Next(m, gw - m);
                    if (!wm.IsWater(x, 0))
                        return new Vector2Int(x, 0);
                }
                for (int x = m; x <= gw - 1 - m; x++)
                {
                    if (!wm.IsWater(x, 0))
                        return new Vector2Int(x, 0);
                }
            }
            else
            {
                if (gh <= 2 * m)
                    return new Vector2Int(-1, -1);
                for (int t = 0; t < 120; t++)
                {
                    int y = rnd.Next(m, gh - m);
                    if (!wm.IsWater(0, y))
                        return new Vector2Int(0, y);
                }
                for (int y = m; y <= gh - 1 - m; y++)
                {
                    if (!wm.IsWater(0, y))
                        return new Vector2Int(0, y);
                }
            }

            return new Vector2Int(-1, -1);
        }

        private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int goal)
        {
            var path = new List<Vector2Int>();
            Vector2Int cur = goal;
            while (cur != start)
            {
                path.Add(cur);
                cur = cameFrom[cur];
            }

            path.Add(start);
            path.Reverse();
            return path;
        }

        private static bool CardinalStepHeightOk(HeightMap hm, int x0, int y0, int x1, int y1)
        {
            int h0 = hm.GetHeight(x0, y0);
            int h1 = hm.GetHeight(x1, y1);
            return Mathf.Abs(h0 - h1) <= 1;
        }

        private static List<Vector2Int> BuildForcedCenterline(WaterMap wm, int gw, int gh, bool nsAxis, System.Random rnd)
        {
            int m = RiverBorderMargin;
            var path = new List<Vector2Int>();
            if (nsAxis)
            {
                int x = Mathf.Clamp(gw / 2, m, gw - 1 - m);
                for (int y = 0; y < gh; y++)
                {
                    int attempts = 0;
                    while (wm.IsWater(x, y) && attempts < gw)
                    {
                        x++;
                        if (x > gw - 1 - m)
                            x = m;
                        attempts++;
                    }

                    if (wm.IsWater(x, y))
                    {
                        for (int xx = m; xx <= gw - 1 - m; xx++)
                        {
                            if (!wm.IsWater(xx, y))
                            {
                                x = xx;
                                break;
                            }
                        }
                    }

                    x = Mathf.Clamp(x, m, gw - 1 - m);
                    path.Add(new Vector2Int(x, y));
                }
            }
            else
            {
                int y = Mathf.Clamp(gh / 2, m, gh - 1 - m);
                for (int x = 0; x < gw; x++)
                {
                    int attempts = 0;
                    while (wm.IsWater(x, y) && attempts < gh)
                    {
                        y++;
                        if (y > gh - 1 - m)
                            y = m;
                        attempts++;
                    }

                    if (wm.IsWater(x, y))
                    {
                        for (int yy = m; yy <= gh - 1 - m; yy++)
                        {
                            if (!wm.IsWater(x, yy))
                            {
                                y = yy;
                                break;
                            }
                        }
                    }

                    y = Mathf.Clamp(y, m, gh - 1 - m);
                    path.Add(new Vector2Int(x, y));
                }
            }

            return path;
        }
    }
}
