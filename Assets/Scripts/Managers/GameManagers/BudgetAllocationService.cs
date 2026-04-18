using UnityEngine;

namespace Territory.Economy
{
    /// <summary>
    /// Helper service implementing <see cref="IBudgetAllocator"/>.
    /// Extracted per invariant #6 (helper carve-out): holds a composition reference to
    /// <see cref="EconomyManager"/> rather than owning treasury state directly.
    /// Wired via Inspector; <see cref="Awake"/> falls back to <c>FindObjectOfType</c> (guardrail #1,
    /// invariant #3 — no per-frame lookups).
    /// Body logic is inert in the scaffold phase; Phase 2 (TECH-420) owns TryDraw /
    /// MonthlyReset init.
    /// </summary>
    public class BudgetAllocationService : MonoBehaviour, IBudgetAllocator
    {
        // ── Dependency refs ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// City economy host. Wired via Inspector; Awake falls back to FindObjectOfType.
        /// Invariant #6: composition reference, not a new singleton.
        /// </summary>
        [SerializeField] private EconomyManager economy;

        /// <summary>
        /// Treasury floor-clamp service. Wired via Inspector; Awake falls back to FindObjectOfType.
        /// </summary>
        [SerializeField] private TreasuryFloorClampService treasuryFloor;

        /// <summary>
        /// Zone sub-type registry. Wired via Inspector; Awake falls back to FindObjectOfType.
        /// </summary>
        [SerializeField] private ZoneSubTypeRegistry subTypeRegistry;

        // ── Private backing fields (declared this task; inert until Phase 2 / TECH-420) ─────

        /// <summary>
        /// Per-sub-type fractional share of the global monthly cap.
        /// Size 7 matches the current zone sub-type count; Phase 2 inits to uniform 1f/7f.
        /// </summary>
        private float[] envelopePct = new float[7];

        /// <summary>
        /// Month-start treasury draw cap seeded from <see cref="EconomyManager"/> by Phase 2.
        /// </summary>
        private int globalMonthlyCap;

        /// <summary>
        /// Remaining envelope per sub-type for the current in-game month.
        /// Reset by <see cref="MonthlyReset"/> each month.
        /// </summary>
        private int[] currentMonthRemaining = new int[7];

        // ── Lifecycle ────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (economy == null)
                economy = FindObjectOfType<EconomyManager>();

            if (economy == null)
                Debug.LogError("BudgetAllocationService: EconomyManager not found. Assign via Inspector.");

            if (treasuryFloor == null)
                treasuryFloor = FindObjectOfType<TreasuryFloorClampService>();

            if (treasuryFloor == null)
                Debug.LogError("BudgetAllocationService: TreasuryFloorClampService not found. Assign via Inspector.");

            if (subTypeRegistry == null)
                subTypeRegistry = FindObjectOfType<ZoneSubTypeRegistry>();

            if (subTypeRegistry == null)
                Debug.LogError("BudgetAllocationService: ZoneSubTypeRegistry not found. Assign via Inspector.");
        }

        // ── IBudgetAllocator ─────────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public bool TryDraw(int subTypeId, int amount)
        {
            // Guard: subTypeId range
            if (subTypeId < 0 || subTypeId > 6)
                return false;

            // Guard: envelope remaining
            if (currentMonthRemaining[subTypeId] < amount)
                return false;

            // Guard: treasury floor
            if (treasuryFloor == null || !treasuryFloor.CanAfford(amount))
                return false;

            // Mutate envelope first (block-before-deduct invariant)
            currentMonthRemaining[subTypeId] -= amount;

            // Drain treasury; CanAfford pre-check makes false path theoretically unreachable.
            // If false, envelope already decremented — log warning, do NOT roll back.
            if (!treasuryFloor.TrySpend(amount, "S envelope draw"))
                Debug.LogWarning($"BudgetAllocationService.TryDraw: TrySpend returned false after CanAfford passed (subTypeId={subTypeId}, amount={amount}). Possible race/bug; envelope already decremented.");

            return true;
        }

        /// <inheritdoc/>
        public int GetMonthlyEnvelope(int subTypeId) => 0;

        /// <inheritdoc/>
        public void SetEnvelopePct(int subTypeId, float pct)
        {
            if (subTypeId < 0 || subTypeId > 6)
                return;

            // Snapshot prior state for rollback.
            float[] snapshot = new float[7];
            System.Array.Copy(envelopePct, snapshot, 7);

            envelopePct[subTypeId] = Mathf.Max(0f, pct);

            if (!NormalizeInPlace())
            {
                System.Array.Copy(snapshot, envelopePct, 7);
                Debug.LogWarning($"BudgetAllocationService.SetEnvelopePct: all-zero result after setting subTypeId={subTypeId}; prior state restored.");
            }
        }

        /// <inheritdoc/>
        public void SetEnvelopePctsBatch(float[] pcts)
        {
            if (pcts == null || pcts.Length != 7)
            {
                Debug.LogWarning("BudgetAllocationService.SetEnvelopePctsBatch: pcts must be non-null with length 7; call ignored.");
                return;
            }

            // Snapshot prior state for rollback.
            float[] snapshot = new float[7];
            System.Array.Copy(envelopePct, snapshot, 7);

            for (int i = 0; i < 7; i++)
                envelopePct[i] = Mathf.Max(0f, pcts[i]);

            if (!NormalizeInPlace())
            {
                System.Array.Copy(snapshot, envelopePct, 7);
                Debug.LogWarning("BudgetAllocationService.SetEnvelopePctsBatch: all-zero input after clamp; prior state restored.");
            }
        }

        /// <summary>
        /// Normalizes <see cref="envelopePct"/> in-place so all slots sum to 1.0.
        /// Returns <c>false</c> (reject) when sum &lt; 1e-9 (all-zero guard);
        /// caller is responsible for restoring prior state.
        /// </summary>
        private bool NormalizeInPlace()
        {
            float sum = 0f;
            for (int i = 0; i < 7; i++)
                sum += envelopePct[i];

            if (sum < 1e-9f)
            {
                Debug.LogWarning("BudgetAllocationService.NormalizeInPlace: sum < 1e-9; normalization rejected.");
                return false;
            }

            float inv = 1f / sum;
            for (int i = 0; i < 7; i++)
                envelopePct[i] *= inv;

            return true;
        }

        /// <inheritdoc/>
        public void MonthlyReset()
        {
            for (int i = 0; i < 7; i++)
                currentMonthRemaining[i] = (int)(globalMonthlyCap * envelopePct[i]);
        }
    }
}
