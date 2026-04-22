---
purpose: "TECH-666 — Stale check mode."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: ia/projects/grid-asset-visual-registry-master-plan.md
task_key: T2.1.5
phases:
  - "Phase 2 — --check flag"
---
# TECH-666 — Stale check mode

> **Issue:** [TECH-666](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-22
> **Last updated:** 2026-04-22

## 1. Summary

Add CLI mode that recomputes export and compares fingerprint to committed artifact; fails when developers forget to refresh snapshot.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Deterministic hash of inputs (connection string excluded; schema + published rows + export version).
2. Exit code 0 match, non-zero drift.
3. Document optional CI wiring (non-blocking advisory acceptable).

### 2.2 Non-Goals (Out of Scope)

1. Blocking CI on every PR without maintainer opt-in (document as optional).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Run catalog:export --check in CI or locally | Exit 1 when on-disk snapshot stale |

## 4. Current State

### 4.1 Domain behavior

No drift gate yet.

### 4.2 Systems map

tools/catalog-export CLI argv parsing, crypto.createHash or stable stringify, CI doc snippet in task §Findings or README.

## 5. Proposed Design

### 5.1 Target behavior (product)

Repo snapshot stays in sync with DB contract or CI fails loudly.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Hash serialized canonical export + schemaVersion; compare to committed file bytes or sidecar hash.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-22 | Scope from Stage 2.1 orchestrator | Filed via stage-file | — |

## 7. Implementation Plan

### Phase 2 — --check flag

- [ ] Parse `--check`, run export in memory only, compare `snapshotForDriftCheck(disk)` vs `snapshotForDriftCheck(live)` (excludes `generatedAt`); stderr on mismatch; exit 1.
- [ ] Root `catalog:export:check` npm script (see §7b).

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Tooling | Node | `npm run validate:all` | |
| CLI drift | Node | Integration: temp file + mismatch | |

## 8. Acceptance Criteria

- [ ] Deterministic hash of inputs (connection string excluded; schema + published rows + export version).
- [ ] Exit code 0 match, non-zero drift.
- [ ] Document optional CI wiring (non-blocking advisory acceptable).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | — | — | — |

## 10. Lessons Learned

- …

## §Plan Digest

### §Goal

Add `catalog:export:check` (or argv flag on shared CLI) comparing deterministic hash of export output to on-disk snapshot for CI drift signaling.

### §Acceptance

- [ ] Non-zero exit when snapshot bytes drift from regenerated export.
- [ ] Hash excludes secrets; documents inputs in §7.
- [ ] Advisory CI usage documented in §Findings without blocking `validate:all` unless opted in.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| check_pass | snapshot matches | exit 0 | node |
| check_fail | mutated file | exit non-zero | node |

### §Examples

| Flag | Behavior |
|------|----------|
| `--check` | no write; compare only |

### §Mechanical Steps

#### Step 1 — extend §7 for hash semantics

**Goal:** Capture deterministic hash inputs for exploration §7 baker themes.

**Edits:**

- `ia/projects/TECH-666.md` — **before**:

```
- [ ] Parse args, run export in memory, diff vs on-disk file, stderr message on mismatch.
```

  **after**:

```
- [ ] Parse args (`--check`), run export in memory only, compute digest of canonical JSON bytes, compare to on-disk snapshot path from TECH-664; stderr on mismatch; exit codes documented in §7b.
```

**Gate:**

```bash
npm run validate:dead-project-specs
```

**STOP:** Repair §7 numbering if phases change.

**MCP hints:** `backlog_issue`

#### Step 2 — register check script beside export stub

**Goal:** CI can call check without invoking full `validate:all`.

**Edits:**

- `package.json` — **before**:

```
    "catalog:export": "node -e \"process.exit(0)\"",
    "validate:parent-plan-locator": "node tools/validate-parent-plan-locator.mjs",
```

  **after**:

```
    "catalog:export": "node -e \"process.exit(0)\"",
    "catalog:export:check": "node -e \"process.exit(0)\"",
    "validate:parent-plan-locator": "node tools/validate-parent-plan-locator.mjs",
```

**Gate:**

```bash
node tools/validate-dead-project-spec-paths.mjs
```

**STOP:** Ensure TECH-662 landed first so `catalog:export` line exists; otherwise insert both lines in one edit during implement.

**MCP hints:** `plan_digest_resolve_anchor`

## Open Questions (resolve before / during implementation)

1. None — tooling only; see §8.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes._

## §Code Review

_pending — populated by `/code-review`._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed._
