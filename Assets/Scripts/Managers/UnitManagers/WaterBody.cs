using System.Collections.Generic;

namespace Territory.Terrain
{
    /// <summary>
    /// A connected water mass (lake, sea, or river) with a single flat surface height shared by all cells.
    /// Terrain floor is stored in <see cref="HeightMap"/>; depth is implicit (surface - terrain).
    /// <see cref="Classification"/> is fixed at creation; merges only join bodies with the same classification (FEAT-38).
    /// </summary>
    public sealed class WaterBody
    {
        public WaterBody(int id, int surfaceHeight, WaterBodyType classification)
        {
            Id = id;
            SurfaceHeight = surfaceHeight;
            Classification = classification;
        }

        public int Id { get; }

        public int SurfaceHeight { get; set; }

        /// <summary>Lake, Sea, or River — used so <see cref="WaterMap"/> does not merge rivers into lakes.</summary>
        public WaterBodyType Classification { get; }

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
