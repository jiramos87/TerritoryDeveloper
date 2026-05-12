using System.Collections.Generic;
using Domains.Water.Services;
using Territory.Core;
using UnityEngine;

namespace Territory.Terrain
{
    /// <summary>
    /// FEAT-38: procedural static rivers after lake/sea init.
    /// Hub (Stage 5.3 THIN): delegates BFS + carve to <see cref="ProceduralRiverService"/>.
    /// FILE PATH UNCHANGED. CLASS NAME UNCHANGED. NAMESPACE UNCHANGED.
    /// Bed floor H_bed non-increasing along centerline (invariant #8) enforced in ProceduralRiverService.ApplyCrossSectionHeights.
    /// </summary>
    public static class ProceduralRiverGenerator
    {
        public static void Generate(WaterManager waterManager, TerrainManager terrainManager, GridManager gridManager, System.Random rnd)
        {
            WaterMap wm = waterManager.GetWaterMap();
            HeightMap hm = terrainManager.GetHeightMap();
            if (wm == null || hm == null || gridManager == null)
                return;

            int gw = gridManager.width;
            int gh = gridManager.height;
            if (!ProceduralRiverService.CanPlaceRiversWithMargin(gw, gh))
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
                HashSet<Vector2Int> avoidForBfs = ProceduralRiverService.BuildAvoidForBfs(used, gw, gh);
                List<Vector2Int> sameAxisEntries = nsAxis ? nsEntryStarts : ewEntryStarts;
                List<Vector2Int> centerline = ProceduralRiverService.TryBuildCenterline(wm, hm, gw, gh, maxL, nsAxis, flowPositive, rnd, avoidForBfs, sameAxisEntries, ProceduralRiverService.MinRiverEntrySeparationOnBorder);
                if (centerline == null || centerline.Count < 2)
                    centerline = ProceduralRiverService.BuildForcedCenterline(wm, gw, gh, nsAxis, flowPositive, rnd, avoidForBfs);

                if (centerline == null || centerline.Count < 2)
                    continue;

                if (nsAxis) nsEntryStarts.Add(centerline[0]);
                else ewEntryStarts.Add(centerline[0]);

                wm.RecordProceduralRiverEntryAnchor(centerline[0].x, centerline[0].y);

                int bedWidth = ProceduralRiverService.MinRiverBedWidth;
                int stepsSinceWidthChange = 0;
                var corridorFootprint = new HashSet<Vector2Int>();
                var waterFootprint = new HashSet<Vector2Int>();
                var crossSections = new List<ProceduralRiverService.RiverCrossSectionData>();
                int pathLen = centerline.Count;

                for (int i = 0; i < pathLen; i++)
                {
                    stepsSinceWidthChange++;
                    if (stepsSinceWidthChange >= 4 && rnd.NextDouble() < 0.3 && bedWidth < ProceduralRiverService.MaxRiverBedWidth)
                    {
                        bedWidth++;
                        stepsSinceWidthChange = 0;
                    }

                    Vector2Int prev = i > 0 ? centerline[i - 1] : centerline[i];
                    Vector2Int cur = centerline[i];
                    Vector2Int next = i + 1 < pathLen ? centerline[i + 1] : cur + (centerline[i] - centerline[i - 1]);
                    ProceduralRiverService.RiverCrossSectionData sec = ProceduralRiverService.BuildCrossSection(wm, gw, gh, prev, cur, next, bedWidth, nsAxis, flowPositive, i, pathLen);
                    crossSections.Add(sec);
                    foreach (Vector2Int p in sec.Bed) waterFootprint.Add(p);
                    foreach (Vector2Int p in sec.AllCorridorCells()) corridorFootprint.Add(p);
                }

                foreach (var p in corridorFootprint) used.Add(p);

                var riverBedCarvedCells = new HashSet<Vector2Int>();
                ProceduralRiverService.ApplyCrossSectionHeights(wm, hm, crossSections, riverBedCarvedCells);
                ProceduralRiverService.PromoteRiverBedInnerCornerShoreContinuity(hm, gw, gh, waterFootprint, riverBedCarvedCells);

                int lastSurface = int.MinValue;
                int currentBodyId = -1;
                foreach (ProceduralRiverService.RiverCrossSectionData sec in crossSections)
                {
                    if (sec.Bed.Count == 0 || sec.AppliedBedHeight < 0) continue;
                    int surface = Mathf.Min(TerrainManager.MAX_HEIGHT, sec.AppliedBedHeight + 1);
                    if (surface != lastSurface || currentBodyId < 0)
                    {
                        currentBodyId = wm.CreateRiverWaterBody(surface);
                        lastSurface = surface;
                    }

                    foreach (Vector2Int p in sec.Bed)
                    {
                        if (hm.GetHeight(p.x, p.y) != sec.AppliedBedHeight) continue;
                        if (wm.IsWater(p.x, p.y))
                            wm.TryReassignCellFromAnyWaterToRiverBody(p.x, p.y, currentBodyId);
                        else
                            wm.TryAssignCellToRiverBody(p.x, p.y, currentBodyId);
                    }
                }

                if (corridorFootprint.Count > 0)
                {
                    ProceduralRiverService.GetFootprintBounds(corridorFootprint, gw, gh, out int bx0, out int by0, out int bx1, out int by1);
                    terrainManager.ApplyHeightMapToRegion(bx0, by0, bx1, by1);
                }
            }
        }
    }
}
