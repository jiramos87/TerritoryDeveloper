### Stage 2 — Foundation: enum extension + floor-clamp treasury + envelope budget + save schema / `TreasuryFloorClampService` + systemic spend delegation

**Status:** Final

**Objectives:** Land hard-cap treasury (Q4 locked decision). Wrap ALL `EconomyManager` spend call sites so balance NEVER goes negative. Existing `SpendMoney(int, string, bool)` keeps signature for backward compat but delegates to new `TrySpend`. This stage is the single riskiest refactor — touches every current money-out path in `EconomyManager`.

**Exit:**

- `TreasuryFloorClampService.cs` under `Assets/Scripts/Managers/GameManagers/` — MonoBehaviour, Inspector-wired on `EconomyManager` GO, `FindObjectOfType` fallback in `Awake` (guardrail #1).
- Public API: `bool CanAfford(int amount)`, `bool TrySpend(int amount, string context)`, `int CurrentBalance`.
- Existing `EconomyManager.SpendMoney` retained for backward compat, internally delegates to `TrySpend` (insufficient → `false` return + game notification, no mutation).
- EditMode test proves balance cannot go below zero via any public EconomyManager spend path.
- Glossary row: `TreasuryFloorClampService`.
- `npm run unity:compile-check` green.
- Phase 1 — `TreasuryFloorClampService` skeleton + Inspector wiring.
- Phase 2 — Delegate existing `SpendMoney` + re-route call sites + EditMode coverage + glossary.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | `TreasuryFloorClampService` skeleton | **TECH-379** | Done (archived) | New MonoBehaviour at `Assets/Scripts/Managers/GameManagers/TreasuryFloorClampService.cs`. `[SerializeField] private EconomyManager economy;` with `FindObjectOfType<EconomyManager>()` fallback in `Awake`. Public API: `CanAfford(int) → bool`, `TrySpend(int, string) → bool`, `CurrentBalance` property reading `economy.GetCurrentMoney()`. `TrySpend` checks `amount <= CurrentBalance` BEFORE mutation; success path calls `economy.cityStats.RemoveMoney(amount)`; failure path emits `GameNotificationManager.PostError`. |
| T2.2 | Wire service on `EconomyManager` GO | **TECH-380** | Done (archived) | Add `[SerializeField] private TreasuryFloorClampService treasuryFloorClamp;` field + `FindObjectOfType` fallback in `EconomyManager.Awake` (guardrail #1, invariant #4). Attach component to the `EconomyManager` GameObject in the main scene prefab. Document composition relationship in XML doc on the field. |
| T2.3 | Re-route `SpendMoney` through `TrySpend` | **TECH-381** | Done (archived) | Existing `EconomyManager.SpendMoney(int amount, string context, bool logToConsole)` keeps signature but body delegates to `treasuryFloorClamp.TrySpend(amount, context)`. On `false` return, log + emit notification; do NOT subtract `currentMoney` (previously allowed negative). Audit all internal call sites inside `EconomyManager.cs` that touch `currentMoney -= X` directly — rewrite via `TrySpend`. |
| T2.4 | Audit cross-file `SpendMoney` call sites | **TECH-382** | Done (archived) | Grep for `SpendMoney(` + `currentMoney -=` across `Assets/Scripts/**`. For each non-EconomyManager caller, confirm path now routes through `TrySpend`; update any direct `currentMoney` mutation to `TrySpend`. Document audit result in Decision Log section of spec stub. Zero remaining direct `currentMoney -=` outside `TreasuryFloorClampService`. |
| T2.5 | EditMode tests + glossary row | **TECH-383** | Done (archived) | `TreasuryFloorClampServiceTests` under `Assets/Tests/EditMode/Economy/`. Cases: (a) `TrySpend(100)` when balance=200 succeeds + balance=100, (b) `TrySpend(300)` when balance=200 returns false + balance UNCHANGED + notification emitted, (c) `CanAfford(200)` true at balance=200, false at balance=199. Add `TreasuryFloorClampService` glossary row linking exploration + forthcoming economy-system spec. Regenerate MCP indexes. |
