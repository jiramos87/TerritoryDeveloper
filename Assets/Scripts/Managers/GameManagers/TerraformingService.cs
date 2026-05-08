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

    void Awake()
    {
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
    }

    /// <summary>
    /// Expand diagonal steps (dx!=0 and dy!=0) into two cardinal steps → road prefabs + terraform logic receive only orthogonal segments.
    /// Public → RoadManager can use same expanded path for both ComputePathPlan + ResolveForPath.
    /// </summary>
    public static System.Collections.Generic.List<Vector2> ExpandDiagonalStepsToCardinal(System.Collections.Generic.IList<Vector2> path)
    {
        if (path == null || path.Count < 2) return new System.Collections.Generic.List<Vector2>(path ?? new Vector2[0]);

        var clean = new System.Collections.Generic.List<Vector2> { path[0] };
        for (int i = 1; i < path.Count; i++)
        {
            Vector2 prev = clean[clean.Count - 1];
            Vector2 curr = path[i];
            int dx = (int)curr.x - (int)prev.x;
            int dy = (int)curr.y - (int)prev.y;

            if (dx != 0 && dy != 0)
            {
                if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                    clean.Add(new Vector2(prev.x + Mathf.Sign(dx), prev.y));
                else
                    clean.Add(new Vector2(prev.x, prev.y + Mathf.Sign(dy)));
            }
            clean.Add(curr);
        }
        return clean;
    }

    /// <summary>
    /// Compute base height for path terraforming. When path crosses slopes (height variation), return min height → terraform flattens to lower side.
    /// Else return last flat cell height before entering slope.
    /// </summary>
    public int ComputePathBaseHeight(System.Collections.Generic.IList<Vector2> path)
    {
        var (baseHeight, _, _) = ComputePathBaseHeightAndCutThrough(path);
        return baseHeight;
    }

    /// <summary>
    /// Return base height + whether path crosses hill (height variation with max ≥ 2).
    /// Used for cut-through mode: flatten only path cells, leave adjacent terrain "cut" with cliffs.
    /// Cut-through only when path has consecutive |Δh|&gt;1 (cannot scale with slopes).
    /// Paths that can scale (all consecutive diffs ≤ 1) use slope prefabs instead.
    /// </summary>
    (int baseHeight, bool pathCrossesHill, int maxHeight) ComputePathBaseHeightAndCutThrough(System.Collections.Generic.IList<Vector2> path)
    {
        if (terrainManager == null || path == null || path.Count == 0) return (1, false, 1);
        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null) return (1, false, 1);

        int minHeight = int.MaxValue;
        int maxHeight = int.MinValue;

        for (int i = 0; i < path.Count; i++)
        {
            int x = (int)path[i].x;
            int y = (int)path[i].y;
            if (!heightMap.IsValidPosition(x, y)) continue;

            if (terrainManager.IsRegisteredOpenWaterAt(x, y))
                continue;

            int h = heightMap.GetHeight(x, y);
            minHeight = Mathf.Min(minHeight, h);
            maxHeight = Mathf.Max(maxHeight, h);
        }

        int baseHeight = maxHeight > minHeight ? minHeight : (minHeight < int.MaxValue ? minHeight : 1);
        bool pathCrossesHill = (maxHeight >= 2) && (maxHeight > minHeight)
            && HasConsecutiveHeightDiffGreaterThanOne(path, heightMap);
        return (baseHeight, pathCrossesHill, maxHeight);
    }

    /// <summary>
    /// True if any consecutive path cells on dry corridor (not registered open water) have |Δh|&gt;1.
    /// Used to decide cut-through: only when cannot scale with slopes.
    /// </summary>
    bool HasConsecutiveHeightDiffGreaterThanOne(System.Collections.Generic.IList<Vector2> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 2 || heightMap == null || terrainManager == null) return false;
        for (int i = 0; i < path.Count - 1; i++)
        {
            int x1 = (int)path[i].x, y1 = (int)path[i].y;
            int x2 = (int)path[i + 1].x, y2 = (int)path[i + 1].y;
            if (terrainManager.IsRegisteredOpenWaterAt(x1, y1) || terrainManager.IsRegisteredOpenWaterAt(x2, y2))
                continue;
            int h1 = heightMap.GetHeight(x1, y1);
            int h2 = heightMap.GetHeight(x2, y2);
            if (Mathf.Abs(h2 - h1) > 1)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Compute path-level terraform plan. Implements Rules 3, 4, 5, 8. Validates height diffs, marks cells for Flatten or DiagonalToOrthogonal, sets postTerraformSlopeType → RoadPrefabResolver can select correct prefabs.
    /// </summary>
    /// <param name="waterBridgeTerraformRelaxation">True (FEAT-44 full span or high deck above water) → skip beside-steep-cliff invalidation, allow cut-through span &gt; 1 vs base (BUG-29), allow dry–dry |Δh|&gt;1 steps adjacent to coastal path cell.</param>
    public PathTerraformPlan ComputePathPlan(IList<Vector2> path, bool waterBridgeTerraformRelaxation = false)
    {
        var plan = new PathTerraformPlan();
        plan.waterBridgeTerraformRelaxation = waterBridgeTerraformRelaxation;
        if (terrainManager == null || path == null || path.Count == 0) return plan;

        if (path.Count >= 2)
            path = ExpandDiagonalStepsToCardinal(path);

        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null) return plan;

        plan.waterBridgeDeckDisplayHeight = 0;
        if (waterBridgeTerraformRelaxation)
            TryAssignWaterBridgeDeckDisplayHeight(plan, path, heightMap);

        var (baseHeight, pathCrossesHill, maxHeight) = ComputePathBaseHeightAndCutThrough(path);
        plan.baseHeight = baseHeight;
        plan.isCutThrough = pathCrossesHill;

        if (pathCrossesHill)
        {
            // BUG-29: Cut-through only valid when height diff <= 1. Reject paths that would cut through tall hills.
            // Water-bridge / high-deck relaxation: allow larger land span toward water (arbitrary deck vs surface S).
            if (maxHeight - baseHeight > 1 && !waterBridgeTerraformRelaxation)
                plan.isValid = false;

            for (int i = 0; i < path.Count; i++)
            {
                int x = (int)path[i].x;
                int y = (int)path[i].y;
                int h = heightMap.IsValidPosition(x, y) ? heightMap.GetHeight(x, y) : TerrainManager.MIN_HEIGHT;

                var cellPlan = new PathTerraformPlan.CellPlan
                {
                    position = new Vector2Int(x, y),
                    action = TerraformAction.Flatten,
                    direction = OrthogonalDirection.North,
                    originalHeight = h,
                    targetHeight = plan.baseHeight,
                    postTerraformSlopeType = TerrainSlopeType.Flat
                };

                if (!heightMap.IsValidPosition(x, y) || terrainManager.ShouldSkipRoadTerraformSurfaceAt(x, y, heightMap))
                {
                    cellPlan.action = TerraformAction.None;
                    cellPlan.targetHeight = h;
                }

                if (i < path.Count - 1)
                {
                    int nx = (int)path[i + 1].x, ny = (int)path[i + 1].y;
                    int hNext = heightMap.GetHeight(nx, ny);
                    bool coastalA = terrainManager.IsRegisteredOpenWaterAt(x, y) || terrainManager.IsWaterSlopeCell(x, y)
                        || terrainManager.IsDryShoreOrRimMembershipEligible(x, y);
                    bool coastalB = terrainManager.IsRegisteredOpenWaterAt(nx, ny) || terrainManager.IsWaterSlopeCell(nx, ny)
                        || terrainManager.IsDryShoreOrRimMembershipEligible(nx, ny);
                    if (!coastalA && !coastalB && Mathf.Abs(hNext - h) > 1
                        && !PathEdgeExemptDryDryForWaterBridgeRelaxation(path, i, heightMap, waterBridgeTerraformRelaxation))
                        plan.isValid = false;
                }

                plan.pathCells.Add(cellPlan);
            }
            // Cut-through: expand adjacentCells so no flattened path cell has a neighbor with height diff > 1.
            // Ensures smooth terrain transitions and prevents black holes at boundaries.
            // FEAT-44 water bridge: skip recursive flood — it can chain far from the stroke and make Phase1 fail on unrelated cliffs.
            if (!waterBridgeTerraformRelaxation)
                ExpandAdjacentFlattenCellsRecursively(plan, path, heightMap);
            if (plan.isValid && cutThroughMinCellsFromMapEdge > 0 && !CutThroughHasAcceptableMapMargin(plan, path, heightMap))
                plan.isValid = false;
            return plan;
        }

        // Scale-with-slopes mode: no consecutive land step with |Δh|>1. Prefer climbing via slope prefabs instead of flattening path cells to baseHeight (avoids fake cut-through / craters).
        bool preferSlopeClimb = !HasConsecutiveHeightDiffGreaterThanOne(path, heightMap);

        for (int i = 0; i < path.Count; i++)
        {
            int x = (int)path[i].x;
            int y = (int)path[i].y;
            int h = heightMap.IsValidPosition(x, y) ? heightMap.GetHeight(x, y) : TerrainManager.MIN_HEIGHT;

            var cellPlan = new PathTerraformPlan.CellPlan
            {
                position = new Vector2Int(x, y),
                action = TerraformAction.None,
                direction = OrthogonalDirection.North,
                originalHeight = h,
                targetHeight = h,
                postTerraformSlopeType = TerrainSlopeType.Flat
            };

            if (!heightMap.IsValidPosition(x, y) || terrainManager.IsRegisteredOpenWaterAt(x, y))
            {
                plan.pathCells.Add(cellPlan);
                continue;
            }

            Vector2 roadDir = i > 0 ? (Vector2)(path[i] - path[i - 1]) : (path.Count > 1 ? (Vector2)(path[1] - path[0]) : Vector2.zero);
            Vector2 roadDirOut = i < path.Count - 1 ? (Vector2)(path[i + 1] - path[i]) : roadDir;
            int dx = Mathf.RoundToInt(roadDir.x);
            int dy = Mathf.RoundToInt(roadDir.y);
            int dxOut = Mathf.RoundToInt(roadDirOut.x);
            int dyOut = Mathf.RoundToInt(roadDirOut.y);

            TerrainSlopeType slopeType = terrainManager.GetTerrainSlopeTypeAt(x, y);

            // BUG-30: use height delta along the active path segment (exit when possible), not only prev cell —
            // diagonal / wedge tiles can mismatch prev-based checks and spuriously flatten.
            int dSeg = ComputeSegmentDeltaHForPostSlope(heightMap, path, i, h);
            bool segmentOneStepLand = preferSlopeClimb && (dSeg == 1 || dSeg == -1);

            if (i < path.Count - 1)
            {
                int nx = (int)path[i + 1].x, ny = (int)path[i + 1].y;
                int hNext = heightMap.GetHeight(nx, ny);
                bool coastalA = terrainManager.IsRegisteredOpenWaterAt(x, y) || terrainManager.IsWaterSlopeCell(x, y)
                    || terrainManager.IsDryShoreOrRimMembershipEligible(x, y);
                bool coastalB = terrainManager.IsRegisteredOpenWaterAt(nx, ny) || terrainManager.IsWaterSlopeCell(nx, ny)
                    || terrainManager.IsDryShoreOrRimMembershipEligible(nx, ny);
                if (!coastalA && !coastalB && Mathf.Abs(hNext - h) > 1
                    && !PathEdgeExemptDryDryForWaterBridgeRelaxation(path, i, heightMap, waterBridgeTerraformRelaxation))
                    plan.isValid = false;
            }

            if (slopeType == TerrainSlopeType.Flat)
            {
                plan.pathCells.Add(cellPlan);
                continue;
            }

            bool isOrthogonalSlope = slopeType == TerrainSlopeType.North || slopeType == TerrainSlopeType.South
                || slopeType == TerrainSlopeType.East || slopeType == TerrainSlopeType.West;
            bool isHorizontalRoad = Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0;
            bool isVerticalRoad = Mathf.Abs(dy) >= Mathf.Abs(dx) && dx == 0;
            if (dx != 0 && dy != 0)
            {
                isHorizontalRoad = Mathf.Abs(dx) >= Mathf.Abs(dy);
                isVerticalRoad = Mathf.Abs(dy) > Mathf.Abs(dx);
            }
            bool roadParallelToSlope = isOrthogonalSlope && ((slopeType == TerrainSlopeType.North || slopeType == TerrainSlopeType.South)
                ? isVerticalRoad : isHorizontalRoad);

            if (roadParallelToSlope)
            {
                if (segmentOneStepLand)
                {
                    cellPlan.action = TerraformAction.None;
                    cellPlan.targetHeight = h;
                    cellPlan.postTerraformSlopeType = GetPostTerraformSlopeTypeAlongExit(heightMap, path, i, h, dxOut, dyOut);
                    plan.pathCells.Add(cellPlan);
                    continue;
                }
                cellPlan.action = TerraformAction.Flatten;
                cellPlan.targetHeight = plan.baseHeight;
                cellPlan.postTerraformSlopeType = TerrainSlopeType.Flat;
                plan.pathCells.Add(cellPlan);
                AddAdjacentFlattenCells(plan, path, heightMap, x, y, plan.baseHeight, h);
                continue;
            }

            bool isDiagonalSlope = slopeType == TerrainSlopeType.NorthEast || slopeType == TerrainSlopeType.NorthWest
                || slopeType == TerrainSlopeType.SouthEast || slopeType == TerrainSlopeType.SouthWest;
            bool isCornerSlope = slopeType == TerrainSlopeType.NorthEastUp || slopeType == TerrainSlopeType.NorthWestUp
                || slopeType == TerrainSlopeType.SouthEastUp || slopeType == TerrainSlopeType.SouthWestUp;

            if ((isDiagonalSlope || isCornerSlope) && (dx != 0 && dy != 0))
            {
                cellPlan.action = TerraformAction.Flatten;
                cellPlan.targetHeight = plan.baseHeight;
                cellPlan.postTerraformSlopeType = TerrainSlopeType.Flat;
                plan.pathCells.Add(cellPlan);
                AddAdjacentFlattenCells(plan, path, heightMap, x, y, plan.baseHeight, h);
                continue;
            }

            if ((isDiagonalSlope || isCornerSlope) && (dx == 0 || dy == 0))
            {
                if (isCornerSlope)
                {
                    // BUG-30: align with diagonal/orthogonal ramps — cardinal slope type from travel + segment Δh (same as GetPostTerraformSlopeTypeAlongExit).
                    cellPlan.action = TerraformAction.None;
                    cellPlan.postTerraformSlopeType = GetPostTerraformSlopeTypeAlongExit(heightMap, path, i, h, dxOut, dyOut);
                }
                else
                {
                    if (segmentOneStepLand)
                    {
                        cellPlan.action = TerraformAction.None;
                        cellPlan.targetHeight = h;
                        cellPlan.postTerraformSlopeType = GetPostTerraformSlopeTypeAlongExit(heightMap, path, i, h, dxOut, dyOut);
                    }
                    else if (preferSlopeClimb && dSeg == 0)
                    {
                        // BUG-51: same land height along path on a diagonal wedge still needs route-aligned ramp type; avoid flattening the corridor (scale-with-slopes: preferSlopeClimb already implies no consecutive |Δh|>1 on path).
                        cellPlan.action = TerraformAction.None;
                        cellPlan.targetHeight = h;
                        cellPlan.postTerraformSlopeType = GetPostTerraformSlopeTypeAlongExit(heightMap, path, i, h, dxOut, dyOut);
                    }
                    else
                    {
                        cellPlan.action = TerraformAction.Flatten;
                        cellPlan.targetHeight = plan.baseHeight;
                        cellPlan.postTerraformSlopeType = TerrainSlopeType.Flat;
                        AddAdjacentFlattenCells(plan, path, heightMap, x, y, plan.baseHeight, h);
                    }
                }
                plan.pathCells.Add(cellPlan);
                continue;
            }

            if (isOrthogonalSlope)
            {
                bool isLower = IsLowerInSlopePair(heightMap, x, y, slopeType);
                if (isLower && preferSlopeClimb && dSeg == 1)
                {
                    cellPlan.action = TerraformAction.None;
                    cellPlan.targetHeight = h;
                }
                else if (isLower)
                {
                    cellPlan.action = TerraformAction.Flatten;
                    cellPlan.targetHeight = plan.baseHeight;
                }
                // Use exit segment direction and land Δh so downhill-facing type matches travel (BUG-30).
                // Grid: +x=North, -x=South, +y=West, -y=East. Screen: -y => bottom-right => South.
                cellPlan.postTerraformSlopeType = GetPostTerraformSlopeTypeAlongExit(heightMap, path, i, h, dxOut, dyOut);
            }
            plan.pathCells.Add(cellPlan);
        }

        if (plan.isValid && !waterBridgeTerraformRelaxation)
            InvalidatePlanIfPathBesideSteepLandCliff(plan, path, heightMap, preferSlopeClimb);

        bool anyFlattenScheduled = false;
        for (int pi = 0; pi < plan.pathCells.Count; pi++)
        {
            if (plan.pathCells[pi].action == TerraformAction.Flatten)
            {
                anyFlattenScheduled = true;
                break;
            }
        }
        if (!anyFlattenScheduled)
        {
            for (int ai = 0; ai < plan.adjacentCells.Count; ai++)
            {
                if (plan.adjacentCells[ai].action == TerraformAction.Flatten)
                {
                    anyFlattenScheduled = true;
                    break;
                }
            }
        }
        // FEAT-44 water bridge: skip recursive flood so adjacentCells stay local to the stroke (see cut-through branch).
        if ((!preferSlopeClimb || anyFlattenScheduled) && !waterBridgeTerraformRelaxation)
            ExpandAdjacentFlattenCellsRecursively(plan, path, heightMap);

        return plan;
    }

    /// <summary>
    /// When <paramref name="waterBridgeTerraformRelaxation"/> on, allow consecutive dry–dry path step with |Δh|&gt;1 if either endpoint Moore-adjoins coastal path cell (open water, water-slope, dry shore/rim). E.g. high land → lip → wet run.
    /// </summary>
    bool PathEdgeExemptDryDryForWaterBridgeRelaxation(IList<Vector2> path, int edgeStartIdx, HeightMap heightMap, bool waterBridgeTerraformRelaxation)
    {
        if (!waterBridgeTerraformRelaxation || path == null || heightMap == null || terrainManager == null)
            return false;
        if (edgeStartIdx < 0 || edgeStartIdx >= path.Count - 1)
            return false;

        int x = (int)path[edgeStartIdx].x, y = (int)path[edgeStartIdx].y;
        int nx = (int)path[edgeStartIdx + 1].x, ny = (int)path[edgeStartIdx + 1].y;
        if (!heightMap.IsValidPosition(x, y) || !heightMap.IsValidPosition(nx, ny))
            return false;

        int h = heightMap.GetHeight(x, y);
        int hNext = heightMap.GetHeight(nx, ny);
        if (Mathf.Abs(hNext - h) <= 1)
            return false;

        bool coastalA = IsCoastalCellForTerraformConsecutiveStep(x, y);
        bool coastalB = IsCoastalCellForTerraformConsecutiveStep(nx, ny);
        if (coastalA || coastalB)
            return false;

        return PathCellMooreTouchesOnPathCoastalTile(path, x, y)
            || PathCellMooreTouchesOnPathCoastalTile(path, nx, ny);
    }

    bool IsCoastalCellForTerraformConsecutiveStep(int cx, int cy)
    {
        return terrainManager.IsRegisteredOpenWaterAt(cx, cy) || terrainManager.IsWaterSlopeCell(cx, cy)
            || terrainManager.IsDryShoreOrRimMembershipEligible(cx, cy);
    }

    /// <summary>True if <paramref name="px"/>,<paramref name="py"/> Moore-adjoins different path cell counting as coastal for consecutive-step rules.</summary>
    bool PathCellMooreTouchesOnPathCoastalTile(IList<Vector2> path, int px, int py)
    {
        if (path == null || terrainManager == null)
            return false;

        for (int i = 0; i < path.Count; i++)
        {
            int qx = (int)path[i].x, qy = (int)path[i].y;
            if (qx == px && qy == py)
                continue;
            int adx = Mathf.Abs(qx - px);
            int ady = Mathf.Abs(qy - py);
            if (adx > 1 || ady > 1)
                continue;
            if (IsCoastalCellForTerraformConsecutiveStep(qx, qy))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Scale-climb mode: path must not run alongside land neighbors with |Δh|&gt;1 (e.g. gorge beside slope tile). Uses current heightmap (pre-apply).
    /// </summary>
    void InvalidatePlanIfPathBesideSteepLandCliff(PathTerraformPlan plan, IList<Vector2> path, HeightMap heightMap, bool preferSlopeClimb)
    {
        if (!preferSlopeClimb || plan == null || heightMap == null || path == null || terrainManager == null || !plan.isValid)
            return;

        var pathSet = new HashSet<Vector2Int>();
        for (int i = 0; i < path.Count; i++)
            pathSet.Add(new Vector2Int((int)path[i].x, (int)path[i].y));

        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int i = 0; i < path.Count; i++)
        {
            int x = (int)path[i].x;
            int y = (int)path[i].y;
            if (!heightMap.IsValidPosition(x, y))
                continue;
            if (terrainManager.IsRegisteredOpenWaterAt(x, y))
                continue;
            int h = heightMap.GetHeight(x, y);
            for (int d = 0; d < 4; d++)
            {
                int nx = x + cdx[d];
                int ny = y + cdy[d];
                if (!heightMap.IsValidPosition(nx, ny))
                    continue;
                if (pathSet.Contains(new Vector2Int(nx, ny)))
                    continue;
                if (terrainManager.IsRegisteredOpenWaterAt(nx, ny))
                    continue;
                int nh = heightMap.GetHeight(nx, ny);
                if (Mathf.Abs(nh - h) > 1)
                {
                    plan.isValid = false;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// True when cut-through path and expanded flatten corridor stay inside an inner rectangle,
    /// at least <see cref="cutThroughMinCellsFromMapEdge"/> cells away from each map edge.
    /// </summary>
    bool CutThroughHasAcceptableMapMargin(PathTerraformPlan plan, IList<Vector2> path, HeightMap heightMap)
    {
        int m = cutThroughMinCellsFromMapEdge;
        if (m <= 0 || heightMap == null || plan == null || path == null)
            return true;

        int w = heightMap.Width;
        int h = heightMap.Height;
        // No room for an inner band — skip margin rule so tiny maps are not always invalid.
        if (w <= m * 2 || h <= m * 2)
            return true;

        bool Inside(int x, int y)
        {
            return x >= m && y >= m && x < w - m && y < h - m;
        }

        for (int i = 0; i < path.Count; i++)
        {
            int x = (int)path[i].x;
            int y = (int)path[i].y;
            if (!heightMap.IsValidPosition(x, y))
                continue;
            if (!Inside(x, y))
                return false;
        }

        for (int i = 0; i < plan.adjacentCells.Count; i++)
        {
            var c = plan.adjacentCells[i];
            if (c.action != TerraformAction.Flatten)
                continue;
            if (!Inside(c.position.x, c.position.y))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Recursively expands adjacent flatten cells so no flattened cell has a neighbor with
    /// height diff &gt; 1. Prevents ValidateNoHeightDiffGreaterThanOne from failing at zone boundaries.
    /// </summary>
    void ExpandAdjacentFlattenCellsRecursively(PathTerraformPlan plan, IList<Vector2> path, HeightMap heightMap)
    {
        if (heightMap == null || plan == null || terrainManager == null) return;

        var pathSet = new HashSet<Vector2Int>();
        for (int i = 0; i < path.Count; i++)
            pathSet.Add(new Vector2Int((int)path[i].x, (int)path[i].y));

        var toFlatten = new HashSet<Vector2Int>();
        foreach (var cell in plan.pathCells)
        {
            if (cell.action == TerraformAction.Flatten)
                toFlatten.Add(cell.position);
        }
        foreach (var cell in plan.adjacentCells)
        {
            if (cell.action == TerraformAction.Flatten)
                toFlatten.Add(cell.position);
        }

        var queue = new Queue<Vector2Int>(toFlatten);
        int baseHeight = plan.baseHeight;
        int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };
        int initialAdjacentCount = plan.adjacentCells.Count;
        const int maxExpansion = 500;

        while (queue.Count > 0 && (plan.adjacentCells.Count - initialAdjacentCount) < maxExpansion)
        {
            var pos = queue.Dequeue();
            for (int d = 0; d < 8; d++)
            {
                int nx = pos.x + dx[d];
                int ny = pos.y + dy[d];
                if (pathSet.Contains(new Vector2Int(nx, ny))) continue;
                if (!heightMap.IsValidPosition(nx, ny)) continue;
                int nh = heightMap.GetHeight(nx, ny);
                if (terrainManager.IsRegisteredOpenWaterAt(nx, ny) || nh == baseHeight) continue;
                bool oneStepRidgeAboveBase = expandCutThroughAdjacentByOneStep && plan.isCutThrough && nh == baseHeight + 1;
                if (Mathf.Abs(nh - baseHeight) <= 1 && !oneStepRidgeAboveBase) continue;
                if (toFlatten.Contains(new Vector2Int(nx, ny))) continue;

                toFlatten.Add(new Vector2Int(nx, ny));
                queue.Enqueue(new Vector2Int(nx, ny));

                bool alreadyInPlan = false;
                for (int j = 0; j < plan.adjacentCells.Count; j++)
                {
                    if (plan.adjacentCells[j].position.x == nx && plan.adjacentCells[j].position.y == ny)
                    {
                        alreadyInPlan = true;
                        break;
                    }
                }
                if (!alreadyInPlan)
                {
                    plan.adjacentCells.Add(new PathTerraformPlan.CellPlan
                    {
                        position = new Vector2Int(nx, ny),
                        action = TerraformAction.Flatten,
                        direction = OrthogonalDirection.North,
                        originalHeight = nh,
                        targetHeight = baseHeight,
                        postTerraformSlopeType = TerrainSlopeType.Flat
                    });
                }
            }
        }
    }

    /// <summary>
    /// Land Δh along the path segment that defines prefab orientation: for interior cells,
    /// height(next) − height(current); for the last cell, height(current) − height(prev).
    /// Zero when an endpoint is invalid or water (cannot infer climb vs descent).
    /// BUG-51: on diagonal wedge tiles, zero here with a valid cardinal path still allows <see cref="ComputePathPlan"/> to preserve terrain when <c>preferSlopeClimb &amp;&amp; dSeg == 0</c>.
    /// </summary>
    int ComputeSegmentDeltaHForPostSlope(HeightMap heightMap, IList<Vector2> path, int i, int hLandAtCell)
    {
        if (heightMap == null || path == null || terrainManager == null) return 0;
        int cx = (int)path[i].x, cy = (int)path[i].y;
        if (!heightMap.IsValidPosition(cx, cy) || terrainManager.IsRegisteredOpenWaterAt(cx, cy))
            return 0;
        if (i < path.Count - 1)
        {
            int nx = (int)path[i + 1].x, ny = (int)path[i + 1].y;
            if (!heightMap.IsValidPosition(nx, ny)) return 0;
            if (terrainManager.IsRegisteredOpenWaterAt(nx, ny)) return 0;
            int hn = heightMap.GetHeight(nx, ny);
            return hn - hLandAtCell;
        }
        if (i > 0)
        {
            int px = (int)path[i - 1].x, py = (int)path[i - 1].y;
            if (!heightMap.IsValidPosition(px, py)) return 0;
            if (terrainManager.IsRegisteredOpenWaterAt(px, py)) return 0;
            int hp = heightMap.GetHeight(px, py);
            return hLandAtCell - hp;
        }
        return 0;
    }

    /// <summary>
    /// <see cref="TerrainSlopeType"/> for orthogonal cells: direction the slope <i>faces</i> (downhill).
    /// Maps a <b>travel</b> vector to that type when the road follows that vector while <b>descending</b> one step.
    /// If the path climbs along (dxOut, dyOut), pass the negated vector (handled by <see cref="GetPostTerraformSlopeTypeAlongExit"/>).
    /// Grid: +x=North, -x=South, +y=West, -y=East.
    /// </summary>
    static TerrainSlopeType GetSlopeTypeFromTravelVector(int dx, int dy)
    {
        if (Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0)
            return dx > 0 ? TerrainSlopeType.North : TerrainSlopeType.South;
        if (dy != 0)
            return dy > 0 ? TerrainSlopeType.West : TerrainSlopeType.East;
        return TerrainSlopeType.Flat;
    }

    /// <summary>
    /// Post-terraform slope type for the path exit segment so ramp prefabs match downhill geometry (BUG-30).
    /// </summary>
    TerrainSlopeType GetPostTerraformSlopeTypeAlongExit(HeightMap heightMap, IList<Vector2> path, int i, int hLand, int dxOut, int dyOut)
    {
        int dSeg = ComputeSegmentDeltaHForPostSlope(heightMap, path, i, hLand);
        if (dSeg > 0)
            return GetSlopeTypeFromTravelVector(-dxOut, -dyOut);
        return GetSlopeTypeFromTravelVector(dxOut, dyOut);
    }

    static TerrainSlopeType OrthogonalToSlopeType(OrthogonalDirection d)
    {
        switch (d)
        {
            case OrthogonalDirection.North: return TerrainSlopeType.South;
            case OrthogonalDirection.South: return TerrainSlopeType.North;
            case OrthogonalDirection.East: return TerrainSlopeType.West;
            case OrthogonalDirection.West: return TerrainSlopeType.East;
            default: return TerrainSlopeType.Flat;
        }
    }

    static bool IsLowerInSlopePair(HeightMap hm, int x, int y, TerrainSlopeType slopeType)
    {
        int h = hm.GetHeight(x, y);
        int n = GetNeighborHeight(hm, x + 1, y);
        int s = GetNeighborHeight(hm, x - 1, y);
        int e = GetNeighborHeight(hm, x, y - 1);
        int w = GetNeighborHeight(hm, x, y + 1);
        switch (slopeType)
        {
            case TerrainSlopeType.South: return n > h;
            case TerrainSlopeType.North: return s > h;
            case TerrainSlopeType.West: return e > h;
            case TerrainSlopeType.East: return w > h;
            default: return false;
        }
    }

    /// <summary>
    /// Adds adjacent cells that need flattening so height difference with path never exceeds 1.
    /// Flattens both lower neighbors (nh &lt; pathCellHeight) and higher neighbors (nh &gt; baseHeight + 1).
    /// </summary>
    void AddAdjacentFlattenCells(PathTerraformPlan plan, IList<Vector2> path, HeightMap heightMap, int x, int y, int baseHeight, int pathCellHeight)
    {
        if (terrainManager == null) return;
        var pathSet = new HashSet<Vector2Int>();
        for (int i = 0; i < path.Count; i++)
            pathSet.Add(new Vector2Int((int)path[i].x, (int)path[i].y));

        int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };
        for (int d = 0; d < 8; d++)
        {
            int nx = x + dx[d];
            int ny = y + dy[d];
            if (pathSet.Contains(new Vector2Int(nx, ny))) continue;
            if (!heightMap.IsValidPosition(nx, ny)) continue;
            int nh = heightMap.GetHeight(nx, ny);
            if (terrainManager.IsRegisteredOpenWaterAt(nx, ny) || nh == baseHeight) continue;
            bool needsFlatten = nh < pathCellHeight || nh > baseHeight + 1;
            if (!needsFlatten) continue;

            var adj = new PathTerraformPlan.CellPlan
            {
                position = new Vector2Int(nx, ny),
                action = TerraformAction.Flatten,
                direction = OrthogonalDirection.North,
                originalHeight = nh,
                targetHeight = baseHeight,
                postTerraformSlopeType = TerrainSlopeType.Flat
            };
            bool alreadyAdded = false;
            for (int j = 0; j < plan.adjacentCells.Count; j++)
                if (plan.adjacentCells[j].position.x == nx && plan.adjacentCells[j].position.y == ny) { alreadyAdded = true; break; }
            if (!alreadyAdded)
                plan.adjacentCells.Add(adj);
        }
    }

    /// <summary>
    /// Applies terraforming to the cell. Modifies heightMap and applies terrain.
    /// </summary>
    /// <param name="allowLowering">When false, skips terraforming if it would lower the cell. Used for interstate to avoid "buried" appearance.</param>
    /// <param name="baseHeight">When set, Flatten uses this height instead of max of neighbors. Used for path-based terraforming.</param>
    public void ApplyTerraform(int x, int y, TerraformAction action, OrthogonalDirection orthogonalDir, bool allowLowering = true, int? baseHeight = null)
    {
        if (terrainManager == null || gridManager == null) return;
        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null || !heightMap.IsValidPosition(x, y)) return;

        int currentHeight = heightMap.GetHeight(x, y);
        int newHeight = ComputeNewHeight(heightMap, x, y, action, orthogonalDir, baseHeight);
        if (newHeight < 0) return;

        if (!allowLowering && newHeight < currentHeight)
            return;

        heightMap.SetHeight(x, y, newHeight);
        terrainManager.RestoreTerrainForCell(x, y);
    }

    /// <summary>
    /// Reverts terraforming for preview cancel. Restores original height.
    /// </summary>
    public void RevertTerraform(int x, int y, int originalHeight)
    {
        if (terrainManager == null || gridManager == null) return;
        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null || !heightMap.IsValidPosition(x, y)) return;

        heightMap.SetHeight(x, y, originalHeight);
        terrainManager.RestoreTerrainForCell(x, y);
    }

    int ComputeNewHeight(HeightMap heightMap, int x, int y, TerraformAction action, OrthogonalDirection orthogonalDir, int? baseHeight = null)
    {
        int n = GetNeighborHeight(heightMap, x + 1, y);
        int s = GetNeighborHeight(heightMap, x - 1, y);
        int e = GetNeighborHeight(heightMap, x, y - 1);
        int w = GetNeighborHeight(heightMap, x, y + 1);
        int ne = GetNeighborHeight(heightMap, x + 1, y - 1);
        int nw = GetNeighborHeight(heightMap, x + 1, y + 1);
        int se = GetNeighborHeight(heightMap, x - 1, y - 1);
        int sw = GetNeighborHeight(heightMap, x - 1, y + 1);

        if (action == TerraformAction.Flatten)
        {
            if (baseHeight.HasValue)
                return baseHeight.Value;
            int maxN = Mathf.Max(n, s, e, w, ne, nw, se, sw);
            return maxN >= 0 ? maxN : heightMap.GetHeight(x, y);
        }

        if (action == TerraformAction.DiagonalToOrthogonal)
        {
            int maxOthers;
            int higherNeighbor;
            switch (orthogonalDir)
            {
                case OrthogonalDirection.East:
                    maxOthers = MaxExcluding(n, s, e, ne, nw, se, sw);
                    higherNeighbor = w;
                    break;
                case OrthogonalDirection.West:
                    maxOthers = MaxExcluding(n, s, w, ne, nw, se, sw);
                    higherNeighbor = e;
                    break;
                case OrthogonalDirection.North:
                    maxOthers = MaxExcluding(s, e, w, ne, nw, se, sw);
                    higherNeighbor = n;
                    break;
                case OrthogonalDirection.South:
                    maxOthers = MaxExcluding(n, e, w, ne, nw, se, sw);
                    higherNeighbor = s;
                    break;
                default:
                    return -1;
            }
            if (higherNeighbor >= 0 && higherNeighbor > maxOthers)
                return maxOthers >= 0 ? maxOthers : heightMap.GetHeight(x, y);
        }

        return -1;
    }

    /// <summary>
    /// Builds a <see cref="PathTerraformPlan"/> with no terraform height mutations (all <see cref="TerraformAction.None"/>), water-bridge relaxation on,
    /// and <see cref="PathTerraformPlan.waterBridgeDeckDisplayHeight"/> from <see cref="TryAssignWaterBridgeDeckDisplayHeight"/>.
    /// Used when the manual stroke locks a straight chord over water/cliffs so preview/commit skip <see cref="ComputePathPlan"/> cut-through / Phase-1 failures
    /// while still placing deck tiles at the lip height. <paramref name="expandedCardinalPath"/> must already be cardinal (use <see cref="ExpandDiagonalStepsToCardinal"/> first).
    /// </summary>
    public bool TryBuildDeckSpanOnlyWaterBridgePlan(IList<Vector2> expandedCardinalPath, out PathTerraformPlan plan)
    {
        plan = null;
        if (terrainManager == null || expandedCardinalPath == null || expandedCardinalPath.Count == 0 || gridManager == null)
            return false;

        HeightMap heightMap = terrainManager.GetHeightMap();
        if (heightMap == null)
            return false;

        plan = new PathTerraformPlan
        {
            isValid = true,
            isCutThrough = false,
            waterBridgeTerraformRelaxation = true
        };

        int x0 = (int)expandedCardinalPath[0].x;
        int y0 = (int)expandedCardinalPath[0].y;
        plan.baseHeight = heightMap.IsValidPosition(x0, y0) ? heightMap.GetHeight(x0, y0) : 1;

        plan.pathCells.Clear();
        plan.adjacentCells.Clear();

        for (int i = 0; i < expandedCardinalPath.Count; i++)
        {
            int x = (int)expandedCardinalPath[i].x;
            int y = (int)expandedCardinalPath[i].y;
            if (!heightMap.IsValidPosition(x, y) || gridManager.GetCell(x, y) == null)
            {
                plan.isValid = false;
                plan = null;
                return false;
            }

            int h = heightMap.GetHeight(x, y);
            TerrainSlopeType slope = TerrainSlopeType.Flat;
            if (!terrainManager.IsRegisteredOpenWaterAt(x, y) && !terrainManager.IsWaterSlopeCell(x, y))
                slope = terrainManager.GetTerrainSlopeTypeAt(x, y);

            plan.pathCells.Add(new PathTerraformPlan.CellPlan
            {
                position = new Vector2Int(x, y),
                action = TerraformAction.None,
                direction = OrthogonalDirection.North,
                originalHeight = h,
                targetHeight = h,
                postTerraformSlopeType = slope
            });
        }

        plan.waterBridgeDeckDisplayHeight = 0;
        TryAssignWaterBridgeDeckDisplayHeight(plan, expandedCardinalPath, heightMap);
        if (plan.waterBridgeDeckDisplayHeight <= 0)
        {
            plan = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// FEAT-44: one <see cref="PathTerraformPlan.waterBridgeDeckDisplayHeight"/> for the whole span so every bridge deck prefab matches. Prefers the
    /// <b>exit</b> dry cell after the wet run (mesa / far bank); valid bridges require matching endpoint instance heights, so entry and exit agree when FEAT-44 passes.
    /// </summary>
    void TryAssignWaterBridgeDeckDisplayHeight(PathTerraformPlan plan, IList<Vector2> path, HeightMap heightMap)
    {
        if (plan == null || path == null || path.Count < 1 || heightMap == null || gridManager == null || terrainManager == null)
            return;

        int runs = 0;
        int rs = -1, re = -1;
        bool inRun = false;
        for (int i = 0; i < path.Count; i++)
        {
            int x = (int)path[i].x;
            int y = (int)path[i].y;
            bool w = IsWaterOrWaterSlopeForBridgeDeckHeight(x, y, heightMap);
            if (w)
            {
                if (!inRun)
                {
                    runs++;
                    rs = i;
                    inRun = true;
                }
                re = i;
            }
            else
                inRun = false;
        }

        if (runs == 1 && rs >= 1 && re < path.Count - 1)
        {
            int bx = (int)path[rs - 1].x, by = (int)path[rs - 1].y;
            int ax = (int)path[re + 1].x, ay = (int)path[re + 1].y;
            if (!IsWaterOrWaterSlopeForBridgeDeckHeight(bx, by, heightMap) && !IsWaterOrWaterSlopeForBridgeDeckHeight(ax, ay, heightMap))
            {
                CityCell landBefore = gridManager.GetCell(bx, by);
                CityCell landExit = gridManager.GetCell(ax, ay);
                if (landBefore != null && landExit != null)
                {
                    int hIn = landBefore.GetCellInstanceHeight();
                    int hOut = landExit.GetCellInstanceHeight();
                    int deckH = hOut > 0 ? hOut : hIn;
                    if (deckH <= 0)
                        deckH = hIn > 0 ? hIn : hOut;
                    if (deckH > 0)
                    {
                        plan.waterBridgeDeckDisplayHeight = deckH;
                        return;
                    }
                }
            }
        }

        if (runs == 1 && re >= 0 && re < path.Count - 1)
        {
            int ax = (int)path[re + 1].x, ay = (int)path[re + 1].y;
            if (!IsWaterOrWaterSlopeForBridgeDeckHeight(ax, ay, heightMap))
            {
                CityCell landExit = gridManager.GetCell(ax, ay);
                if (landExit != null && landExit.GetCellInstanceHeight() > 0)
                {
                    plan.waterBridgeDeckDisplayHeight = landExit.GetCellInstanceHeight();
                    return;
                }
            }
        }

        if (runs == 1 && rs >= 1)
        {
            int bx = (int)path[rs - 1].x, by = (int)path[rs - 1].y;
            if (!IsWaterOrWaterSlopeForBridgeDeckHeight(bx, by, heightMap))
            {
                CityCell landBefore = gridManager.GetCell(bx, by);
                if (landBefore != null && landBefore.GetCellInstanceHeight() > 0)
                {
                    plan.waterBridgeDeckDisplayHeight = landBefore.GetCellInstanceHeight();
                    return;
                }
            }
        }

        WaterManager wm = terrainManager.waterManager != null ? terrainManager.waterManager : FindObjectOfType<WaterManager>();
        int best = 0;
        for (int i = 0; i < path.Count; i++)
        {
            int x = (int)path[i].x;
            int y = (int)path[i].y;
            if (terrainManager.IsRegisteredOpenWaterAt(x, y))
                continue;
            CityCell pathCell = gridManager.GetCell(x, y);
            if (pathCell == null)
                continue;
            int h = pathCell.GetCellInstanceHeight();
            if (h <= 0)
                continue;
            if (!CellQualifiesForDeckDisplayLipRelaxed(x, y, h, heightMap, wm))
                continue;
            if (h > best)
                best = h;
        }

        if (best > 0)
            plan.waterBridgeDeckDisplayHeight = best;
    }

    /// <summary>
    /// Deck display height: strict dry-lower corridor to water, or direct cardinal step onto lower open water / water-slope (last land tile before wet).
    /// </summary>
    bool CellQualifiesForDeckDisplayLipRelaxed(int x, int y, int h, HeightMap heightMap, WaterManager wm)
    {
        if (heightMap == null || terrainManager == null || !heightMap.IsValidPosition(x, y))
            return false;

        int[] cdx = { 1, -1, 0, 0 };
        int[] cdy = { 0, 0, 1, -1 };
        for (int d = 0; d < 4; d++)
        {
            int nx = x + cdx[d];
            int ny = y + cdy[d];
            if (!heightMap.IsValidPosition(nx, ny))
                continue;
            int hn = heightMap.GetHeight(nx, ny);
            if (hn >= h)
                continue;
            if (terrainManager.IsRegisteredOpenWaterAt(nx, ny) || terrainManager.IsWaterSlopeCell(nx, ny))
                return true;
            if (DryCellTouchesRegisteredWaterForDeckHeight(nx, ny, wm))
                return true;
        }

        return false;
    }

    bool DryCellTouchesRegisteredWaterForDeckHeight(int x, int y, WaterManager wm)
    {
        if (terrainManager == null)
            return false;
        if (terrainManager.IsRegisteredOpenWaterAt(x, y) || terrainManager.IsWaterSlopeCell(x, y))
            return true;
        if (wm == null)
            return false;

        int[] mdx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        int[] mdy = { 0, 0, 1, -1, 1, -1, 1, -1 };
        for (int d = 0; d < 8; d++)
        {
            int ax = x + mdx[d];
            int ay = y + mdy[d];
            if (wm.IsWaterAt(ax, ay))
                return true;
        }

        return false;
    }

    bool IsWaterOrWaterSlopeForBridgeDeckHeight(int x, int y, HeightMap heightMap)
    {
        if (heightMap == null || !heightMap.IsValidPosition(x, y) || terrainManager == null)
            return false;
        if (terrainManager.IsRegisteredOpenWaterAt(x, y))
            return true;
        return terrainManager.IsWaterSlopeCell(x, y);
    }

    static int GetNeighborHeight(HeightMap heightMap, int nx, int ny)
    {
        if (!heightMap.IsValidPosition(nx, ny)) return -1;
        return heightMap.GetHeight(nx, ny);
    }

    static int MaxExcluding(int a, int b, int c, int d, int e, int f, int g)
    {
        int max = -1;
        if (a >= 0) max = Mathf.Max(max, a);
        if (b >= 0) max = Mathf.Max(max, b);
        if (c >= 0) max = Mathf.Max(max, c);
        if (d >= 0) max = Mathf.Max(max, d);
        if (e >= 0) max = Mathf.Max(max, e);
        if (f >= 0) max = Mathf.Max(max, f);
        if (g >= 0) max = Mathf.Max(max, g);
        return max;
    }

}
}
