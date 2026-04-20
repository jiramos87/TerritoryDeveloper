---
purpose: "TECH-577 — Collapse rule + CLAUDE.md §3."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T2.1.1"
---
# TECH-577 — Collapse rule + CLAUDE.md §3

> **Issue:** [TECH-577](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

A1 doc-triangle: rule stub + CLAUDE key-files table point at `docs/agent-lifecycle.md` only; remove duplicate lifecycle taxonomy from always-loaded surfaces.

## 2. Goals and Non-Goals

### 2.1 Goals

1. `ia/rules/agent-lifecycle.md` ≤12 lines with pointer to `docs/agent-lifecycle.md` + ordered-flow stub.
2. `CLAUDE.md` §3 Key files ≤20 lines; lifecycle row references canonical doc only.
3. `npm run validate:all` green.

### 2.2 Non-Goals (Out of Scope)

1. Preamble stable-block rollout (Stage 2.2) — separate tasks.
2. Runtime C# or Unity changes.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Single lifecycle authority in docs | Rule + CLAUDE cite `docs/agent-lifecycle.md`; validators pass |

## 4. Current State

### 4.1 Domain behavior

Duplicate lifecycle taxonomy lives in `ia/rules/agent-lifecycle.md`, `CLAUDE.md` §3, and `AGENTS.md` §3; canonical narrative should live only in `docs/agent-lifecycle.md`.

### 4.2 Systems map

Touches: `ia/rules/agent-lifecycle.md`, `CLAUDE.md` §3, `docs/agent-lifecycle.md` (read-only confirm authority marker).

Ref: `docs/session-token-latency-audit-exploration.md` Theme A; master plan Step 2 Stage 2.1 exit bullets.

### 4.3 Implementation investigation notes (optional)

None.

## 5. Proposed Design

### 5.1 Target behavior (product)

Always-loaded entry points reference the canonical lifecycle doc without restating taxonomy tables.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Edit rule file to stub; trim CLAUDE §3 rows per Intent column.

### 5.3 Method / algorithm notes (optional)

N/A.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Stub rule file vs delete | Pointer preserves router links to §Surface map | Full delete |

## 7. Implementation Plan

### Phase 1 — Rule + CLAUDE collapse

1. Edit `ia/rules/agent-lifecycle.md` to header + one-line purpose + pointer + `## Ordered flow` stub linking canonical doc.
2. Trim `CLAUDE.md` §3: remove restated taxonomy; ensure key-files table includes `docs/agent-lifecycle.md` as lifecycle authority.
3. `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Doc / IA edits only | N/A | `npm run validate:all` | No C# |

## 8. Acceptance Criteria

- [ ] `ia/rules/agent-lifecycle.md` ≤12 lines with pointer to `docs/agent-lifecycle.md` + ordered-flow stub.
- [ ] `CLAUDE.md` §3 Key files ≤20 lines; lifecycle row references canonical doc only.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: shrinking `ia/rules/agent-lifecycle.md` breaks links from AGENTS or skills that anchor `§Surface map`. Mitigation: keep stub headings that preserve router jump targets; grep repo for `agent-lifecycle.md` before finalizing line budget.
- Risk: CLAUDE §3 edit collides with parallel doc edits on another branch. Mitigation: rebase early; single-writer pass for §3 table.
- Ambiguity: whether `## Ordered flow` stub stays empty or lists one line. Resolution: stub links to `docs/agent-lifecycle.md` body — no restated numbered list in the rule file.

### §Examples

| Surface | Before (shape) | After (shape) |
|---------|----------------|---------------|
| `ia/rules/agent-lifecycle.md` | Multi-section taxonomy | ≤12 lines + pointer + ordered-flow stub link |
| `CLAUDE.md` §3 | Lifecycle prose + long table | ≤20 lines; key-files row for `docs/agent-lifecycle.md` only |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| rule_stub_lines | `ia/rules/agent-lifecycle.md` | ≤12 non-blank lines of body after header OR meets Stage exit literal | manual |
| claude_key_files | `CLAUDE.md` §3 | ≤20 lines for §3 block; canonical doc cited | manual |
| validate_all | repo root | exit 0 | node |

### §Acceptance

- [ ] `ia/rules/agent-lifecycle.md` meets Stage 2.1 exit (≤12 lines, pointer + stub).
- [ ] `CLAUDE.md` §3 Key files meets ≤20 lines and cites `docs/agent-lifecycle.md` as lifecycle authority.
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
