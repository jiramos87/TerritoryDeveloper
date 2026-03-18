using System.Collections.Generic;
using UnityEngine;
using Territory.Terrain;
using Territory.Core;

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
    /// Applies the terraforming plan: sets heights on the heightmap and refreshes terrain visuals.
    /// Two-phase: first set all heights so RestoreTerrainForCell sees correct neighbors, then restore terrain.
    /// Uses forceFlat/forceSlopeType so terrain matches the plan regardless of apply order.
    /// Returns false if validation fails (height diff &gt; 1); in that case reverts and does not apply Phase 2.
    /// </summary>
    public bool Apply(HeightMap heightMap, TerrainManager terrainManager)
    {
        if (heightMap == null || terrainManager == null) return false;

        // Phase 1: Set all heights so neighbor-dependent terrain logic sees correct state.
        // Skip water and water slope cells - bridge goes on top, no terrain modification.
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

        if (!ValidateNoHeightDiffGreaterThanOne(heightMap))
        {
            // Only revert heights; terrain was never changed (Phase 2 not reached). Avoids expensive RestoreTerrainForCell for failed attempts.
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
            return false;
        }

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
                bool orthogonalSlope = cell.postTerraformSlopeType == TerrainSlopeType.North || cell.postTerraformSlopeType == TerrainSlopeType.South
                    || cell.postTerraformSlopeType == TerrainSlopeType.East || cell.postTerraformSlopeType == TerrainSlopeType.West;
                terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y, null, forceFlat: flat && !orthogonalSlope, forceSlopeType: orthogonalSlope ? cell.postTerraformSlopeType : null);
            }
            else if (cell.postTerraformSlopeType != TerrainSlopeType.Flat)
            {
                bool orthogonalSlope = cell.postTerraformSlopeType == TerrainSlopeType.North || cell.postTerraformSlopeType == TerrainSlopeType.South
                    || cell.postTerraformSlopeType == TerrainSlopeType.East || cell.postTerraformSlopeType == TerrainSlopeType.West;
                if (orthogonalSlope)
                    terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y, null, forceFlat: false, forceSlopeType: cell.postTerraformSlopeType);
            }
            else
            {
                terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y);
            }
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
            {
                int h = heightMap.GetHeight(cell.position.x, cell.position.y);
                if (h <= TerrainManager.SEA_LEVEL || terrainManager.IsWaterSlopeCell(cell.position.x, cell.position.y))
                    continue;
                terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y, null, forceFlat: true);
            }
        }

        // Phase 3: Refresh cardinal neighbors of all modified cells so their slope/cliff sprites
        // update to match the new height landscape (prevents black gaps at height transitions).
        var refreshed = new HashSet<Vector2Int>();
        foreach (var cell in pathCells)
            refreshed.Add(cell.position);
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None)
                refreshed.Add(cell.position);
        }

        int[] ndx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] ndy = { 0, 0, 1, -1, 1, -1, 1, -1 };
        var neighborsToRefresh = new HashSet<Vector2Int>();
        foreach (var pos in refreshed)
        {
            for (int d = 0; d < 8; d++)
            {
                var np = new Vector2Int(pos.x + ndx[d], pos.y + ndy[d]);
                if (!refreshed.Contains(np) && heightMap.IsValidPosition(np.x, np.y))
                    neighborsToRefresh.Add(np);
            }
        }
        foreach (var np in neighborsToRefresh)
            terrainManager.RestoreTerrainForCell(np.x, np.y);

        return true;
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
                {
                    Debug.LogWarning($"[Terraform] Validation failed: ({pos.x},{pos.y}) h={h} vs neighbor ({nx},{ny}) nh={nh} diff={Mathf.Abs(nh - h)}");
                    return false;
                }
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

        // Phase 3: Refresh cardinal neighbors so their slope/cliff sprites match restored heights.
        var refreshed = new HashSet<Vector2Int>();
        foreach (var cell in pathCells)
            refreshed.Add(cell.position);
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None)
                refreshed.Add(cell.position);
        }

        int[] ndx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] ndy = { 0, 0, 1, -1, 1, -1, 1, -1 };
        var neighborsToRefresh = new HashSet<Vector2Int>();
        foreach (var pos in refreshed)
        {
            for (int d = 0; d < 8; d++)
            {
                var np = new Vector2Int(pos.x + ndx[d], pos.y + ndy[d]);
                if (!refreshed.Contains(np) && heightMap.IsValidPosition(np.x, np.y))
                    neighborsToRefresh.Add(np);
            }
        }
        foreach (var np in neighborsToRefresh)
            terrainManager.RestoreTerrainForCell(np.x, np.y);
    }
}
