# FEAT-21 — Expenses and maintenance system

> **Issue:** [FEAT-21](../../BACKLOG.md)
> **Status:** In Review
> **Created:** 2026-04-08
> **Last updated:** 2026-04-08

## 1. Summary

Introduce recurring **city expenses** (maintenance and operating costs) so the treasury is not only fed by **tax base** income. The goal is economic tension and player-visible feedback: larger **road** networks, **utility** **buildings**, and implicit **service** costs should drain **money** on a predictable cadence, complementing the **Economic depth lane** (see [`BACKLOG.md`](../../BACKLOG.md) **Economic depth lane**).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Apply recurring **expenses** that scale with city infrastructure (at minimum **streets** and **utility** **buildings**).
2. Integrate with existing treasury flow (`EconomyManager`, `CityStats`) so outflows use the same rules as other spending where possible.
3. Make costs **player-visible** (e.g. **game notification** and/or HUD line item) on the same schedule as charges.
4. Leave a clear extension point for **TECH-82** Phase 2 (**city events**) as an optional audit trail (see [`BACKLOG.md`](../../BACKLOG.md) **TECH-82** notes on **FEAT-21**).

### 2.2 Non-Goals (Out of Scope)

1. **Tax base** → **demand (R / C / I)** feedback (**FEAT-22**).
2. **City services coverage** model (**FEAT-52**) — no new coverage grid in this issue unless needed only as a data source for counts already on **CityStats**.
3. **Trade / production / salaries** deep economy (**FEAT-09**).
4. Per-**building** identity beyond what existing counters and lists already provide (see **Open Questions** if product later requires per-instance costs).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I want my city to incur upkeep so growing infrastructure has a cost. | Recurring charge appears tied to **roads** / **utility** assets; **money** decreases accordingly. |
| 2 | Player | I want to see when and why money was spent on maintenance. | At least one clear **game notification** (or HUD breakdown) on charge. |
| 3 | Developer | I want expenses wired through the economy layer, not ad-hoc `money` writes. | Outflows go through `EconomyManager` (or a dedicated helper it owns — see **§5.2**), not scattered direct decrements. |

## 4. Current State

### 4.1 Domain behavior

- **Tax base** income is collected on the first **simulation** day of each calendar month: `TimeManager` calls `EconomyManager.ProcessDailyEconomy()`, which invokes private `ProcessMonthlyEconomy()` when `GetCurrentDate().Day == 1`, then `cityStats.AddMoney(totalTaxIncome)` and a **game notification** summary.
- There is no systematic recurring **expense**; **money** trends up as the city grows.
- `EconomyManager.SpendMoney` guards insufficient funds, mutates `cityStats.money` directly on success (it does not call `CityStats.RemoveMoney`), and posts **game notification** errors on failure via **`GameNotificationManager.Instance`** (see **managers-reference** — **Game notifications**).

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog row | [`BACKLOG.md`](../../BACKLOG.md) **FEAT-21** — Files: `EconomyManager.cs`, `CityStats.cs` |
| Economy | [`managers-reference.md`](../specs/managers-reference.md) — **EconomyManager**, **CityStats** |
| Simulation cadence | [`simulation-system.md`](../specs/simulation-system.md) — align new charges with **TimeManager** / existing monthly boundary (see **Open Questions**) |
| Notifications | [`managers-reference.md`](../specs/managers-reference.md) — **Game notifications** |

### 4.3 Implementation investigation notes (optional)

- **Streets:** `CityStats.roadCount` is maintained when **road** cells use `ZoneType.Road` (`AddZoneBuildingCount` / `RemoveZoneBuildingCount`). Confirm product intent for **interstate** segments (often still **road** cells with an **interstate** flag) versus **street (ordinary road)** only.
- **Utility buildings:** `CityStats` registers **`PowerPlant`** instances (`RegisterPowerPlant` / `UnregisterPowerPlant`); there is no public plant count API today—implementer may expose a read-only count or add aggregation without scanning the **grid** if water and other **utility building** types need the same treatment.
- If monthly charges stack awkwardly with other day-1-of-month logic, fold **maintenance** into the same private monthly step as **tax** collection (or a single ordered “monthly ledger” method) so ordering is explicit in one place.

## 5. Proposed Design

### 5.1 Target behavior (product)

1. On each **billing period** (default assumption: monthly, same boundary as **tax** — **TBD** in **Open Questions**), compute **maintenance** from:
   - **Street (ordinary road)** upkeep: cost scales with a maintained **road** metric (default candidate: `CityStats.roadCount` = count of **road** cells). This is **not** the same as cumulative **road stroke** length (glossary: ordered placement path); the game does not currently persist total stroke length as one number.
   - **Utility building** upkeep: cost scales with operational service structures (glossary: non-RCI water/power-type **buildings**; see **managers-reference** — **World features**). Which types and counts are authoritative for v1 is **TBD**.
2. Subtract total **expenses** from the treasury. If funds are insufficient, behavior is **TBD** (block partial payment vs allow negative balance vs service degradation — **Open Questions**).
3. Notify the player with an aggregate line (and optional breakdown by category).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Primary touch: `EconomyManager` for scheduling and orchestration; read metrics from `CityStats` and/or existing managers.
- Prefer extracting pure calculation to a small helper class if logic grows (per **invariants**: do not overload **GridManager**).
- Constants (per-road cost, per-plant cost) should be **SerializeField** or config-friendly for tuning.
- When **TECH-82** **city events** exists, emit or enqueue structured **financial** events for this charge (optional phase).

### 5.3 Method / algorithm notes (optional)

- None locked — implementer proposes formulas after **Open Questions** are answered or defaulted in **Decision Log**.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-08 | Initial spec scoped to **streets** + **utility** **buildings** only | Matches **BACKLOG** **Notes** | Full **service** simulation deferred to **FEAT-52** |
| 2026-04-08 | Kickoff: monthly **tax** runs from `ProcessDailyEconomy` on calendar day 1; **`roadCount`** tracks **road** cells; **utility** counts may need thin **CityStats** API | Code read for **`EconomyManager`**, **`TimeManager`**, **`CityStats`** | — |
| 2026-04-08 | **Billing:** monthly on calendar day 1; **tax** then **maintenance** (income before charges). | Matches existing **`ProcessMonthlyEconomy`** hook. | Maintenance before tax |
| 2026-04-08 | **Insufficient funds:** no debit; **`PostError`** “Maintenance Unpaid” with breakdown (no negative balance, no partial pay). | Aligns with existing **`SpendMoney`** guard. | Partial pay / penalties deferred |
| 2026-04-08 | **Streets:** `CityStats.roadCount` (all **road** cells, including **interstate** where zoned as **Road**). | Single counter; no stroke-length aggregate. | Excluding interstate deferred |
| 2026-04-08 | **Utilities v1:** registered **`PowerPlant`** count only via **`GetRegisteredPowerPlantCount()`**; no water-treatment aggregate yet. | **`CityStats`** already lists plants. | Other **utility building** types later |

## 7. Implementation Plan

### Phase 1 — Model and cadence

- [x] Resolve billing period and ordering vs **tax** in **Open Questions** / **Decision Log** (code anchor: `TimeManager` → `EconomyManager.ProcessDailyEconomy()` → `ProcessMonthlyEconomy()` on day 1).
- [x] Add a dedicated private method (e.g. `ProcessMonthlyMaintenance` or a shared `ApplyMonthlyLedger`) invoked from the same monthly path as **tax** so order is explicit (e.g. **tax** then **maintenance**, or the reverse—record the choice).
- [x] **`ProcessMonthlyMaintenance`** with street + utility formulas on **EconomyManager**; extract a helper class if logic grows (**invariants:** do not push this into **GridManager**).

### Phase 2 — Street upkeep

- [x] Tie **street** maintenance to the agreed metric (likely `CityStats.roadCount`; confirm **interstate** inclusion per **Current State** investigation notes).
- [x] Apply outflow via `EconomyManager.SpendMoney` (or refactor spend to use `CityStats.RemoveMoney` in one place if consolidating—avoid new scattered `money` writes outside `EconomyManager`).
- [x] On successful spend, post **`GameNotificationManager.Instance.PostInfo`** (or equivalent category) so players see upkeep on the success path, not only `PostError` when funds are short.

### Phase 3 — Utility upkeep

- [x] Lock v1 **utility building** inventory (e.g. **power** plants via `PowerPlant` registration; water treatment / other services if tracked—may require small **CityStats** API or counters).
- [x] Scale cost from those counts; avoid full-**grid** scans if counters already exist.

### Phase 4 — Player feedback and tuning

- [x] HUD line item and/or **game notification** breakdown for v1; tune serialized costs (`SerializeField` or config) for noticeable but fair tension.
- [ ] Optional: hook **TECH-82** **city events** when available for a structured **financial** audit trail.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| C# compiles; no regression in economy entry points | Local | Unity batch / **`unity_compile`** / **`get_compilation_status`** | Per **AGENTS.md** after substantive C# edits |
| Play Mode: new game, advance time across a month boundary, observe **money** delta and notification | MCP / dev machine | **territory-ia** **`unity_bridge_command`**: advance **simulation** date across day 1 + **`get_console_logs`** (`severity_filter` as needed); optional **`capture_screenshot`** (`include_ui: true` if HUD line item) | **N/A** in CI; see **unity-development-context** (Editor agent diagnostics / bridge) |
| **Save** / **load**: **money** and any new `CityStats` / save DTO fields round-trip | Manual / dev machine | Save → quit to menu → load; optional bridge **`debug_context_bundle`** at known step | **`CityStatsData`** already persists **`money`** and **`roadCount`**; new fields must follow **persistence-system** **Save** / **Load pipeline** |
| Repo IA hygiene if glossary/specs change | Node | `npm run validate:all` | Only if durable docs or MCP-fed bodies are edited |

## 8. Acceptance Criteria

- [x] Recurring **expenses** reduce **money** on the agreed schedule.
- [x] Costs reflect **streets** and **utility** **buildings** at minimum.
- [x] Player receives readable feedback when charges apply.
- [x] No new direct **`money`** mutation outside the agreed **`EconomyManager`** path (`SpendMoney` now uses **`CityStats.RemoveMoney`**; placement/demolition paths unchanged).
- [x] **Save/load**: no new **`CityStatsData`** fields; **`money`** / **`roadCount`** unchanged — confirm in Play Mode before closeout.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | — | — | — |
| 2 | Maintenance failure could double **game notification** errors if **`SpendMoney`** and **`ProcessMonthlyMaintenance`** both posted | **`SpendMoney`** always notified on failure | **`notifyInsufficientFunds: false`** for maintenance attempt; single “Maintenance Unpaid” toast |

## 10. Lessons Learned

- *(Fill at closure.)*

## Open Questions (resolve before / during implementation)

*Product defaults for v1 are recorded in **Decision Log** (2026-04-08).*

1. **Billing period**: Should **maintenance** run monthly on the same calendar-day-1 path as **tax** (`ProcessDailyEconomy` / `ProcessMonthlyEconomy`), or on a different **simulation** cadence? If monthly, should charges apply before or after **tax** income for that tick?
2. **Insufficient funds**: If **maintenance** exceeds **money**, should the game skip payment (`SpendMoney` fails—no partial pay today), allow going through a new code path that permits negative balance, pay partial amounts, or apply a non-monetary penalty (e.g. **happiness** hit)?
3. **Street (ordinary road)** metric: Should upkeep scale with `roadCount` (Moore **road** cells on the **grid**), or should **interstate** segments be excluded or priced differently? (Total **road stroke** length is a different concept and is not currently aggregated city-wide.)
4. **Utility building** scope for v1: Which categories count (**power** plants only, water treatment, both, other **utility building** types)? Is **`PowerPlant`** registration count sufficient for v1 power, or is a public read-only count needed?
5. **Service** costs: Does v1 include a flat administration charge, or only **street** + **utility building** upkeep as in **BACKLOG** **Notes**?
