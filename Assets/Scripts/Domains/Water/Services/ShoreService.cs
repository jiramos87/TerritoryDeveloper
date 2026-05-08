using System.Collections.Generic;
using Territory.Terrain;
using UnityEngine;

namespace Domains.Water.Services
{
    /// <summary>
    /// Shore and junction query logic extracted from WaterMap for testability.
    /// No MonoBehaviour dependency. Provides shore affiliation + cascade brink classification.
    /// Extracted from Territory.Terrain.WaterMap per Strategy γ atomization (TECH-23777).
    /// Invariant #7 (shore band): affiliation rules preserved from WaterMap.
    /// Guardrail #6 (RefreshShoreTerrainAfterWaterUpdate): caller (WaterManager/TerrainManager) responsibility.
    /// </summary>
    public class ShoreService
    {
        private static readonly int[] D4X = { 1, -1, 0, 0 };
        private static readonly int[] D4Y = { 0, 0, 1, -1 };
        private static readonly int[] DiagX = { 1, 1, -1, -1 };
        private static readonly int[] DiagY = { -1, 1, -1, 1 };

        /// <summary>
        /// True when a dry cell at (x,y) qualifies as a river-junction brink (§12.8).
        /// Outputs the role + affiliated body id.
        /// Lower brink tested before upper → correct cascade shore closure.
        /// </summary>
        public bool TryGetDryLandRiverJunctionBrink(
            WaterMap waterMap,
            int x, int y,
            out RiverJunctionBrinkRole role,
            out int affiliatedBodyId)
        {
            return TryGetDryLandRiverJunctionBrinkWithStep(waterMap, x, y,
                out role, out affiliatedBodyId, out _, out _, out _, out _);
        }

        /// <summary>
        /// Same as <see cref="TryGetDryLandRiverJunctionBrink"/> but also outputs
        /// high- + low-surface water cells of the qualifying river-river step (§12.8).
        /// </summary>
        public bool TryGetDryLandRiverJunctionBrinkWithStep(
            WaterMap waterMap,
            int x, int y,
            out RiverJunctionBrinkRole role, out int affiliatedBodyId,
            out int highX, out int highY, out int lowX, out int lowY)
        {
            return waterMap.TryGetDryLandRiverJunctionBrinkWithStep(
                x, y, out role, out affiliatedBodyId,
                out highX, out highY, out lowX, out lowY);
        }

        /// <summary>
        /// True when water cell at (x,y) is on lower side of a cardinal water-water surface step (§12.7).
        /// </summary>
        public bool IsWaterCellLowerSideOfCardinalSurfaceStep(WaterMap waterMap, int x, int y)
        {
            return waterMap.IsWaterCellLowerSideOfCardinalSurfaceStep(x, y);
        }

        /// <summary>
        /// True when Step high→low is lake-forbidden (§12.7): either body is Lake.
        /// </summary>
        public bool IsLakeSurfaceStepContactForbidden(WaterMap waterMap, int highX, int highY, int lowX, int lowY)
        {
            return waterMap.IsLakeSurfaceStepContactForbidden(highX, highY, lowX, lowY);
        }

        /// <summary>
        /// Among dry land cells sharing same brink role + step, return true only for cell closest
        /// to cascade junction. Ensures single diagonal SlopeWater closure tile per shore strip (§12.8).
        /// </summary>
        public bool IsDryLandRiverJunctionBrinkClosestToCascadeStep(
            WaterMap waterMap,
            int x, int y,
            RiverJunctionBrinkRole role,
            int stepHighX, int stepHighY,
            int stepLowX, int stepLowY)
        {
            return waterMap.IsDryLandRiverJunctionBrinkClosestToCascadeStep(
                x, y, role, stepHighX, stepHighY, stepLowX, stepLowY);
        }

        /// <summary>
        /// True when some cardinal river-river surface step between bodyA + bodyB
        /// lies within Chebyshev distance searchRadius of (x,y).
        /// Used for junction cascade shore post-pass (§12.8.1).
        /// </summary>
        public bool TryFindRiverRiverSurfaceStepBetweenBodiesNear(
            WaterMap waterMap,
            int x, int y,
            int bodyA, int bodyB,
            int searchRadius)
        {
            return waterMap.TryFindRiverRiverSurfaceStepBetweenBodiesNear(x, y, bodyA, bodyB, searchRadius);
        }

        /// <summary>
        /// Returns dry-land cardinal neighbors of water cells — shore band cells (§12.7).
        /// Collects all cells in Moore neighborhood of each water cell that are not water.
        /// </summary>
        public List<Vector2Int> GetShoreBandCells(WaterMap waterMap, int width, int height)
        {
            var shore = new HashSet<Vector2Int>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!waterMap.IsWater(x, y))
                        continue;
                    for (int i = 0; i < 4; i++)
                    {
                        int nx = x + D4X[i];
                        int ny = y + D4Y[i];
                        if (!waterMap.IsValidPosition(nx, ny) || waterMap.IsWater(nx, ny))
                            continue;
                        shore.Add(new Vector2Int(nx, ny));
                    }
                }
            }
            return new List<Vector2Int>(shore);
        }
    }
}
