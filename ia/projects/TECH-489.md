---
purpose: "TECH-489 — User sign-off gate."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/lifecycle-refactor-master-plan.md"
task_key: "T9.1"
---
# TECH-489 — User sign-off gate

> **Issue:** [TECH-489](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Human sign-off gate before merging `feature/lifecycle-collapse-cognitive-split`. Collect artifacts, poll user, record gate row.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Surface dry-run artifacts (M7.dry-run + BACKLOG diff + progress.html).
2. Capture explicit sign-off string + timestamp in migration JSON M8.gate.

### 2.2 Non-Goals (Out of Scope)

1. Running the merge itself (T9.3 scope).
2. Restarting MCP (T9.2 scope).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | As a developer, I want user sign-off recorded before merge so that no unauthorized merge lands. | M8.gate row present with ISO8601 timestamp. |

## 4. Current State

### 4.1 Domain behavior

Dry-run artifacts exist on branch; no gate row in migration JSON yet.

### 4.2 Systems map

- `ia/state/lifecycle-refactor-migration.json` — M8.gate row target.
- `docs/progress.html` — dry-run screenshot source.
- `BACKLOG.md` — diff source.

### 4.3 Implementation investigation notes (optional)

None.

## 5. Proposed Design

### 5.1 Target behavior (product)

Present artifacts. Wait for explicit sign-off. Write gate row. Block T9.3 until gate present.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Read migration JSON M8 section; append gate sub-key with sign-off text + `signed_at` ISO8601.

### 5.3 Method / algorithm notes (optional)

None.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Gate row written to migration JSON M8 | Single source of truth for merge prerequisite | Separate gate file — rejected, adds surface |

## 7. Implementation Plan

### Phase 1 — Surface artifacts + poll

- [ ] Read `ia/state/lifecycle-refactor-migration.json` M7.dry-run row.
- [ ] Produce `BACKLOG.md` diff summary.
- [ ] Reference `docs/progress.html` screenshot path.
- [ ] Present to user; await explicit "LGTM" / "merge".

### Phase 2 — Record gate

- [ ] Write M8.gate entry to migration JSON with sign-off text + ISO8601 timestamp.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Migration JSON M8.gate row present | Manual inspect | `cat ia/state/lifecycle-refactor-migration.json` | Gate row must have `signed_at` field. |

## 8. Acceptance Criteria

- [ ] Dry-run artifacts surfaced to user (migration JSON M7.dry-run row + BACKLOG diff + progress.html screenshot).
- [ ] Explicit user sign-off captured verbatim ("LGTM" / "merge" / equivalent).
- [ ] Migration JSON M8.gate entry written with sign-off text + ISO8601 timestamp.
- [ ] No merge (T9.3) dispatch until gate row present.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- None yet.

## §Plan Author

### §Audit Notes

- Risk: human sign-off captured informally in chat, not recorded. Mitigation: gate Phase 2 on explicit verbatim quote copy into `M8.gate.signoff_text`; refuse merge dispatch if absent.
- Risk: T9.3 merge fires before gate row written (ordering bug). Mitigation: T9.3 Phase 1 asserts `M8.gate.signed_at` present before `git merge`; missing → stop.
- Risk: migration JSON schema drift (M8.gate shape inconsistent with prior M gates). Mitigation: mirror M7 row shape — `{signoff_text: string, signed_at: ISO8601, artifacts_presented: [string]}`.
- Ambiguity: what counts as valid sign-off string beyond "LGTM" / "merge". Resolution: any short affirmative verbatim from user on the artifacts message; quoted in gate row.
- Invariant touch: `ia/state/lifecycle-refactor-migration.json` is hand-authored JSON; do NOT run auto-formatter that reorders keys.

### §Examples

| Input | Expected output | Notes |
|-------|-----------------|-------|
| User replies "LGTM" after artifact message | `M8.gate.signoff_text = "LGTM"`, `signed_at = "2026-04-19T14:22:00Z"` | Canonical happy path. |
| User replies "merge it" | `signoff_text = "merge it"` | Verbatim capture; no normalization. |
| User replies "wait, show me BACKLOG diff again" | No gate row written; loop back to Phase 1 artifact surface | Non-affirmative → block. |
| Gate row present but `signed_at` missing | Validation error; T9.3 refuses dispatch | Schema enforcement. |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| gate_row_shape | M8.gate sub-key after Phase 2 | has `signoff_text` + `signed_at` + `artifacts_presented` | manual |
| signoff_verbatim | user reply "LGTM" | `signoff_text == "LGTM"` byte-for-byte | manual |
| timestamp_iso8601 | `signed_at` field | matches `^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$` | node |
| merge_block_without_gate | M8.gate absent, T9.3 Phase 1 | T9.3 stops with missing-gate error | manual |

### §Acceptance

- [ ] M7.dry-run row summary surfaced verbatim in artifact message.
- [ ] `BACKLOG.md` diff summary (added/removed ids) surfaced.
- [ ] `docs/progress.html` screenshot path referenced.
- [ ] User sign-off captured verbatim in `M8.gate.signoff_text`.
- [ ] `signed_at` ISO8601 UTC timestamp present in gate row.
- [ ] `artifacts_presented` array lists all 3 artifacts.
- [ ] T9.3 dispatch blocked until gate row verified present.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

_pending — populated by `/audit` after `/verify-loop` passes. One paragraph: what shipped, what worked, what to watch._

## §Code Review

_pending — populated by `/code-review`. Verdict: PASS | minor (fix-in-place / deferred) | critical (writes `§Code Fix Plan` below)._

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
