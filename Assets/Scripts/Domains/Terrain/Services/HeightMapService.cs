using UnityEngine;
using System.Collections.Generic;
using Territory.Terrain;

namespace Domains.Terrain.Services
{
    /// <summary>
    /// Pure heightmap generation logic extracted from TerrainManager for testability.
    /// No MonoBehaviour dependency — takes HeightMap + grid dims as constructor params.
    /// Extracted from Territory.Terrain.TerrainManager per Strategy γ atomization (TECH-23775).
    /// Invariant #1: HeightMap/Cell sync remains in TerrainManager.ApplyHeightMapToGrid.
    /// </summary>
    public class HeightMapService
    {
        /// <summary>Minimum terrain height. Matches TerrainManager.MIN_HEIGHT.</summary>
        public const int MIN_HEIGHT = 0;
        /// <summary>Maximum terrain height. Matches TerrainManager.MAX_HEIGHT.</summary>
        public const int MAX_HEIGHT = 5;

        private const int OriginalMapSize = 40;
        private const float ExtendedPerlinScaleCoarse = 58f;
        private const float ExtendedPerlinScaleFine = 38f;
        private const float ExtendedPerlinCoarseWeight = 0.72f;
        private const float ExtendedNoiseRemapLow = 0.32f;
        private const float ExtendedNoiseRemapRange = 0.58f;
        private const int BorderBlendWidth = 16;
        private const int ExtendedTerrainSmoothPasses = 2;
        private const float ExtendedMicroLakeNoiseScale = 9f;

        private readonly int _width;
        private readonly int _height;

        public HeightMapService(int width, int height)
        {
            _width = width;
            _height = height;
        }

        /// <summary>
        /// Populate <paramref name="heightMap"/> with initial heights.
        /// Flat mode fills uniformly; 40×40 uses template; larger maps use procedural extension.
        /// </summary>
        public void LoadInitialHeightMap(HeightMap heightMap, bool flatEnabled, int flatHeight, int terrainSeed, float microLakeNoiseSalt, float microLakeCarveThreshold)
        {
            if (flatEnabled)
            {
                int uniform = Mathf.Clamp(flatHeight, MIN_HEIGHT, MAX_HEIGHT);
                for (int x = 0; x < _width; x++)
                    for (int y = 0; y < _height; y++)
                        heightMap.SetHeight(x, y, uniform);
                return;
            }

            if (_width == OriginalMapSize && _height == OriginalMapSize)
            {
                heightMap.SetHeights(GetOriginal40x40Heights());
                return;
            }

            int[,] extended = new int[_width, _height];
            int[,] template = GetOriginal40x40Heights();
            int ox = Mathf.Max(0, (_width - OriginalMapSize) / 2);
            int oy = Mathf.Max(0, (_height - OriginalMapSize) / 2);

            for (int tx = 0; tx < OriginalMapSize && ox + tx < _width; tx++)
                for (int ty = 0; ty < OriginalMapSize && oy + ty < _height; ty++)
                    extended[ox + tx, oy + ty] = template[ty, tx];

            FillExtendedTerrainProcedural(extended, ox, oy, terrainSeed);
            ApplyExtendedMicroLakeRoughness(extended, ox, oy, microLakeNoiseSalt, microLakeCarveThreshold);
            heightMap.SetHeights(extended);
        }

        /// <summary>
        /// Fill cells outside centered 40×40 template with low-freq Perlin terrain, layered plateaus, 3×3 smoothing.
        /// </summary>
        private void FillExtendedTerrainProcedural(int[,] heights, int ox, int oy, int terrainSeed)
        {
            float offsetX = terrainSeed * 0.1f;
            float offsetY = terrainSeed * 0.27f;
            int[,] template = GetOriginal40x40Heights();
            float fineWeight = 1f - ExtendedPerlinCoarseWeight;
            int txMax = ox + OriginalMapSize;
            int tyMax = oy + OriginalMapSize;

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (x >= ox && x < txMax && y >= oy && y < tyMax)
                        continue;

                    float n1 = Mathf.PerlinNoise((x + offsetX) / ExtendedPerlinScaleCoarse, (y + offsetY) / ExtendedPerlinScaleCoarse);
                    float n2 = Mathf.PerlinNoise((x + offsetX + 100f) / ExtendedPerlinScaleFine, (y + offsetY + 100f) / ExtendedPerlinScaleFine);
                    float n = ExtendedPerlinCoarseWeight * n1 + fineWeight * n2;
                    n = Mathf.Clamp01(ExtendedNoiseRemapLow + ExtendedNoiseRemapRange * n);
                    int perlinHeight = PerlinToHeightExtended(n);

                    TryGetTemplateBorderBlend(x, y, ox, oy, template, perlinHeight, out int edgeHeight, out float blend);
                    int finalHeight = blend >= 1f ? perlinHeight : Mathf.RoundToInt(edgeHeight * (1f - blend) + perlinHeight * blend);
                    heights[x, y] = Mathf.Clamp(finalHeight, 1, MAX_HEIGHT);
                }
            }

            SmoothExtendedTerrainHeights(heights, ox, oy, ExtendedTerrainSmoothPasses);
        }

        /// <summary>
        /// Sparse fine-scale height dips outside template → depression-fill finds valid lake seeds on extended terrain.
        /// </summary>
        private void ApplyExtendedMicroLakeRoughness(int[,] heights, int ox, int oy, float microLakeNoiseSalt, float carveThreshold)
        {
            float offX = microLakeNoiseSalt * 0.031f;
            float offY = microLakeNoiseSalt * 0.019f;
            int txMax = ox + OriginalMapSize;
            int tyMax = oy + OriginalMapSize;
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (x >= ox && x < txMax && y >= oy && y < tyMax)
                        continue;
                    float noise = Mathf.PerlinNoise((x + offX) / ExtendedMicroLakeNoiseScale, (y + offY) / ExtendedMicroLakeNoiseScale);
                    if (noise < carveThreshold && heights[x, y] > MIN_HEIGHT + 1)
                        heights[x, y] = Mathf.Max(MIN_HEIGHT + 1, heights[x, y] - 1);
                }
            }
        }

        /// <summary>Blend procedural height toward centered template along 4 sides + 4 corner bands.</summary>
        private static void TryGetTemplateBorderBlend(int x, int y, int ox, int oy, int[,] template, int perlinHeight, out int edgeHeight, out float blend)
        {
            edgeHeight = perlinHeight;
            blend = 1f;
            int bw = BorderBlendWidth;
            int tx1 = ox + OriginalMapSize - 1;
            int ty0 = oy;
            int ty1 = oy + OriginalMapSize - 1;

            float bestBlend = 1f;
            int bestEdge = perlinHeight;

            void Consider(float b, int eh)
            {
                if (b < bestBlend) { bestBlend = b; bestEdge = eh; }
            }

            // Corner bands
            if (x >= tx1 + 1 && x < tx1 + 1 + bw && y >= ty1 + 1 && y < ty1 + 1 + bw)
            {
                float bx = (x - (tx1 + 1)) / (float)bw;
                float by = (y - (ty1 + 1)) / (float)bw;
                Consider(Mathf.Max(bx, by), template[OriginalMapSize - 1, OriginalMapSize - 1]);
            }
            if (x >= tx1 + 1 && x < tx1 + 1 + bw && y >= ty0 - bw && y < ty0)
            {
                float bx = (x - (tx1 + 1)) / (float)bw;
                float by = (ty0 - 1 - y) / (float)bw;
                Consider(Mathf.Max(bx, by), template[0, OriginalMapSize - 1]);
            }
            if (x >= ox - bw && x < ox && y >= ty1 + 1 && y < ty1 + 1 + bw)
            {
                float bx = (ox - 1 - x) / (float)bw;
                float by = (y - (ty1 + 1)) / (float)bw;
                Consider(Mathf.Max(bx, by), template[OriginalMapSize - 1, 0]);
            }
            if (x >= ox - bw && x < ox && y >= ty0 - bw && y < ty0)
            {
                float bx = (ox - 1 - x) / (float)bw;
                float by = (ty0 - 1 - y) / (float)bw;
                Consider(Mathf.Max(bx, by), template[0, 0]);
            }

            // Edge bands
            if (x >= tx1 + 1 && x < tx1 + 1 + bw && y >= ty0 && y <= ty1)
            {
                float b = (x - (tx1 + 1)) / (float)bw;
                int ty = Mathf.Clamp(y - oy, 0, OriginalMapSize - 1);
                Consider(b, template[ty, OriginalMapSize - 1]);
            }
            if (x >= ox - bw && x < ox && y >= ty0 && y <= ty1)
            {
                float b = (ox - 1 - x) / (float)bw;
                int ty = Mathf.Clamp(y - oy, 0, OriginalMapSize - 1);
                Consider(b, template[ty, 0]);
            }
            if (y >= ty1 + 1 && y < ty1 + 1 + bw && x >= ox && x <= tx1)
            {
                float b = (y - (ty1 + 1)) / (float)bw;
                int tx = Mathf.Clamp(x - ox, 0, OriginalMapSize - 1);
                Consider(b, template[OriginalMapSize - 1, tx]);
            }
            if (y >= ty0 - bw && y < ty0 && x >= ox && x <= tx1)
            {
                float b = (ty0 - 1 - y) / (float)bw;
                int tx = Mathf.Clamp(x - ox, 0, OriginalMapSize - 1);
                Consider(b, template[0, tx]);
            }

            edgeHeight = bestEdge;
            blend = bestBlend;
        }

        /// <summary>3×3 averaging smooth over cells outside template origin.</summary>
        private void SmoothExtendedTerrainHeights(int[,] heights, int ox, int oy, int passes)
        {
            int txMax = ox + OriginalMapSize;
            int tyMax = oy + OriginalMapSize;
            for (int p = 0; p < passes; p++)
            {
                int[,] src = (int[,])heights.Clone();
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        if (x >= ox && x < txMax && y >= oy && y < tyMax)
                            continue;
                        int sum = 0;
                        int count = 0;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;
                                if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;
                                sum += src[nx, ny];
                                count++;
                            }
                        }
                        if (count > 0)
                            heights[x, y] = Mathf.Clamp(Mathf.RoundToInt((float)sum / count), 1, MAX_HEIGHT);
                    }
                }
            }
        }

        /// <summary>Map Perlin [0,1] to land height 1–5: favor wide plains + mid plateaus; fewer peaks.</summary>
        private static int PerlinToHeightExtended(float n)
        {
            if (n < 0.28f) return 1;
            if (n < 0.48f) return 2;
            if (n < 0.66f) return 3;
            if (n < 0.84f) return 4;
            return 5;
        }

        /// <summary>
        /// Shuffle list in-place (Fisher-Yates). Used by lake depression carving.
        /// </summary>
        public static void ShuffleCoordsList(List<Vector2Int> list, System.Random rnd)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                Vector2Int tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        /// <summary>Original 40×40 height map (rows y, cols x).</summary>
        public static int[,] GetOriginal40x40Heights()
        {
            return new int[,] {
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 2, 3, 4, 5, 5, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 2, 2, 3, 4, 5, 4, 3, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 2, 2, 3, 4, 5, 4, 3, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
              {1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
              {2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 3, 3, 3, 3, 3, 3, 3, 2, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {3, 3, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {4, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
              {4, 4, 3, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2},
              {3, 2, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2},
              {2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 4, 4},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 4, 4},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 3, 3, 3, 3, 4},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 3, 2, 2, 3, 4},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 3, 2, 2, 3, 4},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 3, 3, 3, 3, 4},
              {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 3, 3, 4, 4, 4, 4, 4, 4, 4}
            };
        }
    }
}
