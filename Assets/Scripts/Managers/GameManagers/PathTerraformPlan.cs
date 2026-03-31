using System;
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
    /// When true (FEAT-44 water bridge after <see cref="TerraformingService.ComputePathPlan"/>), Phase1 validate/apply skip strict |Δh|≤1
    /// across edges that touch open water or water-slope shore, and cut-through mode skips beside-cliff invalidation for that path.
    /// </summary>
    public bool waterBridgeTerraformRelaxation;

    /// <summary>
    /// FEAT-44: land endpoint height for bridge deck prefab placement (world Y / sorting). Set by <see cref="TerraformingService.ComputePathPlan"/> when <see cref="waterBridgeTerraformRelaxation"/> is true. 0 = unset (legacy placement).
    /// </summary>
    public int waterBridgeDeckDisplayHeight;

    /// <summary>
    /// Phase 1 only: write planned terraform heights. Skips registered open water and water-slope cells (same rules as <see cref="Apply"/>; geography spec water map).
    /// </summary>
    void WritePhase1TerraformHeights(HeightMap heightMap, TerrainManager terrainManager)
    {
        foreach (var cell in pathCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
            {
                if (terrainManager.ShouldSkipRoadTerraformSurfaceAt(cell.position.x, cell.position.y, heightMap))
                    continue;
                heightMap.SetHeight(cell.position.x, cell.position.y, cell.targetHeight);
            }
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action != TerraformingService.TerraformAction.None && heightMap.IsValidPosition(cell.position.x, cell.position.y))
            {
                if (terrainManager.ShouldSkipRoadTerraformSurfaceAt(cell.position.x, cell.position.y, heightMap))
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
    /// <param name="logPhase1HeightFailure">When validation fails, invoked once with the failing cardinal edge (or null if unknown).</param>
    /// <param name="logDryCliffPhase1Detail">When non-null, emits verbose lines for strict Phase1 |Δh|&gt;1 edges and dry-cliff skip decisions (use with road diagnostic cursor only).</param>
    public bool TryValidatePhase1Heights(HeightMap heightMap, TerrainManager terrainManager, Action<string> logPhase1HeightFailure = null, Action<string> logDryCliffPhase1Detail = null)
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
    /// Applies the terraforming plan: sets heights on the heightmap and refreshes terrain visuals.
    /// Two-phase: first set all heights so RestoreTerrainForCell sees correct neighbors, then restore terrain.
    /// Uses forceFlat/forceSlopeType so terrain matches the plan regardless of apply order.
    /// Returns false if validation fails (height diff &gt; 1); in that case reverts and does not apply Phase 2.
    /// </summary>
    public bool Apply(HeightMap heightMap, TerrainManager terrainManager)
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
    /// <summary>
    /// Phase 3 neighbor refresh: do not rebuild open water, registered water, or water-slope tiles (FEAT-44 bridge — avoids grass pillars in the river).
    /// </summary>
    static bool ShouldSkipPhase3NeighborTerrainRefresh(HeightMap heightMap, TerrainManager terrainManager, int x, int y)
    {
        if (terrainManager == null) return true;
        return terrainManager.ShouldSkipRoadTerraformSurfaceAt(x, y, heightMap);
    }

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
            {
                if (ShouldSkipPhase3NeighborTerrainRefresh(heightMap, terrainManager, np.x, np.y))
                    continue;
                terrainManager.RestoreTerrainForCell(np.x, np.y, null, false, null, cutCorridorCells);
            }
            foreach (var np in nextWave)
                touched.Add(np);
        }
    }

    /// <summary>
    /// Returns false if any affected cell has a neighbor with height difference greater than 1.
    /// Used after Phase 1 to avoid invalid terrain (black voids, degenerate slopes).
    /// Skips edges where the stroke sits on high ground and a cardinal neighbor is dry land strictly lower (pre-existing cliff), same rule as FEAT-44 relaxed Phase1 for water bridges.
    /// Also skips high dry land (on or beside stroke) dropping to a lower registered water-slope shore cell so manual roads can preview to cliff lips above NorthSlopeWaterPrefab tiles without full water-bridge relaxation.
    /// </summary>
    bool ValidateNoHeightDiffGreaterThanOne(HeightMap heightMap, TerrainManager terrainManager, out string failureDetail, Action<string> logDryCliffPhase1Detail = null)
    {
        failureDetail = null;
        if (heightMap == null) return true;
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        var pathCellsOnly = new HashSet<Vector2Int>();
        foreach (var cell in pathCells)
            pathCellsOnly.Add(cell.position);
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
                if (Mathf.Abs(nh - h) <= 1)
                    continue;
                string dryExplain = string.Empty;
                bool drySkip = terrainManager != null
                    && DryLandCliffDropsAwayFromPathStroke(heightMap, terrainManager, pos.x, pos.y, nx, ny, pathCellsOnly, out dryExplain);
                if (drySkip)
                    continue;
                string wsExplain = string.Empty;
                bool wsSkip = terrainManager != null
                    && HighDryLandToWaterSlopeSkipsPhase1Strict(heightMap, terrainManager, pos.x, pos.y, nx, ny, pathCellsOnly, out wsExplain);
                if (wsSkip)
                    continue;
                failureDetail =
                    $"Phase1 strict |Δh|>1: ({pos.x},{pos.y}) h={h} → ({nx},{ny}) h={nh} (|Δ|={Mathf.Abs(nh - h)}).";
                if (logDryCliffPhase1Detail != null)
                {
                    logDryCliffPhase1Detail.Invoke(
                        $"REJECT {failureDetail} dryCliff={dryExplain} waterSlopeSkip={wsExplain}");
                }
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// True if <paramref name="gx,gy"/> is cardinally adjacent to any cell in <paramref name="pathCellsOnly"/>.
    /// </summary>
    static bool IsCardinalNeighborOfPathStroke(int gx, int gy, HashSet<Vector2Int> pathCellsOnly)
    {
        if (pathCellsOnly == null || pathCellsOnly.Count == 0) return false;
        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            var p = new Vector2Int(gx + cdx[d], gy + cdy[d]);
            if (pathCellsOnly.Contains(p))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Strict Phase1: allow |Δh|&gt;1 when the higher cell is dry (not open water / not water-slope) on or beside the stroke and the lower cell is a registered water-slope shore (e.g. NorthSlopeWaterPrefab below a grass cliff lip).
    /// </summary>
    static bool HighDryLandToWaterSlopeSkipsPhase1Strict(
        HeightMap heightMap, TerrainManager terrainManager, int x0, int y0, int x1, int y1, HashSet<Vector2Int> pathCellsOnly, out string explain)
    {
        explain = string.Empty;
        if (heightMap == null || terrainManager == null || pathCellsOnly == null)
        {
            explain = "null";
            return false;
        }

        int h0 = heightMap.GetHeight(x0, y0);
        int h1 = heightMap.GetHeight(x1, y1);
        if (h0 == h1)
            return false;

        int hiX, hiY, loX, loY, hiH, loH;
        if (h0 > h1)
        {
            hiX = x0; hiY = y0; hiH = h0;
            loX = x1; loY = y1; loH = h1;
        }
        else
        {
            hiX = x1; hiY = y1; hiH = h1;
            loX = x0; loY = y0; loH = h0;
        }

        if (hiH <= loH)
            return false;

        if (!terrainManager.IsWaterSlopeCell(loX, loY))
        {
            explain = "low not water-slope";
            return false;
        }

        if (terrainManager.IsRegisteredOpenWaterAt(hiX, hiY) || terrainManager.IsWaterSlopeCell(hiX, hiY))
        {
            explain = "high is water/slope";
            return false;
        }

        bool hiOn = pathCellsOnly.Contains(new Vector2Int(hiX, hiY));
        bool loOn = pathCellsOnly.Contains(new Vector2Int(loX, loY));
        if (hiOn && !loOn)
        {
            explain = "high on stroke, low water-slope off";
            return true;
        }

        if (!hiOn && !loOn && IsCardinalNeighborOfPathStroke(hiX, hiY, pathCellsOnly))
        {
            explain = "high dry beside stroke, low water-slope off";
            return true;
        }

        explain = "stroke geometry mismatch";
        return false;
    }

    /// <summary>
    /// True if any path or adjacent cell would perform a terraform height write after water/shore skips in <see cref="WritePhase1TerraformHeights"/>.
    /// </summary>
    bool PlanHasTerraformHeightMutation()
    {
        for (int i = 0; i < pathCells.Count; i++)
        {
            if (pathCells[i].action != TerraformingService.TerraformAction.None)
                return true;
        }
        for (int i = 0; i < adjacentCells.Count; i++)
        {
            if (adjacentCells[i].action != TerraformingService.TerraformAction.None)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Like <see cref="ValidateNoHeightDiffGreaterThanOne"/> but ignores cardinal edges where either cell is open water (height ≤ sea)
    /// or water-slope shore, or <b>both</b> ends lie on planned path cells (deck spans natural |Δh|&gt;1 along the stroke). FEAT-44 bridge preview/commit.
    /// Only edges incident to at least one <see cref="pathCells"/> position are checked so recursive <c>adjacentCells</c> blobs cannot fail the plan far from the stroke.
    /// </summary>
    bool ValidateNoHeightDiffGreaterThanOneWaterBridgeRelaxed(HeightMap heightMap, TerrainManager terrainManager, out string failureDetail)
    {
        failureDetail = null;
        if (heightMap == null || terrainManager == null) return true;
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        var pathCorridor = new HashSet<Vector2Int>();
        foreach (var cell in pathCells)
            pathCorridor.Add(cell.position);
        var checkSet = new HashSet<Vector2Int>();
        foreach (var cell in pathCells)
            checkSet.Add(cell.position);
        foreach (var cell in adjacentCells)
            checkSet.Add(cell.position);
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
                if (Mathf.Abs(nh - h) <= 1)
                    continue;
                bool posOnStroke = pathCorridor.Contains(new Vector2Int(pos.x, pos.y));
                bool nbrOnStroke = pathCorridor.Contains(new Vector2Int(nx, ny));
                if (!posOnStroke && !nbrOnStroke)
                    continue;
                if (WaterBridgeRelaxationSkipsHeightEdge(heightMap, terrainManager, pos.x, pos.y, nx, ny, pathCorridor))
                    continue;
                bool p0Path = pathCorridor.Contains(new Vector2Int(pos.x, pos.y));
                bool p1Path = pathCorridor.Contains(new Vector2Int(nx, ny));
                bool p0OpenW = terrainManager.IsRegisteredOpenWaterAt(pos.x, pos.y);
                bool p1OpenW = terrainManager.IsRegisteredOpenWaterAt(nx, ny);
                bool p0Ws = terrainManager.IsWaterSlopeCell(pos.x, pos.y);
                bool p1Ws = terrainManager.IsWaterSlopeCell(nx, ny);
                failureDetail =
                    $"Phase1 water-bridge relaxed |Δh|>1 rejected edge: ({pos.x},{pos.y}) h={h} → ({nx},{ny}) h={nh} (|Δ|={Mathf.Abs(nh - h)}); " +
                    $"onPathCorridor=({p0Path},{p1Path}) openWaterMap=({p0OpenW},{p1OpenW}) waterSlope=({p0Ws},{p1Ws}).";
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// True when a natural dry cliff drops away from the road stroke: the higher tile is on the stroke OR is cardinally adjacent to the stroke,
    /// the lower tile is off the stroke and not water, and the higher tile is strictly above the lower. Matches FEAT-44 relaxed “deck beside gorge” intent for strict Phase1 when the stroke ends one tile short of the cliff lip (P3 ring pulls the lip into checkSet).
    /// </summary>
    static bool DryLandCliffDropsAwayFromPathStroke(HeightMap heightMap, TerrainManager terrainManager, int x0, int y0, int x1, int y1, HashSet<Vector2Int> pathCellsOnly, out string explain)
    {
        explain = string.Empty;
        if (heightMap == null || terrainManager == null || pathCellsOnly == null)
        {
            explain = "null map/manager/pathCells";
            return false;
        }

        int h0 = heightMap.GetHeight(x0, y0);
        int h1 = heightMap.GetHeight(x1, y1);
        if (h0 == h1)
        {
            explain = "equal heights (no drop)";
            return false;
        }

        int hiX, hiY, loX, loY, hiH, loH;
        if (h0 > h1)
        {
            hiX = x0; hiY = y0; hiH = h0;
            loX = x1; loY = y1; loH = h1;
        }
        else
        {
            hiX = x1; hiY = y1; hiH = h1;
            loX = x0; loY = y0; loH = h0;
        }

        if (terrainManager.IsRegisteredOpenWaterAt(loX, loY) || terrainManager.IsWaterSlopeCell(loX, loY))
        {
            explain = $"low ({loX},{loY}) is open water or water-slope — not dry-cliff skip (bridge / S-height cases use wet path or relaxation)";
            return false;
        }

        bool hiOnStroke = pathCellsOnly.Contains(new Vector2Int(hiX, hiY));
        bool loOnStroke = pathCellsOnly.Contains(new Vector2Int(loX, loY));

        // Classic: last stroke cell on the cliff top, drop is off-stroke (e.g. path includes (61,90), neighbor (62,90) lower).
        if (hiOnStroke && !loOnStroke && hiH > loH)
        {
            explain = $"classic high ON stroke ({hiX},{hiY})h={hiH} low off-stroke ({loX},{loY})h={loH}";
            return true;
        }

        // Beside-stroke: stroke ends at (60,90); P3 adds (61,90) to checkSet; (61) is not on stroke but neighbors stroke; (62) is lower and off stroke.
        if (!hiOnStroke && !loOnStroke && hiH > loH
            && IsCardinalNeighborOfPathStroke(hiX, hiY, pathCellsOnly))
        {
            explain = $"beside-stroke high ({hiX},{hiY})h={hiH} cardinal-adjacent to stroke, low ({loX},{loY})h={loH} off-stroke";
            return true;
        }

        explain = $"no skip hiOnStroke={hiOnStroke} loOnStroke={loOnStroke} hiBesideStroke={IsCardinalNeighborOfPathStroke(hiX, hiY, pathCellsOnly)} " +
                  $"(bridge-through-cliff: both ends often on stroke or waterBridgeTerraformRelaxation; climbing from low stroke to high off-stroke is rejected)";
        return false;
    }

    /// <summary>
    /// FEAT-44 Phase1: skip |Δh|&gt;1 checks for edges that are intentionally steep (water, shore, full path deck span, or dry cliff dropping away from the deck).
    /// </summary>
    static bool WaterBridgeRelaxationSkipsHeightEdge(HeightMap heightMap, TerrainManager terrainManager, int x0, int y0, int x1, int y1, HashSet<Vector2Int> pathCorridor)
    {
        if (pathCorridor != null
            && pathCorridor.Contains(new Vector2Int(x0, y0))
            && pathCorridor.Contains(new Vector2Int(x1, y1)))
            return true;
        if (DryLandCliffDropsAwayFromPathStroke(heightMap, terrainManager, x0, y0, x1, y1, pathCorridor, out _))
            return true;
        if (terrainManager.IsRegisteredOpenWaterAt(x0, y0) || terrainManager.IsRegisteredOpenWaterAt(x1, y1))
            return true;
        if (terrainManager.IsWaterSlopeCell(x0, y0) || terrainManager.IsWaterSlopeCell(x1, y1))
            return true;
        return false;
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
            if (!heightMap.IsValidPosition(cell.position.x, cell.position.y)) continue;
            if (terrainManager.ShouldSkipRoadTerraformSurfaceAt(cell.position.x, cell.position.y, heightMap))
                continue;
            terrainManager.RestoreTerrainForCell(cell.position.x, cell.position.y);
        }
        foreach (var cell in adjacentCells)
        {
            if (cell.action == TerraformingService.TerraformAction.None || !heightMap.IsValidPosition(cell.position.x, cell.position.y))
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
            if (cell.action != TerraformingService.TerraformAction.None)
                refreshed.Add(cell.position);
        }

        RefreshTerrainNeighborWaves(heightMap, terrainManager, refreshed, null, 1);
    }
}
}
