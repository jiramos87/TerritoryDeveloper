# BUG-12 — Happiness UI always shows 50%

> **Issue:** [BUG-12](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — keep **BACKLOG** / spec wording glossary-aligned (**TECH-27**); no dedicated roadmap row.

## 1. Summary

The UI Toolkit city stats panel shows a **hardcoded 50%** happiness value instead of the live **`CityStats.happiness`** score. Players cannot see real **happiness** changes. Fixing this unblocks **FEAT-23** (dynamic happiness) by making the HUD truthful.

## 2. Goals and Non-Goals

### 2.1 Goals

1. **`CityStatsUIController`** displays **`cityStats.happiness`** (or an agreed mapping) with the same intent as the legacy **`UIManager`** happiness text.
2. Color thresholds in **`GetHappinessColor`** remain meaningful for the displayed range.

### 2.2 Non-Goals (Out of Scope)

1. Redesigning the **happiness** simulation model (**FEAT-23**).
2. Changing **cell**-level happiness in **details** popups (unless trivially aligned).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I want the stats overlay to show my city’s real **happiness** so I can see the effect of zoning and economy. | Value updates when **`CityStats.happiness`** changes; not stuck at 50%. |
| 2 | Developer | I want one source of truth for HUD **happiness** so future **FEAT-23** work is visible. | **`GetHappiness()`** reads **`cityStats`**, not a literal. |

## 4. Current State

### 4.1 Domain behavior

**Observed:** UI Toolkit label shows ~50% regardless of **`CityStats.happiness`**.  
**Expected:** Display reflects **`CityStats`** (integer score); formatting consistent with design (percentage label vs raw score — see Open Questions).

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — BUG-12 |
| Code | `CityStatsUIController.cs` — **`GetHappiness()`**, **`UpdateStatisticsDisplay()`** |
| Data | `CityStats.cs` — **`happiness`** (`int`), **`AddHappiness`** |
| Related HUD | `UIManager.cs` — **`happinessText.text = cityStats.happiness.ToString()`** (no `%`) |

### 4.3 Implementation investigation notes (optional)

- Legacy HUD uses integer as plain text; UI Toolkit uses **`{happiness:F1}%`**. Align scale (clamp 0–100 vs unbounded) with product preference.

## 5. Proposed Design

### 5.1 Target behavior (product)

The city statistics overlay must show the **current city happiness** derived from **`CityStats`**, not a placeholder constant.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Resolve **`cityStats`** in **`Awake`** (existing pattern).
2. Replace **`GetHappiness()`** body: return a **`float`** derived from **`cityStats.happiness`** (e.g. cast, or clamp to 0–100 if that matches game meaning).
3. If **`cityStats`** is null, keep a safe fallback (0 or “N/A”) without throwing.
4. Update XML **`/// <summary>`** on the class if behavior changes materially.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created from agent-friendly task list | Tracks implementation plan | — |

## 7. Implementation Plan

### Phase 1 — Wire data

- [ ] Open **`CityStatsUIController.GetHappiness()`** and remove the hardcoded **`50.0f`**.
- [ ] Read **`cityStats.happiness`**; apply agreed mapping (see Open Questions).
- [ ] Ensure **`UpdateStatisticsDisplay`** still handles null **`cityStats`** gracefully.

### Phase 2 — Verify and document

- [ ] Build in Unity; place zones / trigger **`AddHappiness`** if available; confirm label moves off 50%.
- [ ] Adjust **`GetHappinessColor`** thresholds only if the numeric range changes materially.

## 8. Acceptance Criteria

- [ ] With non-default **`CityStats.happiness`**, the UI Toolkit stats panel shows that value (per agreed format), not 50%.
- [ ] No exception when **`cityStats`** is unassigned in a broken scene (defensive behavior).
- [ ] Class-level **`/// <summary>`** remains accurate.
- [ ] **Unity:** Play mode — stats overlay updates when **happiness** changes (smoke test).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

1. Should the overlay show **`cityStats.happiness`** as a **0–100 percentage** (clamp), or as a **raw score** matching **`UIManager.happinessText`** (integer, no clamp)? Product owner or existing **economy** tuning decides; until then, prefer **consistency with `UIManager`** unless UX explicitly requires `%`.
