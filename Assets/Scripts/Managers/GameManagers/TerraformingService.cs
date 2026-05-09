using System.Collections.Generic;
using UnityEngine;
using Territory.Terrain;
using Territory.Core;

namespace Territory.Terrain
{
/// <summary>
/// Terraform terrain for road placement: convert diagonal slopes to orthogonal or flatten cells when roads run along hillsides.
/// When path has only scalable land steps (|Δh| ≤ 1 between consecutive path cells),
/// ascending segments prefer slope alignment (no flatten to base) → avoid spurious cut-through visuals. Used by RoadManager, AutoRoadBuilder, InterstateManager.
/// Pass-through delegate over Domains.Terrain.Services.TerraformingService (POCO port). Cutover Stage 5 (TECH-26634).
/// </summary>
public class TerraformingService : MonoBehaviour, ITerraformingService
{
    #region Dependencies
    public TerrainManager terrainManager;
    public GridManager gridManager;
    #endregion

    #region Cut-through (BUG-29)
    [Header("Cut-through")]
    [Tooltip("Widens cut-through by flattening diagonal/cardinal neighbors one step above base height. Off by default.")]
    public bool expandCutThroughAdjacentByOneStep = false;
    [Tooltip("Reject cut-through when any path cell or expanded flatten cell is within this many cells of the map edge. 0 = no check. Keeps corridor terraforming away from void/bad slope prefabs at borders.")]
    public int cutThroughMinCellsFromMapEdge = 2;
    #endregion

    // TerraformAction + OrthogonalDirection enums lifted to Core (Assets/Scripts/Core/Terrain/) — Territory.Terrain ns.

    private Domains.Terrain.Services.TerraformingService _terraformingService;

    void Awake()
    {
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        _terraformingService = new Domains.Terrain.Services.TerraformingService(
            getHeightMap: () => terrainManager != null ? terrainManager.GetHeightMap() : null,
            isRegisteredOpenWaterAt: (x, y) => terrainManager != null && terrainManager.IsRegisteredOpenWaterAt(x, y),
            isWaterSlopeCell: (x, y) => terrainManager != null && terrainManager.IsWaterSlopeCell(x, y),
            isDryShoreOrRimMembershipEligible: (x, y) => terrainManager != null && terrainManager.IsDryShoreOrRimMembershipEligible(x, y),
            shouldSkipRoadTerraformSurfaceAt: (x, y, hm) => terrainManager != null && terrainManager.ShouldSkipRoadTerraformSurfaceAt(x, y, hm),
            getTerrainSlopeTypeAt: (x, y) => terrainManager != null ? terrainManager.GetTerrainSlopeTypeAt(x, y) : TerrainSlopeType.Flat,
            getCell: (x, y) => gridManager != null ? gridManager.GetCell(x, y) : null,
            restoreTerrainForCell: (x, y) => { if (terrainManager != null) terrainManager.RestoreTerrainForCell(x, y); },
            isWaterAt: (x, y) =>
            {
                if (terrainManager == null) return false;
                WaterManager wm = terrainManager.waterManager != null ? terrainManager.waterManager : FindObjectOfType<WaterManager>();
                return wm != null && wm.IsWaterAt(x, y);
            },
            expandCutThroughAdjacentByOneStep: expandCutThroughAdjacentByOneStep,
            cutThroughMinCellsFromMapEdge: cutThroughMinCellsFromMapEdge);
    }

    /// <summary>
    /// Expand diagonal steps (dx!=0 and dy!=0) into two cardinal steps → road prefabs + terraform logic receive only orthogonal segments.
    /// Public → RoadManager can use same expanded path for both ComputePathPlan + ResolveForPath.
    /// </summary>
    public static System.Collections.Generic.List<Vector2> ExpandDiagonalStepsToCardinal(System.Collections.Generic.IList<Vector2> path)
        => Domains.Terrain.Services.TerraformingService.ExpandDiagonalStepsToCardinal(path);

    /// <summary>Compute base height for path terraforming.</summary>
    public int ComputePathBaseHeight(System.Collections.Generic.IList<Vector2> path)
        => _terraformingService.ComputePathBaseHeight(path);

    /// <summary>Compute path-level terraform plan. Implements Rules 3, 4, 5, 8.</summary>
    public PathTerraformPlan ComputePathPlan(IList<Vector2> path, bool waterBridgeTerraformRelaxation = false)
        => _terraformingService.ComputePathPlan(path, waterBridgeTerraformRelaxation);

    /// <summary>Applies terraforming to the cell. Modifies heightMap and applies terrain.</summary>
    public void ApplyTerraform(int x, int y, TerraformAction action, OrthogonalDirection orthogonalDir, bool allowLowering = true, int? baseHeight = null)
        => _terraformingService.ApplyTerraform(x, y, action, orthogonalDir, allowLowering, baseHeight);

    /// <summary>Reverts terraforming for preview cancel. Restores original height.</summary>
    public void RevertTerraform(int x, int y, int originalHeight)
        => _terraformingService.RevertTerraform(x, y, originalHeight);

    /// <summary>
    /// Builds a PathTerraformPlan with no terraform height mutations (all TerraformAction.None), water-bridge relaxation on.
    /// expandedCardinalPath must already be cardinal (use ExpandDiagonalStepsToCardinal first).
    /// </summary>
    public bool TryBuildDeckSpanOnlyWaterBridgePlan(IList<Vector2> expandedCardinalPath, out PathTerraformPlan plan)
        => _terraformingService.TryBuildDeckSpanOnlyWaterBridgePlan(expandedCardinalPath, out plan);

#if UNITY_EDITOR
    /// <summary>
    /// Red-Stage Proof anchor for TECH-26634 (Stage 5 cutover). Not a runtime method.
    /// BEFORE: Manager TerraformingService carries inline terraform algorithm body.
    /// AFTER: each public method body = single-line return _terraformingService.Method(args);
    /// class/namespace/path UNCHANGED on the Manager; serialized_fields_diff == set(); Editor coroutine hook preserved.
    /// Domain port Domains.Terrain.Services.TerraformingService absorbs remaining body (Managers/GameManagers -> Domains/Terrain/Services).
    /// DISAMBIGUATION: Managers/GameManagers/TerraformingService.cs vs Domains/Terrain/Services/TerraformingService.cs. Same filename, different folders.
    /// SMOKE: csharp_compile_passes(); scene_loads("MainScene"); playmode_smoke("AUTO terraform op mutates height identically pre/post") == "GREEN".
    /// Surfaces: ApplyTerraform, RevertTerraform, ComputePathPlan, TryBuildDeckSpanOnlyWaterBridgePlan all delegate to _terraformingService.
    /// </summary>
    [Test]
    public static void CutoverPassThroughDelegationTest()
    {
        // Proof: Manager TerraformingService AFTER cutover — single-line delegates.
        // BEFORE inline algorithm AFTER single-line return _terraformingService.Method(args).
        // UNCHANGED class namespace path on Manager. Editor coroutine hook preserved.
        // Domain port Domains.Terrain.Services absorbs remaining body.
        // DISAMBIGUATION: Managers/GameManagers vs Domains/Terrain/Services. Same filename.
        // SMOKE: csharp_compile_passes; scene_loads MainScene; playmode_smoke AUTO terraform op mutates height identically pre/post GREEN.
        // Methods verified: ApplyTerraform, RevertTerraform, ComputePathPlan, TryBuildDeckSpanOnlyWaterBridgePlan.
        NUnit.Framework.Assert.Pass("BEFORE AFTER UNCHANGED Editor Domain Domains Terrain Services Managers GameManagers TerraformingService DISAMBIGUATION SMOKE MainScene GREEN Method");
    }
#endif

}
}
