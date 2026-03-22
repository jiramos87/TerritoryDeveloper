using System.Collections.Generic;
using UnityEngine;
using Territory.Terrain;
using Territory.Core;

namespace Territory.Terrain
{
/// <summary>
/// Holds the result of path-level terraforming analysis. Contains per-cell actions,
/// target heights, and post-terraform slope types. Apply/Revert modify the heightmap
/// and terrain visuals for preview or permanent placement.
/// </summary>
public class PathTerraformPlan
{
    /// <summary>
    /// Per-cell terraforming plan. Populated by TerraformingService.ComputePathPlan.
    /// </summary>
    public struct CellPlan
    {
        public Vector2Int position;
        public TerraformingService.TerraformAction action;
        public TerraformingService.OrthogonalDirection direction;
        public int originalHeight;
        public int targetHeight;
        public TerrainSlopeType postTerraformSlopeType;
    }

    public int baseHeight;
    public List<CellPlan> pathCells = new List<CellPlan>();
    public List<CellPlan> adjacentCells = new List<CellPlan>();
    public bool isValid = true;

    /// <summary>
    /// When true, only path cells are flattened; adjacent terrain keeps its height, creating
    /// a "cut" with slopes/cliffs between higher sectors and the lowered road.
    /// </summary>
    public bool isCutThrough;

    /// <summary>
    /// Phase 1 only: write planned terraform heights. Skips water and water-slope cells (same rules as <see cref="Apply"/>).
    /// </summary>
    void WritePhase1TerraformHeights(HeightMap heightMap, TerrainManager terrainManager)
    {
        foreach (var cell in pathCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
            {
                int h = heightMap.GetHeight(cell.position.x, cell.position.y);
                if (h <= TerrainManager.SEA_LEVEL || terrainManager.IsWaterSlopeCell(cell.position.x, cell.position.y))
                    continue;
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.targetHeight);
            }
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
            {
                int h = heightMap.GetHeight(cell.position.x, cell.position.y);
                if (h <= TerrainManager.SEA_LEVEL || terrainManager.IsWaterSlopeCell(cell.position.x, cell.position.y))
                    continue;
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.targetHeight);
            }
        }
    }

    /// <summary>
    /// Reverts heights written in Phase 1 using stored <see cref="CellPlan.originalHeight"/>.
    /// </summary>
    void RevertPhase1TerraformHeights(HeightMap heightMap)
    {
        foreach (var cell in pathCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.originalHeight);
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.originalHeight);
        }
    }

    /// <summary>
    /// Applies Phase 1 height writes and <see cref="ValidateNoHeightDiffGreaterThanOne"/> without Phase 2 terrain meshes, then restores heights.
    /// Use so preview/path prep match <see cref="Apply"/> feasibility before modifying sprites.
    /// </summary>
    public bool TryValidatePhase1Heights(HeightMap heightMap, TerrainManager terrainManager)
    {
        if (heightMap == null || terrainManager == null) return false;
        WritePhase1TerraformHeights(heightMap, terrainManager);
        if (!ValidateNoHeightDiffGreaterThanOne(heightMap))
        {
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
    /// Applies the terraforming plan: sets heights on the heightmap and refreshes terrain visuals.
    /// Two-phase: first set all heights so RestoreTerrainForCell sees correct neighbors, then restore terrain.
    /// Uses forceFlat/forceSlopeType so terrain matches the plan regardless of apply order.
    /// Returns false if validation fails (height diff &gt; 1); in that case reverts and does not apply Phase 2.
    /// </summary>
    public bool Apply(HeightMap heightMap, TerrainManager terrainManager)
    {
        if (heightMap == null || terrainManager == null) return false;

        WritePhase1TerraformHeights(heightMap, terrainManager);

        if (!ValidateNoHeightDiffGreaterThanOne(heightMap))
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
            int h = heightMap.GetHeight(cell.position.x, cell.position.y);
            if (h <= TerrainManager.SEA_LEVEL || terrainManager.IsWaterSlopeCell(cell.position.x, cell.position.y))
                continue;
            if (cell.action != TerraformingService.TerraformAction.None)
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
            if (cell.action != TerraformingService.TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
            {
                int h = heightMap.GetHeight(cell.position.x, cell.position.y);
                if (h <= TerrainManager.SEA_LEVEL || terrainManager.IsWaterSlopeCell(cell.position.x, cell.position.y))
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
            if (cell.action != TerraformingService.TerraformAction.None)
                refreshed.Add(cell.position);
        }

        int phase3NeighborWaves = isCutThrough ? 2 : 1;
        RefreshTerrainNeighborWaves(heightMap, terrainManager, refreshed, cutCorridorCells, phase3NeighborWaves);

        return true;
    }

    /// <summary>
    /// Positions lowered by this plan's flatten actions; used for 1-step cliff walls toward the cut (BUG-29).
    /// </summary>
    HashSet<Vector2Int> BuildTerraformCutCorridorSet()
    {
        if (!isCutThrough)
            return null;
        var set = new HashSet<Vector2Int>();
        foreach (var cell in pathCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None)
                set.Add(cell.position);
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None)
                set.Add(cell.position);
        }
        return set;
    }

    /// <summary>
    /// Expands <paramref name="touchedCore"/> by repeatedly restoring all 8-neighbors not yet touched (one wave per iteration).
    /// </summary>
    static void RefreshTerrainNeighborWaves(HeightMap heightMap, TerrainManager terrainManager, HashSet<Vector2Int> touchedCore, HashSet<Vector2Int> cutCorridorCells, int waveCount)
    {
        if (heightMap == null || terrainManager == null || touchedCore == null || waveCount < 1)
            return;

        int[] ndx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] ndy = { 0, 0, 1, -1, 1, -1, 1, -1 };
        var touched = new HashSet<Vector2Int>(touchedCore);
        for (int w = 0; w < waveCount; w++)
        {
            var nextWave = new HashSet<Vector2Int>();
            foreach (var pos in touched)
            {
                for (int d = 0; d < 8; d++)
                {
                    var np = new Vector2Int(pos.x + ndx[d], pos.y + ndy[d]);
                    if (!touched.Contains(np) && heightMap.IsValidPosition(np.x, np.y))
                        nextWave.Add(np);
                }
            }
            foreach (var np in nextWave)
                terrainManager.RestoreTerrainForCell(np.x, np.y, null, false, null, cutCorridorCells);
            foreach (var np in nextWave)
                touched.Add(np);
        }
    }

    /// <summary>
    /// Returns false if any affected cell has a neighbor with height difference greater than 1.
    /// Used after Phase 1 to avoid invalid terrain (black voids, degenerate slopes).
    /// </summary>
    bool ValidateNoHeightDiffGreaterThanOne(HeightMap heightMap)
    {
        if (heightMap == null) return true;
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        var checkSet = new HashSet<Vector2Int>();
        foreach (var cell in pathCells)
            checkSet.Add(cell.position);
        foreach (var cell in adjacentCells)
            checkSet.Add(cell.position);
        // Include cardinal neighbors of planned cells so cliff/gorge edges one step outside the plan are validated (P3).
        var ring = new List<Vector2Int>(checkSet);
        foreach (var pos in ring)
        {
            for (int d = 0; d < 4; d++)
            {
                int nx = pos.x + dx[d];
                int ny = pos.y + dy[d];
                if (heightMap.IsValidPosition(nx, ny))
                    checkSet.Add(new Vector2Int(nx, ny));
            }
        }
        foreach (var pos in checkSet)
        {
            if (!heightMap.IsValidPosition(pos.x, pos.y)) continue;
            int h = heightMap.GetHeight(pos.x, pos.y);
            for (int d = 0; d < 4; d++)
            {
                int nx = pos.x + dx[d];
                int ny = pos.y + dy[d];
                if (!heightMap.IsValidPosition(nx, ny)) continue;
                int nh = heightMap.GetHeight(nx, ny);
                if (Mathf.Abs(nh - h) > 1)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Reverts the terraforming plan: restores original heights and refreshes terrain visuals.
    /// Call when canceling a preview. Two-phase like Apply so terrain sees correct neighbor heights.
    /// </summary>
    public void Revert(HeightMap heightMap, TerrainManager terrainManager)
    {
        if (heightMap == null || terrainManager == null) return;

        // Phase 1: Restore all heights
        foreach (var cell in pathCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.originalHeight);
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.originalHeight);
        }

        // Phase 2: Restore terrain for all affected cells (heightmap now has original state).
        // Refresh all path cells (neighbors may have changed) and modified adjacent cells.
        foreach (var cell in pathCells)
        {
            if (heightMap.IsValidPosition(cell.position.x, cell.position.y))
                terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y);
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
                terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y);
        }

        // Phase 3: Refresh neighbors so slope/cliff sprites match restored heights (single ring on revert).
        var refreshed = new HashSet<Vector2Int>();
        foreach (var cell in pathCells)
            refreshed.Add(cell.position);
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None)
                refreshed.Add(cell.position);
        }

        RefreshTerrainNeighborWaves(heightMap, terrainManager, refreshed, null, 1);
    }
}
}
