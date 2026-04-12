using System;
using System.Collections.Generic;

namespace Territory.Utilities.Compute
{
    /// <summary>
    /// Read-only top-<c>k</c> sampling over desirability/score field (row-major <c>x</c> fast). Does not read live grid;
    /// callers pass arrays. Do not wire AUTO zoning here without dedicated FEAT issue.
    /// </summary>
    public static class DesirabilityFieldSampler
    {
        /// <summary>
        /// Fill <paramref name="outIndices"/> with up to <paramref name="k"/> cell indices, row-major order, highest score first.
        /// Ties → lower index. Expects <c>scoresRowMajor.Length == width * height</c>.
        /// </summary>
        public static void TryGetTopKCellIndicesByScore(
            float[] scoresRowMajor,
            int width,
            int height,
            int k,
            List<int> outIndices)
        {
            outIndices.Clear();
            if (scoresRowMajor == null || k <= 0 || width <= 0 || height <= 0)
                return;
            int n = width * height;
            if (scoresRowMajor.Length < n)
                return;

            int take = Math.Min(k, n);
            var order = new int[n];
            for (int i = 0; i < n; i++)
                order[i] = i;
            Array.Sort(order, (a, b) =>
            {
                float sa = scoresRowMajor[a];
                float sb = scoresRowMajor[b];
                int cmp = sb.CompareTo(sa);
                return cmp != 0 ? cmp : a.CompareTo(b);
            });
            for (int i = 0; i < take; i++)
                outIndices.Add(order[i]);
        }
    }
}
