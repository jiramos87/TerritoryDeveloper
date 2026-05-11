using System.Collections.Generic;
using Territory.Core;
using UnityEngine;

namespace Territory.Terrain
{
    /// <summary>
    /// Junction-merge + multi-body surface boundary logic extracted from WaterMap (Strategy γ Stage 3.2).
    /// Partial class — same assembly as WaterMap.cs. Accesses internal/private members via partial.
    /// </summary>
    public sealed partial class WaterMap
    {
        // ─────────────────────────────────────────────────────────────────────────
        // TryGetDryLandRiverJunctionBrinkWithStep
        // ─────────────────────────────────────────────────────────────────────────

        internal bool TryGetDryLandRiverJunctionBrinkWithStepImpl(
            int x, int y,
            out RiverJunctionBrinkRole role, out int affiliatedBodyId,
            out int highX, out int highY, out int lowX, out int lowY)
        {
            role = RiverJunctionBrinkRole.None;
            affiliatedBodyId = 0;
            highX = highY = lowX = lowY = 0;
            if (!IsValidPosition(x, y) || IsWater(x, y))
                return false;

            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };

            // 1) Lower first: cardinally adjacent to the low cell of a river–river step.
            for (int i = 0; i < 4; i++)
            {
                int nx = x + d4x[i];
                int ny = y + d4y[i];
                if (!IsValidPosition(nx, ny) || !IsWater(nx, ny))
                    continue;
                if (!IsWaterCellLowerSideOfCardinalSurfaceStep(nx, ny))
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    int wx = nx + d4x[j];
                    int wy = ny + d4y[j];
                    if (!IsValidPosition(wx, wy) || !IsWater(wx, wy))
                        continue;
                    if (IsRiverRiverCardinalSurfaceStepHighToLow(wx, wy, nx, ny))
                    {
                        role = RiverJunctionBrinkRole.LowerBrink;
                        affiliatedBodyId = GetWaterBodyId(nx, ny);
                        highX = wx;
                        highY = wy;
                        lowX = nx;
                        lowY = ny;
                        return true;
                    }
                }
            }

            // 2) Upper: cardinally adjacent to the high cell of a river–river step.
            for (int i = 0; i < 4; i++)
            {
                int nx = x + d4x[i];
                int ny = y + d4y[i];
                if (!IsValidPosition(nx, ny) || !IsWater(nx, ny))
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    int wx = nx + d4x[j];
                    int wy = ny + d4y[j];
                    if (!IsValidPosition(wx, wy) || !IsWater(wx, wy))
                        continue;
                    if (wx == x && wy == y)
                        continue;
                    if (IsRiverRiverCardinalSurfaceStepHighToLow(nx, ny, wx, wy))
                    {
                        role = RiverJunctionBrinkRole.UpperBrink;
                        affiliatedBodyId = GetWaterBodyId(nx, ny);
                        highX = nx;
                        highY = ny;
                        lowX = wx;
                        lowY = wy;
                        return true;
                    }
                    if (IsRiverRiverCardinalSurfaceStepHighToLow(wx, wy, nx, ny))
                    {
                        role = RiverJunctionBrinkRole.UpperBrink;
                        affiliatedBodyId = GetWaterBodyId(wx, wy);
                        highX = wx;
                        highY = wy;
                        lowX = nx;
                        lowY = ny;
                        return true;
                    }
                }
            }

            // 3) Upper: Moore-adjacent to the high cell while low cell may be diagonal-only.
            int[] ddx = { 1, 1, -1, -1 };
            int[] ddy = { -1, 1, -1, 1 };
            for (int d = 0; d < 4; d++)
            {
                int dx = x + ddx[d];
                int dy = y + ddy[d];
                if (!IsValidPosition(dx, dy) || !IsWater(dx, dy))
                    continue;
                if (!IsWaterCellLowerSideOfCardinalSurfaceStep(dx, dy))
                    continue;
                for (int j = 0; j < 4; j++)
                {
                    int stepHighX = dx + d4x[j];
                    int stepHighY = dy + d4y[j];
                    if (!IsValidPosition(stepHighX, stepHighY) || !IsWater(stepHighX, stepHighY))
                        continue;
                    if (!IsRiverRiverCardinalSurfaceStepHighToLow(stepHighX, stepHighY, dx, dy))
                        continue;
                    int dist = Mathf.Max(Mathf.Abs(x - stepHighX), Mathf.Abs(y - stepHighY));
                    if (dist <= 1)
                    {
                        role = RiverJunctionBrinkRole.UpperBrink;
                        affiliatedBodyId = GetWaterBodyId(stepHighX, stepHighY);
                        highX = stepHighX;
                        highY = stepHighY;
                        lowX = dx;
                        lowY = dy;
                        return true;
                    }
                }
            }

            return false;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // IsDryLandRiverJunctionBrinkClosestToCascadeStep
        // ─────────────────────────────────────────────────────────────────────────

        internal bool IsDryLandRiverJunctionBrinkClosestToCascadeStepImpl(
            int x, int y, RiverJunctionBrinkRole role,
            int stepHighX, int stepHighY, int stepLowX, int stepLowY)
        {
            if (!TryGetDryLandRiverJunctionBrinkWithStep(x, y, out RiverJunctionBrinkRole r, out _, out int h0, out int h1, out int l0, out int l1))
                return false;
            if (r != role || h0 != stepHighX || h1 != stepHighY || l0 != stepLowX || l1 != stepLowY)
                return false;

            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };

            var queue = new Queue<(int qx, int qy)>();
            var visited = new HashSet<(int, int)>();
            var component = new List<(int cx, int cy)>();
            queue.Enqueue((x, y));
            visited.Add((x, y));

            int Manhattan(int px, int py, int tx, int ty) =>
                Mathf.Abs(px - tx) + Mathf.Abs(py - ty);

            int Score(int px, int py) =>
                Manhattan(px, py, stepHighX, stepHighY) + Manhattan(px, py, stepLowX, stepLowY);

            while (queue.Count > 0 && component.Count < 96)
            {
                var (qx, qy) = queue.Dequeue();
                if (!TryGetDryLandRiverJunctionBrinkWithStep(qx, qy, out RiverJunctionBrinkRole r2, out _, out int hh0, out int hh1, out int ll0, out int ll1))
                    continue;
                if (r2 != role || hh0 != stepHighX || hh1 != stepHighY || ll0 != stepLowX || ll1 != stepLowY)
                    continue;

                component.Add((qx, qy));

                for (int i = 0; i < 4; i++)
                {
                    int nx = qx + d4x[i];
                    int ny = qy + d4y[i];
                    if (!IsValidPosition(nx, ny) || IsWater(nx, ny))
                        continue;
                    if (visited.Contains((nx, ny)))
                        continue;
                    visited.Add((nx, ny));
                    if (!TryGetDryLandRiverJunctionBrinkWithStep(nx, ny, out RiverJunctionBrinkRole r3, out _, out int h3, out int h3y, out int l3, out int l3y))
                        continue;
                    if (r3 != role || h3 != stepHighX || h3y != stepHighY || l3 != stepLowX || l3y != stepLowY)
                        continue;
                    queue.Enqueue((nx, ny));
                }
            }

            if (component.Count == 0)
                return false;

            int bestScore = int.MaxValue;
            for (int i = 0; i < component.Count; i++)
            {
                int s = Score(component[i].cx, component[i].cy);
                if (s < bestScore) bestScore = s;
            }

            int bestDHigh = int.MaxValue;
            int winX = int.MaxValue;
            int winY = int.MaxValue;

            for (int i = 0; i < component.Count; i++)
            {
                int cx = component[i].cx;
                int cy = component[i].cy;
                if (Score(cx, cy) != bestScore)
                    continue;
                int dh = Manhattan(cx, cy, stepHighX, stepHighY);
                if (dh < bestDHigh)
                {
                    bestDHigh = dh;
                    winX = cx;
                    winY = cy;
                }
                else if (dh == bestDHigh)
                {
                    if (cx < winX || (cx == winX && cy < winY))
                    {
                        winX = cx;
                        winY = cy;
                    }
                }
            }

            return x == winX && y == winY;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // TryFindRiverRiverSurfaceStepBetweenBodiesNear
        // ─────────────────────────────────────────────────────────────────────────

        internal bool TryFindRiverRiverSurfaceStepBetweenBodiesNearImpl(int x, int y, int bodyA, int bodyB, int searchRadius)
        {
            if (bodyA == 0 || bodyB == 0 || bodyA == bodyB || !IsValidPosition(x, y))
                return false;
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    int hx = x + dx;
                    int hy = y + dy;
                    if (!IsValidPosition(hx, hy) || !IsWater(hx, hy))
                        continue;
                    for (int i = 0; i < 4; i++)
                    {
                        int lx = hx + d4x[i];
                        int ly = hy + d4y[i];
                        if (!IsValidPosition(lx, ly) || !IsWater(lx, ly))
                            continue;
                        if (!IsRiverRiverCardinalSurfaceStepHighToLow(hx, hy, lx, ly))
                            continue;
                        int bH = GetWaterBodyId(hx, hy);
                        int bL = GetWaterBodyId(lx, ly);
                        if ((bH == bodyA && bL == bodyB) || (bH == bodyB && bL == bodyA))
                            return true;
                    }
                }
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ApplyMultiBodySurfaceBoundaryNormalization
        // ─────────────────────────────────────────────────────────────────────────

        internal void ApplyMultiBodySurfaceBoundaryNormalizationImpl(HeightMap heightMap)
        {
            if (heightMap == null) return;
            const int maxPassAIterations = 24;
            for (int i = 0; i < maxPassAIterations; i++)
                if (!AlignUpperSurfaceContactBorderBedHeightsOnce(heightMap))
                    break;
        }

        private bool AlignUpperSurfaceContactBorderBedHeightsOnce(HeightMap hm)
        {
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            bool any = false;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!IsWater(x, y)) continue;
                    int sHere = GetSurfaceHeightAt(x, y);
                    if (sHere < 0) continue;
                    if (HasCardinalWaterNeighborAtSameSurface(x, y, sHere)) continue;
                    int targetBed = int.MaxValue;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + d4x[i];
                        int ny = y + d4y[i];
                        if (!IsValidPosition(nx, ny) || !IsWater(nx, ny)) continue;
                        int sN = GetSurfaceHeightAt(nx, ny);
                        if (sN < 0 || sN >= sHere) continue;
                        if (IsLakeSurfaceStepContactForbidden(x, y, nx, ny)) continue;
                        targetBed = Mathf.Min(targetBed, hm.GetHeight(nx, ny));
                    }
                    if (targetBed == int.MaxValue) continue;
                    int clamped = Mathf.Clamp(targetBed, HeightMap.MIN_HEIGHT, HeightMap.MAX_HEIGHT);
                    if (hm.GetHeight(x, y) == clamped) continue;
                    hm.SetHeight(x, y, clamped);
                    any = true;
                }
            }
            return any;
        }

        private bool HasCardinalWaterNeighborAtSameSurface(int x, int y, int surface)
        {
            if (surface < 0) return false;
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                int nx = x + d4x[i];
                int ny = y + d4y[i];
                if (!IsValidPosition(nx, ny) || !IsWater(nx, ny)) continue;
                if (GetSurfaceHeightAt(nx, ny) == surface) return true;
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ApplyWaterSurfaceJunctionMerge
        // ─────────────────────────────────────────────────────────────────────────

        internal bool ApplyWaterSurfaceJunctionMergeImpl(HeightMap heightMap, IGridManager gridManager,
            out int dirtyMinX, out int dirtyMinY, out int dirtyMaxX, out int dirtyMaxY)
        {
            dirtyMinX = dirtyMinY = dirtyMaxX = dirtyMaxY = 0;
            if (heightMap == null) return false;

            const int maxStripDepthCap = 5;
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };

            bool anyChange = false;
            bool haveBounds = false;
            int dMinX = 0, dMinY = 0, dMaxX = 0, dMaxY = 0;
            var absorbClaims = new Dictionary<Vector2Int, (int bodyId, int sLow)>();

            void ExpandDirty(int x, int y)
            {
                if (!haveBounds) { dMinX = dMaxX = x; dMinY = dMaxY = y; haveBounds = true; }
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
                    if (!IsWater(hx, hy)) continue;
                    int sHigh = GetSurfaceHeightAt(hx, hy);
                    if (sHigh < 0) continue;

                    for (int i = 0; i < 4; i++)
                    {
                        int lx = hx + d4x[i];
                        int ly = hy + d4y[i];
                        if (!IsValidPosition(lx, ly) || !IsWater(lx, ly)) continue;
                        int sLow = GetSurfaceHeightAt(lx, ly);
                        if (sLow < 0 || sHigh <= sLow) continue;
                        if (IsLakeSurfaceStepContactForbidden(hx, hy, lx, ly)) continue;

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
                            if (!IsValidPosition(cx, cy)) break;
                            if (IsWater(cx, cy)) break;
                            ProposeAbsorb(cx, cy, lowerBodyId, sLow);
                        }

                        // Upper-bank wedge: dry cells perpendicular to the high→low step.
                        int ddx = lx - hx;
                        int ddy = ly - hy;
                        int p1x = -ddy; int p1y = ddx;
                        int p2x = ddy; int p2y = -ddx;
                        for (int pi = 0; pi < 2; pi++)
                        {
                            int px = pi == 0 ? p1x : p2x;
                            int py = pi == 0 ? p1y : p2y;
                            int ux = hx + px;
                            int uy = hy + py;
                            if (!IsValidPosition(ux, uy)) continue;
                            if (IsWater(ux, uy)) continue;
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

        private bool TryReassignUpperWaterCellsMatchingLowerContactBed(HeightMap hm, IGridManager grid, System.Action<int, int> expandDirty)
        {
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            bool any = false;

            for (int hx = 0; hx < width; hx++)
            {
                for (int hy = 0; hy < height; hy++)
                {
                    if (!IsWater(hx, hy)) continue;
                    int sHigh = GetSurfaceHeightAt(hx, hy);
                    if (sHigh < 0) continue;
                    if (HasCardinalWaterNeighborAtSameSurface(hx, hy, sHigh)) continue;

                    int bestSl = int.MaxValue;
                    int bestBid = int.MaxValue;
                    int bestLx = -1;
                    int bestLy = -1;

                    for (int i = 0; i < 4; i++)
                    {
                        int lx = hx + d4x[i];
                        int ly = hy + d4y[i];
                        if (!IsValidPosition(lx, ly) || !IsWater(lx, ly)) continue;
                        int sLow = GetSurfaceHeightAt(lx, ly);
                        if (sLow < 0 || sHigh <= sLow) continue;
                        if (IsLakeSurfaceStepContactForbidden(hx, hy, lx, ly)) continue;
                        int hH = hm.GetHeight(hx, hy);
                        int hL = hm.GetHeight(lx, ly);
                        if (hH < hL) continue;
                        int bid = GetWaterBodyId(lx, ly);
                        if (bid == 0 || !bodies.TryGetValue(bid, out WaterBody lb) || lb.SurfaceHeight != sLow) continue;

                        if (sLow < bestSl || (sLow == bestSl && bid < bestBid))
                        {
                            bestSl = sLow; bestBid = bid; bestLx = lx; bestLy = ly;
                        }
                    }

                    if (bestLx < 0) continue;
                    int highId = GetWaterBodyId(hx, hy);
                    if (highId == bestBid) continue;

                    if (TryReassignSingleUpperWaterCellToLowerBody(hx, hy, bestLx, bestLy, hm, grid))
                    {
                        any = true;
                        expandDirty?.Invoke(hx, hy);
                    }
                }
            }
            return any;
        }

        private bool TryReassignSingleUpperWaterCellToLowerBody(int hx, int hy, int lx, int ly, HeightMap hm, IGridManager grid)
        {
            int sHigh = GetSurfaceHeightAt(hx, hy);
            int sLow = GetSurfaceHeightAt(lx, ly);
            if (sLow < 0 || sHigh <= sLow) return false;
            if (IsLakeSurfaceStepContactForbidden(hx, hy, lx, ly)) return false;

            int hH = hm.GetHeight(hx, hy);
            int hL = hm.GetHeight(lx, ly);
            if (hH < hL) return false;
            if (hH > hL)
            {
                int clamped = Mathf.Clamp(hL, HeightMap.MIN_HEIGHT, HeightMap.MAX_HEIGHT);
                hm.SetHeight(hx, hy, clamped);
                if (grid != null) grid.SetCellHeight(new Vector2(hx, hy), hm.GetHeight(hx, hy));
            }

            int lowId = GetWaterBodyId(lx, ly);
            int highId = GetWaterBodyId(hx, hy);
            if (lowId == 0 || highId == lowId) return false;
            if (!bodies.TryGetValue(lowId, out WaterBody lowerBody) || lowerBody.SurfaceHeight != sLow) return false;

            RemoveCellFromBody(hx, hy, highId);
            SetWaterBodyId(hx, hy, lowId);
            lowerBody.AddCellIndex(ToFlat(hx, hy));

            if (grid != null) grid.SetCellHeight(new Vector2(hx, hy), hm.GetHeight(hx, hy));
            return true;
        }

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

        private bool TryAbsorbDryCellIntoLowerBody(int x, int y, int targetBodyId, int sLow, HeightMap hm, IGridManager grid, out bool changed)
        {
            changed = false;
            if (!IsValidPosition(x, y) || !bodies.TryGetValue(targetBodyId, out WaterBody targetBody) || targetBody.SurfaceHeight != sLow)
                return false;

            int existingId = GetWaterBodyId(x, y);
            if (existingId != 0)
            {
                if (existingId == targetBodyId) return true;
                if (GetSurfaceHeightAt(x, y) == sLow) return false;
                return false;
            }

            if (grid != null)
            {
                CityCell cell = grid.GetCell(x, y);
                if (cell != null && cell.occupiedBuilding != null) return false;
            }

            int targetBed = ProposeLowerJunctionBedHeight(x, y, sLow, hm);
            int maxBed = Mathf.Min(HeightMap.MAX_HEIGHT, sLow - 1);
            if (maxBed < HeightMap.MIN_HEIGHT) return false;
            targetBed = Mathf.Clamp(targetBed, HeightMap.MIN_HEIGHT, maxBed);

            hm.SetHeight(x, y, targetBed);
            int flat = ToFlat(x, y);
            SetWaterBodyId(x, y, targetBodyId);
            targetBody.AddCellIndex(flat);
            changed = true;

            if (grid != null) grid.SetCellHeight(new Vector2(x, y), targetBed);
            return true;
        }

        private int ProposeLowerJunctionBedHeight(int x, int y, int sLow, HeightMap hm)
        {
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            int best = int.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                int nx = x + d4x[i];
                int ny = y + d4y[i];
                if (!IsValidPosition(nx, ny) || !IsWater(nx, ny) || GetSurfaceHeightAt(nx, ny) != sLow) continue;
                best = Mathf.Min(best, hm.GetHeight(nx, ny));
            }
            return best == int.MaxValue ? hm.GetHeight(x, y) : best;
        }

        private bool IsRiverRiverCardinalSurfaceStepHighToLow(int highX, int highY, int lowX, int lowY)
        {
            if (!IsWater(highX, highY) || !IsWater(lowX, lowY)) return false;
            int sH = GetSurfaceHeightAt(highX, highY);
            int sL = GetSurfaceHeightAt(lowX, lowY);
            if (sH <= sL) return false;
            if (IsLakeSurfaceStepContactForbidden(highX, highY, lowX, lowY)) return false;
            if (GetBodyClassificationAt(highX, highY) != WaterBodyType.River) return false;
            if (GetBodyClassificationAt(lowX, lowY) != WaterBodyType.River) return false;
            return true;
        }
    }
}
