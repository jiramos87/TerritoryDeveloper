### Stage 3 — Foundation: enum extension + floor-clamp treasury + envelope budget + save schema / `BudgetAllocationService` + save-schema v3→v4 migration

**Status:** Final

**Objectives:** Land the envelope allocator (Q3 locked decision) + save-schema bump so fresh games persist state + legacy v3 saves migrate cleanly with default-equal envelope. `TryDraw` enforces Q4 block-before-deduct by checking BOTH envelope remaining AND `TreasuryFloorClampService.CanAfford` before mutation. This stage completes Step 1 — all structural primitives ready for Step 2 consumers.

**Exit:**

- `IBudgetAllocator` interface under `Assets/Scripts/Managers/GameManagers/IBudgetAllocator.cs`.
- `BudgetAllocationService.cs` implements `IBudgetAllocator` + MonoBehaviour + Inspector-wired. Internal state: `float[7] envelopePct`, `int globalMonthlyCap`, `int[7] currentMonthRemaining`.
- `TryDraw(int subTypeId, int amount)` checks envelope remaining AND treasury floor BEFORE deduction.
- `SetEnvelopePct(int, float)` auto-normalizes so sum == 1.0 (N1 non-blocking resolution).
- `MonthlyReset()` called from `EconomyManager.ProcessDailyEconomy` on month-first day.
- `GameSaveData.CurrentSchemaVersion = 4`. New fields: `List<StateServiceZoneData> stateServiceZones` + `BudgetAllocationData budgetAllocation` (float[7] envelopePct, int globalMonthlyCap, int[7] currentMonthRemaining).
- `MigrateLoadedSaveData` branch for v3→v4 seeds equal 14.28% × 7 envelope + empty S list. Normalizes envelope post-seed.
- Glossary rows added: `BudgetAllocationService`, `IBudgetAllocator`.
- EditMode + integration tests green; `npm run unity:compile-check` green.
- Phase 1 — `IBudgetAllocator` + `BudgetAllocationService` skeleton.
- Phase 2 — `TryDraw` logic + monthly reset + envelope normalization.
- Phase 3 — Save schema v3→v4 bump + migration + round-trip + tests + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | `IBudgetAllocator` interface + `BudgetAllocationService` skeleton | **TECH-418** | Done (archived) | Author `IBudgetAllocator.cs` with `TryDraw(int, int) → bool`, `GetMonthlyEnvelope(int) → int`, `SetEnvelopePct(int, float)`, `MonthlyReset()`. Author `BudgetAllocationService.cs` MonoBehaviour implementing the interface; `[SerializeField]` refs to `EconomyManager` + `TreasuryFloorClampService` + `ZoneSubTypeRegistry` with `FindObjectOfType` fallbacks in `Awake`. Fields inert in this task. |
| T3.2 | Wire service on `EconomyManager` GO | **TECH-419** | Done (archived) | Add `[SerializeField] private BudgetAllocationService budgetAllocation;` to `EconomyManager` + `FindObjectOfType` fallback. Attach `BudgetAllocationService` + `ZoneSubTypeRegistry` MonoBehaviours to the `EconomyManager` GO (same GO as `TreasuryFloorClampService`). Does not call them yet — consumer wiring lands in Step 2. |
| T3.3 | `TryDraw` + monthly reset logic | **TECH-420** | Done (archived) | Implement `TryDraw(subTypeId, amount)`: returns false if `subTypeId < 0 |  | subTypeId > 6`; returns false if `currentMonthRemaining[subTypeId] < amount`; returns false if `!treasuryFloorClamp.CanAfford(amount)`; otherwise `currentMonthRemaining[subTypeId] -= amount`, `treasuryFloorClamp.TrySpend(amount, "S envelope draw")` and return true. Implement `MonthlyReset`: for each i, `currentMonthRemaining[i] = (int)(globalMonthlyCap * envelopePct[i])`. |
| T3.4 | `SetEnvelopePct` auto-normalize | **TECH-421** | Done (archived) | Implement `SetEnvelopePct(int subTypeId, float pct)`: store raw value then normalize entire array so `sum == 1.0` (scale every entry by `1.0 / currentSum`). Handles N1 rounding residue from 14.28% × 7. Guard division-by-zero on all-zero envelope (reject + keep prior state). Also expose `SetEnvelopePctsBatch(float[7])` for player slider commits. |
| T3.5 | Save-schema v3→v4 bump | **TECH-422** | Done (archived) | Bump `GameSaveData.CurrentSchemaVersion` from 3 to 4 in `GameSaveManager.cs`. Add `[Serializable] public class BudgetAllocationData { public float[] envelopePct; public int globalMonthlyCap; public int[] currentMonthRemaining; public static BudgetAllocationData Default(int cap); }`. Add `public BudgetAllocationData budgetAllocation` + `public List<StateServiceZoneData> stateServiceZones` fields on `GameSaveData`. Add `[Serializable] public class StateServiceZoneData { public int cellX, cellY; public int subTypeId; public int densityTier; }`. |
| T3.6 | `MigrateLoadedSaveData` v3→v4 branch | **TECH-423** | Done (archived) | Null-coalesce branch in `MigrateLoadedSaveData` seeds `stateServiceZones` (empty list) + `budgetAllocation` (`BudgetAllocationData.Default(DEFAULT_S_CAP)`) before final `schemaVersion = CurrentSchemaVersion` line. Cap constant `DEFAULT_S_CAP = 10_000` on `GameSaveManager` (save-lifecycle owner). Idempotent on already-v4 data (non-null → skip). Integrity asserts appended (null post-migration → `InvalidOperationException`). `Default(cap)` envelope sums 1.0 ±1e-6. Decision Log persisted to `ia_project_spec_journal`. |
| T3.7 | Save/load round-trip wiring | **TECH-424** | Done (archived) | Wire `BudgetAllocationService.CaptureSaveData()` + `RestoreFromSaveData(BudgetAllocationData)` methods. `GameSaveManager.SaveGame` calls capture; `GameSaveManager.LoadGame` calls restore post-migration. Verify envelope + remaining + cap survive save → load → verify identity. No S zones restored yet (placement lands in Step 2). |
| T3.8 | EditMode tests + glossary + MCP reindex | **TECH-425** | Done (archived) | `BudgetAllocationServiceTests` under `Assets/Tests/EditMode/Economy/`. Cases: (a) `TryDraw` blocks when envelope exhausted, (b) `TryDraw` blocks when treasury empty (envelope fat), (c) `TryDraw` succeeds when both OK + both decrement, (d) `MonthlyReset` restores `currentMonthRemaining` to `cap × pct`, (e) `SetEnvelopePct` normalizes sum to 1.0 within 1e-6, (f) legacy v3 save loads into v4 with equal envelope + empty S list. Add `BudgetAllocationService` + `IBudgetAllocator` glossary rows. Run `npm run validate:all`. |

---
