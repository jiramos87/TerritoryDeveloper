using System.Collections.Generic;
using Territory.Core;
using UnityEngine;

namespace Territory.Terrain
{
    /// <summary>
    /// FEAT-38: procedural static rivers after lake/sea init. Bed footprint includes prior lake/sea cells in the cross-section;
    /// those cells are carved to <c>H_bed</c> and reassigned to the river body when allowed (lake at a different
    /// logical <see cref="WaterBody.SurfaceHeight"/> is not carved or reassigned — §12.7 / <see cref="WaterMap.TryReassignCellFromAnyWaterToRiverBody"/>).
    /// Cross-stream <b>bed</b> width (lecho) is 1–3 cells of <b>water</b>; corridor width = bed + 2 (one dry shore strip per side) for terrain refresh and collision with <see cref="RiverBorderMargin"/>.
    /// Each cross-section gets one shared bed height and symmetric bank height; water bodies are split when surface height changes along the path (see <c>isometric-geography-system.md</c> §13.4).
    /// After carving, inner-corner shore continuity is enforced on the bed footprint (see §13.5). Lake/river shore
    /// land heights are aligned with adjacent water surfaces during <see cref="TerrainManager.RefreshShoreTerrainAfterWaterUpdate"/> (§2.4.1).
    /// Bed floor <c>H_bed</c> is <b>non-increasing</b> along the centerline from map entry toward exit so the river never climbs terrain (see §13.4).
    /// Each river picks an axis (N–S vs E–W) and a flow direction along that axis with 50/50 randomness, so entry anchors are equally likely on north, south, west, or east borders (testing cascades from any side).
    /// Centerline and footprint avoid map borders except at designated entry/exit edges (see <see cref="RiverBorderMargin"/>).
    /// BUG-46: prior corridors are dilated in Chebyshev space for BFS; forced centerlines respect the same avoid set;
    /// entry anchors on the same map border must be separated; after placement, <see cref="WaterMap.MergeAdjacentBodiesWithSameSurface"/>
    /// unifies touching river ids at the same logical surface.
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

        /// <summary>
        /// Chebyshev dilation radius around each cell already reserved by a prior river corridor (bed + shores).
        /// New centerlines must not enter this expanded region (minimum gap between corridors).
        /// </summary>
        private const int MinCorridorSeparationDilation = 2;

        /// <summary>
        /// Minimum separation on the same map edge between new and prior entry anchors (|Δx| on north/south borders, |Δy| on west/east).
        /// </summary>
        private const int MinRiverEntrySeparationOnBorder = 5;

        private static readonly int[] D4x = { 0, 0, 1, -1 };
        private static readonly int[] D4y = { 1, -1, 0, 0 };

        /// <summary>
        /// Expands prior corridor cells so the next BFS centerline stays at least Chebyshev (dilation+1) away from reserved cells.
        /// </summary>
        private static HashSet<Vector2Int> BuildAvoidForBfs(HashSet<Vector2Int> usedCorridors, int gw, int gh)
        {
            var dilated = new HashSet<Vector2Int>();
            if (usedCorridors == null || usedCorridors.Count == 0)
                return dilated;
            int r = MinCorridorSeparationDilation;
            foreach (var p in usedCorridors)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) > r)
                            continue;
                        int nx = p.x + dx;
                        int ny = p.y + dy;
                        if (nx < 0 || nx >= gw || ny < 0 || ny >= gh)
                            continue;
                        dilated.Add(new Vector2Int(nx, ny));
                    }
                }
            }
            return dilated;
        }

        private static bool IsEntryTooCloseToExistingStarts(Vector2Int candidate, bool northSouth, int gw, int gh, List<Vector2Int> sameAxisEntryStarts, int minSep)
        {
            if (sameAxisEntryStarts == null || sameAxisEntryStarts.Count == 0 || minSep <= 0)
                return false;
            foreach (var s in sameAxisEntryStarts)
            {
                if (northSouth)
                {
                    bool sameNorth = candidate.y == 0 && s.y == 0;
                    bool sameSouth = candidate.y == gh - 1 && s.y == gh - 1;
                    if ((sameNorth || sameSouth) && Mathf.Abs(candidate.x - s.x) < minSep)
                        return true;
                }
                else
                {
                    bool sameWest = candidate.x == 0 && s.x == 0;
                    bool sameEast = candidate.x == gw - 1 && s.x == gw - 1;
                    if ((sameWest || sameEast) && Mathf.Abs(candidate.y - s.y) < minSep)
                        return true;
                }
            }
            return false;
        }

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

            int riverCount = rnd.Next(4, 8);
            var used = new HashSet<Vector2Int>();
            var nsEntryStarts = new List<Vector2Int>();
            var ewEntryStarts = new List<Vector2Int>();

            for (int r = 0; r < riverCount; r++)
            {
                bool nsAxis = rnd.Next(0, 2) == 0;
                bool flowPositive = rnd.Next(0, 2) == 0;
                HashSet<Vector2Int> avoidForBfs = BuildAvoidForBfs(used, gw, gh);
                List<Vector2Int> sameAxisEntries = nsAxis ? nsEntryStarts : ewEntryStarts;
                List<Vector2Int> centerline = TryBuildCenterline(wm, hm, gw, gh, maxL, nsAxis, flowPositive, rnd, avoidForBfs, sameAxisEntries, MinRiverEntrySeparationOnBorder);
                if (centerline == null || centerline.Count < 2)
                    centerline = BuildForcedCenterline(wm, gw, gh, nsAxis, flowPositive, rnd, avoidForBfs);

                if (centerline == null || centerline.Count < 2)
                    continue;

                if (nsAxis)
                    nsEntryStarts.Add(centerline[0]);
                else
                    ewEntryStarts.Add(centerline[0]);

                wm.RecordProceduralRiverEntryAnchor(centerline[0].x, centerline[0].y);

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
                    RiverCrossSectionData sec = BuildCrossSection(wm, gw, gh, prev, cur, next, bedWidth, nsAxis, flowPositive, i, pathLen);
                    crossSections.Add(sec);
                    foreach (Vector2Int p in sec.Bed)
                        waterFootprint.Add(p);
                    foreach (Vector2Int p in sec.AllCorridorCells())
                        corridorFootprint.Add(p);
                }

                foreach (var p in corridorFootprint)
                    used.Add(p);

                var riverBedCarvedCells = new HashSet<Vector2Int>();
                ApplyCrossSectionHeights(wm, hm, crossSections, riverBedCarvedCells);
                PromoteRiverBedInnerCornerShoreContinuity(hm, gw, gh, waterFootprint, riverBedCarvedCells);

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

        /// <summary>One perpendicular strip: left bank, bed cells, right bank (see project spec <c>.cursor/specs/isometric-geography-system.md</c> §13.4).</summary>
        private sealed class RiverCrossSectionData
        {
            public readonly List<Vector2Int> Bed = new List<Vector2Int>(MaxRiverBedWidth);
            public Vector2Int LeftBank;
            public Vector2Int RightBank;
            public bool HasLeft;
            public bool HasRight;
            /// <summary>HeightMap floor under water after carve; <c>-1</c> if section skipped.</summary>
            public int AppliedBedHeight = -1;
            /// <summary>Shallow carve target before longitudinal clamp; <c>-1</c> if section has no bed.</summary>
            public int CandidateBedHeight = -1;

            public IEnumerable<Vector2Int> AllCorridorCells()
            {
                if (HasLeft)
                    yield return LeftBank;
                foreach (Vector2Int p in Bed)
                    yield return p;
                if (HasRight)
                    yield return RightBank;
            }
        }

        /// <param name="segmentIndex">Index along centerline (entry/exit allow border footprint only here).</param>
        private static RiverCrossSectionData BuildCrossSection(WaterMap wm, int gw, int gh, Vector2Int prev, Vector2Int cur, Vector2Int next, int bedWidth, bool flowIsNorthSouth, bool flowPositive, int segmentIndex, int pathLength)
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
                if (!IsFootprintCellAllowedOnBorder(wx, wy, gw, gh, flowIsNorthSouth, flowPositive, isFirstSegment, isLastSegment))
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
                    sec.LeftBank = cell;
                    sec.HasLeft = true;
                }
                else if (d == right)
                {
                    sec.RightBank = cell;
                    sec.HasRight = true;
                }
            }

            return sec;
        }

        /// <summary>
        /// Shallow carve candidate per section, then <b>longitudinal</b> clamp: <c>H_bed[i] = min(candidate[i], H_bed[i-1])</c> from entry to exit (see <c>isometric-geography-system.md</c> §13.4).
        /// Finally writes bed and symmetric bank heights.
        /// </summary>
        /// <param name="riverBedCarvedCells">Cells where bed height was written; excludes skipped lake cells (different surface).</param>
        private static void ApplyCrossSectionHeights(WaterMap wm, HeightMap hm, List<RiverCrossSectionData> sections, HashSet<Vector2Int> riverBedCarvedCells)
        {
            foreach (RiverCrossSectionData sec in sections)
            {
                sec.CandidateBedHeight = -1;
                sec.AppliedBedHeight = -1;
                if (sec.Bed.Count == 0)
                    continue;

                int minH = int.MaxValue;
                foreach (Vector2Int p in sec.Bed)
                    minH = Mathf.Min(minH, hm.GetHeight(p.x, p.y));

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
                    if (ShouldSkipCarvingLakeCellForRiverBed(wm, p.x, p.y, hBed))
                        continue;
                    hm.SetHeight(p.x, p.y, hBed);
                    riverBedCarvedCells.Add(p);
                }

                if (hBed >= TerrainManager.MAX_HEIGHT)
                    continue;

                int bankH = hBed + 1;
                if (sec.HasLeft && !wm.IsWater(sec.LeftBank.x, sec.LeftBank.y))
                    hm.SetHeight(sec.LeftBank.x, sec.LeftBank.y, bankH);
                if (sec.HasRight && !wm.IsWater(sec.RightBank.x, sec.RightBank.y))
                    hm.SetHeight(sec.RightBank.x, sec.RightBank.y, bankH);
            }
        }

        /// <summary>
        /// Lake cells that would require a cross-body surface step vs the river segment bed are not carved to <paramref name="hBed"/>;
        /// keeps terrain and <see cref="WaterMap"/> consistent when the river corridor overlaps an existing lake (§12.7).
        /// </summary>
        private static bool ShouldSkipCarvingLakeCellForRiverBed(WaterMap wm, int x, int y, int hBed)
        {
            if (wm == null || !wm.IsWater(x, y))
                return false;
            if (wm.GetBodyClassificationAt(x, y) != WaterBodyType.Lake)
                return false;
            int riverSurface = Mathf.Min(TerrainManager.MAX_HEIGHT, hBed + 1);
            return wm.GetSurfaceHeightAt(x, y) != riverSurface;
        }

        /// <summary>
        /// After carving bed and banks, some bed cells can sit at the <b>inner corner</b> of an L-shaped shore where two
        /// perpendicular bank neighbors are one step higher — leaving a water-height hole breaks continuous shore art
        /// (see isometric spec §13.5). Promotes such cells from <c>H_bed</c> to <c>H_bed + 1</c> so they stay dry shore.
        /// </summary>
        /// <param name="riverBedCarvedCells">When non-null, only these bed footprint cells received a river bed carve (skips protected lake cells).</param>
        private static void PromoteRiverBedInnerCornerShoreContinuity(HeightMap hm, int gw, int gh, HashSet<Vector2Int> bedFootprint, HashSet<Vector2Int> riverBedCarvedCells = null)
        {
            if (hm == null || bedFootprint == null || bedFootprint.Count == 0)
                return;

            foreach (Vector2Int p in bedFootprint)
            {
                if (riverBedCarvedCells != null && !riverBedCarvedCells.Contains(p))
                    continue;
                int x = p.x;
                int y = p.y;
                if (x < 0 || x >= gw || y < 0 || y >= gh)
                    continue;
                int h = hm.GetHeight(x, y);
                if (h >= TerrainManager.MAX_HEIGHT)
                    continue;
                int hp = h + 1;

                bool PromoteIfBothPerpendicularShore(int ax, int ay, int bx, int by)
                {
                    if (!hm.IsValidPosition(ax, ay) || !hm.IsValidPosition(bx, by))
                        return false;
                    return hm.GetHeight(ax, ay) == hp && hm.GetHeight(bx, by) == hp;
                }

                if (PromoteIfBothPerpendicularShore(x + 1, y, x, y + 1)
                    || PromoteIfBothPerpendicularShore(x + 1, y, x, y - 1)
                    || PromoteIfBothPerpendicularShore(x - 1, y, x, y + 1)
                    || PromoteIfBothPerpendicularShore(x - 1, y, x, y - 1))
                {
                    hm.SetHeight(x, y, hp);
                }
            }
        }

        /// <summary>
        /// N–S flow: never west/east map edges. <paramref name="flowPositive"/> true = entry north / exit south; false = entry south / exit north.
        /// E–W flow: never north/south map edges. <paramref name="flowPositive"/> true = entry west / exit east; false = entry east / exit west.
        /// </summary>
        private static bool IsFootprintCellAllowedOnBorder(int wx, int wy, int gw, int gh, bool flowIsNorthSouth, bool flowPositive, bool isFirstSegment, bool isLastSegment)
        {
            if (flowIsNorthSouth)
            {
                if (wx == 0 || wx == gw - 1)
                    return false;
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

        private static List<Vector2Int> TryBuildCenterline(WaterMap wm, HeightMap hm, int gw, int gh, int maxL, bool nsAxis, bool flowPositive, System.Random rnd, HashSet<Vector2Int> avoid, List<Vector2Int> sameAxisEntryStarts, int minEntrySeparationOnBorder)
        {
            if (nsAxis)
                return TryBfsEdgeToEdge(wm, hm, gw, gh, maxL, true, flowPositive, rnd, avoid, sameAxisEntryStarts, minEntrySeparationOnBorder);
            return TryBfsEdgeToEdge(wm, hm, gw, gh, maxL, false, flowPositive, rnd, avoid, sameAxisEntryStarts, minEntrySeparationOnBorder);
        }

        private static List<Vector2Int> TryBfsEdgeToEdge(WaterMap wm, HeightMap hm, int gw, int gh, int maxL, bool northSouth, bool flowPositive, System.Random rnd, HashSet<Vector2Int> avoid, List<Vector2Int> sameAxisEntryStarts, int minEntrySeparationOnBorder)
        {
            int m = RiverBorderMargin;
            if (avoid == null)
                avoid = new HashSet<Vector2Int>();
            Vector2Int start = FindDryBorderStart(wm, gw, gh, northSouth, flowPositive, rnd, avoid, sameAxisEntryStarts, minEntrySeparationOnBorder);
            if (start.x < 0)
                return null;

            var goals = new HashSet<Vector2Int>();
            if (northSouth)
            {
                int goalY = flowPositive ? gh - 1 : 0;
                for (int x = m; x <= gw - 1 - m; x++)
                {
                    if (!wm.IsWater(x, goalY))
                        goals.Add(new Vector2Int(x, goalY));
                }
            }
            else
            {
                int goalX = flowPositive ? gw - 1 : 0;
                for (int y = m; y <= gh - 1 - m; y++)
                {
                    if (!wm.IsWater(goalX, y))
                        goals.Add(new Vector2Int(goalX, y));
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

                if (northSouth && flowPositive && p.y == 0 && p == start)
                {
                    TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, flowPositive, start, goals, p, p.x, p.y + 1, avoid, q, cameFrom, dist);
                    continue;
                }

                if (northSouth && !flowPositive && p.y == gh - 1 && p == start)
                {
                    TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, flowPositive, start, goals, p, p.x, p.y - 1, avoid, q, cameFrom, dist);
                    continue;
                }

                if (!northSouth && flowPositive && p.x == 0 && p == start)
                {
                    TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, flowPositive, start, goals, p, p.x + 1, p.y, avoid, q, cameFrom, dist);
                    continue;
                }

                if (!northSouth && !flowPositive && p.x == gw - 1 && p == start)
                {
                    TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, flowPositive, start, goals, p, p.x - 1, p.y, avoid, q, cameFrom, dist);
                    continue;
                }

                int[] ord = { 0, 1, 2, 3 };
                ShuffleOrder(rnd, ord);
                for (int k = 0; k < 4; k++)
                {
                    int i = ord[k];
                    int nx = p.x + D4x[i];
                    int ny = p.y + D4y[i];
                    TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, flowPositive, start, goals, p, nx, ny, avoid, q, cameFrom, dist);
                }
            }

            return null;
        }

        private static void TryEnqueueRiverNeighbor(
            WaterMap wm, HeightMap hm, int gw, int gh, int margin, bool northSouth, bool flowPositive, Vector2Int start, HashSet<Vector2Int> goals,
            Vector2Int p, int nx, int ny, HashSet<Vector2Int> avoid, Queue<Vector2Int> q, Dictionary<Vector2Int, Vector2Int> cameFrom, Dictionary<Vector2Int, int> dist)
        {
            if (nx < 0 || nx >= gw || ny < 0 || ny >= gh)
                return;

            if (northSouth)
            {
                if (nx < margin || nx > gw - 1 - margin)
                    return;
                if (flowPositive)
                {
                    if (ny == 0)
                        return;
                    if (ny == gh - 1 && !goals.Contains(new Vector2Int(nx, ny)))
                        return;
                }
                else
                {
                    if (ny == gh - 1)
                        return;
                    if (ny == 0 && !goals.Contains(new Vector2Int(nx, ny)))
                        return;
                }
            }
            else
            {
                if (ny < margin || ny > gh - 1 - margin)
                    return;
                if (flowPositive)
                {
                    if (nx == 0)
                        return;
                    if (nx == gw - 1 && !goals.Contains(new Vector2Int(nx, ny)))
                        return;
                }
                else
                {
                    if (nx == gw - 1)
                        return;
                    if (nx == 0 && !goals.Contains(new Vector2Int(nx, ny)))
                        return;
                }
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

        /// <summary>Returns <c>(-1,-1)</c> if no valid dry start in the margin band (respects <paramref name="avoid"/> and entry spacing).</summary>
        private static Vector2Int FindDryBorderStart(WaterMap wm, int gw, int gh, bool northSouth, bool flowPositive, System.Random rnd, HashSet<Vector2Int> avoid, List<Vector2Int> sameAxisEntryStarts, int minEntrySeparationOnBorder)
        {
            int m = RiverBorderMargin;
            if (avoid == null)
                avoid = new HashSet<Vector2Int>();

            bool IsValidStart(Vector2Int c)
            {
                if (!wm.IsValidPosition(c.x, c.y) || wm.IsWater(c.x, c.y))
                    return false;
                if (avoid.Contains(c))
                    return false;
                return !IsEntryTooCloseToExistingStarts(c, northSouth, gw, gh, sameAxisEntryStarts, minEntrySeparationOnBorder);
            }

            if (northSouth)
            {
                if (gw <= 2 * m)
                    return new Vector2Int(-1, -1);
                int startRow = flowPositive ? 0 : gh - 1;
                for (int t = 0; t < 120; t++)
                {
                    int x = rnd.Next(m, gw - m);
                    var cand = new Vector2Int(x, startRow);
                    if (IsValidStart(cand))
                        return cand;
                }
                for (int x = m; x <= gw - 1 - m; x++)
                {
                    var cand = new Vector2Int(x, startRow);
                    if (IsValidStart(cand))
                        return cand;
                }
            }
            else
            {
                if (gh <= 2 * m)
                    return new Vector2Int(-1, -1);
                int startCol = flowPositive ? 0 : gw - 1;
                for (int t = 0; t < 120; t++)
                {
                    int y = rnd.Next(m, gh - m);
                    var cand = new Vector2Int(startCol, y);
                    if (IsValidStart(cand))
                        return cand;
                }
                for (int y = m; y <= gh - 1 - m; y++)
                {
                    var cand = new Vector2Int(startCol, y);
                    if (IsValidStart(cand))
                        return cand;
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

        /// <summary>
        /// Deterministic fallback path when BFS fails. Respects <paramref name="avoid"/> (dilated prior corridors) like the BFS path.
        /// Returns <c>null</c> if any row/column cannot be satisfied without water or blocked cells.
        /// </summary>
        private static List<Vector2Int> BuildForcedCenterline(WaterMap wm, int gw, int gh, bool nsAxis, bool flowPositive, System.Random rnd, HashSet<Vector2Int> avoid)
        {
            if (avoid == null)
                avoid = new HashSet<Vector2Int>();

            int m = RiverBorderMargin;
            var path = new List<Vector2Int>();
            if (nsAxis)
            {
                int x = Mathf.Clamp(gw / 2, m, gw - 1 - m);
                int yStart = flowPositive ? 0 : gh - 1;
                int yEnd = flowPositive ? gh : -1;
                int yStep = flowPositive ? 1 : -1;
                for (int y = yStart; y != yEnd; y += yStep)
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

                    attempts = 0;
                    while (avoid.Contains(new Vector2Int(x, y)) && attempts < gw)
                    {
                        x++;
                        if (x > gw - 1 - m)
                            x = m;
                        attempts++;
                    }

                    if (avoid.Contains(new Vector2Int(x, y)))
                    {
                        for (int xx = m; xx <= gw - 1 - m; xx++)
                        {
                            if (!wm.IsWater(xx, y) && !avoid.Contains(new Vector2Int(xx, y)))
                            {
                                x = xx;
                                break;
                            }
                        }
                    }

                    if (wm.IsWater(x, y) || avoid.Contains(new Vector2Int(x, y)))
                        return null;

                    x = Mathf.Clamp(x, m, gw - 1 - m);
                    path.Add(new Vector2Int(x, y));
                }
            }
            else
            {
                int y = Mathf.Clamp(gh / 2, m, gh - 1 - m);
                int xStart = flowPositive ? 0 : gw - 1;
                int xEnd = flowPositive ? gw : -1;
                int xStep = flowPositive ? 1 : -1;
                for (int x = xStart; x != xEnd; x += xStep)
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

                    attempts = 0;
                    while (avoid.Contains(new Vector2Int(x, y)) && attempts < gh)
                    {
                        y++;
                        if (y > gh - 1 - m)
                            y = m;
                        attempts++;
                    }

                    if (avoid.Contains(new Vector2Int(x, y)))
                    {
                        for (int yy = m; yy <= gh - 1 - m; yy++)
                        {
                            if (!wm.IsWater(x, yy) && !avoid.Contains(new Vector2Int(x, yy)))
                            {
                                y = yy;
                                break;
                            }
                        }
                    }

                    if (wm.IsWater(x, y) || avoid.Contains(new Vector2Int(x, y)))
                        return null;

                    y = Mathf.Clamp(y, m, gh - 1 - m);
                    path.Add(new Vector2Int(x, y));
                }
            }

            return path;
        }
    }
}
