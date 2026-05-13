using System.Collections.Generic;
using UnityEngine;
using Territory.Terrain;
using Territory.Core;

namespace Domains.Terrain.Services
{
    /// <summary>
    /// Adjacent-flatten expansion + water-bridge deck height extracted from TerraformingService (TECH-30056 Stage 7.2 split).
    /// Owns: ExpandAdjacentFlattenCellsRecursively, AddAdjacentFlattenCells, water bridge deck helpers.
    /// HeightMap write order invariant preserved — read-only HeightMap access only.
    /// </summary>
    public class TerraformSmoothService
    {
        private readonly System.Func<HeightMap> _getHeightMap;
        private readonly System.Func<int, int, bool> _isRegisteredOpenWaterAt;
        private readonly System.Func<int, int, bool> _isWaterSlopeCell;
        private readonly System.Func<int, int, CityCell> _getCell;
        private readonly System.Func<int, int, bool> _isWaterAt;

        public TerraformSmoothService(
            System.Func<HeightMap> getHeightMap,
            System.Func<int, int, bool> isRegisteredOpenWaterAt,
            System.Func<int, int, bool> isWaterSlopeCell,
            System.Func<int, int, CityCell> getCell,
            System.Func<int, int, bool> isWaterAt = null)
        {
            _getHeightMap = getHeightMap;
            _isRegisteredOpenWaterAt = isRegisteredOpenWaterAt;
            _isWaterSlopeCell = isWaterSlopeCell;
            _getCell = getCell;
            _isWaterAt = isWaterAt;
        }

        internal void ExpandAdjacentFlattenCellsRecursively(PathTerraformPlan plan, IList<Vector2> path, HeightMap heightMap, bool expandCutThroughAdjacentByOneStep)
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

        internal void AddAdjacentFlattenCells(PathTerraformPlan plan, IList<Vector2> path, HeightMap heightMap, int x, int y, int baseHeight, int pathCellHeight, System.Func<int, int, bool> isRegisteredOpenWaterAt)
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
                if (isRegisteredOpenWaterAt(nx, ny) || nh == baseHeight) continue;
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

        internal void TryAssignWaterBridgeDeckDisplayHeight(PathTerraformPlan plan, IList<Vector2> path, HeightMap heightMap)
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

        internal bool IsWaterOrWaterSlopeForBridgeDeckHeight(int x, int y, HeightMap heightMap)
        {
            if (heightMap == null || !heightMap.IsValidPosition(x, y)) return false;
            if (_isRegisteredOpenWaterAt(x, y)) return true;
            return _isWaterSlopeCell(x, y);
        }

        bool CellQualifiesForDeckDisplayLipRelaxed(int x, int y, int h, HeightMap heightMap)
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

        internal void InvalidatePlanIfPathBesideSteepLandCliff(PathTerraformPlan plan, IList<Vector2> path, HeightMap heightMap, bool preferSlopeClimb, System.Func<int, int, bool> isRegisteredOpenWaterAt)
        {
            if (!preferSlopeClimb || plan == null || heightMap == null || path == null || !plan.isValid) return;
            var pathSet = new HashSet<Vector2Int>();
            for (int i = 0; i < path.Count; i++)
                pathSet.Add(new Vector2Int((int)path[i].x, (int)path[i].y));
            int[] cdx = { 1, -1, 0, 0 };
            int[] cdy = { 0, 0, 1, -1 };
            for (int i = 0; i < path.Count; i++)
            {
                int x = (int)path[i].x, y = (int)path[i].y;
                if (!heightMap.IsValidPosition(x, y)) continue;
                if (isRegisteredOpenWaterAt(x, y)) continue;
                int h = heightMap.GetHeight(x, y);
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + cdx[d], ny = y + cdy[d];
                    if (!heightMap.IsValidPosition(nx, ny)) continue;
                    if (pathSet.Contains(new Vector2Int(nx, ny))) continue;
                    if (isRegisteredOpenWaterAt(nx, ny)) continue;
                    int nh = heightMap.GetHeight(nx, ny);
                    if (Mathf.Abs(nh - h) > 1) { plan.isValid = false; return; }
                }
            }
        }

        internal bool CutThroughHasAcceptableMapMargin(PathTerraformPlan plan, IList<Vector2> path, HeightMap heightMap, int cutThroughMinCellsFromMapEdge)
        {
            int m = cutThroughMinCellsFromMapEdge;
            if (m <= 0 || heightMap == null || plan == null || path == null) return true;
            int w = heightMap.Width, h = heightMap.Height;
            if (w <= m * 2 || h <= m * 2) return true;
            bool Inside(int x, int y) => x >= m && y >= m && x < w - m && y < h - m;
            for (int i = 0; i < path.Count; i++)
            {
                int x = (int)path[i].x, y = (int)path[i].y;
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

        /// <summary>
        /// Builds a PathTerraformPlan with no terraform height mutations (all TerraformAction.None),
        /// water-bridge relaxation on, and waterBridgeDeckDisplayHeight assigned.
        /// expandedCardinalPath must already be cardinal.
        /// </summary>
        public bool TryBuildDeckSpanOnlyWaterBridgePlan(IList<Vector2> expandedCardinalPath, System.Func<int, int, bool> isRegisteredOpenWaterAt, System.Func<int, int, bool> isWaterSlopeCell, System.Func<int, int, TerrainSlopeType> getTerrainSlopeTypeAt, out PathTerraformPlan plan)
        {
            plan = null;
            var heightMap = _getHeightMap?.Invoke();
            if (heightMap == null || expandedCardinalPath == null || expandedCardinalPath.Count == 0 || _getCell == null)
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
                if (!heightMap.IsValidPosition(x, y) || _getCell(x, y) == null)
                {
                    plan.isValid = false;
                    plan = null;
                    return false;
                }

                int h = heightMap.GetHeight(x, y);
                TerrainSlopeType slope = TerrainSlopeType.Flat;
                if (!isRegisteredOpenWaterAt(x, y) && !isWaterSlopeCell(x, y))
                    slope = getTerrainSlopeTypeAt(x, y);

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
    }
}
