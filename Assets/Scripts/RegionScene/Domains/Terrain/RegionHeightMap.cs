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

        /// <summary>Procedural Perlin seed. Prototype only — region-specific generation replaces post-prototype.</summary>
        public void Seed(int deterministicSeed)
        {
            float offsetX = deterministicSeed * 0.1f;
            float offsetY = deterministicSeed * 0.17f;
            float scale = 0.12f;
            for (int x = 0; x < RegionGridSize; x++)
            {
                for (int y = 0; y < RegionGridSize; y++)
                {
                    float noise = Mathf.PerlinNoise(x * scale + offsetX, y * scale + offsetY);
                    _height[x, y] = Mathf.Clamp(Mathf.RoundToInt(noise * MaxHeight), MinHeight, MaxHeight);
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
