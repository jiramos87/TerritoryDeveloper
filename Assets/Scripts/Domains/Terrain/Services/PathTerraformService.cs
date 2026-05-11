using Territory.Core;
using Territory.Terrain;

namespace Domains.Terrain.Services
{
    /// <summary>
    /// Thin façade for PathTerraformPlan phase-1 validate/apply/revert operations (Strategy γ Stage 3.2 — invariant #10 road-prep family).
    /// Delegates directly to PathTerraformPlan; callers import this service instead of PathTerraformPlan directly when
    /// operating at the Domain layer.
    /// </summary>
    public static class PathTerraformService
    {
        /// <summary>Run Phase-1 height-write + validate cycle without committing terrain sprites. Returns false on |Δh|&gt;1 violation.</summary>
        public static bool TryValidatePhase1Heights(PathTerraformPlan plan, HeightMap heightMap, ITerrainManager terrainManager)
            => plan.TryValidatePhase1Heights(heightMap, terrainManager);

        /// <summary>Apply terraform plan (Phase 1 + 2 + 3 neighbor waves). Returns false on validation fail.</summary>
        public static bool Apply(PathTerraformPlan plan, HeightMap heightMap, ITerrainManager terrainManager)
            => plan.Apply(heightMap, terrainManager);

        /// <summary>Revert terraform plan to original heights + restore terrain sprites.</summary>
        public static void Revert(PathTerraformPlan plan, HeightMap heightMap, ITerrainManager terrainManager)
            => plan.Revert(heightMap, terrainManager);
    }
}
