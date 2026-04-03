# TECH-13 — Remove obsolete urbanization proposal system

> **Issue:** [TECH-13](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — **TECH-18** / **simulation_tick_outline** and glossary slices help avoid confusing **urbanization proposal** with **urban centroid** / **growth rings**.

## 1. Summary

The **urbanization proposal** feature is **obsolete** and must **never** be re-enabled (**invariant**). This issue removes dead code, UI, and models while **keeping** **`UrbanCentroidService`** / **urban growth rings** for **AUTO** roads and zoning. **Save data** and scenes must not break.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Remove **`UrbanizationProposalManager`**, **`ProposalUIController`**, **`UrbanizationProposal`** (and related types), scene objects, and **UI** hooks.
2. Strip **`SimulationManager`** / **`UIManager`** references to proposals.
3. Audit **save/load** for proposal fields; migrate or ignore safely for legacy saves.

### 2.2 Non-Goals (Out of Scope)

1. Re-enabling or redesigning proposals (forbidden).
2. Removing **AUTO** **urban centroid** / **growth rings** (**FEAT-32** lineage).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Player | I want **Load Game** to work on old saves. | Legacy saves load without proposal data errors. |
| 2 | Developer | I want no dead proposal code paths. | No compile references; **invariant** satisfied. |

## 4. Current State

### 4.1 Domain behavior

**Observed:** Proposal system disabled; code remains.  
**Expected:** Clean codebase; **AUTO** still uses **centroid** / **rings**.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — TECH-13 |
| Files | Per backlog: **`UrbanizationProposalManager.cs`**, **`ProposalUIController.cs`**, **`UrbanizationProposal.cs`**, **`SimulationManager.cs`**, **`UIManager.cs`**, scenes, **save data** |
| Invariant | `.cursor/rules/invariants.mdc` — **`UrbanizationProposal`**: NEVER re-enable |

### 4.3 Implementation investigation notes (optional)

- `rg -i Urbanization`, `rg -i Proposal` across **`Assets/`**.
- Check **`GameSaveData`** / serialization for proposal fields.

## 5. Proposed Design

### 5.1 Target behavior (product)

No **urbanization proposal** UI or simulation hooks. **AUTO** growth behavior unchanged.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

1. Map dependency graph; delete leaf types first.
2. Remove scene **GameObjects** / components; fix **MainScene** YAML if needed.
3. **Save:** remove fields or mark **`[Obsolete]`** with default-on-load; document in **persistence** spec if behavior changes for edge cases.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created | High-audit cleanup | — |

## 7. Implementation Plan

### Phase 1 — Audit

- [ ] Grep all references to proposal types and strings.
- [ ] List **save** DTO fields related to proposals.

### Phase 2 — Code removal

- [ ] Delete or gut proposal classes; fix compile.
- [ ] Remove **`SimulationManager`** / **`UIManager`** branches.

### Phase 3 — Assets and persistence

- [ ] Remove UI prefabs/scene nodes.
- [ ] Handle legacy **save** data (ignore with safe defaults).

### Phase 4 — Verify

- [ ] **New Game** + **Load Game** (old save if available).
- [ ] Confirm **AUTO** roads/zoning still run.

## 8. Acceptance Criteria

- [ ] No **urbanization proposal** feature surface in game.
- [ ] **`UrbanCentroidService`** / **growth rings** intact for **AUTO**.
- [ ] **Invariant**: proposals not re-enabled.
- [ ] **Unity:** **Load Game** on legacy saves — no crash / missing-script spam after removal.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- (Fill at closure.)

## Open Questions (resolve before / during implementation)

1. For **save** payloads: must proposal data be **stripped** on next save, or silently dropped on load only? **Persistence** policy — not **simulation** logic; record in **Decision Log**.
