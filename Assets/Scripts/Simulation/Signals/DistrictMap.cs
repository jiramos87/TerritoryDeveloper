using System;
using Territory.Core;
using Territory.Simulation;

namespace Territory.Simulation.Signals
{
    /// <summary>Per-cell district id grid derived from <c>UrbanCentroidService.GetUrbanRing</c> ordinal (Inner=0/Mid=1/Outer=2/Rural=3). Read by <c>DistrictAggregator</c> for per-district signal rollups. See <c>ia/specs/simulation-signals.md</c> District layer.</summary>
    public class DistrictMap
    {
        /// <summary>Number of distinct districts. Matches <c>UrbanRing</c> enum cardinality (Inner/Mid/Outer/Rural).</summary>
        public const int DistrictCount = 4;

        private int[,] districtIds;

        public int Width { get; }
        public int Height { get; }

        public DistrictMap(int width, int height)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentOutOfRangeException("width/height", "DistrictMap dims must be non-negative.");
            }
            Width = width;
            Height = height;
            districtIds = new int[width, height];
        }

        /// <summary>Repopulate every cell from <c>centroid.GetUrbanRing(Vector2(x,y))</c> ordinal. Reuses backing buffer (no realloc).</summary>
        public void Rebuild(UrbanCentroidService centroid, GridManager grid)
        {
            if (centroid == null || grid == null)
            {
                return;
            }
            int w = grid.width;
            int h = grid.height;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    districtIds[x, y] = (int)centroid.GetUrbanRing(new UnityEngine.Vector2(x, y));
                }
            }
        }

        /// <summary>Read per-cell district id. Out-of-range args throw <c>IndexOutOfRangeException</c> (default <c>int[,]</c> indexer behaviour).</summary>
        public int GetDistrictId(int x, int y)
        {
            return districtIds[x, y];
        }

        /// <summary>Internal write API for tests + custom population paths.</summary>
        public void SetDistrictId(int x, int y, int districtId)
        {
            districtIds[x, y] = districtId;
        }

        /// <summary>Row-major flatten matching <c>WaterMapData</c> convention (<c>flat[x + y * Width]</c>).</summary>
        public DistrictMapData GetSerializableData()
        {
            var data = new DistrictMapData
            {
                width = Width,
                height = Height,
                districtIdsFlat = new int[Width * Height],
            };
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    data.districtIdsFlat[x + y * Width] = districtIds[x, y];
                }
            }
            return data;
        }

        /// <summary>Reproduce per-cell ids byte-identical to source (mirror of <see cref="GetSerializableData"/>).</summary>
        public void RestoreFromSerializableData(DistrictMapData data)
        {
            if (data == null || data.districtIdsFlat == null)
            {
                return;
            }
            int w = data.width;
            int h = data.height;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    districtIds[x, y] = data.districtIdsFlat[x + y * w];
                }
            }
        }
    }

    /// <summary>JSON-serializable POCO mirror of <see cref="DistrictMap"/>. Row-major flatten matches <c>WaterMapData</c> convention.</summary>
    [Serializable]
    public class DistrictMapData
    {
        public int width;
        public int height;
        public int[] districtIdsFlat;
    }
}
