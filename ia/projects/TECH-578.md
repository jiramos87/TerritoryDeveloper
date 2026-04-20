---
purpose: "TECH-578 — Collapse AGENTS.md §3."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T2.1.2"
---
# TECH-578 — Collapse AGENTS.md §3

> **Issue:** [TECH-578](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

A1: AGENTS.md lifecycle section becomes short cross-reference; no restated step/stage/phase/task definitions.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `AGENTS.md` §3 ≤8 lines; points to canonical doc + surface map stub.
2. No section duplicates CLAUDE key-files list inappropriately.
3. `npm run validate:all` green.

### 2.2 Non-Goals (Out of Scope)

1. Slash-command flattening (Stage 2.3).
2. Skill preamble de-dupe (Stage 2.2).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | AGENTS points to one lifecycle doc | §3 is cross-ref only; validators pass |

## 4. Current State

### 4.1 Domain behavior

`AGENTS.md` §3 currently mirrors lifecycle taxonomy; should defer to `docs/agent-lifecycle.md` and `ia/rules/agent-lifecycle.md` §Surface map.

### 4.2 Systems map

Touches: `AGENTS.md` §3, `docs/agent-lifecycle.md`, `ia/rules/agent-lifecycle.md` (surface map anchor).

### 4.3 Implementation investigation notes (optional)

None.

## 5. Proposed Design

### 5.1 Target behavior (product)

Entry guide lists where to read lifecycle flow without duplicating tables.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Replace §3 body per Stage 2.1 Intent; grep for accidental CLAUDE inventory overlap.

### 5.3 Method / algorithm notes (optional)

N/A.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Cross-ref both canonical + surface map | AGENTS readers need router + doc | Doc-only pointer |

## 7. Implementation Plan

### Phase 1 — AGENTS collapse

1. Replace §3 body with cross-ref block per Stage 2.1 Intent.
2. Grep AGENTS for accidental CLAUDE inventory duplication; fix.
3. `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Doc / IA edits only | N/A | `npm run validate:all` | No C# |

## 8. Acceptance Criteria

- [ ] `AGENTS.md` §3 ≤8 lines; points to canonical doc + surface map stub.
- [ ] No section duplicates CLAUDE key-files list inappropriately.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: AGENTS §3 cross-ref too terse — readers lose navigation to **Surface map**. Mitigation: explicit path to `ia/rules/agent-lifecycle.md` §Surface map + `docs/agent-lifecycle.md` in one block.
- Risk: duplicate inventory vs CLAUDE §3 after TECH-577 lands. Mitigation: run TECH-578 after TECH-577 on same branch; grep `AGENTS.md` for duplicated key-files lists.

### §Examples

| Check | Pass criterion |
|-------|----------------|
| §3 line count | ≤8 lines for lifecycle subsection |
| Cross-ref | Contains `docs/agent-lifecycle.md` + surface map pointer |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| agents_s3_lines | `AGENTS.md` | §3 lifecycle block ≤8 lines | manual |
| no_dup_key_files | `AGENTS.md` vs `CLAUDE.md` | no accidental second key-files inventory | manual + grep |
| validate_all | repo | exit 0 | node |

### §Acceptance

- [ ] `AGENTS.md` §3 ≤8 lines with required cross-references.
- [ ] No inappropriate duplication of CLAUDE key-files content.
- [ ] `npm run validate:all` exit 0.

### §Findings

- None.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
