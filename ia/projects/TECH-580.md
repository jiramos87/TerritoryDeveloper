---
purpose: "TECH-580 — MEMORY.md index validation."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T2.1.4"
---
# TECH-580 — MEMORY.md index validation

> **Issue:** [TECH-580](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

Close A4: index hygiene + link integrity + doc metadata consistency after lifecycle edits.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Both MEMORY.md files ≤200 lines; all links resolve.
2. `docs/agent-lifecycle.md` Status + last-updated front matter still correct post-A1.
3. `npm run validate:all` green.

### 2.2 Non-Goals (Out of Scope)

1. Re-authoring `docs/agent-lifecycle.md` body beyond metadata check.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Indices stay under harness caps | Line counts + links verified |

## 4. Current State

### 4.1 Domain behavior

After T2.1.1–2.1.3, MEMORY files and canonical lifecycle doc must remain consistent and under size limits.

### 4.2 Systems map

Touches: `MEMORY.md` (root), user memory path if applicable, `docs/agent-lifecycle.md` metadata.

### 4.3 Implementation investigation notes (optional)

None.

## 5. Proposed Design

### 5.1 Target behavior (product)

No broken pointers; doc metadata accurate; validators green.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Scripted or manual line count + link check + front-matter read.

### 5.3 Method / algorithm notes (optional)

N/A.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | 200-line cap per exploration harness | Matches Stage Intent | Strict 180 |

## 7. Implementation Plan

### Phase 2 — Validation pass

1. Line-count + link check on MEMORY indices.
2. Verify `docs/agent-lifecycle.md` header fields.
3. `npm run validate:all`.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Doc / IA edits only | N/A | `npm run validate:all` | No C# |

## 8. Acceptance Criteria

- [ ] Both MEMORY.md files ≤200 lines; all links resolve.
- [ ] `docs/agent-lifecycle.md` Status + last-updated front matter still correct post-A1.
- [ ] `npm run validate:all` green.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | User-scoped `MEMORY.md` not present locally. | Optional path | N/A for link scan; root index only. |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: TECH-580 runs before TECH-579 finishes — link check fails on partial promotion. Mitigation: implement order T2.1.3 → T2.1.4 on branch; re-run link scan after T2.1.3 merge.
- Risk: `docs/agent-lifecycle.md` front matter format not matching other canonical docs. Mitigation: align `Status:` / last-updated with existing header pattern in that file only.

### §Examples

| File | Check |
|------|--------|
| `MEMORY.md` (root) | ≤200 lines; markdown links resolve |
| User memory file | same, if present |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| memory_line_cap | both MEMORY paths | each ≤200 lines | wc / manual |
| link_resolve | all pointers in indices | target paths exist | manual |
| agent_lifecycle_meta | `docs/agent-lifecycle.md` header | Status + last-updated consistent post-A1 | manual |
| validate_all | repo | exit 0 | node |

### §Acceptance

- [ ] Both MEMORY indices ≤200 lines; pointers resolve.
- [ ] `docs/agent-lifecycle.md` metadata verified after A1 tasks.
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
