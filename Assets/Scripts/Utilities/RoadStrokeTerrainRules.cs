using System.Collections.Generic;
using UnityEngine;
using Territory.Terrain;

namespace Territory.Utilities
{
    /// <summary>
    /// Land slope eligibility for road strokes (manual, AUTO, interstate). Diagonal + corner-up terrain
    /// disallowed → paths truncate or pathfinding must not step onto those cells.
    /// </summary>
    public static class RoadStrokeTerrainRules
    {
        /// <summary>
        /// Allowed: <see cref="TerrainSlopeType.Flat"/> + cardinal ramps (N/S/E/W). All other land slope types disallowed.
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
        /// Empty list if first cell disallowed or <paramref name="terrainManager"/> null.
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
