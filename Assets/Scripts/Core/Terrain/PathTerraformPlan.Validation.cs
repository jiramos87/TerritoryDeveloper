using System;
using System.Collections.Generic;
using UnityEngine;
using Territory.Core;

namespace Territory.Terrain
{
    /// <summary>
    /// Validation + neighbor-wave helpers extracted from PathTerraformPlan (Strategy γ Stage 3.2).
    /// Partial class — same assembly as PathTerraformPlan.cs.
    /// </summary>
    public partial class PathTerraformPlan
    {
        /// <summary>Positions lowered by this plan's flatten actions. Used for 1-step cliff walls toward cut (BUG-29).</summary>
        HashSet<Vector2Int> BuildTerraformCutCorridorSet()
        {
            if (!isCutThrough)
                return null;
            var set = new HashSet<Vector2Int>();
            foreach (var cell in pathCells)
            {
                if (cell.action != TerraformAction.None)
                    set.Add(cell.position);
            }
            foreach (var cell in adjacentCells)
            {
                if (cell.action != TerraformAction.None)
                    set.Add(cell.position);
            }
            return set;
        }

        /// <summary>Phase 3 neighbor refresh: skip open water, registered water, water-slope tiles (FEAT-44 bridge).</summary>
        static bool ShouldSkipPhase3NeighborTerrainRefresh(HeightMap heightMap, ITerrainManager terrainManager, int x, int y)
        {
            if (terrainManager == null) return true;
            return terrainManager.ShouldSkipRoadTerraformSurfaceAt(x, y, heightMap);
        }

        static void RefreshTerrainNeighborWaves(HeightMap heightMap, ITerrainManager terrainManager, HashSet<Vector2Int> touchedCore, HashSet<Vector2Int> cutCorridorCells, int waveCount)
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
        /// Return false if any affected cell has neighbor with |Δh|&gt;1.
        /// Skip pre-existing dry-cliff edges away from stroke and high-dry-land-to-water-slope edges.
        /// </summary>
        bool ValidateNoHeightDiffGreaterThanOne(HeightMap heightMap, ITerrainManager terrainManager, out string failureDetail, Action<string> logDryCliffPhase1Detail = null)
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

        /// <summary>True if <paramref name="gx"/> cardinally adjacent to any cell in <paramref name="pathCellsOnly"/>.</summary>
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
        /// Strict Phase1: allow |Δh|&gt;1 when higher cell is dry on or beside stroke + lower cell is registered water-slope shore.
        /// </summary>
        static bool HighDryLandToWaterSlopeSkipsPhase1Strict(
            HeightMap heightMap, ITerrainManager terrainManager, int x0, int y0, int x1, int y1, HashSet<Vector2Int> pathCellsOnly, out string explain)
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
        /// Like ValidateNoHeightDiffGreaterThanOne but relaxes water/shore + full-deck-span edges. FEAT-44 bridge.
        /// </summary>
        bool ValidateNoHeightDiffGreaterThanOneWaterBridgeRelaxed(HeightMap heightMap, ITerrainManager terrainManager, out string failureDetail)
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
        /// True when natural dry cliff drops away from road stroke: higher tile on or beside stroke, lower tile off stroke + not water.
        /// </summary>
        static bool DryLandCliffDropsAwayFromPathStroke(HeightMap heightMap, ITerrainManager terrainManager, int x0, int y0, int x1, int y1, HashSet<Vector2Int> pathCellsOnly, out string explain)
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

            // Classic: last stroke cell on the cliff top, drop is off-stroke.
            if (hiOnStroke && !loOnStroke && hiH > loH)
            {
                explain = $"classic high ON stroke ({hiX},{hiY})h={hiH} low off-stroke ({loX},{loY})h={loH}";
                return true;
            }

            // Beside-stroke: P3 ring pulls cliff lip into checkSet; lip neighbors stroke; lower cell off stroke.
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

        /// <summary>FEAT-44 Phase1: skip |Δh|&gt;1 checks for intentionally steep edges (water, shore, full deck span, or dry cliff away from deck).</summary>
        static bool WaterBridgeRelaxationSkipsHeightEdge(HeightMap heightMap, ITerrainManager terrainManager, int x0, int y0, int x1, int y1, HashSet<Vector2Int> pathCorridor)
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
    }
}
