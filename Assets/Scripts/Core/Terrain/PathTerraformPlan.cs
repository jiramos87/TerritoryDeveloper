using System;
using System.Collections.Generic;
using UnityEngine;
using Territory.Terrain;
using Territory.Core;

namespace Territory.Terrain
{
/// <summary>
/// Result of path-level terraforming analysis. Per-cell actions, target heights, post-terraform slope types.
/// Apply/Revert modify heightmap + terrain visuals for preview or permanent placement.
/// Validation + neighbor-wave logic: see PathTerraformPlan.Validation.cs (Strategy γ Stage 3.2).
/// </summary>
public partial class PathTerraformPlan
{
    /// <summary>Per-cell terraform plan. Populated by TerraformingService.ComputePathPlan.</summary>
    public struct CellPlan
    {
        public Vector2Int position;
        public TerraformAction action;
        public OrthogonalDirection direction;
        public int originalHeight;
        public int targetHeight;
        public TerrainSlopeType postTerraformSlopeType;
    }

    public int baseHeight;
    public List<CellPlan> pathCells = new List<CellPlan>();
    public List<CellPlan> adjacentCells = new List<CellPlan>();
    public bool isValid = true;

    /// <summary>
    /// True → only path cells flattened; adjacent terrain keeps height.
    /// Creates "cut" with slopes/cliffs between higher sectors + lowered road.
    /// </summary>
    public bool isCutThrough;

    /// <summary>
    /// True (FEAT-44 water bridge after <see cref="TerraformingService.ComputePathPlan"/>) → Phase1 validate/apply skip strict |Δh|≤1
    /// on edges touching open water or water-slope shore. Cut-through mode skips beside-cliff invalidation for that path.
    /// </summary>
    public bool waterBridgeTerraformRelaxation;

    /// <summary>
    /// FEAT-44: uniform height for <b>all</b> bridge deck prefabs on span (world Y / sorting). Set by <see cref="TerraformingService.TryAssignWaterBridgeDeckDisplayHeight"/> / <see cref="TerraformingService.ComputePathPlan"/> when <see cref="waterBridgeTerraformRelaxation"/> applies. 0 = unset (legacy placement).
    /// </summary>
    public int waterBridgeDeckDisplayHeight;

    /// <summary>
    /// Phase 1 only: write planned terraform heights. Skips registered open water + water-slope cells (same rules as <see cref="Apply"/>; geography spec water map).
    /// </summary>
    void WritePhase1TerraformHeights(HeightMap heightMap, ITerrainManager terrainManager)
    {
        foreach (var cell in pathCells)
        {
            if (cell.action != TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
            {
                if (terrainManager.ShouldSkipRoadTerraformSurfaceAt(cell.position.x, cell.position.y, heightMap))
                    continue;
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.targetHeight);
            }
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
            {
                if (terrainManager.ShouldSkipRoadTerraformSurfaceAt(cell.position.x, cell.position.y, heightMap))
                    continue;
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.targetHeight);
            }
        }
    }

    /// <summary>Revert heights written in Phase 1 using stored <see cref="CellPlan.originalHeight"/>.</summary>
    void RevertPhase1TerraformHeights(HeightMap heightMap)
    {
        foreach (var cell in pathCells)
        {
            if (cell.action != TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.originalHeight);
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.originalHeight);
        }
    }

    /// <summary>
    /// Apply Phase 1 height writes + <see cref="ValidateNoHeightDiffGreaterThanOne"/> without Phase 2 terrain meshes, then restore heights.
    /// Use so preview/path prep match <see cref="Apply"/> feasibility before modifying sprites.
    /// </summary>
    /// <param name="logPhase1HeightFailure">On validation fail, invoked once with failing cardinal edge (or null if unknown).</param>
    /// <param name="logDryCliffPhase1Detail">If non-null, emits verbose lines for strict Phase1 |Δh|&gt;1 edges + dry-cliff skip decisions. Use with road diagnostic cursor only.</param>
    public bool TryValidatePhase1Heights(HeightMap heightMap, ITerrainManager terrainManager, Action<string> logPhase1HeightFailure = null, Action<string> logDryCliffPhase1Detail = null)
    {
        if (heightMap == null || terrainManager == null) return false;
        WritePhase1TerraformHeights(heightMap, terrainManager);
        string failureDetail = null;
        bool heightOk;
        if (waterBridgeTerraformRelaxation && !PlanHasTerraformHeightMutation())
            heightOk = true;
        else if (waterBridgeTerraformRelaxation)
            heightOk = ValidateNoHeightDiffGreaterThanOneWaterBridgeRelaxed(heightMap, terrainManager, out failureDetail);
        else
            heightOk = ValidateNoHeightDiffGreaterThanOne(heightMap, terrainManager, out failureDetail, logDryCliffPhase1Detail);
        if (!heightOk)
        {
            // Strict Phase1 emits REJECT via logDryCliffPhase1Detail; relaxed validation does not — always log Phase1 for bridge-relaxed failures.
            bool strictPhase1 = !waterBridgeTerraformRelaxation;
            bool skipPhase1Duplicate = strictPhase1 && logDryCliffPhase1Detail != null;
            if (!string.IsNullOrEmpty(failureDetail) && !skipPhase1Duplicate)
                logPhase1HeightFailure?.Invoke(failureDetail);
            RevertPhase1TerraformHeights(heightMap);
            return false;
        }
        RevertPhase1TerraformHeights(heightMap);
        return true;
    }

    /// <summary>
    /// Cardinal ramp types compatible with <see cref="TerrainManager.GetOrthogonalSlopePrefab"/> (N/S/E/W).
    /// Corner / diagonal path cells store these after <see cref="TerraformingService.ComputePathPlan"/> (BUG-30).
    /// </summary>
    static bool IsOrthogonalRampSlopeType(TerrainSlopeType t)
    {
        return t == TerrainSlopeType.North || t == TerrainSlopeType.South
            || t == TerrainSlopeType.East || t == TerrainSlopeType.West;
    }

    /// <summary>
    /// Apply terraform plan: set heights on heightmap + refresh terrain visuals.
    /// Two-phase: set all heights first so RestoreTerrainForCell sees correct neighbors, then restore terrain.
    /// Uses forceFlat/forceSlopeType → terrain matches plan regardless of apply order.
    /// Return false on validation fail (|Δh|&gt;1); reverts + skips Phase 2.
    /// </summary>
    public bool Apply(HeightMap heightMap, ITerrainManager terrainManager)
    {
        if (heightMap == null || terrainManager == null) return false;

        WritePhase1TerraformHeights(heightMap, terrainManager);

        bool heightOk;
        if (waterBridgeTerraformRelaxation && !PlanHasTerraformHeightMutation())
            heightOk = true;
        else if (waterBridgeTerraformRelaxation)
            heightOk = ValidateNoHeightDiffGreaterThanOneWaterBridgeRelaxed(heightMap, terrainManager, out _);
        else
            heightOk = ValidateNoHeightDiffGreaterThanOne(heightMap, terrainManager, out _, logDryCliffPhase1Detail: null);
        if (!heightOk)
        {
            RevertPhase1TerraformHeights(heightMap);
            return false;
        }

        HashSet<Vector2Int> cutCorridorCells = BuildTerraformCutCorridorSet();

        // Phase 2: Restore terrain for all affected cells with correct force flags.
        // Refresh all path cells (neighbors may have changed) and modified adjacent cells.
        // Skip water and water slope cells - bridge goes on top, no terrain modification.
        foreach (var cell in pathCells)
        {
            if (!heightMap.IsValidPosition(cell.position.x, cell.position.y)) continue;
            if (terrainManager.ShouldSkipRoadTerraformSurfaceAt(cell.position.x, cell.position.y, heightMap))
                continue;
            if (cell.action != TerraformAction.None)
            {
                bool flat = cell.postTerraformSlopeType == TerrainSlopeType.Flat;
                bool orthogonalSlope = IsOrthogonalRampSlopeType(cell.postTerraformSlopeType);
                terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y, null, forceFlat: flat && !orthogonalSlope, forceSlopeType: orthogonalSlope ? cell.postTerraformSlopeType : null, terraformCutCorridorCells: cutCorridorCells);
            }
            else if (cell.postTerraformSlopeType != TerrainSlopeType.Flat)
            {
                // action None: scale-with-slopes cells (incl. former corner / valley tiles) — force orthogonal terrain to match cardinal ramp from travel.
                if (IsOrthogonalRampSlopeType(cell.postTerraformSlopeType))
                    terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y, null, forceFlat: false, forceSlopeType: cell.postTerraformSlopeType, terraformCutCorridorCells: cutCorridorCells);
            }
            else
            {
                terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y, terraformCutCorridorCells: cutCorridorCells);
            }
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
            {
                if (terrainManager.ShouldSkipRoadTerraformSurfaceAt(cell.position.x, cell.position.y, heightMap))
                    continue;
                terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y, null, forceFlat: true, forceSlopeType: null, terraformCutCorridorCells: cutCorridorCells);
            }
        }

        // Phase 3: Refresh rings of 8-neighbors so slope/cliff sprites match (second ring for cut-through — BUG-29).
        var refreshed = new HashSet<Vector2Int>();
        foreach (var cell in pathCells)
            refreshed.Add(cell.position);
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformAction.None)
                refreshed.Add(cell.position);
        }

        int phase3NeighborWaves = isCutThrough ? 2 : 1;
        RefreshTerrainNeighborWaves(heightMap, terrainManager, refreshed, cutCorridorCells, phase3NeighborWaves);

        return true;
    }

    /// <summary>
    /// True if any path or adjacent cell would write terraform height after water/shore skips in <see cref=”WritePhase1TerraformHeights”/>.
    /// Deck-only water bridge plans return false (all <see cref=”TerraformAction.None”/> on path cells).
    /// </summary>
    public bool HasTerraformHeightMutation() => PlanHasTerraformHeightMutation();

    bool PlanHasTerraformHeightMutation()
    {
        for (int i = 0; i < pathCells.Count; i++)
        {
            if (pathCells[i].action != TerraformAction.None)
                return true;
        }
        for (int i = 0; i < adjacentCells.Count; i++)
        {
            if (adjacentCells[i].action != TerraformAction.None)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Revert terraform plan: restore original heights + refresh terrain visuals.
    /// Call on preview cancel. Two-phase like Apply → terrain sees correct neighbor heights.
    /// </summary>
    public void Revert(HeightMap heightMap, ITerrainManager terrainManager)
    {
        if (heightMap == null || terrainManager == null) return;

        // Phase 1: Restore all heights
        foreach (var cell in pathCells)
        {
            if (cell.action != TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.originalHeight);
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.originalHeight);
        }

        // Phase 2: Restore terrain for all affected cells (heightmap now has original state).
        // Refresh all path cells (neighbors may have changed) and modified adjacent cells.
        foreach (var cell in pathCells)
        {
            if (!heightMap.IsValidPosition(cell.position.x, cell.position.y)) continue;
            if (terrainManager.ShouldSkipRoadTerraformSurfaceAt(cell.position.x, cell.position.y, heightMap))
                continue;
            terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y);
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action == TerraformAction.None || !heightMap.IsValidPosition(cell.position.x, cell.position.y))
                continue;
            if (terrainManager.ShouldSkipRoadTerraformSurfaceAt(cell.position.x, cell.position.y, heightMap))
                continue;
            terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y);
        }

        // Phase 3: Refresh neighbors so slope/cliff sprites match restored heights (single ring on revert).
        var refreshed = new HashSet<Vector2Int>();
        foreach (var cell in pathCells)
            refreshed.Add(cell.position);
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformAction.None)
                refreshed.Add(cell.position);
        }

        RefreshTerrainNeighborWaves(heightMap, terrainManager, refreshed, null, 1);
    }
}
}
