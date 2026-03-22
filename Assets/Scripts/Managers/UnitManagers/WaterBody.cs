using System.Collections.Generic;

namespace Territory.Terrain
{
    /// <summary>
    /// A connected water mass (e.g. lake) with a single flat surface height shared by all cells.
    /// Terrain floor is stored in <see cref="HeightMap"/>; depth is implicit (surface - terrain).
    /// </summary>
    public sealed class WaterBody
    {
        public WaterBody(int id, int surfaceHeight)
        {
            Id = id;
            SurfaceHeight = surfaceHeight;
        }

        public int Id { get; }

        public int SurfaceHeight { get; set; }

        /// <summary>Flattened cell indices: x + y * width.</summary>
        public HashSet<int> CellIndices { get; } = new HashSet<int>();

        public int CellCount => CellIndices.Count;

        public void AddCellIndex(int flatIndex)
        {
            CellIndices.Add(flatIndex);
        }

        public void RemoveCellIndex(int flatIndex)
        {
            CellIndices.Remove(flatIndex);
        }
    }
}
