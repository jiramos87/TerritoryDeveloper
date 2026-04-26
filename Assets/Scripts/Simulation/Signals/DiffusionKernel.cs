using UnityEngine;

namespace Territory.Simulation.Signals
{
    /// <summary>Separable Gaussian diffusion + per-step decay over a <see cref="SignalField"/>. Boundary handling clamps source taps to edge per <c>simulation-signals.md</c> §Diffusion physics contract.</summary>
    public static class DiffusionKernel
    {
        /// <summary>Run horizontal Gaussian pass + vertical Gaussian pass + per-cell decay multiply once over <paramref name="field"/>. All writes route through <see cref="SignalField.Set"/> so floor-clamp invariant holds.</summary>
        public static void Apply(SignalField field, SignalMetadataRegistry.Entry meta)
        {
            if (field == null) return;
            if (field.Width == 0 || field.Height == 0) return;

            float sigmaX = meta.diffusionRadius * meta.anisotropy.x;
            float sigmaY = meta.diffusionRadius * meta.anisotropy.y;

            float[,] source = field.Snapshot();
            float[,] horizontal = new float[field.Width, field.Height];

            HorizontalPass(source, horizontal, field.Width, field.Height, sigmaX);
            VerticalPass(horizontal, field, sigmaY);

            float decayMultiplier = 1f - meta.decayPerStep;
            for (int x = 0; x < field.Width; x++)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    field.Set(x, y, field.Get(x, y) * decayMultiplier);
                }
            }
        }

        private static void HorizontalPass(float[,] source, float[,] dest, int width, int height, float sigma)
        {
            if (sigma <= 0f)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        dest[x, y] = source[x, y];
                    }
                }
                return;
            }

            int halfWidth = Mathf.Max(1, Mathf.CeilToInt(3f * sigma));
            float twoSigmaSquared = 2f * sigma * sigma;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float accum = 0f;
                    float weightSum = 0f;
                    for (int k = -halfWidth; k <= halfWidth; k++)
                    {
                        int srcX = Mathf.Clamp(x + k, 0, width - 1);
                        float weight = Mathf.Exp(-(k * k) / twoSigmaSquared);
                        accum += source[srcX, y] * weight;
                        weightSum += weight;
                    }
                    dest[x, y] = weightSum > 0f ? accum / weightSum : source[x, y];
                }
            }
        }

        private static void VerticalPass(float[,] source, SignalField dest, float sigma)
        {
            int width = dest.Width;
            int height = dest.Height;

            if (sigma <= 0f)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        dest.Set(x, y, source[x, y]);
                    }
                }
                return;
            }

            int halfWidth = Mathf.Max(1, Mathf.CeilToInt(3f * sigma));
            float twoSigmaSquared = 2f * sigma * sigma;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float accum = 0f;
                    float weightSum = 0f;
                    for (int k = -halfWidth; k <= halfWidth; k++)
                    {
                        int srcY = Mathf.Clamp(y + k, 0, height - 1);
                        float weight = Mathf.Exp(-(k * k) / twoSigmaSquared);
                        accum += source[x, srcY] * weight;
                        weightSum += weight;
                    }
                    dest.Set(x, y, weightSum > 0f ? accum / weightSum : source[x, y]);
                }
            }
        }
    }
}
