using System.Collections.Generic;
using UnityEngine;
using Territory.Terrain;
using Territory.Core;

namespace Territory.Terrain
{
/// <summary>
/// Terraforms terrain for road placement: converts diagonal slopes to orthogonal or flattens
/// cells when roads run along hillsides. When the path only has scalable land steps (|Δh| ≤ 1 between consecutive path cells),
/// ascending segments prefer slope alignment (no flatten to base) to avoid spurious cut-through visuals. Used by RoadManager, AutoRoadBuilder, and InterstateManager.
/// </summary>
public class TerraformingService : MonoBehaviour
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

    /// <summary>When true, logs plan validity, cut-through flag, and flatten counts after <see cref="ComputePathPlan"/>.</summary>
    public static bool LogTerraformPlanDiagnostics = false;

    /// <summary>
    /// Action to perform when terraforming a cell.
    /// </summary>
    public enum TerraformAction
    {
        None,
        Flatten,
        DiagonalToOrthogonal
    }

    /// <summary>
    /// Orthogonal direction for DiagonalToOrthogonal. Matches TerrainSlopeType cardinal values.
    /// </summary>
    public enum OrthogonalDirection
    {
        North,
        South,
        East,
        West
    }

    void Awake()
    {
        if (terrainManager == null) terrainManager = FindObjectOfType<TerrainManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
    }

    /// <summary>
    /// Expands diagonal steps (dx!=0 and dy!=0) into two cardinal steps so road prefabs
    /// and terraforming logic receive only orthogonal segments. Public so RoadManager can
    /// use the same expanded path for both ComputePathPlan and ResolveForPath.
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
    /// Computes the base height for path terraforming. When the path crosses slopes (has height
    /// variation), returns the minimum height so terraforming flattens to the lower side.
    /// Otherwise returns the last flat cell height before entering a slope.
    /// </summary>
    public int ComputePathBaseHeight(System.Collections.Generic.IList<Vector2> path)
    {
        var (baseHeight, _, _) = ComputePathBaseHeightAndCutThrough(path);
        return baseHeight;
    }

    /// <summary>
    /// Returns base height and whether path crosses a hill (height variation with max >= 2).
    /// Used for cut-through mode: flatten only path cells, leave adjacent terrain "cut" with cliffs.
    /// Cut-through is only used when path has consecutive height diff &gt; 1 (cannot scale with slopes).
    /// Paths that can scale (all consecutive diffs &lt;= 1) use slope prefabs instead.
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

            int h = heightMap.GetHeight(x, y);
            if (h <= TerrainManager.SEA_LEVEL) continue;

            minHeight = Mathf.Min(minHeight, h);
            maxHeight = Mathf.Max(maxHeight, h);
        }

        int baseHeight = maxHeight > minHeight ? minHeight : (minHeight < int.MaxValue ? minHeight : 1);
        bool pathCrossesHill = (maxHeight >= 2) && (maxHeight > minHeight)
            && HasConsecutiveHeightDiffGreaterThanOne(path, heightMap);
        return (baseHeight, pathCrossesHill, maxHeight);
    }

    /// <summary>
    /// True if any consecutive path cells have land height difference &gt; 1.
    /// Used to decide cut-through: only when we cannot scale with slopes.
    /// </summary>
    private static bool HasConsecutiveHeightDiffGreaterThanOne(System.Collections.Generic.IList<Vector2> path, HeightMap heightMap)
    {
        if (path == null || path.Count < 2 || heightMap == null) return false;
        for (int i = 0; i < path.Count - 1; i++)
        {
            int h1 = heightMap.GetHeight((int)path[i].x, (int)path[i].y);
            int h2 = heightMap.GetHeight((int)path[i + 1].x, (int)path[i + 1].y);
            if (h1 > TerrainManager.SEA_LEVEL && h2 > TerrainManager.SEA_LEVEL && Mathf.Abs(h2 - h1) > 1)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Computes a path-level terraform plan. Implements Rules 3, 4, 5, 8. Validates height
    /// differences, marks cells for Flatten or DiagonalToOrthogonal, and sets postTerraformSlopeType
    /// so RoadPrefabResolver can select correct prefabs.
    /// </summary>
    public PathTerraformPlan ComputePathPlan(IList<Vector2> path)
    {
        var plan = new PathTerraformPlan();
        if (terrainManager == null || path == null || path.Count == 0) return plan;

        if (path.Count >= 2)
            path = ExpandDiagonalStepsToCardinal(path);

        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null) return plan;

        var (baseHeight, pathCrossesHill, maxHeight) = ComputePathBaseHeightAndCutThrough(path);
        plan.baseHeight = baseHeight;
        plan.isCutThrough = pathCrossesHill;

        if (pathCrossesHill)
        {
            // BUG-29: Cut-through only valid when height diff <= 1. Reject paths that would cut through tall hills.
            if (maxHeight - baseHeight > 1)
                plan.isValid = false;

            for (int i = 0; i < path.Count; i++)
            {
                int x = (int)path[i].x;
                int y = (int)path[i].y;
                int h = heightMap.IsValidPosition(x, y) ? heightMap.GetHeight(x, y) : TerrainManager.SEA_LEVEL;

                var cellPlan = new PathTerraformPlan.CellPlan
                {
                    position = new Vector2Int(x, y),
                    action = TerraformAction.Flatten,
                    direction = OrthogonalDirection.North,
                    originalHeight = h,
                    targetHeight = plan.baseHeight,
                    postTerraformSlopeType = TerrainSlopeType.Flat
                };

                if (!heightMap.IsValidPosition(x, y) || h <= TerrainManager.SEA_LEVEL)
                {
                    cellPlan.action = TerraformAction.None;
                    cellPlan.targetHeight = h;
                }

                if (i < path.Count - 1)
                {
                    int hNext = heightMap.GetHeight((int)path[i + 1].x, (int)path[i + 1].y);
                    if (h > TerrainManager.SEA_LEVEL && hNext > TerrainManager.SEA_LEVEL && Mathf.Abs(hNext - h) > 1)
                        plan.isValid = false;
                }

                plan.pathCells.Add(cellPlan);
            }
            // Cut-through: expand adjacentCells so no flattened path cell has a neighbor with height diff > 1.
            // Ensures smooth terrain transitions and prevents black holes at boundaries.
            ExpandAdjacentFlattenCellsRecursively(plan, path, heightMap);
            if (plan.isValid && cutThroughMinCellsFromMapEdge > 0 && !CutThroughHasAcceptableMapMargin(plan, path, heightMap))
                plan.isValid = false;
            LogTerraformPlanDiagnosticsInternal(plan, path);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (path != null && plan.pathCells != null && path.Count != plan.pathCells.Count)
                Debug.LogWarning($"[TerraformingService] Path/plan length mismatch: path={path.Count} pathCells={plan.pathCells.Count}");
#endif
            return plan;
        }

        // Scale-with-slopes mode: no consecutive land step with |Δh|>1. Prefer climbing via slope prefabs instead of flattening path cells to baseHeight (avoids fake cut-through / craters).
        bool preferSlopeClimb = !HasConsecutiveHeightDiffGreaterThanOne(path, heightMap);

        for (int i = 0; i < path.Count; i++)
        {
            int x = (int)path[i].x;
            int y = (int)path[i].y;
            int h = heightMap.IsValidPosition(x, y) ? heightMap.GetHeight(x, y) : TerrainManager.SEA_LEVEL;

            var cellPlan = new PathTerraformPlan.CellPlan
            {
                position = new Vector2Int(x, y),
                action = TerraformAction.None,
                direction = OrthogonalDirection.North,
                originalHeight = h,
                targetHeight = h,
                postTerraformSlopeType = TerrainSlopeType.Flat
            };

            if (!heightMap.IsValidPosition(x, y) || h <= TerrainManager.SEA_LEVEL)
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
                int hNext = heightMap.GetHeight((int)path[i + 1].x, (int)path[i + 1].y);
                if (h > TerrainManager.SEA_LEVEL && hNext > TerrainManager.SEA_LEVEL && Mathf.Abs(hNext - h) > 1)
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

        if (plan.isValid)
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
        if (!preferSlopeClimb || anyFlattenScheduled)
            ExpandAdjacentFlattenCellsRecursively(plan, path, heightMap);

        LogTerraformPlanDiagnosticsInternal(plan, path);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (path != null && plan.pathCells != null && path.Count != plan.pathCells.Count)
            Debug.LogWarning($"[TerraformingService] Path/plan length mismatch: path={path.Count} pathCells={plan.pathCells.Count}");
#endif

        return plan;
    }

    /// <summary>
    /// Scale-climb mode: path must not run alongside land neighbors with |Δh|&gt;1 (e.g. gorge beside slope tile). Uses current heightmap (pre-apply).
    /// </summary>
    void InvalidatePlanIfPathBesideSteepLandCliff(PathTerraformPlan plan, IList<Vector2> path, HeightMap heightMap, bool preferSlopeClimb)
    {
        if (!preferSlopeClimb || plan == null || heightMap == null || path == null || !plan.isValid)
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
            int h = heightMap.GetHeight(x, y);
            if (h <= TerrainManager.SEA_LEVEL)
                continue;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + cdx[d];
                int ny = y + cdy[d];
                if (!heightMap.IsValidPosition(nx, ny))
                    continue;
                if (pathSet.Contains(new Vector2Int(nx, ny)))
                    continue;
                int nh = heightMap.GetHeight(nx, ny);
                if (nh <= TerrainManager.SEA_LEVEL)
                    continue;
                if (Mathf.Abs(nh - h) > 1)
                {
                    plan.isValid = false;
                    return;
                }
            }
        }
    }

    void LogTerraformPlanDiagnosticsInternal(PathTerraformPlan plan, IList<Vector2> path)
    {
        if (!LogTerraformPlanDiagnostics || plan == null)
            return;
        int pathFlat = 0;
        int adjFlat = 0;
        for (int i = 0; i < plan.pathCells.Count; i++)
        {
            if (plan.pathCells[i].action == TerraformAction.Flatten)
                pathFlat++;
        }
        for (int i = 0; i < plan.adjacentCells.Count; i++)
        {
            if (plan.adjacentCells[i].action == TerraformAction.Flatten)
                adjFlat++;
        }
        Debug.Log($"[Terraform] valid={plan.isValid} cutThrough={plan.isCutThrough} pathCells={plan.pathCells.Count} pathFlat={pathFlat} adjacent={plan.adjacentCells.Count} adjFlat={adjFlat} pathLen={(path != null ? path.Count : 0)}");
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
        if (heightMap == null || plan == null) return;

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
                if (nh <= TerrainManager.SEA_LEVEL || nh == baseHeight) continue;
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
        if (queue.Count > 0 && (plan.adjacentCells.Count - initialAdjacentCount) >= maxExpansion)
            Debug.LogWarning($"[Terraform] Expansion hit limit {maxExpansion}, may need more. queue.Count={queue.Count}");
    }

    /// <summary>
    /// Land Δh along the path segment that defines prefab orientation: for interior cells,
    /// height(next) − height(current); for the last cell, height(current) − height(prev).
    /// Zero when an endpoint is invalid or water (cannot infer climb vs descent).
    /// </summary>
    static int ComputeSegmentDeltaHForPostSlope(HeightMap heightMap, IList<Vector2> path, int i, int hLandAtCell)
    {
        if (heightMap == null || path == null || hLandAtCell <= TerrainManager.SEA_LEVEL) return 0;
        if (i < path.Count - 1)
        {
            int nx = (int)path[i + 1].x, ny = (int)path[i + 1].y;
            if (!heightMap.IsValidPosition(nx, ny)) return 0;
            int hn = heightMap.GetHeight(nx, ny);
            if (hn <= TerrainManager.SEA_LEVEL) return 0;
            return hn - hLandAtCell;
        }
        if (i > 0)
        {
            int px = (int)path[i - 1].x, py = (int)path[i - 1].y;
            if (!heightMap.IsValidPosition(px, py)) return 0;
            int hp = heightMap.GetHeight(px, py);
            if (hp <= TerrainManager.SEA_LEVEL) return 0;
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
    static TerrainSlopeType GetPostTerraformSlopeTypeAlongExit(HeightMap heightMap, IList<Vector2> path, int i, int hLand, int dxOut, int dyOut)
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
            if (nh <= TerrainManager.SEA_LEVEL || nh == baseHeight) continue;
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
        {
            Debug.Log($"[ApplyTerraform] ({x},{y}) action={action} SKIPPED allowLowering=false currentHeight={currentHeight} newHeight={newHeight}");
            return;
        }

        Debug.Log($"[ApplyTerraform] ({x},{y}) action={action} currentHeight={currentHeight} newHeight={newHeight} baseHeight={baseHeight} allowLowering={allowLowering}");
        heightMap.SetHeight(x, y, newHeight);
        int nw = heightMap.IsValidPosition(x + 1, y + 1) ? heightMap.GetHeight(x + 1, y + 1) : -1;
        int se = heightMap.IsValidPosition(x - 1, y - 1) ? heightMap.GetHeight(x - 1, y - 1) : -1;
        int n = heightMap.IsValidPosition(x + 1, y) ? heightMap.GetHeight(x + 1, y) : -1;
        int w = heightMap.IsValidPosition(x, y + 1) ? heightMap.GetHeight(x, y + 1) : -1;
        Debug.Log($"[ApplyTerraform] ({x},{y}) AFTER SetHeight: self={newHeight} NW={nw} SE={se} N={n} W={w}");
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
