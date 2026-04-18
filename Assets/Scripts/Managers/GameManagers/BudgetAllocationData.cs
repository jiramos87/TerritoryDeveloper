using System;

namespace Territory.Economy
{
    /// <summary>
    /// Persisted snapshot of <see cref="BudgetAllocationService"/> state (envelope pct + cap + remaining).
    /// Added schema 4 (Stage 1.3 Phase 3 — see <c>BACKLOG-ARCHIVE.md</c> TECH-422).
    /// </summary>
    [Serializable]
    public class BudgetAllocationData
    {
        /// <summary>Per-envelope fractional share (length 7, sums to 1.0).</summary>
        public float[] envelopePct;

        /// <summary>Global monthly spend cap in currency units.</summary>
        public int globalMonthlyCap;

        /// <summary>Remaining budget per envelope for the current month (length 7).</summary>
        public int[] currentMonthRemaining;

        /// <summary>
        /// Seed a default <see cref="BudgetAllocationData"/> with equal split across 7 envelopes.
        /// Normalizes in-place so <c>sum(envelopePct) == 1.0</c>.
        /// </summary>
        /// <param name="cap">Global monthly cap to distribute.</param>
        public static BudgetAllocationData Default(int cap)
        {
            const int Count = 7;
            var d = new BudgetAllocationData();
            d.globalMonthlyCap = cap;
            d.envelopePct = new float[Count];
            for (int i = 0; i < Count; i++)
                d.envelopePct[i] = 1f / Count;

            // Normalize in-place — guard against degenerate sum.
            float sum = 0f;
            for (int i = 0; i < Count; i++)
                sum += d.envelopePct[i];
            if (sum < 1e-9f)
            {
                UnityEngine.Debug.LogWarning("[BudgetAllocationData] Default: sum < epsilon — falling back to uniform distribution.");
                float uniform = 1f / Count;
                for (int i = 0; i < Count; i++)
                    d.envelopePct[i] = uniform;
                sum = 1f;
            }
            else
            {
                for (int i = 0; i < Count; i++)
                    d.envelopePct[i] /= sum;
            }

            d.currentMonthRemaining = new int[Count];
            for (int i = 0; i < Count; i++)
                d.currentMonthRemaining[i] = (int)(cap * d.envelopePct[i]);

            return d;
        }
    }
}
