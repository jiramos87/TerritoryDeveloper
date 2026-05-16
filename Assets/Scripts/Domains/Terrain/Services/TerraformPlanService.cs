using System.Collections.Generic;
using UnityEngine;
using Territory.Terrain;
using Territory.Core;

namespace Domains.Terrain.Services
{
    /// <summary>
    /// Path-plan computation extracted from TerraformingService (TECH-30056 Stage 7.2 split).
    /// Owns: ComputePathPlan, ComputePathBaseHeight, ExpandDiagonalStepsToCardinal, validation helpers.
    /// HeightMap write order invariant preserved — read-only HeightMap access only.
    /// </summary>
    public class TerraformPlanService
    {
        private readonly System.Func<HeightMap> _getHeightMap;
        private readonly System.Func<int, int, bool> _isRegisteredOpenWaterAt;
        private readonly System.Func<int, int, bool> _isWaterSlopeCell;
        private readonly System.Func<int, int, bool> _isDryShoreOrRimMembershipEligible;
        private readonly System.Func<int, int, HeightMap, bool> _shouldSkipRoadTerraformSurfaceAt;
        private readonly System.Func<int, int, TerrainSlopeType> _getTerrainSlopeTypeAt;
        private readonly TerraformSmoothService _smooth;
        private readonly bool _expandCutThroughAdjacentByOneStep;
        private readonly int _cutThroughMinCellsFromMapEdge;

        /// <summary>Construct terraform plan service with dependencies.</summary>
        public TerraformPlanService(
            System.Func<HeightMap> getHeightMap,
            System.Func<int, int, bool> isRegisteredOpenWaterAt,
            System.Func<int, int, bool> isWaterSlopeCell,
            System.Func<int, int, bool> isDryShoreOrRimMembershipEligible,
            System.Func<int, int, HeightMap, bool> shouldSkipRoadTerraformSurfaceAt,
            System.Func<int, int, TerrainSlopeType> getTerrainSlopeTypeAt,
            TerraformSmoothService smooth,
            bool expandCutThroughAdjacentByOneStep = false,
            int cutThroughMinCellsFromMapEdge = 2)
        {
            _getHeightMap = getHeightMap;
            _isRegisteredOpenWaterAt = isRegisteredOpenWaterAt;
            _isWaterSlopeCell = isWaterSlopeCell;
            _isDryShoreOrRimMembershipEligible = isDryShoreOrRimMembershipEligible;
            _shouldSkipRoadTerraformSurfaceAt = shouldSkipRoadTerraformSurfaceAt;
            _getTerrainSlopeTypeAt = getTerrainSlopeTypeAt;
            _smooth = smooth;
            _expandCutThroughAdjacentByOneStep = expandCutThroughAdjacentByOneStep;
            _cutThroughMinCellsFromMapEdge = cutThroughMinCellsFromMapEdge;
        }

        /// <summary>Expand diagonal steps to cardinal. Public static — usable by RoadManager.</summary>
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

        internal (int baseHeight, bool pathCrossesHill, int maxHeight) ComputePathBaseHeightAndCutThrough(IList<Vector2> path)
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

        internal bool HasConsecutiveHeightDiffGreaterThanOne(IList<Vector2> path, HeightMap heightMap)
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
        public PathTerraformPlan ComputePathPlan(IList<Vector2> path, bool waterBridgeTerraformRelaxation = false)
        {
            var plan = new PathTerraformPlan();
            plan.waterBridgeTerraformRelaxation = waterBridgeTerraformRelaxation;
            var heightMap = _getHeightMap?.Invoke();
            if (heightMap == null || path == null || path.Count == 0) return plan;

            if (path.Count >= 2)
                path = ExpandDiagonalStepsToCardinal(path);

            plan.waterBridgeDeckDisplayHeight = 0;
            if (waterBridgeTerraformRelaxation)
                _smooth.TryAssignWaterBridgeDeckDisplayHeight(plan, path, heightMap);

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

                    var cellPlan = new PathTerraformPlan.CellPlan
                    {
                        position = new Vector2Int(x, y),
                        action = TerraformAction.Flatten,
                        direction = OrthogonalDirection.North,
                        originalHeight = h,
                        targetHeight = plan.baseHeight,
                        postTerraformSlopeType = TerrainSlopeType.Flat
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
                    _smooth.ExpandAdjacentFlattenCellsRecursively(plan, path, heightMap, _expandCutThroughAdjacentByOneStep);
                if (plan.isValid && _cutThroughMinCellsFromMapEdge > 0 && !_smooth.CutThroughHasAcceptableMapMargin(plan, path, heightMap, _cutThroughMinCellsFromMapEdge))
                    plan.isValid = false;
                return plan;
            }

            bool preferSlopeClimb = !HasConsecutiveHeightDiffGreaterThanOne(path, heightMap);

            for (int i = 0; i < path.Count; i++)
            {
                int x = (int)path[i].x;
                int y = (int)path[i].y;
                int h = heightMap.IsValidPosition(x, y) ? heightMap.GetHeight(x, y) : HeightMap.MIN_HEIGHT;

                var cellPlan = new PathTerraformPlan.CellPlan
                {
                    position = new Vector2Int(x, y),
                    action = TerraformAction.None,
                    direction = OrthogonalDirection.North,
                    originalHeight = h,
                    targetHeight = h,
                    postTerraformSlopeType = TerrainSlopeType.Flat
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

                TerrainSlopeType slopeType = _getTerrainSlopeTypeAt(x, y);

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
                    _smooth.AddAdjacentFlattenCells(plan, path, heightMap, x, y, plan.baseHeight, h, _isRegisteredOpenWaterAt);
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
                    _smooth.AddAdjacentFlattenCells(plan, path, heightMap, x, y, plan.baseHeight, h, _isRegisteredOpenWaterAt);
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
                            cellPlan.postTerraformSlopeType = TerrainSlopeType.Flat;
                            _smooth.AddAdjacentFlattenCells(plan, path, heightMap, x, y, plan.baseHeight, h, _isRegisteredOpenWaterAt);
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
                _smooth.InvalidatePlanIfPathBesideSteepLandCliff(plan, path, heightMap, preferSlopeClimb, _isRegisteredOpenWaterAt);

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
                _smooth.ExpandAdjacentFlattenCellsRecursively(plan, path, heightMap, _expandCutThroughAdjacentByOneStep);

            return plan;
        }

        internal bool IsCoastal(int x, int y)
        {
            return _isRegisteredOpenWaterAt(x, y) || _isWaterSlopeCell(x, y) || _isDryShoreOrRimMembershipEligible(x, y);
        }

        bool PathEdgeExemptDryDryForWaterBridgeRelaxation(IList<Vector2> path, int edgeStartIdx, HeightMap heightMap, bool waterBridgeTerraformRelaxation)
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

        int ComputeSegmentDeltaHForPostSlope(HeightMap heightMap, IList<Vector2> path, int i, int hLandAtCell)
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

        static TerrainSlopeType GetSlopeTypeFromTravelVector(int dx, int dy)
        {
            if (Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0)
                return dx > 0 ? TerrainSlopeType.North : TerrainSlopeType.South;
            if (dy != 0)
                return dy > 0 ? TerrainSlopeType.West : TerrainSlopeType.East;
            return TerrainSlopeType.Flat;
        }

        TerrainSlopeType GetPostTerraformSlopeTypeAlongExit(HeightMap heightMap, IList<Vector2> path, int i, int hLand, int dxOut, int dyOut)
        {
            int dSeg = ComputeSegmentDeltaHForPostSlope(heightMap, path, i, hLand);
            if (dSeg > 0) return GetSlopeTypeFromTravelVector(-dxOut, -dyOut);
            return GetSlopeTypeFromTravelVector(dxOut, dyOut);
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

        internal static int GetNeighborHeight(HeightMap heightMap, int nx, int ny)
        {
            if (!heightMap.IsValidPosition(nx, ny)) return -1;
            return heightMap.GetHeight(nx, ny);
        }
    }
}
