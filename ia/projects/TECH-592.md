---
purpose: "TECH-592 — Optional backlog spec pointer."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/unity-agent-bridge-master-plan.md"
task_key: "T1.3.6"
phases:
  - "Check TECH-552 or successor"
  - "Patch yaml spec or document skip"
---
# TECH-592 — Optional backlog spec pointer

> **Issue:** [TECH-592](../../BACKLOG.md)
> **Status:** Done
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Optional alignment between bridge umbrella backlog record and this master plan path when TECH-552 or
successor exists.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Confirm whether TECH-552.yaml (or listed successor) is active bridge tracker.
2. If yes, set spec field to ia/projects/unity-agent-bridge-master-plan.md and materialize backlog.
3. If no, document skip — do not invent ids in orchestrator body.

### 2.2 Non-Goals (Out of Scope)

1. Creating new umbrella issues.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Maintainer | As backlog owner, I want orchestrator pointer on umbrella issue when applicable | yaml spec or skip note |

## 4. Current State

### 4.1 Domain behavior

TECH-552.yaml may exist as bridge program tracker; spec field may point elsewhere or be empty.

### 4.2 Systems map

- ia/backlog/TECH-552.yaml — conditional
- BACKLOG.md — generated view
- ia/projects/unity-agent-bridge-master-plan.md — orchestrator path

## 5. Proposed Design

### 5.1 Target behavior (product)

Single source of truth for orchestrator path when backlog row exists.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Read-only check first; yaml edit only if appropriate.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Conditional edit | Orchestrator task intent | Force edit |

## 7. Implementation Plan

### Phase 1 — Pointer

- [x] backlog_issue TECH-552 (or successor) status check
- [x] Patch yaml spec field if appropriate
- [x] bash tools/scripts/materialize-backlog.sh when yaml changes

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Backlog yaml valid | Node | `npm run validate:backlog-yaml` | If yaml touched |
| materialize | Bash | `materialize-backlog.sh` | If yaml touched |

## 8. Acceptance Criteria

- [x] Confirm whether TECH-552.yaml (or listed successor) is active bridge tracker
- [x] If yes, spec field points to ia/projects/unity-agent-bridge-master-plan.md and backlog materialized
- [x] If no, skip documented — no invented ids in orchestrator body

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## §Plan Author

### §Audit Notes

- TECH-552.yaml exists in repo — verify `status` + `spec` before edit; do not force pointer if issue closed or unrelated program.
- If `spec:` already correct — no-op with note in §Verification.
- If `spec:` updated — run `materialize-backlog.sh` once; never hand-edit `BACKLOG.md`.

### §Examples

| TECH-552 field | Action when bridge program |
|----------------|----------------------------|
| `spec` empty or wrong | Set to `ia/projects/unity-agent-bridge-master-plan.md` |
| `spec` already correct | Skip — note |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| backlog_issue_read | TECH-552 | yaml fields + status | MCP or read file |
| validate_backlog | after yaml edit | `validate:backlog-yaml` | node |

### §Acceptance

- [ ] TECH-552 (or successor) existence + decision recorded
- [ ] materialize only when yaml touched

### §Findings

- None at author time.

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
