// long-file-allowed: complex algorithm — split would fragment logic without clean boundary
using System.Collections.Generic;
using Territory.Terrain;
using UnityEngine;

namespace Domains.Water.Services
{
    /// <summary>
    /// Pure BFS + carve logic extracted from ProceduralRiverGenerator (Stage 5.3 Tier-C NO-PORT).
    /// No MonoBehaviour / Manager dependency. Takes WaterMap + HeightMap directly.
    /// Hub (ProceduralRiverGenerator) delegates all private helpers here; Generate() stays in hub.
    /// Invariant #8: H_bed non-increasing along centerline (longitudinal clamp in ApplyCrossSectionHeights).
    /// Mirror of TerrainManager.MIN_HEIGHT = 0, MAX_HEIGHT = 5.
    /// </summary>
    public static class ProceduralRiverService
    {
        // Mirror of TerrainManager.MIN_HEIGHT / MAX_HEIGHT
        public const int MIN_HEIGHT = 0; // Mirror of TerrainManager.MIN_HEIGHT
        public const int MAX_HEIGHT = 5; // Mirror of TerrainManager.MAX_HEIGHT

        public const int MinRiverBedWidth = 1;
        public const int MaxRiverBedWidth = 3;

        /// <summary>Min distance from centerline to perpendicular map edges.</summary>
        public const int RiverBorderMargin = 2;

        /// <summary>Chebyshev dilation radius around prior river corridor cells.</summary>
        public const int MinCorridorSeparationDilation = 2;

        /// <summary>Min entry separation on same border.</summary>
        public const int MinRiverEntrySeparationOnBorder = 5;

        public static readonly int[] D4x = { 0, 0, 1, -1 };
        public static readonly int[] D4y = { 1, -1, 0, 0 };

        // ── Cross-section data ──────────────────────────────────────────────────

        /// <summary>One perpendicular strip: left bank, bed cells, right bank (§13.4).</summary>
        public sealed class RiverCrossSectionData
        {
            public readonly List<Vector2Int> Bed = new List<Vector2Int>(MaxRiverBedWidth);
            public Vector2Int LeftBank;
            public Vector2Int RightBank;
            public bool HasLeft;
            public bool HasRight;
            /// <summary>HeightMap floor under water after carve. <c>-1</c> if section skipped.</summary>
            public int AppliedBedHeight = -1;
            /// <summary>Shallow carve target before longitudinal clamp. <c>-1</c> if section has no bed.</summary>
            public int CandidateBedHeight = -1;

            public IEnumerable<Vector2Int> AllCorridorCells()
            {
                if (HasLeft) yield return LeftBank;
                foreach (Vector2Int p in Bed) yield return p;
                if (HasRight) yield return RightBank;
            }
        }

        // ── Avoid-set builder ───────────────────────────────────────────────────

        /// <summary>Dilate prior corridors in Chebyshev space for BFS avoid-set.</summary>
        public static HashSet<Vector2Int> BuildAvoidForBfs(HashSet<Vector2Int> usedCorridors, int gw, int gh)
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

        // ── Entry spacing guard ─────────────────────────────────────────────────

        public static bool IsEntryTooCloseToExistingStarts(Vector2Int candidate, bool northSouth, int gw, int gh, List<Vector2Int> sameAxisEntryStarts, int minSep)
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

        // ── Map guard ───────────────────────────────────────────────────────────

        public static bool CanPlaceRiversWithMargin(int gw, int gh)
            => gw >= 2 * RiverBorderMargin + 1 && gh >= 2 * RiverBorderMargin + 1;

        // ── Footprint bounds ────────────────────────────────────────────────────

        public static void GetFootprintBounds(HashSet<Vector2Int> footprint, int gw, int gh,
            out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = gw; minY = gh; maxX = 0; maxY = 0;
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

        // ── Cross-section builder ───────────────────────────────────────────────

        /// <param name="segmentIndex">Index along centerline. Entry/exit allow border footprint only here.</param>
        public static RiverCrossSectionData BuildCrossSection(WaterMap wm, int gw, int gh,
            Vector2Int prev, Vector2Int cur, Vector2Int next,
            int bedWidth, bool flowIsNorthSouth, bool flowPositive,
            int segmentIndex, int pathLength)
        {
            var sec = new RiverCrossSectionData();
            bedWidth = Mathf.Clamp(bedWidth, MinRiverBedWidth, MaxRiverBedWidth);
            int total = bedWidth + 2;
            int left = -(total / 2);
            int right = (total - 1) / 2;
            int bedLeft = left + 1;
            int bedRight = right - 1;

            Vector2Int dir = next - cur;
            if (dir.x == 0 && dir.y == 0) dir = cur - prev;
            if (dir.x == 0 && dir.y == 0) dir = Vector2Int.down;

            bool widthAlongX = Mathf.Abs(dir.y) >= Mathf.Abs(dir.x);
            bool isFirstSegment = segmentIndex == 0;
            bool isLastSegment = segmentIndex == pathLength - 1;

            for (int d = left; d <= right; d++)
            {
                int wx = widthAlongX ? cur.x + d : cur.x;
                int wy = widthAlongX ? cur.y : cur.y + d;
                if (wx < 0 || wx >= gw || wy < 0 || wy >= gh) continue;
                if (!IsFootprintCellAllowedOnBorder(wx, wy, gw, gh, flowIsNorthSouth, flowPositive, isFirstSegment, isLastSegment))
                    continue;
                var cell = new Vector2Int(wx, wy);
                if (wm.IsWater(wx, wy))
                {
                    if (d >= bedLeft && d <= bedRight) sec.Bed.Add(cell);
                    continue;
                }
                if (d >= bedLeft && d <= bedRight)
                    sec.Bed.Add(cell);
                else if (d == left) { sec.LeftBank = cell; sec.HasLeft = true; }
                else if (d == right) { sec.RightBank = cell; sec.HasRight = true; }
            }
            return sec;
        }

        // ── Height application ──────────────────────────────────────────────────

        /// <summary>
        /// Shallow carve candidate per section, then longitudinal clamp: H_bed[i] = min(candidate[i], H_bed[i-1]).
        /// Writes bed + symmetric bank heights. Invariant #8 preserved.
        /// </summary>
        public static void ApplyCrossSectionHeights(WaterMap wm, HeightMap hm,
            List<RiverCrossSectionData> sections, HashSet<Vector2Int> riverBedCarvedCells)
        {
            foreach (RiverCrossSectionData sec in sections)
            {
                sec.CandidateBedHeight = -1;
                sec.AppliedBedHeight = -1;
                if (sec.Bed.Count == 0) continue;
                int minH = int.MaxValue;
                foreach (Vector2Int p in sec.Bed)
                    minH = Mathf.Min(minH, hm.GetHeight(p.x, p.y));
                if (minH == int.MaxValue) continue;
                int candidate = Mathf.Max(MIN_HEIGHT, minH - Mathf.Min(2, minH - MIN_HEIGHT));
                sec.CandidateBedHeight = candidate;
            }

            int prevBed = int.MaxValue;
            foreach (RiverCrossSectionData sec in sections)
            {
                if (sec.CandidateBedHeight < 0) continue;
                int hBed = Mathf.Min(sec.CandidateBedHeight, prevBed);
                sec.AppliedBedHeight = hBed;
                prevBed = hBed;
            }

            foreach (RiverCrossSectionData sec in sections)
            {
                if (sec.AppliedBedHeight < 0) continue;
                int hBed = sec.AppliedBedHeight;
                foreach (Vector2Int p in sec.Bed)
                {
                    if (ShouldSkipCarvingLakeCellForRiverBed(wm, p.x, p.y, hBed)) continue;
                    hm.SetHeight(p.x, p.y, hBed);
                    riverBedCarvedCells.Add(p);
                }
                if (hBed >= MAX_HEIGHT) continue;
                int bankH = hBed + 1;
                if (sec.HasLeft && !wm.IsWater(sec.LeftBank.x, sec.LeftBank.y))
                    hm.SetHeight(sec.LeftBank.x, sec.LeftBank.y, bankH);
                if (sec.HasRight && !wm.IsWater(sec.RightBank.x, sec.RightBank.y))
                    hm.SetHeight(sec.RightBank.x, sec.RightBank.y, bankH);
            }
        }

        /// <summary>Lake cells with different surface than river segment bed are skipped (§12.7).</summary>
        public static bool ShouldSkipCarvingLakeCellForRiverBed(WaterMap wm, int x, int y, int hBed)
        {
            if (wm == null || !wm.IsWater(x, y)) return false;
            if (wm.GetBodyClassificationAt(x, y) != WaterBodyType.Lake) return false;
            int riverSurface = Mathf.Min(MAX_HEIGHT, hBed + 1);
            return wm.GetSurfaceHeightAt(x, y) != riverSurface;
        }

        /// <summary>
        /// Promote inner-corner bed cells H_bed → H_bed+1 to preserve shore art continuity (§13.5).
        /// </summary>
        public static void PromoteRiverBedInnerCornerShoreContinuity(HeightMap hm, int gw, int gh,
            HashSet<Vector2Int> bedFootprint, HashSet<Vector2Int> riverBedCarvedCells = null)
        {
            if (hm == null || bedFootprint == null || bedFootprint.Count == 0) return;
            foreach (Vector2Int p in bedFootprint)
            {
                if (riverBedCarvedCells != null && !riverBedCarvedCells.Contains(p)) continue;
                int x = p.x; int y = p.y;
                if (x < 0 || x >= gw || y < 0 || y >= gh) continue;
                int h = hm.GetHeight(x, y);
                if (h >= MAX_HEIGHT) continue;
                int hp = h + 1;

                bool BothPerpendicularShore(int ax, int ay, int bx, int by)
                {
                    if (!hm.IsValidPosition(ax, ay) || !hm.IsValidPosition(bx, by)) return false;
                    return hm.GetHeight(ax, ay) == hp && hm.GetHeight(bx, by) == hp;
                }

                if (BothPerpendicularShore(x + 1, y, x, y + 1)
                    || BothPerpendicularShore(x + 1, y, x, y - 1)
                    || BothPerpendicularShore(x - 1, y, x, y + 1)
                    || BothPerpendicularShore(x - 1, y, x, y - 1))
                {
                    hm.SetHeight(x, y, hp);
                }
            }
        }

        // ── Border allowance ────────────────────────────────────────────────────

        public static bool IsFootprintCellAllowedOnBorder(int wx, int wy, int gw, int gh,
            bool flowIsNorthSouth, bool flowPositive, bool isFirstSegment, bool isLastSegment)
        {
            if (flowIsNorthSouth)
            {
                if (wx == 0 || wx == gw - 1) return false;
                if (flowPositive)
                {
                    if (wy == 0 && !isFirstSegment) return false;
                    if (wy == gh - 1 && !isLastSegment) return false;
                }
                else
                {
                    if (wy == gh - 1 && !isFirstSegment) return false;
                    if (wy == 0 && !isLastSegment) return false;
                }
                return true;
            }
            if (wy == 0 || wy == gh - 1) return false;
            if (flowPositive)
            {
                if (wx == 0 && !isFirstSegment) return false;
                if (wx == gw - 1 && !isLastSegment) return false;
            }
            else
            {
                if (wx == gw - 1 && !isFirstSegment) return false;
                if (wx == 0 && !isLastSegment) return false;
            }
            return true;
        }

        // ── Centerline BFS ──────────────────────────────────────────────────────

        public static List<Vector2Int> TryBuildCenterline(WaterMap wm, HeightMap hm, int gw, int gh,
            int maxL, bool nsAxis, bool flowPositive, System.Random rnd,
            HashSet<Vector2Int> avoid, List<Vector2Int> sameAxisEntryStarts, int minEntrySeparationOnBorder)
        {
            if (nsAxis)
                return TryBfsEdgeToEdge(wm, hm, gw, gh, maxL, true, flowPositive, rnd, avoid, sameAxisEntryStarts, minEntrySeparationOnBorder);
            return TryBfsEdgeToEdge(wm, hm, gw, gh, maxL, false, flowPositive, rnd, avoid, sameAxisEntryStarts, minEntrySeparationOnBorder);
        }

        public static List<Vector2Int> TryBfsEdgeToEdge(WaterMap wm, HeightMap hm, int gw, int gh,
            int maxL, bool northSouth, bool flowPositive, System.Random rnd,
            HashSet<Vector2Int> avoid, List<Vector2Int> sameAxisEntryStarts, int minEntrySeparationOnBorder)
        {
            int m = RiverBorderMargin;
            if (avoid == null) avoid = new HashSet<Vector2Int>();
            Vector2Int start = FindDryBorderStart(wm, gw, gh, northSouth, flowPositive, rnd, avoid, sameAxisEntryStarts, minEntrySeparationOnBorder);
            if (start.x < 0) return null;

            var goals = new HashSet<Vector2Int>();
            if (northSouth)
            {
                int goalY = flowPositive ? gh - 1 : 0;
                for (int x = m; x <= gw - 1 - m; x++)
                    if (!wm.IsWater(x, goalY)) goals.Add(new Vector2Int(x, goalY));
            }
            else
            {
                int goalX = flowPositive ? gw - 1 : 0;
                for (int y = m; y <= gh - 1 - m; y++)
                    if (!wm.IsWater(goalX, y)) goals.Add(new Vector2Int(goalX, y));
            }
            if (goals.Count == 0) return null;

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
                if (dist[p] >= maxL) continue;

                if (northSouth && flowPositive && p.y == 0 && p == start)
                { TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, flowPositive, start, goals, p, p.x, p.y + 1, avoid, q, cameFrom, dist); continue; }

                if (northSouth && !flowPositive && p.y == gh - 1 && p == start)
                { TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, flowPositive, start, goals, p, p.x, p.y - 1, avoid, q, cameFrom, dist); continue; }

                if (!northSouth && flowPositive && p.x == 0 && p == start)
                { TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, flowPositive, start, goals, p, p.x + 1, p.y, avoid, q, cameFrom, dist); continue; }

                if (!northSouth && !flowPositive && p.x == gw - 1 && p == start)
                { TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, flowPositive, start, goals, p, p.x - 1, p.y, avoid, q, cameFrom, dist); continue; }

                int[] ord = { 0, 1, 2, 3 };
                ShuffleOrder(rnd, ord);
                for (int k = 0; k < 4; k++)
                {
                    int i = ord[k];
                    TryEnqueueRiverNeighbor(wm, hm, gw, gh, m, northSouth, flowPositive, start, goals, p,
                        p.x + D4x[i], p.y + D4y[i], avoid, q, cameFrom, dist);
                }
            }
            return null;
        }

        public static void TryEnqueueRiverNeighbor(
            WaterMap wm, HeightMap hm, int gw, int gh, int margin,
            bool northSouth, bool flowPositive,
            Vector2Int start, HashSet<Vector2Int> goals,
            Vector2Int p, int nx, int ny,
            HashSet<Vector2Int> avoid, Queue<Vector2Int> q,
            Dictionary<Vector2Int, Vector2Int> cameFrom, Dictionary<Vector2Int, int> dist)
        {
            if (nx < 0 || nx >= gw || ny < 0 || ny >= gh) return;
            if (northSouth)
            {
                if (nx < margin || nx > gw - 1 - margin) return;
                if (flowPositive)
                {
                    if (ny == 0) return;
                    if (ny == gh - 1 && !goals.Contains(new Vector2Int(nx, ny))) return;
                }
                else
                {
                    if (ny == gh - 1) return;
                    if (ny == 0 && !goals.Contains(new Vector2Int(nx, ny))) return;
                }
            }
            else
            {
                if (ny < margin || ny > gh - 1 - margin) return;
                if (flowPositive)
                {
                    if (nx == 0) return;
                    if (nx == gw - 1 && !goals.Contains(new Vector2Int(nx, ny))) return;
                }
                else
                {
                    if (nx == gw - 1) return;
                    if (nx == 0 && !goals.Contains(new Vector2Int(nx, ny))) return;
                }
            }
            Vector2Int np = new Vector2Int(nx, ny);
            if (avoid.Contains(np)) return;
            if (wm.IsWater(nx, ny)) return;
            if (!CardinalStepHeightOk(hm, p.x, p.y, nx, ny)) return;
            if (dist.ContainsKey(np)) return;
            cameFrom[np] = p;
            dist[np] = dist[p] + 1;
            q.Enqueue(np);
        }

        public static void ShuffleOrder(System.Random rnd, int[] arr)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                int t = arr[i]; arr[i] = arr[j]; arr[j] = t;
            }
        }

        /// <summary>Return (-1,-1) if no valid dry start in margin band.</summary>
        public static Vector2Int FindDryBorderStart(WaterMap wm, int gw, int gh, bool northSouth,
            bool flowPositive, System.Random rnd, HashSet<Vector2Int> avoid,
            List<Vector2Int> sameAxisEntryStarts, int minEntrySeparationOnBorder)
        {
            int m = RiverBorderMargin;
            if (avoid == null) avoid = new HashSet<Vector2Int>();

            bool IsValidStart(Vector2Int c)
            {
                if (!wm.IsValidPosition(c.x, c.y) || wm.IsWater(c.x, c.y)) return false;
                if (avoid.Contains(c)) return false;
                return !IsEntryTooCloseToExistingStarts(c, northSouth, gw, gh, sameAxisEntryStarts, minEntrySeparationOnBorder);
            }

            if (northSouth)
            {
                if (gw <= 2 * m) return new Vector2Int(-1, -1);
                int startRow = flowPositive ? 0 : gh - 1;
                for (int t = 0; t < 120; t++)
                {
                    int x = rnd.Next(m, gw - m);
                    var cand = new Vector2Int(x, startRow);
                    if (IsValidStart(cand)) return cand;
                }
                for (int x = m; x <= gw - 1 - m; x++)
                {
                    var cand = new Vector2Int(x, startRow);
                    if (IsValidStart(cand)) return cand;
                }
            }
            else
            {
                if (gh <= 2 * m) return new Vector2Int(-1, -1);
                int startCol = flowPositive ? 0 : gw - 1;
                for (int t = 0; t < 120; t++)
                {
                    int y = rnd.Next(m, gh - m);
                    var cand = new Vector2Int(startCol, y);
                    if (IsValidStart(cand)) return cand;
                }
                for (int y = m; y <= gh - 1 - m; y++)
                {
                    var cand = new Vector2Int(startCol, y);
                    if (IsValidStart(cand)) return cand;
                }
            }
            return new Vector2Int(-1, -1);
        }

        public static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom,
            Vector2Int start, Vector2Int goal)
        {
            var path = new List<Vector2Int>();
            Vector2Int cur = goal;
            while (cur != start) { path.Add(cur); cur = cameFrom[cur]; }
            path.Add(start);
            path.Reverse();
            return path;
        }

        public static bool CardinalStepHeightOk(HeightMap hm, int x0, int y0, int x1, int y1)
        {
            int h0 = hm.GetHeight(x0, y0);
            int h1 = hm.GetHeight(x1, y1);
            return Mathf.Abs(h0 - h1) <= 1;
        }

        /// <summary>Deterministic fallback path on BFS fail. Respects avoid (dilated prior corridors).</summary>
        public static List<Vector2Int> BuildForcedCenterline(WaterMap wm, int gw, int gh,
            bool nsAxis, bool flowPositive, System.Random rnd, HashSet<Vector2Int> avoid)
        {
            if (avoid == null) avoid = new HashSet<Vector2Int>();
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
                    while (wm.IsWater(x, y) && attempts < gw) { x++; if (x > gw - 1 - m) x = m; attempts++; }
                    if (wm.IsWater(x, y))
                        for (int xx = m; xx <= gw - 1 - m; xx++) { if (!wm.IsWater(xx, y)) { x = xx; break; } }
                    attempts = 0;
                    while (avoid.Contains(new Vector2Int(x, y)) && attempts < gw) { x++; if (x > gw - 1 - m) x = m; attempts++; }
                    if (avoid.Contains(new Vector2Int(x, y)))
                        for (int xx = m; xx <= gw - 1 - m; xx++)
                            if (!wm.IsWater(xx, y) && !avoid.Contains(new Vector2Int(xx, y))) { x = xx; break; }
                    if (wm.IsWater(x, y) || avoid.Contains(new Vector2Int(x, y))) return null;
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
                    while (wm.IsWater(x, y) && attempts < gh) { y++; if (y > gh - 1 - m) y = m; attempts++; }
                    if (wm.IsWater(x, y))
                        for (int yy = m; yy <= gh - 1 - m; yy++) { if (!wm.IsWater(x, yy)) { y = yy; break; } }
                    attempts = 0;
                    while (avoid.Contains(new Vector2Int(x, y)) && attempts < gh) { y++; if (y > gh - 1 - m) y = m; attempts++; }
                    if (avoid.Contains(new Vector2Int(x, y)))
                        for (int yy = m; yy <= gh - 1 - m; yy++)
                            if (!wm.IsWater(x, yy) && !avoid.Contains(new Vector2Int(x, yy))) { y = yy; break; }
                    if (wm.IsWater(x, y) || avoid.Contains(new Vector2Int(x, y))) return null;
                    y = Mathf.Clamp(y, m, gh - 1 - m);
                    path.Add(new Vector2Int(x, y));
                }
            }
            return path;
        }
    }
}
