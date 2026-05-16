---
purpose: "TECH-828 — Strict (slug, stage_id) and (task_id) FK pre-check + descriptive error in cron enqueue MCP tools."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-828 — Strict (slug, stage_id) and (task_id) FK pre-check + descriptive error in cron enqueue MCP tools

> **Issue:** [TECH-828](../../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-05-08
> **Last updated:** 2026-05-08

## 1. Summary

Cron enqueue MCP tools (cron_stage_verification_flip_enqueue + siblings under tools/mcp-ia-server/src/tools/cron-*.ts) insert (slug, stage_id) / (task_id) verbatim with NO FK pre-check. Drainer hits FK violation, job row stuck status=done with error populated, no synchronous signal to caller. Implementation TBD. Spec body authored by plan-author at N=1.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Synchronous (slug, stage_id) ∈ ia_stages pre-check in cron_stage_verification_flip_enqueue → reject {code:stage_not_found, message: canonical-list}
2. Symmetric (task_id) ∈ ia_tasks pre-check in cron_task_commit_record_enqueue → {code:task_not_found}
3. Audit + add FK pre-check to all cron-*.ts enqueue tools where drain target FK to ia_stages / ia_tasks
4. Out of scope: drainer-side pre-check, partial-index migration

### 2.2 Non-Goals (Out of Scope)

1. Drainer-side pre-check
2. Partial-index migration

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Prevent silent FK violations in verification-history bridge | Enqueue rejects non-existent (slug, stage_id) / (task_id) synchronously with descriptive error |

## 4. Current State

### 4.1 Domain behavior

Failure mode: async-drain FK-violation post-enqueue leads to silent verification-history gap. Concrete evidence: cron_stage_verification_flip_jobs.job_id=6436292f-a901-43f5-94e8-f386dd20df74, slug=ui-implementation-mvp-rest, stage_id="3" (canonical was "3.0"), error on ia_stage_verifications FK constraint.

### 4.2 Systems map

Cron enqueue tools under tools/mcp-ia-server/src/tools/cron-*.ts. FK targets:
- ia_stages: [ia_tasks, ia_stage_verifications, stage_arch_surfaces, stage_carcass_signals, ia_stage_claims, ia_red_stage_proofs]
- ia_tasks: [ia_task_deps, ia_task_spec_history, ia_task_commits, ia_ship_stage_journal, ia_fix_plan_tuples]

Files to audit:
  - tools/mcp-ia-server/src/tools/cron-stage-verification-flip.ts:81-96
  - tools/mcp-ia-server/src/tools/cron-task-commit-record.ts
  - tools/mcp-ia-server/src/tools/cron-journal-append.ts
  - tools/mcp-ia-server/src/tools/cron-audit-log.ts
  - tools/mcp-ia-server/src/tools/cron-*.ts (full audit)

### 4.3 Implementation investigation notes (optional)

Server-side belt-and-suspenders beyond ship-cycle agent-side guardrail "Stage_id literal match" added 2026-05-08.

## 5. Proposed Design

### 5.1 Target behavior (product)

Enqueue with non-existent (slug, stage_id) → synchronous invalid_input-class error, no row in job table.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

TBD by plan-author at N=1.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-05-08 | Server-side pre-check added | Belt-and-suspenders beyond agent guardrail | Drainer-side only (insufficient) |

## 7. Implementation Plan

_pending — plan-author writes phases at N=1._

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Enqueue with non-existent (slug, stage_id) → synchronous invalid_input-class error, no row in job table | Node | `npm run validate:all` (repo root) | Chains **validate:dead-project-specs**, **test:ia**, **validate:fixtures**, **generate:ia-indexes --check** |
| Existing valid enqueue path unchanged, P95 < 100 ms preserved | Node | Performance baseline | Verify via db load test or cron drainer metrics |
| FK-violation drain rate = 0 for newly-enqueued rows post-rollout | Integration | cron-server post-deploy monitoring | Audit cron_jobs table for error rows |

## 8. Acceptance Criteria

- [ ] Enqueue with non-existent (slug, stage_id) → synchronous invalid_input-class error, no row in job table
- [ ] Existing valid enqueue path unchanged, P95 < 100 ms preserved
- [ ] FK-violation drain rate = 0 for newly-enqueued rows post-rollout

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | … | … | … |

## 10. Lessons Learned

- …

## §Plan Digest

_pending — plan-author writes phases at N=1._

### §Goal

<!-- 1–2 sentences — task outcome in product / domain terms. -->

### §Acceptance

<!-- Refined per-Task acceptance — narrower than Stage Exit. Checkbox list. -->

### §Test Blueprint

<!-- Structured tuples consumed by `/implement` + `/verify-loop`. -->

### §Examples

<!-- Concrete inputs/outputs + edge cases. Tables or code blocks. -->

### §Mechanical Steps

<!-- Sequential, pre-decided. Each step: Goal / Edits (before+after) / Gate / STOP / MCP hints. -->

## Open Questions (resolve before / during implementation)

1. Glossary rows missing for ia_stages / cron_jobs_all / cron_stage_verification_flip_enqueue → add as part of this issue or separate?
2. Pre-check via db_read_batch tool reuse vs raw SELECT in same pool? (P95 < 100 ms target)
3. Error code shape: invalid_input class (existing) vs new stage_not_found / task_not_found subclass?

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
