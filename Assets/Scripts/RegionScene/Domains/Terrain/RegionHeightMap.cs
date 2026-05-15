using UnityEngine;
using Territory.IsoSceneCore.Contracts;

namespace Territory.RegionScene.Terrain
{
    /// <summary>Per-cell elevation int array for 64x64 region grid. Implements IIsoHeightMap. Procedural seed via Perlin noise (prototype only).</summary>
    public class RegionHeightMap : IIsoHeightMap
    {
        public const int RegionGridSize = 64;
        public const int MinHeight = 0;
        public const int MaxHeight = 5;

        private readonly int[,] _height = new int[RegionGridSize, RegionGridSize];

        public int GridSize => RegionGridSize;

        /// <summary>Return elevation at cell. Out-of-range → 0 (invariant #1 parity).</summary>
        public int HeightAt(int x, int y)
        {
            if (x < 0 || x >= RegionGridSize || y < 0 || y >= RegionGridSize) return 0;
            return _height[x, y];
        }

        /// <summary>Flat-grass prototype: every cell elevation = 1. Procedural noise generation
        /// + region-specific terrain logic land post-prototype (see plan top-of-doc reminder).</summary>
        public void Seed(int deterministicSeed)
        {
            for (int x = 0; x < RegionGridSize; x++)
            {
                for (int y = 0; y < RegionGridSize; y++)
                {
                    _height[x, y] = 1;
                }
            }
        }

        public void SetHeight(int x, int y, int value)
        {
            if (x < 0 || x >= RegionGridSize || y < 0 || y >= RegionGridSize) return;
            _height[x, y] = Mathf.Clamp(value, MinHeight, MaxHeight);
        }
    }
}
