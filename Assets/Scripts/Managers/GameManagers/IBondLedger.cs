namespace Territory.Economy
{
    /// <summary>
    /// Contract for the single-bond-per-scale-tier ledger.
    /// Proactive injection only — not remedial overdraft.
    /// Concrete implementation: <see cref="BondLedgerService"/>.
    /// </summary>
    public interface IBondLedger
    {
        /// <summary>
        /// Attempt to issue a bond on <paramref name="scaleTier"/>.
        /// Rejects if an active bond already exists on that tier,
        /// or if <paramref name="principal"/> / <paramref name="termMonths"/> are non-positive.
        /// On success: credits treasury via <see cref="EconomyManager.AddMoney"/>,
        /// records bond with computed monthly repayment.
        /// </summary>
        /// <param name="scaleTier">Scale tier (one active bond per tier).</param>
        /// <param name="principal">Amount credited to treasury. Must be positive.</param>
        /// <param name="termMonths">Repayment term in months. Must be positive.</param>
        /// <returns><c>true</c> if the bond was issued; <c>false</c> otherwise.</returns>
        bool TryIssueBond(int scaleTier, int principal, int termMonths);

        /// <summary>
        /// Returns the active bond on <paramref name="scaleTier"/>, or <c>null</c> if none.
        /// </summary>
        /// <param name="scaleTier">Scale tier to query.</param>
        BondData GetActiveBond(int scaleTier);

        /// <summary>
        /// Process monthly repayment for the bond on <paramref name="scaleTier"/>.
        /// No-op when no active bond exists. Routes repayment through
        /// <see cref="TreasuryFloorClampService.TrySpend"/>; failure flags arrears
        /// (HUD flag only, no mechanical penalty).
        /// </summary>
        /// <param name="scaleTier">Scale tier to process.</param>
        void ProcessMonthlyRepayment(int scaleTier);
    }
}
