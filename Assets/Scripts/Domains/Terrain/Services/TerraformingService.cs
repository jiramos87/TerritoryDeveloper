using System.Collections.Generic;
using UnityEngine;
using Territory.Terrain;
using Territory.Core;

namespace Domains.Terrain.Services
{
    /// <summary>
    /// Pure terraforming path-plan logic extracted from Territory.Terrain.TerraformingService MonoBehaviour.
    /// No MonoBehaviour dependency — deps injected via constructor delegates.
    /// Extracted per Strategy γ atomization (TECH-23791).
    /// Invariants #1 (HeightMap/Cell sync), #7 (shore band) preserved — read-only HeightMap access only.
    /// </summary>
    public class TerraformingService
    {
        private readonly System.Func<Territory.Terrain.HeightMap> _getHeightMap;
        private readonly System.Func<int, int, bool> _isRegisteredOpenWaterAt;
        private readonly System.Func<int, int, bool> _isWaterSlopeCell;
        private readonly System.Func<int, int, bool> _isDryShoreOrRimMembershipEligible;
        private readonly System.Func<int, int, Territory.Terrain.HeightMap, bool> _shouldSkipRoadTerraformSurfaceAt;
        private readonly System.Func<int, int, Territory.Terrain.TerrainSlopeType> _getTerrainSlopeTypeAt;
        private readonly System.Func<int, int, CityCell> _getCell;
        private readonly System.Action<int, int> _restoreTerrainForCell;
        private readonly System.Func<int, int, bool> _isWaterAt;
        private readonly bool _expandCutThroughAdjacentByOneStep;
        private readonly int _cutThroughMinCellsFromMapEdge;

        public TerraformingService(
            System.Func<Territory.Terrain.HeightMap> getHeightMap,
            System.Func<int, int, bool> isRegisteredOpenWaterAt,
            System.Func<int, int, bool> isWaterSlopeCell,
            System.Func<int, int, bool> isDryShoreOrRimMembershipEligible,
            System.Func<int, int, Territory.Terrain.HeightMap, bool> shouldSkipRoadTerraformSurfaceAt,
            System.Func<int, int, Territory.Terrain.TerrainSlopeType> getTerrainSlopeTypeAt,
            System.Func<int, int, CityCell> getCell,
            System.Action<int, int> restoreTerrainForCell = null,
            System.Func<int, int, bool> isWaterAt = null,
            bool expandCutThroughAdjacentByOneStep = false,
            int cutThroughMinCellsFromMapEdge = 2)
        {
            _getHeightMap = getHeightMap;
            _isRegisteredOpenWaterAt = isRegisteredOpenWaterAt;
            _isWaterSlopeCell = isWaterSlopeCell;
            _isDryShoreOrRimMembershipEligible = isDryShoreOrRimMembershipEligible;
            _shouldSkipRoadTerraformSurfaceAt = shouldSkipRoadTerraformSurfaceAt;
            _getTerrainSlopeTypeAt = getTerrainSlopeTypeAt;
            _getCell = getCell;
            _restoreTerrainForCell = restoreTerrainForCell;
            _isWaterAt = isWaterAt;
            _expandCutThroughAdjacentByOneStep = expandCutThroughAdjacentByOneStep;
            _cutThroughMinCellsFromMapEdge = cutThroughMinCellsFromMapEdge;
        }

        /// <summary>
        /// Expand diagonal steps (dx!=0 and dy!=0) into two cardinal steps.
        /// Public static — RoadManager can use for both ComputePathPlan + ResolveForPath.
        /// </summary>
        public static List<Vector2> ExpandDiagonalStepsToCardinal(IList<Vector2> path)
        {
            if (path == null || path.Count < 2) return new List<Vector2>(path ?? new Vector2[0]);

            var clean = new List<Vector2> { path[0] };
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

        /// <summary>Compute base height for path terraforming.</summary>
        public int ComputePathBaseHeight(IList<Vector2> path)
        {
            var (baseHeight, _, _) = ComputePathBaseHeightAndCutThrough(path);
            return baseHeight;
        }

        (int baseHeight, bool pathCrossesHill, int maxHeight) ComputePathBaseHeightAndCutThrough(IList<Vector2> path)
        {
            var heightMap = _getHeightMap?.Invoke();
            if (heightMap == null || path == null || path.Count == 0) return (1, false, 1);

            int minHeight = int.MaxValue;
            int maxHeight = int.MinValue;

            for (int i = 0; i < path.Count; i++)
            {
                int x = (int)path[i].x;
                int y = (int)path[i].y;
                if (!heightMap.IsValidPosition(x, y)) continue;
                if (_isRegisteredOpenWaterAt(x, y)) continue;

                int h = heightMap.GetHeight(x, y);
                minHeight = Mathf.Min(minHeight, h);
                maxHeight = Mathf.Max(maxHeight, h);
            }

            int baseHeight = maxHeight > minHeight ? minHeight : (minHeight < int.MaxValue ? minHeight : 1);
            bool pathCrossesHill = (maxHeight >= 2) && (maxHeight > minHeight)
                && HasConsecutiveHeightDiffGreaterThanOne(path, heightMap);
            return (baseHeight, pathCrossesHill, maxHeight);
        }

        bool HasConsecutiveHeightDiffGreaterThanOne(IList<Vector2> path, Territory.Terrain.HeightMap heightMap)
        {
            if (path == null || path.Count < 2 || heightMap == null) return false;
            for (int i = 0; i < path.Count - 1; i++)
            {
                int x1 = (int)path[i].x, y1 = (int)path[i].y;
                int x2 = (int)path[i + 1].x, y2 = (int)path[i + 1].y;
                if (_isRegisteredOpenWaterAt(x1, y1) || _isRegisteredOpenWaterAt(x2, y2)) continue;
                int h1 = heightMap.GetHeight(x1, y1);
                int h2 = heightMap.GetHeight(x2, y2);
                if (Mathf.Abs(h2 - h1) > 1) return true;
            }
            return false;
        }

        /// <summary>Compute path-level terraform plan. Implements Rules 3, 4, 5, 8.</summary>
        public Territory.Terrain.PathTerraformPlan ComputePathPlan(IList<Vector2> path, bool waterBridgeTerraformRelaxation = false)
        {
            var plan = new Territory.Terrain.PathTerraformPlan();
            plan.waterBridgeTerraformRelaxation = waterBridgeTerraformRelaxation;
            var heightMap = _getHeightMap?.Invoke();
            if (heightMap == null || path == null || path.Count == 0) return plan;

            if (path.Count >= 2)
                path = ExpandDiagonalStepsToCardinal(path);

            plan.waterBridgeDeckDisplayHeight = 0;
            if (waterBridgeTerraformRelaxation)
                TryAssignWaterBridgeDeckDisplayHeight(plan, path, heightMap);

            var (baseHeight, pathCrossesHill, maxHeight) = ComputePathBaseHeightAndCutThrough(path);
            plan.baseHeight = baseHeight;
            plan.isCutThrough = pathCrossesHill;

            if (pathCrossesHill)
            {
                if (maxHeight - baseHeight > 1 && !waterBridgeTerraformRelaxation)
                    plan.isValid = false;

                for (int i = 0; i < path.Count; i++)
                {
                    int x = (int)path[i].x;
                    int y = (int)path[i].y;
                    int h = heightMap.IsValidPosition(x, y) ? heightMap.GetHeight(x, y) : HeightMap.MIN_HEIGHT;

                    var cellPlan = new Territory.Terrain.PathTerraformPlan.CellPlan
                    {
                        position = new Vector2Int(x, y),
                        action = TerraformAction.Flatten,
                        direction = OrthogonalDirection.North,
                        originalHeight = h,
                        targetHeight = plan.baseHeight,
                        postTerraformSlopeType = Territory.Terrain.TerrainSlopeType.Flat
                    };

                    if (!heightMap.IsValidPosition(x, y) || _shouldSkipRoadTerraformSurfaceAt(x, y, heightMap))
                    {
                        cellPlan.action = TerraformAction.None;
                        cellPlan.targetHeight = h;
                    }

                    if (i < path.Count - 1)
                    {
                        int nx = (int)path[i + 1].x, ny = (int)path[i + 1].y;
                        int hNext = heightMap.GetHeight(nx, ny);
                        bool coastalA = IsCoastal(x, y);
                        bool coastalB = IsCoastal(nx, ny);
                        if (!coastalA && !coastalB && Mathf.Abs(hNext - h) > 1
                            && !PathEdgeExemptDryDryForWaterBridgeRelaxation(path, i, heightMap, waterBridgeTerraformRelaxation))
                            plan.isValid = false;
                    }

                    plan.pathCells.Add(cellPlan);
                }

                if (!waterBridgeTerraformRelaxation)
                    ExpandAdjacentFlattenCellsRecursively(plan, path, heightMap);
                if (plan.isValid && _cutThroughMinCellsFromMapEdge > 0 && !CutThroughHasAcceptableMapMargin(plan, path, heightMap))
                    plan.isValid = false;
                return plan;
            }

            bool preferSlopeClimb = !HasConsecutiveHeightDiffGreaterThanOne(path, heightMap);

            for (int i = 0; i < path.Count; i++)
            {
                int x = (int)path[i].x;
                int y = (int)path[i].y;
                int h = heightMap.IsValidPosition(x, y) ? heightMap.GetHeight(x, y) : HeightMap.MIN_HEIGHT;

                var cellPlan = new Territory.Terrain.PathTerraformPlan.CellPlan
                {
                    position = new Vector2Int(x, y),
                    action = TerraformAction.None,
                    direction = OrthogonalDirection.North,
                    originalHeight = h,
                    targetHeight = h,
                    postTerraformSlopeType = Territory.Terrain.TerrainSlopeType.Flat
                };

                if (!heightMap.IsValidPosition(x, y) || _isRegisteredOpenWaterAt(x, y))
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

                Territory.Terrain.TerrainSlopeType slopeType = _getTerrainSlopeTypeAt(x, y);

                int dSeg = ComputeSegmentDeltaHForPostSlope(heightMap, path, i, h);
                bool segmentOneStepLand = preferSlopeClimb && (dSeg == 1 || dSeg == -1);

                if (i < path.Count - 1)
                {
                    int nx = (int)path[i + 1].x, ny = (int)path[i + 1].y;
                    int hNext = heightMap.GetHeight(nx, ny);
                    bool coastalA = IsCoastal(x, y);
                    bool coastalB = IsCoastal(nx, ny);
                    if (!coastalA && !coastalB && Mathf.Abs(hNext - h) > 1
                        && !PathEdgeExemptDryDryForWaterBridgeRelaxation(path, i, heightMap, waterBridgeTerraformRelaxation))
                        plan.isValid = false;
                }

                if (slopeType == Territory.Terrain.TerrainSlopeType.Flat)
                {
                    plan.pathCells.Add(cellPlan);
                    continue;
                }

                bool isOrthogonalSlope = slopeType == Territory.Terrain.TerrainSlopeType.North || slopeType == Territory.Terrain.TerrainSlopeType.South
                    || slopeType == Territory.Terrain.TerrainSlopeType.East || slopeType == Territory.Terrain.TerrainSlopeType.West;
                bool isHorizontalRoad = Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0;
                bool isVerticalRoad = Mathf.Abs(dy) >= Mathf.Abs(dx) && dx == 0;
                if (dx != 0 && dy != 0)
                {
                    isHorizontalRoad = Mathf.Abs(dx) >= Mathf.Abs(dy);
                    isVerticalRoad = Mathf.Abs(dy) > Mathf.Abs(dx);
                }
                bool roadParallelToSlope = isOrthogonalSlope && ((slopeType == Territory.Terrain.TerrainSlopeType.North || slopeType == Territory.Terrain.TerrainSlopeType.South)
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
                    cellPlan.postTerraformSlopeType = Territory.Terrain.TerrainSlopeType.Flat;
                    plan.pathCells.Add(cellPlan);
                    AddAdjacentFlattenCells(plan, path, heightMap, x, y, plan.baseHeight, h);
                    continue;
                }

                bool isDiagonalSlope = slopeType == Territory.Terrain.TerrainSlopeType.NorthEast || slopeType == Territory.Terrain.TerrainSlopeType.NorthWest
                    || slopeType == Territory.Terrain.TerrainSlopeType.SouthEast || slopeType == Territory.Terrain.TerrainSlopeType.SouthWest;
                bool isCornerSlope = slopeType == Territory.Terrain.TerrainSlopeType.NorthEastUp || slopeType == Territory.Terrain.TerrainSlopeType.NorthWestUp
                    || slopeType == Territory.Terrain.TerrainSlopeType.SouthEastUp || slopeType == Territory.Terrain.TerrainSlopeType.SouthWestUp;

                if ((isDiagonalSlope || isCornerSlope) && (dx != 0 && dy != 0))
                {
                    cellPlan.action = TerraformAction.Flatten;
                    cellPlan.targetHeight = plan.baseHeight;
                    cellPlan.postTerraformSlopeType = Territory.Terrain.TerrainSlopeType.Flat;
                    plan.pathCells.Add(cellPlan);
                    AddAdjacentFlattenCells(plan, path, heightMap, x, y, plan.baseHeight, h);
                    continue;
                }

                if ((isDiagonalSlope || isCornerSlope) && (dx == 0 || dy == 0))
                {
                    if (isCornerSlope)
                    {
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
                            cellPlan.action = TerraformAction.None;
                            cellPlan.targetHeight = h;
                            cellPlan.postTerraformSlopeType = GetPostTerraformSlopeTypeAlongExit(heightMap, path, i, h, dxOut, dyOut);
                        }
                        else
                        {
                            cellPlan.action = TerraformAction.Flatten;
                            cellPlan.targetHeight = plan.baseHeight;
                            cellPlan.postTerraformSlopeType = Territory.Terrain.TerrainSlopeType.Flat;
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
            if ((!preferSlopeClimb || anyFlattenScheduled) && !waterBridgeTerraformRelaxation)
                ExpandAdjacentFlattenCellsRecursively(plan, path, heightMap);

            return plan;
        }

        bool IsCoastal(int x, int y)
        {
            return _isRegisteredOpenWaterAt(x, y) || _isWaterSlopeCell(x, y) || _isDryShoreOrRimMembershipEligible(x, y);
        }

        bool PathEdgeExemptDryDryForWaterBridgeRelaxation(IList<Vector2> path, int edgeStartIdx, Territory.Terrain.HeightMap heightMap, bool waterBridgeTerraformRelaxation)
        {
            if (!waterBridgeTerraformRelaxation || path == null || heightMap == null) return false;
            if (edgeStartIdx < 0 || edgeStartIdx >= path.Count - 1) return false;

            int x = (int)path[edgeStartIdx].x, y = (int)path[edgeStartIdx].y;
            int nx = (int)path[edgeStartIdx + 1].x, ny = (int)path[edgeStartIdx + 1].y;
            if (!heightMap.IsValidPosition(x, y) || !heightMap.IsValidPosition(nx, ny)) return false;

            int h = heightMap.GetHeight(x, y);
            int hNext = heightMap.GetHeight(nx, ny);
            if (Mathf.Abs(hNext - h) <= 1) return false;

            if (IsCoastal(x, y) || IsCoastal(nx, ny)) return false;

            return PathCellMooreTouchesOnPathCoastalTile(path, x, y)
                || PathCellMooreTouchesOnPathCoastalTile(path, nx, ny);
        }

        bool PathCellMooreTouchesOnPathCoastalTile(IList<Vector2> path, int px, int py)
        {
            if (path == null) return false;
            for (int i = 0; i < path.Count; i++)
            {
                int qx = (int)path[i].x, qy = (int)path[i].y;
                if (qx == px && qy == py) continue;
                int adx = Mathf.Abs(qx - px);
                int ady = Mathf.Abs(qy - py);
                if (adx > 1 || ady > 1) continue;
                if (IsCoastal(qx, qy)) return true;
            }
            return false;
        }

        void InvalidatePlanIfPathBesideSteepLandCliff(Territory.Terrain.PathTerraformPlan plan, IList<Vector2> path, Territory.Terrain.HeightMap heightMap, bool preferSlopeClimb)
        {
            if (!preferSlopeClimb || plan == null || heightMap == null || path == null || !plan.isValid) return;

            var pathSet = new HashSet<Vector2Int>();
            for (int i = 0; i < path.Count; i++)
                pathSet.Add(new Vector2Int((int)path[i].x, (int)path[i].y));

            int[] cdx = { 1, -1, 0, 0 };
            int[] cdy = { 0, 0, 1, -1 };
            for (int i = 0; i < path.Count; i++)
            {
                int x = (int)path[i].x;
                int y = (int)path[i].y;
                if (!heightMap.IsValidPosition(x, y)) continue;
                if (_isRegisteredOpenWaterAt(x, y)) continue;
                int h = heightMap.GetHeight(x, y);
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + cdx[d];
                    int ny = y + cdy[d];
                    if (!heightMap.IsValidPosition(nx, ny)) continue;
                    if (pathSet.Contains(new Vector2Int(nx, ny))) continue;
                    if (_isRegisteredOpenWaterAt(nx, ny)) continue;
                    int nh = heightMap.GetHeight(nx, ny);
                    if (Mathf.Abs(nh - h) > 1)
                    {
                        plan.isValid = false;
                        return;
                    }
                }
            }
        }

        bool CutThroughHasAcceptableMapMargin(Territory.Terrain.PathTerraformPlan plan, IList<Vector2> path, Territory.Terrain.HeightMap heightMap)
        {
            int m = _cutThroughMinCellsFromMapEdge;
            if (m <= 0 || heightMap == null || plan == null || path == null) return true;

            int w = heightMap.Width;
            int h = heightMap.Height;
            if (w <= m * 2 || h <= m * 2) return true;

            bool Inside(int x, int y) => x >= m && y >= m && x < w - m && y < h - m;

            for (int i = 0; i < path.Count; i++)
            {
                int x = (int)path[i].x;
                int y = (int)path[i].y;
                if (!heightMap.IsValidPosition(x, y)) continue;
                if (!Inside(x, y)) return false;
            }

            for (int i = 0; i < plan.adjacentCells.Count; i++)
            {
                var c = plan.adjacentCells[i];
                if (c.action != TerraformAction.Flatten) continue;
                if (!Inside(c.position.x, c.position.y)) return false;
            }

            return true;
        }

        void ExpandAdjacentFlattenCellsRecursively(Territory.Terrain.PathTerraformPlan plan, IList<Vector2> path, Territory.Terrain.HeightMap heightMap)
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
                    if (_isRegisteredOpenWaterAt(nx, ny) || nh == baseHeight) continue;
                    bool oneStepRidgeAboveBase = _expandCutThroughAdjacentByOneStep && plan.isCutThrough && nh == baseHeight + 1;
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
                        plan.adjacentCells.Add(new Territory.Terrain.PathTerraformPlan.CellPlan
                        {
                            position = new Vector2Int(nx, ny),
                            action = TerraformAction.Flatten,
                            direction = OrthogonalDirection.North,
                            originalHeight = nh,
                            targetHeight = baseHeight,
                            postTerraformSlopeType = Territory.Terrain.TerrainSlopeType.Flat
                        });
                    }
                }
            }
        }

        int ComputeSegmentDeltaHForPostSlope(Territory.Terrain.HeightMap heightMap, IList<Vector2> path, int i, int hLandAtCell)
        {
            if (heightMap == null || path == null) return 0;
            int cx = (int)path[i].x, cy = (int)path[i].y;
            if (!heightMap.IsValidPosition(cx, cy) || _isRegisteredOpenWaterAt(cx, cy)) return 0;
            if (i < path.Count - 1)
            {
                int nx = (int)path[i + 1].x, ny = (int)path[i + 1].y;
                if (!heightMap.IsValidPosition(nx, ny)) return 0;
                if (_isRegisteredOpenWaterAt(nx, ny)) return 0;
                int hn = heightMap.GetHeight(nx, ny);
                return hn - hLandAtCell;
            }
            if (i > 0)
            {
                int px = (int)path[i - 1].x, py = (int)path[i - 1].y;
                if (!heightMap.IsValidPosition(px, py)) return 0;
                if (_isRegisteredOpenWaterAt(px, py)) return 0;
                int hp = heightMap.GetHeight(px, py);
                return hLandAtCell - hp;
            }
            return 0;
        }

        static Territory.Terrain.TerrainSlopeType GetSlopeTypeFromTravelVector(int dx, int dy)
        {
            if (Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0)
                return dx > 0 ? Territory.Terrain.TerrainSlopeType.North : Territory.Terrain.TerrainSlopeType.South;
            if (dy != 0)
                return dy > 0 ? Territory.Terrain.TerrainSlopeType.West : Territory.Terrain.TerrainSlopeType.East;
            return Territory.Terrain.TerrainSlopeType.Flat;
        }

        Territory.Terrain.TerrainSlopeType GetPostTerraformSlopeTypeAlongExit(Territory.Terrain.HeightMap heightMap, IList<Vector2> path, int i, int hLand, int dxOut, int dyOut)
        {
            int dSeg = ComputeSegmentDeltaHForPostSlope(heightMap, path, i, hLand);
            if (dSeg > 0) return GetSlopeTypeFromTravelVector(-dxOut, -dyOut);
            return GetSlopeTypeFromTravelVector(dxOut, dyOut);
        }

        static bool IsLowerInSlopePair(Territory.Terrain.HeightMap hm, int x, int y, Territory.Terrain.TerrainSlopeType slopeType)
        {
            int h = hm.GetHeight(x, y);
            int n = GetNeighborHeight(hm, x + 1, y);
            int s = GetNeighborHeight(hm, x - 1, y);
            int e = GetNeighborHeight(hm, x, y - 1);
            int w = GetNeighborHeight(hm, x, y + 1);
            switch (slopeType)
            {
                case Territory.Terrain.TerrainSlopeType.South: return n > h;
                case Territory.Terrain.TerrainSlopeType.North: return s > h;
                case Territory.Terrain.TerrainSlopeType.West: return e > h;
                case Territory.Terrain.TerrainSlopeType.East: return w > h;
                default: return false;
            }
        }

        void AddAdjacentFlattenCells(Territory.Terrain.PathTerraformPlan plan, IList<Vector2> path, Territory.Terrain.HeightMap heightMap, int x, int y, int baseHeight, int pathCellHeight)
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
                if (_isRegisteredOpenWaterAt(nx, ny) || nh == baseHeight) continue;
                bool needsFlatten = nh < pathCellHeight || nh > baseHeight + 1;
                if (!needsFlatten) continue;

                var adj = new Territory.Terrain.PathTerraformPlan.CellPlan
                {
                    position = new Vector2Int(nx, ny),
                    action = TerraformAction.Flatten,
                    direction = OrthogonalDirection.North,
                    originalHeight = nh,
                    targetHeight = baseHeight,
                    postTerraformSlopeType = Territory.Terrain.TerrainSlopeType.Flat
                };
                bool alreadyAdded = false;
                for (int j = 0; j < plan.adjacentCells.Count; j++)
                    if (plan.adjacentCells[j].position.x == nx && plan.adjacentCells[j].position.y == ny) { alreadyAdded = true; break; }
                if (!alreadyAdded)
                    plan.adjacentCells.Add(adj);
            }
        }

        void TryAssignWaterBridgeDeckDisplayHeight(Territory.Terrain.PathTerraformPlan plan, IList<Vector2> path, Territory.Terrain.HeightMap heightMap)
        {
            if (plan == null || path == null || path.Count < 1 || heightMap == null || _getCell == null) return;

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
                    if (!inRun) { runs++; rs = i; inRun = true; }
                    re = i;
                }
                else inRun = false;
            }

            if (runs == 1 && rs >= 1 && re < path.Count - 1)
            {
                int bx = (int)path[rs - 1].x, by = (int)path[rs - 1].y;
                int ax = (int)path[re + 1].x, ay = (int)path[re + 1].y;
                if (!IsWaterOrWaterSlopeForBridgeDeckHeight(bx, by, heightMap) && !IsWaterOrWaterSlopeForBridgeDeckHeight(ax, ay, heightMap))
                {
                    CityCell landBefore = _getCell(bx, by);
                    CityCell landExit = _getCell(ax, ay);
                    if (landBefore != null && landExit != null)
                    {
                        int hIn = landBefore.GetCellInstanceHeight();
                        int hOut = landExit.GetCellInstanceHeight();
                        int deckH = hOut > 0 ? hOut : hIn;
                        if (deckH <= 0) deckH = hIn > 0 ? hIn : hOut;
                        if (deckH > 0) { plan.waterBridgeDeckDisplayHeight = deckH; return; }
                    }
                }
            }

            if (runs == 1 && re >= 0 && re < path.Count - 1)
            {
                int ax = (int)path[re + 1].x, ay = (int)path[re + 1].y;
                if (!IsWaterOrWaterSlopeForBridgeDeckHeight(ax, ay, heightMap))
                {
                    CityCell landExit = _getCell(ax, ay);
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
                    CityCell landBefore = _getCell(bx, by);
                    if (landBefore != null && landBefore.GetCellInstanceHeight() > 0)
                    {
                        plan.waterBridgeDeckDisplayHeight = landBefore.GetCellInstanceHeight();
                        return;
                    }
                }
            }

            int best = 0;
            for (int i = 0; i < path.Count; i++)
            {
                int x = (int)path[i].x;
                int y = (int)path[i].y;
                if (_isRegisteredOpenWaterAt(x, y)) continue;
                CityCell pathCell = _getCell(x, y);
                if (pathCell == null) continue;
                int h = pathCell.GetCellInstanceHeight();
                if (h <= 0) continue;
                if (!CellQualifiesForDeckDisplayLipRelaxed(x, y, h, heightMap)) continue;
                if (h > best) best = h;
            }

            if (best > 0) plan.waterBridgeDeckDisplayHeight = best;
        }

        bool IsWaterOrWaterSlopeForBridgeDeckHeight(int x, int y, Territory.Terrain.HeightMap heightMap)
        {
            if (heightMap == null || !heightMap.IsValidPosition(x, y)) return false;
            if (_isRegisteredOpenWaterAt(x, y)) return true;
            return _isWaterSlopeCell(x, y);
        }

        static int GetNeighborHeight(Territory.Terrain.HeightMap heightMap, int nx, int ny)
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

        /// <summary>Applies terraforming to a cell: modifies heightMap and restores terrain visual.</summary>
        public void ApplyTerraform(int x, int y, TerraformAction action, OrthogonalDirection orthogonalDir, bool allowLowering = true, int? baseHeight = null)
        {
            var heightMap = _getHeightMap?.Invoke();
            if (heightMap == null || !heightMap.IsValidPosition(x, y)) return;

            int currentHeight = heightMap.GetHeight(x, y);
            int newHeight = ComputeNewHeight(heightMap, x, y, action, orthogonalDir, baseHeight);
            if (newHeight < 0) return;

            if (!allowLowering && newHeight < currentHeight) return;

            heightMap.SetHeight(x, y, newHeight);
            _restoreTerrainForCell?.Invoke(x, y);
        }

        /// <summary>Reverts terraforming for preview cancel: restores original height.</summary>
        public void RevertTerraform(int x, int y, int originalHeight)
        {
            var heightMap = _getHeightMap?.Invoke();
            if (heightMap == null || !heightMap.IsValidPosition(x, y)) return;

            heightMap.SetHeight(x, y, originalHeight);
            _restoreTerrainForCell?.Invoke(x, y);
        }

        int ComputeNewHeight(Territory.Terrain.HeightMap heightMap, int x, int y, TerraformAction action, OrthogonalDirection orthogonalDir, int? baseHeight = null)
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
                if (baseHeight.HasValue) return baseHeight.Value;
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
        /// Builds a PathTerraformPlan with no terraform height mutations (all TerraformAction.None), water-bridge relaxation on,
        /// and waterBridgeDeckDisplayHeight from TryAssignWaterBridgeDeckDisplayHeight.
        /// expandedCardinalPath must already be cardinal.
        /// </summary>
        public bool TryBuildDeckSpanOnlyWaterBridgePlan(IList<Vector2> expandedCardinalPath, out Territory.Terrain.PathTerraformPlan plan)
        {
            plan = null;
            var heightMap = _getHeightMap?.Invoke();
            if (heightMap == null || expandedCardinalPath == null || expandedCardinalPath.Count == 0 || _getCell == null)
                return false;

            plan = new Territory.Terrain.PathTerraformPlan
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
                if (!heightMap.IsValidPosition(x, y) || _getCell(x, y) == null)
                {
                    plan.isValid = false;
                    plan = null;
                    return false;
                }

                int h = heightMap.GetHeight(x, y);
                Territory.Terrain.TerrainSlopeType slope = Territory.Terrain.TerrainSlopeType.Flat;
                if (!_isRegisteredOpenWaterAt(x, y) && !_isWaterSlopeCell(x, y))
                    slope = _getTerrainSlopeTypeAt(x, y);

                plan.pathCells.Add(new Territory.Terrain.PathTerraformPlan.CellPlan
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

        bool CellQualifiesForDeckDisplayLipRelaxed(int x, int y, int h, Territory.Terrain.HeightMap heightMap)
        {
            if (heightMap == null || !heightMap.IsValidPosition(x, y)) return false;

            int[] cdx = { 1, -1, 0, 0 };
            int[] cdy = { 0, 0, 1, -1 };
            for (int d = 0; d < 4; d++)
            {
                int nx = x + cdx[d];
                int ny = y + cdy[d];
                if (!heightMap.IsValidPosition(nx, ny)) continue;
                int hn = heightMap.GetHeight(nx, ny);
                if (hn >= h) continue;
                if (_isRegisteredOpenWaterAt(nx, ny) || _isWaterSlopeCell(nx, ny))
                    return true;
                if (DryCellTouchesRegisteredWaterForDeckHeight(nx, ny))
                    return true;
            }

            return false;
        }

        bool DryCellTouchesRegisteredWaterForDeckHeight(int x, int y)
        {
            if (_isRegisteredOpenWaterAt(x, y) || _isWaterSlopeCell(x, y)) return true;
            if (_isWaterAt == null) return false;

            int[] mdx = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] mdy = { 0, 0, 1, -1, 1, -1, 1, -1 };
            for (int d = 0; d < 8; d++)
            {
                if (_isWaterAt(x + mdx[d], y + mdy[d])) return true;
            }
            return false;
        }
    }
}
