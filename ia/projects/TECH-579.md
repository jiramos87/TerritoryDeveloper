---
purpose: "TECH-579 — MEMORY.md oversized-entry promotion."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T2.1.3"
---
# TECH-579 — MEMORY.md oversized-entry promotion

> **Issue:** [TECH-579](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-20
> **Last updated:** 2026-04-20

## 1. Summary

A4: split oversized MEMORY bullets into slug files; keep MEMORY.md as short index with links + hooks.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Every >10-line entry promoted to dedicated file; MEMORY.md lines replaced with pointer rows.
2. `.claude/memory/` populated for repo-scoped content per Intent.
3. User-local paths documented in spec if scope excludes machine-only files.

### 2.2 Non-Goals (Out of Scope)

1. Changing MEMORY format outside promotion + pointers.
2. Telemetry or hooks code changes (other stages).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Long memory entries are linkable files | Each >10-line entry has slug file + index line |

## 4. Current State

### 4.1 Domain behavior

Root `MEMORY.md` and optional `~/.claude-personal/.../memory/MEMORY.md` may hold long inline entries; Stage 2.1 wants index + linked files.

### 4.2 Systems map

Touches: root `MEMORY.md`, `.claude/memory/`, `~/.claude-personal/.../memory/MEMORY.md` (if present on dev machine).

### 4.3 Implementation investigation notes (optional)

User-home paths may be absent in CI; implementer documents what is repo-committed vs local-only.

## 5. Proposed Design

### 5.1 Target behavior (product)

MEMORY indices stay short; detailed prose lives in slug files.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Enumerate entries; write `{slug}.md`; replace with markdown link line.

### 5.3 Method / algorithm notes (optional)

N/A.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-20 | Repo vs user memory dirs | Match Intent column | Single dir only |

## 7. Implementation Plan

### Phase 2 — Promotion sweep

1. Enumerate entries; measure line counts.
2. For each oversized: write `{slug}.md`; replace with `- [Title](slug.md) — hook` line.
3. Commit repo-scoped files; document user-dir handling in §7 notes if needed.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Doc / IA edits only | N/A | `npm run validate:all` | No C# |

## 8. Acceptance Criteria

- [ ] Every >10-line entry promoted to dedicated file; MEMORY.md lines replaced with pointer rows.
- [ ] `.claude/memory/` populated for repo-scoped content per Intent.
- [ ] User-local paths documented in spec if scope excludes machine-only files.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | No root `MEMORY.md` bullet spanned more than 10 physical lines (each list item is one line). | Current index shape | No slug files required; `.claude/memory/` not created. |
| 2 | `~/.claude-personal/projects/.../memory/MEMORY.md` absent on implementer host. | Machine-local optional path | Documented here only; no repo change. |

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

- Risk: `~/.claude-personal/...` not writable in CI — only repo `MEMORY.md` + `.claude/memory/` may be committed. Mitigation: document user-local steps in §7; do not fail task if home path missing on agent host.
- Risk: slug collision if two entries share title. Mitigation: disambiguate `{slug}` with short hash suffix in filename.
- Ambiguity: entries exactly 10 lines — treat as within limit (promote only **>**10 per Intent).

### §Examples

| Entry type | Action |
|------------|--------|
| Root `MEMORY.md` bullet >10 lines | Write `.claude/memory/{slug}.md`; replace bullet with link line |
| User `MEMORY.md` long entry | Write under user memory dir per Intent; same pointer pattern |

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| line_count_promote | each MEMORY bullet | >10 lines → file exists + index line only | manual |
| slug_files | `.claude/memory/` | one file per promoted repo-scoped entry | manual |
| validate_all | repo | exit 0 | node |

### §Acceptance

- [ ] All oversized entries from root `MEMORY.md` promoted per Intent.
- [ ] `.claude/memory/` contains repo-scoped slug files where applicable.
- [ ] User-local handling documented if home paths not touched in repo.

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
