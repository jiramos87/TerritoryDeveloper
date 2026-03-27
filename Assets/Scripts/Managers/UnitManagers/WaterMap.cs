using System;
using System.Collections.Generic;
using Territory.Core;
using UnityEngine;

namespace Territory.Terrain
{
    /// <summary>
    /// Stores water as an overlay on the height map: each cell has a water body id (0 = dry).
    /// Bodies carry a shared surface height; terrain depth is implicit (surface &gt; terrain floor).
    /// </summary>
    public sealed class WaterMap
    {
        public const int FormatVersionV2 = 2;

        /// <summary>V3: <see cref="WaterBodySerialized.bodyClassification"/> per body (FEAT-38). Loads still accept V2 without it.</summary>
        public const int FormatVersionV3 = 3;

        /// <summary>Neighbor outside the grid is treated as higher than any terrain cell (see <see cref="HeightMap"/> range 0–5).</summary>
        private const int OutsideMapSpillHeight = 6;

        /// <summary>Reserved id for player-painted water (does not collide with procedural lake ids 1..N).</summary>
        public const int LegacyPaintWaterBodyId = 10001;

        private int[,] waterBodyIds;
        private readonly Dictionary<int, WaterBody> bodies = new Dictionary<int, WaterBody>();
        private int nextBodyId = 1;

        private int width;
        private int height;

        /// <summary>First centerline cell per procedural river placed in <see cref="ProceduralRiverGenerator"/> (diagnostics).</summary>
        private readonly List<Vector2Int> proceduralRiverEntryAnchors = new List<Vector2Int>();

        /// <summary>After artificial lake fallback, region in heightmap coords to refresh terrain (inclusive). -1 if none.</summary>
        public int ArtificialDirtyMinX { get; private set; } = -1;
        public int ArtificialDirtyMinY { get; private set; } = -1;
        public int ArtificialDirtyMaxX { get; private set; } = -1;
        public int ArtificialDirtyMaxY { get; private set; } = -1;

        /// <summary>Populated after <see cref="InitializeLakesFromDepressionFill"/> for console diagnostics.</summary>
        public int LastLakeGenerationTargetBodies { get; private set; }
        public int LastLakeGenerationScaledBudget { get; private set; }
        public int LastLakeGenerationFinalBodyCount { get; private set; }
        public int LastLakeGenerationArtificialBodiesPlaced { get; private set; }
        public int LastLakeGenerationRecoveryPasses { get; private set; }
        public bool LastLakeGenerationMetTarget { get; private set; }
        public int LastLakeGenerationProceduralBodiesAfterBounded { get; private set; }

        public WaterMap(int width, int height)
        {
            this.width = width;
            this.height = height;
            waterBodyIds = new int[width, height];
        }

        public int Width => width;
        public int Height => height;

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public bool IsWater(int x, int y)
        {
            if (!IsValidPosition(x, y))
                return false;
            return waterBodyIds[x, y] != 0;
        }

        public int GetWaterBodyId(int x, int y)
        {
            if (!IsValidPosition(x, y))
                return 0;
            return waterBodyIds[x, y];
        }

        /// <summary>Returns -1 when the cell is not water.</summary>
        public int GetSurfaceHeightAt(int x, int y)
        {
            int id = GetWaterBodyId(x, y);
            if (id == 0)
                return -1;
            return bodies[id].SurfaceHeight;
        }

        public WaterBody GetWaterBody(int bodyId)
        {
            return bodyId != 0 && bodies.TryGetValue(bodyId, out WaterBody b) ? b : null;
        }

        /// <summary>Classification of the body at this cell, or <see cref="WaterBodyType.None"/> when dry.</summary>
        public WaterBodyType GetBodyClassificationAt(int x, int y)
        {
            int id = GetWaterBodyId(x, y);
            if (id == 0)
                return WaterBodyType.None;
            return bodies.TryGetValue(id, out WaterBody b) ? b.Classification : WaterBodyType.None;
        }

        /// <summary>
        /// When two registered water cells share a cardinal edge with <c>S_high &gt; S_low</c>, Pass A/B junction processing
        /// and water–water cascades are skipped if <b>either</b> body is a <see cref="WaterBodyType.Lake"/> (§12.7). River–river
        /// steps are unaffected; Sea is not treated as Lake for this rule.
        /// </summary>
        /// <param name="highX">Higher logical surface cell.</param>
        /// <param name="highY">Higher logical surface cell.</param>
        /// <param name="lowX">Lower logical surface neighbor (cardinally adjacent to the high cell).</param>
        /// <param name="lowY">Lower logical surface neighbor.</param>
        public bool IsLakeSurfaceStepContactForbidden(int highX, int highY, int lowX, int lowY)
        {
            if (!IsWater(highX, highY) || !IsWater(lowX, lowY))
                return false;
            int sH = GetSurfaceHeightAt(highX, highY);
            int sL = GetSurfaceHeightAt(lowX, lowY);
            if (sH <= sL)
                return false;
            WaterBodyType cH = GetBodyClassificationAt(highX, highY);
            WaterBodyType cL = GetBodyClassificationAt(lowX, lowY);
            return cH == WaterBodyType.Lake || cL == WaterBodyType.Lake;
        }

        /// <summary>
        /// BUG-45 / §12.7: When a <see cref="WaterBodyType.Lake"/> cell cardinally touches a <see cref="WaterBodyType.River"/>
        /// at a strictly lower logical surface, Pass A/B and water–water cascades are skipped
        /// (<see cref="IsLakeSurfaceStepContactForbidden"/>). This fallback removes those lake water cells and sets dry rim
        /// terrain height to the lake&apos;s logical <see cref="WaterBody.SurfaceHeight"/> <c>S</c> so the upper pool reads as a
        /// closed basin; <see cref="TerrainManager.RefreshLakeShoreAfterLakePlacement"/> then selects shore prefabs.
        /// Call after <see cref="ApplyWaterSurfaceJunctionMerge"/>, before <see cref="WaterManager.PlaceWater"/>.
        /// </summary>
        /// <param name="restoredCells">Cells converted from lake water to dry (for terrain restore and post–lake-shore height fix).</param>
        /// <returns><c>true</c> if any cell was converted.</returns>
        public bool ApplyLakeHighToRiverLowContactFallback(HeightMap heightMap, GridManager gridManager, out List<(int x, int y, int lakeSurface)> restoredCells)
        {
            restoredCells = new List<(int, int, int)>();
            if (heightMap == null)
                return false;

            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            var candidates = new List<(int x, int y, int sLake)>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!IsWater(x, y))
                        continue;
                    if (GetBodyClassificationAt(x, y) != WaterBodyType.Lake)
                        continue;
                    int sLake = GetSurfaceHeightAt(x, y);
                    if (sLake < 0)
                        continue;

                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + d4x[i];
                        int ny = y + d4y[i];
                        if (!IsValidPosition(nx, ny) || !IsWater(nx, ny))
                            continue;
                        if (GetBodyClassificationAt(nx, ny) != WaterBodyType.River)
                            continue;
                        int sRiver = GetSurfaceHeightAt(nx, ny);
                        if (sRiver < 0 || sRiver >= sLake)
                            continue;

                        candidates.Add((x, y, sLake));
                        break;
                    }
                }
            }

            if (candidates.Count == 0)
                return false;

            bool any = false;
            foreach (var (x, y, sLake) in candidates)
            {
                if (!IsWater(x, y) || GetBodyClassificationAt(x, y) != WaterBodyType.Lake)
                    continue;

                ClearWaterAt(x, y);
                int clampedSurface = Mathf.Clamp(sLake, TerrainManager.MIN_HEIGHT, TerrainManager.MAX_HEIGHT);
                heightMap.SetHeight(x, y, clampedSurface);
                if (gridManager != null)
                    gridManager.SetCellHeight(new Vector2(x, y), heightMap.GetHeight(x, y));

                restoredCells.Add((x, y, sLake));
                any = true;
            }

            return any;
        }

        public IReadOnlyDictionary<int, WaterBody> GetBodies()
        {
            return bodies;
        }

        /// <summary>Ordered entry anchors for rivers generated this session (map coordinates).</summary>
        public IReadOnlyList<Vector2Int> GetProceduralRiverEntryAnchors()
        {
            return proceduralRiverEntryAnchors;
        }

        /// <summary>Records the entry centerline cell for one procedural river pass (FEAT-38 diagnostics).</summary>
        public void RecordProceduralRiverEntryAnchor(int x, int y)
        {
            if (IsValidPosition(x, y))
                proceduralRiverEntryAnchors.Add(new Vector2Int(x, y));
        }

        /// <summary>
        /// Inclusive axis-aligned bounding box of every cell with a non-zero water body id.
        /// Returns <c>false</c> when the map has no water.
        /// </summary>
        public bool TryGetAllWaterBoundingBox(out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = minY = maxX = maxY = 0;
            bool found = false;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (waterBodyIds[x, y] == 0)
                        continue;
                    if (!found)
                    {
                        minX = maxX = x;
                        minY = maxY = y;
                        found = true;
                    }
                    else
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            return found;
        }

        /// <summary>Rebuilds water from saved <see cref="CellData"/> (legacy sea-level mask + Water zone).</summary>
        public void RestoreFromLegacyCellData(List<CellData> gridData, int seaLevel)
        {
            ClearAllWater();
            if (gridData == null)
                return;

            const int legacyId = 1;
            var body = new WaterBody(legacyId, seaLevel, WaterBodyType.Lake);
            bodies[legacyId] = body;
            nextBodyId = 2;

            foreach (CellData cd in gridData)
            {
                if (!IsValidPosition(cd.x, cd.y))
                    continue;
                bool isWater = cd.height <= seaLevel
                    || (cd.zoneType != null && cd.zoneType.Equals("Water", StringComparison.OrdinalIgnoreCase));
                if (!isWater)
                    continue;
                waterBodyIds[cd.x, cd.y] = legacyId;
                body.AddCellIndex(ToFlat(cd.x, cd.y));
            }
        }

        /// <summary>Single-cell water from the paint tool at <paramref name="surfaceHeight"/> (or sea-level registration).</summary>
        /// <param name="classification">Sea for sea-level fill; Lake for matrix/paint defaults.</param>
        public void AddLegacyPaintedWaterCell(int x, int y, int surfaceHeight, WaterBodyType classification = WaterBodyType.Lake)
        {
            if (!IsValidPosition(x, y))
                return;

            int legacyBodyId = LegacyPaintWaterBodyId;
            EnsureBody(legacyBodyId, surfaceHeight, classification);
            int oldId = waterBodyIds[x, y];
            if (oldId != 0 && oldId != legacyBodyId)
                RemoveCellFromBody(x, y, oldId);

            int flat = ToFlat(x, y);
            waterBodyIds[x, y] = legacyBodyId;
            bodies[legacyBodyId].SurfaceHeight = surfaceHeight;
            bodies[legacyBodyId].AddCellIndex(flat);
            nextBodyId = Math.Max(nextBodyId, legacyBodyId + 1);
        }

        public void ClearWaterAt(int x, int y)
        {
            if (!IsValidPosition(x, y))
                return;
            int id = waterBodyIds[x, y];
            if (id == 0)
                return;
            RemoveCellFromBody(x, y, id);
        }

        private void RemoveCellFromBody(int x, int y, int bodyId)
        {
            int flat = ToFlat(x, y);
            waterBodyIds[x, y] = 0;
            if (bodies.TryGetValue(bodyId, out WaterBody body))
            {
                body.RemoveCellIndex(flat);
                if (body.CellCount == 0)
                    bodies.Remove(bodyId);
            }
        }

        /// <summary>
        /// BUG-45 / multi-body contact: For each water cell on the <b>higher</b> logical surface that cardinally touches
        /// water at a <b>strictly lower</b> surface, sets <see cref="HeightMap"/> bed height to the <b>minimum</b> bed
        /// among those lower-surface neighbors only. Does <b>not</b> change <c>waterBodyIds</c>, merge bodies, or fill dry cells.
        /// Skips edges where <see cref="IsLakeSurfaceStepContactForbidden"/> applies (§12.7). Idempotent; safe to run from
        /// <see cref="WaterManager.UpdateWaterVisuals"/> before <c>PlaceWater</c>.
        /// </summary>
        /// <returns><c>true</c> if any cell height was written.</returns>
        private bool AlignUpperSurfaceContactBorderBedHeightsOnce(HeightMap hm)
        {
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            bool any = false;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!IsWater(x, y))
                        continue;
                    int sHere = GetSurfaceHeightAt(x, y);
                    if (sHere < 0)
                        continue;
                    int targetBed = int.MaxValue;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + d4x[i];
                        int ny = y + d4y[i];
                        if (!IsValidPosition(nx, ny) || !IsWater(nx, ny))
                            continue;
                        int sN = GetSurfaceHeightAt(nx, ny);
                        if (sN < 0 || sN >= sHere)
                            continue;
                        if (IsLakeSurfaceStepContactForbidden(x, y, nx, ny))
                            continue;
                        targetBed = Mathf.Min(targetBed, hm.GetHeight(nx, ny));
                    }

                    if (targetBed == int.MaxValue)
                        continue;
                    int clamped = Mathf.Clamp(targetBed, TerrainManager.MIN_HEIGHT, TerrainManager.MAX_HEIGHT);
                    if (hm.GetHeight(x, y) == clamped)
                        continue;
                    hm.SetHeight(x, y, clamped);
                    any = true;
                }
            }

            return any;
        }

        /// <summary>
        /// Aligns <see cref="HeightMap"/> beds only on the cardinal contact strip where a higher logical water surface
        /// meets a lower one (see §12.7). Does not reclassify cells or merge bodies.
        /// Runs multiple passes so minimum lower-pool beds propagate along the contact strip (junction width).
        /// Call from <see cref="WaterManager.UpdateWaterVisuals"/> when a <see cref="HeightMap"/> is available.
        /// </summary>
        public void ApplyMultiBodySurfaceBoundaryNormalization(HeightMap heightMap)
        {
            if (heightMap == null)
                return;
            const int maxPassAIterations = 24;
            for (int i = 0; i < maxPassAIterations; i++)
            {
                if (!AlignUpperSurfaceContactBorderBedHeightsOnce(heightMap))
                    break;
            }
        }

        /// <summary>
        /// BUG-45 Pass B: extends the lower logical surface along cardinal contacts where <c>S_high &gt; S_low</c>,
        /// so <see cref="PlaceWater"/> and cascade cliffs see continuous lower pool geometry.
        /// Absorbs dry land (including former water-shore) in the strip away from the high cell, and dry cells on the
        /// <b>upper-bank</b> perpendicular to the contact (beside each high cell at the junction) so the lower body
        /// can replace shore wedges between two surface levels. The next <see cref="WaterManager.UpdateWaterVisuals"/>
        /// pass replaces terrain with open water where absorbed.
        /// After dry absorption, <b>contact-bed reassignment</b> moves upper-pool <b>water</b> cells cardinally touching
        /// lower-surface water into that neighbor’s body (§12.7): if the upper bed is still above the neighbor’s bed, it is
        /// lowered to match first, then <c>waterBodyId</c> is updated so logical <c>S</c> matches the lecho.
        /// Assigns <see cref="waterBodyIds"/> to an existing body at <paramref name="sLow"/> (deterministic: contact cell’s id;
        /// does not merge two lower bodies). Idempotent. Updates <see cref="HeightMap"/> beds; sync <see cref="GridManager.SetCellHeight"/>
        /// when <paramref name="gridManager"/> is non-null.
        /// </summary>
        /// <returns><c>true</c> if any cell or height changed.</returns>
        public bool ApplyWaterSurfaceJunctionMerge(HeightMap heightMap, GridManager gridManager, out int dirtyMinX, out int dirtyMinY, out int dirtyMaxX, out int dirtyMaxY)
        {
            dirtyMinX = dirtyMinY = dirtyMaxX = dirtyMaxY = 0;
            if (heightMap == null)
                return false;

            const int maxStripDepthCap = 5;
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };

            bool anyChange = false;
            bool haveBounds = false;
            int dMinX = 0, dMinY = 0, dMaxX = 0, dMaxY = 0;
            var absorbClaims = new Dictionary<Vector2Int, (int bodyId, int sLow)>();

            void ExpandDirty(int x, int y)
            {
                if (!haveBounds)
                {
                    dMinX = dMaxX = x;
                    dMinY = dMaxY = y;
                    haveBounds = true;
                }
                else
                {
                    if (x < dMinX) dMinX = x;
                    if (x > dMaxX) dMaxX = x;
                    if (y < dMinY) dMinY = y;
                    if (y > dMaxY) dMaxY = y;
                }
            }

            void ProposeAbsorb(int cx, int cy, int lowerBodyId, int sLow)
            {
                var key = new Vector2Int(cx, cy);
                if (!absorbClaims.TryGetValue(key, out var prev))
                    absorbClaims[key] = (lowerBodyId, sLow);
                else if (sLow < prev.sLow)
                    absorbClaims[key] = (lowerBodyId, sLow);
                else if (sLow > prev.sLow)
                    return;
                else if (lowerBodyId < prev.bodyId)
                    absorbClaims[key] = (lowerBodyId, sLow);
            }

            for (int hx = 0; hx < width; hx++)
            {
                for (int hy = 0; hy < height; hy++)
                {
                    if (!IsWater(hx, hy))
                        continue;
                    int sHigh = GetSurfaceHeightAt(hx, hy);
                    if (sHigh < 0)
                        continue;

                    for (int i = 0; i < 4; i++)
                    {
                        int lx = hx + d4x[i];
                        int ly = hy + d4y[i];
                        if (!IsValidPosition(lx, ly) || !IsWater(lx, ly))
                            continue;
                        int sLow = GetSurfaceHeightAt(lx, ly);
                        if (sLow < 0 || sHigh <= sLow)
                            continue;
                        if (IsLakeSurfaceStepContactForbidden(hx, hy, lx, ly))
                            continue;

                        int lowerBodyId = GetWaterBodyId(lx, ly);
                        if (lowerBodyId == 0 || !bodies.TryGetValue(lowerBodyId, out WaterBody lowerBody) || lowerBody.SurfaceHeight != sLow)
                            continue;

                        int stepX = lx - hx;
                        int stepY = ly - hy;
                        int intoUpperX = hx - lx;
                        int intoUpperY = hy - ly;
                        int maxDepth = Mathf.Min(maxStripDepthCap, CountUpperExtentAlongStep(hx, hy, intoUpperX, intoUpperY, sHigh, GetWaterBodyId(hx, hy)));

                        for (int k = 1; k <= maxDepth; k++)
                        {
                            int cx = lx + stepX * k;
                            int cy = ly + stepY * k;
                            if (!IsValidPosition(cx, cy))
                                break;
                            if (IsWater(cx, cy))
                            {
                                int sC = GetSurfaceHeightAt(cx, cy);
                                if (sC == sLow)
                                    break;
                                break;
                            }

                            ProposeAbsorb(cx, cy, lowerBodyId, sLow);
                        }

                        // Upper-bank wedge: dry cells perpendicular to the high→low step (beside the high cell at the contact).
                        // Lets the lower body take shore tiles that sit between the two surface levels along the junction.
                        int ddx = lx - hx;
                        int ddy = ly - hy;
                        int p1x = -ddy;
                        int p1y = ddx;
                        int p2x = ddy;
                        int p2y = -ddx;
                        for (int pi = 0; pi < 2; pi++)
                        {
                            int px = pi == 0 ? p1x : p2x;
                            int py = pi == 0 ? p1y : p2y;
                            int ux = hx + px;
                            int uy = hy + py;
                            if (!IsValidPosition(ux, uy))
                                continue;
                            if (IsWater(ux, uy))
                                continue;
                            ProposeAbsorb(ux, uy, lowerBodyId, sLow);
                        }
                    }
                }
            }

            foreach (var kv in absorbClaims)
            {
                int cx = kv.Key.x;
                int cy = kv.Key.y;
                int bodyId = kv.Value.bodyId;
                int sLow = kv.Value.sLow;
                if (!TryAbsorbDryCellIntoLowerBody(cx, cy, bodyId, sLow, heightMap, gridManager, out bool cellChanged))
                    continue;
                if (cellChanged)
                {
                    anyChange = true;
                    ExpandDirty(cx, cy);
                }
            }

            const int maxContactReassignIterations = 24;
            for (int iter = 0; iter < maxContactReassignIterations; iter++)
            {
                if (!TryReassignUpperWaterCellsMatchingLowerContactBed(heightMap, gridManager, ExpandDirty))
                    break;
                anyChange = true;
            }

            if (haveBounds)
            {
                const int pad = 2;
                dirtyMinX = Mathf.Max(0, dMinX - pad);
                dirtyMinY = Mathf.Max(0, dMinY - pad);
                dirtyMaxX = Mathf.Min(width - 1, dMaxX + pad);
                dirtyMaxY = Mathf.Min(height - 1, dMaxY + pad);
            }

            return anyChange;
        }

        /// <summary>
        /// One sweep: for each water cell on the higher side of a cardinal <c>S_high &gt; S_low</c> contact, pick a
        /// lower-surface neighbor (not lake-forbidden) whose bed is not above this cell’s bed. If this cell’s bed is
        /// still higher than the neighbor’s, align it to the neighbor’s bed first, then reassign <c>waterBodyId</c> to
        /// the lower body. When multiple lower neighbors qualify, prefers the lowest <c>S_low</c> then the lowest body id.
        /// </summary>
        private bool TryReassignUpperWaterCellsMatchingLowerContactBed(HeightMap hm, GridManager grid, System.Action<int, int> expandDirty)
        {
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            bool any = false;

            for (int hx = 0; hx < width; hx++)
            {
                for (int hy = 0; hy < height; hy++)
                {
                    if (!IsWater(hx, hy))
                        continue;
                    int sHigh = GetSurfaceHeightAt(hx, hy);
                    if (sHigh < 0)
                        continue;

                    int bestSl = int.MaxValue;
                    int bestBid = int.MaxValue;
                    int bestLx = -1;
                    int bestLy = -1;

                    for (int i = 0; i < 4; i++)
                    {
                        int lx = hx + d4x[i];
                        int ly = hy + d4y[i];
                        if (!IsValidPosition(lx, ly) || !IsWater(lx, ly))
                            continue;
                        int sLow = GetSurfaceHeightAt(lx, ly);
                        if (sLow < 0 || sHigh <= sLow)
                            continue;
                        if (IsLakeSurfaceStepContactForbidden(hx, hy, lx, ly))
                            continue;
                        int hH = hm.GetHeight(hx, hy);
                        int hL = hm.GetHeight(lx, ly);
                        if (hH < hL)
                            continue;
                        int bid = GetWaterBodyId(lx, ly);
                        if (bid == 0 || !bodies.TryGetValue(bid, out WaterBody lb) || lb.SurfaceHeight != sLow)
                            continue;

                        if (sLow < bestSl || (sLow == bestSl && bid < bestBid))
                        {
                            bestSl = sLow;
                            bestBid = bid;
                            bestLx = lx;
                            bestLy = ly;
                        }
                    }

                    if (bestLx < 0)
                        continue;
                    int highId = GetWaterBodyId(hx, hy);
                    if (highId == bestBid)
                        continue;

                    if (TryReassignSingleUpperWaterCellToLowerBody(hx, hy, bestLx, bestLy, hm, grid))
                    {
                        any = true;
                        expandDirty?.Invoke(hx, hy);
                    }
                }
            }

            return any;
        }

        /// <summary>
        /// Moves one upper-pool water cell to the lower neighbor’s body. When the upper bed is still above the lower
        /// cell’s bed, aligns <see cref="HeightMap"/> (and <see cref="GridManager.SetCellHeight"/>) to the lower bed first.
        /// </summary>
        private bool TryReassignSingleUpperWaterCellToLowerBody(int hx, int hy, int lx, int ly, HeightMap hm, GridManager grid)
        {
            int sHigh = GetSurfaceHeightAt(hx, hy);
            int sLow = GetSurfaceHeightAt(lx, ly);
            if (sLow < 0 || sHigh <= sLow)
                return false;
            if (IsLakeSurfaceStepContactForbidden(hx, hy, lx, ly))
                return false;

            int hH = hm.GetHeight(hx, hy);
            int hL = hm.GetHeight(lx, ly);
            if (hH < hL)
                return false;
            if (hH > hL)
            {
                int clamped = Mathf.Clamp(hL, TerrainManager.MIN_HEIGHT, TerrainManager.MAX_HEIGHT);
                hm.SetHeight(hx, hy, clamped);
                if (grid != null)
                    grid.SetCellHeight(new Vector2(hx, hy), hm.GetHeight(hx, hy));
            }

            int lowId = GetWaterBodyId(lx, ly);
            int highId = GetWaterBodyId(hx, hy);
            if (lowId == 0 || highId == lowId)
                return false;
            if (!bodies.TryGetValue(lowId, out WaterBody lowerBody) || lowerBody.SurfaceHeight != sLow)
                return false;

            RemoveCellFromBody(hx, hy, highId);
            waterBodyIds[hx, hy] = lowId;
            lowerBody.AddCellIndex(ToFlat(hx, hy));

            if (grid != null)
                grid.SetCellHeight(new Vector2(hx, hy), hm.GetHeight(hx, hy));

            return true;
        }

        /// <summary>
        /// Counts consecutive cells from <paramref name="startX"/>,<paramref name="startY"/> stepping by
        /// <paramref name="stepX"/>,<paramref name="stepY"/> (unit cardinal) that share <paramref name="surface"/> and <paramref name="bodyId"/>.
        /// </summary>
        private int CountUpperExtentAlongStep(int startX, int startY, int stepX, int stepY, int surface, int bodyId)
        {
            int count = 0;
            int cx = startX;
            int cy = startY;
            while (IsValidPosition(cx, cy) && IsWater(cx, cy) && GetWaterBodyId(cx, cy) == bodyId && GetSurfaceHeightAt(cx, cy) == surface)
            {
                count++;
                cx += stepX;
                cy += stepY;
            }
            return Mathf.Max(1, count);
        }

        /// <summary>
        /// Registers a dry cell on the lower surface body and aligns its bed to adjacent lower-surface water.
        /// Does not take cells from other water bodies. Skips cells with a placed building.
        /// </summary>
        private bool TryAbsorbDryCellIntoLowerBody(int x, int y, int targetBodyId, int sLow, HeightMap hm, GridManager grid, out bool changed)
        {
            changed = false;
            if (!IsValidPosition(x, y) || !bodies.TryGetValue(targetBodyId, out WaterBody targetBody) || targetBody.SurfaceHeight != sLow)
                return false;

            int existingId = waterBodyIds[x, y];
            if (existingId != 0)
            {
                if (existingId == targetBodyId)
                    return true;
                if (GetSurfaceHeightAt(x, y) == sLow)
                    return false;
                return false;
            }

            if (grid != null)
            {
                Cell cell = grid.GetCell(x, y);
                if (cell != null && cell.occupiedBuilding != null)
                    return false;
            }

            int targetBed = ProposeLowerJunctionBedHeight(x, y, sLow, hm);
            int maxBed = Mathf.Min(TerrainManager.MAX_HEIGHT, sLow - 1);
            if (maxBed < TerrainManager.MIN_HEIGHT)
                return false;
            targetBed = Mathf.Clamp(targetBed, TerrainManager.MIN_HEIGHT, maxBed);

            hm.SetHeight(x, y, targetBed);
            int flat = ToFlat(x, y);
            waterBodyIds[x, y] = targetBodyId;
            targetBody.AddCellIndex(flat);
            changed = true;

            if (grid != null)
                grid.SetCellHeight(new Vector2(x, y), targetBed);

            return true;
        }

        /// <summary>Minimum bed among cardinal neighbors that are water at <paramref name="sLow"/>; otherwise current terrain height.</summary>
        private int ProposeLowerJunctionBedHeight(int x, int y, int sLow, HeightMap hm)
        {
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            int best = int.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                int nx = x + d4x[i];
                int ny = y + d4y[i];
                if (!IsValidPosition(nx, ny) || !IsWater(nx, ny) || GetSurfaceHeightAt(nx, ny) != sLow)
                    continue;
                best = Mathf.Min(best, hm.GetHeight(nx, ny));
            }

            if (best == int.MaxValue)
                return hm.GetHeight(x, y);
            return best;
        }

        private void EnsureBody(int id, int surfaceHeight, WaterBodyType classification = WaterBodyType.Lake)
        {
            if (!bodies.TryGetValue(id, out WaterBody body))
            {
                body = new WaterBody(id, surfaceHeight, classification);
                bodies[id] = body;
                nextBodyId = Math.Max(nextBodyId, id + 1);
            }
            else
            {
                body.SurfaceHeight = surfaceHeight;
            }
        }

        /// <summary>
        /// Fills natural depressions in the height map with lake bodies (FEAT-37a).
        /// </summary>
        /// <param name="seaLevelForArtificialFallback">Used when carving fallback lakes so surface height stays above sea.</param>
        public void InitializeLakesFromDepressionFill(HeightMap heightMap, LakeFillSettings settings, int seaLevelForArtificialFallback = 0)
        {
            ClearAllWater();
            ArtificialDirtyMinX = -1;
            ArtificialDirtyMinY = -1;
            ArtificialDirtyMaxX = -1;
            ArtificialDirtyMaxY = -1;
            LastLakeGenerationArtificialBodiesPlaced = 0;
            LastLakeGenerationRecoveryPasses = 0;
            LastLakeGenerationMetTarget = false;
            if (heightMap == null || settings == null)
                return;

            int areaScaledBudget = settings.GetAreaScaledLakeBudgetDiagnostic(width, height);
            int effectiveMaxLakeBodies = settings.GetEffectiveMaxLakeBodies(width, height);

            LastLakeGenerationTargetBodies = effectiveMaxLakeBodies;
            LastLakeGenerationScaledBudget = areaScaledBudget;

            int randomExtraAttempts = settings.GetScaledRandomExtraSeedAttempts(width, height);

            var rnd = new System.Random(settings.RandomSeed);
            var seedSet = new HashSet<Vector2Int>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (IsStrictLocalMinimum(x, y, heightMap))
                        seedSet.Add(new Vector2Int(x, y));
                    if (IsLocalMinimumInWindow(x, y, heightMap, settings.LocalMinWindowRadius))
                        seedSet.Add(new Vector2Int(x, y));
                }
            }

            for (int i = 0; i < randomExtraAttempts; i++)
                seedSet.Add(new Vector2Int(rnd.Next(width), rnd.Next(height)));

            var minima = new List<Vector2Int>(seedSet);
            SortSeedCandidatesBySpillHeadroom(minima, heightMap, rnd);

            var claimed = new bool[width, height];
            int bodiesCreated = 0;
            foreach (var seed in minima)
            {
                if (claimed[seed.x, seed.y])
                    continue;
                if (bodiesCreated >= effectiveMaxLakeBodies)
                    break;

                int spill = GetPlateauSpillHeight(seed.x, seed.y, heightMap);
                int seedH = heightMap.GetHeight(seed.x, seed.y);
                if (spill <= seedH)
                    continue;

                if (rnd.NextDouble() > settings.LakeAcceptProbability)
                    continue;

                var basinCells = new List<Vector2Int>();
                CollectBasin(seed.x, seed.y, spill, heightMap, basinCells);
                if (basinCells.Count < settings.MinLakeCells)
                    continue;
                if (!LakeBoundingBoxFits(settings, basinCells))
                    continue;

                bool overlaps = false;
                foreach (var c in basinCells)
                {
                    if (claimed[c.x, c.y])
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (overlaps)
                    continue;

                int bodyId = nextBodyId++;
                var body = new WaterBody(bodyId, spill, WaterBodyType.Lake);
                bodies[bodyId] = body;

                foreach (var c in basinCells)
                {
                    waterBodyIds[c.x, c.y] = bodyId;
                    body.AddCellIndex(ToFlat(c.x, c.y));
                    claimed[c.x, c.y] = true;
                }

                bodiesCreated++;
            }

            if (settings.RunBoundedLocalDepressionPass)
                TryFillBoundedLocalDepressions(heightMap, settings, rnd, claimed, ref bodiesCreated, effectiveMaxLakeBodies);

            LastLakeGenerationProceduralBodiesAfterBounded = bodies.Count;

            RunMergeAdjacentBodiesWithSameSurface();

            // Artificial fallback and corner last resort run only after procedural depression-fill (main + bounded) and merge
            // still leave the map short of the scaled lake budget — not as a primary generator.
            if (bodies.Count < effectiveMaxLakeBodies)
            {
                const int maxRecoveryPasses = 48;
                int totalArtificial = 0;
                int pass = 0;
                while (bodies.Count < effectiveMaxLakeBodies && pass < maxRecoveryPasses)
                {
                    int added = TryArtificialLakeFallback(heightMap, settings, seaLevelForArtificialFallback, effectiveMaxLakeBodies, claimed, rnd);
                    totalArtificial += added;
                    RunMergeAdjacentBodiesWithSameSurface();
                    pass++;
                    LastLakeGenerationRecoveryPasses = pass;
                    if (bodies.Count >= effectiveMaxLakeBodies)
                        break;
                    if (added == 0)
                        break;
                }

                if (bodies.Count < effectiveMaxLakeBodies)
                {
                    int cornerAdded = TryLastResortCornerArtificialLakes(heightMap, settings, seaLevelForArtificialFallback, effectiveMaxLakeBodies, claimed);
                    totalArtificial += cornerAdded;
                    RunMergeAdjacentBodiesWithSameSurface();
                }

                LastLakeGenerationArtificialBodiesPlaced = totalArtificial;
            }

            LastLakeGenerationFinalBodyCount = bodies.Count;
            LastLakeGenerationMetTarget = bodies.Count >= effectiveMaxLakeBodies;
        }

        /// <summary>
        /// Orders candidates by spill headroom (descending), then terrain height, then random tie-break (BUG-36).
        /// </summary>
        private void SortSeedCandidatesBySpillHeadroom(List<Vector2Int> cells, HeightMap heightMap, System.Random rnd)
        {
            int n = cells.Count;
            if (n <= 1)
                return;

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

        /// <summary>
        /// Second pass: seeds that are minima in a larger window (lakes between plateaus), same spill/basin rules, optional cap via bbox.
        /// </summary>
        private void TryFillBoundedLocalDepressions(HeightMap heightMap, LakeFillSettings settings, System.Random rnd, bool[,] claimed, ref int bodiesCreated, int effectiveMaxLakeBodies)
        {
            var extraSeeds = new List<Vector2Int>();
            int r = settings.BoundedLocalDepressionWindowRadius;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (claimed[x, y])
                        continue;
                    if (!IsLocalMinimumInWindow(x, y, heightMap, r))
                        continue;
                    extraSeeds.Add(new Vector2Int(x, y));
                }
            }

            SortSeedCandidatesBySpillHeadroom(extraSeeds, heightMap, rnd);

            foreach (var seed in extraSeeds)
            {
                if (claimed[seed.x, seed.y])
                    continue;
                if (bodiesCreated >= effectiveMaxLakeBodies)
                    break;

                int spill = GetPlateauSpillHeight(seed.x, seed.y, heightMap);
                int seedH = heightMap.GetHeight(seed.x, seed.y);
                if (spill <= seedH)
                    continue;

                if (rnd.NextDouble() > settings.LakeAcceptProbability * settings.BoundedLocalDepressionAcceptScale)
                    continue;

                var basinCells = new List<Vector2Int>();
                CollectBasin(seed.x, seed.y, spill, heightMap, basinCells);
                if (basinCells.Count < settings.MinLakeCells)
                    continue;
                if (basinCells.Count > settings.BoundedLocalDepressionMaxBasinCells)
                    continue;
                if (!LakeBoundingBoxFits(settings, basinCells))
                    continue;

                bool overlaps = false;
                foreach (var c in basinCells)
                {
                    if (claimed[c.x, c.y])
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (overlaps)
                    continue;

                int bodyId = nextBodyId++;
                var body = new WaterBody(bodyId, spill, WaterBodyType.Lake);
                bodies[bodyId] = body;

                foreach (var c in basinCells)
                {
                    waterBodyIds[c.x, c.y] = bodyId;
                    body.AddCellIndex(ToFlat(c.x, c.y));
                    claimed[c.x, c.y] = true;
                }

                bodiesCreated++;
            }
        }

        /// <summary>True if an artificial rectangle's width/height match procedural <see cref="LakeBoundingBoxFits"/> axis limits.</summary>
        private static bool ArtificialRectangleBboxFits(LakeFillSettings settings, int rw, int rh)
        {
            return rw >= settings.MinLakeBoundingExtent && rw <= settings.MaxLakeBoundingExtent
                && rh >= settings.MinLakeBoundingExtent && rh <= settings.MaxLakeBoundingExtent;
        }

        /// <summary>True if axis-aligned bounds of the lake (in grid cells) are within configured min/max extent on both axes.</summary>
        private static bool LakeBoundingBoxFits(LakeFillSettings settings, List<Vector2Int> basinCells)
        {
            if (basinCells == null || basinCells.Count == 0)
                return false;
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var c in basinCells)
            {
                if (c.x < minX) minX = c.x;
                if (c.x > maxX) maxX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.y > maxY) maxY = c.y;
            }

            int bw = maxX - minX + 1;
            int bh = maxY - minY + 1;
            return bw >= settings.MinLakeBoundingExtent && bw <= settings.MaxLakeBoundingExtent
                && bh >= settings.MinLakeBoundingExtent && bh <= settings.MaxLakeBoundingExtent;
        }

        /// <summary>
        /// After lake depression-fill, marks cells at or below <paramref name="seaLevel"/> that are still dry.
        /// Matches terrain sea placement (<c>PlaceSeaLevelWater</c>) so <see cref="WaterManager.IsWaterAt"/> and minimap stay consistent.
        /// </summary>
        public void MergeSeaLevelDryCellsFromHeightMap(HeightMap heightMap, int seaLevel)
        {
            if (heightMap == null)
                return;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!heightMap.IsValidPosition(x, y))
                        continue;
                    if (heightMap.GetHeight(x, y) > seaLevel)
                        continue;
                    if (IsWater(x, y))
                        continue;
                    AddLegacyPaintedWaterCell(x, y, seaLevel, WaterBodyType.Sea);
                }
            }
        }

        /// <summary>Legacy: all cells at or below sea level become one body (id 1) at <paramref name="seaLevel"/> surface.</summary>
        public void InitializeWaterBodiesBasedOnHeight(HeightMap heightMap, int seaLevel)
        {
            ClearAllWater();
            if (heightMap == null)
                return;

            const int legacyId = 1;
            var body = new WaterBody(legacyId, seaLevel, WaterBodyType.Sea);
            bodies[legacyId] = body;
            nextBodyId = 2;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (heightMap.GetHeight(x, y) <= seaLevel)
                    {
                        waterBodyIds[x, y] = legacyId;
                        body.AddCellIndex(ToFlat(x, y));
                    }
                }
            }
        }

        public WaterMapData GetSerializableData()
        {
            var data = new WaterMapData
            {
                formatVersion = FormatVersionV3,
                width = width,
                height = height,
                waterBodyIds = new int[width * height]
            };

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    data.waterBodyIds[x + y * width] = waterBodyIds[x, y];
                }
            }

            var list = new List<WaterBodySerialized>();
            foreach (var kv in bodies)
            {
                var wb = kv.Value;
                var ser = new WaterBodySerialized
                {
                    id = wb.Id,
                    surfaceHeight = wb.SurfaceHeight,
                    bodyClassification = (int)wb.Classification,
                    cellIndicesFlat = new int[wb.CellIndices.Count]
                };
                int i = 0;
                foreach (int idx in wb.CellIndices)
                    ser.cellIndicesFlat[i++] = idx;
                list.Add(ser);
            }
            data.bodies = list.ToArray();
            return data;
        }

        public void LoadFromSerializableData(WaterMapData data)
        {
            if (data == null)
                return;

            if (data.formatVersion < FormatVersionV2 && data.waterCells != null && data.waterCells.Length == width * height)
            {
                LoadLegacyBoolFormat(data);
                return;
            }

            if (data.waterBodyIds == null || data.waterBodyIds.Length != width * height)
                return;

            ClearAllWater();
            for (int i = 0; i < data.waterBodyIds.Length; i++)
            {
                int x = i % width;
                int y = i / width;
                waterBodyIds[x, y] = data.waterBodyIds[i];
            }

            bodies.Clear();
            nextBodyId = 1;
            if (data.bodies != null)
            {
                foreach (var ser in data.bodies)
                {
                    WaterBodyType cls = DeserializeBodyClassification(ser);
                    var body = new WaterBody(ser.id, ser.surfaceHeight, cls);
                    if (ser.cellIndicesFlat != null)
                    {
                        foreach (int flat in ser.cellIndicesFlat)
                            body.AddCellIndex(flat);
                    }
                    bodies[ser.id] = body;
                    nextBodyId = Math.Max(nextBodyId, ser.id + 1);
                }
            }

            RebuildBodyIdsFromCellsIfNeeded();
        }

        private static WaterBodyType DeserializeBodyClassification(WaterBodySerialized ser)
        {
            if (ser == null)
                return WaterBodyType.Lake;
            WaterBodyType cls = (WaterBodyType)ser.bodyClassification;
            if (cls == WaterBodyType.None)
                return WaterBodyType.Lake;
            return cls;
        }

        private void LoadLegacyBoolFormat(WaterMapData data)
        {
            ClearAllWater();
            const int legacyId = 1;
            int surface = 0;
            var body = new WaterBody(legacyId, surface, WaterBodyType.Lake);
            bodies[legacyId] = body;
            nextBodyId = 2;

            for (int i = 0; i < data.waterCells.Length; i++)
            {
                if (!data.waterCells[i])
                    continue;
                int x = i % width;
                int y = i / width;
                waterBodyIds[x, y] = legacyId;
                body.AddCellIndex(ToFlat(x, y));
            }
        }

        private void RebuildBodyIdsFromCellsIfNeeded()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int id = waterBodyIds[x, y];
                    if (id == 0)
                        continue;
                    if (!bodies.ContainsKey(id))
                        waterBodyIds[x, y] = 0;
                }
            }
        }

        private void ClearAllWater()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                    waterBodyIds[x, y] = 0;
            }
            bodies.Clear();
            nextBodyId = 1;
            proceduralRiverEntryAnchors.Clear();
        }

        private int ToFlat(int x, int y) => x + y * width;

        private static int BorderNeighborHeight(int x, int y, HeightMap hm)
        {
            if (hm.IsValidPosition(x, y))
                return hm.GetHeight(x, y);
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
                if (nh <= h0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Minimum in a (2r+1)×(2r+1) window with variation: center is ≤ all cells in the window and some cell in the window is higher (excludes flat plateaus).
        /// </summary>
        private bool IsLocalMinimumInWindow(int x, int y, HeightMap hm, int radius)
        {
            if (radius < 0)
                return false;
            int h0 = hm.GetHeight(x, y);
            int maxH = int.MinValue;
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (!IsValidPosition(nx, ny))
                        continue;
                    int h = hm.GetHeight(nx, ny);
                    if (h < h0)
                        return false;
                    if (h > maxH)
                        maxH = h;
                }
            }
            return maxH > h0;
        }

        /// <summary>
        /// Spill height for the seed's same-height plateau: minimum height over all cardinal neighbors
        /// of the 4-connected component at <paramref name="x"/>,<paramref name="y"/> that lie outside the component.
        /// Off-map neighbors use <see cref="OutsideMapSpillHeight"/>.
        /// If the plateau is the entire grid (uniform flat terrain), returns <c>seedH</c> so callers reject (no valid depression).
        /// </summary>
        private int GetPlateauSpillHeight(int x, int y, HeightMap hm)
        {
            int seedH = hm.GetHeight(x, y);
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            int spill = int.MaxValue;
            var q = new Queue<Vector2Int>();
            var inPlateau = new bool[width, height];
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

            if (plateauCount == width * height)
                return seedH;

            if (spill == int.MaxValue)
                return seedH;
            return spill;
        }

        private void CollectBasin(int sx, int sy, int spillHeight, HeightMap hm, List<Vector2Int> outCells)
        {
            outCells.Clear();
            if (hm.GetHeight(sx, sy) >= spillHeight)
                return;

            var q = new Queue<Vector2Int>();
            var visited = new bool[width, height];
            q.Enqueue(new Vector2Int(sx, sy));
            visited[sx, sy] = true;

            while (q.Count > 0)
            {
                var c = q.Dequeue();
                outCells.Add(c);
                int[] dx = { 1, -1, 0, 0 };
                int[] dy = { 0, 0, 1, -1 };
                for (int i = 0; i < 4; i++)
                {
                    int nx = c.x + dx[i];
                    int ny = c.y + dy[i];
                    if (!IsValidPosition(nx, ny) || visited[nx, ny])
                        continue;
                    if (hm.GetHeight(nx, ny) >= spillHeight)
                        continue;
                    visited[nx, ny] = true;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        /// <summary>Rivers merge only with rivers; lakes/seas still merge with each other at the same surface.</summary>
        private static bool CanMergeWaterBodies(WaterBody a, WaterBody b)
        {
            if (a.Classification == b.Classification)
                return true;
            if (a.Classification == WaterBodyType.River || b.Classification == WaterBodyType.River)
                return false;
            return true;
        }

        /// <summary>
        /// BUG-46 / §13.3: Merge cardinally adjacent cells that belong to different bodies but share the same logical
        /// <see cref="WaterBody.SurfaceHeight"/> (rivers with rivers, lakes with lakes, lake+sea). Call after procedural
        /// river placement so parallel segments at the same surface become one <see cref="WaterBody"/> id where they touch.
        /// </summary>
        public void MergeAdjacentBodiesWithSameSurface()
        {
            RunMergeAdjacentBodiesWithSameSurface();
        }

        private void RunMergeAdjacentBodiesWithSameSurface()
        {
            bool changed = true;
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (changed)
            {
                changed = false;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int idA = waterBodyIds[x, y];
                        if (idA == 0)
                            continue;
                        for (int i = 0; i < 4; i++)
                        {
                            int nx = x + dx[i];
                            int ny = y + dy[i];
                            if (!IsValidPosition(nx, ny))
                                continue;
                            int idB = waterBodyIds[nx, ny];
                            if (idB == 0 || idA == idB)
                                continue;
                            if (bodies[idA].SurfaceHeight != bodies[idB].SurfaceHeight)
                                continue;
                            if (!CanMergeWaterBodies(bodies[idA], bodies[idB]))
                                continue;

                            int keep = Math.Min(idA, idB);
                            int merge = Math.Max(idA, idB);
                            MergeBodyInto(merge, keep);
                            changed = true;
                            idA = waterBodyIds[x, y];
                        }
                    }
                }
            }
        }

        private void MergeBodyInto(int fromId, int toId)
        {
            if (fromId == toId || !bodies.TryGetValue(fromId, out WaterBody from) || !bodies.TryGetValue(toId, out WaterBody to))
                return;

            foreach (int flat in from.CellIndices)
            {
                int x = flat % width;
                int y = flat / width;
                waterBodyIds[x, y] = toId;
                to.AddCellIndex(flat);
            }
            bodies.Remove(fromId);
        }

        /// <summary>
        /// Carves axis-aligned rectangles and registers them as lakes until <paramref name="targetBodies"/> is reached or placement fails.
        /// Returns the number of new bodies placed in this call (before any merge the caller may run afterward).
        /// </summary>
        /// <summary>Distance from map border for artificial lakes; smaller on tiny grids so 1×1 lakes can still be placed.</summary>
        private int ComputeArtificialEdgeMargin()
        {
            int m = Mathf.Min(width, height);
            if (m <= 4) return 0;
            if (m <= 9) return 1;
            return 2;
        }

        /// <summary>After random + deterministic fallback, try square lakes at corners (extent respects MinLakeCells / MinLakeBoundingExtent).</summary>
        private int TryLastResortCornerArtificialLakes(HeightMap heightMap, LakeFillSettings settings, int seaLevel, int targetBodies, bool[,] claimed)
        {
            int em = ComputeArtificialEdgeMargin();
            int maxSpan = Mathf.Min(width - 2 * em, height - 2 * em);
            if (maxSpan < 1)
                return 0;

            int extent = Mathf.Max(settings.MinLakeBoundingExtent, Mathf.CeilToInt(Mathf.Sqrt(settings.MinLakeCells)));
            extent = Mathf.Min(extent, maxSpan, settings.MaxLakeBoundingExtent);
            if (extent < 1 || extent * extent < settings.MinLakeCells)
                return 0;

            int added = 0;
            int maxX0 = width - em - extent;
            int maxY0 = height - em - extent;
            if (maxX0 < em || maxY0 < em)
                return 0;

            var corners = new Vector2Int[]
            {
                new Vector2Int(em, em),
                new Vector2Int(maxX0, em),
                new Vector2Int(em, maxY0),
                new Vector2Int(maxX0, maxY0),
            };

            foreach (Vector2Int c in corners)
            {
                if (bodies.Count >= targetBodies)
                    break;
                if (TryPlaceArtificialRectangle(heightMap, settings, seaLevel, c.x, c.y, extent, extent, claimed, em))
                    added++;
            }

            return added;
        }

        private int TryArtificialLakeFallback(HeightMap heightMap, LakeFillSettings settings, int seaLevel, int targetBodies, bool[,] claimed, System.Random rnd)
        {
            int edgeMargin = ComputeArtificialEdgeMargin();
            const int maxRandomAttempts = 2500;
            int attempts = 0;
            int added = 0;

            while (bodies.Count < targetBodies && attempts < maxRandomAttempts)
            {
                attempts++;
                int rw = rnd.Next(settings.MinLakeBoundingExtent, settings.MaxLakeBoundingExtent + 1);
                int rh = rnd.Next(settings.MinLakeBoundingExtent, settings.MaxLakeBoundingExtent + 1);
                rw = Mathf.Clamp(rw, 1, Mathf.Max(1, width - 2 * edgeMargin));
                rh = Mathf.Clamp(rh, 1, Mathf.Max(1, height - 2 * edgeMargin));
                if (width - rw - 2 * edgeMargin < 1 || height - rh - 2 * edgeMargin < 1)
                    continue;

                int x0 = rnd.Next(edgeMargin, width - rw - edgeMargin + 1);
                int y0 = rnd.Next(edgeMargin, height - rh - edgeMargin + 1);

                if (TryPlaceArtificialRectangle(heightMap, settings, seaLevel, x0, y0, rw, rh, claimed, edgeMargin))
                {
                    added++;
                    if (bodies.Count >= targetBodies)
                        break;
                }
            }

            if (bodies.Count < targetBodies)
                added += TryDeterministicArtificialLakeFallback(heightMap, settings, seaLevel, targetBodies, claimed);

            return added;
        }

        /// <summary>Grid sweep (smallest extent first) when random placement fails.</summary>
        private int TryDeterministicArtificialLakeFallback(HeightMap heightMap, LakeFillSettings settings, int seaLevel, int targetBodies, bool[,] claimed)
        {
            int edgeMargin = ComputeArtificialEdgeMargin();
            int added = 0;
            int maxExtent = Mathf.Min(settings.MaxLakeBoundingExtent, 12, width - 2 * edgeMargin, height - 2 * edgeMargin);
            maxExtent = Mathf.Max(1, maxExtent);
            int minExtent = Mathf.Min(settings.MinLakeBoundingExtent, maxExtent);

            for (int extent = minExtent; extent <= maxExtent; extent++)
            {
                if (bodies.Count >= targetBodies)
                    break;
                for (int x0 = edgeMargin; x0 + extent <= width - edgeMargin; x0++)
                {
                    for (int y0 = edgeMargin; y0 + extent <= height - edgeMargin; y0++)
                    {
                        if (TryPlaceArtificialRectangle(heightMap, settings, seaLevel, x0, y0, extent, extent, claimed, edgeMargin))
                        {
                            added++;
                            if (bodies.Count >= targetBodies)
                                return added;
                        }
                    }
                }
            }

            return added;
        }

        /// <summary>
        /// True if any cardinal neighbor outside the rectangle touches water with the same surface height (would merge on next merge pass).
        /// </summary>
        private bool HasCardinalNeighborOutsideRectWithSameSurface(int x0, int y0, int rw, int rh, int surface)
        {
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            for (int ox = 0; ox < rw; ox++)
            {
                for (int oy = 0; oy < rh; oy++)
                {
                    int x = x0 + ox;
                    int y = y0 + oy;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + dx[i];
                        int ny = y + dy[i];
                        if (nx >= x0 && nx < x0 + rw && ny >= y0 && ny < y0 + rh)
                            continue;
                        if (!IsValidPosition(nx, ny))
                            continue;
                        int id = waterBodyIds[nx, ny];
                        if (id == 0)
                            continue;
                        if (bodies.TryGetValue(id, out WaterBody body) && body.SurfaceHeight == surface)
                            return true;
                    }
                }
            }
            return false;
        }

        private bool ResolveSurfaceForNewLake(int x0, int y0, int rw, int rh, int maxTerrainH, int seaLevel, out int surfaceOut)
        {
            surfaceOut = Mathf.Min(TerrainManager.MAX_HEIGHT, Mathf.Max(seaLevel + 1, maxTerrainH + 1));
            for (int bump = 0; bump < 16; bump++)
            {
                if (surfaceOut <= seaLevel)
                    return false;
                if (surfaceOut > TerrainManager.MAX_HEIGHT)
                    return false;
                if (!HasCardinalNeighborOutsideRectWithSameSurface(x0, y0, rw, rh, surfaceOut))
                    return true;
                surfaceOut++;
            }
            return false;
        }

        /// <summary>
        /// Lowest height among cardinal neighbors strictly outside the axis-aligned rectangle (the lake rim).
        /// Used to carve a basin at least one step below surrounding land when possible.
        /// </summary>
        private static int GetMinCardinalHeightOutsideRectangle(HeightMap heightMap, int x0, int y0, int rw, int rh)
        {
            int minH = int.MaxValue;
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };
            for (int ox = 0; ox < rw; ox++)
            {
                for (int oy = 0; oy < rh; oy++)
                {
                    int x = x0 + ox;
                    int y = y0 + oy;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + dx[i];
                        int ny = y + dy[i];
                        if (nx >= x0 && nx < x0 + rw && ny >= y0 && ny < y0 + rh)
                            continue;
                        if (!heightMap.IsValidPosition(nx, ny))
                            continue;
                        int nh = heightMap.GetHeight(nx, ny);
                        if (nh < minH)
                            minH = nh;
                    }
                }
            }
            return minH;
        }

        /// <summary>
        /// Artificial rectangles only carve interior cells. Land cells that touch a lake corner only diagonally
        /// (no cardinal neighbor inside the rectangle) could stay too high vs the resolved surface — clamp them
        /// to <paramref name="surface"/> so rim/bay continuity matches cardinal shores (BUG-42). After init,
        /// <see cref="TerrainManager.RefreshLakeShoreAfterLakePlacement"/> also lowers any Moore shore land above
        /// adjacent water logical surface (see isometric spec §2.4.1).
        /// </summary>
        private static void CoerceDiagonalCornerRimForArtificialLake(HeightMap heightMap, int x0, int y0, int rw, int rh, int surface)
        {
            if (heightMap == null || rw <= 0 || rh <= 0)
                return;

            int[] cx = { x0 - 1, x0 + rw, x0 - 1, x0 + rw };
            int[] cy = { y0 - 1, y0 - 1, y0 + rh, y0 + rh };
            int[] ddx = { 1, -1, 1, -1 };
            int[] ddy = { 1, 1, -1, -1 };

            for (int i = 0; i < 4; i++)
            {
                int x = cx[i];
                int y = cy[i];
                if (!heightMap.IsValidPosition(x, y))
                    continue;

                bool touchesLakeDiagonally = false;
                for (int k = 0; k < 4; k++)
                {
                    int lx = x + ddx[k];
                    int ly = y + ddy[k];
                    if (lx >= x0 && lx < x0 + rw && ly >= y0 && ly < y0 + rh)
                    {
                        touchesLakeDiagonally = true;
                        break;
                    }
                }

                if (!touchesLakeDiagonally)
                    continue;

                int[] cdx = { 1, -1, 0, 0 };
                int[] cdy = { 0, 0, 1, -1 };
                bool touchesLakeCardinally = false;
                for (int k = 0; k < 4; k++)
                {
                    int lx = x + cdx[k];
                    int ly = y + cdy[k];
                    if (lx >= x0 && lx < x0 + rw && ly >= y0 && ly < y0 + rh)
                    {
                        touchesLakeCardinally = true;
                        break;
                    }
                }

                if (touchesLakeCardinally)
                    continue;

                int cur = heightMap.GetHeight(x, y);
                if (cur > surface)
                    heightMap.SetHeight(x, y, Mathf.Clamp(surface, TerrainManager.MIN_HEIGHT, TerrainManager.MAX_HEIGHT));
            }
        }

        private void ExpandArtificialDirtyRect(int x0, int y0, int rw, int rh)
        {
            const int dirtyPad = 2;
            int dminX = Mathf.Max(0, x0 - dirtyPad);
            int dminY = Mathf.Max(0, y0 - dirtyPad);
            int dmaxX = Mathf.Min(width - 1, x0 + rw - 1 + dirtyPad);
            int dmaxY = Mathf.Min(height - 1, y0 + rh - 1 + dirtyPad);
            if (ArtificialDirtyMinX < 0)
            {
                ArtificialDirtyMinX = dminX;
                ArtificialDirtyMinY = dminY;
                ArtificialDirtyMaxX = dmaxX;
                ArtificialDirtyMaxY = dmaxY;
            }
            else
            {
                ArtificialDirtyMinX = Mathf.Min(ArtificialDirtyMinX, dminX);
                ArtificialDirtyMinY = Mathf.Min(ArtificialDirtyMinY, dminY);
                ArtificialDirtyMaxX = Mathf.Max(ArtificialDirtyMaxX, dmaxX);
                ArtificialDirtyMaxY = Mathf.Max(ArtificialDirtyMaxY, dmaxY);
            }
        }

        private bool TryPlaceArtificialRectangle(HeightMap heightMap, LakeFillSettings settings, int seaLevel, int x0, int y0, int rw, int rh, bool[,] claimed, int edgeMargin)
        {
            if (x0 < edgeMargin || y0 < edgeMargin || x0 + rw > width - edgeMargin || y0 + rh > height - edgeMargin)
                return false;
            if (rw * rh < settings.MinLakeCells)
                return false;
            if (!ArtificialRectangleBboxFits(settings, rw, rh))
                return false;

            for (int ox = 0; ox < rw; ox++)
            {
                for (int oy = 0; oy < rh; oy++)
                {
                    int x = x0 + ox;
                    int y = y0 + oy;
                    if (claimed[x, y] || IsWater(x, y))
                        return false;
                }
            }

            int maxHBefore = TerrainManager.MIN_HEIGHT;
            for (int ox = 0; ox < rw; ox++)
            {
                for (int oy = 0; oy < rh; oy++)
                {
                    int h = heightMap.GetHeight(x0 + ox, y0 + oy);
                    if (h > maxHBefore) maxHBefore = h;
                }
            }

            // Carve a real basin: floor must sit below the exterior rim (cardinal neighbors outside the rectangle).
            // Do not use a pre-carve surface here — resolving surface before carving made WaterBody.SurfaceHeight
            // follow old max terrain while floors dropped far lower, so water tiles floated above the hole.
            int minCardinalOutside = GetMinCardinalHeightOutsideRectangle(heightMap, x0, y0, rw, rh);
            int bowlFloorCap;
            if (minCardinalOutside == int.MaxValue)
                bowlFloorCap = Mathf.Max(TerrainManager.MIN_HEIGHT, maxHBefore - 1);
            else
                bowlFloorCap = Mathf.Max(TerrainManager.MIN_HEIGHT, minCardinalOutside - 1);

            for (int ox = 0; ox < rw; ox++)
            {
                for (int oy = 0; oy < rh; oy++)
                {
                    int x = x0 + ox;
                    int y = y0 + oy;
                    int cur = heightMap.GetHeight(x, y);
                    int targetFloor = Mathf.Min(cur, bowlFloorCap);
                    heightMap.SetHeight(x, y, targetFloor);
                }
            }

            int maxHPost = TerrainManager.MIN_HEIGHT;
            for (int ox = 0; ox < rw; ox++)
            {
                for (int oy = 0; oy < rh; oy++)
                {
                    int h = heightMap.GetHeight(x0 + ox, y0 + oy);
                    if (h > maxHPost) maxHPost = h;
                }
            }

            // Surface must reflect post-carve terrain so PlaceWater aligns with the basin floor.
            if (!ResolveSurfaceForNewLake(x0, y0, rw, rh, maxHPost, seaLevel, out int surface))
                surface = Mathf.Min(TerrainManager.MAX_HEIGHT, Mathf.Max(seaLevel + 1, maxHPost + 1));

            CoerceDiagonalCornerRimForArtificialLake(heightMap, x0, y0, rw, rh, surface);

            int bodyId = nextBodyId++;
            var wb = new WaterBody(bodyId, surface, WaterBodyType.Lake);
            bodies[bodyId] = wb;
            for (int ox = 0; ox < rw; ox++)
            {
                for (int oy = 0; oy < rh; oy++)
                {
                    int x = x0 + ox;
                    int y = y0 + oy;
                    waterBodyIds[x, y] = bodyId;
                    wb.AddCellIndex(ToFlat(x, y));
                    claimed[x, y] = true;
                }
            }

            ExpandArtificialDirtyRect(x0, y0, rw, rh);
            return true;
        }

        /// <summary>Creates a new river body and returns its id (FEAT-38).</summary>
        public int CreateRiverWaterBody(int surfaceHeight)
        {
            int bodyId = nextBodyId++;
            bodies[bodyId] = new WaterBody(bodyId, surfaceHeight, WaterBodyType.River);
            return bodyId;
        }

        /// <summary>Assigns a dry cell to an existing river body.</summary>
        public bool TryAssignCellToRiverBody(int x, int y, int bodyId)
        {
            if (!IsValidPosition(x, y))
                return false;
            if (waterBodyIds[x, y] != 0)
                return false;
            if (!bodies.TryGetValue(bodyId, out WaterBody wb) || wb.Classification != WaterBodyType.River)
                return false;
            int flat = ToFlat(x, y);
            waterBodyIds[x, y] = bodyId;
            wb.AddCellIndex(flat);
            return true;
        }

        /// <summary>
        /// Moves a cell from any water body into an existing river body (procedural river bed overlapping prior lake/sea).
        /// Dry cells delegate to <see cref="TryAssignCellToRiverBody"/>.
        /// Does not reassign <see cref="WaterBodyType.Lake"/> cells when the lake&apos;s <see cref="WaterBody.SurfaceHeight"/>
        /// differs from the river body&apos;s surface (§12.7 — same rule as <see cref="IsLakeSurfaceStepContactForbidden"/> at contacts).
        /// </summary>
        public bool TryReassignCellFromAnyWaterToRiverBody(int x, int y, int riverBodyId)
        {
            if (!IsValidPosition(x, y))
                return false;
            if (!bodies.TryGetValue(riverBodyId, out WaterBody river) || river.Classification != WaterBodyType.River)
                return false;
            int oldId = waterBodyIds[x, y];
            if (oldId == 0)
                return TryAssignCellToRiverBody(x, y, riverBodyId);
            if (oldId == riverBodyId)
                return true;
            if (bodies.TryGetValue(oldId, out WaterBody oldBody)
                && oldBody.Classification == WaterBodyType.Lake
                && oldBody.SurfaceHeight != river.SurfaceHeight)
            {
                return false;
            }

            RemoveCellFromBody(x, y, oldId);
            waterBodyIds[x, y] = riverBodyId;
            river.AddCellIndex(ToFlat(x, y));
            return true;
        }

    }

    /// <summary>Parameters for procedural lake placement (depression fill). Tuned in code only — not exposed on <see cref="WaterManager"/> until terrain generator UI (see FEAT-18 / BACKLOG).</summary>
    [Serializable]
    public sealed class LakeFillSettings
    {
        /// <summary>Applied only after spill &gt; seed height (hydrologically feasible). Use &lt; 1 to thin redundant seeds on dense minima maps.</summary>
        public float LakeAcceptProbability = 1f;
        public int MinLakeCells = 6;
        /// <summary>Hard upper bound on procedural lake bodies (safety cap).</summary>
        public int MaxLakeBodies = 48;

        /// <summary>Caps procedural lake count from scaled budget until map-size UI exists. Default 4.</summary>
        public int ProceduralLakeBudgetHardCap = 4;

        /// <summary>Extra spill-passing cells carved in terrain (margin over effective lake budget). See <see cref="TerrainManager"/> lake feasibility.</summary>
        public int LakeFeasibilityExtraBowls = 2;

        /// <summary>Default only; <see cref="WaterManager.InitializeWaterMap"/> sets this from <see cref="Territory.Persistence.MapGenerationSeed"/> before lake fill (BUG-36).</summary>
        public int RandomSeed = 54321;

        /// <summary>
        /// When true, <see cref="GetEffectiveMaxLakeBodies"/> scales with map area then caps by <see cref="ProceduralLakeBudgetHardCap"/>.
        /// When false (default), the target lake count is <see cref="ProceduralLakeBudgetHardCap"/> clamped to <see cref="MaxLakeBodies"/>, independent of map size.
        /// TODO(FEAT-18): wire from terrain generation options.
        /// </summary>
        public bool UseScaledProceduralLakeBudget = false;

        /// <summary>Reference edge length in cells (128×128 = 16384 cells).</summary>
        public int ReferenceMapSide = 128;

        /// <summary>Target number of procedural lakes on a map of <see cref="ReferenceMapSide"/>×<see cref="ReferenceMapSide"/> (used only when <see cref="UseScaledProceduralLakeBudget"/> is true).</summary>
        public int ProceduralLakeBudgetAtReference = 10;

        /// <summary>Extra random grid positions tried as basin seeds (mesas without strict local minima need this).</summary>
        public int RandomExtraSeedAttempts = 640;

        /// <summary>Radius for <see cref="IsLocalMinimumInWindow"/> (e.g. 2 = 5×5). Relaxes seeds vs 4-neighbor strict minima.</summary>
        public int LocalMinWindowRadius = 2;

        /// <summary>Minimum width and minimum height (in grid cells) of a lake's axis-aligned bounding box on each axis.</summary>
        public int MinLakeBoundingExtent = 2;
        /// <summary>
        /// Maximum width and maximum height (in grid cells) of a lake's axis-aligned bounding box — same rule as procedural depression-fill (<see cref="LakeBoundingBoxFits"/>).
        /// Artificial fallback uses these bounds so carved lakes never exceed procedural footprint limits.
        /// </summary>
        public int MaxLakeBoundingExtent = 10;

        /// <summary>If true, runs an extra pass with larger window minima and <see cref="BoundedLocalDepressionMaxBasinCells"/>.</summary>
        public bool RunBoundedLocalDepressionPass = true;
        public int BoundedLocalDepressionWindowRadius = 3;
        public int BoundedLocalDepressionMaxBasinCells = 100;
        public float BoundedLocalDepressionAcceptScale = 0.85f;

        /// <summary>
        /// Area-scaled lake count capped by <see cref="MaxLakeBodies"/> (for diagnostics when comparing to fixed <see cref="ProceduralLakeBudgetHardCap"/>).
        /// </summary>
        public int GetAreaScaledLakeBudgetDiagnostic(int mapWidth, int mapHeight)
        {
            int refArea = Mathf.Max(1, ReferenceMapSide * ReferenceMapSide);
            int area = Mathf.Max(1, mapWidth * mapHeight);
            int scaled = Mathf.RoundToInt(ProceduralLakeBudgetAtReference * (area / (float)refArea));
            scaled = Mathf.Max(1, scaled);
            return Mathf.Min(MaxLakeBodies, scaled);
        }

        /// <summary>
        /// Target procedural lake body count: either <see cref="ProceduralLakeBudgetHardCap"/> (clamped) when area scaling is off, or area-scaled value capped by the hard cap when scaling is on.
        /// </summary>
        public int GetEffectiveMaxLakeBodies(int mapWidth, int mapHeight)
        {
            if (!UseScaledProceduralLakeBudget)
                return Mathf.Clamp(ProceduralLakeBudgetHardCap, 1, MaxLakeBodies);

            int scaled = GetAreaScaledLakeBudgetDiagnostic(mapWidth, mapHeight);
            return Mathf.Min(ProceduralLakeBudgetHardCap, scaled);
        }

        /// <summary>At least <see cref="RandomExtraSeedAttempts"/>, scaled up for maps larger than the reference area.</summary>
        public int GetScaledRandomExtraSeedAttempts(int mapWidth, int mapHeight)
        {
            int refArea = Mathf.Max(1, ReferenceMapSide * ReferenceMapSide);
            int area = mapWidth * mapHeight;
            int scaled = Mathf.RoundToInt(RandomExtraSeedAttempts * (area / (float)refArea));
            return Mathf.Max(RandomExtraSeedAttempts, scaled);
        }
    }

    [Serializable]
    public sealed class WaterMapData
    {
        public int formatVersion = WaterMap.FormatVersionV3;
        public int width;
        public int height;

        /// <summary>V2: flattened water body id per cell (0 = dry).</summary>
        public int[] waterBodyIds;

        /// <summary>V1 legacy: flattened bool water mask.</summary>
        public bool[] waterCells;

        public WaterBodySerialized[] bodies;
    }

    [Serializable]
    public sealed class WaterBodySerialized
    {
        public int id;
        public int surfaceHeight;

        /// <summary>Values from <see cref="WaterBodyType"/> (Lake, Sea, River). 0 / missing = treat as Lake on load.</summary>
        public int bodyClassification;

        public int[] cellIndicesFlat;
    }
}
