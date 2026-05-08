using System.Collections.Generic;

namespace Territory.Terrain
{
    /// <summary>
    /// Connected water mass (lake, sea, river). Single flat surface height shared by all cells.
    /// Terrain floor in <see cref="HeightMap"/>; depth implicit (surface - terrain).
    /// <see cref="Classification"/> fixed at creation. Merges only join bodies with same classification.
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
