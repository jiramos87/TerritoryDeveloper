using UnityEngine;
using Territory.Terrain;

namespace Domains.Terrain.Services
{
    /// <summary>
    /// Terraform apply + revert extracted from TerraformingService (TECH-30056 Stage 7.2 split).
    /// Owns: ApplyTerraform, RevertTerraform, ComputeNewHeight.
    /// HeightMap write order invariant preserved — SetHeight then restoreTerrainForCell.
    /// </summary>
    public class TerraformApplyService
    {
        private readonly System.Func<HeightMap> _getHeightMap;
        private readonly System.Action<int, int> _restoreTerrainForCell;

        public TerraformApplyService(
            System.Func<HeightMap> getHeightMap,
            System.Action<int, int> restoreTerrainForCell = null)
        {
            _getHeightMap = getHeightMap;
            _restoreTerrainForCell = restoreTerrainForCell;
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
