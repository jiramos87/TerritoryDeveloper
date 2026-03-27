using System.Collections.Generic;
using System.Text;
using Territory.Core;
using UnityEngine;

namespace Territory.Terrain
{
    /// <summary>
    /// Debug / QA: straight grid West→East test river (four equal-length surface segments S=4,3,2,1), flat beds per segment.
    /// Centerline is fixed grid <c>x</c> and decreasing <c>y</c> from west toward east (canonical spec: +y = West, East neighbor is (x, y−1)).
    /// Runs after standard lakes and procedural rivers when enabled from <see cref="Territory.Geography.GeographyManager"/>.
    /// Does not run inner-corner shore promotion (that step was lifting forced bed heights and blocking water assignment on S=3 and S=1 segments).
    /// Full grid span on <c>y</c> (west → east); only <c>x</c> is kept in [10, width−10] for corridor width. Post-gen refresh uses <see cref="WaterManager.UpdateWaterVisuals"/> with Pass A/B skipped so §12.7 junction merge does not collapse multi-surface segments.
    /// </summary>
    public static class TestRiverGenerator
    {
        private const int MinBedWidth = 1;
        private const int MaxBedWidth = 3;
        private static readonly int[] DefaultSegmentBedWidths = { 1, 2, 3, 2 };

        /// <summary>Surfaces S=4,3,2,1 → bed heights 3,2,1,0.</summary>
        private static readonly int[] SegmentTargetBedHeights = { 3, 2, 1, 0 };

        /// <summary>
        /// Carves terrain, assigns river bodies, and logs parameters. Caller runs <see cref="WaterMap.MergeAdjacentBodiesWithSameSurface"/>
        /// and <see cref="WaterManager.UpdateWaterVisuals"/> after this returns.
        /// </summary>
        public static void Generate(
            WaterManager waterManager,
            TerrainManager terrainManager,
            GridManager gridManager,
            System.Random rnd,
            IReadOnlyList<int> segmentBedWidths = null)
        {
            if (waterManager == null || terrainManager == null || gridManager == null || rnd == null)
                return;
            WaterMap wm = waterManager.GetWaterMap();
            HeightMap hm = terrainManager.GetHeightMap();
            if (wm == null || hm == null)
                return;

            int gw = gridManager.width;
            int gh = gridManager.height;
            if (gw < 21 || gh < 4)
            {
                Debug.LogWarning("TestRiverGenerator: Map too small (need width ≥ 21 for column margin, height ≥ 4 for four segments). Skipped.");
                return;
            }

            int[] widthsRaw = NormalizeWidths(segmentBedWidths);
            var widthsClamped = new int[4];
            for (int i = 0; i < 4; i++)
                widthsClamped[i] = Mathf.Clamp(widthsRaw[i], MinBedWidth, MaxBedWidth);

            int minXR = 10;
            int maxXR = gw - 10;
            int riverX = rnd.Next(minXR, maxXR + 1);

            int yWest = gh - 1;
            int yEast = 0;
            int travelCount = gh;
            ComputeSegmentIndexRanges(travelCount, out int[] segStart, out int[] segEnd);

            var sections = new List<TestCrossSection>();
            var corridorFootprint = new HashSet<Vector2Int>();
            var waterFootprint = new HashSet<Vector2Int>();

            for (int t = 0; t < travelCount; t++)
            {
                int y = yWest - t;
                int seg = SegmentIndexForTravelIndex(t, segStart, segEnd);
                int bedW = widthsClamped[seg];

                Vector2Int cur = new Vector2Int(riverX, y);
                Vector2Int prev = y < gh - 1 ? new Vector2Int(riverX, y + 1) : cur;
                Vector2Int next = y > 0 ? new Vector2Int(riverX, y - 1) : cur;
                bool isFirst = t == 0;
                bool isLast = t == travelCount - 1;

                TestCrossSection sec = BuildWestToEastCrossSection(wm, gw, gh, prev, cur, next, bedW, isFirst, isLast);
                sec.SegmentIndex = seg;
                sec.CenterGridX = riverX;
                sec.CenterGridY = y;
                sections.Add(sec);
                foreach (Vector2Int p in sec.Bed)
                    waterFootprint.Add(p);
                foreach (Vector2Int p in sec.AllCorridorCells())
                    corridorFootprint.Add(p);
            }

            var riverBedCarvedCells = new HashSet<Vector2Int>();
            ApplyForcedSegmentHeights(wm, hm, sections, riverBedCarvedCells);

            int lastSurface = int.MinValue;
            int currentBodyId = -1;
            var bodyIdsPerSegment = new int[4];
            for (int si = 0; si < 4; si++)
                bodyIdsPerSegment[si] = -1;

            foreach (TestCrossSection sec in sections)
            {
                if (sec.Bed.Count == 0 || sec.AppliedBedHeight < 0)
                    continue;
                int surface = Mathf.Min(TerrainManager.MAX_HEIGHT, sec.AppliedBedHeight + 1);
                if (surface != lastSurface || currentBodyId < 0)
                {
                    currentBodyId = wm.CreateRiverWaterBody(surface);
                    lastSurface = surface;
                    bodyIdsPerSegment[sec.SegmentIndex] = currentBodyId;
                }

                foreach (Vector2Int p in sec.Bed)
                {
                    if (hm.GetHeight(p.x, p.y) != sec.AppliedBedHeight)
                        continue;
                    if (wm.IsWater(p.x, p.y))
                        wm.TryReassignCellFromAnyWaterToRiverBody(p.x, p.y, currentBodyId);
                    else
                        wm.TryAssignCellToRiverBody(p.x, p.y, currentBodyId);
                }
            }

            if (corridorFootprint.Count > 0)
            {
                GetFootprintBounds(corridorFootprint, gw, gh, out int bx0, out int by0, out int bx1, out int by1);
                terrainManager.ApplyHeightMapToRegion(bx0, by0, bx1, by1);
            }

            LogTestRiverSummary(
                gw, gh, riverX, yWest, yEast, segStart, segEnd, widthsRaw, widthsClamped, bodyIdsPerSegment, corridorFootprint.Count);
        }

        private static int[] NormalizeWidths(IReadOnlyList<int> segmentBedWidths)
        {
            var w = new int[4];
            if (segmentBedWidths == null || segmentBedWidths.Count < 4)
            {
                for (int i = 0; i < 4; i++)
                    w[i] = DefaultSegmentBedWidths[i];
                return w;
            }

            for (int i = 0; i < 4; i++)
                w[i] = segmentBedWidths[i];
            return w;
        }

        /// <summary>Four contiguous travel-index ranges [0, count−1] with equal length (remainder to first segments).</summary>
        private static void ComputeSegmentIndexRanges(int count, out int[] segStart, out int[] segEnd)
        {
            segStart = new int[4];
            segEnd = new int[4];
            int baseLen = count / 4;
            int rem = count % 4;
            int pos = 0;
            for (int i = 0; i < 4; i++)
            {
                int len = baseLen + (i < rem ? 1 : 0);
                segStart[i] = pos;
                segEnd[i] = pos + len - 1;
                pos += len;
            }
        }

        private static int SegmentIndexForTravelIndex(int t, int[] segStart, int[] segEnd)
        {
            for (int i = 0; i < 4; i++)
            {
                if (t >= segStart[i] && t <= segEnd[i])
                    return i;
            }

            return 3;
        }

        private sealed class TestCrossSection
        {
            public readonly List<Vector2Int> Bed = new List<Vector2Int>(4);
            public Vector2Int LeftShore;
            public Vector2Int RightShore;
            public bool HasLeft;
            public bool HasRight;
            public int AppliedBedHeight = -1;
            public int SegmentIndex;
            public int CenterGridX;
            public int CenterGridY;

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

        /// <summary>
        /// Flow along decreasing y (East); perpendicular strip varies x. Uses N–S footprint rules (path axis along y) like FEAT-38 N–S rivers.
        /// </summary>
        private static TestCrossSection BuildWestToEastCrossSection(
            WaterMap wm,
            int gw,
            int gh,
            Vector2Int prev,
            Vector2Int cur,
            Vector2Int next,
            int bedWidth,
            bool isFirstSegment,
            bool isLastSegment)
        {
            var sec = new TestCrossSection();
            bedWidth = Mathf.Clamp(bedWidth, MinBedWidth, MaxBedWidth);
            int total = bedWidth + 2;
            int left = -(total / 2);
            int right = (total - 1) / 2;
            int bedLeft = left + 1;
            int bedRight = right - 1;

            Vector2Int dir = next - cur;
            if (dir.x == 0 && dir.y == 0)
                dir = cur - prev;
            if (dir.x == 0 && dir.y == 0)
                dir = new Vector2Int(0, -1);

            bool widthAlongX = Mathf.Abs(dir.y) >= Mathf.Abs(dir.x);
            for (int d = left; d <= right; d++)
            {
                int wx = widthAlongX ? cur.x + d : cur.x;
                int wy = widthAlongX ? cur.y : cur.y + d;
                if (wx < 0 || wx >= gw || wy < 0 || wy >= gh)
                    continue;
                if (!IsFootprintCellAllowedOnBorder(wx, wy, gw, gh, true, true, isFirstSegment, isLastSegment, relaxNsEndpointRows: true))
                    continue;
                var cell = new Vector2Int(wx, wy);
                if (wm.IsWater(wx, wy))
                {
                    if (d >= bedLeft && d <= bedRight)
                        sec.Bed.Add(cell);
                    continue;
                }

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

        /// <param name="relaxNsEndpointRows">When true (QA test river), north/south map rows are allowed on the first and last centerline steps only (full y span 0–height−1).</param>
        private static bool IsFootprintCellAllowedOnBorder(int wx, int wy, int gw, int gh, bool flowIsNorthSouth, bool flowPositive, bool isFirstSegment, bool isLastSegment, bool relaxNsEndpointRows = false)
        {
            if (flowIsNorthSouth)
            {
                if (wx == 0 || wx == gw - 1)
                    return false;
                if (relaxNsEndpointRows)
                {
                    if (flowPositive)
                    {
                        if (wy == 0 && !isFirstSegment && !isLastSegment)
                            return false;
                        if (wy == gh - 1 && !isFirstSegment && !isLastSegment)
                            return false;
                    }
                    else
                    {
                        if (wy == gh - 1 && !isFirstSegment && !isLastSegment)
                            return false;
                        if (wy == 0 && !isFirstSegment && !isLastSegment)
                            return false;
                    }

                    return true;
                }

                if (flowPositive)
                {
                    if (wy == 0 && !isFirstSegment)
                        return false;
                    if (wy == gh - 1 && !isLastSegment)
                        return false;
                }
                else
                {
                    if (wy == gh - 1 && !isFirstSegment)
                        return false;
                    if (wy == 0 && !isLastSegment)
                        return false;
                }

                return true;
            }

            if (wy == 0 || wy == gh - 1)
                return false;
            if (flowPositive)
            {
                if (wx == 0 && !isFirstSegment)
                    return false;
                if (wx == gw - 1 && !isLastSegment)
                    return false;
            }
            else
            {
                if (wx == gw - 1 && !isFirstSegment)
                    return false;
                if (wx == 0 && !isLastSegment)
                    return false;
            }

            return true;
        }

        private static void ApplyForcedSegmentHeights(WaterMap wm, HeightMap hm, List<TestCrossSection> sections, HashSet<Vector2Int> riverBedCarvedCells)
        {
            foreach (TestCrossSection sec in sections)
            {
                int seg = Mathf.Clamp(sec.SegmentIndex, 0, 3);
                int hBed = SegmentTargetBedHeights[seg];
                sec.AppliedBedHeight = hBed;

                foreach (Vector2Int p in sec.Bed)
                {
                    if (ShouldSkipCarvingLakeCellForRiverBed(wm, p.x, p.y, hBed))
                        continue;
                    hm.SetHeight(p.x, p.y, hBed);
                    riverBedCarvedCells.Add(p);
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

        private static bool ShouldSkipCarvingLakeCellForRiverBed(WaterMap wm, int x, int y, int hBed)
        {
            if (wm == null || !wm.IsWater(x, y))
                return false;
            if (wm.GetBodyClassificationAt(x, y) != WaterBodyType.Lake)
                return false;
            int riverSurface = Mathf.Min(TerrainManager.MAX_HEIGHT, hBed + 1);
            return wm.GetSurfaceHeightAt(x, y) != riverSurface;
        }

        private static void GetFootprintBounds(HashSet<Vector2Int> footprint, int gw, int gh, out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = gw;
            minY = gh;
            maxX = 0;
            maxY = 0;
            foreach (Vector2Int p in footprint)
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

        private static void LogTestRiverSummary(
            int gw,
            int gh,
            int riverX,
            int yWest,
            int yEast,
            int[] segStartT,
            int[] segEndT,
            int[] widthsRaw,
            int[] widthsClamped,
            int[] bodyIdsPerSegment,
            int corridorCells)
        {
            var sb = new StringBuilder(640);
            sb.AppendLine("[TestRiverGenerator] Straight grid West→East test river (canonical: +y = West, flow decreases y toward East).");
            sb.AppendLine($"  Map: {gw}×{gh}, centerline column x={riverX} (random in [10, width-10]), y spans {yWest} (west) → {yEast} (east), full height.");
            for (int i = 0; i < 4; i++)
            {
                int s = 4 - i;
                int bed = SegmentTargetBedHeights[i];
                int ySegHi = yWest - segStartT[i];
                int ySegLo = yWest - segEndT[i];
                sb.AppendLine(
                    $"  Segment {i + 1}: travel t [{segStartT[i]}, {segEndT[i]}], grid y [{ySegLo}, {ySegHi}] (west→east), S={s}, bed={bed}, bed width inspector={widthsRaw[i]}, clamped={widthsClamped[i]} (max {MaxBedWidth}), river body id={bodyIdsPerSegment[i]}");
            }

            sb.AppendLine($"  Corridor footprint cells (bed + shores): {corridorCells}");
            Debug.Log(sb.ToString());
        }
    }
}
