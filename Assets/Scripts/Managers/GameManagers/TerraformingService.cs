using System.Collections.Generic;
using UnityEngine;
using Territory.Terrain;
using Territory.Core;

/// <summary>
/// Terraforms terrain for road placement: converts diagonal slopes to orthogonal or flattens
/// cells when roads run along hillsides. Used by RoadManager, AutoRoadBuilder, and InterstateManager.
/// </summary>
public class TerraformingService : MonoBehaviour
{
    #region Dependencies
    public TerrainManager terrainManager;
    public GridManager gridManager;
    #endregion

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
    /// and terraforming logic receive only orthogonal segments.
    /// </summary>
    static System.Collections.Generic.List<Vector2> ExpandDiagonalStepsToCardinal(System.Collections.Generic.IList<Vector2> path)
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
        if (terrainManager == null || path == null || path.Count == 0) return 1;
        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null) return 1;

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

        if (maxHeight > minHeight)
            return minHeight;
        return minHeight < int.MaxValue ? minHeight : 1;
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

        plan.baseHeight = ComputePathBaseHeight(path);

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
                    cellPlan.action = TerraformAction.None;
                    cellPlan.postTerraformSlopeType = GetOrthogonalFromCornerSlope(slopeType, dxOut, dyOut);
                }
                else
                {
                    cellPlan.action = TerraformAction.Flatten;
                    cellPlan.targetHeight = plan.baseHeight;
                    cellPlan.postTerraformSlopeType = TerrainSlopeType.Flat;
                    AddAdjacentFlattenCells(plan, path, heightMap, x, y, plan.baseHeight, h);
                }
                plan.pathCells.Add(cellPlan);
                continue;
            }

            if (isOrthogonalSlope)
            {
                bool isLower = IsLowerInSlopePair(heightMap, x, y, slopeType);
                if (isLower)
                {
                    cellPlan.action = TerraformAction.Flatten;
                    cellPlan.targetHeight = plan.baseHeight;
                }
                // Use road direction so cliff aligns with road segment (not terrain-only).
                // Grid: +x=North, -x=South, +y=West, -y=East. Screen: -y => bottom-right => South.
                cellPlan.postTerraformSlopeType = GetSlopeTypeFromRoadDirection(dxOut, dyOut);
            }
            plan.pathCells.Add(cellPlan);
        }

        ExpandAdjacentFlattenCellsRecursively(plan, path, heightMap);
        return plan;
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
                if (Mathf.Abs(nh - baseHeight) <= 1) continue;
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
    /// Returns the slope type that matches the road's travel direction.
    /// Used when the road crosses an orthogonal slope so the cliff aligns with the road segment.
    /// Grid: +x=North, -x=South, +y=West, -y=East. Screen: -y => bottom-right => South.
    /// </summary>
    static TerrainSlopeType GetSlopeTypeFromRoadDirection(int dx, int dy)
    {
        if (Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0)
            return dx > 0 ? TerrainSlopeType.North : TerrainSlopeType.South;
        if (dy != 0)
            return dy > 0 ? TerrainSlopeType.West : TerrainSlopeType.East;
        return TerrainSlopeType.Flat;
    }

    /// <summary>
    /// Derives the orthogonal slope for a corner slope cell when road is orthogonal.
    /// Uses exit direction (dxOut, dyOut) to maintain continuity of the road segment.
    /// Picks East/West for horizontal road, North/South for vertical road, based on which neighbor is higher.
    /// </summary>
    static TerrainSlopeType GetOrthogonalFromCornerSlope(TerrainSlopeType cornerSlope, int dxOut, int dyOut)
    {
        bool isHorizontalRoad = dxOut != 0 && dyOut == 0;
        switch (cornerSlope)
        {
            case TerrainSlopeType.SouthEastUp: return isHorizontalRoad ? TerrainSlopeType.East : TerrainSlopeType.South;
            case TerrainSlopeType.NorthEastUp: return isHorizontalRoad ? TerrainSlopeType.East : TerrainSlopeType.North;
            case TerrainSlopeType.SouthWestUp: return isHorizontalRoad ? TerrainSlopeType.West : TerrainSlopeType.South;
            case TerrainSlopeType.NorthWestUp: return isHorizontalRoad ? TerrainSlopeType.West : TerrainSlopeType.North;
            default: return TerrainSlopeType.Flat;
        }
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
    /// [Obsolete] Use ComputePathPlan for path-based terraforming. Returns true if the cell needs terraforming.
    /// </summary>
    [System.Obsolete("Use ComputePathPlan for path-based terraforming instead.")]
    public bool TerraformNeeded(int x, int y, Vector2 roadDir, out TerraformAction action, out OrthogonalDirection orthogonalDir,
        System.Collections.Generic.IList<Vector2> path = null)
    {
        action = TerraformAction.None;
        orthogonalDir = OrthogonalDirection.North;

        if (terrainManager == null || gridManager == null) return false;
        var heightMap = terrainManager.GetHeightMap();
        if (heightMap == null || !heightMap.IsValidPosition(x, y)) return false;

        int currentHeight = heightMap.GetHeight(x, y);
        if (currentHeight <= TerrainManager.SEA_LEVEL) return false;

        TerrainSlopeType slopeType = terrainManager.GetTerrainSlopeTypeAt(x, y);
        int dx = Mathf.RoundToInt(roadDir.x);
        int pathCount = path != null ? path.Count : 0;
        int dy = Mathf.RoundToInt(roadDir.y);

        if (slopeType == TerrainSlopeType.Flat)
        {
            Debug.Log($"[TerraformNeeded] ({x},{y}) slopeType=Flat -> SKIP (no terraform)");
            return false;
        }

        int n = GetNeighborHeight(heightMap, x + 1, y);
        int s = GetNeighborHeight(heightMap, x - 1, y);
        int e = GetNeighborHeight(heightMap, x, y - 1);
        int w = GetNeighborHeight(heightMap, x, y + 1);
        int ne = GetNeighborHeight(heightMap, x + 1, y - 1);
        int nw = GetNeighborHeight(heightMap, x + 1, y + 1);
        int sw = GetNeighborHeight(heightMap, x - 1, y + 1);
        int se = GetNeighborHeight(heightMap, x - 1, y - 1);
        Debug.Log($"[TerraformNeeded] ({x},{y}) currentHeight={currentHeight} slopeType={slopeType} roadDir=({dx},{dy}) neighbors N={n} S={s} E={e} W={w} NE={ne} NW={nw} SW={sw} SE={se}");

        bool isHorizontalRoad = Mathf.Abs(dx) > Mathf.Abs(dy);
        bool isVerticalRoad = Mathf.Abs(dy) >= Mathf.Abs(dx) && dx == 0;

        if (dx != 0 && dy != 0)
        {
            isHorizontalRoad = Mathf.Abs(dx) >= Mathf.Abs(dy);
            isVerticalRoad = Mathf.Abs(dy) > Mathf.Abs(dx);
        }

        bool isOrthogonalSlope = slopeType == TerrainSlopeType.North || slopeType == TerrainSlopeType.South
            || slopeType == TerrainSlopeType.East || slopeType == TerrainSlopeType.West;

        bool roadParallelToSlope = isOrthogonalSlope && ((slopeType == TerrainSlopeType.North || slopeType == TerrainSlopeType.South)
            ? isVerticalRoad : isHorizontalRoad);

        if (roadParallelToSlope)
        {
            action = TerraformAction.Flatten;
            Debug.Log($"[TerraformNeeded] ({x},{y}) slopeType={slopeType} roadDir=({dx},{dy}) orthogonal->Flatten");
            return true;
        }

        bool isDiagonalSlope = slopeType == TerrainSlopeType.NorthEast || slopeType == TerrainSlopeType.NorthWest
            || slopeType == TerrainSlopeType.SouthEast || slopeType == TerrainSlopeType.SouthWest;

        bool isCornerSlope = slopeType == TerrainSlopeType.NorthEastUp || slopeType == TerrainSlopeType.NorthWestUp
            || slopeType == TerrainSlopeType.SouthEastUp || slopeType == TerrainSlopeType.SouthWestUp;

        if (isDiagonalSlope || isCornerSlope)
        {
            if (dx != 0 && dy != 0)
            {
                orthogonalDir = GetOrthogonalFromRoadDirection(dx, dy);
                action = TerraformAction.DiagonalToOrthogonal;
                Debug.Log($"[TerraformNeeded] ({x},{y}) slopeType={slopeType} roadDir=({dx},{dy}) diagonal segment->DiagonalToOrthogonal");
                return true;
            }
            if (path != null && path.Count >= 2)
            {
                action = TerraformAction.Flatten;
                Debug.Log($"[TerraformNeeded] ({x},{y}) slopeType={slopeType} roadDir=({dx},{dy}) diagonal/corner pathCount={pathCount}->Flatten");
                return true;
            }
            Debug.Log($"[TerraformNeeded] ({x},{y}) slopeType={slopeType} roadDir=({dx},{dy}) diagonal/corner path=null->return false");
            return false;
        }

            Debug.Log($"[TerraformNeeded] ({x},{y}) slopeType={slopeType} roadDir=({dx},{dy}) no match->return false");
        return false;
    }

    static OrthogonalDirection GetOrthogonalFromRoadDirection(int dx, int dy)
    {
        if (Mathf.Abs(dx) >= Mathf.Abs(dy))
        {
            return dx > 0 ? OrthogonalDirection.North : OrthogonalDirection.South;
        }
        return dy > 0 ? OrthogonalDirection.West : OrthogonalDirection.East;
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
