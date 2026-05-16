// registry-resolve-exempt: internal factory — constructs own sub-services (TerraformSmoothService, TerraformPlanService, TerraformApplyService) within Terrain domain
using System.Collections.Generic;
using UnityEngine;
using Territory.Terrain;
using Territory.Core;

namespace Domains.Terrain.Services
{
    /// <summary>
    /// Thin orchestrator delegating to TerraformPlanService, TerraformApplyService, TerraformSmoothService.
    /// Facade contract preserved — public API unchanged. Extracted per TECH-30056 Stage 7.2 split.
    /// Invariants #1 (HeightMap/Cell sync), #7 (shore band) preserved.
    /// </summary>
    public class TerraformingService
    {
        private readonly TerraformPlanService _plan;
        private readonly TerraformApplyService _apply;
        private readonly TerraformSmoothService _smooth;

        // Store delegates for TryBuildDeckSpanOnlyWaterBridgePlan forwarding
        private readonly System.Func<int, int, bool> _isRegisteredOpenWaterAt;
        private readonly System.Func<int, int, bool> _isWaterSlopeCell;
        private readonly System.Func<int, int, Territory.Terrain.TerrainSlopeType> _getTerrainSlopeTypeAt;

        /// <summary>Construct terraforming facade with dependencies.</summary>
        public TerraformingService(
            System.Func<Territory.Terrain.HeightMap> getHeightMap,
            System.Func<int, int, bool> isRegisteredOpenWaterAt,
            System.Func<int, int, bool> isWaterSlopeCell,
            System.Func<int, int, bool> isDryShoreOrRimMembershipEligible,
            System.Func<int, int, Territory.Terrain.HeightMap, bool> shouldSkipRoadTerraformSurfaceAt,
            System.Func<int, int, Territory.Terrain.TerrainSlopeType> getTerrainSlopeTypeAt,
            System.Func<int, int, CityCell> getCell,
            System.Action<int, int> restoreTerrainForCell = null,
            System.Func<int, int, bool> isWaterAt = null,
            bool expandCutThroughAdjacentByOneStep = false,
            int cutThroughMinCellsFromMapEdge = 2)
        {
            _isRegisteredOpenWaterAt = isRegisteredOpenWaterAt;
            _isWaterSlopeCell = isWaterSlopeCell;
            _getTerrainSlopeTypeAt = getTerrainSlopeTypeAt;

            _smooth = new TerraformSmoothService(
                getHeightMap,
                isRegisteredOpenWaterAt,
                isWaterSlopeCell,
                getCell,
                isWaterAt);

            _plan = new TerraformPlanService(
                getHeightMap,
                isRegisteredOpenWaterAt,
                isWaterSlopeCell,
                isDryShoreOrRimMembershipEligible,
                shouldSkipRoadTerraformSurfaceAt,
                getTerrainSlopeTypeAt,
                _smooth,
                expandCutThroughAdjacentByOneStep,
                cutThroughMinCellsFromMapEdge);

            _apply = new TerraformApplyService(
                getHeightMap,
                restoreTerrainForCell);
        }

        /// <summary>Expand diagonal steps to cardinal. Delegates to TerraformPlanService.</summary>
        public static List<Vector2> ExpandDiagonalStepsToCardinal(IList<Vector2> path)
            => TerraformPlanService.ExpandDiagonalStepsToCardinal(path);

        /// <summary>Compute base height for path terraforming.</summary>
        public int ComputePathBaseHeight(IList<Vector2> path)
            => _plan.ComputePathBaseHeight(path);

        /// <summary>Compute path-level terraform plan. Implements Rules 3, 4, 5, 8.</summary>
        public Territory.Terrain.PathTerraformPlan ComputePathPlan(IList<Vector2> path, bool waterBridgeTerraformRelaxation = false)
            => _plan.ComputePathPlan(path, waterBridgeTerraformRelaxation);

        /// <summary>Applies terraforming to a cell: modifies heightMap and restores terrain visual.</summary>
        public void ApplyTerraform(int x, int y, TerraformAction action, OrthogonalDirection orthogonalDir, bool allowLowering = true, int? baseHeight = null)
            => _apply.ApplyTerraform(x, y, action, orthogonalDir, allowLowering, baseHeight);

        /// <summary>Reverts terraforming for preview cancel: restores original height.</summary>
        public void RevertTerraform(int x, int y, int originalHeight)
            => _apply.RevertTerraform(x, y, originalHeight);

        /// <summary>
        /// Builds a PathTerraformPlan with no terraform height mutations (all TerraformAction.None),
        /// water-bridge relaxation on, and waterBridgeDeckDisplayHeight from TryAssignWaterBridgeDeckDisplayHeight.
        /// expandedCardinalPath must already be cardinal.
        /// </summary>
        public bool TryBuildDeckSpanOnlyWaterBridgePlan(IList<Vector2> expandedCardinalPath, out Territory.Terrain.PathTerraformPlan plan)
            => _smooth.TryBuildDeckSpanOnlyWaterBridgePlan(
                expandedCardinalPath,
                _isRegisteredOpenWaterAt,
                _isWaterSlopeCell,
                _getTerrainSlopeTypeAt,
                out plan);
    }
}
