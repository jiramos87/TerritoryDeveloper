using System.Collections.Generic;
using UnityEngine;

namespace Territory.Economy
{
    /// <summary>
    /// Helper service implementing <see cref="IBondLedger"/>.
    /// Single-bond-per-scale-tier ledger — proactive injection, not remedial overdraft.
    /// Extracted per invariant #6 (helper carve-out): holds composition references to
    /// <see cref="EconomyManager"/> and <see cref="TreasuryFloorClampService"/>.
    /// Wired via Inspector; <see cref="Awake"/> falls back to <c>FindObjectOfType</c> (guardrail #1,
    /// invariant #3 — no per-frame lookups).
    /// </summary>
    public class BondLedgerService : MonoBehaviour, IBondLedger
    {
        [SerializeField] private EconomyManager economy;
        [SerializeField] private TreasuryFloorClampService treasuryFloor;

        /// <summary>Fixed annual interest rate applied at issuance. Default 12%.</summary>
        [SerializeField] private float fixedInterestRate = 0.12f;

        /// <summary>Active bonds keyed by scale tier. One bond per tier max.</summary>
        private Dictionary<int, BondData> active = new Dictionary<int, BondData>();

        private void Awake()
        {
            if (economy == null)
                economy = FindObjectOfType<EconomyManager>();
            if (economy == null)
                Debug.LogError("BondLedgerService: EconomyManager not found. Assign via Inspector.");

            if (treasuryFloor == null)
                treasuryFloor = FindObjectOfType<TreasuryFloorClampService>();
            if (treasuryFloor == null)
                Debug.LogError("BondLedgerService: TreasuryFloorClampService not found. Assign via Inspector.");
        }

        /// <inheritdoc/>
        public bool TryIssueBond(int scaleTier, int principal, int termMonths)
        {
            if (principal <= 0 || termMonths <= 0)
                return false;

            if (active.ContainsKey(scaleTier))
                return false;

            int monthlyRepayment = (int)((principal * (1f + fixedInterestRate)) / termMonths);

            economy.AddMoney(principal);

            var bond = new BondData
            {
                scaleTier = scaleTier,
                principal = principal,
                termMonths = termMonths,
                monthlyRepayment = monthlyRepayment,
                fixedInterestRate = fixedInterestRate,
                issuedOnDate = economy.timeManager != null
                    ? economy.timeManager.GetCurrentDate().ToString("yyyy-MM-dd")
                    : System.DateTime.Now.ToString("yyyy-MM-dd"),
                monthsRemaining = termMonths,
                arrears = false
            };

            active[scaleTier] = bond;
            return true;
        }

        /// <inheritdoc/>
        public BondData GetActiveBond(int scaleTier)
        {
            active.TryGetValue(scaleTier, out BondData bond);
            return bond;
        }

        /// <inheritdoc/>
        public void ProcessMonthlyRepayment(int scaleTier)
        {
            if (!active.TryGetValue(scaleTier, out BondData bond))
                return;

            if (treasuryFloor == null)
            {
                Debug.LogError("BondLedgerService.ProcessMonthlyRepayment: TreasuryFloorClampService is null.");
                return;
            }

            bool paid = treasuryFloor.TrySpend(bond.monthlyRepayment, "Bond repayment");
            if (!paid)
            {
                bond.arrears = true;
                return;
            }

            bond.arrears = false;
            bond.monthsRemaining--;

            if (bond.monthsRemaining <= 0)
                active.Remove(scaleTier);
        }

        /// <summary>
        /// Process repayments for ALL active bonds. Called from economy tick.
        /// </summary>
        public void ProcessAllMonthlyRepayments()
        {
            var tiers = new List<int>(active.Keys);
            foreach (int tier in tiers)
                ProcessMonthlyRepayment(tier);
        }

        /// <summary>
        /// Returns all active bonds. Used by save capture.
        /// </summary>
        public Dictionary<int, BondData> GetAllActiveBonds() => active;

        /// <summary>
        /// Replaces active bonds from loaded save data. Used by save restore.
        /// </summary>
        public void SetAllActiveBonds(Dictionary<int, BondData> bonds)
        {
            active = bonds ?? new Dictionary<int, BondData>();
        }

        /// <summary>Exposes the configured fixed interest rate for issuance math.</summary>
        public float FixedInterestRate => fixedInterestRate;

        // ── Save / load round-trip (save-schema v4 — Stage 4) ──────────────────────────

        /// <summary>
        /// Serialize active bonds into a flat list for <see cref="GameSaveData.bondRegistry"/>.
        /// Called by <see cref="Territory.Persistence.GameSaveManager"/> pre-write.
        /// </summary>
        public List<BondData> CaptureSaveData()
        {
            return new List<BondData>(active.Values);
        }

        /// <summary>
        /// Restore active bonds from a saved list. Rebuilds internal Dictionary keyed by scaleTier.
        /// Null or empty list → clears registry (valid steady state for fresh games).
        /// Called by <see cref="Territory.Persistence.GameSaveManager"/> post-migration.
        /// </summary>
        public void RestoreFromSaveData(List<BondData> bonds)
        {
            active.Clear();
            if (bonds == null) return;
            foreach (var b in bonds)
            {
                if (b != null && !active.ContainsKey(b.scaleTier))
                    active[b.scaleTier] = b;
            }
        }
    }
}
