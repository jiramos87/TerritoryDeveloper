using System.Collections.Generic;
using UnityEngine;
using Territory.Terrain;

namespace Territory.Utilities
{
    /// <summary>
    /// Land slope eligibility for any road stroke (manual, AUTO, interstate). Diagonal and corner-up terrain
    /// is not allowed; paths truncate or pathfinding must not step onto those cells (BUG-51 policy).
    /// </summary>
    public static class RoadStrokeTerrainRules
    {
        /// <summary>
        /// Allowed: <see cref="TerrainSlopeType.Flat"/> and cardinal ramps (N/S/E/W). All other land slope types are disallowed.
        /// </summary>
        public static bool IsLandSlopeAllowedForRoadStroke(TerrainSlopeType slopeType)
        {
            return slopeType == TerrainSlopeType.Flat
                || slopeType == TerrainSlopeType.North
                || slopeType == TerrainSlopeType.South
                || slopeType == TerrainSlopeType.East
                || slopeType == TerrainSlopeType.West;
        }

        /// <summary>
        /// Longest prefix of <paramref name="path"/> whose cells pass <see cref="IsLandSlopeAllowedForRoadStroke"/> at integer grid coords.
        /// Returns empty list if the first cell is disallowed or <paramref name="terrainManager"/> is null.
        /// </summary>
        public static List<Vector2> TruncatePathAtFirstDisallowedLandSlope(IList<Vector2> path, TerrainManager terrainManager)
        {
            var result = new List<Vector2>();
            if (path == null || path.Count == 0 || terrainManager == null)
                return result;

            HeightMap heightMap = terrainManager.GetHeightMap();

            for (int i = 0; i < path.Count; i++)
            {
                int x = Mathf.RoundToInt(path[i].x);
                int y = Mathf.RoundToInt(path[i].y);
                if (heightMap != null && heightMap.IsValidPosition(x, y) && heightMap.GetHeight(x, y) < 0)
                {
                    result.Add(path[i]);
                    continue;
                }

                if (terrainManager.IsWaterSlopeCell(x, y))
                {
                    result.Add(path[i]);
                    continue;
                }

                TerrainSlopeType st = terrainManager.GetTerrainSlopeTypeAt(x, y);
                if (!IsLandSlopeAllowedForRoadStroke(st))
                    break;
                result.Add(path[i]);
            }

            return result;
        }
    }
}
