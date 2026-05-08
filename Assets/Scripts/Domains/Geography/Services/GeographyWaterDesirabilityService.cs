using Territory.Core;

namespace Domains.Geography.Services
{
    /// <summary>
    /// Populates closeWaterCount on each CityCell and recalculates desirability.
    /// Extracted from GeographyManager.InitializeWaterDesirability.
    /// Invariant #8: water state never modified — read-only pass over WaterMap.
    /// </summary>
    public static class GeographyWaterDesirabilityService
    {
        private static readonly int[] Dx = { 1, -1, 0, 0 };
        private static readonly int[] Dy = { 0, 0, 1, -1 };

        /// <summary>
        /// Scan all grid cells; set closeWaterCount from 4-directional adjacency; call UpdateDesirability.
        /// </summary>
        /// <param name="width">Grid width.</param>
        /// <param name="height">Grid height.</param>
        /// <param name="getCell">Delegate returning CityCell at (x,y), or null.</param>
        /// <param name="isWaterAt">Delegate returning true when (x,y) is water.</param>
        public static void Apply(
            int width,
            int height,
            System.Func<int, int, CityCell> getCell,
            System.Func<int, int, bool> isWaterAt)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CityCell cell = getCell(x, y);
                    if (cell == null) continue;

                    int count = 0;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + Dx[d];
                        int ny = y + Dy[d];
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height && isWaterAt(nx, ny))
                            count++;
                    }
                    cell.closeWaterCount = count;
                    cell.UpdateDesirability();
                }
            }
        }
    }
}
