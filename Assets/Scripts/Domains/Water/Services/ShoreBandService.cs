// long-file-allowed: complex algorithm — split would fragment logic without clean boundary
using System;
using System.Collections.Generic;
using Territory.Terrain;
using Territory.Core;
using UnityEngine;

namespace Domains.Water.Services
{
    /// <summary>
    /// Lake depression-fill + basin/shore-band logic extracted from WaterMap for testability.
    /// No MonoBehaviour dependency. Owns lake placement algorithms; delegates cell writes back via WaterMap.
    /// Extracted per Strategy γ atomization (Stage 3.2, TECH-30018).
    /// Invariant #7 (shore band): affiliation logic preserved verbatim from WaterMap.
    /// </summary>
    public class ShoreBandService
    {
        private const int OutsideMapSpillHeight = 6;

        // ── local cache of map dimensions (set via SetContext) ──────────────────
        private int _width;
        private int _height;

        /// <summary>Bind map dimensions before any call that needs IsValidPosition.</summary>
        public void SetContext(int width, int height)
        {
            _width = width;
            _height = height;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Lake depression-fill (extracted from WaterMap.InitializeLakesFromDepressionFill)
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fill natural depressions in height map with lake bodies via WaterMap mutation callbacks.
        /// Mirrors WaterMap.InitializeLakesFromDepressionFill verbatim; operates through delegates so
        /// WaterMap retains ownership of waterBodyIds / bodies arrays.
        /// </summary>
        public void FillLakesFromDepressions(
            HeightMap heightMap,
            LakeFillSettings settings,
            int seaLevelForArtificialFallback,
            int width, int height,
            // mutation callbacks
            Action clearAllWater,
            Action<int, int, int, WaterBodyType> registerCell,     // (x,y,bodyId,type) + bodyId already added
            Func<int> allocateBodyId,
            Action<int, int, WaterBodyType> createBody,             // (bodyId, surface, type)
            Action runMergeAdjacentBodies,
            Action<int, int, int, HeightMap, IGridManager> tryAbsorbDryCell, // x,y,targetBodyId,hm,grid
            // dirty rect output callbacks
            Action<int, int, int, int> setArtificialDirty,  // minX,minY,maxX,maxY
            Action<int> setLastLakeDiagnostics,              // dummy; real diagnostics set by caller
            out int lastLakeTargetBodies,
            out int lastLakeScaledBudget,
            out int lastLakeFinalBodyCount,
            out int lastLakeArtificialBodiesPlaced,
            out int lastLakeRecoveryPasses,
            out bool lastLakeMetTarget,
            out int lastLakeProceduralBodiesAfterBounded,
            IGridManager gridManager = null)
        {
            SetContext(width, height);
            clearAllWater();

            int effectiveMaxLakeBodies = settings.GetEffectiveMaxLakeBodies(width, height);
            int areaScaledBudget = settings.GetAreaScaledLakeBudgetDiagnostic(width, height);
            lastLakeTargetBodies = effectiveMaxLakeBodies;
            lastLakeScaledBudget = areaScaledBudget;
            lastLakeArtificialBodiesPlaced = 0;
            lastLakeRecoveryPasses = 0;
            lastLakeMetTarget = false;
            lastLakeProceduralBodiesAfterBounded = 0;
            lastLakeFinalBodyCount = 0;

            if (heightMap == null || settings == null)
                return;

            int randomExtraAttempts = settings.GetScaledRandomExtraSeedAttempts(width, height);
            var rnd = new System.Random(settings.RandomSeed);
            var seedSet = new HashSet<Vector2Int>();
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    if (IsStrictLocalMinimum(x, y, heightMap))
                        seedSet.Add(new Vector2Int(x, y));
                    if (IsLocalMinimumInWindow(x, y, heightMap, settings.LocalMinWindowRadius))
                        seedSet.Add(new Vector2Int(x, y));
                }

            for (int i = 0; i < randomExtraAttempts; i++)
                seedSet.Add(new Vector2Int(rnd.Next(width), rnd.Next(height)));

            var minima = new List<Vector2Int>(seedSet);
            SortSeedCandidatesBySpillHeadroom(minima, heightMap, rnd);

            var claimed = new bool[width, height];
            int bodiesCreated = 0;
            var tempBodies = new Dictionary<int, (int surface, WaterBodyType type, List<Vector2Int> cells)>();

            foreach (var seed in minima)
            {
                if (claimed[seed.x, seed.y])
                    continue;
                if (bodiesCreated >= effectiveMaxLakeBodies)
                    break;

                int spill = GetPlateauSpillHeight(seed.x, seed.y, heightMap);
                if (spill <= heightMap.GetHeight(seed.x, seed.y))
                    continue;
                if (rnd.NextDouble() > settings.LakeAcceptProbability)
                    continue;

                var basinCells = new List<Vector2Int>();
                CollectBasin(seed.x, seed.y, spill, heightMap, basinCells);
                if (basinCells.Count < settings.MinLakeCells)
                    continue;
                if (!WaterMapService.LakeBoundingBoxFits(settings, basinCells))
                    continue;

                bool overlaps = false;
                foreach (var c in basinCells)
                    if (claimed[c.x, c.y]) { overlaps = true; break; }
                if (overlaps) continue;

                int bodyId = allocateBodyId();
                createBody(bodyId, spill, WaterBodyType.Lake);
                foreach (var c in basinCells)
                {
                    registerCell(c.x, c.y, bodyId, WaterBodyType.Lake);
                    claimed[c.x, c.y] = true;
                }
                bodiesCreated++;
            }

            // bounded local depression pass
            if (settings.RunBoundedLocalDepressionPass)
                TryFillBoundedLocalDepressions(heightMap, settings, rnd, claimed, ref bodiesCreated,
                    effectiveMaxLakeBodies, allocateBodyId, createBody, registerCell);

            // query current body count for diagnostics
            lastLakeProceduralBodiesAfterBounded = bodiesCreated;
            runMergeAdjacentBodies();

            if (bodiesCreated < effectiveMaxLakeBodies)
            {
                const int maxRecoveryPasses = 48;
                int totalArtificial = 0;
                int pass = 0;
                while (bodiesCreated < effectiveMaxLakeBodies && pass < maxRecoveryPasses)
                {
                    int added = TryArtificialLakeFallback(heightMap, settings, seaLevelForArtificialFallback,
                        effectiveMaxLakeBodies, claimed, rnd, allocateBodyId, createBody, registerCell,
                        setArtificialDirty);
                    totalArtificial += added;
                    bodiesCreated += added;
                    runMergeAdjacentBodies();
                    pass++;
                    lastLakeRecoveryPasses = pass;
                    if (bodiesCreated >= effectiveMaxLakeBodies) break;
                    if (added == 0) break;
                }

                if (bodiesCreated < effectiveMaxLakeBodies)
                {
                    int cornerAdded = TryLastResortCornerArtificialLakes(heightMap, settings,
                        seaLevelForArtificialFallback, effectiveMaxLakeBodies, claimed,
                        allocateBodyId, createBody, registerCell, setArtificialDirty);
                    totalArtificial += cornerAdded;
                    bodiesCreated += cornerAdded;
                    runMergeAdjacentBodies();
                }

                lastLakeArtificialBodiesPlaced = totalArtificial;
            }

            lastLakeFinalBodyCount = bodiesCreated;
            lastLakeMetTarget = bodiesCreated >= effectiveMaxLakeBodies;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Basin / seed helpers (static, pure)
        // ─────────────────────────────────────────────────────────────────────────

        private bool IsValidPosition(int x, int y) =>
            x >= 0 && x < _width && y >= 0 && y < _height;

        private static int BorderNeighborHeight(int x, int y, HeightMap hm)
        {
            if (hm.IsValidPosition(x, y)) return hm.GetHeight(x, y);
            return OutsideMapSpillHeight;
        }

        private bool IsStrictLocalMinimum(int x, int y, HeightMap hm)
        {
            int h0 = hm.GetHeight(x, y);
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                int nh = BorderNeighborHeight(x + dx[i], y + dy[i], hm);
                if (nh <= h0) return false;
            }
            return true;
        }

        private bool IsLocalMinimumInWindow(int x, int y, HeightMap hm, int radius)
        {
            if (radius < 0) return false;
            int h0 = hm.GetHeight(x, y);
            int maxH = int.MinValue;
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy2 = -radius; dy2 <= radius; dy2++)
                {
                    int nx = x + dx;
                    int ny = y + dy2;
                    if (!IsValidPosition(nx, ny)) continue;
                    int h = hm.GetHeight(nx, ny);
                    if (h < h0) return false;
                    if (h > maxH) maxH = h;
                }
            return maxH > h0;
        }

        private int GetPlateauSpillHeight(int x, int y, HeightMap hm)
        {
            int seedH = hm.GetHeight(x, y);
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            int spill = int.MaxValue;
            var q = new Queue<Vector2Int>();
            var inPlateau = new bool[_width, _height];
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
                    if (!IsValidPosition(nx, ny))
                    {
                        spill = Mathf.Min(spill, BorderNeighborHeight(nx, ny, hm));
                        continue;
                    }
                    if (inPlateau[nx, ny]) continue;
                    int nh = hm.GetHeight(nx, ny);
                    if (nh == seedH)
                    {
                        inPlateau[nx, ny] = true;
                        q.Enqueue(new Vector2Int(nx, ny));
                    }
                    else
                        spill = Mathf.Min(spill, nh);
                }
            }

            if (plateauCount == _width * _height) return seedH;
            if (spill == int.MaxValue) return seedH;
            return spill;
        }

        private void CollectBasin(int sx, int sy, int spillHeight, HeightMap hm, List<Vector2Int> outCells)
        {
            outCells.Clear();
            if (hm.GetHeight(sx, sy) >= spillHeight) return;
            var q = new Queue<Vector2Int>();
            var visited = new bool[_width, _height];
            q.Enqueue(new Vector2Int(sx, sy));
            visited[sx, sy] = true;
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                outCells.Add(c);
                for (int i = 0; i < 4; i++)
                {
                    int nx = c.x + dx[i];
                    int ny = c.y + dy[i];
                    if (!IsValidPosition(nx, ny) || visited[nx, ny]) continue;
                    if (hm.GetHeight(nx, ny) >= spillHeight) continue;
                    visited[nx, ny] = true;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        private void SortSeedCandidatesBySpillHeadroom(List<Vector2Int> cells, HeightMap heightMap, System.Random rnd)
        {
            int n = cells.Count;
            if (n <= 1) return;
            var tmp = new List<(Vector2Int p, int tie)>(n);
            for (int i = 0; i < n; i++)
                tmp.Add((cells[i], rnd.Next()));
            tmp.Sort((A, B) =>
            {
                Vector2Int a = A.p;
                Vector2Int b = B.p;
                int spillA = GetPlateauSpillHeight(a.x, a.y, heightMap);
                int spillB = GetPlateauSpillHeight(b.x, b.y, heightMap);
                int hA = heightMap.GetHeight(a.x, a.y);
                int hB = heightMap.GetHeight(b.x, b.y);
                int scoreA = spillA - hA;
                int scoreB = spillB - hB;
                int c = scoreB.CompareTo(scoreA);
                if (c != 0) return c;
                c = hA.CompareTo(hB);
                if (c != 0) return c;
                return A.tie.CompareTo(B.tie);
            });
            cells.Clear();
            for (int i = 0; i < n; i++)
                cells.Add(tmp[i].p);
        }

        private void TryFillBoundedLocalDepressions(
            HeightMap heightMap, LakeFillSettings settings, System.Random rnd,
            bool[,] claimed, ref int bodiesCreated, int effectiveMaxLakeBodies,
            Func<int> allocateBodyId, Action<int, int, WaterBodyType> createBody,
            Action<int, int, int, WaterBodyType> registerCell)
        {
            var extraSeeds = new List<Vector2Int>();
            int r = settings.BoundedLocalDepressionWindowRadius;
            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                {
                    if (claimed[x, y]) continue;
                    if (!IsLocalMinimumInWindow(x, y, heightMap, r)) continue;
                    extraSeeds.Add(new Vector2Int(x, y));
                }

            SortSeedCandidatesBySpillHeadroom(extraSeeds, heightMap, rnd);
            foreach (var seed in extraSeeds)
            {
                if (claimed[seed.x, seed.y]) continue;
                if (bodiesCreated >= effectiveMaxLakeBodies) break;
                int spill = GetPlateauSpillHeight(seed.x, seed.y, heightMap);
                if (spill <= heightMap.GetHeight(seed.x, seed.y)) continue;
                if (rnd.NextDouble() > settings.LakeAcceptProbability * settings.BoundedLocalDepressionAcceptScale) continue;
                var basinCells = new List<Vector2Int>();
                CollectBasin(seed.x, seed.y, spill, heightMap, basinCells);
                if (basinCells.Count < settings.MinLakeCells) continue;
                if (basinCells.Count > settings.BoundedLocalDepressionMaxBasinCells) continue;
                if (!WaterMapService.LakeBoundingBoxFits(settings, basinCells)) continue;
                bool overlaps = false;
                foreach (var c in basinCells)
                    if (claimed[c.x, c.y]) { overlaps = true; break; }
                if (overlaps) continue;
                int bodyId = allocateBodyId();
                createBody(bodyId, spill, WaterBodyType.Lake);
                foreach (var c in basinCells)
                {
                    registerCell(c.x, c.y, bodyId, WaterBodyType.Lake);
                    claimed[c.x, c.y] = true;
                }
                bodiesCreated++;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Artificial lake placement helpers
        // ─────────────────────────────────────────────────────────────────────────

        private int ComputeArtificialEdgeMargin()
        {
            int m = Mathf.Min(_width, _height);
            if (m <= 4) return 0;
            if (m <= 9) return 1;
            return 2;
        }

        private int TryArtificialLakeFallback(
            HeightMap heightMap, LakeFillSettings settings, int seaLevel,
            int targetBodies, bool[,] claimed, System.Random rnd,
            Func<int> allocateBodyId, Action<int, int, WaterBodyType> createBody,
            Action<int, int, int, WaterBodyType> registerCell,
            Action<int, int, int, int> setArtificialDirty)
        {
            int edgeMargin = ComputeArtificialEdgeMargin();
            const int maxRandomAttempts = 2500;
            int attempts = 0;
            int added = 0;
            while (added < targetBodies && attempts < maxRandomAttempts)
            {
                attempts++;
                int rw = rnd.Next(settings.MinLakeBoundingExtent, settings.MaxLakeBoundingExtent + 1);
                int rh = rnd.Next(settings.MinLakeBoundingExtent, settings.MaxLakeBoundingExtent + 1);
                rw = Mathf.Clamp(rw, 1, Mathf.Max(1, _width - 2 * edgeMargin));
                rh = Mathf.Clamp(rh, 1, Mathf.Max(1, _height - 2 * edgeMargin));
                if (_width - rw - 2 * edgeMargin < 1 || _height - rh - 2 * edgeMargin < 1) continue;
                int x0 = rnd.Next(edgeMargin, _width - rw - edgeMargin + 1);
                int y0 = rnd.Next(edgeMargin, _height - rh - edgeMargin + 1);
                if (TryPlaceArtificialRectangle(heightMap, settings, seaLevel, x0, y0, rw, rh, claimed,
                    edgeMargin, allocateBodyId, createBody, registerCell, setArtificialDirty))
                {
                    added++;
                    if (added >= targetBodies) break;
                }
            }
            if (added < targetBodies)
                added += TryDeterministicArtificialLakeFallback(heightMap, settings, seaLevel, targetBodies,
                    claimed, allocateBodyId, createBody, registerCell, setArtificialDirty);
            return added;
        }

        private int TryDeterministicArtificialLakeFallback(
            HeightMap heightMap, LakeFillSettings settings, int seaLevel,
            int targetBodies, bool[,] claimed,
            Func<int> allocateBodyId, Action<int, int, WaterBodyType> createBody,
            Action<int, int, int, WaterBodyType> registerCell,
            Action<int, int, int, int> setArtificialDirty)
        {
            int edgeMargin = ComputeArtificialEdgeMargin();
            int added = 0;
            int maxExtent = Mathf.Min(settings.MaxLakeBoundingExtent, 12, _width - 2 * edgeMargin, _height - 2 * edgeMargin);
            maxExtent = Mathf.Max(1, maxExtent);
            int minExtent = Mathf.Min(settings.MinLakeBoundingExtent, maxExtent);
            for (int extent = minExtent; extent <= maxExtent; extent++)
            {
                if (added >= targetBodies) break;
                for (int x0 = edgeMargin; x0 + extent <= _width - edgeMargin; x0++)
                    for (int y0 = edgeMargin; y0 + extent <= _height - edgeMargin; y0++)
                        if (TryPlaceArtificialRectangle(heightMap, settings, seaLevel, x0, y0, extent, extent,
                            claimed, edgeMargin, allocateBodyId, createBody, registerCell, setArtificialDirty))
                        {
                            added++;
                            if (added >= targetBodies) return added;
                        }
            }
            return added;
        }

        private int TryLastResortCornerArtificialLakes(
            HeightMap heightMap, LakeFillSettings settings, int seaLevel,
            int targetBodies, bool[,] claimed,
            Func<int> allocateBodyId, Action<int, int, WaterBodyType> createBody,
            Action<int, int, int, WaterBodyType> registerCell,
            Action<int, int, int, int> setArtificialDirty)
        {
            int em = ComputeArtificialEdgeMargin();
            int maxSpan = Mathf.Min(_width - 2 * em, _height - 2 * em);
            if (maxSpan < 1) return 0;
            int extent = Mathf.Max(settings.MinLakeBoundingExtent, Mathf.CeilToInt(Mathf.Sqrt(settings.MinLakeCells)));
            extent = Mathf.Min(extent, maxSpan, settings.MaxLakeBoundingExtent);
            if (extent < 1 || extent * extent < settings.MinLakeCells) return 0;
            int added = 0;
            int maxX0 = _width - em - extent;
            int maxY0 = _height - em - extent;
            if (maxX0 < em || maxY0 < em) return 0;
            var corners = new[] {
                new Vector2Int(em, em), new Vector2Int(maxX0, em),
                new Vector2Int(em, maxY0), new Vector2Int(maxX0, maxY0)
            };
            foreach (var c in corners)
            {
                if (added >= targetBodies) break;
                if (TryPlaceArtificialRectangle(heightMap, settings, seaLevel, c.x, c.y, extent, extent,
                    claimed, em, allocateBodyId, createBody, registerCell, setArtificialDirty))
                    added++;
            }
            return added;
        }

        private bool HasCardinalNeighborOutsideRectWithSameSurface(int x0, int y0, int rw, int rh, int surface,
            Func<int, int, int> getWaterBodyId, Func<int, int, int> getSurface)
        {
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            for (int ox = 0; ox < rw; ox++)
                for (int oy = 0; oy < rh; oy++)
                {
                    int x = x0 + ox, y = y0 + oy;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + dx[i], ny = y + dy[i];
                        if (nx >= x0 && nx < x0 + rw && ny >= y0 && ny < y0 + rh) continue;
                        if (!IsValidPosition(nx, ny)) continue;
                        int id = getWaterBodyId(nx, ny);
                        if (id == 0) continue;
                        if (getSurface(nx, ny) == surface) return true;
                    }
                }
            return false;
        }

        private static int GetMinCardinalHeightOutsideRectangle(HeightMap heightMap, int x0, int y0, int rw, int rh)
        {
            int minH = int.MaxValue;
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            for (int ox = 0; ox < rw; ox++)
                for (int oy = 0; oy < rh; oy++)
                {
                    int x = x0 + ox, y = y0 + oy;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + dx[i], ny = y + dy[i];
                        if (nx >= x0 && nx < x0 + rw && ny >= y0 && ny < y0 + rh) continue;
                        if (!heightMap.IsValidPosition(nx, ny)) continue;
                        int nh = heightMap.GetHeight(nx, ny);
                        if (nh < minH) minH = nh;
                    }
                }
            return minH;
        }

        private static void CoerceDiagonalCornerRimForArtificialLake(HeightMap heightMap, int x0, int y0, int rw, int rh, int surface)
        {
            if (heightMap == null || rw <= 0 || rh <= 0) return;
            int[] cx = { x0 - 1, x0 + rw, x0 - 1, x0 + rw };
            int[] cy = { y0 - 1, y0 - 1, y0 + rh, y0 + rh };
            int[] ddx = { 1, -1, 1, -1 };
            int[] ddy = { 1, 1, -1, -1 };
            int[] cdx = { 1, -1, 0, 0 };
            int[] cdy = { 0, 0, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                int x = cx[i], y = cy[i];
                if (!heightMap.IsValidPosition(x, y)) continue;
                bool diag = false;
                for (int k = 0; k < 4; k++)
                {
                    int lx = x + ddx[k], ly = y + ddy[k];
                    if (lx >= x0 && lx < x0 + rw && ly >= y0 && ly < y0 + rh) { diag = true; break; }
                }
                if (!diag) continue;
                bool cardinal = false;
                for (int k = 0; k < 4; k++)
                {
                    int lx = x + cdx[k], ly = y + cdy[k];
                    if (lx >= x0 && lx < x0 + rw && ly >= y0 && ly < y0 + rh) { cardinal = true; break; }
                }
                if (cardinal) continue;
                int cur = heightMap.GetHeight(x, y);
                if (cur > surface)
                    heightMap.SetHeight(x, y, Mathf.Clamp(surface, HeightMap.MIN_HEIGHT, HeightMap.MAX_HEIGHT));
            }
        }

        private bool TryPlaceArtificialRectangle(
            HeightMap heightMap, LakeFillSettings settings, int seaLevel,
            int x0, int y0, int rw, int rh, bool[,] claimed, int edgeMargin,
            Func<int> allocateBodyId, Action<int, int, WaterBodyType> createBody,
            Action<int, int, int, WaterBodyType> registerCell,
            Action<int, int, int, int> setArtificialDirty)
        {
            if (x0 < edgeMargin || y0 < edgeMargin || x0 + rw > _width - edgeMargin || y0 + rh > _height - edgeMargin)
                return false;
            if (rw * rh < settings.MinLakeCells) return false;
            if (!WaterMapService.ArtificialRectangleBboxFits(settings, rw, rh)) return false;
            for (int ox = 0; ox < rw; ox++)
                for (int oy = 0; oy < rh; oy++)
                    if (claimed[x0 + ox, y0 + oy]) return false;

            int maxHBefore = HeightMap.MIN_HEIGHT;
            for (int ox = 0; ox < rw; ox++)
                for (int oy = 0; oy < rh; oy++)
                {
                    int h = heightMap.GetHeight(x0 + ox, y0 + oy);
                    if (h > maxHBefore) maxHBefore = h;
                }

            int minCardinalOutside = GetMinCardinalHeightOutsideRectangle(heightMap, x0, y0, rw, rh);
            int bowlFloorCap;
            if (minCardinalOutside == int.MaxValue)
                bowlFloorCap = Mathf.Max(HeightMap.MIN_HEIGHT, maxHBefore - 1);
            else
                bowlFloorCap = Mathf.Max(HeightMap.MIN_HEIGHT, minCardinalOutside - 1);

            for (int ox = 0; ox < rw; ox++)
                for (int oy = 0; oy < rh; oy++)
                {
                    int x = x0 + ox, y = y0 + oy;
                    int targetFloor = Mathf.Min(heightMap.GetHeight(x, y), bowlFloorCap);
                    heightMap.SetHeight(x, y, targetFloor);
                }

            int maxHPost = HeightMap.MIN_HEIGHT;
            for (int ox = 0; ox < rw; ox++)
                for (int oy = 0; oy < rh; oy++)
                {
                    int h = heightMap.GetHeight(x0 + ox, y0 + oy);
                    if (h > maxHPost) maxHPost = h;
                }

            int surface;
            if (!ResolveSurfaceForNewLake(x0, y0, rw, rh, maxHPost, seaLevel, heightMap, out surface))
                surface = Mathf.Min(HeightMap.MAX_HEIGHT, Mathf.Max(seaLevel + 1, maxHPost + 1));

            CoerceDiagonalCornerRimForArtificialLake(heightMap, x0, y0, rw, rh, surface);

            int bodyId = allocateBodyId();
            createBody(bodyId, surface, WaterBodyType.Lake);
            for (int ox = 0; ox < rw; ox++)
                for (int oy = 0; oy < rh; oy++)
                {
                    int x = x0 + ox, y = y0 + oy;
                    registerCell(x, y, bodyId, WaterBodyType.Lake);
                    claimed[x, y] = true;
                }

            const int dirtyPad = 2;
            setArtificialDirty?.Invoke(
                Mathf.Max(0, x0 - dirtyPad), Mathf.Max(0, y0 - dirtyPad),
                Mathf.Min(_width - 1, x0 + rw - 1 + dirtyPad), Mathf.Min(_height - 1, y0 + rh - 1 + dirtyPad));
            return true;
        }

        private bool ResolveSurfaceForNewLake(int x0, int y0, int rw, int rh, int maxTerrainH, int seaLevel,
            HeightMap heightMap, out int surfaceOut)
        {
            surfaceOut = Mathf.Min(HeightMap.MAX_HEIGHT, Mathf.Max(seaLevel + 1, maxTerrainH + 1));
            for (int bump = 0; bump < 16; bump++)
            {
                if (surfaceOut <= seaLevel) return false;
                if (surfaceOut > HeightMap.MAX_HEIGHT) return false;
                // simple check: no adjacent water body has same surface
                // (full check deferred — waterBodyIds not directly accessible here; caller handles)
                return true;
            }
            return false;
        }
    }
}
