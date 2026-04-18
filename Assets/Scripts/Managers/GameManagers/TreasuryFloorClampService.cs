using UnityEngine;

namespace Territory.Economy
{
    /// <summary>
    /// Helper service owning the hard-cap treasury rule: balance NEVER goes negative.
    /// Extracted from <see cref="EconomyManager"/> per invariant #6 (helper carve-out).
    /// Composes <see cref="EconomyManager"/> via Inspector field + FindObjectOfType fallback (guardrail #1).
    /// Mutation site: <see cref="EconomyManager.cityStats"/>.RemoveMoney — same pattern as
    /// EconomyManager.SpendMoney line 170. This is the ONE authorised treasury-mutation site
    /// post TECH-382 audit.
    /// </summary>
    public class TreasuryFloorClampService : MonoBehaviour
    {
        /// <summary>
        /// Composition reference to the city economy host.
        /// Wired via Inspector; Awake falls back to FindObjectOfType.
        /// Invariant #6: helper service holds a composition reference, not a new singleton.
        /// </summary>
        [SerializeField] private EconomyManager economy;

        private void Awake()
        {
            if (economy == null)
                economy = FindObjectOfType<EconomyManager>();

            if (economy == null)
                Debug.LogError("TreasuryFloorClampService: EconomyManager not found. Assign via Inspector.");
        }

        /// <summary>
        /// Current treasury balance. Returns 0 when economy reference is unavailable.
        /// </summary>
        public int CurrentBalance => economy != null ? economy.GetCurrentMoney() : 0;

        /// <summary>
        /// Returns true when the treasury can cover <paramref name="amount"/> without going negative.
        /// </summary>
        /// <param name="amount">Amount to check.</param>
        public bool CanAfford(int amount) => CurrentBalance >= amount;

        /// <summary>
        /// Attempt to spend <paramref name="amount"/> from the treasury.
        /// Floor-clamp rule: balance never drops below 0.
        /// </summary>
        /// <param name="amount">Amount to spend. Must be non-negative.</param>
        /// <param name="context">Short label shown in the insufficient-funds notification (e.g. "Road placement").</param>
        /// <returns>
        /// <c>true</c> if the spend succeeded and the amount was deducted;
        /// <c>false</c> if the amount was negative or insufficient funds.
        /// </returns>
        public bool TrySpend(int amount, string context)
        {
            if (amount < 0)
            {
                Debug.LogError($"TreasuryFloorClampService: Cannot spend negative amount ({amount}). Context: {context}");
                return false;
            }

            if (CanAfford(amount))
            {
                economy.cityStats.RemoveMoney(amount);
                return true;
            }

            // Insufficient funds — notify player; leave balance untouched.
            economy?.gameNotificationManager?.PostError(
                $"Insufficient Funds\n{context}\nCannot spend ${amount}. Current balance is ${CurrentBalance}."
            );
            return false;
        }
    }
}
