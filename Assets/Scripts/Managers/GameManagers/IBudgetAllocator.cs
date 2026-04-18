namespace Territory.Economy
{
    /// <summary>
    /// Contract for the budget-allocation primitive.
    /// Sub-type index parameter maps to <see cref="ZoneSubTypeRegistry"/> ordinal.
    /// Concrete implementation: <see cref="BudgetAllocationService"/>.
    /// </summary>
    public interface IBudgetAllocator
    {
        /// <summary>
        /// Attempt to draw <paramref name="amount"/> from the envelope of sub-type
        /// <paramref name="subTypeId"/>.
        /// </summary>
        /// <param name="subTypeId">ZoneSubTypeRegistry ordinal for the requesting sub-type.</param>
        /// <param name="amount">Amount to draw. Must be non-negative.</param>
        /// <returns><c>true</c> if the draw succeeded and was deducted; <c>false</c> otherwise.</returns>
        bool TryDraw(int subTypeId, int amount);

        /// <summary>
        /// Returns the monthly spend envelope for <paramref name="subTypeId"/>.
        /// Value is derived from <see cref="SetEnvelopePct"/> share of the global monthly cap.
        /// </summary>
        /// <param name="subTypeId">ZoneSubTypeRegistry ordinal.</param>
        int GetMonthlyEnvelope(int subTypeId);

        /// <summary>
        /// Set the fractional share of the global monthly cap allocated to <paramref name="subTypeId"/>.
        /// Negative values are clamped to 0 before storage. After the call, all envelope shares are
        /// auto-normalized so their sum equals 1.0 (within 1e-6). If the resulting sum is all-zero,
        /// the call is rejected and prior state is preserved.
        /// </summary>
        /// <param name="subTypeId">ZoneSubTypeRegistry ordinal [0, 6].</param>
        /// <param name="pct">Fractional share (clamped to [0, ∞) before normalization).</param>
        void SetEnvelopePct(int subTypeId, float pct);

        /// <summary>
        /// Batch-set all 7 envelope fractional shares from <paramref name="pcts"/> and normalize.
        /// Requires <paramref name="pcts"/> non-null and length exactly 7. Negative per-slot values
        /// are clamped to 0. If the sum of all slots is all-zero the call is rejected and prior state
        /// is preserved; otherwise all shares are scaled so they sum to 1.0.
        /// </summary>
        /// <param name="pcts">Array of length 7; one fractional share per ZoneSubTypeRegistry ordinal.</param>
        void SetEnvelopePctsBatch(float[] pcts);

        /// <summary>
        /// Reset all per-sub-type remaining envelopes to their month-start values.
        /// Called once per in-game month by the timing subsystem (Phase-2 wiring, TECH-420).
        /// </summary>
        void MonthlyReset();
    }
}
