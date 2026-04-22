---
purpose: "TECH-661 — Capture Lighthouse baseline (LCP / CLS / TBT) on all 4 production routes (`local"
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/web-platform-master-plan.md
task_key: T27.7
phases:
  - "Phase 1 — Implement"
---
# TECH-661 — Capture Lighthouse baseline (LCP / CLS / TBT) on all 4 production rout

> **Issue:** [TECH-661](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Stage 27 T27.7: Capture Lighthouse baseline (LCP / CLS / TBT) on all 4 production routes (`localhost:4000/`, `/dashboard`, `/dashboard/r…

## 2. Goals and Non-Goals

### 2.1 Goals

1. Meet master-plan Intent for T27.7.
2. npm run validate:web green.
3. Preserve locked D4/D7 server contracts where applicable.

### 2.2 Non-Goals (Out of Scope)

1. Change Stage 7.2 server loader contracts (presentation-only port per D4).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Ship Stage 27 T27.7 per web-platform master plan | §8 satisfied; validate:web green |

## 4. Current State

### 4.1 Domain behavior

Web dashboard + landing consume CD-derived console chrome; Stage 7.2 data plane locked.

### 4.2 Systems map

web/ App Router pages, design-refs bundle read-only, tools/scripts when task touches audit harness.

## 5. Proposed Design

### 5.1 Target behavior (product)

Match master-plan Intent column for T27.7; preserve full-English user-facing copy where applicable (CLAUDE.md §6).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Follow **§Plan Digest** after plan-digest; CD bundle under `web/design-refs/step-8-console/` read-only per D4.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|-------------------------|
| 2026-04-22 | Scope from Stage 27 orchestrator | Filed via stage-file | — |

## 7. Implementation Plan

### Phase 1 — Implement

- [ ] Execute per master-plan Intent T27.7.
- [ ] validate:web.
- [ ] Lighthouse baseline + post tables copied into PR; caps enforced per Stage exit.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Web regression | Node | `npm run validate:web` | Required each task |

## 8. Acceptance Criteria

- [ ] 1. Meet master-plan Intent for T27.7.
- [ ] 2. npm run validate:web green.
- [ ] 3. Preserve locked D4/D7 server contracts where applicable.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Capture Lighthouse baselines on four production routes before ports land; rerun after ports; enforce LCP ≤ 1.1× baseline and CLS < 0.1; document NB-CD2 schema diffs in PR text.

### §Acceptance

- [ ] Baseline + post tables attached to PR narrative.
- [ ] Regression called out with proposed Surface motion mitigation when caps fail.
- [ ] `npm run validate:web` exits 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| validate_web | branch state | exit 0 | node |

### §Mechanical Steps

#### Step 1 — record plan in spec

**Goal:** Track lighthouse obligation inside task spec for reviewers.

**Edits:**

- `ia/projects/TECH-661.md` — **before**:

```
- [ ] validate:web.
```

  **after**:

```
- [ ] validate:web.
- [ ] Lighthouse baseline + post tables copied into PR; caps enforced per Stage exit.
```

**Gate:**

```bash
npm run validate:web
```

**STOP:** Fix markdown list indentation if validator complains.

**MCP hints:** `backlog_issue`

#### Step 2 — manual lighthouse capture

**Goal:** Run Lighthouse CLI or devtools capture on the four routes; store numbers off-repo; link in PR.

**Edits:**

- No repository file edits required beyond PR description tables.

**Gate:**

```bash
npm run validate:web
```

**STOP:** When local server unavailable, queue capture after deploy preview; note timing in PR.

**MCP hints:** `backlog_issue`


## Open Questions (resolve before / during implementation)

1. None — tooling + web presentation scope; resolve file-level details in implementation unless product behavior changes.

---

## §Audit

_pending — populated by `/audit` after verify-loop._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` when critical only._
