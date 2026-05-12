using Territory.Terrain;
using Territory.Core;
using UnityEngine;

namespace Domains.Water.Services
{
    /// <summary>
    /// Water output/consumption accounting extracted from WaterManager hub (Stage 4.4 THIN).
    /// Pure class — no MonoBehaviour. References Territory.Core only (Water domain boundary).
    /// Plant registry is delegated via RegisterWaterProduction/UnregisterWaterProduction
    /// so WaterManager hub owns WaterPlant (Game layer) references (locked constraint #3).
    /// Invariants: #3 no per-frame FindObjectOfType, #4 no new singletons.
    /// </summary>
    public class WaterService
    {
        private int _cityWaterConsumption;
        private int _cityWaterOutput;

        // ─── Water output/consumption accounting ──────────────────────────────

        /// <summary>Register additional water production (call when a WaterPlant is added).</summary>
        public void RegisterWaterProduction(int outputValue) => _cityWaterOutput += outputValue;

        /// <summary>Unregister water production (call when a WaterPlant is removed).</summary>
        public void UnregisterWaterProduction(int outputValue) => _cityWaterOutput -= outputValue;

        /// <summary>Clear all water output (call when plant list is reset).</summary>
        public void ResetWaterOutput() => _cityWaterOutput = 0;

        public int GetTotalWaterOutput() => _cityWaterOutput;

        public void AddWaterConsumption(int value) => _cityWaterConsumption += value;
        public void RemoveWaterConsumption(int value) => _cityWaterConsumption -= value;
        public int GetTotalWaterConsumption() => _cityWaterConsumption;
        public bool GetCityWaterAvailability() => _cityWaterOutput > _cityWaterConsumption;

        // ─── Adjacency query (pure; no MonoBehaviour) ─────────────────────────

        /// <summary>True when any cardinal neighbor of (x,y) is registered water.</summary>
        public static bool IsAdjacentToWater(WaterMap waterMap, int x, int y)
        {
            if (waterMap == null) return false;
            int[] dx = { -1, 0, 1, 0 };
            int[] dy = { 0, 1, 0, -1 };
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (waterMap.IsValidPosition(nx, ny) && waterMap.IsWater(nx, ny))
                    return true;
            }
            return false;
        }

        // ─── Lake terrain refresh region computation ──────────────────────────

        /// <summary>
        /// Build inclusive rect for TerrainManager.ApplyHeightMapToRegion after lake fill.
        /// Moved from WaterManager static method (Stage 4.4 THIN).
        /// </summary>
        public static bool TryGetLakeTerrainRefreshRegion(
            WaterMap wm, int gridWidth, int gridHeight,
            out int minX, out int minY, out int maxX, out int maxY)
        {
            const int waterMargin = 3;
            minX = minY = maxX = maxY = 0;
            bool has = false;
            if (wm.TryGetAllWaterBoundingBox(out int wx0, out int wy0, out int wx1, out int wy1))
            {
                minX = wx0 - waterMargin; minY = wy0 - waterMargin;
                maxX = wx1 + waterMargin; maxY = wy1 + waterMargin;
                has = true;
            }
            if (wm.ArtificialDirtyMinX >= 0)
            {
                int ax0 = wm.ArtificialDirtyMinX; int ay0 = wm.ArtificialDirtyMinY;
                int ax1 = wm.ArtificialDirtyMaxX; int ay1 = wm.ArtificialDirtyMaxY;
                if (!has) { minX = ax0; minY = ay0; maxX = ax1; maxY = ay1; has = true; }
                else
                {
                    minX = Mathf.Min(minX, ax0); minY = Mathf.Min(minY, ay0);
                    maxX = Mathf.Max(maxX, ax1); maxY = Mathf.Max(maxY, ay1);
                }
            }
            if (!has) return false;
            minX = Mathf.Clamp(minX, 0, gridWidth - 1);
            maxX = Mathf.Clamp(maxX, 0, gridWidth - 1);
            minY = Mathf.Clamp(minY, 0, gridHeight - 1);
            maxY = Mathf.Clamp(maxY, 0, gridHeight - 1);
            return true;
        }
    }
}
