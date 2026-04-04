using UnityEngine;

namespace Territory.Utilities.Compute
{
    /// <summary>
    /// Pure isometric grid ↔ world math aligned with <c>GridManager</c> and
    /// <c>isometric-geography-system.md</c> §1.1 / §1.3 (see <c>tools/compute-lib</c> golden tests, TECH-37).
    /// Runtime gameplay remains authoritative on <see cref="Territory.Core.GridManager"/>; use this for shared static helpers only.
    /// </summary>
    public static class IsometricGridMath
    {
        /// <summary>
        /// Planar world → logical cell (matches <see cref="Territory.Core.GridManager.GetGridPosition"/>).
        /// </summary>
        public static Vector2Int WorldToGridPlanar(
            Vector2 world,
            float tileWidth,
            float tileHeight,
            Vector2 origin = default)
        {
            Vector2 w = world - origin;
            float posX = w.x / (tileWidth / 2f);
            float posY = w.y / (tileHeight / 2f);
            int gridX = Mathf.RoundToInt((posY + posX) / 2f);
            int gridY = Mathf.RoundToInt((posY - posX) / 2f);
            return new Vector2Int(gridX, gridY);
        }

        /// <summary>
        /// Grid → world with optional terrain height level (matches <see cref="Territory.Core.GridManager.GetWorldPositionVector"/>).
        /// </summary>
        public static Vector2 GridToWorldPlanar(
            int gridX,
            int gridY,
            float tileWidth,
            float tileHeight,
            int heightLevel = 1,
            Vector2 origin = default)
        {
            float heightOffset = (heightLevel - 1) * (tileHeight / 2f);
            float wx = (gridX - gridY) * (tileWidth / 2f);
            float wy = (gridX + gridY) * (tileHeight / 2f) + heightOffset;
            return new Vector2(wx, wy) + origin;
        }
    }
}
