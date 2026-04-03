# TECH-31 — AUTO / simulation scenario and fixture generator

> **Issue:** [TECH-31](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **21**. Targets **BUG-52**-class regressions.

## 1. Summary

Build tooling that turns **structured constraints** (YAML, JSON, or thin front matter on `.cursor/templates/project-spec-template.md`) into **Unity Play Mode tests** or **serialized fixtures** (grid seeds) so **AUTO** **road** / **zoning** behavior can be reproduced without hand-building scenes. Vocabulary in fixture files must follow **glossary** and **simulation-system** (**tick execution order**, **AUTO systems**).

## 2. Goals and Non-Goals

### 2.1 Goals

1. At least one **reference fixture** checked in that runs in CI or locally with one command.
2. Document how to add a new scenario for **grass cell** / **road stroke** adjacency cases.
3. Outputs are **deterministic** given seed (document Unity random seed handling).

### 2.2 Non-Goals (Out of Scope)

1. Full property-based generation (**TECH-35** separate).
2. Replacing manual QA for art/visual **sorting order**.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want to reproduce **BUG-52** on demand. | Fixture + test documented. |
| 2 | AI agent | I want a file-backed repro, not a paragraph. | Path under `Tests/` or `tools/fixtures/`. |

## 4. Current State

### 4.1 Domain behavior

Scenarios must respect **simulation tick** order and **road reservation** rules per specs — generator must not encode contradictions.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Spec | `simulation-system.md`, `isometric-geography-system.md` §13 |
| Code | `AutoZoningManager`, `AutoRoadBuilder`, `SimulationManager` |

## 5. Proposed Design

### 5.1 Target behavior (product)

Generated tests assert **documented** expectations (e.g. “after N ticks, no persistent **grass** Moore-adjacent to **street**” — exact assertion agent/product owner defines in fixture).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Option A: Editor script imports YAML → sets grid → saves **ScriptableObject** test asset.
- Option B: Play Mode test loads JSON from `StreamingAssets` test folder.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created | Roadmap task 21 | — |

## 7. Implementation Plan

### Phase 1 — Format + one fixture

- [ ] Choose format; document schema.
- [ ] One **BUG-52**-oriented scenario.

### Phase 2 — Generator ergonomics

- [ ] CLI or menu to emit new fixture stub.

## 8. Acceptance Criteria

- [ ] **Unity:** At least one automated run (Edit Mode or Play Mode) passes on clean tree.
- [ ] Fixture/README explains glossary terms used.
- [ ] Linked from **BUG-52** backlog **Notes** when first scenario lands (optional follow-up).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

Fixture assertions may imply **game logic** choices (what “fixed” means for **BUG-52**) — coordinate with **BUG-52** acceptance; if unresolved, record under **BUG-52** spec **Open Questions**, not here. This spec stays **tooling** unless product locks expected behavior.
