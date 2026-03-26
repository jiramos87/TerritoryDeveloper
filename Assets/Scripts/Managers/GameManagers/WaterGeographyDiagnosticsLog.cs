using System;
using System.Collections.Generic;
using UnityEngine;

namespace Territory.Terrain
{
    /// <summary>
    /// One-shot console summary after procedural water generation: final surface height per lake/sea/river body,
    /// and orthogonal segments where two bodies meet at different logical surfaces (BUG-45 / geography QA).
    /// </summary>
    public static class WaterGeographyDiagnosticsLog
    {
        private const string Tag = "[WaterGeography]";

        private static readonly int[] D4x = { 1, -1, 0, 0 };
        private static readonly int[] D4y = { 0, 0, 1, -1 };

        /// <summary>
        /// Writes one line per non-empty water body and one line per connected high-side segment between distinct surfaces.
        /// </summary>
        public static void WriteProceduralWaterSummary(WaterMap wm)
        {
            if (wm == null)
                return;

            int gw = wm.Width;
            int gh = wm.Height;
            var bodyList = new List<WaterBody>();
            foreach (var kv in wm.GetBodies())
                bodyList.Add(kv.Value);
            bodyList.Sort((a, b) => a.Id.CompareTo(b.Id));

            IReadOnlyList<Vector2Int> riverAnchors = wm.GetProceduralRiverEntryAnchors();

            foreach (WaterBody body in bodyList)
            {
                if (body.CellCount == 0)
                    continue;

                if (!TryGetRepresentativeCell(wm, body, riverAnchors, gw, out int rx, out int ry))
                    continue;

                string kind = body.Classification.ToString();
                Debug.Log($"{Tag} {kind} bodyId={body.Id} surfaceHeight={body.SurfaceHeight} representativeCell=({rx},{ry})");
            }

            LogDistinctSurfaceIntersections(wm, gw, gh);
        }

        private static bool TryGetRepresentativeCell(
            WaterMap wm,
            WaterBody body,
            IReadOnlyList<Vector2Int> riverAnchors,
            int gridWidth,
            out int rx,
            out int ry)
        {
            if (body.Classification == WaterBodyType.River && riverAnchors != null)
            {
                for (int i = 0; i < riverAnchors.Count; i++)
                {
                    Vector2Int a = riverAnchors[i];
                    if (!wm.IsValidPosition(a.x, a.y))
                        continue;
                    if (wm.GetWaterBodyId(a.x, a.y) == body.Id)
                    {
                        rx = a.x;
                        ry = a.y;
                        return true;
                    }
                }
            }

            return TryGetMostCentralCell(body, gridWidth, out rx, out ry);
        }

        private static bool TryGetMostCentralCell(WaterBody body, int gridWidth, out int cx, out int cy)
        {
            cx = cy = 0;
            if (body.CellCount == 0)
                return false;

            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (int flat in body.CellIndices)
            {
                int x = flat % gridWidth;
                int y = flat / gridWidth;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            double mx = (minX + maxX) * 0.5;
            double my = (minY + maxY) * 0.5;
            double bestDist = double.MaxValue;
            int bestX = minX;
            int bestY = minY;

            foreach (int flat in body.CellIndices)
            {
                int x = flat % gridWidth;
                int y = flat / gridWidth;
                double dx = x - mx;
                double dy = y - my;
                double d = dx * dx + dy * dy;
                if (d < bestDist)
                {
                    bestDist = d;
                    bestX = x;
                    bestY = y;
                }
                else if (Math.Abs(d - bestDist) < 1e-9)
                {
                    if (x < bestX || (x == bestX && y < bestY))
                    {
                        bestX = x;
                        bestY = y;
                    }
                }
            }

            cx = bestX;
            cy = bestY;
            return true;
        }

        private static void LogDistinctSurfaceIntersections(WaterMap wm, int gw, int gh)
        {
            var pools = new Dictionary<(int sHi, int sLo), HashSet<Vector2Int>>();

            for (int x = 0; x < gw; x++)
            {
                for (int y = 0; y < gh; y++)
                {
                    if (!wm.IsWater(x, y))
                        continue;
                    int idHere = wm.GetWaterBodyId(x, y);
                    int sHere = wm.GetSurfaceHeightAt(x, y);
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + D4x[i];
                        int ny = y + D4y[i];
                        if (!wm.IsValidPosition(nx, ny) || !wm.IsWater(nx, ny))
                            continue;
                        int idN = wm.GetWaterBodyId(nx, ny);
                        if (idHere == idN)
                            continue;
                        int sN = wm.GetSurfaceHeightAt(nx, ny);
                        if (sHere == sN)
                            continue;

                        if (sHere > sN)
                            AddToPool(pools, (sHere, sN), new Vector2Int(x, y));
                        else
                            AddToPool(pools, (sN, sHere), new Vector2Int(nx, ny));
                    }
                }
            }

            foreach (var kv in pools)
            {
                (int sHi, int sLo) = kv.Key;
                HashSet<Vector2Int> pool = kv.Value;
                if (pool == null || pool.Count == 0)
                    continue;

                var globallyVisited = new HashSet<Vector2Int>();
                foreach (Vector2Int seed in pool)
                {
                    if (globallyVisited.Contains(seed))
                        continue;
                    var component = new List<Vector2Int>();
                    CollectOrthogonalComponent(seed, pool, globallyVisited, component);
                    if (component.Count == 0)
                        continue;
                    component.Sort(CompareGridCells);
                    Vector2Int mid = component[component.Count / 2];
                    Debug.Log($"{Tag} WaterBodyIntersection upperSurface={sHi} lowerSurface={sLo} representativeCell=({mid.x},{mid.y}) segmentCellCount={component.Count}");
                }
            }
        }

        private static int CompareGridCells(Vector2Int a, Vector2Int b)
        {
            int ca = a.x + a.y;
            int cb = b.x + b.y;
            if (ca != cb)
                return ca.CompareTo(cb);
            return a.x.CompareTo(b.x);
        }

        private static void AddToPool(Dictionary<(int sHi, int sLo), HashSet<Vector2Int>> pools, (int sHi, int sLo) key, Vector2Int cell)
        {
            if (!pools.TryGetValue(key, out HashSet<Vector2Int> set))
            {
                set = new HashSet<Vector2Int>();
                pools[key] = set;
            }

            set.Add(cell);
        }

        private static void CollectOrthogonalComponent(
            Vector2Int start,
            HashSet<Vector2Int> pool,
            HashSet<Vector2Int> globallyVisited,
            List<Vector2Int> componentOut)
        {
            var q = new Queue<Vector2Int>();
            q.Enqueue(start);
            globallyVisited.Add(start);

            while (q.Count > 0)
            {
                Vector2Int c = q.Dequeue();
                componentOut.Add(c);
                for (int i = 0; i < 4; i++)
                {
                    int nx = c.x + D4x[i];
                    int ny = c.y + D4y[i];
                    var n = new Vector2Int(nx, ny);
                    if (!pool.Contains(n) || globallyVisited.Contains(n))
                        continue;
                    globallyVisited.Add(n);
                    q.Enqueue(n);
                }
            }
        }
    }
}
