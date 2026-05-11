using System;
using System.Collections.Generic;
using Territory.Terrain;
using Territory.Core;
using UnityEngine;

namespace Domains.Water.Services
{
    /// <summary>
    /// H_bed monotonic write paths extracted from WaterMap for testability.
    /// No MonoBehaviour dependency. Owns bed-alignment sweeps; delegates cell writes via HeightMap + WaterMap.
    /// Extracted per Strategy γ atomization (Stage 3.2, TECH-30018).
    /// Invariant #8 (H_bed monotonic): bed-alignment logic preserved verbatim from WaterMap.
    /// </summary>
    public class WaterDepthService
    {
        // ─────────────────────────────────────────────────────────────────────────
        // Pass A: multi-body surface boundary normalization (bed-height alignment)
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Aligns HeightMap bed heights on cardinal contact strip where higher logical water surface meets lower.
        /// Delegates to WaterMap for water queries; mutates HeightMap directly.
        /// Mirrors WaterMap.ApplyMultiBodySurfaceBoundaryNormalization verbatim.
        /// </summary>
        public void ApplyMultiBodySurfaceBoundaryNormalization(WaterMap waterMap, HeightMap heightMap)
        {
            if (waterMap == null || heightMap == null) return;
            const int maxPassAIterations = 24;
            for (int i = 0; i < maxPassAIterations; i++)
                if (!AlignUpperSurfaceContactBorderBedHeightsOnce(waterMap, heightMap))
                    break;
        }

        private bool AlignUpperSurfaceContactBorderBedHeightsOnce(WaterMap waterMap, HeightMap hm)
        {
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            bool any = false;
            int w = waterMap.Width;
            int h = waterMap.Height;

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (!waterMap.IsWater(x, y)) continue;
                    int sHere = waterMap.GetSurfaceHeightAt(x, y);
                    if (sHere < 0) continue;
                    if (HasCardinalWaterNeighborAtSameSurface(waterMap, x, y, sHere)) continue;
                    int targetBed = int.MaxValue;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + d4x[i];
                        int ny = y + d4y[i];
                        if (!waterMap.IsValidPosition(nx, ny) || !waterMap.IsWater(nx, ny)) continue;
                        int sN = waterMap.GetSurfaceHeightAt(nx, ny);
                        if (sN < 0 || sN >= sHere) continue;
                        if (waterMap.IsLakeSurfaceStepContactForbidden(x, y, nx, ny)) continue;
                        targetBed = Mathf.Min(targetBed, hm.GetHeight(nx, ny));
                    }
                    if (targetBed == int.MaxValue) continue;
                    int clamped = Mathf.Clamp(targetBed, HeightMap.MIN_HEIGHT, HeightMap.MAX_HEIGHT);
                    if (hm.GetHeight(x, y) == clamped) continue;
                    hm.SetHeight(x, y, clamped);
                    any = true;
                }
            return any;
        }

        private static bool HasCardinalWaterNeighborAtSameSurface(WaterMap waterMap, int x, int y, int surface)
        {
            if (surface < 0) return false;
            int[] d4x = { 1, -1, 0, 0 };
            int[] d4y = { 0, 0, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                int nx = x + d4x[i];
                int ny = y + d4y[i];
                if (!waterMap.IsValidPosition(nx, ny) || !waterMap.IsWater(nx, ny)) continue;
                if (waterMap.GetSurfaceHeightAt(nx, ny) == surface) return true;
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Pass B: junction merge + contact-bed reassignment
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Delegates to WaterMap.ApplyWaterSurfaceJunctionMerge verbatim.
        /// Invariant #8: H_bed monotonic constraint preserved — upper cells aligned to lower pool before reassign.
        /// </summary>
        public bool ApplyWaterSurfaceJunctionMerge(
            WaterMap waterMap, HeightMap heightMap, IGridManager gridManager,
            out int dirtyMinX, out int dirtyMinY, out int dirtyMaxX, out int dirtyMaxY)
        {
            return waterMap.ApplyWaterSurfaceJunctionMerge(heightMap, gridManager,
                out dirtyMinX, out dirtyMinY, out dirtyMaxX, out dirtyMaxY);
        }

        /// <summary>
        /// Lake-high-to-river-low contact fallback: removes lake cells that touch lower river, sets terrain.
        /// Delegates to WaterMap.ApplyLakeHighToRiverLowContactFallback verbatim.
        /// </summary>
        public bool ApplyLakeHighToRiverLowContactFallback(
            WaterMap waterMap, HeightMap heightMap, IGridManager gridManager,
            out List<(int x, int y, int lakeSurface)> restoredCells)
        {
            return waterMap.ApplyLakeHighToRiverLowContactFallback(heightMap, gridManager, out restoredCells);
        }
    }
}
