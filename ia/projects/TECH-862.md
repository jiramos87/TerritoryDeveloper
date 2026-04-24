---
purpose: "TECH-862 — Warn on `depends_on_raw` drift."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/backlog-yaml-mcp-alignment-master-plan.md"
task_key: "T5.4"
---
# TECH-862 — Warn on `depends_on_raw` drift

> **Issue:** [TECH-862](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-24
> **Last updated:** 2026-04-24

## 1. Summary

Warning (not error) when `depends_on_raw` mentions an id not present in `depends_on: []`. Tokenize raw by `,` + strip soft markers before compare. Emit warning w/ record id + drift token. Scoped to Stage 5 of `ia/projects/backlog-yaml-mcp-alignment-master-plan.md` — validator extensions (IP8). Tooling-only, no runtime C# touches.

## 2. Goals and Non-Goals

### 2.1 Goals

1. validator: warn on drift tokens in depends_on_raw vs depends_on.
2. Land under `tools/validate-backlog-yaml.mjs` shared lint core where applicable; cross-record checks (need whole set) stay in script body.
3. `npm run validate:backlog-yaml` green on passing fixture, red on failing fixture (via fixture-runner harness).

### 2.2 Non-Goals (Out of Scope)

1. No schema changes to `backlog-record-schema.ts`.
2. No materialize-script touch.
3. No archive backfill (Stage 16 territory).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want validator: warn on drift tokens in depends_on_raw vs depends_on so that backlog yaml drift surfaces before merge. | `npm run validate:backlog-yaml` exits non-zero on failing fixture. |

## 4. Current State

### 4.1 Domain behavior

`tools/validate-backlog-yaml.mjs` currently validates per-record shape (Stage 1 + Stage 4). Cross-record checks not yet implemented.

### 4.2 Systems map

- `tools/validate-backlog-yaml.mjs` — validator script (cross-record loop lives here).
- `tools/mcp-ia-server/src/parser/backlog-record-schema.ts` — shared lint core (per-record checks only).
- `tools/scripts/test-fixtures/` — fixture root for pass/fail cases.

## 5. Proposed Design

### 5.1 Target behavior (product)

Warning (not error) when `depends_on_raw` mentions an id not present in `depends_on: []`. Tokenize raw by `,` + strip soft markers before compare. Emit warning w/ record id + drift token.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Agent decides — see task intent in orchestrator Stage 5 row.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-24 | Scope locked to Stage 5 exit criteria | Inherited from orchestrator Exit. | — |

## 7. Implementation Plan

### Phase 1 — Implement + fixtures

- [ ] validator: warn on drift tokens in depends_on_raw vs depends_on.
- [ ] Fixture pair(s) under `tools/scripts/test-fixtures/`.
- [ ] Fixture-runner harness asserts pass/fail text.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Validator check lands + fixture harness green | Node | `npm run validate:backlog-yaml` + `npm run validate:all` | Exit 0 on pass fixture; non-zero on fail fixture. |

## 8. Acceptance Criteria

- [ ] validator: warn on drift tokens in depends_on_raw vs depends_on.
- [ ] Fixture(s) land under `tools/scripts/test-fixtures/`.
- [ ] `validate:backlog-yaml` green on passing fixture, red on failing fixture.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Digest

_pending — populated by `/plan-digest ia/projects/backlog-yaml-mcp-alignment-master-plan.md 5`._

### §Goal

### §Acceptance

### §Test Blueprint

### §Examples

### §Mechanical Steps

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
