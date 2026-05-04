// BUG-61 W4 — bond data layer hidden behind feature flag (default OFF) for MVP.
// Define BONDS_ENABLED in Player Settings → Scripting Define Symbols to re-enable post-MVP.
#if BONDS_ENABLED
using System;

namespace Territory.Economy
{
    /// <summary>
    /// Persisted state for a single active bond on one scale tier.
    /// Proactive injection lever — not remedial overdraft.
    /// Added schema 4 (Stage 4 — bond ledger).
    /// </summary>
    [Serializable]
    public class BondData
    {
        /// <summary>Scale tier this bond is issued against (one active bond per tier).</summary>
        public int scaleTier;

        /// <summary>Original principal amount credited to treasury on issuance.</summary>
        public int principal;

        /// <summary>Total term in months at issuance.</summary>
        public int termMonths;

        /// <summary>Fixed monthly repayment amount = (principal × (1 + fixedInterestRate)) / termMonths.</summary>
        public int monthlyRepayment;

        /// <summary>Interest rate locked at issuance (e.g. 0.12 = 12%).</summary>
        public float fixedInterestRate;

        /// <summary>In-game date string when bond was issued.</summary>
        public string issuedOnDate;

        /// <summary>Remaining months until bond clears from registry.</summary>
        public int monthsRemaining;

        /// <summary>True when a monthly repayment failed due to insufficient treasury balance. HUD flag only — no mechanical penalty per Review Note N6.</summary>
        public bool arrears;
    }
}
#endif
