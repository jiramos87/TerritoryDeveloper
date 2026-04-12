using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Territory.Utilities.Compute;

namespace Territory.Tests.EditMode
{
    /// <summary>
    /// Top-k sampling over synthetic score fields.
    /// </summary>
    public class DesirabilityFieldSamplerTests
    {
        [Test]
        public void TopK_MatchesBruteForceOrderAndTieBreak()
        {
            int w = 5;
            int h = 4;
            var scores = new float[w * h];
            var rng = new System.Random(12345);
            for (int i = 0; i < scores.Length; i++)
                scores[i] = (float)rng.NextDouble();

            // Force a tie: two cells share max score
            scores[7] = 99f;
            scores[18] = 99f;

            int k = 8;
            var expected = Enumerable.Range(0, scores.Length)
                .OrderByDescending(i => scores[i])
                .ThenBy(i => i)
                .Take(k)
                .ToList();

            var got = new List<int>();
            DesirabilityFieldSampler.TryGetTopKCellIndicesByScore(scores, w, h, k, got);
            CollectionAssert.AreEqual(expected, got);
        }

        [Test]
        public void TopK_ClampedToFieldSize()
        {
            float[] scores = { 3f, 1f, 2f };
            var got = new List<int>();
            DesirabilityFieldSampler.TryGetTopKCellIndicesByScore(scores, 3, 1, 100, got);
            Assert.AreEqual(3, got.Count);
            Assert.AreEqual(0, got[0]);
            Assert.AreEqual(2, got[1]);
            Assert.AreEqual(1, got[2]);
        }
    }
}
