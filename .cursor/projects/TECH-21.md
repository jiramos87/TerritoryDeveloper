# TECH-21 — JSON leverage: schema pilot and tooling alignment

> **Issue:** [TECH-21](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **31**.

## 1. Summary

Satisfy **TECH-21** backlog **Acceptance** with a **written plan** plus at least one **pilot**: a **JSON Schema** (or equivalent) for a concrete artifact — e.g. **TECH-15**/**TECH-16** report `schema_version`, MCP test fixture, or a small save-adjacent export — without breaking existing **save data**. Align naming with **glossary** / **persistence-system** where the pilot touches game concepts.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Document prioritized JSON use cases (runtime vs tooling vs future API) in this spec **Decision Log** or **Implementation Plan** outcome.
2. Ship one **pilot** schema + validator (`ajv`, `jsonschema`, or CI step) on a low-risk path.
3. Versioning field (`schema_version`) on new JSON artifacts.

### 2.2 Non-Goals (Out of Scope)

1. Replacing entire **save/load** pipeline.
2. Duplicating full reference specs as JSON (**TECH-18** policy).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want CI to reject invalid tool outputs. | Validator fails on bad fixture. |
| 2 | Agent | I want stable shapes for reports. | Schema checked into `tools/` or `docs/schemas/`. |

## 4. Current State

### 4.1 Domain behavior

**Save data** and **CellData** semantics remain per **persistence-system**; pilot must not alter on-disk player saves without migration plan.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Spec | `.cursor/specs/persistence-system.md` |
| Related | **TECH-15**, **TECH-16** report JSON |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A unless pilot touches runtime — then no behavior regression.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Pick pilot in Phase 1 (recommend: **TECH-16** `sim-tick-profile` JSON).
- Add `schemas/sim-tick-profile.schema.json` (example name) + `npm run validate:fixtures` or similar.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created | Backlog integration | — |

## 7. Implementation Plan

### Phase 1 — Choose pilot artifact

- [ ] List use cases; select pilot.
- [ ] Add JSON Schema file.

### Phase 2 — Validator

- [ ] Wire Node script or CI step.
- [ ] Add golden good + bad fixture tests.

### Phase 3 — Document

- [ ] Update **TECH-21** backlog Acceptance checkbox when done.

## 8. Acceptance Criteria

- [ ] Plan + pilot schema + validator merged.
- [ ] **Unity / game:** no save breakage; if pilot is Editor-only, state explicitly in Decision Log.
- [ ] Glossary-aligned vocabulary in schema `description` fields where applicable.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — tooling only; pilot choice is **implementation**, not game logic.
